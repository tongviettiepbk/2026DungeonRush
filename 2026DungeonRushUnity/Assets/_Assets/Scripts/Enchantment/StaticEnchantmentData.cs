using UnityEngine;

// Config tĩnh hệ Enchantment: sprite icon/nền theo tier (số liệu balance nằm ở EnchantmentConfig).
public class StaticEnchantmentData
{
    public EnchantmentSpriteConfig sprites;

    public StaticEnchantmentData()
    {
        sprites = Resources.Load<EnchantmentSpriteConfig>("Scriptable Objects/Enchantment/EnchantmentSpriteConfig");
        if (sprites == null)
        {
            DebugCustom.LogError("[StaticEnchantmentData] Thiếu EnchantmentSpriteConfig");
        }
    }

    public Sprite GetTierIcon(int tier)
    {
        return GetAt(sprites != null ? sprites.tierIcons : null, tier - 1);
    }

    // Nền tier relic (GameResources.jgg): tier clamp vào 1..số sprite.
    public Sprite GetTierBackground(int tier)
    {
        return GetAt(sprites != null ? sprites.backgrounds : null, tier - 1);
    }

    // Nền rarity món đồ (ô slot trang bị trong trang Enchantment).
    public Sprite GetRarityBackground(Rarity rarity)
    {
        return GetAt(sprites != null ? sprites.backgrounds : null, (int)rarity);
    }

    private static Sprite GetAt(Sprite[] list, int index)
    {
        if (list == null || list.Length == 0)
        {
            return null;
        }
        return list[Mathf.Clamp(index, 0, list.Length - 1)];
    }
}
