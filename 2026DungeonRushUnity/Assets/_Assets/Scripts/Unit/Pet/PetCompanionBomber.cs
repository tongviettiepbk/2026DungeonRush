using System.Collections.Generic;
using UnityEngine;

// Companion Bomber (type 5) — Flame Wing / Blaze Tail: "Triggers an explosion that deals <Value> Area Damage [and burns targets]."
// Logic GỐC (reverse strategy `kk`):
//   • Điểm ném = vị trí enemy có NHIỀU enemy khác trong BombRadius nhất (lw.gpp).
//   • Bom bay cong: thời gian = max(khoảng cách / BombProjectileSpeed, 0.25), cao BombArcHeight.
//   • Nổ: FX BombExplosionPrefab phóng theo BombRadius, sống BombExplosionDuration; mọi enemy trong
//     BombRadius nhận damage = (DamageBase + DamageScaler×lv)×bonus; BurnEnabled → CompanionBurnEffect
//     (mỗi 0.5s BurnTickDamage cố định, trong BurnDuration).
// AoeSlower (Glacier Fist) kế thừa, đổi bộ tham số + hiệu ứng trúng (slow thay vì cháy).
public class PetCompanionBomber : PetUnit
{
    [Tooltip("FX vụ nổ (BombExplosionPrefab / AoeSlowExplosionPrefab gốc).")]
    [SerializeField] protected GameObject explosionFx;

    private const float MIN_TRAVEL_DURATION = 0.25f;

    protected virtual float Radius => companionData.bombRadius;
    protected virtual float TravelSpeed => companionData.bombProjectileSpeed;
    protected virtual float ArcHeight => companionData.bombArcHeight;
    protected virtual float ExplosionDuration => companionData.bombExplosionDuration;

    protected override void ReleaseAbility()
    {
        Vector3 point = FindDensestEnemyPoint(Radius, target.Transform.position);
        Vector3 start = firePoint.position;
        float travel = Mathf.Max(Vector3.Distance(start, point) / Mathf.Max(0.01f, TravelSpeed), MIN_TRAVEL_DURATION);

        CompanionBullet bomb = SpawnCompanionBullet(projectilePrefab, 0f);
        if (bomb != null)
        {
            bomb.ActiveArc(start, point, travel, ArcHeight, this, Explode);
        }
        else
        {
            Explode(point);
        }
        PlayShootSfx();
    }

    protected void Explode(Vector3 point)
    {
        CompanionFx.Spawn(explosionFx, point, Radius, ExplosionDuration);

        double damage = GetAbilityDamage();
        List<BaseUnit> hits = GetEnemiesInRadius(point, Radius);
        for (int i = 0; i < hits.Count; i++)
        {
            OnExplosionHit(hits[i], damage);
        }
    }

    protected virtual void OnExplosionHit(BaseUnit hit, double damage)
    {
        DealDamage(hit, damage);
        if (companionData.burnEnabled)
        {
            CompanionBurnEffect.Apply(hit, this, companionData.burnDuration, companionData.burnTickDamage);
        }
    }
}
