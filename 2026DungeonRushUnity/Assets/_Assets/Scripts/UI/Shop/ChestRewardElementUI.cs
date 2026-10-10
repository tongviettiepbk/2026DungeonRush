using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô 1 món mở ra từ rương (ChestRewardElementUI gốc): icon pet/cape trên nền rarity, nhãn NEW khi là mẫu mới.
public class ChestRewardElementUI : MonoBehaviour
{
    public Image Icon;
    public Image RarityFrame;
    public GameObject NewBadge;
    public TextMeshProUGUI NameText;

    // fob gốc.
    public void Setup(ChestOutcome outcome)
    {
        if (Icon != null) Icon.sprite = outcome.icon;
        if (RarityFrame != null)
        {
            Sprite frame = RarityBackgroundConfig.Get(outcome.rarity);
            if (frame != null) RarityFrame.sprite = frame;
        }
        if (NewBadge != null) NewBadge.SetActive(outcome.isNew);
        ShopUIUtil.SetText(NameText, outcome.name);
    }
}
