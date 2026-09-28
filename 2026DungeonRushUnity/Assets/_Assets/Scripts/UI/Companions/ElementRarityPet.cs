using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 hàng rarity trong popup tỉ lệ summon (UIUpgradePet): tên giữ theo prefab, 2 ô % = level hiện tại / level kế.
public class ElementRarityPet : MonoBehaviour
{
    public TMP_Text txtName;
    public TMP_Text txtRateCurret;
    public TMP_Text txtRateNext;

    public void SetData(float rateCurrent, float rateNext)
    {
        txtRateCurret.text = rateCurrent.ToString("0.##") + "%";
        txtRateNext.text = rateNext.ToString("0.##") + "%";
    }
}
