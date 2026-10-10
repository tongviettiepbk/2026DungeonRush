using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Chọn đối thủ (PvPFindOpponentPopup gốc): "Searching for opponents" → findPvPOpponents (roster cache còn hạn thì dùng lại)
// → "Choose Opponent", danh sách sắp theo trophy-nếu-thắng GIẢM dần (ts.jpi). Bấm đánh → startPvPBattle → vào trận.
public class UIPvPFindOpponentPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtError;
    public Transform listContent;
    public ElementPvPPlayerUI elementPrefab;
    public Button btClose;
    public GameObject objLoaded;
    public GameObject objLoading;

    private readonly List<ElementPvPPlayerUI> elements = new List<ElementPvPPlayerUI>();
    private bool isStarting;

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
        SetLoading(true, "Searching for opponents");
        if (txtError != null) txtError.gameObject.SetActive(false);

        PvPController.Instance.FindOpponents(OnRoster, OnError);
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
        SetLoading(false, "Choose Opponent");

        StaticPvPData data = GameData.staticData.pvp;
        int myTrophy = PvPController.Instance.Trophy;
        List<PvPPlayerModel> list = new List<PvPPlayerModel>(roster.Opponents);
        list.Sort((a, b) => data.GetWinDelta(myTrophy, b.Trophy).CompareTo(data.GetWinDelta(myTrophy, a.Trophy)));

        for (int i = 0; i < list.Count; i++)
        {
            ElementPvPPlayerUI element = Instantiate(elementPrefab, listContent);
            element.gameObject.SetActive(true);
            element.Setup(list[i], data.GetWinDelta(myTrophy, list[i].Trophy), false, OnClickBattle);
            elements.Add(element);
        }

        if (txtError != null)
        {
            txtError.gameObject.SetActive(list.Count == 0);
            txtError.text = "No opponents found.\nTry again later.";
        }
    }

    private void OnError(string message)
    {
        if (this == null || !gameObject.activeInHierarchy) return;
        SetLoading(false, "Battle");
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
        if (txtTitle != null) txtTitle.text = "Starting battle...";

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
            if (txtTitle != null) txtTitle.text = "Choose Opponent";
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
