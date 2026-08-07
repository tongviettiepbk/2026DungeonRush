using System.Collections.Generic;
using UnityEngine;

// Logic loot đồ (không dính UI). Random 1 item từ pool 6 loại: vũ khí (C1) + gear (C2..C6),
// tính sẵn chỉ số chính/phụ rồi trả về LootResult cho tầng UI hiển thị.
// KHÔNG bao giờ ra Cape (C7) / Wing (C8): chúng là class riêng (CapeData/WingData) nên không
// nằm trong pool gear (StaticGearItemData chỉ load GearItemData).
public static class LootService
{
    // Level của item rơi ra (bản cơ bản: cố định Lv1, chỉ số chính đã đủ khác nhau theo rarity).
    public const int LOOT_LEVEL = 1;

    // Bảng công thức chỉ số, cache 1 lần từ Resources (asset Gears/GearStatConfig).
    private static GearStatConfigData gearStatConfig;

    // Random 1 item đúng bậc rarity roll từ ForgeData (theo forgeLevel truyền vào, xem
    // ForgeController.RollRarity). Trả null nếu thiếu config hoặc không còn asset nào để lấy.
    public static LootResult RollOne(int forgeLevel)
    {
        // Đảm bảo data tĩnh đã nạp (gears + weapons pool + config công thức).
        if (GameData.staticData.gears == null || GameData.staticData.weapons == null)
            GameData.staticData.Load();

        if (gearStatConfig == null)
            gearStatConfig = Resources.Load<GearStatConfigData>("Scriptable Objects/Gears/GearStatConfig");

        if (gearStatConfig == null)
        {
            DebugCustom.LogError("[Loot] Thiếu GearStatConfig.");
            return null;
        }

        List<GearItemData> gearPool = GameData.staticData.gears.all;

        // Vũ khí rơi cho người chơi: loại bỏ vũ khí quái/boss.
        List<WeaponData> weaponPool = new List<WeaponData>();
        List<WeaponData> allWeapons = GameData.staticData.weapons.weapons;
        if (allWeapons != null)
        {
            for (int i = 0; i < allWeapons.Count; i++)
            {
                if (allWeapons[i].isMonsterWeapon == false)
                    weaponPool.Add(allWeapons[i]);
            }
        }

        if ((gearPool == null || gearPool.Count == 0) && weaponPool.Count == 0)
        {
            DebugCustom.LogError("[Loot] Cả gear pool lẫn weapon pool đều rỗng.");
            return null;
        }

        // Roll rarity trước theo bảng ForgeData, rồi mới lọc pool theo đúng rarity đó.
        Rarity rarity = ForgeController.RollRarity(forgeLevel);
        List<GearItemData> gearMatches = FilterByRarity(gearPool, rarity);
        List<WeaponData> weaponMatches = FilterByRarity(weaponPool, rarity);

        // Rarity roll ra chưa có asset nào (thiếu content) -> lùi dần xuống rarity thấp hơn.
        while (gearMatches.Count == 0 && weaponMatches.Count == 0 && rarity > Rarity.Common)
        {
            rarity--;
            gearMatches = FilterByRarity(gearPool, rarity);
            weaponMatches = FilterByRarity(weaponPool, rarity);
        }

        if (gearMatches.Count == 0 && weaponMatches.Count == 0)
        {
            DebugCustom.LogError("[Loot] Không có asset nào cho rarity đã roll.");
            return null;
        }

        // Random đều tay trên tập đã lọc để tỉ lệ ra vũ khí đúng theo số lượng asset cùng rarity.
        bool lootWeapon = weaponMatches.Count > 0 && (gearMatches.Count == 0 || Random.Range(0, gearMatches.Count + weaponMatches.Count) >= gearMatches.Count);

        return lootWeapon
            ? BuildWeaponResult(weaponMatches[Random.Range(0, weaponMatches.Count)])
            : BuildGearResult(gearMatches[Random.Range(0, gearMatches.Count)]);
    }

    private static List<GearItemData> FilterByRarity(List<GearItemData> pool, Rarity rarity)
    {
        List<GearItemData> result = new List<GearItemData>();
        if (pool == null) return result;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].rarity == rarity)
                result.Add(pool[i]);
        }
        return result;
    }

    private static List<WeaponData> FilterByRarity(List<WeaponData> pool, Rarity rarity)
    {
        List<WeaponData> result = new List<WeaponData>();
        if (pool == null) return result;

        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].rarity == rarity)
                result.Add(pool[i]);
        }
        return result;
    }

    // Dựng LootResult cho 1 gear (C2..C6).
    private static LootResult BuildGearResult(GearItemData gear)
    {
        GearMainStatKind mainKind;
        double mainStat = GearStatCalculator.GetGearMainStat(gearStatConfig, gear.slot, gear.rarity, LOOT_LEVEL, out mainKind);

        return new LootResult
        {
            kind = LootItemKind.Gear,
            displayName = gear.displayName,
            rarity = gear.rarity,
            level = LOOT_LEVEL,
            icon = gear.icon,
            equipId = gear.assetName,
            gearSlot = gear.slot,
            mainStatKind = mainKind,
            mainStat = mainStat,
            subStats = GearStatCalculator.RollSubStats(gearStatConfig, gear.rarity),
        };
    }

    // Dựng LootResult cho 1 vũ khí (C1) — chỉ số chính luôn là Sát thương.
    private static LootResult BuildWeaponResult(WeaponData weapon)
    {
        double mainStat = GearStatCalculator.GetWeaponMainStat(gearStatConfig, weapon.weaponType, weapon.rarity, LOOT_LEVEL);

        return new LootResult
        {
            kind = LootItemKind.Weapon,
            displayName = weapon.displayName,
            rarity = weapon.rarity,
            level = LOOT_LEVEL,
            icon = weapon.icon,
            equipId = weapon.assetName,
            weaponType = weapon.weaponType,
            mainStatKind = GearMainStatKind.Damage,
            mainStat = mainStat,
            subStats = GearStatCalculator.RollSubStats(gearStatConfig, weapon.rarity),
        };
    }
}
