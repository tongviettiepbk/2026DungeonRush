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

    // Random 1 item. Trả null nếu thiếu config hoặc cả 2 pool đều rỗng (đã log lỗi bên trong).
    public static LootResult RollOne()
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

        int gearCount = gearPool != null ? gearPool.Count : 0;
        if (gearCount == 0 && weaponPool.Count == 0)
        {
            DebugCustom.LogError("[Loot] Cả gear pool lẫn weapon pool đều rỗng.");
            return null;
        }

        // Random đều tay trên toàn bộ (gear + vũ khí) để tỉ lệ ra vũ khí đúng theo số lượng asset.
        bool lootWeapon = weaponPool.Count > 0 && (gearCount == 0 || Random.Range(0, gearCount + weaponPool.Count) >= gearCount);

        return lootWeapon
            ? BuildWeaponResult(weaponPool[Random.Range(0, weaponPool.Count)])
            : BuildGearResult(gearPool[Random.Range(0, gearCount)]);
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
            weaponType = weapon.weaponType,
            mainStatKind = GearMainStatKind.Damage,
            mainStat = mainStat,
            subStats = GearStatCalculator.RollSubStats(gearStatConfig, weapon.rarity),
        };
    }
}
