using System.Collections.Generic;
using UnityEngine;

// 1 mẫu boss (BossRushBossDefinition gốc v59). Boss #N dùng mẫu (N-1) % 10.
public class BossRushBossDefinition
{
    public string bossName;                 // Common.BossRush.Boss.{index+1}
    public string prefabName;               // Resources/Prefabs/Units/Enemies/<prefabName>
    public float colliderRadiusMultiplier = 1.5f;
    public float bodyScaleMultiplier = 1f;   // hình boss × (Character.fyh)
    public float massMultiplier = 5f;
    public bool canMove = true;
    public BossRushEnemyBehaviorType behaviorType = BossRushEnemyBehaviorType.RepositionAfterAttack;
    public float repositionDelay = 1.6f;    // giây
    public float repositionSearchRadius = 3.5f;  // ô
    public float moveSpeedMultiplier = 1f;
    public BossRushBossSkillType skillType = BossRushBossSkillType.None;
}

// FX skill riêng của 1 boss (FireBreathFXPrefab / SlamTelegraphFXPrefab / SlamImpactFXPrefab của BossRushBossDefinition gốc v59).
// Gán trên prefab BossRushMode vì định nghĩa boss ở project nằm trong code.
[System.Serializable]
public class BossRushBossFX
{
    public GameObject fireBreathFXPrefab;
    public GameObject slamTelegraphFXPrefab;
    public GameObject slamImpactFXPrefab;
}

public enum BossRushBossSkillType
{
    None = 0,
    Slam = 1,
    Earthquake = 2,
    Teleport = 3,
    FireBreath = 4,
}

public enum BossRushEnemyBehaviorType
{
    DirectChase = 1,
    RepositionAfterAttack = 2,
}

// Config tĩnh Boss Rush — REVERSE từ game gốc (BossRushConfig.asset + .cctor trong libil2cpp); bảng HP/damage v41 = v59,
// phần chỗ đứng quanh boss + skill boss theo v59. Server (server/functions/src/config.ts) giữ bản sao bảng HP/damage.
// Chi tiết: DecodedData/BOSS_RUSH_MODEL.md (mục 10 = v59).
public class StaticBossRushData
{
    public const int MAX_BOSS_COUNT = 20;
    public const int MAX_TIER = 10;
    public const float FIGHT_DURATION = 30f;        // FightDuration
    public const int DAMAGE_DIVISOR = 100;          // DamageDivisor — client gốc không dùng
    public const float PLAYER_COLLIDER_RADIUS_MULTIPLIER = 1.1f;
    public static readonly Vector2Int ARENA_GRID_SIZE = new Vector2Int(12, 12);
    public static readonly Vector3 CAMERA_FOLLOW_OFFSET = new Vector3(0f, -5f, 0f);

    // Chỗ đứng quanh boss cho đội cận chiến (class gq gốc).
    public const float SLOT_STANDOFF = 1.25f;
    public const float MELEE_RANGE_BONUS = 1.6f;
    public const float SLOT_ARRIVAL_TOLERANCE = 0.9f;
    public const float SLOT_GRACE_SECONDS = 3f;
    public const int PLAYER_SPAWN_RING_RADIUS = 2;

    // Skill boss.
    public const float SKILL_INTERVAL_MIN = 4f;
    public const float SKILL_INTERVAL_MAX = 6f;
    public const float SLAM_WINDUP_SECONDS = 0.7f;
    public const float SLAM_RECOVERY_SECONDS = 0.4f;
    public const float SLAM_RADIUS = 4.2f;
    public const float SLAM_KNOCKBACK_FORCE = 8f;
    public const float SLAM_TELEGRAPH_WARMUP_SECONDS = 1f;   // chạy trước particle telegraph cho hiện đầy ngay
    public const float EARTHQUAKE_WINDUP_SECONDS = 0.5f;
    public const float EARTHQUAKE_WAVE_INTERVAL = 0.55f;
    public static readonly float[] EARTHQUAKE_WAVE_RADII = { 2.6f, 4.4f, 6f };
    public const float EARTHQUAKE_KNOCKBACK_FORCE = 7.5f;
    public const float EARTHQUAKE_HOP_HEIGHT = 1.25f;
    public const float TELEPORT_WINDUP_SECONDS = 0.35f;
    public const float TELEPORT_RECOVERY_SECONDS = 0.25f;
    public const float TELEPORT_MIN_DISTANCE_CELLS = 7f;
    public const float TELEPORT_MAX_DISTANCE_CELLS = 10f;
    public const float TELEPORT_MIN_PLAYER_DISTANCE_CELLS = 5f;
    public const float FIRE_BREATH_WINDUP_SECONDS = 0.4f;
    public const float FIRE_BREATH_SWEEP_SECONDS = 2.5f;
    public const float FIRE_BREATH_RECOVERY_SECONDS = 0.3f;
    public const float FIRE_BREATH_RADIUS = 3.5f;
    public const float FIRE_BREATH_CONE_ANGLE = 70f;
    public const float FIRE_BREATH_MUZZLE_OFFSET = 0.8f;
    public const float FIRE_BREATH_POINT_BLANK_MARGIN = 0.8f;
    public const float FIRE_BREATH_HIT_INTERVAL = 0.5f;
    public const float FIRE_BREATH_KNOCKBACK_FORCE = 4.5f;
    public const float FIRE_BREATH_FX_SCALE = 1f;   // prefab lửa × (Radius × FXScale)
    public const float ALLY_HIT_KNOCKBACK_FORCE = 3f;
    public const float BOSS_CRIT_PUNCH_SCALE = 0.08f;
    public const float BOSS_CRIT_PUNCH_COOLDOWN = 0.35f;

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

    // BossDefinitions v59 (collider × / hình × / dời chỗ sau / tốc chạy × / skill).
    public readonly List<BossRushBossDefinition> bosses = new List<BossRushBossDefinition>
    {
        new BossRushBossDefinition { bossName = "Dark Lich", prefabName = "BossRushDarkLich", colliderRadiusMultiplier = 1.6f, bodyScaleMultiplier = 1.2f, moveSpeedMultiplier = 1.5f, skillType = BossRushBossSkillType.Slam },
        new BossRushBossDefinition { bossName = "Black Dragon", prefabName = "BossRushBlackDragon", colliderRadiusMultiplier = 1.75f, bodyScaleMultiplier = 1.5f, repositionDelay = 1.75f, skillType = BossRushBossSkillType.FireBreath },
        new BossRushBossDefinition { bossName = "Green Dragon", prefabName = "BossRushGreenDragon", colliderRadiusMultiplier = 1.75f, bodyScaleMultiplier = 1.5f, skillType = BossRushBossSkillType.FireBreath },
        new BossRushBossDefinition { bossName = "Red Dragon", prefabName = "BossRushRedDragon", colliderRadiusMultiplier = 1.75f, bodyScaleMultiplier = 1.5f, skillType = BossRushBossSkillType.FireBreath },
        new BossRushBossDefinition { bossName = "Crimson Lich", prefabName = "BossRushCrimsonLich", colliderRadiusMultiplier = 1.6f, bodyScaleMultiplier = 1.2f, moveSpeedMultiplier = 1.5f, skillType = BossRushBossSkillType.Slam },
        new BossRushBossDefinition { bossName = "Elder Lich", prefabName = "BossRushElderLich", colliderRadiusMultiplier = 1.6f, bodyScaleMultiplier = 1.2f, moveSpeedMultiplier = 1.5f, skillType = BossRushBossSkillType.Slam },
        new BossRushBossDefinition { bossName = "Ogre King", prefabName = "BossRushOgreKing", colliderRadiusMultiplier = 1.75f, bodyScaleMultiplier = 1.5f, skillType = BossRushBossSkillType.Earthquake },
        new BossRushBossDefinition { bossName = "Ogre Chieftain", prefabName = "BossRushOgreChieftain", colliderRadiusMultiplier = 1.75f, bodyScaleMultiplier = 1.5f, skillType = BossRushBossSkillType.Earthquake },
        new BossRushBossDefinition { bossName = "Green Hag", prefabName = "BossRushGreenHag", colliderRadiusMultiplier = 1.5f, bodyScaleMultiplier = 1.1f, moveSpeedMultiplier = 1.5f, skillType = BossRushBossSkillType.Teleport },
        new BossRushBossDefinition { bossName = "Purple Hag", prefabName = "BossRushPurpleHag", colliderRadiusMultiplier = 1.5f, bodyScaleMultiplier = 1.1f, moveSpeedMultiplier = 1.5f, skillType = BossRushBossSkillType.Teleport },
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
