using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Enter Clan Name" (CreateClanPopup gốc): tên 3-15 chữ+số, Public (bỏ tick = Approval Only), banner (Browse → editor),
// nút "Create \n<sprite=0>100" (mờ khi thiếu gem), trừ 100 gem khi tạo thành công.
public class UICreateClanPopup : BaseUI
{
    public TMP_InputField NameInput;
    public CheckButton ApprovalOnlyToggle;
    public ClanBannerView BannerPreview;
    public Button BannerButton;
    public ClanBannerCatalog DefaultCatalog;
    public Button CreateButton;
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public TMP_Text TitleText;
    public TMP_Text NameLabel;
    public TMP_Text BannerLabel;
    public TMP_Text BrowseButtonText;
    public TMP_Text PublicLabel;
    public TMP_Text CreateButtonText;

    private ClanBannerData banner;
    private bool isBusy;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TitleText, "Enter Clan Name");
        ClanUIUtil.SetText(NameLabel, "Clan Name");
        ClanUIUtil.SetText(BannerLabel, "Banner");
        ClanUIUtil.SetText(BrowseButtonText, "Browse");
        ClanUIUtil.SetText(PublicLabel, "Public");
        ClanUIUtil.SetText(CreateButtonText, "Create \n<sprite=0>" + StaticClanData.CREATE_COST_GEM);
        if (NameInput != null)
        {
            NameInput.characterLimit = StaticClanData.NAME_MAX;
            if (NameInput.placeholder is TMP_Text ph) ph.text = "Clan Name...";
        }
        if (CreateButton != null) CreateButton.onClick.AddListener(OnCreateClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseClicked);
        if (BannerButton != null) BannerButton.onClick.AddListener(OnBannerClicked);
    }

    // eip gốc.
    protected override void OnEnable()
    {
        base.OnEnable();
        isBusy = false;
        if (LoadingView != null) LoadingView.Hide();
        banner = DefaultCatalog != null ? DefaultCatalog.Default : new ClanBannerData();
        if (NameInput != null) NameInput.text = string.Empty;
        if (ApprovalOnlyToggle != null) ApprovalOnlyToggle.SetChecked(true);   // mặc định Public
        if (BannerPreview != null) BannerPreview.SetBanner(banner);
        RefreshCreateButton();
    }

    // fxv gốc: chữ nút mờ khi gem ≤ 99.
    private void RefreshCreateButton()
    {
        if (CreateButtonText == null) return;
        bool enough = GameData.userData.items.IsEnough(ItemType.GEM, StaticClanData.CREATE_COST_GEM);
        Color c = CreateButtonText.color;
        c.a = enough ? 1f : 0.5f;
        CreateButtonText.color = c;
    }

    private void OnBannerClicked()
    {
        if (isBusy) return;
        UIClanBannerEditorPopup popup = UIManager.Instance.LoadUI(UIKey.ClanBannerEditorPopup) as UIClanBannerEditorPopup;
        if (popup != null) popup.Setup(banner, OnBannerApplied);
    }

    private void OnBannerApplied(ClanBannerData data)
    {
        banner = data.Clone();
        if (BannerPreview != null) BannerPreview.SetBanner(banner);
    }

    // fxy gốc.
    private void OnCreateClicked()
    {
        if (isBusy) return;
        string name = NameInput != null ? NameInput.text.Trim() : string.Empty;
        if (name.Length < StaticClanData.NAME_MIN || name.Length > StaticClanData.NAME_MAX || !Regex.IsMatch(name, StaticClanData.NAME_REGEX))
        {
            ClanUIUtil.Toast("Clan name must be 3-15 characters (letters and numbers only)");
            return;
        }
        if (!GameData.userData.items.IsEnough(ItemType.GEM, StaticClanData.CREATE_COST_GEM))
        {
            ClanUIUtil.Toast("Not enough gems");
            return;
        }
        ClanJoinSetting join = ApprovalOnlyToggle != null && !ApprovalOnlyToggle.IsChecked ? ClanJoinSetting.ApprovalOnly : ClanJoinSetting.Open;
        SetBusy(true);
        ClanController.Instance.Create(name, join, banner, OnCreated);
    }

    // fyb gốc: thành công → trừ 100 gem.
    private void OnCreated(bool ok, ClanModel clan, string error)
    {
        SetBusy(false);
        if (!ok)
        {
            ClanUIUtil.Toast(error ?? "Failed to create clan");
            return;
        }
        GameData.userData.items.Consume(ItemType.GEM, StaticClanData.CREATE_COST_GEM);
        GameData.Save(true);
        Close();
    }

    // fxz gốc.
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
