using DG.Tweening;
using Spine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum BattleState
{
    None,
    Intro,
    Idle,
    Move,
    Attack,
    Skill,
    Die,
    KnockUp,
    Fear,
    Patrol,     // PatrolState gốc: quái không có mục tiêu đi loanh quanh ô xuất phát
}

public class BaseUnit : MonoBehaviour
{
    public float bodyOffset = 0.3f;
    public BaseFx impactAttack;
    public AudioClip sfxAttack;

    // Vũ khí đang cầm quyết định CÁCH ra đòn: có đạn (bắn xa) hay đánh thẳng (cận chiến).
    // Mặc định không đạn → enemy/pet giữ nguyên hành vi cũ. Hero nạp từ đồ đang mặc (OnBeginBattle).
    protected bool hasProjectile;
    protected BaseBullet bulletPrefab;

    protected Transform flipPoints;
    protected Collider2D bodyCollider;
    protected Rigidbody2D rigid;
    protected AudioSource audioSource;
    protected List<StatModifier> modifiers = new List<StatModifier>();

    // Lái theo đường A* bằng lực vật lý (PhysicsMovementController gốc) — tạo ở SetupRig.
    protected UnitMover mover;
    public UnitMover Mover => mover;

    // Bộ đếm MoveState gốc (uvu kẹt / uvy đổi mục tiêu / uwa chờ tìm đường) + mốc kiểm tra kẹt.
    private const float MOVE_STUCK_CHECK_INTERVAL = 1f;     // uvv
    private const float MOVE_STUCK_DISTANCE = 0.05f;        // uvw
    private const float MOVE_RETARGET_INTERVAL = 0.15f;     // uvz
    private const float MOVE_REPATH_INTERVAL = 0.2f;        // uwb
    private float moveStuckTimer;
    private float moveRetargetTimer;
    private float moveRepathTimer;
    private Vector2 moveLastPosition;

    // PatrolState gốc (Character.CanPatrol): quái không có mục tiêu đi loanh quanh ô xuất phát, tốc độ ×0.5.
    public bool canPatrol;
    private const float PATROL_SPEED_FACTOR = 0.5f;
    private const float PATROL_STUCK_DISTANCE = 0.05f;
    private const float PATROL_STUCK_TIME = 0.8f;           // uwo
    private const float PATROL_WAIT_MIN = 0.5f;
    private const float PATROL_WAIT_MAX = 1.5f;
    private Vector2Int patrolHomeCell;
    private bool patrolWaiting;
    private float patrolWaitTimer;
    private float patrolWaitDuration;
    private float patrolStuckTimer;
    private Vector2 patrolLastPosition;
    protected float moveSpeedFactor = 1f;

    // Tìm mục tiêu (quét toàn bộ unit + cấp list) tốn kém nếu chạy mỗi frame cho từng unit -> giật.
    // Giới hạn ~0.2s/lần, giữ target cũ giữa các nhịp; nextFindTargetTime lệch pha mỗi unit (Renew)
    // để các unit không cùng quét trong 1 frame (tránh spike định kỳ).
    private const float FIND_TARGET_INTERVAL = 0.2f;
    private float nextFindTargetTime;

    // Chỉ số gốc (server/scale set vào khi spawn) — CalculateCurrentStats đổ sang stats runtime.
    protected BaseStats baseStats = new BaseStats();

    public int slotIndex { get; set; } = -1;
    public Transform Transform { get; protected set; }
    public HealthBar hpBar { get; protected set; }
    public virtual AnimationController animationController { get; protected set; }
    public BattleState stateCurrent = BattleState.None;
    public int battleId { get; set; }
    public Transform centerBodyPoint { get; protected set; }
    public Transform firePoint { get; protected set; }
    public double hp { get; protected set; }
    public Stats stats { get; protected set; } = new Stats();
    public BaseUnit target { get; protected set; }
    public int level { get; protected set; } = 1;

    // Hiệu ứng tạm từ companion (gốc Character.BlockChance 0xE4 / IsImmortal 0xF9):
    //   bonusBlockChance: % chặn đòn cộng thêm (Guardian +5) ; isImmortal: HP không xuống dưới 1.
    public float bonusBlockChance { get; set; }
    public bool isImmortal { get; set; }
    // Hệ số làm chậm (companion Slow, gốc CompanionSlowEffect: NHÂN MoveSpeed & AttackSpeed); 1 = không chậm.
    // Áp ở cuối ReloadStats nên không mất khi chỉ số được tính lại.
    public float slowMultiplier { get; private set; } = 1f;

    public void SetSlowMultiplier(float multiplier)
    {
        slowMultiplier = multiplier;
        ReloadStats();
    }

    // Controllers
    public BuffController buffController { get; protected set; }
    public ShieldController shieldController { get; protected set; }
    public DebuffController debuffController { get; protected set; }
    public DamageOverTimeController dotController { get; protected set; }
    public CrowdControlController crowdController { get; protected set; }

    // Boolean
    public virtual bool isMoveable { get; set; } = true;
    public virtual bool isPause { get; set; }
    public virtual bool isDisableChangeState => isDead || crowdController.isKnocked;
    public virtual bool isMoving { get; set; }
    public virtual bool isAttacking { get; protected set; }
    public virtual bool isDead => hp <= 0f;
    public virtual bool isTargetable => isDead == false && bodyCollider.gameObject.activeInHierarchy && gameObject.activeInHierarchy;
    public virtual bool isImmuneCC => true;
    //public virtual bool isEnableFaceDirection => state != BattleState.Skill && crowdController?.isHardCC == false;
    public virtual bool isEnableFaceDirection => false;
    public virtual bool isEnableMove => crowdController?.isDisableMove == false;
    public virtual bool isEnableAttack => crowdController?.isDisableAttack == false;
    public virtual bool isEnableSkill => crowdController?.isDisableSkill == false;

    public bool isFacingRight
    {
        get { return animationController.isFacingRight; }
        set { animationController.isFacingRight = value; }
    }

    public bool isTeamA => CompareTag(StaticValue.TAG_TEAM_A);

    protected const float MINIMUM_UPDATE_DIRECTION_VALUE = 0.1f;
    private const float DEFAULT_IMPACT_LIFETIME = 1f;

    #region Unity Methods
    protected virtual void Awake()
    {
        Transform = GetComponent<Transform>();

        // BASIC-COMBAT (chưa port Spine/rig): prefab thật (rig "Soldier" rip) KHÔNG có rig contract
        // của BaseUnit (Rigidbody2D ở root, con "Body"/"FlipPoints"/"CenterBody"/"FirePoint"/"health-bar").
        // Ở đây tự resolve/tạo runtime cho đủ để action (move/target/attack) chạy. Khi dựng prefab
        // combat-ready thật thì các node có sẵn sẽ được tái dùng, không tạo thêm.
        // TODO(follow-stick): thay bằng rig thật + Spine anim + health-bar.
        SetupRig();
    }

    private void SetupRig()
    {
        // Rigidbody2D để di chuyển: đặt trên ROOT (cùng transform với logic vị trí). Prefab rip để
        // Rigidbody2D ở con "Soldier" -> nếu chỉ dùng nó thì art tách khỏi root khi di chuyển.
        rigid = GetComponent<Rigidbody2D>();
        if (rigid == null)
        {
            rigid = gameObject.AddComponent<Rigidbody2D>();
        }

        // Thông số Rigidbody2D của SoldierPrefab gốc: động (lái bằng lực, va chạm nhau + chướng ngại),
        // mass 1, cản 2, không trọng lực, khoá xoay, nội suy, va chạm liên tục.
        rigid.bodyType = RigidbodyType2D.Dynamic;
        rigid.mass = 1f;
        rigid.linearDamping = 2f;
        rigid.angularDamping = 0.05f;
        rigid.gravityScale = 0f;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Bỏ Rigidbody2D ở con (VD trên "Soldier") → collider con gắn vào Rigidbody root, art đi theo root.
        Rigidbody2D[] childBodies = GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < childBodies.Length; i++)
        {
            if (childBodies[i] != rigid)
            {
                childBodies[i].simulated = false;
                Destroy(childBodies[i]);
            }
        }

        mover = new UnitMover(rigid, MoverSettings);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // AnimationController hiện là stub no-op -> thêm runtime để mọi lời gọi animationController.*
        // an toàn mà không cần null-check rải khắp.
        animationController = GetComponent<AnimationController>();
        if (animationController == null)
        {
            animationController = gameObject.AddComponent<AnimationController>();
        }

        bodyCollider = ResolveBodyCollider();
        flipPoints = ResolveRigNode("FlipPoints", Transform, Vector3.zero);
        centerBodyPoint = ResolveRigNode("CenterBody", flipPoints, new Vector3(0f, 0.5f, 0f));
        firePoint = ResolveRigNode("FirePoint", flipPoints, new Vector3(0f, 0.5f, 0f));

        // Thanh máu: dùng node "health-bar" có sẵn trên rig nếu prefab đã dựng; nếu không (rig rip
        // hiện tại chưa có) thì instantiate prefab health-bar từ Resources rồi gắn dưới FlipPoints.
        Transform hpBarTransform = flipPoints.Find("health-bar");
        if (hpBarTransform == null)
        {
            HealthBar hpPrefab = Resources.Load<HealthBar>("Prefabs/health-bar");
            if (hpPrefab != null)
            {
                HealthBar hpInstance = Instantiate(hpPrefab, flipPoints, false);
                hpInstance.name = "health-bar";
                hpInstance.transform.localPosition = HP_BAR_LOCAL_OFFSET;
                hpInstance.transform.localScale = Vector3.one;
                hpBarTransform = hpInstance.transform;
            }
        }

        if (hpBarTransform)
        {
            hpBar = hpBarTransform.GetComponent<HealthBar>();
            hpBar.Init(this);
        }
    }

    // Vị trí thanh máu so với gốc unit (trên đầu). Chỉnh theo chiều cao art thật khi có rig chuẩn.
    private static readonly Vector3 HP_BAR_LOCAL_OFFSET = new Vector3(0f, 1.1f, 0f);

    // Lấy collider dùng làm hitbox/target: ưu tiên con "Body" (rig contract), rồi collider bất kỳ
    // trong children (rig rip để collider trên "Soldier"), cuối cùng tạo tạm nếu không có.
    private Collider2D ResolveBodyCollider()
    {
        Transform body = transform.Find("Body");
        if (body != null)
        {
            Collider2D col = body.GetComponent<Collider2D>();
            if (col != null) return col;
        }

        Collider2D anyCol = GetComponentInChildren<Collider2D>(true);
        if (anyCol != null) return anyCol;

        var placeholder = new GameObject("Body");
        placeholder.transform.SetParent(Transform, false);
        var circle = placeholder.AddComponent<CircleCollider2D>();
        circle.radius = 0.4f;
        circle.isTrigger = true;
        return circle;
    }

    // Node rig theo tên; thiếu thì tạo mới dưới parent tại localPos (rig rip chưa có các node này).
    private Transform ResolveRigNode(string nodeName, Transform parent, Vector3 localPos)
    {
        Transform node = parent.Find(nodeName);
        if (node == null)
        {
            node = new GameObject(nodeName).transform;
            node.SetParent(parent, false);
            node.localPosition = localPos;
        }
        return node;
    }

    protected virtual void FixedUpdate()
    {
        if (isPause)
        {
            return;
        }

        TickMover(Time.fixedDeltaTime * GameController.Instance.gameSpeed);
    }

    // Bộ tham số lái: hero/quái = PhysicsMovementController trong GameplayScene; pet override.
    protected virtual UnitMover.Settings MoverSettings => UnitMover.CharacterSettings;

    // PhysicsMovementController.htk gốc: chỉ đẩy lực khi trận đang chạy (GameState Playing).
    // moveSpeed = 0 (VD rồng) → không đẩy.
    protected virtual void TickMover(float dt)
    {
        BaseMode mode = GameController.Instance.mode;
        if (mover == null || !mover.HasPath || !isEnableMove || (mode != null && mode.isSkipUpdateBehaviour))
        {
            return;
        }

        float speed = stats.moveSpeed * moveSpeedFactor * GameController.Instance.gameSpeed;
        if (speed <= 0f)
        {
            return;
        }

        mover.Tick(dt, speed);

        // Gốc xoay thân theo hướng đi (htu/hto); project lật trái/phải theo vận tốc ngang.
        float vx = rigid.linearVelocity.x;
        if (Mathf.Abs(vx) > MINIMUM_UPDATE_DIRECTION_VALUE)
        {
            Face(vx > 0f);
        }
        animationController.SetTimeScaleAnimMoveBySpeed();
    }

    protected virtual void OnEnable()
    {
        EventDispatcher.Instance.RegisterListener(EventID.EndGame, OnEndGame);
    }

    protected virtual void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.EndGame, OnEndGame);
    }

    public virtual void UpdateBehavior()
    {
        if (isPause)
        {
            return;
        }

        if (isTargetable)
        {
            UpdateBattleState();
            animationController.UpdateSortingOrder();
            TickHpRegen();
        }
    }

    // GỐC Character.ewf: mỗi frame, còn sống và chưa đầy máu → hp += hồi máu/giây × deltaTime × tốc game
    // (không hiện số, không FX). Hồi máu/giây = stats.hpRecovery (Hero: MaxHp × Regen% × HealthRegenMultiplier).
    private void TickHpRegen()
    {
        double regen = GetHpRegenPerSecond();
        if (regen <= 0 || isDead || hp <= 0 || hp >= GetMaxHp())
        {
            return;
        }

        GetHeal(this, regen * Time.deltaTime * GameController.Instance.gameSpeed, showFx: false, showText: false);
    }
    #endregion

    #region Initialize
    protected virtual void Initialize()
    {
        if (buffController == null) buffController = gameObject.AddComponent<BuffController>();
        if (debuffController == null) debuffController = gameObject.AddComponent<DebuffController>();
        if (dotController == null) dotController = gameObject.AddComponent<DamageOverTimeController>();
        if (crowdController == null) crowdController = gameObject.AddComponent<CrowdControlController>();
        if (shieldController == null) shieldController = gameObject.AddComponent<ShieldController>();
    }

    public virtual void Active(Vector3 position)
    {
        Initialize();
        Renew();
        GameController.Instance.AddUnit(this);
        Transform.position = position;
        gameObject.SetActive(true);
    }

    protected virtual void Renew()
    {
        // Lệch pha thời điểm quét mục tiêu để các unit không dồn quét vào cùng 1 frame.
        nextFindTargetTime = Time.time + Random.value * FIND_TARGET_INTERVAL;

        ResetControllers();
        hp = stats.maxHp;
        //rigid.DOKill();
        //transform.DOKill();
        animationController?.Renew();
        animationController?.UpdateSortingOrder();
        ResetAction();
        hpBar?.Reset();
        hpBar?.Deactive();
        isPause = GameController.Instance.mode.isPause;
    }

    public virtual void ResetAction(bool isClearTarget = true)
    {
        if (isDead)
        {
            return;
        }

        if (isClearTarget)
        {
            target = null;
        }

        isMoving = false;
        isAttacking = false;
        mover?.Stop();
        moveSpeedFactor = 1f;

        stateCurrent = BattleState.None;
        animationController?.Reset();

        ActiveHpAndCollider(true);
    }

    protected virtual void ResetControllers()
    {
        try
        {
            RemoveBuffs();
            RemoveDebuffs();
        }
        catch { }
    }

    protected virtual void RemoveBuffs()
    {
        buffController?.Reset();
        shieldController?.Reset();
        ReloadStats();
    }

    protected virtual void RemoveDebuffs()
    {
        debuffController?.Reset();
        crowdController?.Reset();
        dotController?.Reset();
        ReloadStats();
    }

    public virtual void Intro(Vector3 spawnPosition, UnityAction callback = null) { }

    public virtual void OnBeginBattle()
    {
        ReloadStats();                    // gồm cả áp vũ khí (tầm đánh + nhánh đạn)
        hp = stats.maxHp;
        CheckUniqueSkill();
        //UpdateHealthBar();
    }

    // Vũ khí quyết định CÁCH ra đòn của unit này (null = cận chiến tay không, giữ stat mặc định).
    // Hero lấy từ đồ đang mặc (save); Enemy/Pet gán sẵn trên prefab — xem override ở lớp con.
    protected virtual WeaponData GetCombatWeapon()
    {
        return null;
    }

    // Nạp cấu hình chiến đấu từ vũ khí: có đạn hay không, prefab đạn, và tầm đánh.
    protected void ApplyWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            return;
        }

        hasProjectile = weapon.hasProjectile;
        bulletPrefab = weapon.bulletPrefab;
        stats.attackRange = weapon.attackDistance;
    }

    public virtual void OnResetMode()
    {
        Deactive();
    }
    #endregion

    #region Stats
    // Tính lại chỉ số từ đầu rồi ÁP LẠI VŨ KHÍ (tầm đánh/đạn) — mọi nơi gọi ReloadStats (InitModeDone,
    // buff/debuff/shield/CC, đổi đồ) đều giữ đúng tầm đánh của vũ khí.
    public virtual void ReloadStats()
    {
        modifiers.Clear();
        LoadPermanentModifiers();
        LoadDurationModifiers();
        CalculateCurrentStats();
        ApplyWeapon(GetCombatWeapon());

        stats.moveSpeed *= slowMultiplier;
        stats.attackSpeed *= slowMultiplier;
    }

    protected virtual void LoadPermanentModifiers()
    {

    }

    protected virtual void LoadDurationModifiers()
    {
        buffController?.RemainModifiers();
        debuffController?.RemainModifiers();
        shieldController?.RemainModifiers();
    }

    // Đổ giá trị tuyệt đối từ baseStats vào stats runtime.
    // (Buff/modifier chưa áp — thêm khi làm hệ buff sâu; xem BattleMechanic.)
    protected virtual void CalculateCurrentStats()
    {
        // Dựng Stats MỚI mỗi lần tính: mọi field (companionDamage, crit, blockChance...) về mặc định rồi
        // lớp con mới áp modifier → ReloadStats gọi nhiều lần không bị cộng/nhân dồn.
        stats = new Stats(baseStats);
    }

    #region Map unit (DungeonRush) — spawn + targeting chung cho Hero/Pet/Enemy
    // Vào trận: gán stat + team + battleId rồi kích hoạt tại vị trí world.
    public virtual void SpawnInBattle(BaseStats newBaseStats, string teamTag, Vector3 position)
    {
        baseStats = newBaseStats ?? new BaseStats();
        gameObject.tag = teamTag;
        battleId = GameController.Instance.NextBattleId();

        ReloadStats();          // đổ baseStats -> stats (qua CalculateCurrentStats)
        Active(position);       // Initialize + Renew(hp=maxHp) + AddUnit + set vị trí
        OnBeginBattle();
    }

    protected BaseUnit FindNearestEnemyAmong(List<BaseUnit> candidates, float maxRange = -1f)
    {
        return FindNearestEnemyFrom(Transform.position, candidates, maxRange);
    }

    // Enemy gần nhất so với 'center', trong 'maxRange' (maxRange < 0 = không giới hạn).
    // KHÔNG lọc theo biên camera (map top-down của DungeonRush).
    protected BaseUnit FindNearestEnemyFrom(Vector3 center, List<BaseUnit> candidates, float maxRange = -1f)
    {
        if (candidates == null)
        {
            return null;
        }

        BaseUnit nearest = null;
        float nearestSqr = float.MaxValue;
        float maxSqr = maxRange * maxRange;

        for (int i = 0; i < candidates.Count; i++)
        {
            BaseUnit c = candidates[i];
            if (c == null || !c.isTargetable || c.battleId == battleId)
            {
                continue;
            }

            float sqr = VectorUtils.SqrDistance(center, c.Transform.position);
            if (maxRange >= 0f && sqr > maxSqr)
            {
                continue;
            }

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = c;
            }
        }

        return nearest;
    }
    #endregion

    public virtual void AddModifier(StatModifier input)
    {
        AddModifier(new List<StatModifier>() { input });
    }

    public virtual void AddModifier(List<StatModifier> input)
    {
        if (input != null)
        {
            for (int i = 0; i < input.Count; i++)
            {
                if (input[i] != null)
                {
                    modifiers.Add(input[i]);
                }
            }
        }
    }

    public virtual double GetMaxHp()
    {
        return stats.maxHp;
    }

    public BaseStats GetBaseStats()
    {
        return baseStats;
    }

    public float GetHpPercent()
    {
        // Unit chưa Init stats (vd. OnDisable của health-bar ngay trong Awake) -> tránh 0/0 = NaN
        if (stats.maxHp <= 0) return 0f;
        float percent = (float)(hp / stats.maxHp);
        return Mathf.Clamp01(percent);
    }
    #endregion

    #region Animations
    public virtual void OnAnimationEventExecute(TrackEntry entry, Spine.Event e)
    {
        if (stateCurrent == BattleState.Attack)
        {
            if (string.CompareOrdinal(e.Data.Name, animationController.eventAttack) == 0)
            {
                ReleaseNormalAttack();
            }
        }

    }

    public virtual void OnAnimationComplete(TrackEntry entry)
    {
        string anim = entry.Animation.Name;
        if (string.CompareOrdinal(anim, animationController.animDie) == 0)
        {
            DebugCustom.Log("Die animation complete " + name);
            Deactive();
        }
        else if (string.CompareOrdinal(anim, animationController.animAttack) == 0)
        {
            animationController.PlayAnimIdle();
        }
    }

    protected virtual void EndAttack()
    {
        if (stateCurrent == BattleState.Attack)
        {
            isAttacking = false;
            animationController.Reset();
            ChangeState(BattleState.Idle, true);
            OnAttackEnd();
        }
    }

    public virtual void Pause()
    {
        isPause = true;
        if (rigid != null && rigid.bodyType == RigidbodyType2D.Dynamic)
        {
            rigid.linearVelocity = Vector2.zero;   // vật lý động không tự dừng khi pause bằng cờ
        }
        animationController.Pause();
        hpBar?.Pause();
        rigid.DOPause();
        transform.DOPause();
    }

    public virtual void Resume()
    {
        isPause = false;
        animationController.Resume();
        hpBar?.Resume();
        rigid.DOPlay();
        transform.DOPlay();
    }

    protected virtual void InterruptAction() { }

    public virtual bool IsTweening()
    {
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (DOTween.IsTweening(child.gameObject))
                return true;
        }

        return false;
    }
    #endregion

    #region Skills
    protected virtual void CheckUniqueSkill()
    {
    }

    public virtual bool IsReadyAutoSkill() { return false; }
    #endregion

    #region Behaviour
    protected virtual void UpdateBattleState()
    {
        FaceToTarget();

        switch (stateCurrent)
        {
            case BattleState.None: ChangeState(BattleState.Idle); break;
            case BattleState.Idle: UpdateIdle(); break;
            case BattleState.Move: UpdateMove(); break;
            case BattleState.Attack: UpdateAttack(); break;
            case BattleState.Fear: UpdateFear(); break;
            case BattleState.Patrol: UpdatePatrol(); break;
        }
    }

    protected virtual void ChangeState(BattleState newState, bool isForceChange = false)
    {
        if (isDisableChangeState)
        {
            return;
        }

        if (isForceChange || stateCurrent != newState)
        {
            if (stateCurrent == BattleState.Patrol && newState != BattleState.Patrol)
            {
                EndPatrol();
            }

            stateCurrent = newState;
            switch (stateCurrent)
            {
                case BattleState.Idle:
                    BeginIdle();
                    break;

                case BattleState.Patrol:
                    BeginPatrol();
                    break;

                case BattleState.Move:
                    BeginMove();
                    break;

                case BattleState.Attack:
                    {
                        if (crowdController.isDisableAttack)
                        {
                            ChangeState(BattleState.Idle);
                        }
                        else
                        {
                            BeginAttack();
                        }
                    }
                    break;
            }
        }
    }

    //Attack
    protected virtual void UpdateAttack() { }

    //Idle — IdleState.fbq gốc: mỗi frame chọn mục tiêu (SelectTarget). Cận chiến vào tầm → đánh; bắn xa
    // vào tầm và thấy mục tiêu → bắn; còn lại đi tới nếu được di chuyển. Không có mục tiêu: quái đi tuần (canPatrol).
    protected virtual void UpdateIdle()
    {
        if (IsReadyAutoSkill())
        {
            return;
        }

        target = SelectTarget();
        if (target == null)
        {
            if (canPatrol && isMoveable)
            {
                ChangeState(BattleState.Patrol);
            }
            return;
        }

        if (IsInAttackPosition())
        {
            ChangeState(BattleState.Attack);
        }
        else if (isMoveable)
        {
            ChangeState(BattleState.Move);
        }
    }

    protected virtual void BeginIdle()
    {
        StopMove();
        animationController.PlayAnimIdle();
    }

    // GridCombatController.hor gốc: kẻ địch còn sống gần nhất trên toàn map (phe người chơi).
    // Quái override = hos (chỉ trong tầm phát hiện). Đang bị khiêu khích thì giữ mục tiêu.
    protected virtual BaseUnit SelectTarget()
    {
        if (crowdController.isTaunted && IsTargetAvailable())
        {
            return target;
        }

        return FindNearestEnemyAmong(GetAliveEnemies());
    }

    // Đánh xa = vũ khí có đạn (cùng điều kiện PlayAnimAttack / ReleaseAttack).
    protected virtual bool IsRangedAttacker => hasProjectile && bulletPrefab != null;

    // MoveState.fcg / GridCombatController.hot + hou gốc: trong tầm đánh; bắn xa còn phải thấy mục tiêu.
    protected virtual bool IsInAttackPosition()
    {
        if (!IsTargetInAttackRange())
        {
            return false;
        }

        return !IsRangedAttacker || MapController.HasLineOfSight(Transform.position, target.Transform.position);
    }

    //Move — MoveState gốc.
    protected virtual void UpdateMove()
    {
        if (IsReadyAutoSkill())
        {
            return;
        }

        if (!IsTargetAvailable())
        {
            StopMove();
            ChangeState(BattleState.Idle);
            return;
        }

        if (IsInAttackPosition())
        {
            StopMove();
            ChangeState(BattleState.Attack);
            return;
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        if (!mover.HasPath)
        {
            // Chưa có đường (vừa tới nơi / tìm không ra) → 0.2s thử tìm lại.
            moveRepathTimer += dt;
            if (moveRepathTimer >= MOVE_REPATH_INTERVAL)
            {
                moveRepathTimer = 0f;
                ResetMoveTimers();
                RepathToTarget();
            }
            return;
        }

        moveRepathTimer = 0f;

        // Mục tiêu đã sang ô khác ô đích → tìm đường lại ngay.
        if (MapController.Instance.WorldToCell(target.Transform.position) != mover.Goal)
        {
            ResetMoveTimers();
            RepathToTarget();
            return;
        }

        // 0.15s xét lại kẻ địch gần nhất, đổi mục tiêu thì tìm đường lại.
        moveRetargetTimer += dt;
        if (moveRetargetTimer >= MOVE_RETARGET_INTERVAL)
        {
            moveRetargetTimer = 0f;
            BaseUnit nearest = SelectTarget();
            if (nearest != null && nearest != target)
            {
                target = nearest;
                ResetMoveTimers();
                RepathToTarget();
                return;
            }
        }

        // Mỗi 1s: đi chưa được 0.05 → coi như kẹt, tìm đường lại.
        moveStuckTimer += dt;
        if (moveStuckTimer >= MOVE_STUCK_CHECK_INTERVAL)
        {
            Vector2 position = Transform.position;
            float moved = Vector2.Distance(position, moveLastPosition);
            moveLastPosition = position;
            moveStuckTimer = 0f;
            if (moved < MOVE_STUCK_DISTANCE)
            {
                RepathToTarget();
            }
        }
    }

    public virtual void BeginMove()
    {
        isMoving = true;
        animationController.PlayAnimMove();
        moveRepathTimer = MOVE_REPATH_INTERVAL;
        ResetMoveTimers();
        RepathToTarget();
    }

    public virtual void StopMove()
    {
        isMoving = false;
        mover?.Stop();
    }

    private void ResetMoveTimers()
    {
        moveStuckTimer = 0f;
        moveRetargetTimer = 0f;
        moveLastPosition = Transform.position;
    }

    // MoveState.fcf gốc: chọn lại mục tiêu, ô đích = ô mục tiêu (cận chiến) hoặc ô bắn (bắn xa, fch);
    // không tới được thì thử 8 ô quanh đích (đi được, chưa có ai đứng); vẫn không được → đứng Idle.
    private void RepathToTarget()
    {
        BaseUnit next = SelectTarget();
        if (next == null)
        {
            StopMove();
            ChangeState(BattleState.Idle);
            return;
        }
        target = next;

        MapController map = MapController.Instance;
        Vector2Int goal = map.ClampCell(map.WorldToCell(target.Transform.position));
        if (IsRangedAttacker)
        {
            goal = FindFiringCell(goal);
        }

        if (mover.RequestMoveTo(goal))
        {
            return;
        }

        var neighbors = new List<Vector2Int>(8);
        MapSpawnCells.GetNeighbors8(map.Grid, goal, neighbors);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector2Int cell = neighbors[i];
            if (map.IsWalkable(cell) && !map.IsOccupiedByOther(cell, this) && mover.RequestMoveTo(cell))
            {
                return;
            }
        }

        StopMove();
        ChangeState(BattleState.Idle);
    }

    // MoveState.fch gốc: ô bắn cho đơn vị bắn xa — trong vuông bán kính max(1, floor(tầm) − 1) quanh mục tiêu,
    // đi được, cách mục tiêu ≥ nửa tầm, thấy được mục tiêu; chọn ô GẦN mục tiêu nhất. Không có → ô mục tiêu.
    private Vector2Int FindFiringCell(Vector2Int targetCell)
    {
        MapController map = MapController.Instance;
        float range = stats.attackRange;
        int radius = Mathf.Max(1, Mathf.FloorToInt(range) - 1);
        float minDistance = range * 0.5f;
        Vector2 targetPosition = target.Transform.position;

        Vector2Int best = targetCell;
        float bestDistance = float.MaxValue;
        for (int dCol = -radius; dCol <= radius; dCol++)
        {
            for (int dRow = -radius; dRow <= radius; dRow++)
            {
                var cell = new Vector2Int(targetCell.x + dRow, targetCell.y + dCol);
                if (!map.IsWalkable(cell))
                {
                    continue;
                }

                Vector2 position = map.CellToWorld(cell);
                float distance = Vector2.Distance(position, targetPosition);
                if (distance < minDistance || distance >= bestDistance || !MapController.HasLineOfSight(position, targetPosition))
                {
                    continue;
                }

                best = cell;
                bestDistance = distance;
            }
        }
        return best;
    }

    //Patrol — PatrolState gốc.
    protected virtual void BeginPatrol()
    {
        patrolHomeCell = MapController.Instance.WorldToCell(Transform.position);
        patrolWaiting = false;
        patrolWaitTimer = 0f;
        patrolStuckTimer = 0f;
        patrolLastPosition = Transform.position;
        moveSpeedFactor = PATROL_SPEED_FACTOR;
        PickPatrolCell();
    }

    protected virtual void EndPatrol()
    {
        moveSpeedFactor = 1f;
        StopMove();
    }

    protected virtual void UpdatePatrol()
    {
        BaseUnit found = SelectTarget();
        if (found != null)
        {
            target = found;
            ChangeState(BattleState.Move);
            return;
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        if (patrolWaiting)
        {
            patrolWaitTimer += dt;
            if (patrolWaitTimer >= patrolWaitDuration)
            {
                patrolWaiting = false;
                PickPatrolCell();
            }
            return;
        }

        if (!mover.HasPath)
        {
            StartPatrolWait();
            return;
        }

        // Đi chưa được 0.05 trong frame này dồn đủ 0.8s → dừng, đứng chờ.
        Vector2 position = Transform.position;
        if (Vector2.Distance(position, patrolLastPosition) < PATROL_STUCK_DISTANCE)
        {
            patrolStuckTimer += dt;
            if (patrolStuckTimer >= PATROL_STUCK_TIME)
            {
                StopMove();
                StartPatrolWait();
            }
        }
        else
        {
            patrolStuckTimer = 0f;
        }
        patrolLastPosition = position;
    }

    // PatrolState.fcj: xáo 8 ô quanh ô xuất phát, đi tới ô đầu tiên (khác ô đang đứng, đi được); không có → đứng chờ.
    private void PickPatrolCell()
    {
        MapController map = MapController.Instance;
        var offsets = new List<Vector2Int>(8);
        for (int dCol = -1; dCol <= 1; dCol++)
        {
            for (int dRow = -1; dRow <= 1; dRow++)
            {
                if (dCol != 0 || dRow != 0)
                {
                    offsets.Add(new Vector2Int(dRow, dCol));
                }
            }
        }
        for (int i = offsets.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (offsets[i], offsets[j]) = (offsets[j], offsets[i]);
        }

        Vector2Int current = map.WorldToCell(Transform.position);
        for (int i = 0; i < offsets.Count; i++)
        {
            Vector2Int cell = patrolHomeCell + offsets[i];
            if (cell == current || !map.IsWalkable(cell))
            {
                continue;
            }

            patrolStuckTimer = 0f;
            if (mover.RequestMoveTo(cell))
            {
                isMoving = true;
                animationController.PlayAnimMove();
            }
            return;
        }

        StartPatrolWait();
    }

    private void StartPatrolWait()
    {
        patrolWaiting = true;
        patrolWaitTimer = 0f;
        patrolWaitDuration = Random.Range(PATROL_WAIT_MIN, PATROL_WAIT_MAX);
        patrolStuckTimer = 0f;
        isMoving = false;
        animationController.PlayAnimIdle();
    }

    public virtual void EndWaitBossAppear()
    {
        ChangeState(BattleState.Idle, true);
    }

    public virtual AttackData GetBasicAttackData(double damageMultiply = 1f)
    {
        double damage = stats.attack * damageMultiply;
        damage *= stats.basicAttackDamage;

        return new AttackData(this, AttackType.BasicAttack, CritType.Critable, damage);
    }

    protected virtual void BeginAttack()
    {
        if (isEnableAttack)
        {
            if (IsTargetAvailable())
            {
                StopMove();
                // Đầu mỗi nhịp đánh quay mặt về phía đối thủ (trái/phải).
                SetFacingDirection(GetDirectionToTarget());
                PlayAnimAttack();
                isAttacking = true;

                float delayNextAttack = (1f / (stats.attackSpeed * GameController.Instance.gameSpeed));
                this.StartDelayAction(delayNextAttack, () =>
                {
                    EndAttack();
                });
            }
            else
            {
                animationController.Reset();
                ChangeState(BattleState.Idle);
            }
        }
    }

    protected virtual void GetHit()
    {
        animationController.PlayAnimHit();
    }

    protected virtual void PlayAnimAttack()
    {
        animationController.ResetAnimHit();
        // Cùng điều kiện với ReleaseAttack: có đạn = đánh xa.
        animationController.isRangedAttack = hasProjectile && bulletPrefab != null;
        animationController.PlayAnimAttack();
    }

    protected virtual void ReleaseNormalAttack()
    {
        RaiseEventNormalAttack();
        PlaySfxAttack();
    }

    protected virtual BaseBullet ReleaseBullet()
    {
        return null;
    }

    // Áp sát thương ở CUỐI mỗi nhịp đánh. Lý do: AnimationController hiện là stub → Spine
    // anim-event không bắn → sát thương gốc (ReleaseNormalAttack) không chạy. EndAttack gọi
    // OnAttackEnd chắc chắn mỗi nhịp → dùng làm điểm ra đòn tạm. (Thay bằng anim-event thật khi port Spine.)
    protected virtual void OnAttackEnd()
    {
        ReleaseAttack();
    }

    // Ra đòn 1 nhịp: vũ khí có đạn thì nhả bullet (bắn xa) mang theo AttackData, không thì đánh
    // thẳng (cận chiến). Damage tính CHUNG qua GetBasicAttackData() (server-driven) — nhánh chỉ khác
    // CÁCH truyền sát thương tới target, không đổi công thức damage.
    protected virtual void ReleaseAttack()
    {
        if (target == null || !target.isTargetable || !IsTargetInAttackRange())
        {
            return;
        }

        AttackData data = GetBasicAttackData();
        if (hasProjectile && bulletPrefab != null)
        {
            BaseBullet bullet = PoolingController.Instance.GetBullet(bulletPrefab);
            bullet.Active(firePoint, this, target, data);
        }
        else
        {
            target.TakeAttack(data, impactAttack);
        }
    }

    public virtual bool IsInBattleScreen()
    {
        // Map top-down của DungeonRush không giới hạn theo biên camera ngang như StickIdle.
        // Chưa gán biên camera → coi như luôn trong "màn" (mọi unit đều target được).
        if (CameraController.Instance.left == null || CameraController.Instance.right == null)
        {
            return true;
        }

        float minX = CameraController.Instance.left.transform.position.x - 0.5f;
        float maxX = CameraController.Instance.right.transform.position.x + 0.5f;

        return Transform.position.x < maxX && Transform.position.x > minX;
    }
    #endregion

    #region Events
    public virtual void OnAttackDone(ProcessedAttackData processedAttackData, double damageDealt) { }

    public virtual void OnKillEnemy(int enemyBattleId) { }

    public virtual void OnUnitDie(object obj) { }

    protected virtual void RaiseEventNormalAttack()
    {
    }

    protected virtual void OnEndGame(object obj) { }
    #endregion

    #region Take Damage
    public virtual double TakeAttack(AttackData attackData, BaseFx impact = null, UnitBodyPoint impactPoint = UnitBodyPoint.CenterBody)
    {
        if (attackData == null || isDead)
        {
            return 0f;
        }

        if (stats.evasionRate > 0f && Random.value <= stats.evasionRate)
        {
            FxController.Instance.ShowStatus(TextDamageStatus.Evade, centerBodyPoint.position);
            return 0f;
        }

        if (impact != null)
        {
            ShowImpactHit(impact, impactPoint);
        }

        ProcessedAttackData processedAttackData = ProcessAttackData(attackData);
        TakeDamageData takeDamageData = new TakeDamageData(processedAttackData);
        double takenDamage = TakeDamage(takeDamageData);
        processedAttackData.attacker.OnAttackDone(processedAttackData, takenDamage);

        if (isDead == false)
        {
            crowdController.TakeCC(attackData.controls);
            dotController.TakeDots(attackData.dots);
            debuffController.TakeDebuffs(attackData.debuffs);
        }

        return takenDamage;
    }

    public virtual double TakeDamage(TakeDamageData data, bool showTextDamage = true, bool flashColor = true)
    {
        if (isDead || data == null)
        {
            return 0f;
        }

        if (IsBlock(data))
        {
            FxController.Instance.ShowStatus(TextDamageStatus.Block, centerBodyPoint.position);
            return 0f;
        }

        double takenDamage = data.damage;
        if (takenDamage > 0f)
        {
            double remainingShieldBlockValue = shieldController.GetRemainingValue(ShieldType.Block);
            if (remainingShieldBlockValue > 0f)
            {
                shieldController.BlockDamage();
                FxController.Instance.ShowStatus(TextDamageStatus.Block, centerBodyPoint.position);
                return 0f;
            }

            double remainingShieldAbsorbValue = shieldController.GetRemainingValue(ShieldType.Absorb);
            if (remainingShieldAbsorbValue > 0f)
            {
                double damageAbsorbed = 0f;
                if (takenDamage >= remainingShieldAbsorbValue)
                {
                    damageAbsorbed = remainingShieldAbsorbValue;
                    takenDamage -= damageAbsorbed;
                }
                else
                {
                    damageAbsorbed = takenDamage;
                    takenDamage = 0f;
                }

                shieldController.ReduceAbsorbValue(damageAbsorbed);
            }
        }

        // Bất tử (companion Immortal): giữ HP tối thiểu 1.
        if (isImmortal && hp - takenDamage < 1f)
        {
            takenDamage = System.Math.Max(0f, hp - 1f);
        }

        hp -= takenDamage;
        crowdController.OnTakeDamage(takenDamage);

        if (showTextDamage)
            ShowTextDamage(takenDamage, data.isCrit);

        if (flashColor)
            FlashColorTakeDamage();

        if (hp <= 0)
        {
            Die();
        }
        else
        {
            GetHit();
        }

        UpdateHealthBar();
        return takenDamage;
    }

    protected virtual ProcessedAttackData ProcessAttackData(AttackData attackData)
    {
        BaseUnit attacker = attackData.attacker;
        double damage = attackData.damage;
        damage *= MultiplyDamageByUnitType(attackData);
        bool isCrit = IsCrit(attacker, attackData.critType);
        double finalDamage = CalculateDamage(damage, attacker, isCrit);
        ProcessedAttackData processedData = new ProcessedAttackData(attacker, this, attackData.attackType, finalDamage, isCrit);
        return processedData;
    }

    public virtual double ProcessDamageOverTime(DamageOverTimeData dotData)
    {
        BaseUnit attacker = dotData.attacker;

        if (isDead || attacker == null)
        {
            return 0f;
        }

        double damage = 0f;
        bool isCrit = false;

        if (dotData.calculateType == DamageOverTimeCalculateType.ByVictimMaxHp)
        {
            damage = stats.maxHp * dotData.multiply;
        }
        else if (dotData.calculateType == DamageOverTimeCalculateType.ByAttack)
        {
            damage = stats.attack * dotData.multiply;
        }

        damage = CalculateDamage(damage, attacker, isCrit: false);
        TakeDamageData takeDamageData = new TakeDamageData(attacker, AttackType.SkillEquipped, damage, isCrit);
        return TakeDamage(takeDamageData, showTextDamage: false, flashColor: false);
    }

    protected virtual double CalculateDamage(double inputDamage, BaseUnit attacker, bool isCrit)
    {
        double damage = inputDamage;

        if (isCrit)
        {
            damage *= attacker.stats.critDamage;
        }

        return damage;
    }

    // Chặn TRỌN 1 đòn — GỐC (Character.ewb + rm.ioz): chỉ khi bên đánh là ENEMY (không phải phe người
    // chơi), roll Random.Range(0,100) <= BlockChance của bên bị đánh. BlockChance = substat đồ + phần
    // Guardian (Vital Root) cộng thêm tạm thời.
    protected virtual bool IsBlock(TakeDamageData data)
    {
        if (data.attacker == null || data.attacker.isTeamA)
        {
            return false;
        }

        float chance = stats.blockChance + bonusBlockChance;
        return chance > 0f && Random.Range(0f, 100f) <= chance;
    }

    protected virtual bool IsCrit(BaseUnit attacker, CritType critType)
    {
        if (critType == CritType.AlwaysCrit)
        {
            return true;
        }

        if (critType == CritType.NonCrit)
        {
            return false;
        }

        if (critType == CritType.Critable && attacker.stats.critRate > 0f)
        {
            if (Random.value <= attacker.stats.critRate)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        return false;
    }

    protected virtual double MultiplyDamageByUnitType(AttackData attackData)
    {
        return 1f;
    }

    public virtual void ShowTextDamage(double damage, bool isCrit = false)
    {
        Vector3 v = centerBodyPoint.position;
        FxController.Instance.ShowDamage(v, damage, isCrit, isTeamA);
    }

    public virtual void FlashColorTakeDamage()
    {
        if (animationController != null)
            animationController.FlashColor(Color.white);
    }

    public virtual BaseFx ShowImpactHit(BaseFx prefab, UnitBodyPoint impactPoint = UnitBodyPoint.CenterBody)
    {
        BaseFx fxImpact = null;

        if (prefab != null)
        {
            if (impactPoint == UnitBodyPoint.CenterBody)
            {
                fxImpact = FxController.Instance.SpawnFx(prefab, centerBodyPoint.position);
            }
            else if (impactPoint == UnitBodyPoint.Pivot)
            {
                fxImpact = FxController.Instance.SpawnFx(prefab, Transform.position);
            }

            // Impact FX must always return to the pool. Imported prefabs often
            // leave lifeTime at zero, which otherwise leaks active instances
            // until FxController reaches its per-effect spawn limit.
            if (fxImpact != null && fxImpact.lifeTime <= 0f)
            {
                fxImpact.DelayDeactive(DEFAULT_IMPACT_LIFETIME);
            }
        }

        return fxImpact;
    }

    public virtual void Die()
    {
        hp = 0f;
        ResetAction();
        //ResetControllers();
        InterruptAction();
        ActiveHpAndCollider(false);
        stateCurrent = BattleState.Die;
        StopAllCoroutines();
        //rigid.DOKill();
        StopMove();
        hpBar?.Deactive();
        DeactiveHitBoxes();
        ShowFxDie();
        animationController.SetDefaultColor();
        // Adapt: PostEvent của DungeonRush cần sender Component -> dùng extension trên MonoBehaviour.
        this.PostEvent(EventID.UnitDie, battleId);
    }

    public virtual void BeginDie()
    {
        animationController.PlayAnimDie();
    }

    protected virtual void ShowFxDie() { }

    public virtual void Deactive()
    {
        //try
        //{
        //    animationController.skeletonAnimation.ClearState();
        //}
        //catch { }

        GameController.Instance.RemoveUnit(gameObject);
        gameObject.SetActive(false);


    }

    protected virtual void DeactiveHitBoxes() { }

    public virtual bool CanTakeDamageFrom(BaseUnit attacker)
    {
        if (isTargetable)
        {
            if (IsEnemy(attacker))
            {
                return true;
            }

            if (IsAlly(attacker))
            {
                return attacker.crowdController.isCharm;
            }
        }

        return false;
    }

    public virtual void ActiveHpAndCollider(bool isOn)
    {
        if (bodyCollider == null)
            bodyCollider = transform.Find("Body").GetComponent<Collider2D>();

        if (bodyCollider != null)
            bodyCollider.gameObject.SetActive(isOn);

        if (isOn)
        {
            hpBar?.Resume();
        }
        else
        {
            hpBar?.Pause();
        }
    }
    #endregion

    #region Hp/Energy
    public virtual void UpdateHealthBar()
    {
        float hpPercent = GetHpPercent();
        hpBar?.UpdateHealthBar(hpPercent);
    }

    public virtual void GetHeal(BaseUnit healer, double value, bool showFx = true, bool showText = true, float fxScale = 1f)
    {
        if (isDead || value <= 0f)
        {
            return;
        }

        double hpHeal = value;
        if (hpHeal > 0)
        {
            hp += hpHeal;
            double maxHp = GetMaxHp();
            if (hp > maxHp)
                hp = maxHp;

            if (showFx)
            {
                FxController.Instance.SpawnFx(FxController.Instance.fxHeal, Transform.position, fxScale);
            }

            if (showText)
            {
                FxController.Instance.ShowHeal(centerBodyPoint.position, hpHeal);
            }

            if (hpBar.gameObject.activeSelf)
            {
                UpdateHealthBar();
            }
        }
    }

    public virtual double GetHpRegenPerSecond()
    {
        double hpRegen = stats.hpRecovery;
        return hpRegen;
    }
    #endregion

    #region Buffs - Debuffs
    public virtual void AnimateCC(CrowdControlData ccData)
    {
        try
        {
            InterruptAction();

            switch (ccData.type)
            {
                case CrowdControlType.Stun: Stun(); break;
                case CrowdControlType.Paralyze: Paralyze(); break;
                case CrowdControlType.Freeze: Freeze(); break;
                case CrowdControlType.Rooted: Rooted(); break;
                case CrowdControlType.Taunted: Taunted(ccData.attacker.battleId); break;
                case CrowdControlType.Silent: Silence(); break;
                case CrowdControlType.Disarm: Disarm(); break;
                case CrowdControlType.Charm: Charm(); break;
                case CrowdControlType.Hex: Hex(); break;
                case CrowdControlType.Fear: Fear(ccData.attacker); break;
                case CrowdControlType.KnockBack: KnockBack(ccData.attacker, (float)ccData.value, ccData.duration); break;
                case CrowdControlType.KnockDown: KnockDown(); break;
                case CrowdControlType.KnockUp: KnockUp((float)ccData.value, ccData.duration); break;
            }
        }
        catch (Exception e)
        {
            DebugCustom.LogError(e.Message);
            ResetAction();
        }
    }

    public virtual void RemoveCC(CrowdControlType type)
    {
        if (type == CrowdControlType.KnockDown)
        {
            GetUp();
            return;
        }

        animationController.Reset();

        if (type == CrowdControlType.Freeze)
        {
            animationController.Freeze(false);
        }
        else if (type == CrowdControlType.Taunted)
        {
            animationController.SetDefaultColor();
        }
        else if (type == CrowdControlType.Hex)
        {
            animationController.ActiveMeshRenderer(true);
            hpBar?.Resume();
            ReloadStats();
        }
        else if (type == CrowdControlType.Fear)
        {
            stateCurrent = BattleState.None;
        }
    }

    protected virtual void ShowFxCC(CrowdControlType type, Vector3 v)
    {
        BaseFx prefab = null;

        switch (type)
        {
            case CrowdControlType.Stun: prefab = FxController.Instance.fxStun; break;
            case CrowdControlType.Paralyze: prefab = FxController.Instance.fxParalyze; break;
            case CrowdControlType.Rooted: prefab = FxController.Instance.fxRooted; break;
            case CrowdControlType.Silent: prefab = FxController.Instance.fxSilent; break;
            case CrowdControlType.Disarm: prefab = FxController.Instance.fxDisarm; break;
            case CrowdControlType.Charm: prefab = FxController.Instance.fxCharm; break;
            case CrowdControlType.Hex: prefab = FxController.Instance.fxHex; break;
            case CrowdControlType.Fear: prefab = FxController.Instance.fxFear; break;
        }

        if (prefab != null)
        {
            BaseFx fx = FxController.Instance.SpawnFx(prefab, v);
            if (fx != null)
            {
                fx.transform.parent = flipPoints;
                fx.transform.localRotation = Quaternion.identity;
                crowdController.remainingFxInstances[type] = fx;
            }
        }
    }

    public virtual bool IsImmuneKnock() { return false; }

    protected virtual void Stun()
    {
        animationController.Reset();
        Vector3 v = Vector3.zero;
        if (hpBar != null)
        {
            v = hpBar.transform.position;
            v.y -= 0.2f;
        }
        else
        {
            v = Transform.position;
            v.y += 1.2f;
        }

        ShowFxCC(CrowdControlType.Stun, v);
    }

    protected virtual void Paralyze()
    {
        animationController.Reset();
        ShowFxCC(CrowdControlType.Paralyze, Transform.position);
    }

    protected virtual void Freeze()
    {
        animationController.Freeze(true);
    }

    protected virtual void Rooted()
    {
        animationController.Reset();
        ShowFxCC(CrowdControlType.Rooted, Transform.position);
    }

    protected virtual void Taunted(int attackerBattleId)
    {
        BaseUnit attacker = GameController.Instance.GetUnitByBattleId(attackerBattleId);
        if (attacker != null)
        {
            ResetAction();
            target = attacker;
            animationController.ResetColorByCC();
        }
    }

    protected virtual void Silence()
    {
        ShowFxCC(CrowdControlType.Silent, Transform.position);
    }

    protected virtual void Disarm()
    {
        if (isAttacking)
        {
            ResetAction();
        }

        Vector3 v = Vector3.zero;
        if (hpBar != null)
        {
            v = hpBar.transform.position;
            v.y += 0.4f;
        }
        else
        {
            v = Transform.position;
            v.y += 1.2f;
        }

        ShowFxCC(CrowdControlType.Disarm, v);
    }

    protected virtual void Charm()
    {
        Vector3 v = Vector3.zero;
        if (hpBar != null)
        {
            v = hpBar.transform.position;
            v.y -= 0.2f;
        }
        else
        {
            v = Transform.position;
            v.y += 1.2f;
        }

        ShowFxCC(CrowdControlType.Charm, v);
    }

    protected virtual void Hex()
    {
        animationController.Reset();
        animationController.ActiveMeshRenderer(false);
        hpBar?.Pause();
        ShowFxCC(CrowdControlType.Hex, Transform.position);
        RemoveBuffs();
    }

    protected virtual void Fear(BaseUnit caster)
    {
        if (hpBar != null)
        {
            Vector3 v = Vector3.zero;
            v = hpBar.transform.position;
            v.y += 0.2f;
            ShowFxCC(CrowdControlType.Fear, v);
        }

        Vector3 point = Vector3.zero;

        if (caster != null)
        {
            Vector3 dir = (Transform.position - caster.Transform.position).normalized;
            point = caster.Transform.position + dir * Random.Range(2.5f, 4f);
        }
        else
        {
            point = Transform.position.GetRandomPointAround(2f, 3f);
        }

        crowdController.fearMovePoint = MapController.Instance.ClampPointInMap(point);
        Face(crowdController.fearMovePoint);
        mover?.Stop();
        stateCurrent = BattleState.Fear;
    }

    protected virtual void UpdateFear()
    {
        if (stateCurrent == BattleState.Fear)
        {
            if (isEnableMove == false)
            {
                return;
            }

            Vector3 fearMovePoint = crowdController.fearMovePoint;
            float distance = Vector3.Distance(Transform.position, fearMovePoint);
            if (distance <= 0.1f)
            {
                animationController.PlayAnimIdle();
            }
            else
            {

                float speed = stats.moveSpeed * GameController.Instance.gameSpeed;
                Vector3 dir = (fearMovePoint - Transform.position).normalized;
                rigid.MovePosition(Transform.position + speed * dir * Time.fixedDeltaTime);
                animationController.PlayAnimMove();
                animationController.SetTimeScaleAnimMoveBySpeed();
            }

            Face(fearMovePoint);
        }
    }



    public virtual bool Pulled(Vector3 point, float duration)
    {
        if (isImmuneCC || crowdController.isDisablePull)
        {
            return false;
        }

        crowdController.isPulled = true;
        animationController.Reset();
        animationController.PlayAnimIdle();
        rigid.DOMove(point, duration).OnComplete(() =>
        {
            crowdController.isPulled = false;
            ResetAction();
        });

        return true;
    }

    public virtual void KnockBack(Vector3 direction, float range = 1f, float duration = 0.3f)
    {
        animationController.Reset();
        animationController.PlayAnimIdle();

        Vector3 backPos = Transform.position + direction * range;
        backPos = MapController.Instance.ClampPointInMap(backPos);
        rigid.DOMove(backPos, duration).OnComplete(() =>
        {
            ResetAction(isClearTarget: false);
        });
    }

    public virtual void KnockBack(BaseUnit attacker, float range = 1f, float duration = 0.3f)
    {
        Vector3 dir = Vector3.zero;

        if (attacker != null)
        {
            dir = (Transform.position - attacker.Transform.position).normalized;
        }
        else
        {
            float euler = isFacingRight ? -90f : 90f;
            dir = Quaternion.Euler(0, euler, 0) * transform.up;
        }

        KnockBack(dir, range, duration);
    }

    public virtual void KnockDown()
    {
        animationController.Reset();
        animationController.PlayAnimKnockDown();
        hpBar?.gameObject.SetActive(false);
    }

    public virtual void KnockUp(float high, float duration)
    {
        stateCurrent = BattleState.KnockUp;
        animationController.Reset();
        animationController.PlayAnimKnockUp();
        rigid.DOJump(Transform.position, high, 1, duration).OnComplete(() =>
        {
            ResetAction();
        });
    }

    protected virtual void GetUp()
    {
        animationController.PlayAnimGetUp(() =>
        {
            hpBar?.gameObject.SetActive(true);
            ResetAction();
        });
    }

    public virtual void ActiveShield(ShieldId type, bool isOn) { }
    #endregion

    #region Direction
    protected virtual void SetFacingDirection(Vector3 dir)
    {
        if (dir.x > MINIMUM_UPDATE_DIRECTION_VALUE)
        {
            if (animationController.isFacingRight == false)
            {
                Face(isRight: true);
            }
        }
        else if (dir.x < -MINIMUM_UPDATE_DIRECTION_VALUE)
        {
            if (animationController.isFacingRight)
            {
                Face(isRight: false);
            }
        }
    }

    public virtual void Face(bool isRight)
    {
        animationController.isFacingRight = isRight;
        flipPoints.right = isRight ? Vector3.right : Vector3.left;

        if (hpBar != null)
        {
            Vector3 v = hpBar.transform.localScale;
            v.x = isRight ? Mathf.Abs(v.x) : -Mathf.Abs(v.x);
            hpBar.transform.localScale = v;
        }
    }

    protected virtual void Face(Vector3 point)
    {
        bool isRight = point.x > Transform.position.x;
        Face(isRight);
    }

    protected virtual void FaceToTarget()
    {
        if (isEnableFaceDirection)
        {
            Vector3 dir = GetDirectionToTarget();
            SetFacingDirection(dir);
        }
    }

    protected virtual Vector3 GetDirectionToTarget()
    {
        Vector3 dir = Vector3.zero;

        if (target != null)
        {
            dir = (target.Transform.position - Transform.position).normalized;
        }

        return dir;
    }
    #endregion

    #region Targets
    public List<BaseUnit> GetAllies()
    {
        List<BaseUnit> allies = new List<BaseUnit>();

        if (CompareTag(StaticValue.TAG_TEAM_A))
        {
            allies = GameController.Instance.mode.teamA;
        }
        else if (CompareTag(StaticValue.TAG_TEAM_B))
        {
            allies = GameController.Instance.mode.teamB;
        }

        return allies;
    }

    public bool IsAlly(BaseUnit unit)
    {
        if (unit == null)
        {
            return false;
        }

        if (CompareTag(StaticValue.TAG_TEAM_A))
        {
            return unit.gameObject.CompareTag(StaticValue.TAG_TEAM_A);
        }
        else if (CompareTag(StaticValue.TAG_TEAM_B))
        {
            return unit.gameObject.CompareTag(StaticValue.TAG_TEAM_B);
        }

        return false;
    }

    public bool IsEnemy(BaseUnit unit)
    {
        if (unit == null)
        {
            return false;
        }

        if (CompareTag(StaticValue.TAG_TEAM_A))
        {
            return unit.gameObject.CompareTag(StaticValue.TAG_TEAM_B);
        }
        else if (CompareTag(StaticValue.TAG_TEAM_B))
        {
            return unit.gameObject.CompareTag(StaticValue.TAG_TEAM_A);
        }

        return false;
    }

    public virtual void FindNextTarget()
    {
        // Throttle: chỉ quét lại mục tiêu mỗi FIND_TARGET_INTERVAL, giữ target hiện tại giữa các nhịp.
        if (Time.time < nextFindTargetTime)
        {
            return;
        }
        nextFindTargetTime = Time.time + FIND_TARGET_INTERVAL;

        FindNearestTarget();
    }

    public virtual List<BaseUnit> GetAliveAllies()
    {
        List<BaseUnit> units = GameController.Instance.GetAliveUnits(isTeamA);
        return units;
    }

    public virtual List<BaseUnit> GetAllyInRange(float range)
    {
        return GetAllyInRange(Transform.position, range);
    }

    public virtual List<BaseUnit> GetAllyInRange(Vector3 point, float range)
    {
        List<BaseUnit> allies = new List<BaseUnit>();

        var e = GameController.Instance.activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            BaseUnit activeUnit = e.Current.Value;

            if (activeUnit.IsAlly(this) && activeUnit.isTargetable && VectorUtils.IsInRange(point, activeUnit.Transform.position, range))
            {
                allies.Add(activeUnit);
            }
        }

        return allies;
    }

    public virtual BaseUnit GetMostCrowdedAlly(float range, float radius = 1.5f)
    {
        List<BaseUnit> inRangeAllies = GetAllyInRange(range);

        BaseUnit mostCrowdedAlly = null;
        int highestSurround = 0;

        for (int i = 0; i < inRangeAllies.Count; i++)
        {
            BaseUnit ally = inRangeAllies[i];
            int countSurround = 0;

            var e = GameController.Instance.activeUnits.GetEnumerator();
            while (e.MoveNext())
            {
                BaseUnit activeUnit = e.Current.Value;

                if (activeUnit.IsAlly(this) && activeUnit.isTargetable && VectorUtils.IsInRange(ally.Transform.position, activeUnit.Transform.position, radius))
                {
                    countSurround++;
                }
            }

            if (countSurround > highestSurround)
            {
                highestSurround = countSurround;
                mostCrowdedAlly = ally;
            }
        }

        return mostCrowdedAlly;
    }

    public virtual List<BaseUnit> GetEnemyInBattle()
    {
        List<BaseUnit> aliveEnemies = GetAliveEnemies();
        List<BaseUnit> inBattleEnemies = aliveEnemies.Where(x => x.IsInBattleScreen()).ToList();
        return inBattleEnemies;
    }

    public virtual List<BaseUnit> GetAliveEnemies()
    {
        List<BaseUnit> units = new List<BaseUnit>();

        if (crowdController.isCharm)
        {
            units = GameController.Instance.GetAliveUnits(isTeamA);
        }
        else
        {
            units = GameController.Instance.GetAliveUnits(!isTeamA);
        }

        return units;
    }

    public virtual List<BaseUnit> GetEnemyInRange(float range)
    {
        return GetEnemyInRange(Transform.position, range);
    }

    public virtual List<BaseUnit> GetEnemyInRange(Vector3 point, float range)
    {
        List<BaseUnit> inRangeEnemies = new List<BaseUnit>();

        List<BaseUnit> aliveEnemies = GetEnemyInBattle();
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            BaseUnit enemy = aliveEnemies[i];
            if (enemy.CanTakeDamageFrom(this) && VectorUtils.IsInRange(point, enemy.Transform.position, range))
            {
                inRangeEnemies.Add(enemy);
            }
        }

        return inRangeEnemies;
    }

    protected virtual BaseUnit GetMostCrowdedEnemy(float range, float radius = 1.5f)
    {
        List<BaseUnit> inRangeEnemies = GetEnemyInRange(range);

        BaseUnit mostCrowdedEnemy = null;
        int highestSurround = 0;

        for (int i = 0; i < inRangeEnemies.Count; i++)
        {
            BaseUnit enemy = inRangeEnemies[i];
            int countSurround = 0;

            var e = GameController.Instance.activeUnits.GetEnumerator();
            while (e.MoveNext())
            {
                BaseUnit activeUnit = e.Current.Value;
                if (activeUnit.CanTakeDamageFrom(this) && VectorUtils.IsInRange(enemy.Transform.position, activeUnit.Transform.position, radius))
                {
                    countSurround++;
                }
            }

            if (countSurround > highestSurround)
            {
                highestSurround = countSurround;
                mostCrowdedEnemy = enemy;
            }
        }

        return mostCrowdedEnemy;
    }

    protected virtual void FindNearestTarget()
    {
        if (crowdController.isTaunted)
        {
            return;
        }

        List<BaseUnit> opponents = GetEnemyInBattle().Where(x => x.battleId != battleId).ToList();

        if (opponents != null && opponents.Count > 0)
        {
            BaseUnit nearestUnit = null;
            float nearestDistance = 100000f;

            for (int i = 0; i < opponents.Count; i++)
            {
                BaseUnit opp = opponents[i];
                if (opp.isTargetable)
                {
                    float distance = VectorUtils.SqrDistance(transform.position, opp.Transform.position);
                    float yAmount = Mathf.Abs(transform.position.y - opp.Transform.position.y);
                    distance += yAmount * yAmount;

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestUnit = opp;
                    }
                }
            }

            target = nearestUnit;
        }
        else
        {
            target = null;
        }
    }

    protected BaseUnit FindNearestAlly()
    {
        List<BaseUnit> aliveAllies = GetAliveAllies();
        if (aliveAllies.Count > 0)
        {
            BaseUnit nearestAlly = null;
            float nearestDistance = 100000f;

            for (int i = 0; i < aliveAllies.Count; i++)
            {
                BaseUnit ally = aliveAllies[i];
                if (ally.gameObject.activeInHierarchy && ally.battleId != battleId)
                {
                    float distance = VectorUtils.SqrDistance(transform.position, ally.Transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestAlly = ally;
                    }
                }
            }

            return nearestAlly;
        }

        return null;
    }

    public virtual bool IsTargetAvailable()
    {
        if (target == null)
        {
            return false;
        }

        return target.isTargetable;
    }

    public virtual bool IsTargetInAttackRange()
    {
        if (IsTargetAvailable())
        {
            float sqrDistance = (target.Transform.position - Transform.position).sqrMagnitude;
            float range = stats.attackRange + target.bodyOffset;
            return sqrDistance <= range * range;
        }

        return false;
    }
    #endregion

    #region Audio
    protected virtual void PlaySfxAttack()
    {
        // TODO(follow-stick): bỏ check UIGamePlay/UIManager (chưa port). Thêm lại khi có hệ UI gameplay.
        AudioManager.Instance.PlaySfx(sfxAttack);
    }

    public virtual void PlaySfx(AudioClip clip, bool isLoop = false)
    {
        if (AudioManager.Instance.isMuteSfx || audioSource == null || clip == null)
        {
            return;
        }

        // TODO(follow-stick): bỏ check UIGamePlay/UIManager (chưa port).

        audioSource.loop = isLoop;
        audioSource.volume = AudioManager.Instance.SfxVolume;

        if (isLoop)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            audioSource.PlayOneShot(clip);
        }
    }
    #endregion
}
