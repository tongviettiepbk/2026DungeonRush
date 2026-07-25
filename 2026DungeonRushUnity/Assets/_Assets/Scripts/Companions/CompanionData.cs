using UnityEngine;

// Config 1 companion (D1) — tạo asset qua menu: Create > DungOnRush > Companion Data.
// Đặt asset trong Resources/Scriptable Objects/Companions để StaticCompanionData load được.
//
// Companion = thú/vật đồng hành bay theo người chơi, mỗi con 1 kiểu kỹ năng (CompanionType)
// và dùng nhóm field effect tương ứng (Bomb/Burn/Lightning/Beam/Guardian/AoeSlow/Blaster/
// Meteor/Siphon/HealNova/Clone/Immortality). Các con không dùng field nào để = 0.
//
// LƯU Ý (theo pattern C1–C9): layer data CHỈ giữ stat cân bằng. Đã bỏ mọi reference của
// bản gốc (Prefab, ProjectilePrefab, BombPrefab, các *ImpactPrefab, âm thanh + volume,
// màu VFX LightningFlashColor/SlowFlashColor/CloneTintColor). Chỉ giữ lại icon để hiển thị.
[CreateAssetMenu(fileName = "Companion-", menuName = "DungOnRush/Companion Data")]
public class CompanionData : ScriptableObject
{
    [Header("Định danh")]
    public string assetName;        // Khoá duy nhất, VD "Companion_1_Common_DPS_Single".
    public string companionName;    // Tên hiển thị mặc định (EN), VD "Ember Fist".
    public string localizationKey;  // "Items.Companion.Name.{assetName}".
    public string descriptionKey;   // "Items.Companion.Desc.{assetName}".
    public CompanionType type;
    public Rarity rarity;
    [TextArea] public string details; // Mô tả kỹ năng, có placeholder <Value>.
    public Sprite icon;

    [Header("Di chuyển / nhịp")]
    public float moveSpeed;
    public float followDistance;
    public float minDistance;
    public float initialDelay;
    public float cooldown;
    public float effectDuration;
    public int level;               // Cấp khởi điểm (đa số =1, vài con =2).

    [Header("Combat lõi")]
    public float damageBase;
    public float damageScaler;
    public float healBase;
    public float healScaler;
    public float slowDuration;
    public float slowAmount;
    public float projectileSpeed;

    [Header("Bomber (bom AOE)")]
    public float bombRadius;
    public float bombProjectileSpeed;
    public float bombTravelDuration;
    public float bombArcHeight;
    public float bombExplosionDuration;

    [Header("Burn (đốt cháy)")]
    public bool burnEnabled;
    public float burnDuration;
    public float burnTickDamage;

    [Header("MultiSlower")]
    public int multiSlowChainCount;

    [Header("ChainHealer")]
    public float chainHealSpeed;
    public float chainHealDuration;
    public float healTickInterval;

    [Header("Lightning")]
    public int lightningChainCount;
    public float lightningSpeed;
    public float lightningDuration;
    public float lightningTickInterval;

    [Header("Beam")]
    public float beamSpeed;
    public float beamDuration;
    public float beamTickInterval;

    [Header("Guardian")]
    public float guardianActiveDuration;
    public float guardianHealAmount;
    public float guardianBlockChance;

    [Header("AoeSlower")]
    public float aoeSlowRadius;
    public float aoeSlowProjectileSpeed;
    public float aoeSlowArcHeight;
    public float aoeSlowExplosionDuration;

    [Header("Blaster")]
    public float blasterRange;
    public float blasterFireDuration;
    public int blasterWaveCount;
    public float blasterConeAngle;

    [Header("Meteor")]
    public int meteorBombCount;
    public float meteorOffsetRange;
    public float meteorBombDelay;
    public float meteorDropHeight;

    [Header("Siphon (hút máu/dùng stat bản thân)")]
    public float siphonProjectileSpeed;
    public float siphonSineAmplitude;
    public float siphonSineFrequency;
    public float ownAttackBase;
    public float ownAttackScaler;
    public float ownHealthBase;
    public float ownHealthScaler;

    [Header("HealNova")]
    public float healNovaStartRadius;
    public float healNovaEndRadius;
    public float healNovaDuration;
    public float healNovaTickInterval;
    public float healNovaDamageBase;
    public float healNovaDamageScaler;

    [Header("MirrorClone")]
    public float cloneHealthPercent;
    public float cloneHealthPercentScaler;
    public float cloneLifetime;
    public int cloneGridSearchCount;
    public float cloneWalkDuration;
    public float cloneTintAmount;
    public float cloneAlpha;
    public float cloneBrightness;

    [Header("Immortality")]
    public float immortalityDuration;
    public float immortalityDurationScaler;

    public string GetLocalizeKey()
    {
        return "Items.Companion.Name." + assetName;
    }
}
