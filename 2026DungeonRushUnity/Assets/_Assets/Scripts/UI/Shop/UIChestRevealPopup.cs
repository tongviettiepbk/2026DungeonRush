using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Màn mở rương mua bằng gem (ChestRevealPopup gốc). Chạm: mở rương → lần lượt từng món (ô lớn) → bảng tổng kết (ô nhỏ)
// → rương kế / đóng. "Open all" nhảy thẳng tới bảng tổng kết mọi món.
// Rút gọn so với gốc: bỏ hiệu ứng món bay từ ItemRevealStartTransform ra ô.
public class UIChestRevealPopup : BaseUI
{
    public Button AdvanceButton;
    public Button SkipAllButton;
    public Image ChestIcon;
    public TextMeshProUGUI ChestNameText;
    public GameObject TapToOpenHint;
    public ChestRewardElementUI CellPrefab;
    public Transform CellContainer;
    public GridLayoutGroup CellGrid;
    public Vector2 SingleItemCellSize;
    public Vector2 SummaryCellSize;
    public TextMeshProUGUI ItemsRemainingText;
    public TextMeshProUGUI CounterText;
    public Transform ItemRevealStartTransform;
    public float PunchStrength;
    public float PunchDuration;
    public float StepDelay;
    public float ItemBumpAmount;
    public float ItemBumpDuration;

    private enum Step
    {
        Closed,     // rương đóng, chờ chạm
        Revealing,  // đang lật từng món
        Summary,    // đã hiện hết món của rương này
    }

    private List<ChestOutcome> allOutcomes;
    private readonly List<List<ChestOutcome>> groups = new List<List<ChestOutcome>>();  // mỗi nhóm = 1 lần mở rương
    private int groupIndex;
    private int itemIndex;
    private Step step;
    private float nextInputTime;

    protected override void Awake()
    {
        base.Awake();
        if (AdvanceButton != null) AdvanceButton.onClick.AddListener(OnAdvanceClicked);
        if (SkipAllButton != null) SkipAllButton.onClick.AddListener(OnSkipAllClicked);
    }

    // fni gốc: gom kết quả thành từng lần mở (ItemCount món / rương).
    public void Setup(List<ChestOutcome> outcomes)
    {
        allOutcomes = outcomes;
        groups.Clear();
        List<ChestOutcome> current = null;
        for (int i = 0; i < outcomes.Count; i++)
        {
            if (current == null || current[0].chest != outcomes[i].chest || current.Count >= Mathf.Max(1, outcomes[i].chest.ItemCount))
            {
                current = new List<ChestOutcome>();
                groups.Add(current);
            }
            current.Add(outcomes[i]);
        }

        groupIndex = 0;
        ShowClosedChest();
    }

    private void ShowClosedChest()
    {
        step = Step.Closed;
        itemIndex = 0;
        ChestData chest = groups[groupIndex][0].chest;
        if (ChestIcon != null) ChestIcon.sprite = chest.Icon;
        ShopUIUtil.SetText(ChestNameText, StaticShopData.GetChestName(chest));
        if (TapToOpenHint != null) TapToOpenHint.SetActive(true);
        ShopUIUtil.SetActive(SkipAllButton, groups.Count > 1 || groups[groupIndex].Count > 1);
        ClearCells();
        RefreshCounters();
    }

    private void OnAdvanceClicked()
    {
        if (Time.unscaledTime < nextInputTime) return;
        nextInputTime = Time.unscaledTime + StepDelay;

        switch (step)
        {
            case Step.Closed:
                OpenChest();
                RevealNextItem();
                break;
            case Step.Revealing:
                if (itemIndex < groups[groupIndex].Count) RevealNextItem();
                else ShowSummary(groups[groupIndex]);
                break;
            case Step.Summary:
                groupIndex++;
                if (groupIndex < groups.Count) ShowClosedChest();
                else Close();
                break;
        }
    }

    private void OpenChest()
    {
        step = Step.Revealing;
        ChestData chest = groups[groupIndex][0].chest;
        if (ChestIcon != null)
        {
            if (chest.OpenIcon != null) ChestIcon.sprite = chest.OpenIcon;
            ChestIcon.transform.DOKill(true);
            ChestIcon.transform.DOPunchScale(Vector3.one * Mathf.Max(PunchStrength, ItemBumpAmount), PunchDuration).SetUpdate(true);
        }
        if (TapToOpenHint != null) TapToOpenHint.SetActive(false);
    }

    private void RevealNextItem()
    {
        List<ChestOutcome> group = groups[groupIndex];
        ClearCells();
        if (CellGrid != null) CellGrid.cellSize = SingleItemCellSize;
        AddCell(group[itemIndex]);
        itemIndex++;
        RefreshCounters();
        // Rương chỉ có 1 món thì món đó cũng là bảng tổng kết.
        if (group.Count == 1) step = Step.Summary;
    }

    private void ShowSummary(List<ChestOutcome> outcomes)
    {
        step = Step.Summary;
        ClearCells();
        if (CellGrid != null) CellGrid.cellSize = SummaryCellSize;
        for (int i = 0; i < outcomes.Count; i++) AddCell(outcomes[i]);
        ShopUIUtil.SetActive(SkipAllButton, false);
    }

    // "Open all": hiện toàn bộ món của mọi rương còn lại.
    private void OnSkipAllClicked()
    {
        if (step == Step.Closed) OpenChest();
        groupIndex = groups.Count - 1;
        itemIndex = groups[groupIndex].Count;
        ShowSummary(allOutcomes);
        RefreshCounters();
    }

    private void AddCell(ChestOutcome outcome)
    {
        ChestRewardElementUI cell = Instantiate(CellPrefab, CellContainer);
        cell.Setup(outcome);
        cell.transform.DOPunchScale(Vector3.one * ItemBumpAmount, ItemBumpDuration).SetUpdate(true);
    }

    private void ClearCells()
    {
        for (int i = CellContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = CellContainer.GetChild(i);
            child.DOKill();
            Destroy(child.gameObject);
        }
    }

    private void RefreshCounters()
    {
        ShopUIUtil.SetText(ItemsRemainingText, (groups[groupIndex].Count - itemIndex).ToString());
        if (CounterText != null)
        {
            int remainingChests = groups.Count - groupIndex - 1;
            CounterText.gameObject.SetActive(remainingChests > 0);
            CounterText.text = remainingChests.ToString();
        }
    }
}
