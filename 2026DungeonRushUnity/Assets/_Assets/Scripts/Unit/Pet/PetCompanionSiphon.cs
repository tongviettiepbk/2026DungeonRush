// Companion Siphon (type 12) — Phantom Gaze: "Fires a spectral projectile that deals <Value> Damage and returns to restore <Value> Health."
// Logic GỐC (reverse strategy `lm`):
//   • Mục tiêu = enemy XA HERO NHẤT (Companion.gjn).
//   • damage = (DamageBase + DamageScaler×lv)×bonus ; heal = (HealBase + HealScaler×lv)×bonus (chốt lúc bắn).
//   • Đạn lượn hình sin (SiphonProjectileSpeed/SineAmplitude/SineFrequency) tới mục tiêu → damage →
//     bay NGƯỢC về hero → hồi máu (kẹp maxHp).
public class PetCompanionSiphon : PetUnit
{
    protected override void ReleaseAbility()
    {
        BaseUnit victim = GetFarthestEnemy() ?? target;
        double damage = GetAbilityDamage();
        double heal = GetAbilityHeal();
        float speed = companionData.siphonProjectileSpeed;

        CompanionBullet bullet = SpawnCompanionBullet(projectilePrefab, speed);
        if (bullet == null)
        {
            DealDamage(victim, damage);
            HealOwner(heal);
            return;
        }

        bullet.ActiveSine(firePoint.position, this, victim, companionData.siphonSineAmplitude, companionData.siphonSineFrequency,
            hit =>
            {
                UnityEngine.Vector3 from = hit.centerBodyPoint.position;
                DealDamage(hit, damage);
                FireHomingFrom(projectilePrefab, from, owner, speed, _ => HealOwner(heal));
            });
        PlayShootSfx();
    }
}
