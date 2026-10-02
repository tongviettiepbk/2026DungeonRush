using System.Collections.Generic;
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
    public TMP_Text txtRankName;
    public Transform rewardsContent;
    public TMP_Text rewardTextPrefab;
    public Button btClaim;
    public Button btClose;
    public Button btRetry;
    public TMP_Text txtError;
    public GameObject objLoaded;
    public GameObject objLoading;

    private string poolId;
    private readonly List<TMP_Text> spawned = new List<TMP_Text>();

    public static void Open(string poolId)
    {
        UIBossRushClaimPopup popup = UIManager.Instance.LoadUI(UIKey.BossRushClaimPopup, isBackable: false) as UIBossRushClaimPopup;
        if (popup != null) popup.Claim(poolId);
    }

    protected override void Awake()
    {
        base.Awake();
        btClaim.onClick.AddListener(Close);
        if (btClose != null) btClose.onClick.AddListener(Close);
        if (btRetry != null) btRetry.onClick.AddListener(() => Claim(poolId));
    }

    public void Claim(string id)
    {
        poolId = id;
        if (txtTitle != null) txtTitle.text = "Boss Rush Results";
        SetState(loading: true, error: false);

        BossRushController.Instance.Claim(poolId, (res, error) =>
        {
            if (this == null) return;
            if (error != null || res == null)
            {
                SetState(loading: false, error: true);
                if (txtError != null) txtError.text = error ?? "Failed to claim rewards. Please try again.";
                return;
            }

            SetState(loading: false, error: false);
            StaticBossRushData data = GameData.staticData.bossRush;
            if (txtRank != null) txtRank.text = "Placement: " + res.rank;
            if (txtCurrentTier != null) txtCurrentTier.text = data.GetTierName(res.tier);
            if (txtNextTier != null) txtNextTier.text = data.GetTierName(res.newTier);
            if (txtRankName != null) txtRankName.text = res.promoted ? "Promoted!" : res.demoted ? "Demoted" : data.GetTierName(res.newTier);
            ShowRewards(res.rewards);
            GameController.Instance.uiLobby.Refresh();
        });
    }

    private void ShowRewards(List<RewardEntry> rewards)
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i].gameObject);
        }
        spawned.Clear();
        if (rewards == null || rewardsContent == null || rewardTextPrefab == null) return;

        for (int i = 0; i < rewards.Count; i++)
        {
            TMP_Text line = Instantiate(rewardTextPrefab, rewardsContent);
            line.gameObject.SetActive(true);
            line.text = rewards[i].Type + " x" + rewards[i].Amount;
            spawned.Add(line);
        }
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
