using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng đóng góp 1 clan trong war (ClanContributionLeaderboardPopup gốc): tab Daily | Weekly,
// "Day {n} Leaderboard" / "Weekly Leaderboard", "Updates every minute.".
public class UIClanContributionLeaderboardPopup : BaseUI
{
    private const int MODE_DAILY = 0;
    private const int MODE_WEEKLY = 1;

    public Button CloseButton;
    public ClanBannerView Banner;
    public TMP_Text ClanNameText;
    public TabSelector ModeSelector;
    public ClanWarContributionCard CardPrefab;
    public Transform ListContent;
    public ClanLoadingView LoadingView;
    public TMP_Text SubheaderText;
    public TMP_Text DelayInfoText;

    private ClanWarClanSideDTO side;
    private int requestId;
    private readonly List<ClanWarContributionCard> cards = new List<ClanWarContributionCard>();

    protected override void Awake()
    {
        base.Awake();
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (DelayInfoText != null) DelayInfoText.text = "Updates every minute.";
        if (CardPrefab != null) CardPrefab.gameObject.SetActive(false);
        if (ModeSelector != null)
        {
            ModeSelector.Setup(new[] { "Daily", "Weekly" });
            ModeSelector.OnTabSelected += Load;
        }
    }

    // gbf + eip gốc.
    public void Setup(ClanWarClanSideDTO side)
    {
        this.side = side;
        if (Banner != null) Banner.SetBanner(side.Banner);
        ClanUIUtil.SetText(ClanNameText, side.clanName);
        if (ModeSelector != null) ModeSelector.Select(MODE_DAILY, false);
        Load(MODE_DAILY);
    }

    // gbg/gbh gốc.
    private void Load(int mode)
    {
        int id = ++requestId;
        Clear();
        ClanWarWireDTO war = ClanWarController.Instance.State?.war;
        int day = war != null ? Mathf.Clamp(war.activeDay, 1, 6) : 1;
        ClanUIUtil.SetText(SubheaderText, mode == MODE_DAILY ? "Day " + day + " Leaderboard" : "Weekly Leaderboard");
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanWarController.Instance.GetContributionLeaderboard(side.clanId, mode == MODE_DAILY ? "daily" : "weekly", res =>
        {
            if (this == null || id != requestId) return;
            if (res == null)
            {
                if (LoadingView != null) LoadingView.ShowError("Failed to load clan war data.", () => Load(mode));
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            Draw(res);
        });
    }

    // gbi gốc.
    private void Draw(ClanWarContributionLeaderboardResponseDTO res)
    {
        string me = ClanController.Instance.MyUserId;
        if (res.entries == null) return;
        for (int i = 0; i < res.entries.Count; i++)
        {
            ClanWarContributionCard card = Instantiate(CardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Set(res.entries[i], me, null);
            cards.Add(card);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
    }
}
