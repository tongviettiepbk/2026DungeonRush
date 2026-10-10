using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Clan Settings" (ClanSettingsPopup gốc): mô tả ≤ 200 ký tự, Public, banner. Chỉ Leader sửa + thấy nút Save.
public class UIClanSettingsPopup : BaseUI
{
    public TMP_Text ClanNameLabel;
    public TMP_InputField DescriptionInput;
    public CheckButton ApprovalOnlyToggle;
    public ClanBannerView BannerPreview;
    public Button BannerButton;
    public Button SaveButton;
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public TMP_Text TitleText;
    public TMP_Text NameLabel;
    public TMP_Text DescriptionLabel;
    public TMP_Text BannerLabel;
    public TMP_Text BrowseButtonText;
    public TMP_Text PublicLabel;
    public TMP_Text SaveButtonText;

    private ClanModel clan;
    private ClanBannerData banner;
    private bool isBusy;
    private bool canEdit;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Clan Settings");
        ClanUIUtil.SetText(NameLabel, "Clan Name");
        ClanUIUtil.SetText(DescriptionLabel, "Description");
        ClanUIUtil.SetText(BannerLabel, "Banner");
        ClanUIUtil.SetText(BrowseButtonText, "Browse");
        ClanUIUtil.SetText(PublicLabel, "Public");
        ClanUIUtil.SetText(SaveButtonText, "Save");
        if (DescriptionInput != null)
        {
            DescriptionInput.characterLimit = StaticClanData.DESCRIPTION_MAX;
            DescriptionInput.lineType = TMP_InputField.LineType.MultiLineNewline;
            if (DescriptionInput.placeholder is TMP_Text ph) ph.text = "Description...";
        }
        if (SaveButton != null) SaveButton.onClick.AddListener(OnSaveClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseClicked);
        if (BannerButton != null) BannerButton.onClick.AddListener(OnBannerClicked);
    }

    // fxg + eip gốc.
    public void Setup(ClanModel clan)
    {
        this.clan = clan;
        isBusy = false;
        if (LoadingView != null) LoadingView.Hide();
        if (clan == null) return;
        canEdit = clan.MyRole == ClanRole.Leader;
        banner = clan.Banner.Clone();
        ClanUIUtil.SetText(ClanNameLabel, clan.ClanName);
        if (DescriptionInput != null) DescriptionInput.text = clan.Description;
        if (ApprovalOnlyToggle != null) ApprovalOnlyToggle.SetChecked(clan.JoinSetting == ClanJoinSetting.Open);
        if (BannerPreview != null) BannerPreview.SetBanner(banner);
        RefreshEditable();
    }

    // fxh gốc.
    private void RefreshEditable()
    {
        if (DescriptionInput != null) DescriptionInput.interactable = canEdit;
        if (BannerButton != null) BannerButton.interactable = canEdit;
        if (ApprovalOnlyToggle != null)
        {
            Button b = ApprovalOnlyToggle.GetComponent<Button>();
            if (b != null) b.interactable = canEdit;
        }
        ClanUIUtil.SetActive(SaveButton, canEdit);
    }

    // fxi gốc.
    private void OnBannerClicked()
    {
        if (isBusy || !canEdit) return;
        UIClanBannerEditorPopup popup = UIManager.Instance.LoadUI(UIKey.ClanBannerEditorPopup) as UIClanBannerEditorPopup;
        if (popup != null) popup.Setup(banner, data =>
        {
            banner = data.Clone();
            if (BannerPreview != null) BannerPreview.SetBanner(banner);
        });
    }

    // fxj gốc.
    private void OnSaveClicked()
    {
        if (isBusy || !canEdit || clan == null) return;
        string description = DescriptionInput != null ? DescriptionInput.text.Trim() : string.Empty;
        ClanJoinSetting join = ApprovalOnlyToggle != null && !ApprovalOnlyToggle.IsChecked ? ClanJoinSetting.ApprovalOnly : ClanJoinSetting.Open;
        SetBusy(true);
        ClanController.Instance.UpdateSettings(description, join, banner, (ok, updated, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error ?? "Failed to save changes");
                return;
            }
            Close();
        });
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        if (LoadingView == null) return;
        if (busy) LoadingView.ShowLoading();
        else LoadingView.Hide();
    }

    private void OnCloseClicked()
    {
        if (!isBusy) Close();
    }
}
