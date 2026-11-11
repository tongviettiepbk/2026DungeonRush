using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 hàng rarity trong popup tỉ lệ summon (UIUpgradePet): tên giữ theo prefab, 2 ô % = level hiện tại / level kế.
public class ElementRarityPet : MonoBehaviour
{
    public TMP_Text txtName;
    public TMP_Text txtRateCurret;
    public TMP_Text txtRateNext;

    // Cột Lv sau: tăng = xanh, giảm = hồng nhạt, giữ nguyên (hoặc max level) = trắng (concept batch3).
    public static readonly Color RATE_UP_COLOR = new Color32(157, 255, 122, 255);    // #9DFF7A
    public static readonly Color RATE_DOWN_COLOR = new Color32(255, 179, 179, 255);  // #FFB3B3

    public void SetData(float rateCurrent, float rateNext)
    {
        txtRateCurret.text = rateCurrent.ToString("0.##") + "%";
        txtRateNext.text = rateNext.ToString("0.##") + "%";
        txtRateNext.color = rateNext > rateCurrent ? RATE_UP_COLOR : rateNext < rateCurrent ? RATE_DOWN_COLOR : Color.white;
    }
}
