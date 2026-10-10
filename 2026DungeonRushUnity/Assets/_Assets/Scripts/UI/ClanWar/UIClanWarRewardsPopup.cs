using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "War Rewards" (ClanWarRewardsPopup gốc): thưởng clan theo tier (Win / Lose), track mốc cá nhân theo điểm tuần
// (thanh tiến độ + thẻ mốc Claim), "Your Score: {0}"; trong cooldown hiện 2 thẻ Collect Clan / Remaining Personal.
public class UIClanWarRewardsPopup : BaseUI
{
    public Button CloseButton;
    public ClanWarRewardsView WinRewardsView;
    public ClanWarRewardsView LoseRewardsView;
    public ClanWarMilestoneCard MilestoneCardPrefab;
    public ScrollRect TrackScrollRect;
    public Transform TrackContent;
    public RectTransform TrackProgressBackgroundRect;
    public RectTransform TrackProgressFillRect;
    public ClanLoadingView ClaimLoadingView;
    public RectTransform CollectFlyTarget;
    public Sprite LootBoxIcon;
    public Sprite BonesIcon;
    public Sprite PickaxeIcon;
    public Sprite ExperienceIcon;
    public Sprite CloakCurrencyIcon;
    public Sprite GoldenPickaxeIcon;
    public Sprite DrillIcon;
    public Sprite VialIcon;
    public GameObject CollectClanCardRoot;
    public ClanWarRewardsView CollectClanRewardsView;
    public Button CollectClanButton;
    public GameObject CollectClanClaimedRoot;
    public GameObject CollectPersonalCardRoot;
    public ClanWarRewardsView CollectPersonalRewardsView;
    public Button CollectPersonalButton;
    public GameObject CollectPersonalClaimedRoot;
    public TMP_Text TitleText;
    public TMP_Text TierHeaderText;
    public TMP_Text WinCardLabelText;
    public TMP_Text LoseCardLabelText;
    public TMP_Text CollectClanTitleText;
    public TMP_Text CollectPersonalTitleText;
    public TMP_Text CollectClanCardText;
    public TMP_Text CollectPersonalCardText;
    public TMP_Text CollectClanClaimedText;
    public TMP_Text CollectPersonalClaimedText;
    public TMP_Text DelayInfoText;
    public TMP_Text MyScoreText;

    private readonly List<ClanWarMilestoneCard> cards = new List<ClanWarMilestoneCard>();
    private bool isBusy;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "War Rewards");
        ClanUIUtil.SetText(WinCardLabelText, "Win");
        ClanUIUtil.SetText(LoseCardLabelText, "Lose");
        ClanUIUtil.SetText(CollectClanTitleText, "Clan Rewards");
        ClanUIUtil.SetText(CollectPersonalTitleText, "Personal Rewards");
        ClanUIUtil.SetText(CollectClanCardText, "Collect Clan Reward");
        ClanUIUtil.SetText(CollectPersonalCardText, "Collect Remaining Personal Rewards");
        ClanUIUtil.SetText(CollectClanClaimedText, "Claimed");
        ClanUIUtil.SetText(CollectPersonalClaimedText, "Claimed");
        ClanUIUtil.SetText(DelayInfoText, "Updates every minute.");
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (CollectClanButton != null) CollectClanButton.onClick.AddListener(OnCollectClan);
        if (CollectPersonalButton != null) CollectPersonalButton.onClick.AddListener(OnCollectPersonal);
        if (MilestoneCardPrefab != null) MilestoneCardPrefab.gameObject.SetActive(false);
    }

    // eip/gea gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        isBusy = false;
        if (ClaimLoadingView != null) ClaimLoadingView.Hide();
        ClanWarController.Instance.FetchState(false, s =>
        {
            if (this != null && s?.war != null) Draw(s.war, ClanWarController.Instance.Config);
        });
    }

    // geb/gec gốc.
    private void Draw(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        string tier = war.myClan != null ? war.myClan.rewardTier : "D";
        ClanUIUtil.SetText(TierHeaderText, "Tier " + ClanRoleExt.TierText(tier) + " Rewards");
        if (WinRewardsView != null) WinRewardsView.SetRewards(ClanWarController.ClanReward(config, tier, "win"));
        if (LoseRewardsView != null) LoseRewardsView.SetRewards(ClanWarController.ClanReward(config, tier, "lose"));
        int score = war.milestone != null ? war.milestone.weeklyContribution : war.myWeeklyPoints;
        ClanUIUtil.SetText(MyScoreText, "Your Score: " + ClanWarController.FormatNumber(score));
        DrawTrack(war, config);
        DrawCollect(war, config);
    }

    // ged/gee gốc.
    private void DrawTrack(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
        List<ClanWarMilestoneRowDTO> rows = config?.individualMilestones ?? new List<ClanWarMilestoneRowDTO>();
        ClanWarMilestoneDTO m = war.milestone ?? new ClanWarMilestoneDTO();
        for (int i = 0; i < rows.Count; i++)
        {
            ClanWarMilestoneRowDTO row = rows[i];
            ClanWarMilestoneCard card = Instantiate(MilestoneCardPrefab, TrackContent);
            card.gameObject.SetActive(true);
            card.Set(row, StateOf(row, m, war), () => OnClaimMilestone(row, card));
            cards.Add(card);
        }
        SetProgress(Progress(m, rows));
    }

    private static ClanWarMilestoneCard.State StateOf(ClanWarMilestoneRowDTO row, ClanWarMilestoneDTO m, ClanWarWireDTO war)
    {
        bool claimed = m.claimedMilestones != null && m.claimedMilestones.Contains(row.threshold);
        if (claimed) return ClanWarMilestoneCard.State.Collected;
        if (m.weeklyContribution < row.threshold) return ClanWarMilestoneCard.State.Unreached;
        return war.IsCooldown ? ClanWarMilestoneCard.State.ReachedInBundle : ClanWarMilestoneCard.State.ReachedClaimable;
    }

    // gee/geg gốc: tiến độ chia đều theo đoạn giữa các mốc.
    private static float Progress(ClanWarMilestoneDTO m, List<ClanWarMilestoneRowDTO> rows)
    {
        if (rows.Count == 0) return 0f;
        int score = m.weeklyContribution;
        int prev = 0;
        for (int i = 0; i < rows.Count; i++)
        {
            if (score < rows[i].threshold)
            {
                float seg = (float)(score - prev) / Mathf.Max(1, rows[i].threshold - prev);
                return (i + seg) / rows.Count;
            }
            prev = rows[i].threshold;
        }
        return 1f;
    }

    // gef gốc.
    private void SetProgress(float t)
    {
        if (TrackProgressBackgroundRect == null || TrackProgressFillRect == null) return;
        bool vertical = TrackProgressBackgroundRect.rect.height > TrackProgressBackgroundRect.rect.width;
        RectTransform.Axis axis = vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal;
        float size = vertical ? TrackProgressBackgroundRect.rect.height : TrackProgressBackgroundRect.rect.width;
        TrackProgressFillRect.SetSizeWithCurrentAnchors(axis, size * Mathf.Clamp01(t));
    }

    private void DrawCollect(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        bool cooldown = war.IsCooldown;
        ClanUIUtil.SetActive(CollectClanCardRoot, cooldown);
        ClanUIUtil.SetActive(CollectPersonalCardRoot, cooldown);
        if (!cooldown) return;
        ClanWarClaimsDTO claims = war.claims ?? new ClanWarClaimsDTO();
        ClanWarRewardsDTO clan = claims.clanReward ?? ClanWarController.ClanReward(config, war.myClan?.rewardTier, claims.outcome);
        if (CollectClanRewardsView != null) CollectClanRewardsView.SetRewards(clan);
        ClanUIUtil.SetActive(CollectClanButton, !claims.clanRewardClaimed);
        if (CollectClanButton != null) CollectClanButton.interactable = claims.clanRewardEligible && clan != null && !clan.IsEmpty;
        ClanUIUtil.SetActive(CollectClanClaimedRoot, claims.clanRewardClaimed);

        ClanWarRewardsDTO personal = ClanWarController.RemainingPersonalRewards(war.milestone, config);
        bool bundleClaimed = war.milestone != null && war.milestone.bundleClaimed;
        if (CollectPersonalRewardsView != null) CollectPersonalRewardsView.SetRewards(personal);
        ClanUIUtil.SetActive(CollectPersonalButton, !bundleClaimed);
        if (CollectPersonalButton != null) CollectPersonalButton.interactable = !personal.IsEmpty;
        ClanUIUtil.SetActive(CollectPersonalClaimedRoot, bundleClaimed);
    }

    // gel/geo gốc.
    private void OnClaimMilestone(ClanWarMilestoneRowDTO row, ClanWarMilestoneCard card)
    {
        if (isBusy) return;
        isBusy = true;
        card.SetBusy(true);
        ClanWarController.Instance.ClaimMilestone(row.threshold, OnClaimed);
    }

    // geq gốc.
    private void OnCollectClan()
    {
        if (isBusy) return;
        isBusy = true;
        if (ClaimLoadingView != null) ClaimLoadingView.ShowLoading();
        ClanWarController.Instance.ClaimClanReward(OnClaimed);
    }

    // ger gốc.
    private void OnCollectPersonal()
    {
        if (isBusy) return;
        isBusy = true;
        if (ClaimLoadingView != null) ClaimLoadingView.ShowLoading();
        ClanWarController.Instance.ClaimPersonalBundle(OnClaimed);
    }

    private void OnClaimed(bool ok, string error, ClanWarRewardsDTO rewards)
    {
        isBusy = false;
        if (ClaimLoadingView != null) ClaimLoadingView.Hide();
        ClanUIUtil.Toast(ok ? "Rewards claimed!" : error);
        ClanWarStateResponseDTO s = ClanWarController.Instance.State;
        if (this != null && s?.war != null) Draw(s.war, ClanWarController.Instance.Config);
    }
}
