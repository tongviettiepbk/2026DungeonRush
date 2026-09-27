// Bất tử (gốc CompanionImmortalityEffect.gnl): bật Character.IsImmortal (HP không xuống dưới 1)
// trong thời gian cho trước rồi tắt.
public class CompanionImmortalEffect : CompanionUnitEffect
{
    public static void Apply(BaseUnit target, float duration)
    {
        Apply<CompanionImmortalEffect>(target, duration);
    }

    protected override void OnBegin()
    {
        unit.isImmortal = true;
    }

    protected override void OnEnd()
    {
        if (unit != null)
        {
            unit.isImmortal = false;
        }
    }
}
