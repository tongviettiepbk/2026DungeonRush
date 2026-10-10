using TMPro;
using UnityEngine;

// 1 dòng thưởng của Daily Deal: "<sprite=0>{số}" (DailyDealRewardEntryUI gốc).
public class DailyDealRewardEntryUI : MonoBehaviour
{
    public TextMeshProUGUI RewardText;

    // kak gốc.
    public void Setup(TMP_SpriteAsset spriteAsset, int amount)
    {
        if (RewardText == null) return;
        if (spriteAsset != null) RewardText.spriteAsset = spriteAsset;
        RewardText.text = "<sprite=0>" + ShopUIUtil.FormatAmount(amount);
    }
}
