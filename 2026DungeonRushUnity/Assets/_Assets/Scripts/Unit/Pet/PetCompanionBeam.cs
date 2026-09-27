using System.Collections.Generic;
using UnityEngine;

// Companion Beam (type 7) — Arcane Paw: "Fires a continuous magical beam that deals <Value> Damage per second."
// Logic GỐC (reverse strategy `kf` + CompanionBeamEffect): tia phép bắn tới enemy gần hero nhất với
// BeamSpeed; chạm → TRỌN damage (gkh); giữ tia BeamDuration, mỗi BeamTickInterval gây TRỌN damage (gki).
// damage = (DamageBase + DamageScaler × level) × bonus.
public class PetCompanionBeam : PetUnit
{
    [SerializeField] private Color beamColor = new Color(0.75f, 0.45f, 1f, 1f);
    [SerializeField] private float beamWidth = 0.14f;

    private CompanionLink link;

    protected override void ReleaseAbility()
    {
        if (link == null)
        {
            link = new CompanionLink(this, Transform, beamColor, beamWidth, false);
        }

        BaseUnit victim = target;
        List<BaseUnit> chain = new List<BaseUnit> { victim };
        double damage = GetAbilityDamage();

        link.Play(firePoint, chain, companionData.beamSpeed, companionData.beamDuration, companionData.beamTickInterval,
            _ => DealDamage(victim, damage),
            () => DealDamage(victim, damage),
            () => isPause);
        PlayShootSfx();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        link?.Stop();
    }
}
