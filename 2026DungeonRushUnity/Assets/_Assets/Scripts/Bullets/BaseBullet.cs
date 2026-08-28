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
            ActiveParabol(firePoint, attacker, target);
        }
        else if (movingType == BulletMovingType.Straight)
        {
            ActiveStraight(firePoint, attacker, target);
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
            ActiveStraight(firePoint, attacker, target);
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
