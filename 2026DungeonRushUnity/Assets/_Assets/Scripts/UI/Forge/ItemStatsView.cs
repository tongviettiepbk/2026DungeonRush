using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Khối chữ 1 món đồ dùng chung cho ItemInfo / NewItem / NewForge (concept tools/concept/batch1):
//  - Rarity + tên tô màu rarity (chữ viền).
//  - Chỉ số chính: cột nhãn nâu + cột giá trị chữ viền (Wing/Cape có 2 dòng).
//  - Substat: cột tên nâu (trái) + cột giá trị chữ viền (phải), ô cao cố định theo số dòng tối đa;
//    không có substat → hiện chữ mờ "No bonus stats".
public class ItemStatsView : MonoBehaviour
{
    public TMP_Text txtRarity;
    public TMP_Text txtName;
    public TMP_Text txtMainLabels;
    public TMP_Text txtMainValues;
    public TMP_Text txtSubNames;
    public TMP_Text txtSubValues;
    public TMP_Text txtNoSub;

    public void Set(LootResult item, float multiplier = 1f)
    {
        SetHeader(item.rarity, ItemTextUtil.Name(item));
        txtMainLabels.text = ItemTextUtil.MainStatLabel(item);
        txtMainValues.text = ItemTextUtil.MainStatValue(item, multiplier);
        PlaceMainValues();
        SetSubStats(item.subStats);
    }

    // mainStats dựng sẵn mỗi dòng "Nhãn giá trị" (Wing/Cape): tách ở dấu cách cuối.
    public void Set(Rarity rarity, string itemName, string mainStats, List<GearSubStat> subStats)
    {
        SetHeader(rarity, itemName);
        StringBuilder labels = new StringBuilder();
        StringBuilder values = new StringBuilder();
        string[] lines = mainStats.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                labels.Append('\n');
                values.Append('\n');
            }
            int space = lines[i].LastIndexOf(' ');
            labels.Append(space > 0 ? lines[i].Substring(0, space) : lines[i]);
            values.Append(space > 0 ? lines[i].Substring(space + 1) : string.Empty);
        }
        txtMainLabels.text = labels.ToString();
        txtMainValues.text = values.ToString();
        PlaceMainValues();
        SetSubStats(subStats);
    }

    private void SetHeader(Rarity rarity, string itemName)
    {
        txtRarity.text = ItemTextUtil.RarityLine(rarity);
        txtName.text = itemName;
        txtMainValues.color = Color.white;
    }

    // Cột giá trị đứng ngay sau nhãn dài nhất (nhãn đổi theo ngôn ngữ). Hai cột cùng neo/pivot trái.
    private void PlaceMainValues()
    {
        RectTransform labels = txtMainLabels.rectTransform;
        RectTransform values = txtMainValues.rectTransform;
        float width = txtMainLabels.GetPreferredValues(txtMainLabels.text).x;
        values.anchoredPosition = new Vector2(labels.anchoredPosition.x + width + 14f, values.anchoredPosition.y);
    }

    private void SetSubStats(List<GearSubStat> subStats)
    {
        bool hasSub = subStats != null && subStats.Count > 0;
        txtSubNames.text = hasSub ? ItemTextUtil.SubStatNames(subStats) : string.Empty;
        txtSubValues.text = hasSub ? ItemTextUtil.SubStatValues(subStats) : string.Empty;
        txtNoSub.gameObject.SetActive(hasSub == false);
    }
}
