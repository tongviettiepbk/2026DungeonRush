using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng xếp hạng PvP (PvPLeaderboardPopup gốc): 2 tab "world" / "country" (ygt), top người chơi + (nếu mình ngoài top)
// dải phân cách rồi vài người quanh mình; dòng của mình highlight và cuộn tới.
public class UIPvPLeaderboardPopup : BaseUI
{
    public TMP_Text txtTitle;
    public Button[] tabButtons = new Button[2];       // index theo StaticPvPData.LEADERBOARD_SCOPES
    public RectTransform tabSelector;
    public ElementPvPLeaderboardUI elementPrefab;
    public GameObject separatorPrefab;
    public Transform listContent;
    public ScrollRect scrollRect;
    public Button btClose;
    public GameObject objLoaded;
    public GameObject objLoading;

    private readonly List<GameObject> rows = new List<GameObject>();
    private int tabIndex;
    private int requestId;

    protected override void Awake()
    {
        base.Awake();
        if (btClose != null) btClose.onClick.AddListener(Close);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab(index));
        }
        if (elementPrefab != null) elementPrefab.gameObject.SetActive(false);
        if (separatorPrefab != null) separatorPrefab.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (txtTitle != null) txtTitle.text = "Leaderboard";
        SelectTab(0);
    }

    private void SelectTab(int index)
    {
        tabIndex = Mathf.Clamp(index, 0, StaticPvPData.LEADERBOARD_SCOPES.Length - 1);
        Load(StaticPvPData.LEADERBOARD_SCOPES[tabIndex]);
    }

    // Khung chọn bám tab đang chọn (sau khi HorizontalLayoutGroup chia bề rộng tab).
    private void LateUpdate()
    {
        if (tabSelector == null || tabButtons.Length <= tabIndex || tabButtons[tabIndex] == null) return;
        RectTransform tab = (RectTransform)tabButtons[tabIndex].transform;
        if (tab.rect.width <= 0f) return;
        tabSelector.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tab.rect.width);
        tabSelector.position = tab.position;
    }

    private void Load(string scope)
    {
        ClearRows();
        SetLoading(true);
        int id = ++requestId;
        PvPController.Instance.GetLeaderboard(scope, res =>
        {
            if (this == null || !gameObject.activeInHierarchy || id != requestId) return;
            SetLoading(false);
            if (res == null)
            {
                UIManager.Instance.ShowToastMessage("Failed to load leaderboard.", isLocalize: false);
                return;
            }
            Draw(res);
        });
    }

    // jpz/jqc.
    private void Draw(PvPLeaderboardResponseDTO res)
    {
        RectTransform me = null;
        AddRows(res.topPlayers, ref me);
        if (res.nearbyPlayers != null && res.nearbyPlayers.Count > 0)
        {
            if (separatorPrefab != null)
            {
                GameObject sep = Instantiate(separatorPrefab, listContent);
                sep.SetActive(true);
                rows.Add(sep);
            }
            AddRows(res.nearbyPlayers, ref me);
        }

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            if (me != null && scrollRect.content != null)
            {
                float contentHeight = scrollRect.content.rect.height;
                float viewHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0f;
                if (contentHeight > viewHeight)
                {
                    float y = Mathf.Abs(me.anchoredPosition.y) - viewHeight * 0.5f;
                    scrollRect.verticalNormalizedPosition = 1f - Mathf.Clamp01(y / (contentHeight - viewHeight));
                }
            }
        }
    }

    private void AddRows(List<PvPLeaderboardEntryModel> list, ref RectTransform me)
    {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            ElementPvPLeaderboardUI row = Instantiate(elementPrefab, listContent);
            row.gameObject.SetActive(true);
            row.Setup(list[i]);
            rows.Add(row.gameObject);
            if (list[i].IsCurrentPlayer) me = row.transform as RectTransform;
        }
    }

    private void SetLoading(bool isOn)
    {
        if (objLoading != null) objLoading.SetActive(isOn);
        if (objLoaded != null) objLoaded.SetActive(!isOn);
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null) Destroy(rows[i]);
        }
        rows.Clear();
    }
}
