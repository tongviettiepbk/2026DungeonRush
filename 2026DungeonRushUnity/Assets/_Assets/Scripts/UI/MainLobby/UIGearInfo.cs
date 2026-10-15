using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup xem chi tiết món đang mặc (ItemInfoPopup gốc): ô item + dòng rarity + khối InfoText
// (tên, chỉ số chính, substat).
public class UIGearInfo : BaseUI
{
    public Button btClose;
    public Button btBackground;
    public ItemElementView itemElement;
    public TMP_Text txtRarity;
    public TMP_Text txtInfo;

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
        txtRarity.text = ItemTextUtil.RarityLine(result.rarity);
        txtInfo.text = BuildInfo(ItemTextUtil.Name(result),
            ItemTextUtil.MainStat(result, EnchantmentConfig.GetStatMultiplier(enchantmentTier)), result.subStats);
        gameObject.SetActive(true);
    }

    // Đổ thẳng chữ đã dựng sẵn — cho món không đi qua LootResult (Wing/Cape có 2 chỉ số chính).
    public void Show(Sprite icon, Rarity rarity, int level, string itemName, string mainStats, List<GearSubStat> subStats)
    {
        itemElement.Set(icon, rarity, level);
        txtRarity.text = ItemTextUtil.RarityLine(rarity);
        txtInfo.text = BuildInfo(ShopUIUtil.Colored(itemName, UITabWing.GetRarityColor(rarity)), mainStats, subStats);
        gameObject.SetActive(true);
    }

    private static string BuildInfo(string name, string mainStats, List<GearSubStat> subStats)
    {
        string info = name + "\n\n" + mainStats;
        string subText = ItemTextUtil.SubStats(subStats);
        if (subText.Length > 0)
            info += "\n\n" + subText;
        return info;
    }
}
