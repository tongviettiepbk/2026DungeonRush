using UnityEngine;

// Enemy (TeamB) — hành vi THỤ ĐỘNG đúng spec:
//   KHÔNG tự đi tìm hero. Chỉ khi mục tiêu lọt vào 'aggroRange' (vùng tấn công) thì:
//     - đã trong tầm đánh  → attack luôn,
//     - còn trong aggro nhưng ngoài tầm đánh → đi tới cho vào tầm rồi đánh.
//   Mục tiêu rời aggro → bỏ target, đứng yên (base UpdateIdle chuyển về Idle).
// Đạt được bằng cách chỉ chọn target trong aggroRange; ngoài ra tái dùng nguyên state machine.
public class EnemyUnit : BaseUnit
{
    [Tooltip("Bán kính vùng tấn công: mục tiêu vào đây enemy mới phản ứng.")]
    public float aggroRange = 3.5f;
    public bool isBoss;

    [Tooltip("Vũ khí quái thường (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;
    [Tooltip("Vũ khí khi là boss (VD boss dragon bắn đạn). Trống → dùng weaponData.")]
    [SerializeField] private WeaponData bossWeaponData;

    private const float DEFAULT_ATTACK_RANGE = 1.2f;

    // Boss ưu tiên vũ khí boss (đạn Dragon...); quái thường dùng weaponData.
    protected override WeaponData GetCombatWeapon()
    {
        return (isBoss && bossWeaponData != null) ? bossWeaponData : weaponData;
    }

    // Spawn từ dữ liệu EnemySpawnGenerator (health/attack tuyệt đối + isBoss).
    public void SpawnEnemy(EnemySpawnGenerator.EnemySpawnInfo info, Vector3 position)
    {
        isBoss = info.isBoss;
        level = info.level;

        var bs = new BaseStats
        {
            attack = info.attackPower,
            attackPerSecond = info.attackSpeed,
            attackRange = DEFAULT_ATTACK_RANGE,
            maxHp = info.health,
            moveSpeed = info.moveSpeed,
        };

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
    }

    // Thụ động: chỉ nhắm mục tiêu đã ở trong aggroRange; ngoài vùng → null (đứng yên).
    protected override void FindNearestTarget()
    {
        target = FindNearestEnemyAmong(GetAliveEnemies(), aggroRange);
    }

    // Leash: mục tiêu rời aggroRange thì coi như "không còn mục tiêu" ở MỌI state
    // (kể cả đang Move) → enemy dừng đuổi, về đứng yên. Nếu không, base UpdateMove sẽ
    // đuổi vô hạn vì không lọc lại tầm.
    public override bool IsTargetAvailable()
    {
        if (!base.IsTargetAvailable())
        {
            return false;
        }

        return VectorUtils.IsInRange(Transform.position, target.Transform.position, aggroRange);
    }
}
