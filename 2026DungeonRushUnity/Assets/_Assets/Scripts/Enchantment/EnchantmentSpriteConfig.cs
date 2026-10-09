using UnityEngine;

// Sprite hệ Enchantment (gốc GameResources.EnchantmentTierIcons + ItemRarityBackgroundSprites).
// Asset: Resources/Scriptable Objects/Enchantment/EnchantmentSpriteConfig.
[CreateAssetMenu(fileName = "EnchantmentSpriteConfig", menuName = "DungOnRush/Enchantment Sprite Config")]
public class EnchantmentSpriteConfig : ScriptableObject
{
    // Icon relic theo tier: [0] = tier 1 … [10] = tier 11 (enchantment_01..11).
    public Sprite[] tierIcons;

    // Nền ô theo bậc: [i] = rarity i (Common..) = tier i+1 (GameResources.jgg: tier → index tier-1).
    // Game gốc dùng CHUNG 1 bảng cho nền rarity món đồ và nền tier relic.
    public Sprite[] backgrounds;
}
