using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Clan Search" (AdvancedSearchPopup gốc): tên, Min/Max member (0..50, nút −/+), Hide Approval Only, Search / Clear.
// Min ≥ 1 mới lọc min; Max ≤ 49 mới lọc max (fsd gốc).
public class UIAdvancedSearchPopup : BaseUI
{
    public TMP_InputField NameInput;
    public TMP_Text MinMemberText;
    public Button MinDecrementButton;
    public Button MinIncrementButton;
    public TMP_Text MaxMemberText;
    public Button MaxDecrementButton;
    public Button MaxIncrementButton;
    public CheckButton HideApprovalToggle;
    public Button SearchButton;
    public Button CloseButton;
    public Button ClearButton;
    public TMP_Text TitleText;
    public TMP_Text NameLabel;
    public TMP_Text MinMemberLabel;
    public TMP_Text MaxMemberLabel;
    public TMP_Text HideApprovalLabel;
    public TMP_Text SearchButtonText;

    private Action<ClanSearchFilter> onSearch;
    private int min;
    private int max = StaticClanData.MAX_MEMBERS;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Clan Search");
        ClanUIUtil.SetText(NameLabel, "Clan Name");
        ClanUIUtil.SetText(MinMemberLabel, "Min Member");
        ClanUIUtil.SetText(MaxMemberLabel, "Max Member");
        ClanUIUtil.SetText(HideApprovalLabel, "Hide Approval Only");
        ClanUIUtil.SetText(SearchButtonText, "Search");
        if (NameInput != null)
        {
            NameInput.characterLimit = StaticClanData.NAME_MAX;
            if (NameInput.placeholder is TMP_Text ph) ph.text = "Clan Name...";
        }
        if (MinDecrementButton != null) MinDecrementButton.onClick.AddListener(() => SetMin(min - 1));
        if (MinIncrementButton != null) MinIncrementButton.onClick.AddListener(() => SetMin(min + 1));
        if (MaxDecrementButton != null) MaxDecrementButton.onClick.AddListener(() => SetMax(max - 1));
        if (MaxIncrementButton != null) MaxIncrementButton.onClick.AddListener(() => SetMax(max + 1));
        if (SearchButton != null) SearchButton.onClick.AddListener(OnSearchClicked);
        if (ClearButton != null) ClearButton.onClick.AddListener(OnClearClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
    }

    // frx gốc.
    public void Setup(ClanSearchFilter filter, Action<ClanSearchFilter> onSearch)
    {
        this.onSearch = onSearch;
        filter = filter ?? new ClanSearchFilter();
        if (NameInput != null) NameInput.text = filter.ClanName ?? string.Empty;
        min = Mathf.Clamp(filter.MinMemberCount ?? 0, 0, StaticClanData.MAX_MEMBERS);
        max = Mathf.Clamp(filter.MaxMemberCount ?? StaticClanData.MAX_MEMBERS, min, StaticClanData.MAX_MEMBERS);
        if (HideApprovalToggle != null) HideApprovalToggle.SetChecked(filter.HideApprovalOnly);
        RefreshTexts();
    }

    // frz gốc.
    private void SetMin(int value)
    {
        min = Mathf.Clamp(value, 0, max);
        RefreshTexts();
    }

    // fsa gốc.
    private void SetMax(int value)
    {
        max = Mathf.Clamp(value, min, StaticClanData.MAX_MEMBERS);
        RefreshTexts();
    }

    // fsb gốc.
    private void RefreshTexts()
    {
        ClanUIUtil.SetText(MinMemberText, min.ToString());
        ClanUIUtil.SetText(MaxMemberText, max.ToString());
    }

    // fsc gốc.
    private void OnClearClicked()
    {
        if (NameInput != null) NameInput.text = string.Empty;
        min = 0;
        max = StaticClanData.MAX_MEMBERS;
        if (HideApprovalToggle != null) HideApprovalToggle.SetChecked(false);
        RefreshTexts();
    }

    // fsd gốc.
    private void OnSearchClicked()
    {
        ClanSearchFilter filter = new ClanSearchFilter
        {
            ClanName = NameInput != null && !string.IsNullOrEmpty(NameInput.text) ? NameInput.text.Trim() : null,
            MinMemberCount = min >= 1 ? min : (int?)null,
            MaxMemberCount = max <= StaticClanData.MAX_MEMBERS - 1 ? max : (int?)null,
            HideApprovalOnly = HideApprovalToggle != null && HideApprovalToggle.IsChecked,
        };
        onSearch?.Invoke(filter);
        Close();
    }
}
