// Companion AoeSlower (type 9) — Glacier Fist: "Fires an exploding snowball that deals <Value> Area Damage and slows targets by 50%."
// Logic GỐC (reverse strategy `kd`) = cùng khung Bomber nhưng bộ tham số AoeSlow*: ném cầu tuyết
// (AoeSlowProjectileSpeed, AoeSlowArcHeight) vào cụm enemy đông nhất trong AoeSlowRadius; nổ (FX sống
// AoeSlowExplosionDuration) → damage chuẩn + CompanionSlowEffect(SlowDuration, SlowAmount).
public class PetCompanionAoeSlow : PetCompanionBomber
{
    protected override float Radius => companionData.aoeSlowRadius;
    protected override float TravelSpeed => companionData.aoeSlowProjectileSpeed;
    protected override float ArcHeight => companionData.aoeSlowArcHeight;
    protected override float ExplosionDuration => companionData.aoeSlowExplosionDuration;

    protected override void OnExplosionHit(BaseUnit hit, double damage)
    {
        DealDamage(hit, damage);
        CompanionSlowEffect.Apply(hit, companionData.slowDuration, companionData.slowAmount);
    }
}
