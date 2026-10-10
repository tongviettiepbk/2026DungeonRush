using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô thưởng icon + số (ClanWarRewardCell gốc).
public class ClanWarRewardCell : MonoBehaviour
{
    public Image IconImage;
    public TMP_Text AmountText;

    // gdj gốc.
    public void Set(Sprite icon, int amount)
    {
        if (IconImage != null)
        {
            IconImage.sprite = icon;
            IconImage.preserveAspect = true;
        }
        if (AmountText != null) AmountText.text = ClanWarController.FormatNumber(amount);
    }
}
