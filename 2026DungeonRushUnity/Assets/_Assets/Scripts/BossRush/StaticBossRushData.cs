using System.Collections.Generic;
using UnityEngine;

// 1 mẫu boss (BossRushBossDefinition gốc). Boss #N dùng mẫu (N-1) % 10.
public class BossRushBossDefinition
{
    public string bossName;                 // Common.BossRush.Boss.{index+1}
    public string prefabName;               // Resources/Prefabs/Units/Enemies/<prefabName>
    public float colliderRadiusMultiplier = 1.5f;
    public float massMultiplier = 5f;
    public bool canMove = false;
    public BossRushEnemyBehaviorType behaviorType = BossRushEnemyBehaviorType.RepositionAfterAttack;
    public float repositionDelay = 5f;      // giây
    public int repositionSearchRadius = 3;  // ô
}

public enum BossRushEnemyBehaviorType
{
    DirectChase = 1,
    RepositionAfterAttack = 2,
}

// Config tĩnh Boss Rush — REVERSE từ game gốc v41 (BossRushConfig.asset + .cctor trong libil2cpp).
// Server (server/functions/src/config.ts) giữ bản sao y hệt. Chi tiết: DecodedData/BOSS_RUSH_MODEL.md.
public class StaticBossRushData
{
    public const int POOL_SIZE = 8;                 // "Fight with an 8-player team"
    public const int MAX_GHOSTS = 7;                // BossRushController.ekz(7)
    public const int MAX_BOSS_COUNT = 20;
    public const int MAX_TIER = 10;
    public const float FIGHT_DURATION = 30f;        // FightDuration
    public const int DAMAGE_DIVISOR = 100;          // DamageDivisor — client gốc không dùng
    public const float PLAYER_COLLIDER_RADIUS_MULTIPLIER = 1f;
    public static readonly Vector2Int ARENA_GRID_SIZE = new Vector2Int(12, 12);
    public static readonly Vector3 CAMERA_FOLLOW_OFFSET = new Vector3(0f, -5f, 0f);

    // BossHPTable[tier-1][boss-1] (double, đọc thẳng từ global-metadata).
    private static readonly double[][] BossHPTable =
    {
    new double[] { 500000000d, 750000000d, 1000000000d, 2000000000d, 3000000000d, 4000000000d, 6000000000d, 9000000000d, 13000000000d, 19000000000d, 29000000000d, 43000000000d, 65000000000d, 97000000000d, 146000000000d, 219000000000d, 328000000000d, 493000000000d, 739000000000d, 1108000000000d },
    new double[] { 1000000000d, 1500000000d, 2250000000d, 3375000000d, 5000000000d, 8000000000d, 11000000000d, 17000000000d, 26000000000d, 38000000000d, 58000000000d, 86000000000d, 130000000000d, 195000000000d, 292000000000d, 438000000000d, 657000000000d, 985000000000d, 1478000000000d, 2217000000000d },
    new double[] { 2000000000d, 3000000000d, 4500000000d, 6750000000d, 10000000000d, 15000000000d, 23000000000d, 34000000000d, 51000000000d, 77000000000d, 115000000000d, 173000000000d, 259000000000d, 389000000000d, 584000000000d, 876000000000d, 1314000000000d, 1971000000000d, 2956000000000d, 4434000000000d },
    new double[] { 3000000000d, 4500000000d, 6750000000d, 10000000000d, 15000000000d, 23000000000d, 34000000000d, 51000000000d, 77000000000d, 115000000000d, 173000000000d, 259000000000d, 389000000000d, 584000000000d, 876000000000d, 1314000000000d, 1971000000000d, 2956000000000d, 4434000000000d, 6651000000000d },
    new double[] { 5000000000d, 8000000000d, 11000000000d, 17000000000d, 25000000000d, 38000000000d, 57000000000d, 85000000000d, 128000000000d, 192000000000d, 288000000000d, 432000000000d, 649000000000d, 973000000000d, 1460000000000d, 2189000000000d, 3284000000000d, 4926000000000d, 7389000000000d, 11084000000000d },
    new double[] { 10000000000d, 15000000000d, 23000000000d, 34000000000d, 51000000000d, 76000000000d, 114000000000d, 171000000000d, 256000000000d, 384000000000d, 577000000000d, 865000000000d, 1297000000000d, 1946000000000d, 2919000000000d, 4379000000000d, 6568000000000d, 9853000000000d, 14779000000000d, 22168000000000d },
    new double[] { 20000000000d, 30000000000d, 45000000000d, 68000000000d, 101000000000d, 152000000000d, 228000000000d, 342000000000d, 513000000000d, 769000000000d, 1153000000000d, 1730000000000d, 2595000000000d, 3892000000000d, 5839000000000d, 8758000000000d, 13137000000000d, 19705000000000d, 29558000000000d, 44337000000000d },
    new double[] { 30000000000d, 45000000000d, 68000000000d, 101000000000d, 152000000000d, 228000000000d, 342000000000d, 513000000000d, 769000000000d, 1153000000000d, 1730000000000d, 2595000000000d, 3892000000000d, 5839000000000d, 8758000000000d, 13137000000000d, 19705000000000d, 29558000000000d, 44337000000000d, 66505000000000d },
    new double[] { 50000000000d, 75000000000d, 113000000000d, 169000000000d, 253000000000d, 380000000000d, 570000000000d, 854000000000d, 1281000000000d, 1922000000000d, 2883000000000d, 4325000000000d, 6487000000000d, 9731000000000d, 14596000000000d, 21895000000000d, 32842000000000d, 49263000000000d, 73895000000000d, 110842000000000d },
    new double[] { 100000000000d, 150000000000d, 225000000000d, 338000000000d, 506000000000d, 759000000000d, 1139000000000d, 1709000000000d, 2563000000000d, 3844000000000d, 5767000000000d, 8650000000000d, 12975000000000d, 19462000000000d, 29193000000000d, 43789000000000d, 65684000000000d, 98526000000000d, 147789000000000d, 221684000000000d },
    };

    // BossDamageByTier[tier-1].
    private static readonly float[] BossDamageByTier = { 300f, 600f, 900f, 3000f, 6000f, 9000f, 12000f, 15000f, 18000f, 21000f };

    // Tên tier = tên league PvP (Common.PvP.League.{tier}).
    private static readonly string[] TierNames =
    {
        "Iron League", "Bronze League", "Silver League", "Gold League", "Platinum League",
        "Emerald League", "Diamond League", "Master", "Grandmaster", "Legend",
    };

    public readonly List<BossRushBossDefinition> bosses = new List<BossRushBossDefinition>
    {
        new BossRushBossDefinition { bossName = "Dark Lich", prefabName = "BossRushDarkLich" },
        new BossRushBossDefinition { bossName = "Black Dragon", prefabName = "BossRushBlackDragon" },
        new BossRushBossDefinition { bossName = "Green Dragon", prefabName = "BossRushGreenDragon" },
        new BossRushBossDefinition { bossName = "Red Dragon", prefabName = "BossRushRedDragon" },
        new BossRushBossDefinition { bossName = "Crimson Lich", prefabName = "BossRushCrimsonLich" },
        new BossRushBossDefinition { bossName = "Elder Lich", prefabName = "BossRushElderLich" },
        new BossRushBossDefinition { bossName = "Ogre King", prefabName = "BossRushOgreKing" },
        new BossRushBossDefinition { bossName = "Ogre Chieftain", prefabName = "BossRushOgreChieftain" },
        new BossRushBossDefinition { bossName = "Green Hag", prefabName = "BossRushGreenHag" },
        new BossRushBossDefinition { bossName = "Purple Hag", prefabName = "BossRushPurpleHag" },
    };

    // elw(bossNumber, tier) — kẹp index như gốc.
    public double GetBossHp(int bossNumber, int tier)
    {
        int t = Mathf.Clamp(tier - 1, 0, MAX_TIER - 1);
        int b = Mathf.Clamp(bossNumber - 1, 0, MAX_BOSS_COUNT - 1);
        return BossHPTable[t][b];
    }

    // elx(_, tier).
    public float GetBossDamage(int tier)
    {
        return BossDamageByTier[Mathf.Clamp(tier - 1, 0, BossDamageByTier.Length - 1)];
    }

    // elu(bossNumber).
    public BossRushBossDefinition GetBoss(int bossNumber)
    {
        int index = ((Mathf.Max(1, bossNumber) - 1) % bosses.Count);
        return bosses[index];
    }

    public string GetTierName(int tier)
    {
        return TierNames[Mathf.Clamp(tier - 1, 0, TierNames.Length - 1)];
    }
}
