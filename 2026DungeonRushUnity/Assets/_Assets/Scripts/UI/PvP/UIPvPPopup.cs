using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Sảnh PvP (PvPPopup gốc, "PvP Arena"): openPvP → tên league (màu league), trophy, thanh trophy trong league,
// "Tickets refreshed in: {0}" (tới 0h UTC), nút Battle "<sprite=0>{vé}/{vé free/ngày}\nBattle", nút vé ads khi hết vé,
// Leaderboard, bấm khung trophy → bảng thưởng theo league.
public class UIPvPPopup : BaseUI
{
    public GameObject objLoaded;
    public TMP_Text txtTitle;
    public TMP_Text txtRankName;
    public TMP_Text txtTrophy;
    public TMP_Text txtResetTime;
    public GameObject objLoading;
    public Button btBattle;
    public Button btLeaderboard;
    public Button btReward;
    public Button btAdTicket;
    public Button btClose;
    public TMP_Text txtBattle;
    public TMP_Text txtAdTicket;
    public GameObject objTrophySliderRoot;
    public Slider sliderTrophy;
    public TMP_Text txtTrophySlider;

    private float tick;
    private bool isLoaded;
    private bool isRequesting;

    private static PvPController Controller => PvPController.Instance;

    protected override void Awake()
    {
        base.Awake();
        btBattle.onClick.AddListener(OnClickBattle);
        if (btLeaderboard != null) btLeaderboard.onClick.AddListener(OnClickLeaderboard);
        if (btReward != null) btReward.onClick.AddListener(OnClickRewards);
        if (btAdTicket != null) btAdTicket.onClick.AddListener(OnClickAdTicket);
        if (btClose != null) btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PvPController.TicketsUpdated += OnTicketsUpdated;
        if (txtTitle != null) txtTitle.text = "PvP Arena";
        isLoaded = false;
        SetLoading(true);
        Open();
    }

    private void OnDisable()
    {
        PvPController.TicketsUpdated -= OnTicketsUpdated;
    }

    private void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 1f;
        DrawResetTime();
    }

    private void OnTicketsUpdated(PvPTicketsDTO tickets)
    {
        if (isLoaded) Draw();
    }

    private void Open()
    {
        if (isRequesting) return;
        isRequesting = true;
        Controller.Open((ok, message) =>
        {
            isRequesting = false;
            if (this == null || !gameObject.activeInHierarchy) return;
            if (!ok)
            {
                UIManager.Instance.ShowToastMessage(message ?? "Failed to load PvP.", isLocalize: false);
                Close();
                return;
            }

            // jne(true): còn thưởng trận trước chưa nhận (tắt app trước khi Claim) → tự cộng + báo.
            Controller.ApplyPendingSettlement(true);
            isLoaded = true;
            SetLoading(false);
            Draw();
        });
    }

    private void SetLoading(bool isOn)
    {
        if (objLoading != null) objLoading.SetActive(isOn);
        if (objLoaded != null) objLoaded.SetActive(!isOn);
        btBattle.gameObject.SetActive(!isOn);
        if (btAdTicket != null && isOn) btAdTicket.gameObject.SetActive(false);
    }

    // ===== Vẽ (jqs/jqx/jqz) =====

    private void Draw()
    {
        StaticPvPData data = GameData.staticData.pvp;
        int trophy = Controller.Trophy;
        PvPLeagueDefinition league = data.GetLeague(trophy);

        if (txtRankName != null)
        {
            txtRankName.text = league.leagueName;
            txtRankName.color = league.leagueColor;
        }
        if (txtTrophy != null) txtTrophy.text = "<sprite=0>" + trophy;
        DrawTrophySlider(trophy, league);
        DrawResetTime();
        DrawButtons();
    }

    // jqx gốc: max = MaxTrophy của league (league cuối không trần → max = trophy); value = (trophy−min)/(max−min)
    // kẹp ≤ 1 (khoảng < 1 → đầy); chữ "{trophy}/{max}".
    private void DrawTrophySlider(int trophy, PvPLeagueDefinition league)
    {
        if (sliderTrophy == null) return;
        int max = league.maxTrophy == int.MaxValue ? trophy : league.maxTrophy;
        int range = max - league.minTrophy;
        sliderTrophy.minValue = 0f;
        sliderTrophy.maxValue = 1f;
        sliderTrophy.value = range < 1 ? 1f : Mathf.Clamp01((float)(trophy - league.minTrophy) / range);
        if (txtTrophySlider != null) txtTrophySlider.text = trophy + "/" + max;
    }

    // Gốc chỉ gán chữ, KHÔNG bật ResetText (prefab gốc để tắt) — giữ nguyên như vậy.
    private void DrawResetTime()
    {
        if (txtResetTime == null || !isLoaded) return;
        DateTime now = DateTime.UtcNow;
        TimeSpan remain = now.Date.AddDays(1) - now;
        txtResetTime.text = "Tickets refreshed in: <color=#E3EC42>" + FormatTime(remain) + "</color>";
    }

    // jqz: còn vé → nút Battle; hết vé → khoá Battle, hiện nút xem ads lấy vé nếu còn lượt.
    private void DrawButtons()
    {
        PvPTicketsDTO t = GameData.userData.pvp.tickets;
        int total = t.freeRemaining + t.adRemaining;
        int daily = t.dailyFreeTickets > 0 ? t.dailyFreeTickets : StaticPvPData.DAILY_FREE_TICKETS;
        int maxAd = t.maxAdPerDay > 0 ? t.maxAdPerDay : StaticPvPData.MAX_AD_TICKETS_PER_DAY;

        btBattle.gameObject.SetActive(true);
        btBattle.interactable = total > 0;
        if (txtBattle != null) txtBattle.text = "<sprite=0>" + total + "/" + daily + "\nBattle";

        if (btAdTicket != null)
        {
            bool showAd = total <= 0 && t.adClaimedToday < maxAd;
            btAdTicket.gameObject.SetActive(showAd);
            btAdTicket.interactable = showAd;
            if (txtAdTicket != null) txtAdTicket.text = (maxAd - t.adClaimedToday) + "/" + maxAd + "\n<sprite=0>Enter";
        }
    }

    // ev.ehx gốc: 2 đơn vị lớn nhất "1d 2h" / "5h 30m" / "3m 20s".
    public static string FormatTime(TimeSpan time)
    {
        if (time.TotalSeconds < 0) time = TimeSpan.Zero;
        if (time.Days > 0) return time.Days + "d " + time.Hours + "h";
        if (time.Hours > 0) return time.Hours + "h " + time.Minutes + "m";
        if (time.Minutes > 0) return time.Minutes + "m " + time.Seconds + "s";
        return time.Seconds + "s";
    }

    // ===== Nút =====

    private void OnClickBattle()
    {
        if (GameData.userData.pvp.TotalTickets <= 0)
        {
            UIManager.Instance.ShowToastMessage("No PvP tickets remaining.", isLocalize: false);
            return;
        }
        UIManager.Instance.LoadUI(UIKey.PvPFindOpponentPopup);
    }

    private void OnClickLeaderboard()
    {
        UIManager.Instance.LoadUI(UIKey.PvPLeaderboardPopup);
    }

    private void OnClickRewards()
    {
        UIManager.Instance.LoadUI(UIKey.PvPRewardsPopup);
    }

    // jra/jrb: xem ads (RewardedType.PvPTicket = 13) → grantPvPAdTicket.
    private void OnClickAdTicket()
    {
        btAdTicket.interactable = false;
        MediationAds.Instance.ShowRewardedVideoAd(PvPController.AD_PLACEMENT, result =>
        {
            if (result != ShowResultADS.Finished)
            {
                if (this != null) DrawButtons();
                return;
            }

            Controller.GrantAdTicket((ok, message) =>
            {
                UIManager.Instance.ShowToastMessage(ok ? "PvP ticket added." : message, isLocalize: false);
                if (this != null && gameObject.activeInHierarchy) DrawButtons();
            });
        });
    }

    // Sau trận: chờ đổi mode xong rồi mở lại sảnh.
    public static void OpenAfterModeChange()
    {
        PvPController.Instance.StartCoroutine(RoutineOpenAfterModeChange());
    }

    private static IEnumerator RoutineOpenAfterModeChange()
    {
        yield return null;
        while (GameController.Instance.isChangingMode)
        {
            yield return null;
        }
        UIManager.Instance.LoadUI(UIKey.PvPPopup);
    }
}
