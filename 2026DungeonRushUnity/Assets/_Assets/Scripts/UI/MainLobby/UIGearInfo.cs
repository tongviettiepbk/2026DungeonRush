using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Popup xem chi tiết món đang mặc (ItemInfoPopup gốc): ô item + khối chữ ItemStatsView
// (rarity, tên, chỉ số chính, substat — Wing/Cape tối đa 2 chỉ số chính + 5 substat).
public class UIGearInfo : BaseUI
{
    public Button btClose;
    public Button btBackground;
    public ItemElementView itemElement;
    public ItemStatsView stats;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(Close);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
    }

    // enchantmentTier: relic đang đeo ở slot của món (ItemInfoPopup.EnchantmentTier gốc, chỉ khi mở từ ô
    // đang mặc) → main stat hiển thị × (1 + tier²/100) như ItemInfoPopup.igz. 0 = không có relic.
    public void Show(LootResult result, int enchantmentTier = 0)
    {
        itemElement.SetItem(result);
        stats.Set(result, EnchantmentConfig.GetStatMultiplier(enchantmentTier));
        gameObject.SetActive(true);
    }

    // Đổ thẳng chữ đã dựng sẵn — cho món không đi qua LootResult (Wing/Cape có 2 chỉ số chính).
    public void Show(Sprite icon, Rarity rarity, int level, string itemName, string mainStats, List<GearSubStat> subStats)
    {
        itemElement.Set(icon, rarity, level);
        stats.Set(rarity, ShopUIUtil.Colored(itemName, UITabWing.GetRarityColor(rarity)), mainStats, subStats);
        gameObject.SetActive(true);
    }
}
