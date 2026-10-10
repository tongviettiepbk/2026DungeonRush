using TMPro;
using UnityEngine;

// 1 dòng "hành động → điểm" (ClanWarActionCard gốc).
public class ClanWarActionCard : MonoBehaviour
{
    public TMP_Text ActionLabelText;
    public TMP_Text PointValueText;

    // gbj gốc.
    public void Set(string label, string points)
    {
        if (ActionLabelText != null) ActionLabelText.text = label;
        if (PointValueText != null) PointValueText.text = points;
    }
}
