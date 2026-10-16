using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô item (ItemElementUI gốc): nền theo rarity + pattern Ancient/Immortal/Divine, icon, "Lv.N".
// Dùng trong NewItemPopup / ItemInfoPopup và các món nằm trên bục forge.
// dismissedTutorial: tay chỉ trên món bị bỏ qua ở bục (ItemElementUI.DismissedItemTutorialUI gốc, FTUE step 21).
public class ItemElementView : MonoBehaviour
{
    public Button button;
    public Image imgBackground;
    public Image imgItem;
    public TMP_Text txtLevel;
    public GameObject objAncient;
    public GameObject objImmortal;
    public GameObject objDivine;
    public TutorialUI dismissedTutorial;

    public void SetItem(LootResult item)
    {
        Set(item.icon, item.rarity, item.level);
    }

    // level <= 0 → ẩn chữ level.
    public void Set(Sprite icon, Rarity rarity, int level)
    {
        if (imgBackground != null)
        {
            Sprite bg = RarityBackgroundConfig.Get(rarity);
            if (bg != null)
                imgBackground.sprite = bg;
            // Bản rip NewForgePopup để nền tint đỏ (FD5E5E) → nền rarity bị ám đỏ; màu do sprite quyết định.
            imgBackground.color = Color.white;
        }

        if (imgItem != null)
        {
            imgItem.sprite = icon;
            imgItem.gameObject.SetActive(icon != null);
        }

        if (txtLevel != null)
        {
            txtLevel.gameObject.SetActive(level > 0);
            txtLevel.text = "Lv." + level;
        }

        if (objAncient != null) objAncient.SetActive(rarity == Rarity.Ancient);
        if (objImmortal != null) objImmortal.SetActive(rarity == Rarity.Immortal);
        if (objDivine != null) objDivine.SetActive(rarity == Rarity.Divine);
    }
}
