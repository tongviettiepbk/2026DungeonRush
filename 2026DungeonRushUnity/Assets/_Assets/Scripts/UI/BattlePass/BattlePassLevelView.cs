using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô số mốc giữa 2 hàng thưởng (BattlePassLevelElementUI gốc, juf): nền theo trạng thái mốc (popup chọn sprite).
// Reskin batch2 (user yêu cầu): ghi rõ màn thay số level — tên độ khó + chương-màn như thanh trên lobby, VD "Normal 3-5".
public class BattlePassLevelView : MonoBehaviour
{
    private const int TIER_SIZE_PERCENT = 80;

    public TMP_Text LevelText;
    public Image BackgroundImage;

    public void Setup(int level, Sprite background)
    {
        LevelText.text = StaticCampaignData.GetLevelTextTwoLines(level, TIER_SIZE_PERCENT);
        BackgroundImage.sprite = background;
    }
}
