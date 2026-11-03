using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup "Season Complete!" (SpecialPvPSeasonClaimPopup gốc): hạng cuối, league mới nếu đổi, số cúp, thưởng theo league
// của mùa vừa xong. Bấm Claim mới gọi server và cộng thưởng; lỗi thì giữ popup để bấm lại.
public class UIPvPSeasonClaimPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtRank;
    public TMP_Text txtLeagueResult;
    public TMP_Text txtTrophy;
    public Transform rewardsContent;
    public TMP_Text rewardTextPrefab;
    public RewardIconSet rewardIcons;
    public Button btClaim;
    public TMP_Text txtClaim;
    public TMP_Text txtError;
    public GameObject objLoading;

    private readonly List<GameObject> spawned = new List<GameObject>();
    private System.Action onClosed;
    private Transform gemRow;
    private int gemAmount;

    public static void Open(PvPSeasonResultDTO result, System.Action onClosed)
    {
        UIPvPSeasonClaimPopup popup = UIManager.Instance.LoadUI(UIKey.PvPSeasonClaimPopup, isBackable: false) as UIPvPSeasonClaimPopup;
        if (popup != null) popup.Show(result, onClosed);
    }

    protected override void Awake()
    {
        base.Awake();
        btClaim.onClick.AddListener(OnClickClaim);
        if (rewardTextPrefab != null) rewardTextPrefab.gameObject.SetActive(false);
    }

    public void Show(PvPSeasonResultDTO result, System.Action onClosed)
    {
        this.onClosed = onClosed;
        StaticPvPData data = GameData.staticData.pvp;
        if (txtTitle != null) txtTitle.text = Loc.L("Popup.PvP.Season.Complete");
        if (txtRank != null) txtRank.text = string.Format(Loc.L("Popup.PvP.Season.FinalRank"), result.finalRank);
        if (txtLeagueResult != null) txtLeagueResult.text = LeagueText(data, result);
        if (txtTrophy != null) txtTrophy.text = "<sprite=0>" + result.trophy;
        if (txtClaim != null) txtClaim.text = Loc.L(result.rewards != null && result.rewards.Count > 0 ? "Common.Claim" : "Common.Continue");
        if (txtError != null) txtError.gameObject.SetActive(false);
        if (objLoading != null) objLoading.SetActive(false);
        btClaim.interactable = true;

        UIPvPRewardsPopup.FillRewards(result.rewards, rewardsContent, rewardTextPrefab, rewardIcons, spawned);
        gemRow = null;
        gemAmount = 0;
        for (int i = 0; i < spawned.Count; i++)
        {
            if (result.rewards[i].Type != RewardType.Gem) continue;
            gemRow = spawned[i].transform;
            gemAmount = result.rewards[i].Amount;
        }
    }

    private static string LeagueText(StaticPvPData data, PvPSeasonResultDTO result)
    {
        PvPLeagueDefinition league = data.GetLeagueByIndex(result.newLeagueIndex);
        string name = "<color=#" + ColorUtility.ToHtmlStringRGB(league.leagueColor) + ">" + data.GetLeagueName(result.newLeagueIndex) + "</color>";
        if (result.newLeagueIndex > result.leagueIndex) return string.Format(Loc.L("Popup.PvP.Season.Promoted"), name);
        if (result.newLeagueIndex < result.leagueIndex) return string.Format(Loc.L("Popup.PvP.Season.Demoted"), name);
        return name;
    }

    private void OnClickClaim()
    {
        btClaim.interactable = false;
        if (objLoading != null) objLoading.SetActive(true);
        if (txtError != null) txtError.gameObject.SetActive(false);

        PvPController.Instance.ClaimSeason((rewards, error) =>
        {
            if (this == null) return;
            if (objLoading != null) objLoading.SetActive(false);
            if (error != null)
            {
                btClaim.interactable = true;
                if (txtError != null)
                {
                    txtError.gameObject.SetActive(true);
                    txtError.text = error;
                }
                return;
            }

            ShopUIUtil.RefreshLobby();
            if (gemAmount > 0) GameController.Instance.uiLobby.PlayGemFly(gemRow, gemAmount);
            System.Action callback = onClosed;
            onClosed = null;
            Close();
            callback?.Invoke();
        });
    }
}
