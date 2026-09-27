using UnityEngine;

// Nền cho hiệu ứng companion GẮN LÊN 1 unit (bám CompanionSlowEffect/BurnEffect/... gốc): tự thêm
// component vào unit, đếm thời gian theo thời gian TRẬN (dừng khi pause, nhân gameSpeed), hết giờ
// hoặc unit chết thì gỡ. Trúng lại cùng loại → Refresh (gốc chỉ làm mới thời gian, không cộng dồn).
public abstract class CompanionUnitEffect : MonoBehaviour
{
    protected BaseUnit unit;
    protected float duration;
    protected float elapsed;
    private bool isRunning;

    // Gắn (hoặc làm mới) hiệu ứng loại T lên unit. Trả về component để lớp con nạp tham số.
    public static T Apply<T>(BaseUnit target, float duration) where T : CompanionUnitEffect
    {
        if (target == null || !target.isTargetable)
        {
            return null;
        }

        T effect = target.GetComponent<T>();
        if (effect == null)
        {
            effect = target.gameObject.AddComponent<T>();
        }

        effect.Begin(target, duration);
        return effect;
    }

    private void Begin(BaseUnit target, float newDuration)
    {
        duration = newDuration;
        elapsed = 0f;

        if (!isRunning)
        {
            unit = target;
            isRunning = true;
            OnBegin();
        }
        else
        {
            OnRefresh();
        }
    }

    protected virtual void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (unit == null || !unit.isTargetable)
        {
            End();
            return;
        }

        if (unit.isPause)
        {
            return;
        }

        float dt = Time.deltaTime * GameController.Instance.gameSpeed;
        elapsed += dt;
        OnTick(dt);

        if (elapsed >= duration)
        {
            End();
        }
    }

    private void End()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        OnEnd();
        Destroy(this);
    }

    protected virtual void OnDisable()
    {
        End();
    }

    protected abstract void OnBegin();
    protected abstract void OnEnd();
    protected virtual void OnRefresh() { }
    protected virtual void OnTick(float dt) { }
}
