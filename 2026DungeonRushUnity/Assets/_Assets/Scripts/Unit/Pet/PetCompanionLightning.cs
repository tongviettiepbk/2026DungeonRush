using System.Collections.Generic;
using UnityEngine;

// Companion Lightning (type 3) — Spark Mouse (đơn) / Storm Eye (chuỗi):
// Logic GỐC (reverse strategy `kx` + CompanionLightningEffect):
//   • Chuỗi mục tiêu = enemy gần hero nhất + tối đa LightningChainCount enemy khác (Storm Eye: 1+2).
//   • Tia sét chạy qua từng mục tiêu với LightningSpeed, chạm con nào gây TRỌN damage con đó (goe).
//   • Sau đó giữ tia LightningDuration, mỗi LightningTickInterval gây TRỌN damage cho cả chuỗi (gof).
//   • damage = (DamageBase + DamageScaler × level) × bonus (không chia theo tick).
// Gốc nối thêm theo thứ tự danh sách enemy của SpawnController; ở đây nối theo enemy GẦN mục tiêu
// trước nhất để tia không nhảy lung tung (cùng số lượng).
public class PetCompanionLightning : PetUnit
{
    [SerializeField] private Color lightningColor = new Color(0.6f, 0.85f, 1f, 1f);
    [SerializeField] private float lightningWidth = 0.08f;

    private CompanionLink link;

    protected override void ReleaseAbility()
    {
        if (link == null)
        {
            link = new CompanionLink(this, Transform, lightningColor, lightningWidth, true);
        }

        List<BaseUnit> chain = BuildChain(target, companionData.lightningChainCount);
        double damage = GetAbilityDamage();

        link.Play(firePoint, chain, companionData.lightningSpeed, companionData.lightningDuration, companionData.lightningTickInterval,
            i => DealDamage(chain[i], damage),
            () =>
            {
                for (int i = 0; i < chain.Count; i++)
                {
                    DealDamage(chain[i], damage);
                }
            },
            () => isPause);
        PlayShootSfx();
    }

    private List<BaseUnit> BuildChain(BaseUnit first, int extraCount)
    {
        List<BaseUnit> chain = new List<BaseUnit> { first };
        List<BaseUnit> pool = GetEnemiesAroundOwner();
        pool.Remove(first);

        BaseUnit last = first;
        for (int n = 0; n < extraCount && pool.Count > 0; n++)
        {
            BaseUnit next = FindNearestEnemyFrom(last.Transform.position, pool);
            if (next == null)
            {
                break;
            }
            chain.Add(next);
            pool.Remove(next);
            last = next;
        }
        return chain;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        link?.Stop();
    }
}
