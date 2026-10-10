using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "War Results" (ClanWarDaysAndResultsPopup gốc): 2 clan + tỉ số War Score, "Daily Scores" = 6 thẻ ngày
// (ngày chưa chốt → "Not Concluded").
public class UIClanWarDaysAndResultsPopup : BaseUI
{
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public ClanBannerView MyBanner;
    public TMP_Text MyClanNameText;
    public TMP_Text MyTierText;
    public TMP_Text MyServerText;
    public ClanBannerView EnemyBanner;
    public TMP_Text EnemyClanNameText;
    public TMP_Text EnemyTierText;
    public TMP_Text EnemyServerText;
    public ClanWarDayCard DayCardPrefab;
    public Transform DaysContent;
    public TMP_Text TitleText;
    public TMP_Text SubtitleText;
    public TMP_Text ScoreboardText;
    public TMP_Text DailyScoresHeaderText;

    private readonly List<ClanWarDayCard> cards = new List<ClanWarDayCard>();

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "War Results");
        ClanUIUtil.SetText(SubtitleText, "War Scores earned this week");
        ClanUIUtil.SetText(DailyScoresHeaderText, "Daily Scores");
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        if (DayCardPrefab != null) DayCardPrefab.gameObject.SetActive(false);
    }

    // eip gốc: tải lại state (kèm dailyResults).
    protected override void OnEnable()
    {
        base.OnEnable();
        Load();
    }

    // gbw gốc.
    private void Load()
    {
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanWarController.Instance.FetchState(true, s =>
        {
            if (this == null) return;
            if (s?.war == null)
            {
                if (LoadingView != null) LoadingView.ShowError("Failed to load clan war data.", Load);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            Draw(s.war);
        });
    }

    // gbx gốc.
    private void Draw(ClanWarWireDTO war)
    {
        ClanWarProgressSection.SetSide(war.myClan, MyBanner, MyClanNameText, MyTierText, MyServerText);
        ClanWarProgressSection.SetSide(war.enemyClan, EnemyBanner, EnemyClanNameText, EnemyTierText, EnemyServerText);
        ClanUIUtil.SetText(ScoreboardText, war.myWarScore + " : " + war.enemyWarScore);
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
        ClanWarConfigDTO config = ClanWarController.Instance.Config;
        for (int day = 1; day <= 6; day++)
        {
            ClanWarDailyResultDTO result = war.dailyResults != null ? war.dailyResults.Find(r => r.day == day) : null;
            ClanWarDayCard card = Instantiate(DayCardPrefab, DaysContent);
            card.gameObject.SetActive(true);
            card.Set(day, DefaultScore(config, day), result, null);
            cards.Add(card);
        }
    }

    // gby gốc: War Score mặc định của ngày theo config.
    private static int DefaultScore(ClanWarConfigDTO config, int day)
    {
        if (config?.warScoreByDay != null && config.warScoreByDay.TryGetValue(day.ToString(), out int s)) return s;
        return 1;
    }
}
