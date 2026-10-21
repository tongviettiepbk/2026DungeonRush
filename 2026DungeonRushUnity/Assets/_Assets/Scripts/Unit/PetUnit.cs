using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pet/Companion (TeamA) — theo Companion gốc: KHÔNG lao vào đánh, chỉ ĐI THEO hero (Companion.giu):
//   cách hero > FollowDistance (data, 3) → cứ 0.3s tìm đường A* tới ô hero, lái lực (UnitMover tham số Companion);
//   về trong FollowDistance thì thôi theo. Ra chiêu thì đứng yên 0.3s. Các pet đẩy nhau ra khi gần hơn 1.2 (gip).
//   Kỹ năng hồi xong + có mục tiêu (quanh hero) là ra đòn ngay tại chỗ đứng.
//
// LỚP NỀN companion (bám StickIdle BaseCompanion): giữ CompanionData, quy đổi data → hành vi
// (nhịp = cooldown, tầm = followDistance/minDistance) và tính SÁT THƯƠNG companion từ data +
// hệ số CompanionDamage của chủ. Mỗi loại companion (DPS/Heal/Lightning/…) là 1 lớp con
// override ReleaseAbility() khi khác kiểu mặc định (đạn đuổi theo target đơn mục tiêu).
public class PetUnit : BaseUnit
{
    public BaseUnit owner;               // hero để đi theo (gán khi spawn)
    public float engageRange = 3f;       // chỉ giao chiến enemy trong tầm này quanh hero
    public float followDistance = 3f;    // CompanionData.FollowDistance: xa hơn thì đi theo hero

    // Hằng số Companion gốc.
    private const float FOLLOW_REPATH_INTERVAL = 0.3f;   // vzz: chu kỳ tìm lại đường tới hero
    private const float CAST_FREEZE = 0.3f;              // gis: đứng yên sau khi ra chiêu
    private const float SEPARATION_RADIUS = 1.2f;        // wai
    private const float SEPARATION_FORCE = 15f;          // waj
    private const float SEPARATION_MIN_SQR = 0.001f;

    private static readonly List<PetUnit> activePets = new List<PetUnit>();
    private bool isFollowing;
    private float followRepathTimer;
    private float castFreezeRemain;

    [Tooltip("Vũ khí của pet (cận chiến/bắn xa + tầm đánh). Trống = cận chiến tay không.")]
    [SerializeField] private WeaponData weaponData;

    [Tooltip("Data companion: chỉ số cân bằng + loại kỹ năng. Gán sẵn trên prefab hoặc set khi spawn.")]
    [SerializeField] protected CompanionData companionData;

    [Tooltip("Prefab đạn mặc định (gắn BaseBullet) — đuổi theo target. Trống → đánh cận chiến.")]
    [SerializeField] protected BaseBullet projectilePrefab;

    public CompanionData Data => companionData;

    // Companion là quân đồng hành: KHÔNG bị enemy nhắm và KHÔNG chết (giống StickIdle BaseCompanion).
    public override bool isDead => false;
    public override bool isTargetable => false;
    public override bool isImmuneCC => true;

    // ===== Va chạm giữa các pet =====
    // Prefab rip để collider pet là TRIGGER → các pet đi xuyên, chồng lên nhau. Bật collider THẬT
    // (Rigidbody2D Dynamic sẵn trên prefab tự đẩy tách) nhưng CHỈ va với pet khác + tường, không
    // chạm hero/enemy (tránh pet đẩy/chặn đường hero). Thuần hiển thị: đạn/target tính theo khoảng cách.
    private const int PET_LAYER = 14;        // layer companion trên prefab rip (chỉ pet dùng)
    private const int OBSTACLE_LAYER = 12;   // layer tường (MainObstacle/ZombieObstacle/...)

    protected override void Awake()
    {
        base.Awake();
        SetupPetCollision();
    }

    private void SetupPetCollision()
    {
        // Va chạm có thể làm xoay body (prefab pet không khoá rotation).
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (bodyCollider == null)
        {
            return;
        }

        bodyCollider.gameObject.layer = PET_LAYER;   // Blaster rip để layer 0 (trùng hero)
        bodyCollider.isTrigger = false;
        bodyCollider.excludeLayers = ~((1 << PET_LAYER) | (1 << OBSTACLE_LAYER));
        bodyCollider.layerOverridePriority = 10;     // quyết định của pet thắng collider hero/enemy (priority 0)
    }

    // ===== Spawn từ CompanionData =====

    // Vào trận: gán chủ + data, quy đổi data → hành vi/chỉ số rồi kích hoạt tại vị trí.
    // Pet theo phe của chủ (PvP: pet đối thủ đứng TeamB, gốc SpawnController.hvh) — pet không phải của
    // người chơi thì do AI điều khiển: luôn tự ra chiêu, bỏ qua nút Auto/bấm tay của lobby.
    public virtual void SetupCompanion(CompanionData data, BaseUnit owner, int level, Vector3 position)
    {
        companionData = data;
        this.owner = owner;
        string teamTag = owner != null && owner.CompareTag(StaticValue.TAG_TEAM_B) ? StaticValue.TAG_TEAM_B : StaticValue.TAG_TEAM_A;
        isAiControlled = teamTag == StaticValue.TAG_TEAM_B;
        ApplyCompanionData(data, level);
        SpawnInBattle(BuildCompanionStats(data), teamTag, position);
    }

    private bool isAiControlled;

    // Quy đổi các field "di chuyển/nhịp" của companion sang tham số hành vi của PetUnit.
    protected void ApplyCompanionData(CompanionData data, int level)
    {
        if (data == null)
        {
            return;
        }

        this.level = Mathf.Max(1, level);
        engageRange = ATTACK_RANGE;
        followDistance = data.followDistance;

        // Vào trận chờ initialDelay rồi mới được kích hoạt lần đầu.
        cooldownDuration = data.cooldown > 0.01f ? data.cooldown : 1f;
        cooldownRemain = Mathf.Max(0f, data.initialDelay);
        cooldownCurrent = cooldownRemain > 0f ? cooldownRemain : cooldownDuration;
        isActivateRequested = false;
    }

    // ===== Kích hoạt kỹ năng (cooldown) =====
    // Mỗi lần ra đòn tốn 1 lượt cooldown. Hồi xong (IsReady):
    //   • Auto   → tự ra đòn khi có enemy trong tầm.
    //   • Thủ công → chờ người chơi bấm ô pet ở lobby (RequestActivate) rồi mới ra đòn.
    // Chế độ lưu ở save (UserCompanionData.isAutoActive), nút btAutoPet ở UIMainLobby bật/tắt.

    private const float ABILITY_WINDUP = 0.3f;   // thời gian vung đòn sau khi kích hoạt
    private const float ATTACK_RANGE = 20f;      // tầm đánh chung mọi companion (chọn mục tiêu quanh hero + tầm ra đòn)

    private float cooldownDuration = 1f;          // cooldown đầy đủ (CompanionData.cooldown)
    private float cooldownCurrent = 1f;           // độ dài lượt đang hồi (lần đầu = initialDelay)
    private float cooldownRemain;
    private bool isActivateRequested;

    public bool IsReady => cooldownRemain <= 0f;

    // 0 → 1: tiến độ hồi chiêu (1 = sẵn sàng) — UI lobby đổ vào thanh fill.
    public float CooldownProgress => cooldownCurrent > 0f ? 1f - Mathf.Clamp01(cooldownRemain / cooldownCurrent) : 1f;

    private static bool IsAutoActive => GameData.userData.companions.isAutoActive;

    // Được ra đòn ngay chưa: đã hồi + (đang auto hoặc người chơi đã bấm).
    private bool CanReleaseAbility => IsReady && (isAiControlled || IsAutoActive || isActivateRequested);

    // Người chơi bấm kích hoạt (chế độ thủ công). Chưa hồi xong → bỏ qua. Bấm rồi mà chưa có enemy
    // trong tầm thì giữ lệnh, gặp enemy là ra đòn.
    public bool RequestActivate()
    {
        if (!IsReady)
        {
            return false;
        }

        isActivateRequested = true;
        return true;
    }

    // Gốc (Companion.gis): ra đòn xong vào pha ACTIVE (EffectDuration, Immortal = thời gian bất tử)
    // rồi mới tới pha COOLDOWN → một lượt đầy = thời gian active + cooldown.
    // Cooldown gốc = Cooldown × (1 − CompanionCooldownReduction/100) của CHỦ (substat trang bị,
    // % cộng dồn thô). Không áp cho initialDelay và pha active.
    private void StartCooldown()
    {
        cooldownCurrent = Mathf.Max(0f, GetActiveDuration()) + GetReducedCooldown();
        cooldownRemain = cooldownCurrent;
        isActivateRequested = false;
    }

    private float GetReducedCooldown()
    {
        float reduction = owner != null ? owner.stats.companionCooldownReduction : 0f;
        return Mathf.Max(0f, cooldownDuration * (1f - reduction / 100f));
    }

    // Thời gian pha active sau khi ra đòn (mặc định CompanionData.effectDuration).
    protected virtual float GetActiveDuration()
    {
        return companionData != null ? companionData.effectDuration : 0f;
    }

    // KHÔNG gọi base: BaseUnit chỉ chạy state machine khi isTargetable, mà pet luôn isTargetable=false
    // (enemy không nhắm được) → base sẽ bỏ qua toàn bộ AI của pet.
    public override void UpdateBehavior()
    {
        if (isPause || !gameObject.activeInHierarchy)
        {
            return;
        }

        UpdateBattleState();
        TickFollow(Time.deltaTime * GameController.Instance.gameSpeed);
        animationController.UpdateSortingOrder();

        if (cooldownRemain > 0f)
        {
            cooldownRemain -= Time.deltaTime * GameController.Instance.gameSpeed;
        }
    }

    // Companion ra chiêu ở CUỐI thời gian vung đòn (ABILITY_WINDUP), không theo nhịp 50% của Soldier.
    protected override bool isHitAtMidCycle => false;

    // Vào state Attack nhưng chưa được ra đòn → đứng chờ cạnh mục tiêu (UpdateAttack sẽ gọi lại).
    protected override void BeginAttack()
    {
        if (!CanReleaseAbility)
        {
            return;
        }

        base.BeginAttack();
        if (isAttacking)
        {
            isFollowing = false;
            castFreezeRemain = CAST_FREEZE;
        }
    }

    protected override void UpdateAttack()
    {
        if (isAttacking)
        {
            return;
        }

        // Đang hồi chiêu / chờ bấm → về Idle để đi theo hero (không đứng chờ tại chỗ).
        if (!CanReleaseAbility)
        {
            ChangeState(BattleState.Idle);
            return;
        }

        // Mục tiêu chết, rời tầm giao chiến quanh hero hoặc ra ngoài tầm đánh → về Idle tìm mục tiêu khác.
        Vector3 center = owner != null ? owner.Transform.position : Transform.position;
        bool inEngage = IsTargetAvailable()
            && VectorUtils.IsInRange(center, target.Transform.position, engageRange);
        if (!inEngage || !IsTargetInAttackRange())
        {
            ChangeState(BattleState.Idle);
            return;
        }

        BeginAttack();
    }

    // BaseStats cho companion: tầm = ATTACK_RANGE, tốc độ = moveSpeed. Nhịp ra đòn KHÔNG còn do
    // attackPerSecond mà do cooldown kỹ năng (xem vùng "Kích hoạt kỹ năng"); attackPerSecond chỉ là
    // thời gian vung đòn (wind-up) sau khi kích hoạt.
    // maxHp chỉ cần > 0 (pet không chết); SÁT THƯƠNG tính riêng ở GetAbilityDamage() lúc ra đòn.
    protected virtual BaseStats BuildCompanionStats(CompanionData data)
    {
        return new BaseStats
        {
            attack = data != null ? data.damageBase : 0f,
            attackPerSecond = 1f / ABILITY_WINDUP,
            attackRange = ATTACK_RANGE,
            moveSpeed = data != null ? data.moveSpeed : 2f,
            maxHp = 1f,
        };
    }

    // Sát thương 1 nhịp kỹ năng — CÔNG THỨC GỐC (reverse libil2cpp v41, strategy DPS `kn$$gfk` @0x2C408D0):
    //   damage = (DamageBase + DamageScaler × level) × (1 + CompanionDamageBonus/100)
    // Trong đó:
    //   • level = cấp companion (1-based, cap 100) — dùng THẲNG level, KHÔNG phải (level-1).
    //     ⇒ DPS level 1 = 90 + 4.5×1 = 94.5 (mô tả "<Value>" hiển thị 90 là base, khác giá trị combat).
    //   • (1 + CompanionDamageBonus/100): native `Companion.bgoh` = Character.CompanionDamageBonus(0xF4)/100,
    //     rồi (bgoh + 1). Trùng đúng owner.stats.companionDamage (đã lưu dạng bội số 1+%).
    // Xem [[dungonrush-companion-battle-classes]].
    protected double GetAbilityDamage()
    {
        if (companionData == null)
        {
            return stats.attack;
        }

        return ScaleByLevel(companionData.damageBase, companionData.damageScaler);
    }

    // Hồi máu 1 nhịp — CÔNG THỨC GỐC (reverse `kq$$gfk`, cùng dạng sát thương):
    //   heal = (HealBase + HealScaler × level) × (1 + CompanionDamageBonus/100)
    protected double GetAbilityHeal()
    {
        if (companionData == null)
        {
            return 0f;
        }

        return ScaleByLevel(companionData.healBase, companionData.healScaler);
    }

    // (base + scaler × level) × hệ số CompanionDamage của chủ — dạng chung mọi giá trị kỹ năng gốc.
    protected double ScaleByLevel(float baseValue, float scaler)
    {
        double value = baseValue + scaler * level;
        if (owner != null)
        {
            value *= owner.stats.companionDamage;
        }
        return value;
    }

    // AttackData mang sát thương companion (thay cho GetBasicAttackData dựa trên stats.attack).
    protected AttackData GetAbilityAttackData()
    {
        return MakeAttackData(GetAbilityDamage());
    }

    // Gốc: companion gây damage thẳng qua Character.ewh/ewg (không roll chí mạng) → NonCrit.
    protected AttackData MakeAttackData(double damage)
    {
        return new AttackData(this, AttackType.BasicAttack, CritType.NonCrit, damage);
    }

    // ===== Helper dùng chung cho các lớp con (mục tiêu / sát thương / hồi máu) =====

    // Gây sát thương kỹ năng lên 1 enemy (bỏ qua nếu đã chết/biến mất).
    protected void DealDamage(BaseUnit victim, double damage)
    {
        if (victim != null && victim.isTargetable)
        {
            victim.TakeAttack(MakeAttackData(damage));
        }
    }

    // Hồi máu cho chủ (kẹp ở maxHp trong BaseUnit.GetHeal).
    protected void HealOwner(double heal)
    {
        if (owner != null && owner.isTargetable)
        {
            owner.GetHeal(this, heal);
        }
    }

    // Enemy còn sống trong tầm giao chiến quanh hero (bản sao, an toàn khi duyệt + gây damage).
    protected List<BaseUnit> GetEnemiesAroundOwner()
    {
        Vector3 center = owner != null ? owner.Transform.position : Transform.position;
        List<BaseUnit> result = new List<BaseUnit>();
        List<BaseUnit> enemies = GetAliveEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            BaseUnit e = enemies[i];
            if (e != null && e.isTargetable && VectorUtils.IsInRange(center, e.Transform.position, engageRange))
            {
                result.Add(e);
            }
        }
        return result;
    }

    // Enemy còn sống trong bán kính quanh 1 điểm (gốc Companion.gjo).
    protected List<BaseUnit> GetEnemiesInRadius(Vector3 center, float radius)
    {
        List<BaseUnit> result = new List<BaseUnit>();
        List<BaseUnit> enemies = GetAliveEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            BaseUnit e = enemies[i];
            if (e != null && e.isTargetable && VectorUtils.IsInRange(center, e.Transform.position, radius))
            {
                result.Add(e);
            }
        }
        return result;
    }

    // Enemy XA hero nhất (gốc Companion.gjn — Siphon).
    protected BaseUnit GetFarthestEnemy()
    {
        Vector3 center = owner != null ? owner.Transform.position : Transform.position;
        List<BaseUnit> enemies = GetEnemiesAroundOwner();
        BaseUnit farthest = null;
        float farthestSqr = -1f;
        for (int i = 0; i < enemies.Count; i++)
        {
            float sqr = VectorUtils.SqrDistance(center, enemies[i].Transform.position);
            if (sqr > farthestSqr)
            {
                farthestSqr = sqr;
                farthest = enemies[i];
            }
        }
        return farthest;
    }

    // Vị trí enemy có NHIỀU enemy khác trong 'radius' nhất (gốc lw.gpp — Bomber/AoeSlower).
    protected Vector3 FindDensestEnemyPoint(float radius, Vector3 fallback)
    {
        List<BaseUnit> enemies = GetEnemiesAroundOwner();
        int bestCount = -1;
        Vector3 best = fallback;
        for (int i = 0; i < enemies.Count; i++)
        {
            Vector3 p = enemies[i].Transform.position;
            int count = 0;
            for (int j = 0; j < enemies.Count; j++)
            {
                if (i != j && VectorUtils.IsInRange(p, enemies[j].Transform.position, radius))
                {
                    count++;
                }
            }

            if (count > bestCount)
            {
                bestCount = count;
                best = p;
            }
        }
        return best;
    }

    // Bắn 1 viên CompanionBullet đuổi theo 'victim' từ firePoint; tới nơi gọi onArrive(victim).
    protected CompanionBullet FireHoming(BaseBullet prefab, BaseUnit victim, float speed, Action<BaseUnit> onArrive)
    {
        return FireHomingFrom(prefab, firePoint.position, victim, speed, onArrive);
    }

    protected CompanionBullet FireHomingFrom(BaseBullet prefab, Vector3 start, BaseUnit victim, float speed, Action<BaseUnit> onArrive)
    {
        CompanionBullet bullet = SpawnCompanionBullet(prefab, speed);
        if (bullet == null)
        {
            // Thiếu prefab đạn → tác động ngay để kỹ năng vẫn chạy.
            onArrive?.Invoke(victim);
            return null;
        }

        bullet.ActiveHoming(start, this, victim, hit =>
        {
            PlayProjectileHitSfx();
            onArrive?.Invoke(hit);
        });
        return bullet;
    }

    protected CompanionBullet SpawnCompanionBullet(BaseBullet prefab, float speed)
    {
        if (prefab == null)
        {
            return null;
        }

        CompanionBullet bullet = PoolingController.Instance.GetBullet(prefab) as CompanionBullet;
        if (bullet != null && speed > 0.01f)
        {
            bullet.speed = speed;
        }
        return bullet;
    }

    // ===== Âm thanh (CompanionData.*Sound gốc, số liệu ở AudioConfig.Companions) =====

    private int effectSfxToken;

    private CompanionAudio CompanionSfx
    {
        get
        {
            AudioConfig config = AudioManager.Instance.Config;
            return config != null && companionData != null ? config.GetCompanion(companionData.assetName) : null;
        }
    }

    // Companion.hqd gốc: tung chiêu kêu ShootSound.
    protected void PlayShootSfx()
    {
        CompanionAudio audio = CompanionSfx;
        if (audio != null)
        {
            AudioManager.Instance.PlaySfx(audio.ShootSound, audio.ShootVolume);
        }
    }

    // CompanionProjectile gốc: đạn pet tới nơi kêu ProjectileHitSound.
    protected void PlayProjectileHitSfx()
    {
        CompanionAudio audio = CompanionSfx;
        if (audio != null)
        {
            AudioManager.Instance.PlaySfx(audio.ProjectileHitSound, audio.ProjectileHitVolume);
        }
    }

    // Companion.hrj gốc: bom/thiên thạch nổ kêu ProjectileImpactSound.
    protected void PlayImpactSfx()
    {
        CompanionAudio audio = CompanionSfx;
        if (audio != null)
        {
            AudioManager.Instance.PlaySfx(audio.ProjectileImpactSound, audio.ProjectileImpactVolume);
        }
    }

    // Companion.hri gốc: hiệu ứng bắt đầu → AudioSource riêng của pet phát EffectActiveSound
    // (lặp nếu LoopEffectActiveSound), volume = EffectActiveSoundVolume × Sound FX.
    // stopAfter > 0: tự tắt sau ngần ấy giây trận (hiệu ứng có thời hạn: Guardian/Immortal).
    protected void PlayEffectActiveSfx(float stopAfter = 0f)
    {
        CompanionAudio audio = CompanionSfx;
        if (audio == null || audio.EffectActiveSound == null || audioSource == null)
        {
            return;
        }

        audioSource.clip = audio.EffectActiveSound;
        audioSource.volume = audio.EffectActiveSoundVolume * AudioManager.Instance.SfxVolume;
        audioSource.loop = audio.LoopEffectActiveSound;
        audioSource.Play();

        int token = ++effectSfxToken;
        if (stopAfter > 0f)
        {
            StartCoroutine(RoutineStopEffectSfx(stopAfter, token));
        }
    }

    // Companion.hrk gốc.
    protected void StopEffectActiveSfx()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator RoutineStopEffectSfx(float seconds, int token)
    {
        yield return WaitBattleTime(seconds);
        if (token == effectSfxToken)
        {
            StopEffectActiveSfx();
        }
    }

    // Companion.hrl gốc: kéo slider Sound FX → cập nhật volume tiếng hiệu ứng đang chạy của mọi pet.
    public static void RefreshEffectSfxVolume(float sfxVolume)
    {
        for (int i = 0; i < activePets.Count; i++)
        {
            CompanionAudio audio = activePets[i].CompanionSfx;
            if (audio != null && activePets[i].audioSource != null)
            {
                activePets[i].audioSource.volume = audio.EffectActiveSoundVolume * sfxVolume;
            }
        }
    }

    // Chờ theo thời gian TRẬN (dừng khi pause, nhân gameSpeed) — dùng trong coroutine kỹ năng.
    protected IEnumerator WaitBattleTime(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (!isPause)
            {
                t += Time.deltaTime * GameController.Instance.gameSpeed;
            }
            yield return null;
        }
    }

    // Pet dùng vũ khí gán sẵn trên prefab cho basic attack (melee/ranged). Trống → companion tự
    // quyết cách ra đòn qua ReleaseAbility().
    protected override WeaponData GetCombatWeapon()
    {
        return weaponData;
    }

    // ===== Ra đòn =====

    // Điểm ra đòn mỗi nhịp (OnAttackEnd của BaseUnit). Chặn khi mất mục tiêu / ngoài tầm rồi
    // uỷ quyền cho ReleaseAbility() — lớp con định nghĩa hiệu ứng thật. Chỉ tính lượt cooldown khi
    // ra đòn thật (mất mục tiêu giữa lúc vung → giữ nguyên trạng thái sẵn sàng + lệnh bấm).
    protected override void ReleaseAttack()
    {
        if (!IsTargetAvailable() || !IsTargetInAttackRange())
        {
            return;
        }

        ReleaseAbility();
        StartCooldown();
    }

    // Kỹ năng MẶC ĐỊNH mọi companion: bắn 1 viên đạn ĐUỔI THEO target (bay thẳng tới vị trí hiện
    // tại của target mỗi frame), trúng gây sát thương companion. Tốc độ đạn = CompanionData.projectileSpeed.
    // Thiếu prefab đạn → cận chiến tạm. Companion có kiểu khác (Heal/Lightning/Bomber…) override hàm này.
    protected virtual void ReleaseAbility()
    {
        if (target == null)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            target.TakeAttack(GetAbilityAttackData(), impactAttack);
            PlaySfxAttack();
            return;
        }

        BaseBullet bullet = PoolingController.Instance.GetBullet(projectilePrefab);
        if (bullet != null)
        {
            if (companionData != null && companionData.projectileSpeed > 0.01f)
            {
                bullet.speed = companionData.projectileSpeed;
            }

            bullet.ActiveHoming(firePoint, this, target, GetAbilityAttackData());
        }

        PlayShootSfx();
    }

    // ===== Targeting / di chuyển theo hero (giữ nguyên spec) =====

    // Companion hỗ trợ (Heal/ChainHeal/Guardian/HealNova/Clone/Immortal) nhắm CHỦ thay vì enemy:
    // hồi chiêu xong là ra đòn, không cần enemy (đúng các strategy gốc nhắm Companion+0x40).
    protected virtual bool TargetsOwner => false;

    // Chọn enemy gần nhất trong engageRange tính từ vị trí HERO (gốc Companion.gjm) — hoặc chính
    // hero với companion hỗ trợ.
    protected override void FindNearestTarget()
    {
        if (TargetsOwner)
        {
            target = owner != null && owner.isTargetable ? owner : null;
            return;
        }

        Vector3 center = owner != null ? owner.Transform.position : Transform.position;
        target = FindNearestEnemyFrom(center, GetAliveEnemies(), engageRange);
    }

    // Pet không đi tới mục tiêu: kỹ năng sẵn sàng + có mục tiêu là ra đòn tại chỗ; di chuyển do TickFollow lo.
    protected override void UpdateIdle()
    {
        FindNextTarget();
        if (target != null && CanReleaseAbility)
        {
            ChangeState(BattleState.Attack);
        }
    }

    protected override void UpdateMove()
    {
        ChangeState(BattleState.Idle);
    }

    protected override void BeginIdle()
    {
        animationController.PlayAnimIdle();
    }

    // ===== Đi theo hero — Companion.Update / giu / giv / gip gốc =====

    protected override UnitMover.Settings MoverSettings => UnitMover.CompanionSettings;

    protected override void OnEnable()
    {
        base.OnEnable();
        activePets.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        activePets.Remove(this);
        StopEffectActiveSfx();   // Companion.OnDespawn gốc
    }

    private void TickFollow(float dt)
    {
        BaseMode mode = GameController.Instance.mode;
        if (mode != null && mode.isSkipUpdateBehaviour)
        {
            return;
        }

        if (castFreezeRemain > 0f)
        {
            castFreezeRemain -= dt;
            mover.ClearPath();
            rigid.linearVelocity = Vector2.zero;
            return;
        }

        bool wasMoving = mover.HasPath;
        if (owner != null && owner.isTargetable)
        {
            float distance = Vector2.Distance(Transform.position, owner.Transform.position);
            if (distance > followDistance)
            {
                if (!isFollowing)
                {
                    isFollowing = true;
                    mover.ClearPath();
                    followRepathTimer = FOLLOW_REPATH_INTERVAL;
                }

                followRepathTimer += dt;
                if (followRepathTimer >= FOLLOW_REPATH_INTERVAL || !mover.HasPath)
                {
                    followRepathTimer = 0f;
                    MapController map = MapController.Instance;
                    mover.RequestMoveTo(map.ClampCell(map.WorldToCell(owner.Transform.position)));
                }
            }
            else if (isFollowing)
            {
                isFollowing = false;
                mover.ClearPath();
            }
        }

        isMoving = mover.HasPath;
        if (isMoving != wasMoving && !isAttacking)
        {
            if (isMoving) animationController.PlayAnimMove();
            else animationController.PlayAnimIdle();
        }
    }

    // gip: đẩy các pet tách nhau — lực 15 × (1 − d/1.2) khi gần hơn 1.2; rồi lái theo path (đứng yên khi đang ra chiêu).
    protected override void TickMover(float dt)
    {
        if (castFreezeRemain > 0f)
        {
            return;
        }

        Vector2 position = rigid.position;
        for (int i = 0; i < activePets.Count; i++)
        {
            PetUnit other = activePets[i];
            if (other == this || other == null)
            {
                continue;
            }

            Vector2 away = position - (Vector2)other.Transform.position;
            float sqr = away.sqrMagnitude;
            if (sqr >= SEPARATION_RADIUS * SEPARATION_RADIUS || sqr < SEPARATION_MIN_SQR)
            {
                continue;
            }

            float distance = Mathf.Sqrt(sqr);
            rigid.AddForce(away / distance * (SEPARATION_FORCE * (1f - distance / SEPARATION_RADIUS)));
        }

        base.TickMover(dt);
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
