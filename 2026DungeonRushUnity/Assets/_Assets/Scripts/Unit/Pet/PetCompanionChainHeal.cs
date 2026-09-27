using System.Collections.Generic;
using UnityEngine;

// Companion ChainHealer (type 4) — Life Horn: "Fires a continuous healing ray that restores <Value> Health per second."
// Logic GỐC (reverse strategy `kl` + CompanionChainHealEffect): tia hồi máu nối pet → HERO (không cần
// enemy), chạy tới với ChainHealSpeed, giữ ChainHealDuration; mỗi HealTickInterval hồi TRỌN
// heal = (HealBase + HealScaler × level) × bonus (glh: hp = min(hp + heal, maxHp)).
public class PetCompanionChainHeal : PetUnit
{
    [SerializeField] private Color rayColor = new Color(0.45f, 1f, 0.55f, 1f);
    [SerializeField] private float rayWidth = 0.1f;

    private CompanionLink link;

    protected override bool TargetsOwner => true;

    protected override void ReleaseAbility()
    {
        if (link == null)
        {
            link = new CompanionLink(this, Transform, rayColor, rayWidth, false);
        }

        double heal = GetAbilityHeal();
        List<BaseUnit> chain = new List<BaseUnit> { owner };

        link.Play(firePoint, chain, companionData.chainHealSpeed, companionData.chainHealDuration, companionData.healTickInterval,
            null,
            () => HealOwner(heal),
            () => isPause);
        PlayShootSfx();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        link?.Stop();
    }
}
