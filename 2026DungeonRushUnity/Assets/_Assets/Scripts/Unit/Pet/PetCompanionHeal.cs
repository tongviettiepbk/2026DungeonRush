// Companion Healer (type 1) — Magic Wool: "Fires a healing projectile that restores <Value> Health."
// Logic GỐC (reverse il2cpp v41, strategy `kq`, xem DecodedData/COMPANION_MODEL.md §6):
//   • Mục tiêu = CHỦ (hero), KHÔNG cần enemy: hồi chiêu xong là bắn, kể cả khi hero đầy máu
//     (kq.gfk không kiểm tra HP; hồi xong kẹp ở maxHp).
//   • heal = (HealBase + HealScaler × level) × (1 + CompanionDamageBonus/100)  → GetAbilityHeal().
//   • Pet quay mặt về hero, bắn ProjectilePrefab đuổi theo hero với ProjectileSpeed;
//     tới nơi → hồi máu + particle Heal trên hero.
public class PetCompanionHeal : PetUnit
{
    protected override bool TargetsOwner => true;

    protected override void ReleaseAbility()
    {
        double heal = GetAbilityHeal();
        FireHoming(projectilePrefab, owner, companionData.projectileSpeed, _ => HealOwner(heal));
        PlayShootSfx();
    }
}
