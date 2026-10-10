using TMPro;
using UnityEngine;

// Kết quả tuần trong cooldown (ClanWarResultSummary gốc): 2 clan, "War Result" / "Championship War Result",
// "Reward Cooldown", Win/Loss, tỉ số War Score, "New War Starts in: {0}".
public class ClanWarResultSummary : MonoBehaviour
{
    public GameObject NormalHeaderRoot;
    public GameObject LeadershipHeaderRoot;
    public ClanBannerView MyBanner;
    public TMP_Text MyClanNameText;
    public TMP_Text MyTierText;
    public TMP_Text MyServerText;
    public ClanBannerView EnemyBanner;
    public TMP_Text EnemyClanNameText;
    public TMP_Text EnemyTierText;
    public TMP_Text EnemyServerText;
    public TMP_Text ResultTitleText;
    public TMP_Text ResultSubtitleText;
    public TMP_Text OutcomeText;
    public TMP_Text ResultScoreText;
    public TMP_Text CooldownTimeLeftText;

    private static readonly Color WIN = new Color(0.298f, 0.686f, 0.314f);
    private static readonly Color LOSS = new Color(1f, 0.267f, 0.267f);

    private void Awake()
    {
        if (ResultSubtitleText != null) ResultSubtitleText.text = "Reward Cooldown";
    }

    // gdg gốc.
    public void Set(ClanWarWireDTO war)
    {
        if (NormalHeaderRoot != null) NormalHeaderRoot.SetActive(!war.IsLeadership);
        if (LeadershipHeaderRoot != null) LeadershipHeaderRoot.SetActive(war.IsLeadership);
        if (ResultTitleText != null) ResultTitleText.text = war.IsLeadership ? "Championship War Result" : "War Result";
        ClanWarProgressSection.SetSide(war.myClan, MyBanner, MyClanNameText, MyTierText, MyServerText);
        ClanWarProgressSection.SetSide(war.enemyClan, EnemyBanner, EnemyClanNameText, EnemyTierText, EnemyServerText);
        bool won = war.won ?? (war.winnerClanId == war.myClan?.clanId);
        if (OutcomeText != null)
        {
            OutcomeText.text = won ? "Win" : "Loss";
            OutcomeText.color = won ? WIN : LOSS;
        }
        SetScore(war.myWarScore, war.enemyWarScore);
    }

    // gdh gốc.
    public void SetScore(int my, int enemy)
    {
        if (ResultScoreText != null) ResultScoreText.text = my + " : " + enemy;
    }

    public void SetTimeLeft(long seconds)
    {
        if (CooldownTimeLeftText != null) CooldownTimeLeftText.text = "New War Starts in: " + ClanWarController.FormatCountdown(seconds);
    }
}
