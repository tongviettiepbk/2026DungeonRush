using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup kết quả trận dungeon (DungeonEndPopup.kcd gốc):
//   thắng → "Completed" + thưởng "<sprite=0>{n}" (sprite asset theo dungeon), thua → "Defeat" + "Better luck next time!".
// Bấm nút → đóng popup rồi gọi onClose (DungeonMode về lại campaign).
public class UIDungeonEndPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtInfo;
    public Button btOk;

    [Space(20)]
    // Sprite asset icon thưởng theo DungeonType (index 0 DragonBoss, 1 ZombieHorde, 2 Cultist). Trống → chỉ hiện số.
    public TMP_SpriteAsset[] rewardSpriteAssets;

    private Action onClose;

    protected override void Awake()
    {
        base.Awake();
        btOk.onClick.AddListener(OnClickOk);
    }

    public void Show(bool isWin, DungeonType type, int reward, Action onClose)
    {
        this.onClose = onClose;

        if (isWin)
        {
            txtTitle.text = "Completed";
            TMP_SpriteAsset spriteAsset = GetSpriteAsset(type);
            if (spriteAsset != null)
            {
                txtInfo.spriteAsset = spriteAsset;
                txtInfo.text = "<sprite=0>" + reward;
            }
            else
            {
                txtInfo.text = "+" + reward;
            }
        }
        else
        {
            txtTitle.text = "Defeat";
            txtInfo.text = "Better luck next time!";
        }
    }

    private TMP_SpriteAsset GetSpriteAsset(DungeonType type)
    {
        int index = (int)type;
        return rewardSpriteAssets != null && index < rewardSpriteAssets.Length ? rewardSpriteAssets[index] : null;
    }

    private void OnClickOk()
    {
        Close();
        onClose?.Invoke();
    }
}
