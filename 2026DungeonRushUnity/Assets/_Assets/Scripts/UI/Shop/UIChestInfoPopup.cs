using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thông tin rương (ChestInfoPopup gốc): tên, ảnh, "Contains … cloak and companion", tỉ lệ từng rarity, dòng pity.
public class UIChestInfoPopup : BaseUI
{
    public TextMeshProUGUI ChestNameText;
    public Button CloseButton;
    public Image ChestIcon;
    public TextMeshProUGUI DescriptionText;
    public List<ChestOddsRow> RarityRows;
    public RectTransform ContentTransform;
    public float RarityRowHeight;
    public GameObject PityParent;
    public TextMeshProUGUI PityCountText;
    public Color PityCountColor;

    private float fullHeight = -1f;

    protected override void Awake()
    {
        base.Awake();
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
    }

    // fmw + eip gốc.
    public void Setup(ChestData chest)
    {
        ShopUIUtil.SetText(ChestNameText, StaticShopData.GetChestName(chest));
        if (ChestIcon != null) ChestIcon.sprite = chest.Icon;
        ShopUIUtil.SetText(DescriptionText, string.Format("Contains {0} cloak and companion", ChestStoreCard.GetRarityList(chest)));

        if (PityParent != null) PityParent.SetActive(chest.PityCount > 0);
        if (chest.PityCount > 0) ShopUIUtil.SetText(PityCountText, ChestStoreCard.GetPityText(chest, PityCountColor));

        // fnb / fnc gốc: % = trọng số / tổng, định dạng "0.##"; rarity không có trong rương thì ẩn dòng.
        float total = 0f;
        for (int i = 0; i < chest.RarityOdds.Count; i++) total += Mathf.Max(chest.RarityOdds[i].Weight, 0f);

        int hidden = 0;
        for (int i = 0; i < RarityRows.Count; i++)
        {
            ChestOddsRow row = RarityRows[i];
            if (row == null) continue;
            float weight = GetWeight(chest, row.Rarity);
            bool isShow = weight > 0f && total > 0f;
            row.gameObject.SetActive(isShow);
            if (isShow) ShopUIUtil.SetText(row.PercentText, (weight / total * 100f).ToString("0.##") + "%");
            else hidden++;
        }

        // fna gốc: bảng thấp đi theo số dòng bị ẩn.
        if (ContentTransform != null)
        {
            if (fullHeight < 0f) fullHeight = ContentTransform.rect.height;
            ContentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fullHeight - RarityRowHeight * hidden);
        }
    }

    private static float GetWeight(ChestData chest, Rarity rarity)
    {
        for (int i = 0; i < chest.RarityOdds.Count; i++)
        {
            if (chest.RarityOdds[i].Rarity == rarity) return Mathf.Max(chest.RarityOdds[i].Weight, 0f);
        }
        return 0f;
    }
}
