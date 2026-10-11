using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô trong popup thông tin người chơi (ô ItemElementUI của PlayerPopup gốc): nền theo độ hiếm, icon, "Lv N",
// huy hiệu relic "+tier". Dùng cho 8 ô đồ và các ô pet (pet không có huy hiệu relic).
public class ElementPlayerItemUI : MonoBehaviour
{
    public GearSlotType typeEquipment;
    public Image imgBackground;
    public Image imgItem;
    public GameObject objPlaceholder;
    public TMP_Text txtLevel;
    public GameObject objTier;
    public Image imgTierBackground;
    public TMP_Text txtTier;

    // Gắn listener 1 lần. Button nằm trên root của ô.
    public void Init(Action<ElementPlayerItemUI> onClick)
    {
        GetComponent<Button>().onClick.AddListener(() => onClick(this));
    }

    public void SetData(Sprite icon, Rarity rarity, int level, int relicTier)
    {
        imgBackground.sprite = RarityBackgroundConfig.Get(rarity);
        objPlaceholder.SetActive(false);
        imgItem.gameObject.SetActive(true);
        imgItem.sprite = icon;
        txtLevel.text = "Lv " + level;
        SetRelicTier(relicTier);
    }

    // Ô chưa mặc đồ: hình mờ của slot, không level. Relic vẫn hiện vì relic gắn theo slot chứ không theo món.
    public void SetEmpty(int relicTier)
    {
        imgBackground.sprite = RarityBackgroundConfig.Get(Rarity.Common);
        objPlaceholder.SetActive(true);
        imgItem.gameObject.SetActive(false);
        txtLevel.text = string.Empty;
        SetRelicTier(relicTier);
    }

    private void SetRelicTier(int tier)
    {
        bool hasRelic = tier >= 1;
        objTier.SetActive(hasRelic);
        if (!hasRelic) return;

        txtTier.text = "+" + tier;
        imgTierBackground.sprite = GameData.staticData.enchantments.GetTierBackground(tier);
    }
}
