using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup nhận thưởng cuối đợt (BossRushClaimPopup gốc): mở lên gọi claimBossRushRewards → "Boss Rush Results",
// "Placement: {0}", tier cũ → tier mới, danh sách thưởng (đã cộng vào túi). Lỗi → ErrorText + Retry.
public class UIBossRushClaimPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtRank;
    public TMP_Text txtCurrentTier;
    public TMP_Text txtNextTier;
    public Transform rewardsContent;
    public TMP_Text rewardTextPrefab;
    public RewardIconSet rewardIcons;
    public Button btClaim;
    public Button btClose;
    public Button btRetry;
    public TMP_Text txtError;
    public GameObject objLoaded;
    public GameObject objLoading;

    private string poolId;
    private readonly List<GameObject> spawned = new List<GameObject>();
    private float currentTierY;
    private float nextTierY;
    private Transform gemRow;
    private int gemAmount;

    public static void Open(string poolId)
    {
        UIBossRushClaimPopup popup = UIManager.Instance.LoadUI(UIKey.BossRushClaimPopup, isBackable: false) as UIBossRushClaimPopup;
        if (popup != null) popup.Claim(poolId);
    }

    protected override void Awake()
    {
        base.Awake();
        currentTierY = txtCurrentTier.rectTransform.anchoredPosition.y;
        nextTierY = txtNextTier.rectTransform.anchoredPosition.y;
        btClaim.onClick.AddListener(OnClickClose);
        if (btClose != null) btClose.onClick.AddListener(OnClickClose);
        if (btRetry != null) btRetry.onClick.AddListener(() => Claim(poolId));
    }

    public void Claim(string id)
    {
        poolId = id;
        gemAmount = 0;
        if (txtTitle != null) txtTitle.text = Loc.L("Popup.BossRushClaim.Title");
        SetState(loading: true, error: false);

        BossRushController.Instance.Claim(poolId, (res, error) =>
        {
            if (this == null) return;
            if (error != null || res == null)
            {
                SetState(loading: false, error: true);
                if (txtError != null) txtError.text = error ?? Loc.L("Popup.BossRushClaim.ClaimFailed");
                return;
            }

            SetState(loading: false, error: false);
            StaticBossRushData data = GameData.staticData.bossRush;
            if (txtRank != null) txtRank.text = "Placement: " + res.rank;
            ShowTier(data.GetTierName(res.tier), data.GetTierName(res.newTier), res.newTier != res.tier);
            UIPvPRewardsPopup.FillRewards(res.rewards, rewardsContent, rewardTextPrefab, rewardIcons, spawned);
            for (int i = 0; i < spawned.Count; i++)
            {
                if (res.rewards[i].Type != RewardType.Gem) continue;
                gemRow = spawned[i].transform;
                gemAmount = res.rewards[i].Amount;
            }
        });
    }

    // Đóng popup mới cập nhật số trên lobby; có gem thì cho gem bay từ dòng thưởng vào ô gem.
    private void OnClickClose()
    {
        ShopUIUtil.RefreshLobby();
        if (gemAmount > 0) GameController.Instance.uiLobby.PlayGemFly(gemRow, gemAmount);
        gemAmount = 0;
        Close();
    }

    // Giữ league: chỉ hiện tên league. Đổi league: tên cũ trượt xuống khỏi khung, tên mới trượt từ trên vào.
    private void ShowTier(string current, string next, bool changed)
    {
        float step = nextTierY - currentTierY;
        txtCurrentTier.text = current;
        txtNextTier.text = next;
        Slide(txtCurrentTier.rectTransform, currentTierY, changed ? currentTierY - step : currentTierY);
        Slide(txtNextTier.rectTransform, nextTierY, changed ? currentTierY : nextTierY);
    }

    private void Slide(RectTransform rect, float fromY, float toY)
    {
        rect.DOKill();
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, fromY);
        if (fromY != toY) rect.DOAnchorPosY(toY, 0.4f).SetDelay(0.6f).SetUpdate(true);
    }

    private void SetState(bool loading, bool error)
    {
        if (objLoading != null) objLoading.SetActive(loading);
        if (objLoaded != null) objLoaded.SetActive(!loading && !error);
        if (txtError != null) txtError.gameObject.SetActive(error);
        if (btRetry != null) btRetry.gameObject.SetActive(error);
        btClaim.gameObject.SetActive(!loading && !error);
    }
}
