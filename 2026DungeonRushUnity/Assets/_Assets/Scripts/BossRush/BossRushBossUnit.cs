using DG.Tweening;
using UnityEngine;

// Boss Boss Rush (Character.IsBossRushBoss gốc v59). Chỉ số lấy từ BossRushBossModel của trận:
//   HP hiện tại/tối đa = máu boss CHUNG của nhóm (server), sát thương = BossDamageByTier[tier] (EnemyAttackPower).
// Định nghĩa từng boss (BossRushBossDefinition, SpawnController.jmj): CanMove = true (Ogre cận chiến đuổi theo),
// tốc chạy = EnemyBaseMoveSpeed × MoveSpeedMultiplier, hình × BodyScaleMultiplier, collider × ColliderRadiusMultiplier,
// khối lượng × MassMultiplier (5).
// RepositionAfterAttack (Character.fzr/fzs + BossRushRepositionState): đòn trúng đầu tiên hẹn dời chỗ sau RepositionDelay
// (đã có hẹn thì KHÔNG đặt lại); Idle mà tới hẹn → đi tới ô trống ngẫu nhiên cách RepositionSearchRadius (GridManager.kcq).
// Skill (IdleState.gfu + BossRushSkillState): hẹn skill đầu sau Random(SkillIntervalMin, Max); tới giờ thì đứng yên
// (Kinematic) chạy 1 skill theo loại boss (BossRushSkills.cs), xong hẹn skill sau. Skill ưu tiên hơn dời chỗ.
// Bị chí mạng: hình nảy nhẹ (Character.fzx). Đòn của boss trúng ai trong đội nhà → người đó bị đẩy lùi (Character.fzw).
// Mọi đòn trúng boss được báo về BossRushMode để cộng damage đội / damage của mình (GameController.hif gốc).
// Animation: rig dùng CharacterAnimatorController gốc (bool IsIdle / IsAttacking). Clip đánh bắn event
// OnAttackAnimationHit → ra đòn đúng khung hình vung tay, OnAttackAnimationCompleted → về tư thế đứng.
public class BossRushBossUnit : EnemyUnit
{
    private const float ENEMY_BASE_MOVE_SPEED = 1.5f;   // GameResources.EnemyBaseMoveSpeed
    private const float COLLIDER_MULTIPLIER_EPSILON = 0.01f;
    private const float CRIT_PUNCH_DURATION = 0.18f;
    private static readonly int ANIM_IS_IDLE = Animator.StringToHash("IsIdle");
    private static readonly int ANIM_IS_ATTACKING = Animator.StringToHash("IsAttacking");

    public BossRushBossDefinition Definition { get; private set; }
    public BossRushBossModel Model { get; private set; }
    public string DisplayName => Definition != null ? Definition.bossName : "Boss";
    public BossRushMode Mode => mode;
    public Transform VisualTransform => visual;
    public Vector2 Position => rigid != null ? rigid.position : (Vector2)Transform.position;

    private BossRushMode mode;
    private bool pendingReposition;     // Character.BossRushPendingReposition
    private float repositionAtTime;     // Character.BossRushRepositionAtTime
    private bool isRepositioning;
    private float nextSkillAtTime;      // Character.BossRushNextSkillAtTime
    private BossRushSkill skill;        // BossRushSkillState.yud
    private RigidbodyType2D bodyTypeBeforeSkill;
    private Animator animator;
    private Transform visual;           // Character 0x2c — node hình "Soldier/Body"
    private Vector3 visualBaseScale;
    private float lastCritPunchTime = float.MinValue;

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
            moveSpeed = definition.moveSpeedMultiplier > 0f ? ENEMY_BASE_MOVE_SPEED * definition.moveSpeedMultiplier : ENEMY_BASE_MOVE_SPEED,
        };

        SpawnInBattle(bs, StaticValue.TAG_TEAM_B, position);
        ApplyColliderMultiplier(definition.colliderRadiusMultiplier);   // SpawnController.jll
        rigid.mass *= definition.massMultiplier;                          // SpawnController.jlm
        ApplyBodyScale(definition.bodyScaleMultiplier);                   // Character.fyh

        // Máu thật = máu CHUNG còn lại của nhóm (có thể < max).
        hp = System.Math.Min(model.CurrentHP, GetMaxHp());
        UpdateHealthBar();
        pendingReposition = false;
        isRepositioning = false;
        nextSkillAtTime = 0f;
        skill = null;
        SetupAnimator();
    }

    // jll: |mult − 1| > 0.01 thì phóng bán kính CircleCollider2D (không đụng hình).
    private void ApplyColliderMultiplier(float multiplier)
    {
        var circle = bodyCollider as CircleCollider2D;
        if (circle != null && Mathf.Abs(multiplier - 1f) > COLLIDER_MULTIPLIER_EPSILON)
        {
            circle.radius *= multiplier;
        }
    }

    // fyh/fyi: hình = scale gốc × BodyScaleMultiplier (> 0).
    private void ApplyBodyScale(float multiplier)
    {
        visual = Transform.Find("Soldier/Body");
        if (visual == null && bodyCollider != null)
        {
            visual = bodyCollider.transform;
        }
        if (visual == null)
        {
            return;
        }
        if (multiplier > 0f)
        {
            visual.localScale *= multiplier;
        }
        visualBaseScale = visual.localScale;
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
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_IDLE, false);
            animator.SetBool(ANIM_IS_ATTACKING, true);
        }
    }

    // Dùng chung cờ ra đòn với mốc 50% nhịp (MeleeAttackState.fcd / RangeAttackState.fcn gốc) → không ra 2 lần.
    private void OnAttackAnimationHit()
    {
        ReleaseHitFromAnimation();
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
        if (data != null && data.isCrit && !isDead)
        {
            PlayCritPunch();
        }
        return taken;
    }

    // fzx: hình nảy × BossCritPunchScale, cách nhau ít nhất BossCritPunchCooldown.
    private void PlayCritPunch()
    {
        if (visual == null || Time.time - lastCritPunchTime < StaticBossRushData.BOSS_CRIT_PUNCH_COOLDOWN)
        {
            return;
        }
        lastCritPunchTime = Time.time;
        visual.DOKill();
        visual.localScale = visualBaseScale;
        visual.DOPunchScale(visualBaseScale * StaticBossRushData.BOSS_CRIT_PUNCH_SCALE, CRIT_PUNCH_DURATION, 1, 0.5f);
    }

    // fzw: đòn của boss (thường, đạn hay skill) trúng người đội nhà → đẩy lùi AllyHitKnockbackForce.
    public override void OnAttackDone(ProcessedAttackData processedAttackData, double damageDealt)
    {
        base.OnAttackDone(processedAttackData, damageDealt);
        BaseUnit victim = processedAttackData != null ? processedAttackData.victim : null;
        if (victim != null && victim.isTeamA && victim.isTargetable)
        {
            victim.AddKnockbackImpulse(Position, StaticBossRushData.ALLY_HIT_KNOCKBACK_FORCE);
        }
    }

    // ===== Dùng cho skill (BossRushSkills.cs) =====

    // Character.fyw: 1 đòn thường của boss lên mục tiêu (cùng công thức chí mạng / chặn).
    public void SkillAttack(BaseUnit victim)
    {
        if (victim != null && victim.isTargetable)
        {
            victim.TakeAttack(GetBasicAttackData(), impactAttack);
        }
    }

    // hb.fnx: cỡ thân boss = bề ngang collider (tối thiểu 0.2).
    public float BodyDiameter => Mathf.Max(bodyCollider != null ? bodyCollider.bounds.extents.x * 2f : 1f, 0.2f);

    public void TeleportTo(Vector2 destination)
    {
        mover.Stop();
        Transform.position = new Vector3(destination.x, destination.y, Transform.position.z);
        rigid.position = destination;
    }

    public BaseUnit ClearTargetForSkill()
    {
        BaseUnit saved = target;
        target = null;
        return saved;
    }

    public void RestoreTargetAfterSkill(BaseUnit saved)
    {
        if (target == null)
        {
            target = saved;
        }
    }

    // ===== RepositionAfterAttack (Character.fzr/fzs + BossRushRepositionState gốc) =====

    // fzr: đòn trúng → hẹn dời chỗ sau RepositionDelay, chỉ khi chưa có hẹn.
    protected override void ReleaseAttack()
    {
        base.ReleaseAttack();
        if (Definition != null && Definition.behaviorType == BossRushEnemyBehaviorType.RepositionAfterAttack && !pendingReposition)
        {
            pendingReposition = true;
            repositionAtTime = Time.time + Definition.repositionDelay;
        }
    }

    // IdleState.gfu gốc: skill trước (hẹn lần đầu / tới giờ thì vào skill), rồi tới dời chỗ, rồi mới chọn mục tiêu.
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

        if (Definition != null && Definition.skillType != BossRushBossSkillType.None)
        {
            if (nextSkillAtTime <= 0f)
            {
                ScheduleNextSkill();
            }
            else if (Time.time >= nextSkillAtTime && BeginSkill())
            {
                return;
            }
        }

        if (pendingReposition && Time.time >= repositionAtTime && StartReposition())
        {
            return;
        }

        base.UpdateIdle();
    }

    // BossRushRepositionState.gao: bỏ hẹn, ô đích = GridManager.kcq(ô hiện tại, R = max(1, SearchRadius), r = max(1, R − 1)).
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

        float maxRadius = Mathf.Max(1f, Definition.repositionSearchRadius);
        float minRadius = Mathf.Max(1f, maxRadius - 1f);
        Vector2Int destination = MapSpawnCells.GetRepositionCell(map.Grid, current, maxRadius, minRadius, occupied);
        if (destination == current || !mover.RequestMoveTo(destination))
        {
            return false;
        }

        isRepositioning = true;
        return true;
    }

    // ===== Skill (BossRushSkillState gốc) =====

    // Character.fzt
    private void ScheduleNextSkill()
    {
        nextSkillAtTime = Time.time + Random.Range(StaticBossRushData.SKILL_INTERVAL_MIN, StaticBossRushData.SKILL_INTERVAL_MAX);
    }

    protected override void UpdateBattleState()
    {
        if (stateCurrent == BattleState.Skill)
        {
            UpdateSkill();
            return;
        }
        base.UpdateBattleState();
    }

    // gao: dừng, Kinematic (fzz(true)), Animator IsIdle, tạo + Begin skill. Không tạo được thì về Idle.
    private bool BeginSkill()
    {
        skill = BossRushSkill.Create(Definition.skillType, this);
        if (skill == null)
        {
            ScheduleNextSkill();
            return false;
        }

        StopMove();
        stateCurrent = BattleState.Skill;
        bodyTypeBeforeSkill = rigid.bodyType;
        rigid.linearVelocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        rigid.bodyType = RigidbodyType2D.Kinematic;
        if (animator != null)
        {
            animator.SetBool(ANIM_IS_ATTACKING, false);
            animator.SetBool(ANIM_IS_IDLE, true);
        }
        skill.Begin();
        return true;
    }

    // gfu: Tick(dt × tốc game) trả true → về Idle.
    private void UpdateSkill()
    {
        if (skill == null)
        {
            EndSkill();
            return;
        }
        if (skill.Tick(Time.deltaTime * GameController.Instance.gameSpeed))
        {
            skill = null;
            EndSkill();
        }
    }

    // gap: Cancel skill, trả bodyType, hẹn skill sau. Gốc đặt IsIdle = false rồi IdleState bật lại — project giữ IsIdle.
    private void EndSkill()
    {
        if (skill != null)
        {
            skill.Cancel();
            skill = null;
        }
        if (rigid != null)
        {
            rigid.bodyType = bodyTypeBeforeSkill;
        }
        ScheduleNextSkill();
        if (stateCurrent == BattleState.Skill)
        {
            stateCurrent = BattleState.Idle;
            ChangeState(BattleState.Idle, true);
        }
    }

    // Chết / bị ngắt giữa skill → dọn FX skill, trả Rigidbody.
    protected override void InterruptAction()
    {
        base.InterruptAction();
        if (stateCurrent == BattleState.Skill || skill != null)
        {
            EndSkill();
        }
    }

    private void OnDestroy()
    {
        if (skill != null)
        {
            skill.Cancel();
            skill = null;
        }
        if (visual != null)
        {
            visual.DOKill();
        }
    }
}
