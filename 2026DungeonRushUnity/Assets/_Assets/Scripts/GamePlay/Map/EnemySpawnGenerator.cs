using System;
using System.Collections.Generic;
using UnityEngine;

// Sinh enemy cho 1 màn CAMPAIGN — CÔNG THỨC GỐC (reverse từ libil2cpp.so bản 41).
//
// Campaign là 1 mạch liên tục (1-1..1-10, 2-1..) và CHỈ có lính thường (melee/ranged),
// KHÔNG có dragon/cultist (đó là dungeon riêng). Nhánh campaign trong LevelController.hpv:
//   combatLevel = level THÔ (không có base+×3 của dungeon)
//   preset      = jgx(level): level≤10 → ManualPresets[level-1]; level>10 → ArmyPresets xáo trộn (jhe)
// Sau đó EconomyController.hcm chia sức mạnh theo Lancaster:
//   totalArmyPow = 500 * 10^hck(level)                              [hcj]
//   perUnitPow   = totalArmyPow / unitCount^Lancaster              [hcm]
//   mỗi unit (ratio r: Melee 3 / Ranged 2):
//       damage = round(sqrt(perUnitPow / r)) * (0.8 nếu Ranged)    (min 1)
//       health = round(sqrt(perUnitPow * r))                        (min 1)
// Hằng số & preset: DecodedData/gameresources_values.json. Chi tiết: DecodedData/ENEMY_STATS_MODEL.md
public static class EnemySpawnGenerator
{
    // ===== Hằng số THẬT (GameResources) =====
    private const float ARMY_POWER_BASE = 500f;
    private const float EXP_SCALER = 3.16227770f;             // √10 ⇒ scaler^(2x) = 10^x
    private const float RANGED_DMG_MULT = 0.8f;              // RangedUnitDamageMultiplier
    private const float RATIO_MELEE = 3f;                     // MeleeHealthToDamageRatio
    private const float RATIO_RANGED = 2f;                    // RangedHealthToDamageRatio

    private const float ENEMY_MOVE_SPEED = 1.5f;             // EnemyBaseMoveSpeed
    private const float MELEE_ATTACK_RANGE = 1.5f;           // EnemyBaseMeleeAttackDistance
    private const float RANGED_ATTACK_RANGE = 3f;            // EnemyBaseRangeAttackDistance

    private const int PRESET_SEED = 42;                      // GameResources.PresetSeed

    // army_power_segments: (Threshold, LevelScaler) — dùng cho hck.
    private static readonly (int threshold, float scaler)[] SEGMENTS =
    {
        (80, 20f), (200, 30f), (999999, 40f),
    };

    private struct Preset { public int melee, ranged; public float lancaster;
        public Preset(int m, int r, float l) { melee = m; ranged = r; lancaster = l; } }

    // ManualPresets: dùng cho campaign level 1..10 (đúng thứ tự gốc).
    private static readonly Preset[] MANUAL_PRESETS =
    {
        new Preset(1, 0, 1.0f), new Preset(2, 0, 1.0f), new Preset(0, 1, 1.0f),
        new Preset(0, 2, 1.3f), new Preset(1, 1, 1.0f), new Preset(1, 2, 1.5f),
        new Preset(2, 2, 1.5f), new Preset(3, 3, 1.5f), new Preset(0, 5, 2.0f),
        new Preset(5, 0, 1.0f),
    };

    // ArmyPresets: pool cho campaign level > 10 (chọn bằng shuffle seeded, xem jhe).
    private static readonly Preset[] ARMY_PRESETS =
    {
        new Preset(1, 0, 1.0f), new Preset(0, 1, 1.2f), new Preset(1, 1, 1.0f),
        new Preset(3, 0, 1.2f), new Preset(0, 3, 1.5f), new Preset(6, 0, 1.2f),
        new Preset(0, 6, 1.8f), new Preset(2, 2, 1.5f), new Preset(3, 3, 1.5f),
    };

    // Thông tin 1 enemy được sinh ra: ô đứng, level, stat đã tính sẵn, role.
    public struct EnemySpawnInfo
    {
        public Vector2Int cell;
        public int level;              // = campaign level (combatLevel thô)
        public float health;
        public float attackPower;
        public float attackSpeed;
        public float moveSpeed;
        public float attackRange;
        public bool isRanged;
        public bool isBoss;            // campaign không dùng (để tương thích code cũ)
    }

    // stageId theo convention campaign (101, 102... 201...). bossCell chừa sẵn (campaign không boss).
    public static List<EnemySpawnInfo> Generate(int stageId, List<Vector2Int> enemySpawnCells, Vector2Int bossCell)
    {
        var result = new List<EnemySpawnInfo>();
        if (enemySpawnCells == null || enemySpawnCells.Count == 0) return result;

        var campaign = GameData.staticData.campaign;
        int chapter = campaign.GetChapter(stageId);
        int stageIndex = campaign.GetStageIndex(stageId);
        // Level campaign LIÊN TỤC: 1-1=1 ... 1-10=10, 2-1=11 ... (globalStage).
        int level = Mathf.Max(1, (chapter - 1) * StaticCampaignData.STAGES_PER_CHAPTER + stageIndex);

        // Nhánh campaign: combatLevel = level thô (không có base+×3 như dungeon).
        float totalArmyPower = TotalArmyPower(level);
        Preset preset = PresetForLevel(level);

        int unitCount = preset.melee + preset.ranged;
        if (unitCount < 1) return result;
        float perUnitPower = totalArmyPower / Mathf.Pow(unitCount, preset.lancaster);

        float meleeDmg = Mathf.Max(1f, Round(Mathf.Sqrt(perUnitPower / RATIO_MELEE)));
        float meleeHp = Mathf.Max(1f, Round(Mathf.Sqrt(perUnitPower * RATIO_MELEE)));
        float rangedDmg = Mathf.Max(1f, Round(Mathf.Sqrt(perUnitPower / RATIO_RANGED) * RANGED_DMG_MULT));
        float rangedHp = Mathf.Max(1f, Round(Mathf.Sqrt(perUnitPower * RATIO_RANGED)));

        int total = Mathf.Min(unitCount, enemySpawnCells.Count);   // không đủ ô thì cắt bớt
        for (int i = 0; i < total; i++)
        {
            bool ranged = i >= preset.melee;                       // melee trước, ranged sau
            Vector2Int cell = enemySpawnCells[i * enemySpawnCells.Count / total];
            result.Add(new EnemySpawnInfo
            {
                cell = cell, level = level,
                attackPower = ranged ? rangedDmg : meleeDmg,
                health = ranged ? rangedHp : meleeHp,
                attackSpeed = 1f, moveSpeed = ENEMY_MOVE_SPEED,
                attackRange = ranged ? RANGED_ATTACK_RANGE : MELEE_ATTACK_RANGE,
                isRanged = ranged, isBoss = false,
            });
        }
        return result;
    }

    // GameResources.jgx: level≤10 → ManualPresets[level-1]; level>10 → ArmyPresets shuffle (jhe).
    private static Preset PresetForLevel(int level)
    {
        if (level <= MANUAL_PRESETS.Length)
        {
            return MANUAL_PRESETS[level - 1];
        }
        return ProceduralPreset(level - MANUAL_PRESETS.Length);   // jhe(n)
    }

    // GameResources.jhe(n): xáo ArmyPresets bằng Random(PresetSeed + (n-1)/count) rồi lấy [(n-1)%count].
    private static Preset ProceduralPreset(int n)
    {
        int count = ARMY_PRESETS.Length;
        int round = (n - 1) / count;
        var rng = new NetRandom(PRESET_SEED + round);

        int[] idx = new int[count];
        for (int i = 0; i < count; i++) idx[i] = i;
        for (int i = count - 1; i > 0; i--)   // Fisher-Yates: j = rng.Next(0, i+1)
        {
            int j = rng.Next(0, i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }
        return ARMY_PRESETS[idx[(n - 1) % count]];
    }

    // EconomyController.hcj: totalArmyPower = 500 * EXP_SCALER^(2*hck) = 500 * 10^hck.
    private static float TotalArmyPower(int combatLevel)
    {
        return ARMY_POWER_BASE * Mathf.Pow(EXP_SCALER, 2f * Hck(combatLevel));
    }

    // EconomyController.hck: L≤80 → L/20 ; 80<L≤200 → 4+(L-80)/30 ; L>200 → 8+(L-200)/40.
    private static float Hck(int level)
    {
        float acc = 0f;
        int prev = 0;
        foreach (var (threshold, scaler) in SEGMENTS)
        {
            if (threshold >= level) return acc + (level - prev) / scaler;
            acc += (threshold - prev) / scaler;
            prev = threshold;
        }
        return acc;
    }

    // Math.Round mặc định C# = banker's rounding — khớp Math.Round trong hcm.
    private static float Round(float v)
    {
        return (float)Math.Round((double)v, MidpointRounding.ToEven);
    }

    // System.Random bản Mono/.NET Framework (subtractive PRNG) — để shuffle khớp game gốc.
    private sealed class NetRandom
    {
        private const int MBIG = int.MaxValue;
        private const int MSEED = 161803398;
        private readonly int[] seedArray = new int[56];
        private int inext, inextp;

        public NetRandom(int seed)
        {
            int subtraction = (seed == int.MinValue) ? int.MaxValue : Math.Abs(seed);
            int mj = MSEED - subtraction;
            seedArray[55] = mj;
            int mk = 1;
            for (int i = 1; i < 55; i++)
            {
                int ii = (21 * i) % 55;
                seedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = seedArray[ii];
            }
            for (int k = 1; k < 5; k++)
            {
                for (int i = 1; i < 56; i++)
                {
                    seedArray[i] -= seedArray[1 + (i + 30) % 55];
                    if (seedArray[i] < 0) seedArray[i] += MBIG;
                }
            }
            inext = 0;
            inextp = 21;
        }

        private int InternalSample()
        {
            int locINext = inext, locINextp = inextp;
            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;
            int retVal = seedArray[locINext] - seedArray[locINextp];
            if (retVal == MBIG) retVal--;
            if (retVal < 0) retVal += MBIG;
            seedArray[locINext] = retVal;
            inext = locINext;
            inextp = locINextp;
            return retVal;
        }

        public int Next(int minValue, int maxValue)
        {
            long range = (long)maxValue - minValue;
            return (int)((InternalSample() * (1.0 / MBIG)) * range) + minValue;
        }
    }
}
