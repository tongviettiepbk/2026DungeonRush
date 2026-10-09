using System.Collections.Generic;
using UnityEngine;

// Logic hệ Cape — mirror UserController.efa/efb..efd/eem + CapeConfig.eql/eqm gốc (il2cpp v41). Stateless, thao tác
// trên GameData.userData.capes (+ Cloak ở userData.items). Mỗi hàm đổi dữ liệu tự Save; UI gọi xong thì post
// EventID.CapeChanged (+ EquipmentChanged(CAPE) nếu cape đang mặc đổi chỉ số/hình).
public static class CapeService
{
    // GameResources.CapeUnlockPlayerLevel (đọc từ asset gốc).
    public const int UNLOCK_PLAYER_LEVEL = 15;

    private static UserCapeData User => GameData.userData.capes;
    private static CapeConfigData Config => GameData.staticData.capes.config;

    public static bool IsUnlocked()
    {
        return GameData.userData.player.playerLevel >= UNLOCK_PLAYER_LEVEL;
    }

    public static int GetCloak()
    {
        return (int)GameData.userData.items.GetQuantityHave(ItemType.CLOAK);
    }

    public static CapeData GetData(CapeModel model)
    {
        return model != null ? GameData.staticData.capes.GetData(model.capeId) : null;
    }

    public static CapeModel GetEquipped()
    {
        return string.IsNullOrEmpty(User.equippedInstanceId) ? null : User.Get(User.equippedInstanceId);
    }

    public static bool IsEquipped(CapeModel model)
    {
        return model != null && model.instanceId == User.equippedInstanceId;
    }

    // ----- Summon (CapePopup.etg + UserController.efa) -----

    public static int GetSummonCost(int multiplier)
    {
        return Config.summonCost * multiplier;
    }

    // Đủ Cloak → trừ SummonCost × multiplier → summon `multiplier` cape. Thiếu → null.
    public static List<CapeModel> TrySummon(int multiplier)
    {
        int cost = GetSummonCost(multiplier);
        if (GetCloak() < cost)
        {
            return null;
        }

        GameData.userData.items.Consume(ItemType.CLOAK, cost);
        return Summon(multiplier);
    }

    // Mỗi lượt: rarity theo TotalCapeSummons hiện tại → random 1 mẫu cùng rarity (jhn) → CapeModel level 1, Guid mới,
    // substat roll CapeData.subStatCount dòng (jht) → TotalCapeSummons++.
    public static List<CapeModel> Summon(int count)
    {
        List<CapeModel> results = new List<CapeModel>();
        GearStatConfigData statConfig = EquipmentStatResolver.LoadConfig();
        for (int n = 0; n < count; n++)
        {
            Rarity rarity = CapeSummonConfig.RollRarity(User.totalSummons);
            CapeData data = PickByRarity(rarity);
            if (data == null)
            {
                continue;
            }

            CapeModel model = new CapeModel
            {
                instanceId = System.Guid.NewGuid().ToString(),
                capeId = data.capeId,
                level = 1,
                isNew = true,
                subStats = GearStatCalculator.RollSubStats(statConfig, data.subStatCount),
            };
            User.owned.Add(model);
            User.totalSummons++;
            results.Add(model);
        }

        User.isDataChanged = true;
        GameData.Save();
        return results;
    }

    private static CapeData PickByRarity(Rarity rarity)
    {
        List<CapeData> pool = new List<CapeData>();
        List<CapeData> all = GameData.staticData.capes.capes;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].rarity == rarity)
            {
                pool.Add(all[i]);
            }
        }
        return pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
    }

    // ----- XP / lên cấp (CapeConfig.eql / eqm, UserController.efd) -----

    public static int MaxLevel => Config.maxLevel;

    // XP cần để từ `level` lên level+1. x = (level−1)×0.5:
    //   level ≤ 29: LevelUpBaseXP + LevelUpScaler × x^LevelUpExpo ;  level > 29: LevelUpBaseXP2 + LevelUpScaler2 × x.
    public static int GetLevelUpXP(Rarity rarity, int level)
    {
        CapeRarityConfig cfg = Config.GetRarityConfig(rarity);
        if (cfg == null)
        {
            return int.MaxValue;
        }

        float x = (level - 1) * 0.5f;
        float xp = level > 29
            ? cfg.levelUpBaseXP2 + x * cfg.levelUpScaler2
            : cfg.levelUpBaseXP + cfg.levelUpScaler * Mathf.Pow(x, cfg.levelUpExpo);
        return (int)System.Math.Round((double)xp, System.MidpointRounding.ToEven);
    }

    // XP nhận được khi đem 1 cape đi salvage = SalvageBaseXP + tổng XP các level đã lên (không tính CurrentXP dở dang).
    public static int GetSalvageXP(Rarity rarity, int level)
    {
        CapeRarityConfig cfg = Config.GetRarityConfig(rarity);
        if (cfg == null)
        {
            return 0;
        }

        float xp = cfg.salvageBaseXP;
        for (int l = 1; l < level; l++)
        {
            xp += GetLevelUpXP(rarity, l);
        }
        return (int)System.Math.Round((double)xp, System.MidpointRounding.ToEven);
    }

    // Mô phỏng cộng XP (dùng cho cả preview Salvage lẫn efd thật): lên cấp tới khi thiếu XP hoặc chạm MaxLevel (XP về 0).
    public static void SimulateXP(Rarity rarity, int level, int currentXP, int addXP, out int newLevel, out int newXP)
    {
        newLevel = level;
        newXP = currentXP + addXP;
        while (newLevel < MaxLevel)
        {
            int need = GetLevelUpXP(rarity, newLevel);
            if (newXP < need)
            {
                break;
            }
            newXP -= need;
            newLevel++;
        }

        if (newLevel >= MaxLevel)
        {
            newXP = 0;
        }
    }

    // ----- Salvage (CapeSalvagePopup.euy) -----

    // Tổng XP của các cape được chọn (theo instanceId).
    public static int GetSalvageXP(List<string> instanceIds)
    {
        int xp = 0;
        for (int i = 0; i < instanceIds.Count; i++)
        {
            CapeModel model = User.Get(instanceIds[i]);
            CapeData data = GetData(model);
            if (data != null)
            {
                xp += GetSalvageXP(data.rarity, model.level);
            }
        }
        return xp;
    }

    // Bỏ các cape được chọn khỏi kho → cộng XP cho target. Cape đang mặc không được làm nguyên liệu.
    public static void Salvage(CapeModel target, List<string> instanceIds)
    {
        CapeData data = GetData(target);
        if (data == null)
        {
            return;
        }

        List<string> materials = new List<string>();
        for (int i = 0; i < instanceIds.Count; i++)
        {
            if (instanceIds[i] != target.instanceId && instanceIds[i] != User.equippedInstanceId)
            {
                materials.Add(instanceIds[i]);
            }
        }

        int xp = GetSalvageXP(materials);
        for (int i = 0; i < materials.Count; i++)
        {
            User.Remove(materials[i]);
        }

        SimulateXP(data.rarity, target.level, target.currentXP, xp, out int level, out int currentXP);
        target.level = level;
        target.currentXP = currentXP;
        User.isDataChanged = true;
        SyncEquipped(target);
        GameData.Save();
    }

    // ----- Mặc / cởi (UserController.eem) -----

    public static void Equip(CapeModel model)
    {
        CapeData data = GetData(model);
        if (data == null)
        {
            return;
        }

        User.equippedInstanceId = model.instanceId;
        User.isDataChanged = true;
        GameData.userData.equipment.Equip(GearSlotType.CAPE, data.capeId.ToString(), data.rarity, model.level, model.subStats);
        GameData.Save();
    }

    public static void Unequip()
    {
        User.equippedInstanceId = null;
        User.isDataChanged = true;
        GameData.userData.equipment.Unequip(GearSlotType.CAPE);
        GameData.Save();
    }

    // Bản ghi slot CAPE copy level/subStats → cape đang mặc đổi thì ghi lại.
    private static void SyncEquipped(CapeModel model)
    {
        if (IsEquipped(model))
        {
            CapeData data = GetData(model);
            GameData.userData.equipment.Equip(GearSlotType.CAPE, data.capeId.ToString(), data.rarity, model.level, model.subStats);
        }
    }

    // ----- Ẩn/hiện áo choàng (UserController.efg) -----

    public static bool IsShowCloak()
    {
        return User.showCloak;
    }

    public static void SetShowCloak(bool isShow)
    {
        User.showCloak = isShow;
        User.isDataChanged = true;
        GameData.Save();
    }
}
