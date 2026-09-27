// Tăng tỉ lệ chặn đòn (gốc CompanionGuardianEffect.gnd/gne): cộng GuardianBlockChance (%) vào chủ
// trong GuardianActiveDuration rồi trừ lại.
public class CompanionBlockEffect : CompanionUnitEffect
{
    private float blockChance;
    private bool isApplied;

    public static void Apply(BaseUnit target, float duration, float blockChance)
    {
        CompanionBlockEffect effect = Apply<CompanionBlockEffect>(target, duration);
        if (effect != null && !effect.isApplied)
        {
            effect.blockChance = blockChance;
            effect.unit.bonusBlockChance += blockChance;
            effect.isApplied = true;
        }
    }

    protected override void OnBegin() { }

    protected override void OnEnd()
    {
        if (isApplied && unit != null)
        {
            unit.bonusBlockChance -= blockChance;
        }
        isApplied = false;
    }
}
