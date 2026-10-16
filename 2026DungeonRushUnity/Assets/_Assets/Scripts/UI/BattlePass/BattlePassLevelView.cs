using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô số mốc giữa 2 hàng thưởng (BattlePassLevelElementUI gốc, juf): chưa tới mốc thì nền tối.
public class BattlePassLevelView : MonoBehaviour
{
    public TMP_Text LevelText;
    public Image BackgroundImage;

    private Color reachedColor;
    private bool isColorCached;

    public void Setup(int level, Color notReachedColor)
    {
        if (isColorCached == false)
        {
            isColorCached = true;
            reachedColor = BackgroundImage.color;
        }
        LevelText.text = level.ToString();
        BackgroundImage.color = BattlePassService.IsReached(level) ? reachedColor : notReachedColor;
    }
}
