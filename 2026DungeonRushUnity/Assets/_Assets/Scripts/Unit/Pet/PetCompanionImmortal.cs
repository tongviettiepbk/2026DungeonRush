// Companion Immortality (type 15) — Celestial Mind: "Blesses you with immortality, preventing your Health from dropping below 1 for <Value> seconds."
// Logic GỐC (reverse strategy `kv` + CompanionImmortalityEffect):
//   • Nhắm CHỦ. duration = ImmortalityDuration + ImmortalityDurationScaler × level (KHÔNG nhân bonus);
//     giá trị này cũng thay EffectDuration làm pha active của pet (Companion+0x94).
//   • Bắn đạn về hero → tới nơi bật IsImmortal trong 'duration' (HP không xuống dưới 1).
public class PetCompanionImmortal : PetUnit
{
    protected override bool TargetsOwner => true;

    private float ImmortalDuration => companionData.immortalityDuration + companionData.immortalityDurationScaler * level;

    protected override float GetActiveDuration()
    {
        return companionData != null ? ImmortalDuration : 0f;
    }

    protected override void ReleaseAbility()
    {
        float duration = ImmortalDuration;
        FireHoming(projectilePrefab, owner, companionData.projectileSpeed, hero => CompanionImmortalEffect.Apply(hero, duration));
        PlayShootSfx();
    }
}
