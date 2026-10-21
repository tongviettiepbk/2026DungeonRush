using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Kiểu bay của đạn: đi thẳng hoặc bay theo đường parabol.
public enum BulletMovingType
{
    Straight,
    Parabol,
}

// Hệ đạn/projectile — port cấu trúc base từ StickIdle (Bullets/BaseBullet.cs).
// Đạn được lấy/trả qua PoolingController (GetBullet/StoreBullet). Mỗi lần bắn:
// hero/companion override ReleaseBullet() -> GetBullet(prefab) -> bullet.Active(firePoint, attacker, target).
// Khi trúng đích: spawn Fx va chạm (attacker.impactAttack) rồi gọi target.TakeAttack.
public class BaseBullet : MonoBehaviour
{
    public bool isPooling;
    public BulletMovingType movingType = BulletMovingType.Parabol;
    public Transform Transform;
    public float height = 0.5f;
    public float speed = 5f;

    public int poolingId { get; set; }
    public float durationPerDistance => 1f / (speed * GameController.Instance.gameSpeed);

    protected bool isActive;
    protected BaseUnit attacker;
    protected BaseUnit target;
    protected AttackData attackData;

    // ProjectileContext.HitAudios gốc: tiếng kêu khi đạn trúng (đạn hero = WeaponData.ProjectileHitAudios).
    protected List<AudioClip> hitAudios;
    protected float hitAudioVolume = 1f;

    public void SetHitAudios(List<AudioClip> clips, float volume)
    {
        hitAudios = clips;
        hitAudioVolume = volume;
    }

    protected virtual void Awake()
    {
        EventDispatcher.Instance.RegisterListener(EventID.ResetMode, OnResetMode);
    }

    // Kích hoạt đạn bắn tới một BaseUnit mục tiêu.
    public void Active(Transform firePoint, BaseUnit attacker, BaseUnit target, AttackData attackData = null)
    {
        if (target == null)
        {
            isActive = false;
            gameObject.SetActive(false);
            return;
        }

        if (movingType == BulletMovingType.Parabol)
        {
            ActiveParabol(firePoint, attacker, target, attackData);
        }
        else if (movingType == BulletMovingType.Straight)
        {
            ActiveStraight(firePoint, attacker, target, attackData);
        }

        isActive = true;
        Transform.FaceUpAxisToPoint(target.centerBodyPoint.position);
    }

    // Đạn "giả" bay tới một điểm cố định (không có target thật) — chỉ hiển thị Fx khi tới nơi.
    public void ActiveFake(Transform firePoint, BaseUnit attacker, Vector3 target, AttackData attackData = null)
    {
        DebugCustom.ShowLog("active fake");

        bool isStraight = movingType == BulletMovingType.Straight;
        if (movingType == BulletMovingType.Parabol)
        {
            Vector3 endPoint = target;
            float distanceX = Mathf.Abs(attacker.firePoint.position.x - target.x);
            if (distanceX > 1f)
            {
                Transform.SetParent(firePoint);
                Transform.localEulerAngles = Vector3.zero;
                Transform.localPosition = Vector3.zero;
                Transform.SetParent(null);

                Vector3 beginPoint = firePoint.position;
                float distance = Vector3.Distance(endPoint, beginPoint);
                float durationMove = distance * durationPerDistance;

                DOTween.To(setter: value =>
                {
                    Vector3 newPosition = MathUtils.Parabola(beginPoint, endPoint, height, value);
                    transform.FaceUpAxisToPoint(newPosition);
                    transform.position = newPosition;

                }, startValue: 0, endValue: 1, durationMove).SetEase(Ease.Linear).
                OnComplete(() =>
                {
                    if (gameObject.activeInHierarchy)
                    {
                        FxController.Instance.SpawnFx(attacker.impactAttack, transform.position);
                        DebugCustom.ShowLog("SpawnFx active fake 1");
                        Deactive();
                    }
                }).
                OnUpdate(() =>
                {
                    gameObject.SetActive(true);

                    bool isCloseToTarget = VectorUtils.IsInRange(Transform.position, target, 0.1f);
                    if (isCloseToTarget)
                    {
                        if (gameObject.activeInHierarchy)
                        {
                            FxController.Instance.SpawnFx(attacker.impactAttack, transform.position);
                            DebugCustom.ShowLog("SpawnFx active fake 2");
                            Deactive();
                        }
                    }
                });

                gameObject.SetActive(true);
            }
            else
            {
                isStraight = true;
            }
        }

        if (isStraight)
        {
            Transform.SetParent(firePoint);
            Transform.localEulerAngles = Vector3.zero;
            Transform.localPosition = Vector3.zero;
            Transform.SetParent(null);

            Vector3 endPoint = target;
            float distance = Vector3.Distance(endPoint, attacker.firePoint.position);
            Transform.DOMove(endPoint, distance * durationPerDistance).OnComplete(() =>
            {
                if (gameObject.activeInHierarchy)
                {
                    FxController.Instance.SpawnFx(attacker.impactAttack, transform.position);

                    DebugCustom.ShowLog("SpawnFx active fake 3");
                    Deactive();
                }
            });

            gameObject.SetActive(true);
        }

        isActive = true;
        Transform.FaceUpAxisToPoint(target);
    }

    // Bay thẳng tới target: DOMove tuyến tính, tới nơi thì gây damage.
    private void ActiveStraight(Transform firePoint, BaseUnit attacker, BaseUnit target, AttackData attackData = null)
    {
        this.attacker = attacker;
        this.target = target;
        this.attackData = attackData;

        Transform.SetParent(firePoint);
        Transform.localEulerAngles = Vector3.zero;
        Transform.localPosition = Vector3.zero;
        Transform.SetParent(null);

        Vector3 endPoint = target.centerBodyPoint.position;
        float distance = Vector3.Distance(endPoint, attacker.firePoint.position);
        Transform.DOMove(endPoint, distance * durationPerDistance).OnComplete(() =>
        {
            DebugCustom.ShowLog("Bullet hit target ActiveStraight: " + target.name);

            OnTargetTakeDamage();
        });

        gameObject.SetActive(true);
    }

    // Bay theo parabol: nếu khoảng cách X đủ xa thì bay vòng cung (bù trừ target đang di chuyển),
    // ngược lại (quá gần) rơi về bay thẳng.
    private void ActiveParabol(Transform firePoint, BaseUnit attacker, BaseUnit target, AttackData attackData = null)
    {
        this.attacker = attacker;
        this.target = target;
        this.attackData = attackData;

        Vector3 endPoint = target.centerBodyPoint.position;
        float distanceX = Mathf.Abs(attacker.firePoint.position.x - target.Transform.position.x);
        if (distanceX > 1f)
        {
            Transform.SetParent(firePoint);
            Transform.localEulerAngles = Vector3.zero;
            Transform.localPosition = Vector3.zero;
            Transform.SetParent(null);

            Vector3 beginPoint = firePoint.position;
            float distance = Vector3.Distance(endPoint, beginPoint);
            float durationMove = distance * durationPerDistance;
            endPoint.x -= (durationMove * (target.stats.moveSpeed * GameController.Instance.gameSpeed));

            DOTween.To(setter: value =>
            {
                if (attacker != null)
                {
                    if (attacker.isPause == false)
                    {
                        Vector3 newPosition = MathUtils.Parabola(beginPoint, endPoint, height, value);
                        transform.FaceUpAxisToPoint(newPosition);
                        transform.position = newPosition;
                    }
                }
                else
                {
                    DebugCustom.ShowLog("attacker is null");
                    Deactive();
                }

            }, startValue: 0, endValue: 1, durationMove).SetEase(Ease.Linear).

            OnComplete(() =>
            {
                OnTargetTakeDamage();
            }).
            OnUpdate(() =>
            {
                gameObject.SetActive(true);

                bool isCloseToTarget = VectorUtils.IsInRange(Transform.position, target.centerBodyPoint.position, 0.1f);
                if (isCloseToTarget)
                {
                    OnTargetTakeDamage();
                }
            });

            gameObject.SetActive(true);
        }
        else
        {
            ActiveStraight(firePoint, attacker, target, attackData);
        }
    }

    // ===== Bay thẳng theo gốc (ProjectileArrow/ProjectileBullet.gvb + Update, reverse v41) =====
    // Hướng chốt LÚC BẮN về vị trí gốc của target, tốc độ không đổi, không đuổi → target đi chỗ khác thì TRƯỢT.
    // Chỉ trúng đúng target đã nhắm (chạm collider thân). Target chết giữa đường → bay tới điểm nhắm rồi biến
    // (không damage); người bắn chết → đạn biến ngay; sống tối đa 4s.

    private const float LINEAR_LIFETIME = 4f;          // wlt gốc
    private const float LINEAR_ARRIVE_SQR = 0.09f;     // gốc: tới điểm nhắm (0.3) khi target đã chết
    private const float LINEAR_MIN_RADIUS = 0.05f;

    private bool isLinear;
    private Vector3 linearVelocity;
    private Vector3 linearAimPoint;
    private float linearLifeTimer;
    private float linearRadius;

    public void ActiveLinear(Transform firePoint, BaseUnit attacker, BaseUnit target, AttackData attackData = null)
    {
        if (target == null)
        {
            isActive = false;
            gameObject.SetActive(false);
            return;
        }

        this.attacker = attacker;
        this.target = target;
        this.attackData = attackData;

        Transform.SetParent(null);
        Transform.position = firePoint.position;

        linearAimPoint = target.Transform.position;
        Vector3 direction = linearAimPoint - Transform.position;
        direction.z = 0f;
        linearVelocity = direction.sqrMagnitude > 1e-10f ? direction.normalized * speed : Vector3.zero;
        linearLifeTimer = 0f;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        Vector3 scale = Transform.lossyScale;
        linearRadius = circle != null
            ? Mathf.Max(circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), LINEAR_MIN_RADIUS)
            : LINEAR_MIN_RADIUS;

        isLinear = true;
        isActive = true;
        Transform.FaceUpAxisToPoint(linearAimPoint);
        gameObject.SetActive(true);
    }

    private void UpdateLinear()
    {
        if (attacker == null || attacker.isDead)
        {
            Deactive();
            return;
        }

        if (attacker.isPause)
        {
            return;
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        linearLifeTimer += dt;
        if (linearLifeTimer >= LINEAR_LIFETIME)
        {
            Deactive();
            return;
        }

        bool isTargetAlive = target != null && target.isTargetable;
        Vector3 move = linearVelocity * dt;

        if (!isTargetAlive)
        {
            Transform.position += move;
            Vector3 toAim = linearAimPoint - Transform.position;
            toAim.z = 0f;
            if (toAim.sqrMagnitude < LINEAR_ARRIVE_SQR || Vector3.Dot(linearVelocity, toAim) <= 0f)
            {
                Deactive();
            }
            return;
        }

        // Chia nhỏ bước theo bán kính đạn để đạn nhanh không xuyên qua collider (gốc dùng Continuous).
        Collider2D targetCollider = target.BodyCollider;
        int steps = Mathf.Max(1, Mathf.CeilToInt(move.magnitude / linearRadius));
        Vector3 subMove = move / steps;
        for (int i = 0; i < steps; i++)
        {
            Transform.position += subMove;
            if (targetCollider != null)
            {
                Vector2 position = Transform.position;
                Vector2 closest = targetCollider.ClosestPoint(position);
                if ((closest - position).sqrMagnitude <= linearRadius * linearRadius)
                {
                    isLinear = false;
                    OnTargetTakeDamage();
                    return;
                }
            }
        }
    }

    // ===== Homing: đuổi theo target, bay thẳng tới vị trí HIỆN TẠI của target mỗi frame =====
    // Target chết/biến mất giữa đường → bay nốt tới vị trí cuối rồi tắt (không gây damage).

    private bool isHoming;
    private Vector3 homingPoint;

    public void ActiveHoming(Transform firePoint, BaseUnit attacker, BaseUnit target, AttackData attackData = null)
    {
        if (target == null)
        {
            isActive = false;
            gameObject.SetActive(false);
            return;
        }

        this.attacker = attacker;
        this.target = target;
        this.attackData = attackData;

        Transform.SetParent(firePoint);
        Transform.localEulerAngles = Vector3.zero;
        Transform.localPosition = Vector3.zero;
        Transform.SetParent(null);

        homingPoint = target.centerBodyPoint.position;
        isHoming = true;
        isActive = true;
        Transform.FaceUpAxisToPoint(homingPoint);
        gameObject.SetActive(true);
    }

    protected virtual void Update()
    {
        if (isLinear && isActive)
        {
            UpdateLinear();
            return;
        }

        if (!isHoming || !isActive)
        {
            return;
        }

        if (attacker != null && attacker.isPause)
        {
            return;
        }

        bool isTargetAlive = target != null && target.isTargetable;
        if (isTargetAlive)
        {
            homingPoint = target.centerBodyPoint.position;
        }

        float step = speed * GameController.Instance.gameSpeed * Time.deltaTime;
        Transform.position = Vector3.MoveTowards(Transform.position, homingPoint, step);
        Transform.FaceUpAxisToPoint(homingPoint);

        if (VectorUtils.IsInRange(Transform.position, homingPoint, 0.05f))
        {
            isHoming = false;
            if (isTargetAlive)
            {
                OnTargetTakeDamage();
            }
            else
            {
                Deactive();
            }
        }
    }

    // Đạn chạm đích: spawn Fx va chạm và truyền sát thương cho target rồi thu hồi đạn.
    protected virtual void OnTargetTakeDamage()
    {
        DebugCustom.ShowLog("Bullet hit target: " + target.name);

        if (gameObject.activeInHierarchy)
        {
            if (isActive)
            {
                if (attacker != null && target != null)
                {
                    if (target.gameObject.activeInHierarchy)
                    {
                        FxController.Instance.SpawnFx(attacker.impactAttack, target.centerBodyPoint.position);
                        DebugCustom.ShowLog("SHOW FX OnTargetTakeDamage");
                    }

                    AudioManager.Instance.PlayRandomSfx(hitAudios, hitAudioVolume);

                    if (attackData != null)
                    {
                        target.TakeAttack(attackData);
                    }
                    else
                    {
                        DebugCustom.ShowLog("GetBasicAttackData" + attacker.GetBasicAttackData());
                        target.TakeAttack(attacker.GetBasicAttackData());
                    }
                }
            }

            Deactive();
        }
    }

    // Tắt đạn và trả về pool (nếu isPooling).
    public virtual void Deactive()
    {
        isActive = false;
        hitAudios = null;
        isHoming = false;
        isLinear = false;
        gameObject.SetActive(false);

        if (isPooling)
        {
            PoolingController.Instance.StoreBullet(this);
        }
    }

    // Reset mode: đảm bảo đạn đang bay bị thu hồi khi trận reset.
    protected virtual void OnResetMode(object obj)
    {
        try
        {
            if (gameObject.activeInHierarchy)
            {
                Deactive();
            }
        }
        catch { }
    }
}
