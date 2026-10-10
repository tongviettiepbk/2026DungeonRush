using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup cuối trận PvP (PvPEndPopup gốc): "Victory!" / "Defeated", "<sprite=0>{trophy mới} <color=#E3EC42|#FF4444>(+delta)</color>",
// danh sách thưởng "<sprite=0>{số}" (icon theo loại), nút Claim (cộng thưởng). Báo kết quả lỗi → "Failed to report battle."
// + Retry (gửi lại PendingPvPReport) / Close.
public class UIPvPEndPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtTrophy;
    public Transform rewardContent;
    public TMP_Text rewardTextPrefab;
    public Button btClaim;
    public TMP_Text txtClaim;
    public Button btRetry;
    public Button btClose;
    public GameObject objLoading;
    public GameObject objResult;
    public TMP_Text txtError;
    public RewardIconSet rewardIcons;

    private readonly List<GameObject> rewardRows = new List<GameObject>();
    private Action onClose;
    private bool won;

    protected override void Awake()
    {
        base.Awake();
        btClaim.onClick.AddListener(OnClickClaim);
        if (btRetry != null) btRetry.onClick.AddListener(OnClickRetry);
        if (btClose != null) btClose.onClick.AddListener(OnClickClose);
        if (rewardTextPrefab != null) rewardTextPrefab.gameObject.SetActive(false);
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
            ShowError(string.IsNullOrEmpty(error) ? "Failed to report battle." : error);
        }
    }

    // jpf/jpg.
    private void ShowResult(PendingPvPRewardSettlement s)
    {
        if (txtTitle != null) txtTitle.text = s.Won ? "Victory!" : "Defeated";
        if (txtError != null) txtError.gameObject.SetActive(false);
        if (objResult != null) objResult.SetActive(true);

        if (txtTrophy != null)
        {
            string color = s.TrophyDelta >= 0 ? "#E3EC42" : "#FF4444";
            string sign = s.TrophyDelta > 0 ? "+" : string.Empty;
            txtTrophy.text = "<sprite=0>" + s.NewTrophy + " <color=" + color + ">(" + sign + s.TrophyDelta + ")</color>";
        }

        UIPvPRewardsPopup.FillRewards(s.Rewards, rewardContent, rewardTextPrefab, rewardIcons, rewardRows);

        btClaim.gameObject.SetActive(true);
        btClaim.interactable = true;
        if (txtClaim != null) txtClaim.text = "Claim";
        if (btRetry != null) btRetry.gameObject.SetActive(false);
        if (btClose != null) btClose.gameObject.SetActive(false);
    }

    // jpc: lỗi báo kết quả.
    private void ShowError(string message)
    {
        if (txtTitle != null) txtTitle.text = won ? "Victory!" : "Defeated";
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
                ShowError(res != null && !string.IsNullOrEmpty(res.message) ? res.message : "Failed to report battle.");
            }
        });
    }

    private void OnClickClaim()
    {
        btClaim.interactable = false;
        PvPController.Instance.ClaimSettlement();
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
