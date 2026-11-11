using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup cuối trận PvP: "Victory!" / "Defeated", "<sprite=0>{cúp mới} <color=#E3EC42>(+delta)</color>", nút Continue.
// PvP không có thưởng theo trận, thưởng trả cuối mùa. Báo kết quả lỗi → "Failed to report battle." + Retry / Close.
public class UIPvPEndPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtTrophy;
    public Button btClaim;
    public TMP_Text txtClaim;
    public Button btRetry;
    public Button btClose;
    public GameObject objLoading;
    public GameObject objResult;
    public TMP_Text txtError;
    // Reskin (mockup): tiêu đề Defeat màu đỏ.
    public Color victoryTitleColor = Color.white;
    public Color defeatTitleColor = new Color(1f, 0.29f, 0.25f, 1f);
    // Reskin Đợt 6: Continue xanh lá khi thắng, tím khi thua (như popup kết quả Dungeon).
    public Sprite sprContinueWin;
    public Sprite sprContinueLose;

    private Action onClose;
    private bool won;

    protected override void Awake()
    {
        base.Awake();
        btClaim.onClick.AddListener(OnClickClaim);
        if (btRetry != null) btRetry.onClick.AddListener(OnClickRetry);
        if (btClose != null) btClose.onClick.AddListener(OnClickClose);
    }

    // settlement null + error → hiện lỗi (báo server thất bại).
    public void Show(bool won, PendingPvPRewardSettlement settlement, string error, Action onClose)
    {
        this.won = won;
        this.onClose = onClose;
        if (objLoading != null) objLoading.SetActive(false);
        if (settlement != null)
        {
            ShowResult(settlement);
        }
        else
        {
            ShowError(string.IsNullOrEmpty(error) ? Loc.L("Errors.PvP.ReportBattleFailed") : error);
        }
    }

    // jpf/jpg.
    private void ShowResult(PendingPvPRewardSettlement s)
    {
        SetTitle(s.Won);
        if (txtError != null) txtError.gameObject.SetActive(false);
        if (objResult != null) objResult.SetActive(true);

        if (txtTrophy != null)
        {
            string sign = s.TrophyDelta > 0 ? "+" : string.Empty;
            txtTrophy.text = "<sprite=0>" + s.NewTrophy + " <color=#E3EC42>(" + sign + s.TrophyDelta + ")</color>";
        }

        btClaim.gameObject.SetActive(true);
        btClaim.interactable = true;
        if (txtClaim != null) txtClaim.text = Loc.L("Common.Continue");
        Sprite claimSprite = s.Won ? sprContinueWin : sprContinueLose;
        if (claimSprite != null) btClaim.image.sprite = claimSprite;
        if (btRetry != null) btRetry.gameObject.SetActive(false);
        if (btClose != null) btClose.gameObject.SetActive(false);
    }

    // jpc: lỗi báo kết quả.
    private void ShowError(string message)
    {
        SetTitle(won);
        if (objResult != null) objResult.SetActive(false);
        if (txtError != null)
        {
            txtError.gameObject.SetActive(true);
            txtError.text = message;
        }
        btClaim.gameObject.SetActive(false);
        if (btRetry != null) btRetry.gameObject.SetActive(PvPController.Instance.HasPendingReport);
        if (btClose != null) btClose.gameObject.SetActive(true);
    }

    private void SetTitle(bool isWon)
    {
        if (txtTitle == null) return;
        txtTitle.text = isWon ? Loc.L("Popup.PvP.Victory") : Loc.L("Popup.PvP.Defeat");
        txtTitle.color = isWon ? victoryTitleColor : defeatTitleColor;
    }

    private void OnClickRetry()
    {
        if (objLoading != null) objLoading.SetActive(true);
        if (btRetry != null) btRetry.interactable = false;
        PvPController.Instance.RetryPendingReport((ok, res) =>
        {
            if (this == null) return;
            if (objLoading != null) objLoading.SetActive(false);
            if (btRetry != null) btRetry.interactable = true;
            if (ok && GameData.userData.pvp.pendingSettlement != null)
            {
                ShowResult(GameData.userData.pvp.pendingSettlement);
            }
            else
            {
                ShowError(res != null && !string.IsNullOrEmpty(res.message) ? res.message : Loc.L("Errors.PvP.ReportBattleFailed"));
            }
        });
    }

    private void OnClickClaim()
    {
        btClaim.interactable = false;
        PvPController.Instance.ClearSettlement();
        Finish();
    }

    private void OnClickClose()
    {
        Finish();
    }

    private void Finish()
    {
        Close();
        Action callback = onClose;
        onClose = null;
        callback?.Invoke();
    }
}

// Bộ icon TMP cho chữ thưởng "<sprite=0>{số}" (GameResources.jge gốc: RewardType → TMP_SpriteAsset). Index = (int)RewardType.
[Serializable]
public class RewardIconSet
{
    public TMP_SpriteAsset[] spriteAssets = new TMP_SpriteAsset[12];

    public TMP_SpriteAsset Get(RewardType type)
    {
        int i = (int)type;
        return spriteAssets != null && i >= 0 && i < spriteAssets.Length ? spriteAssets[i] : null;
    }
}
