using UnityEngine;

// Companion HealNova (type 13) — Radiant Talon: "Creates a radiant circle that restores <Value> Health per second and deals <Value> Area Damage to nearby enemies."
// Logic GỐC (reverse strategy `kt` + CompanionHealNovaEffect):
//   • Nhắm CHỦ. Bắn đạn hồi máu về hero (ProjectileSpeed) → hồi heal = (HealBase + HealScaler×lv)×bonus.
//   • Rồi bung vòng sáng tại hero: bán kính HealNovaStartRadius → HealNovaEndRadius trong
//     HealNovaDuration, quét mỗi HealNovaTickInterval; MỖI enemy bị vòng chạm nhận 1 lần
//     novaDamage = (HealNovaDamageBase + HealNovaDamageScaler×lv)×bonus.
public class PetCompanionHealNova : PetUnit
{
    [SerializeField] private Color novaColor = new Color(1f, 0.95f, 0.55f, 1f);

    protected override bool TargetsOwner => true;

    protected override void ReleaseAbility()
    {
        double heal = GetAbilityHeal();
        double novaDamage = ScaleByLevel(companionData.healNovaDamageBase, companionData.healNovaDamageScaler);

        FireHoming(projectilePrefab, owner, companionData.projectileSpeed, hero =>
        {
            HealOwner(heal);
            CompanionNova.Spawn(hero, companionData.healNovaStartRadius, companionData.healNovaEndRadius,
                companionData.healNovaDuration, companionData.healNovaTickInterval, novaColor,
                GetEnemiesInRadius, enemy => DealDamage(enemy, novaDamage));
        });
        PlayShootSfx();
    }
}
