using UnityEngine;

// Pet/Companion (TeamA) — ĐI THEO hero và chỉ tấn công enemy trong tầm (spec).
// Khác Hero: không truy đuổi khắp map. Vòng ưu tiên mỗi tick:
//   1. Có enemy trong 'engageRange' (đo từ hero) → bám & đánh.
//   2. Không có → đi theo hero, giữ khoảng 'followDistance'.
//
// LỚP NỀN companion (bám StickIdle BaseCompanion): giữ CompanionData, quy đổi data → hành vi
// (nhịp = cooldown, tầm = followDistance/minDistance) và tính SÁT THƯƠNG companion từ data +
// hệ số CompanionDamage của chủ. Mỗi loại companion (DPS/Heal/Lightning/…) là 1 lớp con
// override ReleaseAbility() để bắn đạn/tia/heal riêng — xem PetCompanionDps.
public class PetUnit : BaseUnit
{
    public BaseUnit owner;               // hero để đi theo (gán khi spawn)
    public float engageRange = 3f;       // chỉ giao chiến enemy trong tầm này quanh hero
    public float followDistance = 1.5f;  // khoảng cách đứng cạnh hero
    public float followSlack = 0.4f;     // vùng đệm để khỏi rung khi đã đủ gần

    [Tooltip("Vũ khí của pet (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;

    [Tooltip("Data companion: chỉ số cân bằng + loại kỹ năng. Gán sẵn trên prefab hoặc set khi spawn.")]
    [SerializeField] protected CompanionData companionData;

    public CompanionData Data => companionData;

    // Companion là quân đồng hành: KHÔNG bị enemy nhắm và KHÔNG chết (giống StickIdle BaseCompanion).
    public override bool isDead => false;
    public override bool isTargetable => false;
    public override bool isImmuneCC => true;

    // ===== Spawn từ CompanionData =====

    // Vào trận: gán chủ + data, quy đổi data → hành vi/chỉ số rồi kích hoạt tại vị trí.
    public virtual void SetupCompanion(CompanionData data, BaseUnit owner, int level, Vector3 position)
    {
        companionData = data;
        this.owner = owner;
        ApplyCompanionData(data, level);
        SpawnInBattle(BuildCompanionStats(data), StaticValue.TAG_TEAM_A, position);
    }

    // Quy đổi các field "di chuyển/nhịp" của companion sang tham số hành vi của PetUnit.
    protected void ApplyCompanionData(CompanionData data, int level)
    {
        if (data == null)
        {
            return;
        }

        this.level = Mathf.Max(1, level);
        engageRange = Mathf.Max(data.followDistance, data.minDistance);
        followDistance = Mathf.Max(0.5f, data.minDistance);
    }

    // BaseStats cho companion: nhịp = cooldown, tầm = engageRange, tốc độ = moveSpeed.
    // maxHp chỉ cần > 0 (pet không chết); SÁT THƯƠNG tính riêng ở GetAbilityDamage() lúc ra đòn.
    protected virtual BaseStats BuildCompanionStats(CompanionData data)
    {
        float cooldown = (data != null && data.cooldown > 0.01f) ? data.cooldown : 1f;
        return new BaseStats
        {
            attack = data != null ? data.damageBase : 0f,
            attackPerSecond = 1f / cooldown,               // cadence ra đòn = cooldown companion
            attackRange = data != null ? Mathf.Max(data.followDistance, data.minDistance) : engageRange,
            moveSpeed = data != null ? data.moveSpeed : 2f,
            maxHp = 1f,
        };
    }

    // Sát thương 1 nhịp kỹ năng = (base + scaler*(level-1)) * hệ số CompanionDamage của chủ.
    // LƯU Ý: đường cong theo level (cộng dồn scaler) suy từ pattern các model đã reverse của project;
    // ở level 1 = damageBase, khớp <Value> hiển thị trên mô tả. Cần đối chiếu native nếu cân bằng
    // theo level về sau. damageBase/damageScaler là sát thương TUYỆT ĐỐI (mô tả "deal X Damage").
    protected double GetAbilityDamage()
    {
        if (companionData == null)
        {
            return stats.attack;
        }

        double dmg = companionData.damageBase + companionData.damageScaler * (level - 1);
        if (owner != null)
        {
            dmg *= owner.stats.companionDamage;
        }
        return dmg;
    }

    // Lượng hồi máu 1 nhịp (dùng cho companion healer) — cùng dạng công thức với sát thương.
    protected double GetAbilityHeal()
    {
        if (companionData == null)
        {
            return 0f;
        }

        double heal = companionData.healBase + companionData.healScaler * (level - 1);
        if (owner != null)
        {
            heal *= owner.stats.companionDamage;
        }
        return heal;
    }

    // AttackData mang sát thương companion (thay cho GetBasicAttackData dựa trên stats.attack).
    protected AttackData GetAbilityAttackData()
    {
        return new AttackData(this, AttackType.BasicAttack, CritType.Critable, GetAbilityDamage());
    }

    // Pet dùng vũ khí gán sẵn trên prefab cho basic attack (melee/ranged). Trống → companion tự
    // quyết cách ra đòn qua ReleaseAbility().
    protected override WeaponData GetCombatWeapon()
    {
        return weaponData;
    }

    // ===== Ra đòn =====

    // Điểm ra đòn mỗi nhịp (OnAttackEnd của BaseUnit). Chặn khi mất mục tiêu / ngoài tầm rồi
    // uỷ quyền cho ReleaseAbility() — lớp con định nghĩa hiệu ứng thật.
    protected override void ReleaseAttack()
    {
        if (!IsTargetAvailable() || !IsTargetInAttackRange())
        {
            return;
        }

        ReleaseAbility();
    }

    // Kỹ năng mặc định: cận chiến đơn mục tiêu bằng sát thương companion. DPS/Heal/Lightning override.
    protected virtual void ReleaseAbility()
    {
        if (target == null)
        {
            return;
        }

        target.TakeAttack(GetAbilityAttackData(), impactAttack);
        PlaySfxAttack();
    }

    // ===== Targeting / di chuyển theo hero (giữ nguyên spec) =====

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

    // Companion quay mặt theo mục tiêu; không có mục tiêu thì đồng bộ hướng với hero.
    protected override void FaceToTarget()
    {
        if (target != null)
        {
            Face(target.Transform.position.x > Transform.position.x);
        }
        else if (owner != null)
        {
            Face(owner.Transform.position.x >= Transform.position.x);
        }
    }
}
