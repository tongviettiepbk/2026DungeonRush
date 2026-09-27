using System.Collections.Generic;
using UnityEngine;

// Companion MultiSlower (type 6) — Frost Claw: "Fires an ice ball that splits dealing <Value> Area Damage and slows targets by <Value>%."
// Logic GỐC (reverse strategy `lh`): bắn quả cầu băng vào enemy gần hero nhất → damage + slow + FX va
// chạm (MultiSlowImpactPrefab) → TÁCH thành MultiSlowChainCount viên bay từ điểm trúng tới các enemy
// gần nhất CHƯA trúng (lh.ght), mỗi viên cũng damage + slow. Damage/slow cùng công thức Snow Fang.
public class PetCompanionMultiSlow : PetCompanionSlow
{
    [Tooltip("FX va chạm khi quả cầu chính vỡ (MultiSlowImpactPrefab gốc).")]
    [SerializeField] private GameObject impactFx;

    private const float IMPACT_FX_LIFETIME = 1.5f;

    protected override void ReleaseAbility()
    {
        double damage = GetAbilityDamage();
        FireHoming(projectilePrefab, target, companionData.projectileSpeed, hit => OnMainHit(hit, damage));
        PlayShootSfx();
    }

    private void OnMainHit(BaseUnit hit, double damage)
    {
        Vector3 hitPoint = hit.centerBodyPoint.position;
        HitAndSlow(hit, damage);
        CompanionFx.Spawn(impactFx, hitPoint, 1f, IMPACT_FX_LIFETIME);

        List<BaseUnit> splits = GetNearestOthers(hitPoint, hit, companionData.multiSlowChainCount);
        for (int i = 0; i < splits.Count; i++)
        {
            FireHomingFrom(projectilePrefab, hitPoint, splits[i], companionData.projectileSpeed, h => HitAndSlow(h, damage));
        }
    }

    // 'count' enemy gần 'center' nhất, bỏ qua 'exclude'.
    private List<BaseUnit> GetNearestOthers(Vector3 center, BaseUnit exclude, int count)
    {
        List<BaseUnit> candidates = GetEnemiesAroundOwner();
        candidates.Remove(exclude);
        candidates.Sort((a, b) => VectorUtils.SqrDistance(center, a.Transform.position)
            .CompareTo(VectorUtils.SqrDistance(center, b.Transform.position)));

        if (candidates.Count > count)
        {
            candidates.RemoveRange(count, candidates.Count - count);
        }
        return candidates;
    }
}
