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
    public GameObject objRewardLabel;   // chữ "Reward" trên ô thưởng (reskin): ẩn khi thua
    public GameObject objRewardBox;     // ô nền thưởng: ẩn khi thua
    public TMP_Text txtDefeat;          // chữ nâu "Better luck next time!" khi thua (concept dungeon_end)
    public Image imgButton;             // nền nút Okay: xanh lá khi thắng (nhận), tím khi thua
    public Sprite sprButtonWin;
    public Sprite sprButtonLose;

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
        if (objRewardLabel != null)
            objRewardLabel.SetActive(isWin);
        if (objRewardBox != null)
            objRewardBox.SetActive(isWin);
        if (txtDefeat != null)
        {
            txtDefeat.gameObject.SetActive(!isWin);
            txtInfo.gameObject.SetActive(isWin);
        }
        if (imgButton != null)
            imgButton.sprite = isWin ? sprButtonWin : sprButtonLose;

        if (isWin)
        {
            txtTitle.text = Loc.L("Popup.DungeonEnd.SuccessTitle");
            TMP_SpriteAsset spriteAsset = GetSpriteAsset(type);
            if (spriteAsset != null)
            {
                txtInfo.spriteAsset = spriteAsset;
                txtInfo.text = "<sprite=0><space=10>" + reward;
            }
            else
            {
                txtInfo.text = "+" + reward;
            }
        }
        else
        {
            txtTitle.text = Loc.L("Popup.DungeonEnd.DefeatTitle");
            TMP_Text defeatText = txtDefeat != null ? txtDefeat : txtInfo;
            defeatText.text = Loc.L("Popup.DungeonEnd.DefeatDescription");
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
