using System.Collections.Generic;
using UnityEngine;

// Logic hệ Wing — mirror WingCraftTabPage / WingCraftPopup / WingRerollPopup gốc (il2cpp v41). Stateless,
// thao tác trên GameData.userData.wings (+ quặng ở userData.items). Mỗi hàm đổi dữ liệu tự Save; UI gọi
// xong thì post EventID.WingChanged (+ EquipmentChanged(WING) nếu wing đang mặc đổi chỉ số).
public static class WingService
{
    // GameResources.WingUnlockPlayerLevel (đọc từ asset gốc).
    public const int UNLOCK_PLAYER_LEVEL = 6;

    private static UserWingData User => GameData.userData.wings;

    public static bool IsUnlocked()
    {
        return GameData.userData.player.playerLevel >= UNLOCK_PLAYER_LEVEL;
    }

    // ----- Quặng -----

    public static ItemType ToItemType(MineOreType ore)
    {
        return ItemType.COAL_ORE + (int)ore;
    }

    // UserController.ecx
    public static int GetOre(MineOreType ore)
    {
        return (int)GameData.userData.items.GetQuantityHave(ToItemType(ore));
    }

    // ----- Tra cứu -----

    public static WingModel GetOwned(WingData wing)
    {
        return wing != null ? User.Get(wing.wingId) : null;
    }

    // UserController.eea: id wing đang mặc (-1 = không mặc).
    public static int GetEquippedId()
    {
        string id = GameData.userData.equipment.GetEquipped(GearSlotType.WING);
        return int.TryParse(id, out int wingId) ? wingId : -1;
    }

    public static bool IsEquipped(WingData wing)
    {
        return wing != null && GetEquippedId() == wing.wingId;
    }

    // ----- Chế tạo (WingCraftPopup.kye) -----

    // Đủ quặng → trừ CraftOreCost → thêm WingModel level 1, IsNew, substat = bản sao WingData.SubStats.
    // Trả false nếu thiếu quặng (UI báo "Không đủ tài nguyên").
    public static bool Craft(WingData wing)
    {
        if (GetOre(wing.craftOreType) < wing.craftOreCost)
        {
            return false;
        }

        GameData.userData.items.Consume(ToItemType(wing.craftOreType), wing.craftOreCost);
        if (User.Get(wing.wingId) == null)
        {
            WingModel model = new WingModel
            {
                wingId = wing.wingId,
                level = 1,
                isNew = true,
                subStats = new List<GearSubStat>(),
            };
            for (int i = 0; i < wing.subStats.Count; i++)
            {
                model.subStats.Add(new GearSubStat(wing.subStats[i].type, wing.subStats[i].value));
            }
            User.Add(model);
        }

        GameData.Save();
        return true;
    }

    // ----- Lên cấp (WingCraftTabPage.kzc) -----

    public static bool IsMaxLevel(WingData wing, WingModel model)
    {
        return model.level >= wing.maxLevel;
    }

    // Giá lên cấp từ `level`: round(LevelUpCost × LevelUpCostMultiplier^(level−1)) — float như gốc, Math.Round về số chẵn.
    public static int GetLevelUpCost(WingData wing, int level)
    {
        float cost = Mathf.Pow(wing.levelUpCostMultiplier, level - 1) * wing.levelUpCost;
        return (int)System.Math.Round((double)cost, System.MidpointRounding.ToEven);
    }

    // Trả false nếu đã max hoặc thiếu quặng.
    public static bool LevelUp(WingData wing)
    {
        WingModel model = GetOwned(wing);
        if (model == null || IsMaxLevel(wing, model))
        {
            return false;
        }

        int cost = GetLevelUpCost(wing, model.level);
        if (GetOre(wing.levelUpOreType) < cost)
        {
            return false;
        }

        GameData.userData.items.Consume(ToItemType(wing.levelUpOreType), cost);
        model.level++;
        User.isDataChanged = true;
        SyncEquipped(wing, model);
        GameData.Save();
        return true;
    }

    // ----- Reroll substat (WingRerollPopup.lad / laf / lag) -----

    // Giá reroll = RerollCosts[min(số dòng khoá, Count−1)] (lad).
    public static int GetRerollCost(WingData wing, int lockedCount)
    {
        if (wing.rerollCosts == null || wing.rerollCosts.Count == 0)
        {
            return 0;
        }
        return wing.rerollCosts[Mathf.Min(lockedCount, wing.rerollCosts.Count - 1)];
    }

    // locked[i] = dòng substat i đang khoá (giữ nguyên). Dòng không khoá roll lại: loại KHÁC mọi loại hiện có
    // và khác các dòng vừa roll; giá trị roll như đồ thường (GameResources.jhh). Trả false nếu thiếu quặng
    // (UI tự chặn trường hợp khoá hết trước khi gọi).
    public static bool Reroll(WingData wing, List<bool> locked)
    {
        WingModel model = GetOwned(wing);
        if (model == null)
        {
            return false;
        }

        int lockedCount = CountLocked(locked);
        int cost = GetRerollCost(wing, lockedCount);
        if (lockedCount >= model.subStats.Count || GetOre(wing.rerollOreType) < cost)
        {
            return false;
        }

        GameData.userData.items.Consume(ToItemType(wing.rerollOreType), cost);

        List<SubStatType> exclude = new List<SubStatType>();
        for (int i = 0; i < model.subStats.Count; i++)
        {
            exclude.Add(model.subStats[i].type);
        }

        for (int i = 0; i < model.subStats.Count; i++)
        {
            if (i < locked.Count && locked[i])
            {
                continue;
            }

            GearSubStat rolled = RollSubStat(exclude);
            exclude.Add(rolled.type);
            model.subStats[i] = rolled;
        }

        User.isDataChanged = true;
        SyncEquipped(wing, model);
        GameData.Save();
        return true;
    }

    public static int CountLocked(List<bool> locked)
    {
        int count = 0;
        for (int i = 0; i < locked.Count; i++)
        {
            if (locked[i])
            {
                count++;
            }
        }
        return count;
    }

    // lag: pool = SubStatConfigs có weight >= 1 và loại chưa nằm trong exclude → random theo weight.
    // Pool rỗng → (Damage, 1) như gốc.
    private static GearSubStat RollSubStat(List<SubStatType> exclude)
    {
        GearStatConfigData config = EquipmentStatResolver.LoadConfig();
        List<SubStatPoolEntry> pool = new List<SubStatPoolEntry>();
        int total = 0;
        for (int i = 0; i < config.subStatPool.Count; i++)
        {
            SubStatPoolEntry entry = config.subStatPool[i];
            if (entry.weight >= 1 && exclude.Contains(entry.type) == false)
            {
                pool.Add(entry);
                total += entry.weight;
            }
        }

        if (pool.Count == 0)
        {
            return new GearSubStat(SubStatType.Damage, 1f);
        }

        int roll = Random.Range(0, total);
        int acc = 0;
        SubStatPoolEntry picked = pool[0];
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (roll < acc)
            {
                picked = pool[i];
                break;
            }
        }

        return new GearSubStat(picked.type, GearStatCalculator.RollSubStatValue(picked.maxValue, config.subStatDistributionSpread));
    }

    // ----- Mặc / cởi (UserController.eeb) -----

    public static void Equip(WingData wing)
    {
        WingModel model = GetOwned(wing);
        if (model == null)
        {
            return;
        }

        GameData.userData.equipment.Equip(GearSlotType.WING, wing.wingId.ToString(), wing.rarity, model.level, model.subStats);
        GameData.Save();
    }

    public static void Unequip()
    {
        GameData.userData.equipment.Unequip(GearSlotType.WING);
        GameData.Save();
    }

    // Bản ghi slot WING copy level/subStats → wing đang mặc đổi thì ghi lại.
    private static void SyncEquipped(WingData wing, WingModel model)
    {
        if (IsEquipped(wing))
        {
            GameData.userData.equipment.Equip(GearSlotType.WING, wing.wingId.ToString(), wing.rarity, model.level, model.subStats);
        }
    }
}
