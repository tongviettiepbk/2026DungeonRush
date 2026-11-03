using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Chọn đối thủ: "Searching for opponents" → findPvPOpponents (roster cache còn hạn thì dùng lại) → "Choose an Opponent",
// tối đa 5 người cùng nhóm, đã xếp sẵn từ +5 xuống ở server. Bấm Challenge → startPvPBattle → vào trận.
public class UIPvPFindOpponentPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtError;
    public Transform listContent;
    public ElementPvPPlayerUI elementPrefab;
    public Button btClose;
    public GameObject objLoaded;
    public GameObject objLoading;
    // Reskin (mockup): giờ hồi vé + ô vé "{còn}/{free mỗi ngày}".
    public TMP_Text txtResetTime;
    public TMP_Text txtTickets;

    private readonly List<ElementPvPPlayerUI> elements = new List<ElementPvPPlayerUI>();
    private bool isStarting;
    private float tick;

    protected override void Awake()
    {
        base.Awake();
        if (btClose != null) btClose.onClick.AddListener(OnClickClose);
        if (elementPrefab != null) elementPrefab.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isStarting = false;
        ClearList();
        SetLoading(true, Loc.L("Popup.PvP.SearchingOpponents"));
        if (txtError != null) txtError.gameObject.SetActive(false);

        PvPController.Instance.FindOpponents(OnRoster, OnError);
        tick = 0f;
        DrawTickets();
    }

    private void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 1f;
        DrawTickets();
    }

    private void DrawTickets()
    {
        if (txtResetTime != null)
        {
            DateTime now = ServerTime.UtcNow;
            txtResetTime.text = string.Format(Loc.L("Popup.PvP.TicketsResetIn"), UIPvPPopup.FormatTime(now.Date.AddDays(1) - now));
        }
        if (txtTickets != null)
        {
            PvPTicketsDTO t = GameData.userData.pvp.tickets;
            int daily = t.dailyTickets > 0 ? t.dailyTickets : StaticPvPData.DAILY_TICKETS;
            txtTickets.text = "<sprite=0>" + t.remaining + "/" + daily;
        }
    }

    private void SetLoading(bool isOn, string title)
    {
        if (txtTitle != null) txtTitle.text = title;
        if (objLoading != null) objLoading.SetActive(isOn);
        if (objLoaded != null) objLoaded.SetActive(!isOn);
    }

    private void OnRoster(PvPCachedRosterModel roster)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        SetLoading(false, Loc.L("Popup.PvP.ChooseOpponent"));

        List<PvPPlayerModel> list = roster.Opponents;

        for (int i = 0; i < list.Count; i++)
        {
            ElementPvPPlayerUI element = Instantiate(elementPrefab, listContent);
            element.gameObject.SetActive(true);
            element.Setup(list[i], list[i].StarsOnWin, false, OnClickBattle);
            elements.Add(element);
        }

        if (txtError != null)
        {
            txtError.gameObject.SetActive(list.Count == 0);
            txtError.text = Loc.L("Popup.PvP.NoOpponents");
        }
    }

    private void OnError(string message)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        SetLoading(false, Loc.L("Popup.PvP.Challenge"));
        if (txtError != null)
        {
            txtError.gameObject.SetActive(true);
            txtError.text = message;
        }
    }

    private void OnClickBattle(PvPPlayerModel opponent)
    {
        if (isStarting) return;
        isStarting = true;
        SetInteractable(false);
        if (txtTitle != null) txtTitle.text = Loc.L("Popup.PvP.StartingBattle");

        PvPController.Instance.StartBattle(opponent, (ok, message) =>
        {
            if (ok)
            {
                // Vào trận: đóng popup này + sảnh.
                Close();
                UIPvPPopup lobby = FindObjectOfType<UIPvPPopup>();
                if (lobby != null) lobby.Close();
                return;
            }

            isStarting = false;
            UIManager.Instance.ShowToastMessage(message, isLocalize: false);
            if (this == null || !gameObject.activeInHierarchy) return;
            // Roster hết hạn → tìm lại.
            if (message == "Opponent roster expired.")
            {
                OnEnable();
                return;
            }
            if (txtTitle != null) txtTitle.text = Loc.L("Popup.PvP.ChooseOpponent");
            SetInteractable(true);
        });
    }

    private void SetInteractable(bool isOn)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] != null) elements[i].SetInteractable(isOn);
        }
    }

    private void ClearList()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i] != null) Destroy(elements[i].gameObject);
        }
        elements.Clear();
    }

    private void OnClickClose()
    {
        if (isStarting) return;
        Close();
    }
}
