using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tab Battle của Clan = Clan War (ClanBattleTab gốc). Chưa có war → thẻ "Waiting For War" + đếm ngược tuần sau.
// Có war: thanh tiến độ (trừ cooldown) + "Day N Actions" (ngày 1-5) / "Day 6 PvP" (ngày 6) / kết quả + nhận thưởng (cooldown).
// Tự tải lại mỗi 20s, đếm ngược mỗi giây. Nút Rewards / Days & Results / Leaderboard.
public class ClanBattleTab : MonoBehaviour
{
    public ClanWarProgressSection ProgressSection;
    public ClanWarDayActionsSection DayActionsSection;
    public ClanWarPvpSection PvpSection;
    public ClanWarResultSummary ResultSummary;
    public ClanWarRewardClaimCards ClaimCards;
    public GameObject WaitingCardRoot;
    public ClanLoadingView LoadingView;
    public Button RewardsButton;
    public Button DaysResultsButton;
    public Button LeaderboardButton;
    public TMP_Text TitleText;
    public TMP_Text WaitingTitleText;
    public TMP_Text WaitingBodyText;
    public TMP_Text WaitingCountdownText;
    public TMP_Text RewardsButtonText;
    public TMP_Text DaysResultsButtonText;
    public TMP_Text LeaderboardButtonText;

    private float refreshTimer;
    private float tickTimer;
    private bool isVisible;
    private bool hasData;
    private float serverOffset;      // serverTime - giờ máy

    private static ClanWarController Controller => ClanWarController.Instance;

    private void Awake()
    {
        ClanUIUtil.SetText(WaitingTitleText, "Waiting For War");
        ClanUIUtil.SetText(WaitingBodyText, "Your clan will be matched when the next war week begins.");
        ClanUIUtil.SetText(RewardsButtonText, "Rewards");
        ClanUIUtil.SetText(DaysResultsButtonText, "Days & Results");
        ClanUIUtil.SetText(LeaderboardButtonText, "Leaderboard");
        if (RewardsButton != null) RewardsButton.onClick.AddListener(() => UIManager.Instance.LoadUI(UIKey.ClanWarRewardsPopup));
        if (DaysResultsButton != null) DaysResultsButton.onClick.AddListener(() => UIManager.Instance.LoadUI(UIKey.ClanWarDaysAndResultsPopup));
        if (LeaderboardButton != null) LeaderboardButton.onClick.AddListener(() => UIManager.Instance.LoadUI(UIKey.ClanWarLeaderboardPopup));
        ClanWarController.WarStateUpdated += OnStateUpdated;
    }

    private void OnDestroy()
    {
        ClanWarController.WarStateUpdated -= OnStateUpdated;
    }

    private void OnDisable()
    {
        isVisible = false;
    }

    // Show gốc.
    public void Show()
    {
        gameObject.SetActive(true);
        isVisible = true;
        refreshTimer = 0f;
        if (!hasData)
        {
            HideAll();
            if (LoadingView != null) LoadingView.ShowLoading();
        }
        Load();
    }

    private void Load()
    {
        Controller.FetchState(true, s =>
        {
            if (this == null) return;
            if (s == null && !hasData && LoadingView != null)
            {
                LoadingView.ShowError("Failed to load clan war data.", Load);
            }
        });
    }

    private void Update()
    {
        if (!isVisible) return;
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= StaticClanData.BATTLE_TAB_REFRESH)
        {
            refreshTimer = 0f;
            Controller.FetchState(true, null, false);
        }
        tickTimer += Time.unscaledDeltaTime;
        if (tickTimer >= 1f)
        {
            tickTimer = 0f;
            Tick();
        }
    }

    // fte/ftg gốc.
    private void OnStateUpdated(ClanWarStateResponseDTO s)
    {
        if (this == null || s == null || !gameObject.activeInHierarchy) return;
        hasData = true;
        serverOffset = s.serverTime > 0 ? s.serverTime - ClanController.Now() : 0;
        if (LoadingView != null) LoadingView.Hide();
        Draw(s);
    }

    private void Draw(ClanWarStateResponseDTO s)
    {
        ClanWarWireDTO war = s.war;
        bool waiting = war == null || war.IsConcluded;
        ClanUIUtil.SetText(TitleText, war != null && war.IsLeadership ? "Championship War" : "Clan War");
        ClanUIUtil.SetActive(WaitingCardRoot, waiting);
        ClanUIUtil.SetActive(ProgressSection, !waiting && !war.IsCooldown);
        ClanUIUtil.SetActive(DayActionsSection, !waiting && war.IsDay);
        ClanUIUtil.SetActive(PvpSection, !waiting && war.IsDay6);
        ClanUIUtil.SetActive(ResultSummary, !waiting && war.IsCooldown);
        ClanUIUtil.SetActive(ClaimCards, !waiting && war.IsCooldown);
        ClanUIUtil.SetActive(RewardsButton, !waiting);
        ClanUIUtil.SetActive(DaysResultsButton, !waiting);
        ClanUIUtil.SetActive(LeaderboardButton, true);
        if (!waiting)
        {
            ClanWarConfigDTO config = Controller.Config;
            if (!war.IsCooldown && ProgressSection != null) ProgressSection.Set(war, OpenContribution, OpenContribution);
            if (war.IsDay && DayActionsSection != null) DayActionsSection.Set(war, config);
            if (war.IsDay6 && PvpSection != null) PvpSection.Set(war, config);
            if (war.IsCooldown)
            {
                if (ResultSummary != null) ResultSummary.Set(war);
                if (ClaimCards != null) ClaimCards.Set(war, config);
            }
        }
        Tick();
    }

    // fth gốc: đếm ngược.
    private void Tick()
    {
        ClanWarStateResponseDTO s = Controller.State;
        if (s == null) return;
        long now = ClanController.Now() + (long)serverOffset;
        ClanWarWireDTO war = s.war;
        if (war == null || war.IsConcluded)
        {
            ClanUIUtil.SetText(WaitingCountdownText, "Starts in " + ClanWarController.FormatCountdown(s.nextWeekStartsAt - now));
            return;
        }
        if (war.IsDay && DayActionsSection != null) DayActionsSection.SetTimeLeft(war.dayEndsAt - now);
        if (war.IsDay6 && PvpSection != null) PvpSection.SetTimeLeft(war.dayEndsAt - now);
        if (war.IsCooldown && ResultSummary != null) ResultSummary.SetTimeLeft(war.cooldownEndsAt - now);
        if (now >= war.dayEndsAt && !war.IsCooldown) refreshTimer = StaticClanData.BATTLE_TAB_REFRESH;   // qua ngày → tải lại
    }

    // ftk/ftl gốc: bấm banner → bảng đóng góp clan đó.
    private void OpenContribution(ClanWarClanSideDTO side)
    {
        UIClanContributionLeaderboardPopup popup = UIManager.Instance.LoadUI(UIKey.ClanContributionLeaderboardPopup) as UIClanContributionLeaderboardPopup;
        if (popup != null) popup.Setup(side);
    }

    // ftj gốc.
    private void HideAll()
    {
        ClanUIUtil.SetActive(WaitingCardRoot, false);
        ClanUIUtil.SetActive(ProgressSection, false);
        ClanUIUtil.SetActive(DayActionsSection, false);
        ClanUIUtil.SetActive(PvpSection, false);
        ClanUIUtil.SetActive(ResultSummary, false);
        ClanUIUtil.SetActive(ClaimCards, false);
        ClanUIUtil.SetActive(RewardsButton, false);
        ClanUIUtil.SetActive(DaysResultsButton, false);
        ClanUIUtil.SetActive(LeaderboardButton, false);
    }
}
