using UnityEngine;

// Boss Boss Rush (Character.IsBossRushBoss gốc). Chỉ số lấy từ BossRushBossModel của trận:
//   HP hiện tại/tối đa = máu boss CHUNG của nhóm (server), sát thương = BossDamageByTier[tier] (EnemyAttackPower).
// Hành vi BossRushBossDefinition (gốc giống nhau cả 10 boss): CanMove = false (không đuổi), RepositionAfterAttack —
// MỖI đòn trúng hẹn dời chỗ sau RepositionDelay (5s, đòn sau đặt lại hẹn) (Character.ewo); đang Idle mà tới hẹn
// (ewp) → BossRushRepositionState: chọn ô trống ngẫu nhiên trong vuông bán kính RepositionSearchRadius (3, iju) rồi
// ĐI tới đó bằng lực theo đường A* (UnitMover), tới nơi về Idle. Khối lượng ×MassMultiplier (5), collider ×1.5.
// Khác nhau giữa các boss chỉ ở vũ khí: rồng/Lich/Hag bắn đạn tầm 100 tốc 12, Ogre đánh gần tầm 3.
// Mọi đòn trúng boss được báo về BossRushMode để cộng damage đội / damage của mình (GameController.hif gốc).
// Animation: rig dùng CharacterAnimatorController gốc (bool IsIdle / IsAttacking). Clip đánh bắn event
// OnAttackAnimationHit → ra đòn đúng khung hình vung tay, OnAttackAnimationCompleted → về tư thế đứng.
public class BossRushBossUnit : EnemyUnit
{
    private const float REPOSITION_MOVE_SPEED = 1.5f;   // GameResources.EnemyBaseMoveSpeed (MoveSpeed mặc định của quái)
    private static readonly int ANIM_IS_IDLE = Animator.StringToHash("IsIdle");
    private static readonly int ANIM_IS_ATTACKING = Animator.StringToHash("IsAttacking");

    public BossRushBossDefinition Definition { get; private set; }
    public BossRushBossModel Model { get; private set; }
    public string DisplayName => Definition != null ? Definition.bossName : "Boss";

    private BossRushMode mode;
    private bool pendingReposition;     // Character.BossRushPendingReposition
    private float repositionAtTime;     // Character.BossRushRepositionAtTime
    private bool isRepositioning;
    private Animator animator;
    private bool attackReleased;

    public override bool isMoveable
    {
        get { return Definition != null && Definition.canMove; }
        set { }
    }

    public void SpawnBoss(BossRushMode owner, BossRushBossModel model, BossRushBossDefinition definition, Vector3 position)
    {
        mode = owner;
        Model = model;
        Definition = definition;
        isBoss = true;
        detectionRange = 1000f; // đánh mọi mục tiêu trong tầm vũ khí, không cần tầm phát hiện

        var bs = new BaseStats
        {
            attack = model.AttackPower,
            maxHp = (float)model.MaxHP,
            attackPerSecond = 1f,
            moveSpeed = REPOSITION_MOVE_SPEED,   // chỉ dùng khi dời chỗ (isMoveable = CanMove = false)
        };

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
        if (bodyCollider != null)
        {
            bodyCollider.transform.localScale *= definition.colliderRadiusMultiplier;
        }
        rigid.mass *= definition.massMultiplier;   // SpawnController.hux

        // Máu thật = máu CHUNG còn lại của nhóm (có thể < max).
        hp = System.Math.Min(model.CurrentHP, GetMaxHp());
        UpdateHealthBar();
        pendingReposition = false;
        isRepositioning = false;
        SetupAnimator();
    }

    private void SetupAnimator()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            return;
        }

        UnitAnimationEventRelay relay = animator.GetComponent<UnitAnimationEventRelay>();
        if (relay == null)
        {
            relay = animator.gameObject.AddComponent<UnitAnimationEventRelay>();
        }
        relay.AttackAnimationHit = OnAttackAnimationHit;
        relay.AttackAnimationCompleted = OnAttackAnimationCompleted;
    }

    protected override void PlayAnimAttack()
    {
        base.PlayAnimAttack();
        attackReleased = false;
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_IDLE, false);
            animator.SetBool(ANIM_IS_ATTACKING, true);
        }
    }

    private void OnAttackAnimationHit()
    {
        if (stateCurrent == BattleState.Attack && !attackReleased)
        {
            attackReleased = true;
            ReleaseAttack();
        }
    }

    private void OnAttackAnimationCompleted()
    {
        animator.SetBool(ANIM_IS_ATTACKING, false);
        animator.SetBool(ANIM_IS_IDLE, true);
    }

    public override double GetMaxHp()
    {
        return Model != null ? Model.MaxHP : base.GetMaxHp();
    }

    public override double TakeDamage(TakeDamageData data, bool showTextDamage = true, bool flashColor = true)
    {
        double taken = base.TakeDamage(data, showTextDamage, flashColor);
        if (Model != null)
        {
            Model.CurrentHP = System.Math.Max(0d, hp);
        }
        if (mode != null && taken > 0d && data != null)
        {
            mode.OnBossDamaged(data.attacker, taken);
        }
        return taken;
    }

    // ===== RepositionAfterAttack (Character.ewo/ewp + BossRushRepositionState gốc) =====

    protected override void OnAttackEnd()
    {
        // Đòn đã ra ở event OnAttackAnimationHit; chỉ ra bù khi rig không có Animator / clip không bắn event.
        if (!attackReleased)
        {
            base.OnAttackEnd();
        }
    }

    // Character.ewo: mỗi đòn trúng → hẹn dời chỗ sau RepositionDelay.
    protected override void ReleaseAttack()
    {
        base.ReleaseAttack();
        if (Definition != null && Definition.behaviorType == BossRushEnemyBehaviorType.RepositionAfterAttack)
        {
            pendingReposition = true;
            repositionAtTime = Time.time + Definition.repositionDelay;
        }
    }

    // IdleState.fbq gốc xét ewp ĐẦU TIÊN: tới hẹn → vào BossRushRepositionState; đang dời chỗ thì đi cho tới nơi.
    protected override void UpdateIdle()
    {
        if (isRepositioning)
        {
            if (mover.HasPath)
            {
                return;
            }
            isRepositioning = false;
        }

        if (pendingReposition && Time.time >= repositionAtTime && StartReposition())
        {
            return;
        }

        base.UpdateIdle();
    }

    // BossRushRepositionState.ewu: bỏ hẹn, chọn ô quanh (bán kính ≥ 1), đi tới bằng PhysicsMovementController.
    private bool StartReposition()
    {
        pendingReposition = false;
        MapController map = MapController.Instance;
        if (map == null || Definition == null)
        {
            return false;
        }

        Vector2Int current = map.WorldToCell(Transform.position);
        var occupied = new System.Collections.Generic.HashSet<Vector2Int>();
        var e = GameController.Instance.activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            BaseUnit unit = e.Current.Value;
            if (unit != null && unit != this && unit.isTargetable) occupied.Add(map.WorldToCell(unit.Transform.position));
        }

        Vector2Int destination = MapSpawnCells.GetRandomCellAround(map.Grid, current, Mathf.Max(1, Definition.repositionSearchRadius), occupied);
        if (destination == current || !mover.RequestMoveTo(destination))
        {
            return false;
        }

        isRepositioning = true;
        return true;
    }
}
