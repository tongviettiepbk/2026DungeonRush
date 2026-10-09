using UnityEngine;

// Số liệu hệ Enchantment ("Relic") — REVERSE il2cpp v41 (static class `qm` + hằng EnchantmentTabPage
// + UserController.edo..edy). Xem DecodedData/ENCHANTMENT_MODEL.md.
//   • Relic chỉ có TIER (1..11), không có id/level riêng. Kho = số lượng mỗi tier.
//   • Đeo 1 relic vào 1 slot trang bị (8 slot) → main stat món ở slot đó × (1 + tier²/100).
//   • Summon (tiêu Vial) ra tier 1..6 theo Summon Level (max 50); tier 7..11 chỉ có qua Merge.
//   • Merge 3 relic cùng tier → 1 relic tier+1. Dismantle 1 relic tier t → 3 relic tier t-1.
public static class EnchantmentConfig
{
    public const int MAX_TIER = 11;               // qm.xkk
    public const int SUMMON_TIER_COUNT = 6;       // qm.xkl — summon chỉ ra tier 1..6
    public const int MERGE_COUNT = 3;             // qm.xkm
    public const int MAX_SUMMON_LEVEL = 50;       // qm.xkj / qm.hxb

    // EnchantmentTabPage: 1 "lần bấm" (multiplier x1) = 100 Vial → 5 lượt summon.
    public const int VIAL_PER_MULTIPLIER = 100;   // xlr
    public const int SUMMON_PER_MULTIPLIER = 5;   // xls
    // Nút đổi multiplier chỉ hiện khi Vial >= 2000 (dưới mức này multiplier bị reset về x1).
    public const int MULTIPLIER_UNLOCK_VIAL = 2000; // xlt
    public static readonly int[] MULTIPLIERS = { 1, 2, 3, 5, 10, 20, 30 }; // xlu

    // Thứ tự slot khi Quick Equip hoà priority (UserController.ugv = Weapon, Cloak, Wing, Helmet,
    // Backpack, Gloves, Ring, Necklace) — đổi sang GearSlotType của project.
    public static readonly GearSlotType[] SLOT_ORDER =
    {
        GearSlotType.WEAPON,
        GearSlotType.CAPE,
        GearSlotType.WING,
        GearSlotType.HELMET,
        GearSlotType.BACKPACK,
        GearSlotType.GLOVES,
        GearSlotType.RING,
        GearSlotType.NECKLACE,
    };

    // RATES[i] = % ra tier 1..6 ở summon level i+1 (qm.xkn, dựng trong .cctor; mọi hàng tổng 100).
    private static readonly float[,] RATES =
    {
        { 100f, 0f, 0f, 0f, 0f, 0f },              // lv 1
        { 99.5f, 0.5f, 0f, 0f, 0f, 0f },           // lv 2
        { 99.31f, 0.69f, 0f, 0f, 0f, 0f },         // lv 3
        { 99.05f, 0.95f, 0f, 0f, 0f, 0f },         // lv 4
        { 98.69f, 1.31f, 0f, 0f, 0f, 0f },         // lv 5
        { 98.19f, 1.81f, 0f, 0f, 0f, 0f },         // lv 6
        { 97.5f, 2.5f, 0f, 0f, 0f, 0f },           // lv 7
        { 96.55f, 3.45f, 0f, 0f, 0f, 0f },         // lv 8
        { 95f, 5f, 0f, 0f, 0f, 0f },               // lv 9
        { 92.9f, 7f, 0.1f, 0f, 0f, 0f },           // lv 10
        { 90.02f, 9.8f, 0.18f, 0f, 0f, 0f },       // lv 11
        { 85.96f, 13.72f, 0.32f, 0f, 0f, 0f },     // lv 12
        { 80.21f, 19.21f, 0.58f, 0f, 0f, 0f },     // lv 13
        { 72.06f, 26.89f, 1.05f, 0f, 0f, 0f },     // lv 14
        { 60.46f, 37.65f, 1.89f, 0f, 0f, 0f },     // lv 15
        { 43.89f, 52.71f, 3.4f, 0f, 0f, 0f },      // lv 16
        { 35.11f, 59.89f, 5f, 0f, 0f, 0f },        // lv 17
        { 28.09f, 64.81f, 7f, 0.1f, 0f, 0f },      // lv 18
        { 22.47f, 67.55f, 9.8f, 0.18f, 0f, 0f },   // lv 19
        { 17.98f, 67.98f, 13.72f, 0.32f, 0f, 0f }, // lv 20
        { 16.5f, 63.71f, 19.21f, 0.58f, 0f, 0f },  // lv 21
        { 16.5f, 55.56f, 26.89f, 1.05f, 0f, 0f },  // lv 22
        { 16.5f, 43.96f, 37.65f, 1.89f, 0f, 0f },  // lv 23
        { 16.5f, 27.39f, 52.71f, 3.4f, 0f, 0f },   // lv 24
        { 16.5f, 16.5f, 62f, 5f, 0f, 0f },         // lv 25
        { 16.5f, 16.5f, 59.9f, 7f, 0.1f, 0f },     // lv 26
        { 16.5f, 16.5f, 57.02f, 9.8f, 0.18f, 0f }, // lv 27
        { 16.5f, 16.5f, 52.96f, 13.72f, 0.32f, 0f }, // lv 28
        { 16.5f, 16.5f, 47.21f, 19.21f, 0.58f, 0f }, // lv 29
        { 16.5f, 16.5f, 39.06f, 26.89f, 1.05f, 0f }, // lv 30
        { 16.5f, 16.5f, 27.46f, 37.65f, 1.89f, 0f }, // lv 31
        { 16.5f, 16.5f, 16.5f, 47.1f, 3.4f, 0f },  // lv 32
        { 16.5f, 16.5f, 16.5f, 45.5f, 5f, 0f },    // lv 33
        { 16.5f, 16.5f, 16.5f, 43.4f, 7f, 0.1f },  // lv 34
        { 16.5f, 16.5f, 16.5f, 40.52f, 9.8f, 0.18f }, // lv 35
        { 16.5f, 16.5f, 16.5f, 36.46f, 13.72f, 0.32f }, // lv 36
        { 16.5f, 16.5f, 16.5f, 30.71f, 19.21f, 0.58f }, // lv 37
        { 16.5f, 16.5f, 16.5f, 22.56f, 26.89f, 1.05f }, // lv 38
        { 16.5f, 16.5f, 16.5f, 16.5f, 32.11f, 1.89f }, // lv 39
        { 16.5f, 16.5f, 16.5f, 16.5f, 30.6f, 3.4f },  // lv 40
        { 16.5f, 16.5f, 16.5f, 16.5f, 29f, 5f },      // lv 41
        { 16.5f, 16.5f, 16.5f, 16.5f, 28.3f, 5.7f },  // lv 42
        { 16.5f, 16.5f, 16.5f, 16.5f, 27.5f, 6.5f },  // lv 43
        { 16.5f, 16.5f, 16.5f, 16.5f, 26.59f, 7.41f }, // lv 44
        { 16.5f, 16.5f, 16.5f, 16.5f, 25.56f, 8.44f }, // lv 45
        { 16.5f, 16.5f, 16.5f, 16.5f, 24.37f, 9.63f }, // lv 46
        { 16.5f, 16.5f, 16.5f, 16.5f, 23.03f, 10.97f }, // lv 47
        { 16.5f, 16.5f, 16.5f, 16.5f, 21.49f, 12.51f }, // lv 48
        { 16.5f, 16.5f, 16.5f, 16.5f, 19.74f, 14.26f }, // lv 49
        { 17.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f },   // lv 50
    };

    // ----- Summon Level -----

    // Số lượt cần để VƯỢT level `level` (qm.xko = 15, 20, 25 … 255 — 49 phần tử, đúng bằng 10 + 5×level).
    private static int GetRequired(int level)
    {
        return 10 + 5 * level;
    }

    // Summon level 1-based theo tổng lượt đã summon (qm.hwz).
    public static int GetLevel(int totalSummons)
    {
        int acc = 0;
        for (int level = 1; level < MAX_SUMMON_LEVEL; level++)
        {
            acc += GetRequired(level);
            if (acc > totalSummons)
            {
                return level;
            }
        }
        return MAX_SUMMON_LEVEL;
    }

    // Tiến độ trong level hiện tại (qm.hxa): current/required; isMax khi đã ở level 50.
    public static void GetProgress(int totalSummons, out int current, out int required, out bool isMax)
    {
        int acc = 0;
        for (int level = 1; level < MAX_SUMMON_LEVEL; level++)
        {
            int need = GetRequired(level);
            if (acc + need > totalSummons)
            {
                current = totalSummons - acc;
                required = need;
                isMax = false;
                return;
            }
            acc += need;
        }

        current = 0;
        required = 0;
        isMax = true;
    }

    // % ra tier (1..6) ở summon level (qm.hxd/hxf: level clamp 1..50). Tier ngoài 1..6 → 0.
    public static float GetRate(int level, int tier)
    {
        if (tier < 1 || tier > SUMMON_TIER_COUNT)
        {
            return 0f;
        }
        int row = Mathf.Clamp(level, 1, MAX_SUMMON_LEVEL) - 1;
        return RATES[row, tier - 1];
    }

    // Roll tier cho 1 lượt (qm.hxc): roll = Random.Range(0f, 100f), cộng dồn từ tier 6 xuống 2;
    // không trúng mức nào = tier 1.
    public static int RollTier(int level)
    {
        float roll = Random.Range(0f, 100f);
        float acc = 0f;
        for (int tier = SUMMON_TIER_COUNT; tier >= 2; tier--)
        {
            acc += GetRate(level, tier);
            if (roll < acc)
            {
                return tier;
            }
        }
        return 1;
    }

    // ----- Hiệu ứng -----

    // "Relic Power +X%" (qm.hxe): (tier/10) × (tier/10 × 100) = tier². Tier 0 = không có relic.
    public static float GetBuffPercent(int tier)
    {
        return tier >= 1 ? tier * tier : 0f;
    }

    // Hệ số nhân main stat món ở slot đeo relic (Soldier.eyo / rm.iqn): 1 + tier²/100.
    public static float GetStatMultiplier(int tier)
    {
        return 1f + GetBuffPercent(tier) / 100f;
    }

    // Priority slot khi Quick Equip (UserController.edu): rarity món đang mặc + bonus (Cape +4, Wing +1),
    // tối đa 10. Slot có priority cao được relic tier cao trước.
    public static int GetQuickEquipPriority(GearSlotType slot, Rarity rarity)
    {
        int bonus = slot == GearSlotType.CAPE ? 4 : (slot == GearSlotType.WING ? 1 : 0);
        return Mathf.Min((int)rarity + bonus, 10);
    }
}
