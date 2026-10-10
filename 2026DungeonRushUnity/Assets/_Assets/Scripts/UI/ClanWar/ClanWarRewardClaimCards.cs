using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 2 thẻ nhận thưởng trong cooldown (ClanWarRewardClaimCards gốc): "Personal Rewards" = các mốc đã đạt chưa nhận (gdl),
// "Clan Rewards" = thưởng clan theo tier + thắng/thua (cần đã đóng góp). Đã nhận → "Claimed".
public class ClanWarRewardClaimCards : MonoBehaviour
{
    public GameObject PersonalCardRoot;
    public ClanWarRewardsGridView PersonalRewardsView;
    public Button PersonalClaimButton;
    public ClanLoadingView PersonalClaimLoadingView;
    public GameObject PersonalClaimedRoot;
    public GameObject ClanCardRoot;
    public ClanWarRewardsGridView ClanRewardsView;
    public Button ClanClaimButton;
    public ClanLoadingView ClanClaimLoadingView;
    public GameObject ClanClaimedRoot;
    public RectTransform CollectFlyTarget;
    public Sprite LootBoxIcon;
    public Sprite BonesIcon;
    public Sprite PickaxeIcon;
    public Sprite ExperienceIcon;
    public Sprite CloakCurrencyIcon;
    public Sprite GoldenPickaxeIcon;
    public Sprite DrillIcon;
    public Sprite VialIcon;
    public TMP_Text PersonalCardTitleText;
    public TMP_Text PersonalClaimButtonText;
    public TMP_Text PersonalClaimedStateText;
    public TMP_Text ClanCardTitleText;
    public TMP_Text ClanClaimButtonText;
    public TMP_Text ClanClaimedStateText;

    private bool isBusy;

    private void Awake()
    {
        ClanUIUtil.SetText(PersonalCardTitleText, "Personal Rewards");
        ClanUIUtil.SetText(ClanCardTitleText, "Clan Rewards");
        ClanUIUtil.SetText(PersonalClaimButtonText, "Claim");
        ClanUIUtil.SetText(ClanClaimButtonText, "Claim");
        ClanUIUtil.SetText(PersonalClaimedStateText, "Claimed");
        ClanUIUtil.SetText(ClanClaimedStateText, "Claimed");
        if (PersonalClaimButton != null) PersonalClaimButton.onClick.AddListener(OnPersonalClicked);
        if (ClanClaimButton != null) ClanClaimButton.onClick.AddListener(OnClanClicked);
    }

    // gdk gốc.
    public void Set(ClanWarWireDTO war, ClanWarConfigDTO config)
    {
        if (PersonalClaimLoadingView != null) PersonalClaimLoadingView.Hide();
        if (ClanClaimLoadingView != null) ClanClaimLoadingView.Hide();

        ClanWarRewardsDTO personal = ClanWarController.RemainingPersonalRewards(war.milestone, config);
        bool personalClaimed = war.milestone != null && war.milestone.bundleClaimed;
        if (PersonalRewardsView != null) PersonalRewardsView.SetRewards(personal);
        ClanUIUtil.SetActive(PersonalClaimButton, !personalClaimed && !personal.IsEmpty);
        ClanUIUtil.SetActive(PersonalClaimedRoot, personalClaimed);
        ClanUIUtil.SetActive(PersonalCardRoot, personalClaimed || !personal.IsEmpty);

        ClanWarClaimsDTO claims = war.claims;
        ClanWarRewardsDTO clan = claims?.clanReward ?? ClanWarController.ClanReward(config, war.myClan?.rewardTier, claims?.outcome);
        bool clanClaimed = claims != null && claims.clanRewardClaimed;
        bool eligible = claims != null && claims.clanRewardEligible;
        if (ClanRewardsView != null) ClanRewardsView.SetRewards(clan);
        ClanUIUtil.SetActive(ClanClaimButton, !clanClaimed && clan != null && !clan.IsEmpty);
        if (ClanClaimButton != null) ClanClaimButton.interactable = eligible;
        ClanUIUtil.SetActive(ClanClaimedRoot, clanClaimed);
    }

    // gdm gốc.
    private void OnPersonalClicked()
    {
        if (isBusy) return;
        isBusy = true;
        if (PersonalClaimLoadingView != null) PersonalClaimLoadingView.ShowLoading();
        ClanWarController.Instance.ClaimPersonalBundle(OnClaimed);
    }

    // gdn gốc.
    private void OnClanClicked()
    {
        if (isBusy) return;
        isBusy = true;
        if (ClanClaimLoadingView != null) ClanClaimLoadingView.ShowLoading();
        ClanWarController.Instance.ClaimClanReward(OnClaimed);
    }

    private void OnClaimed(bool ok, string error, ClanWarRewardsDTO rewards)
    {
        isBusy = false;
        if (PersonalClaimLoadingView != null) PersonalClaimLoadingView.Hide();
        if (ClanClaimLoadingView != null) ClanClaimLoadingView.Hide();
        if (!ok) ClanUIUtil.Toast(error);
        else ClanUIUtil.Toast("Rewards claimed!");
        ClanWarStateResponseDTO s = ClanWarController.Instance.State;
        if (this != null && s?.war != null) Set(s.war, ClanWarController.Instance.Config);
    }
}
