// Làm chậm (gốc CompanionSlowEffect.gpm): NHÂN tốc chạy + tốc đánh của mục tiêu với SlowAmount
// (0.5 = chậm 50%) trong SlowDuration; hết giờ trả lại. Trúng lại chỉ làm mới thời gian (không cộng dồn).
// Hệ số đặt qua BaseUnit.SetSlowMultiplier → áp trong ReloadStats nên không mất khi chỉ số tính lại.
public class CompanionSlowEffect : CompanionUnitEffect
{
    public static void Apply(BaseUnit target, float duration, float slowAmount)
    {
        CompanionSlowEffect effect = Apply<CompanionSlowEffect>(target, duration);
        if (effect != null && effect.unit.slowMultiplier == 1f)
        {
            effect.unit.SetSlowMultiplier(slowAmount);
        }
    }

    protected override void OnBegin() { }

    protected override void OnEnd()
    {
        if (unit != null && unit.slowMultiplier != 1f)
        {
            unit.SetSlowMultiplier(1f);
        }
    }
}
