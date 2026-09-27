// Đốt cháy (gốc CompanionBurnEffect): mỗi 0.5s gây BurnTickDamage CỐ ĐỊNH (không nhân level/bonus)
// trong BurnDuration. Trúng lại chỉ làm mới thời gian (gốc gkw: elapsed = 0).
public class CompanionBurnEffect : CompanionUnitEffect
{
    private const float TICK_INTERVAL = 0.5f;

    private BaseUnit attacker;
    private double tickDamage;
    private float tickTimer;

    public static void Apply(BaseUnit target, BaseUnit attacker, float duration, double tickDamage)
    {
        CompanionBurnEffect effect = Apply<CompanionBurnEffect>(target, duration);
        if (effect != null)
        {
            effect.attacker = attacker;
            effect.tickDamage = tickDamage;
        }
    }

    protected override void OnBegin()
    {
        tickTimer = 0f;
    }

    protected override void OnTick(float dt)
    {
        tickTimer += dt;
        if (tickTimer < TICK_INTERVAL)
        {
            return;
        }

        tickTimer -= TICK_INTERVAL;
        if (tickDamage > 0f)
        {
            unit.TakeDamage(new TakeDamageData(attacker, AttackType.SkillEquipped, tickDamage));
        }
    }

    protected override void OnEnd() { }
}
