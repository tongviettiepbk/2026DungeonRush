using UnityEngine;

// Enemy (TeamB) — theo Character phe "Away" gốc (GridCombatController.hos):
//   chỉ nhắm hero/đồng minh trong TẦM PHÁT HIỆN = max(DetectionRange, tầm đánh), DetectionRange = 0 thì lấy
//   GameResources.EnemyDetectionRange = 4. Đã có mục tiêu còn sống thì BÁM TIẾP dù mục tiêu chạy ra xa.
//   Không có mục tiêu → đi tuần quanh ô xuất phát (SoldierPrefab gốc CanMove = CanPatrol = true).
//   Dungeon có theme EnemyCanMove = false (rồng) → moveSpeed 0 → không đi, không tuần.
public class EnemyUnit : BaseUnit
{
    private const float DEFAULT_DETECTION_RANGE = 4f;   // GameResources.EnemyDetectionRange

    [Tooltip("Tầm phát hiện riêng (Character.DetectionRange). 0 = dùng mặc định gốc 4.")]
    public float detectionRange;
    public bool isBoss;

    [Tooltip("Vũ khí quái thường (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;
    [Tooltip("Vũ khí khi là boss (VD boss dragon bắn đạn). Trống → dùng weaponData.")]
    [SerializeField] private WeaponData bossWeaponData;

    // Boss ưu tiên vũ khí boss (đạn Dragon...); quái thường dùng weaponData.
    protected override WeaponData GetCombatWeapon()
    {
        return (isBoss && bossWeaponData != null) ? bossWeaponData : weaponData;
    }

    // Spawn từ dữ liệu EnemySpawnGenerator (health/attack tuyệt đối + role + isBoss).
    public void SpawnEnemy(EnemySpawnGenerator.EnemySpawnInfo info, Vector3 position)
    {
        isBoss = info.isBoss;
        level = info.level;

        var bs = new BaseStats
        {
            attack = info.attackPower,
            attackPerSecond = info.attackSpeed,
            attackRange = info.attackRange,   // melee 1.5 / ranged 3.0 (EnemyBase*AttackDistance gốc)
            maxHp = info.health,
            moveSpeed = info.moveSpeed,
        };

        isMoveable = info.moveSpeed > 0f;
        canPatrol = isMoveable;

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
    }

    // GridCombatController.hos: đối thủ gần nhất trong tầm phát hiện; không có thì giữ mục tiêu cũ còn sống.
    protected override BaseUnit SelectTarget()
    {
        if (crowdController.isTaunted && IsTargetAvailable())
        {
            return target;
        }

        float range = Mathf.Max(detectionRange > 0f ? detectionRange : DEFAULT_DETECTION_RANGE, stats.attackRange);
        BaseUnit nearest = FindNearestEnemyAmong(GetAliveEnemies(), range);
        if (nearest == null && IsTargetAvailable())
        {
            return target;
        }
        return nearest;
    }

    protected override void FindNearestTarget()
    {
        target = SelectTarget();
    }
}
