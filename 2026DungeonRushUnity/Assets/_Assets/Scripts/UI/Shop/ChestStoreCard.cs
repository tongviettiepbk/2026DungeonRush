using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thẻ 1 rương trong Store (ChestStoreCard gốc): ảnh, tên, giá gem, "Contains …", dòng pity, nút mua + nút info.
public class ChestStoreCard : MonoBehaviour
{
    public Image Icon;
    public List<Image> ShowcaseImages;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI CostText;
    public Button BuyButton;
    public Button InfoButton;
    public TextMeshProUGUI ContainsText;
    public TextMeshProUGUI PityText;
    public Color PityCountColor;

    private ChestData chest;

    // foc gốc.
    public void Setup(ChestData data)
    {
        chest = data;
        if (Icon != null) Icon.sprite = chest.GetMainIcon();
        SetupShowcase();
        ShopUIUtil.SetText(NameText, StaticShopData.GetChestName(chest));
        ShopUIUtil.SetText(CostText, "<sprite=0>" + chest.GemCost);

        if (BuyButton != null)
        {
            BuyButton.onClick.RemoveAllListeners();
            BuyButton.onClick.AddListener(OnBuyClicked);
        }
        if (InfoButton != null)
        {
            InfoButton.onClick.RemoveAllListeners();
            InfoButton.onClick.AddListener(OnInfoClicked);
        }

        ShopUIUtil.SetText(ContainsText, string.Format("Contains {0}", GetRarityList(chest)));
        RefreshPity();
    }

    // fod gốc: ảnh pet/cape tiêu biểu quanh rương (chỉ thẻ ngang có).
    private void SetupShowcase()
    {
        if (ShowcaseImages == null) return;
        for (int i = 0; i < ShowcaseImages.Count; i++)
        {
            if (ShowcaseImages[i] == null) continue;
            bool has = i < chest.ShowcaseSprites.Count && chest.ShowcaseSprites[i] != null;
            ShowcaseImages[i].gameObject.SetActive(has);
            if (has) ShowcaseImages[i].sprite = chest.ShowcaseSprites[i];
        }
    }

    // Danh sách rarity có trong rương, mỗi tên tô màu rarity, cách nhau ", " (foe / ChestInfoPopup.fmx gốc).
    public static string GetRarityList(ChestData chest)
    {
        string list = string.Empty;
        for (int i = 0; i < chest.RarityOdds.Count; i++)
        {
            if (list.Length > 0) list += ", ";
            list += ShopUIUtil.ColoredRarity(chest.RarityOdds[i].Rarity);
        }
        return list;
    }

    // "Get {rarity} in {n} opens" (fof / ChestInfoPopup.fmy gốc).
    public static string GetPityText(ChestData chest, Color countColor)
    {
        return string.Format("Get {0} in {1} opens", ShopUIUtil.ColoredRarity(chest.Rarity),
            ShopUIUtil.Colored(ShopService.GetPityRemaining(chest).ToString(), countColor));
    }

    public void RefreshPity()
    {
        if (PityText == null || chest == null) return;
        PityText.gameObject.SetActive(chest.PityCount > 0);
        if (chest.PityCount > 0) PityText.text = GetPityText(chest, PityCountColor);
    }

    // fog gốc: đủ gem → trừ gem, mở rương, hiện màn mở rương.
    private void OnBuyClicked()
    {
        List<ChestOutcome> outcomes = ShopService.BuyChest(chest);
        if (outcomes == null)
        {
            ShopUIUtil.Toast(ShopUIUtil.NOT_ENOUGH_GEMS);
            return;
        }

        RefreshPity();
        ShopUIUtil.RefreshLobby();
        UIChestRevealPopup popup = UIManager.Instance.LoadUI(UIKey.ChestRevealPopup) as UIChestRevealPopup;
        if (popup != null) popup.Setup(outcomes);
    }

    // foh gốc.
    private void OnInfoClicked()
    {
        UIChestInfoPopup popup = UIManager.Instance.LoadUI(UIKey.ChestInfoPopup) as UIChestInfoPopup;
        if (popup != null) popup.Setup(chest);
    }
}
