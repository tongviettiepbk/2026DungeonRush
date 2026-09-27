// Companion Guardian (type 8) — Vital Root: "Creates a healing aura that restores <Value> Health per second and increases Block Chance by 5%."
// Logic GỐC (reverse strategy `ko` + CompanionGuardianEffect):
//   • Nhắm CHỦ, không cần enemy. Kích hoạt → hồi máu hero MỘT LẦN heal = (HealBase + HealScaler×lv)×bonus
//     (gnc: hp = min(hp + heal, maxHp)); GuardianHealAmount trong data KHÔNG được dùng.
//   • Cộng GuardianBlockChance (%) vào BlockChance của hero trong GuardianActiveDuration rồi trừ lại.
public class PetCompanionGuardian : PetUnit
{
    protected override bool TargetsOwner => true;

    protected override void ReleaseAbility()
    {
        HealOwner(GetAbilityHeal());
        CompanionBlockEffect.Apply(owner, companionData.guardianActiveDuration, companionData.guardianBlockChance);
        PlayShootSfx();
    }
}
