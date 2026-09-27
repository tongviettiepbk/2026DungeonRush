using System;
using UnityEngine;

// Đạn của companion (bám CompanionProjectile gốc): KHÔNG tự gây sát thương — tới đích thì gọi
// callback để lớp pet quyết định (damage / hồi máu / slow / nổ...). Lấy/trả qua PoolingController
// như BaseBullet. Các kiểu bay:
//   • Homing : đuổi theo 1 BaseUnit (mục tiêu chết giữa đường → bay nốt rồi tắt, KHÔNG gọi callback).
//   • Arc    : bay cong từ A tới B trong 'duration' (bom Bomber/AoeSlower): y += 4·h·t·(1−t).
//   • Point  : bay thẳng tới 1 điểm với 'speed' (thiên thạch Meteor rơi từ trên xuống).
//   • Sine   : homing + lắc ngang hình sin (Siphon: SiphonSineAmplitude/Frequency).
public class CompanionBullet : BaseBullet
{
    private enum FlyMode { None, Homing, Arc, Point }

    private FlyMode mode;
    private Action<BaseUnit> onArriveUnit;
    private Action<Vector3> onArrivePoint;

    private Vector3 startPoint;
    private Vector3 endPoint;
    private float arcHeight;
    private float duration;
    private float elapsed;

    private float sineAmplitude;
    private float sineFrequency;
    private Vector3 basePosition;   // vị trí "tâm" khi bay sin (chưa cộng lệch ngang)

    public void ActiveHoming(Vector3 start, BaseUnit attacker, BaseUnit target, Action<BaseUnit> onArrive)
    {
        ActiveSine(start, attacker, target, 0f, 0f, onArrive);
    }

    public void ActiveSine(Vector3 start, BaseUnit attacker, BaseUnit target, float amplitude, float frequency, Action<BaseUnit> onArrive)
    {
        if (target == null)
        {
            Deactive();
            return;
        }

        Begin(attacker, start);
        this.target = target;
        mode = FlyMode.Homing;
        onArriveUnit = onArrive;
        sineAmplitude = amplitude;
        sineFrequency = frequency;
        endPoint = target.centerBodyPoint.position;
    }

    public void ActiveArc(Vector3 start, Vector3 end, float flyDuration, float height, BaseUnit attacker, Action<Vector3> onArrive)
    {
        Begin(attacker, start);
        mode = FlyMode.Arc;
        startPoint = start;
        endPoint = end;
        duration = Mathf.Max(0.01f, flyDuration);
        arcHeight = height;
        onArrivePoint = onArrive;
    }

    public void ActivePoint(Vector3 start, Vector3 end, BaseUnit attacker, Action<Vector3> onArrive)
    {
        Begin(attacker, start);
        mode = FlyMode.Point;
        endPoint = end;
        onArrivePoint = onArrive;
    }

    private void Begin(BaseUnit attacker, Vector3 start)
    {
        this.attacker = attacker;
        target = null;
        attackData = null;
        onArriveUnit = null;
        onArrivePoint = null;
        elapsed = 0f;
        sineAmplitude = 0f;

        Transform.SetParent(null);
        Transform.position = start;
        basePosition = start;
        isActive = true;
        gameObject.SetActive(true);
    }

    protected override void Update()
    {
        if (!isActive || mode == FlyMode.None)
        {
            return;
        }

        if (attacker != null && attacker.isPause)
        {
            return;
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        elapsed += dt;

        switch (mode)
        {
            case FlyMode.Homing: UpdateHoming(dt); break;
            case FlyMode.Arc: UpdateArc(); break;
            case FlyMode.Point: UpdatePoint(dt); break;
        }
    }

    private void UpdateHoming(float dt)
    {
        bool isTargetAlive = target != null && target.isTargetable;
        if (isTargetAlive)
        {
            endPoint = target.centerBodyPoint.position;
        }

        basePosition = Vector3.MoveTowards(basePosition, endPoint, speed * dt);

        // Lệch ngang hình sin (vuông góc hướng bay), tắt dần khi tới đích để chạm đúng mục tiêu.
        Vector3 offset = Vector3.zero;
        if (sineAmplitude > 0f)
        {
            Vector3 dir = endPoint - basePosition;
            Vector3 side = new Vector3(-dir.y, dir.x, 0f).normalized;
            float fade = Mathf.Clamp01(dir.magnitude);
            offset = side * (Mathf.Sin(elapsed * sineFrequency * Mathf.PI * 2f) * sineAmplitude * fade);
        }

        Vector3 next = basePosition + offset;
        Transform.FaceUpAxisToPoint(endPoint);
        Transform.position = next;

        if (VectorUtils.IsInRange(basePosition, endPoint, 0.05f))
        {
            Action<BaseUnit> callback = onArriveUnit;
            BaseUnit arrived = target;
            Deactive();
            if (isTargetAlive)
            {
                callback?.Invoke(arrived);
            }
        }
    }

    private void UpdateArc()
    {
        float t = Mathf.Clamp01(elapsed / duration);
        Vector3 p = Vector3.Lerp(startPoint, endPoint, t);
        p.y += 4f * arcHeight * t * (1f - t);
        Transform.FaceUpAxisToPoint(p);
        Transform.position = p;

        if (t >= 1f)
        {
            Arrive();
        }
    }

    private void UpdatePoint(float dt)
    {
        Transform.position = Vector3.MoveTowards(Transform.position, endPoint, speed * dt);
        Transform.FaceUpAxisToPoint(endPoint);

        if (VectorUtils.IsInRange(Transform.position, endPoint, 0.05f))
        {
            Arrive();
        }
    }

    private void Arrive()
    {
        Action<Vector3> callback = onArrivePoint;
        Vector3 point = endPoint;
        Deactive();
        callback?.Invoke(point);
    }

    // Không dùng luồng gây damage của BaseBullet.
    protected override void OnTargetTakeDamage()
    {
        Deactive();
    }

    public override void Deactive()
    {
        mode = FlyMode.None;
        base.Deactive();
    }
}
