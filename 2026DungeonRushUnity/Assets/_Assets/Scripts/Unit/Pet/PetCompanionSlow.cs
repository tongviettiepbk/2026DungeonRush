// Companion Slower (type 2) — Snow Fang: "Fires an ice ball that deals <Value> Damage and slows the target by <Value>%."
// Logic GỐC (reverse strategy `lo`): bắn đạn vào enemy GẦN HERO NHẤT (Companion.gjm); trúng →
// damage = (DamageBase + DamageScaler × level) × bonus, rồi CompanionSlowEffect(SlowDuration, SlowAmount)
// (nhân tốc chạy + tốc đánh với SlowAmount; trúng lại chỉ làm mới thời gian).
public class PetCompanionSlow : PetUnit
{
    protected override void ReleaseAbility()
    {
        double damage = GetAbilityDamage();
        FireHoming(projectilePrefab, target, companionData.projectileSpeed, hit => HitAndSlow(hit, damage));
        PlayShootSfx();
    }

    protected void HitAndSlow(BaseUnit hit, double damage)
    {
        DealDamage(hit, damage);
        CompanionSlowEffect.Apply(hit, companionData.slowDuration, companionData.slowAmount);
    }
}
