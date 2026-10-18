using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô số mốc giữa 2 hàng thưởng (BattlePassLevelElementUI gốc, juf): nền theo trạng thái mốc (popup chọn sprite).
public class BattlePassLevelView : MonoBehaviour
{
    public TMP_Text LevelText;
    public Image BackgroundImage;

    public void Setup(int level, Sprite background)
    {
        LevelText.text = level.ToString();
        BackgroundImage.sprite = background;
    }
}
