using UnityEngine;

// Pet/Companion (TeamA) — ĐI THEO hero và chỉ tấn công enemy trong tầm (spec).
// Khác Hero: không truy đuổi khắp map. Vòng ưu tiên mỗi tick:
//   1. Có enemy trong 'engageRange' (đo từ hero) → bám & đánh.
//   2. Không có → đi theo hero, giữ khoảng 'followDistance'.
public class PetUnit : BaseUnit
{
    public BaseUnit owner;               // hero để đi theo (gán khi spawn)
    public float engageRange = 3f;       // chỉ giao chiến enemy trong tầm này quanh hero
    public float followDistance = 1.5f;  // khoảng cách đứng cạnh hero
    public float followSlack = 0.4f;     // vùng đệm để khỏi rung khi đã đủ gần

    [Tooltip("Vũ khí của pet (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;

    // Pet dùng vũ khí gán sẵn trên prefab cho basic attack (melee/ranged).
    protected override WeaponData GetCombatWeapon()
    {
        return weaponData;
    }

    // Chọn enemy gần nhất trong engageRange tính từ vị trí HERO (giữ pet quanh hero).
    protected override void FindNearestTarget()
    {
        Vector3 center = owner != null ? owner.Transform.position : Transform.position;
        target = FindNearestEnemyFrom(center, GetAliveEnemies(), engageRange);
    }

    protected override void UpdateIdle()
    {
        FindNextTarget();

        if (target != null)
        {
            if (IsTargetInAttackRange())
            {
                ChangeState(BattleState.Attack);
            }
            else if (isMoveable)
            {
                ChangeState(BattleState.Move);
            }
            return;
        }

        // Không có enemy → đi theo hero nếu tụt lại quá xa.
        if (owner != null && owner.isTargetable && isMoveable)
        {
            float d = Vector3.Distance(Transform.position, owner.Transform.position);
            if (d > followDistance + followSlack)
            {
                moveDestination = GetFollowPoint();
                ChangeState(BattleState.Move);
            }
        }
    }

    protected override void UpdateMove()
    {
        if (target != null)
        {
            // Rời tầm giao chiến (đo từ hero) hoặc mục tiêu chết → thôi đuổi, về theo hero.
            Vector3 center = owner != null ? owner.Transform.position : Transform.position;
            bool inEngage = IsTargetAvailable()
                && VectorUtils.IsInRange(center, target.Transform.position, engageRange);

            if (!inEngage)
            {
                target = null;
                ChangeState(BattleState.Idle);
                return;
            }

            moveDestination = target.Transform.position;
            if (IsTargetInAttackRange())
            {
                ChangeState(BattleState.Attack);
            }
            return;
        }

        // Đang đi theo hero.
        if (owner == null || !owner.isTargetable)
        {
            ChangeState(BattleState.Idle);
            return;
        }

        moveDestination = GetFollowPoint();
        if (Vector3.Distance(Transform.position, owner.Transform.position) <= followDistance)
        {
            ChangeState(BattleState.Idle);
        }
    }

    // Điểm cách hero 'followDistance' về phía pet (đứng cạnh, không chồng lên hero).
    private Vector3 GetFollowPoint()
    {
        Vector3 heroPos = owner.Transform.position;
        Vector3 dir = Transform.position - heroPos;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector3.right;
        }
        dir.Normalize();
        return heroPos + dir * followDistance;
    }
}
