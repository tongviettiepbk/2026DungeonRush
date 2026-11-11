using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup đầu PvP (SpecialPvPPopup gốc): openPvP → tên league, đồng hồ mùa, bảng xếp hạng nhóm với vạch lên/xuống league,
// dòng của mình ghim ở đáy khi đang ngoài vùng nhìn thấy, nút Season Rewards, nút Challenge. Thứ Hai nghỉ: hiện giờ tới
// mùa sau và khoá Challenge. Còn kết quả mùa cũ thì mở Season Complete trước.
public class UIPvPPopup : BaseUI
{
    public GameObject objLoaded;
    public TMP_Text txtLeagueName;
    public TMP_Text txtSeasonTimer;
    public GameObject objLoading;
    public Button btBattle;
    public Button btReward;
    public Button btClose;
    public TMP_Text txtBattle;
    public ElementPvPLeaderboardUI leaderboardElement;
    public GameObject leaderboardSeparator;
    public Transform leaderboardContent;
    public ScrollRect leaderboardScroll;
    public ElementPvPLeaderboardUI stickyRow;
    // Reskin Đợt 6: Challenge cam, ngoài mùa xám.
    public Sprite sprBattle;
    public Sprite sprBattleDisabled;
    public Color promotionColor = new Color(0.27f, 0.85f, 0.45f, 1f);
    public Color demotionColor = new Color(0.9f, 0.27f, 0.25f, 1f);

    private readonly List<GameObject> leaderboardRows = new List<GameObject>();
    private RectTransform myRow;
    private float tick;
    private bool isLoaded;
    private bool isRequesting;

    private static PvPController Controller => PvPController.Instance;

    protected override void Awake()
    {
        base.Awake();
        btBattle.onClick.AddListener(OnClickBattle);
        if (btReward != null) btReward.onClick.AddListener(OnClickRewards);
        if (btClose != null) btClose.onClick.AddListener(Close);
        if (leaderboardElement != null) leaderboardElement.gameObject.SetActive(false);
        if (leaderboardSeparator != null) leaderboardSeparator.SetActive(false);
        if (stickyRow != null) stickyRow.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isLoaded = false;
        SetLoading(true);
        Open();
    }

    private void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 1f;
        DrawSeasonTimer();
    }

    private void LateUpdate()
    {
        UpdateStickyRow();
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
                UIManager.Instance.ShowToastMessage(message ?? Loc.L("Errors.PvP.LoadFailed"), isLocalize: false);
                Close();
                return;
            }

            Controller.ClearSettlement();
            isLoaded = true;
            SetLoading(false);
            Draw();
            if (Controller.State.pendingSeasonResult != null)
            {
                UIPvPSeasonClaimPopup.Open(Controller.State.pendingSeasonResult, null);
            }
        });
    }

    private void SetLoading(bool isOn)
    {
        if (objLoading != null) objLoading.SetActive(isOn);
        if (objLoaded != null) objLoaded.SetActive(!isOn);
        btBattle.gameObject.SetActive(!isOn);
    }

    // ===== Vẽ =====

    private void Draw()
    {
        StaticPvPData data = GameData.staticData.pvp;
        PvPLeagueDefinition league = data.GetLeagueByIndex(Controller.LeagueIndex);
        if (txtLeagueName != null)
        {
            txtLeagueName.text = data.GetLeagueName(league.leagueIndex);
            txtLeagueName.color = league.leagueColor;
        }
        DrawSeasonTimer();

        btBattle.gameObject.SetActive(true);
        btBattle.interactable = IsSeasonActive;
        if (sprBattle != null && sprBattleDisabled != null) btBattle.image.sprite = IsSeasonActive ? sprBattle : sprBattleDisabled;
        if (txtBattle != null) txtBattle.text = Loc.L("Popup.PvP.Challenge");
        DrawLeaderboard();
    }

    private static bool IsSeasonActive => Controller.State != null && Controller.State.season != null && Controller.State.season.isActive;

    private void DrawSeasonTimer()
    {
        if (txtSeasonTimer == null || !isLoaded || Controller.State == null || Controller.State.season == null) return;
        PvPSeasonDTO season = Controller.State.season;
        long now = new DateTimeOffset(ServerTime.UtcNow).ToUnixTimeSeconds();
        long target = season.isActive ? season.endsAt : season.nextStartsAt;
        string key = season.isActive ? "Popup.PvP.Season.EndsIn" : "Popup.PvP.Season.NextIn";
        txtSeasonTimer.text = string.Format(Loc.L(key), FormatTime(TimeSpan.FromSeconds(target - now)));
    }

    // Bảng xếp hạng + vạch lên (dưới người cuối được lên) + vạch xuống (trên người đầu tiên bị xuống).
    private void DrawLeaderboard()
    {
        for (int i = 0; i < leaderboardRows.Count; i++)
        {
            if (leaderboardRows[i] != null) Destroy(leaderboardRows[i]);
        }
        leaderboardRows.Clear();
        myRow = null;
        if (stickyRow != null) stickyRow.gameObject.SetActive(false);
        if (leaderboardElement == null || leaderboardContent == null) return;

        PvPOpenResponseDTO state = Controller.State;
        List<PvPLeaderboardEntryModel> list = state.leaderboard;
        if (list == null) return;

        int promote = state.promotion != null ? state.promotion.promoteCount : 0;
        int demote = state.promotion != null ? state.promotion.demoteCount : 0;
        int demoteFrom = list.Count - demote;
        for (int i = 0; i < list.Count; i++)
        {
            if (demote > 0 && i == demoteFrom) AddSeparator(Loc.L("Popup.PvP.Season.DemotionArea"), demotionColor);

            ElementPvPLeaderboardUI row = Instantiate(leaderboardElement, leaderboardContent);
            row.gameObject.SetActive(true);
            row.Setup(list[i]);
            leaderboardRows.Add(row.gameObject);
            if (list[i].IsCurrentPlayer)
            {
                myRow = row.transform as RectTransform;
                if (stickyRow != null) stickyRow.Setup(list[i]);
            }

            if (promote > 0 && i == promote - 1 && i < list.Count - 1) AddSeparator(Loc.L("Popup.PvP.Season.PromotionArea"), promotionColor);
        }

        if (leaderboardScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            leaderboardScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void AddSeparator(string label, Color color)
    {
        if (leaderboardSeparator == null) return;
        GameObject separator = Instantiate(leaderboardSeparator, leaderboardContent);
        separator.SetActive(true);
        foreach (Graphic graphic in separator.GetComponentsInChildren<Graphic>(true))
        {
            graphic.color = color;
        }
        TMP_Text text = separator.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
        leaderboardRows.Add(separator);
    }

    // Dòng của mình nằm trọn trong vùng nhìn thấy (chừa phần dòng ghim che ở đáy) thì không cần ghim.
    private void UpdateStickyRow()
    {
        if (stickyRow == null || myRow == null || leaderboardScroll == null) return;
        RectTransform viewport = leaderboardScroll.viewport != null ? leaderboardScroll.viewport : (RectTransform)leaderboardScroll.transform;
        Vector3[] viewCorners = new Vector3[4];
        Vector3[] rowCorners = new Vector3[4];
        Vector3[] stickyCorners = new Vector3[4];
        viewport.GetWorldCorners(viewCorners);
        myRow.GetWorldCorners(rowCorners);
        ((RectTransform)stickyRow.transform).GetWorldCorners(stickyCorners);
        bool isVisible = rowCorners[1].y <= viewCorners[1].y && rowCorners[0].y >= stickyCorners[1].y;
        if (stickyRow.gameObject.activeSelf == isVisible) stickyRow.gameObject.SetActive(!isVisible);
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
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.PvP.NoTicketsRemaining"), isLocalize: false);
            return;
        }
        UIManager.Instance.LoadUI(UIKey.PvPFindOpponentPopup);
    }

    private void OnClickRewards()
    {
        UIManager.Instance.LoadUI(UIKey.PvPRewardsPopup);
    }

    // Sau trận: chờ đổi mode xong rồi mở lại popup.
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
