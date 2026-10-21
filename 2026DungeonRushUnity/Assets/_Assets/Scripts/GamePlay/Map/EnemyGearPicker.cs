using System.Collections.Generic;
using UnityEngine;

// Chọn vũ khí cho quái CAMPAIGN — port GameResources.jgz/jha/jhd gốc (reverse libil2cpp.so bản 41).
// Gọi từ SpawnController.huy khi KHÔNG ở dungeon: quái chỉ cầm vũ khí (không mũ/găng/animator).
// Vũ khí chỉ đổi HÌNH + kiểu đánh (cận/xa, tầm đánh, đạn); damage/HP vẫn do Lancaster (EnemySpawnGenerator).
//
//   jgz(level): t = clamp01(level / EnemyGearMaxCombatLevel) × tổng EnemyGearTierWeights;
//               tier = chỉ số đầu tiên mà t < tổng cộng dồn; level > Max → tier = số phần tử.
//   jha(level, role): rnd = new Random(level×997 + (Ranged ? 7 : 3) + EnemyGearSeed×31)
//   jhd: rarity = min(tier, 11) - 1; tier 0 → melee TAY KHÔNG, ranged lấy Common.
//        Pool = vũ khí người chơi rarity đó, đúng loại (melee/ranged), giữ THỨ TỰ list gốc;
//        rỗng thì lùi rarity (Ultimate không có vũ khí → lấy Divine). Chọn pool[rnd.Next(0, count)].
// Chi tiết: DecodedData/ENEMY_STATS_MODEL.md mục 4.
public static class EnemyGearPicker
{
    // ===== Hằng số THẬT (GameResources) =====
    private const int ENEMY_GEAR_SEED = 12345;
    private const int ENEMY_GEAR_MAX_COMBAT_LEVEL = 300;
    private static readonly int[] TIER_WEIGHTS = { 5, 15, 25, 25, 25, 25, 30, 30, 40, 40, 40 };
    private const int RARITY_COUNT = 11;                     // Enum.GetValues(CharacterRarity).Length

    // GameResources.WeaponDatas gốc — THỨ TỰ quyết định món nào được rnd chọn (không theo itemId/tên).
    // Đọc từ GameResources trong apk bản 41 (UnityPy + typetree).
    private static readonly string[] WEAPON_ORDER =
    {
        "Weapon_m_1_2", "Weapon_m_1_1", "Weapon_m_2_2", "Weapon_m_2_1", "Weapon_m_3_2",
        "Weapon_r_1_1", "Weapon_m_4_1", "Weapon_m_4_2", "Weapon_m_5_1", "Weapon_m_5_2",
        "Weapon_r_1_2", "Weapon_r_2_1", "Weapon_r_2_2", "Weapon_r_3_2", "Weapon_r_3_1",
        "Weapon_r_4_1", "Weapon_r_4_2", "Weapon_r_5_2", "Weapon_r_5_1", "Weapon_m_1_3",
        "Weapon_m_2_3", "Weapon_m_3_3", "Weapon_m_4_3", "Weapon_m_5_3", "Weapon_m_3_1",
        "Weapon_r_5_3", "Weapon_r_3_3", "Weapon_r_1_3", "Weapon_r_4_3", "Weapon_m_6_1",
        "Weapon_m_6_2", "Weapon_m_6_3", "Weapon_m_7_1", "Weapon_m_7_2", "Weapon_m_7_3",
        "Weapon_r_6_1", "Weapon_r_6_2", "Weapon_r_6_3", "Weapon_r_7_1", "Weapon_r_7_2",
        "Weapon_r_7_3", "Weapon_m_8_1", "Weapon_m_8_2", "Weapon_m_8_3", "Weapon_r_8_1",
        "Weapon_r_8_2", "Weapon_r_8_3", "Weapon_r_9_1", "Weapon_r_9_2", "Weapon_r_9_3",
        "Weapon_r_10_1", "Weapon_r_10_2", "Weapon_r_10_3", "Weapon_m_9_1", "Weapon_m_9_2",
        "Weapon_m_9_3", "Weapon_m_10_1", "Weapon_m_10_2", "Weapon_m_10_3",
    };

    // jgz: tier vũ khí quái theo combatLevel (0..10, level > 300 → 11).
    public static int TierForLevel(int combatLevel)
    {
        if (combatLevel > ENEMY_GEAR_MAX_COMBAT_LEVEL)
        {
            return TIER_WEIGHTS.Length;
        }

        float t = Mathf.Max(0f, Mathf.Min((float)combatLevel / ENEMY_GEAR_MAX_COMBAT_LEVEL, 1f));
        int total = 0;
        for (int i = 0; i < TIER_WEIGHTS.Length; i++) total += TIER_WEIGHTS[i];
        t *= total;

        int sum = 0;
        for (int i = 0; i < TIER_WEIGHTS.Length; i++)
        {
            sum += TIER_WEIGHTS[i];
            if (t < sum) return i;
        }
        return TIER_WEIGHTS.Length - 1;
    }

    // jha + jhd: vũ khí quái campaign. null = cận chiến tay không.
    public static WeaponData PickWeapon(int combatLevel, bool isRanged)
    {
        var rnd = new NetRandom(combatLevel * 997 + (isRanged ? 7 : 3) + ENEMY_GEAR_SEED * 31);
        int tier = TierForLevel(combatLevel);

        int rarity = Mathf.Min(tier, RARITY_COUNT) - 1;
        if (rarity < 0)
        {
            if (isRanged == false) return null;
            rarity = 0;
        }

        WeaponType type = isRanged ? WeaponType.Range : WeaponType.Melee;
        for (int r = Mathf.Min(rarity, RARITY_COUNT - 1); r >= 0; r--)
        {
            List<WeaponData> pool = Pool((Rarity)r, type);
            if (pool.Count > 0)
            {
                return pool[rnd.Next(0, pool.Count)];
            }
        }
        return null;
    }

    // Vũ khí người chơi theo rarity + loại, giữ thứ tự WEAPON_ORDER (GameResources.jgc + lọc WeaponType).
    private static List<WeaponData> Pool(Rarity rarity, WeaponType type)
    {
        var pool = new List<WeaponData>();
        for (int i = 0; i < WEAPON_ORDER.Length; i++)
        {
            WeaponData w = GameData.staticData.weapons.GetData(WEAPON_ORDER[i]);
            if (w != null && w.rarity == rarity && w.weaponType == type)
            {
                pool.Add(w);
            }
        }
        return pool;
    }
}
