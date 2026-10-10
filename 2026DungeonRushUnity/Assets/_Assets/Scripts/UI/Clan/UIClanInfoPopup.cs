using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Xem clan khác (ClanInfoPopup gốc): banner, tên, mô tả, Type/Members/Power/Tier, danh sách thành viên,
// Join (Open) / Request (Approval Only) / "Requested". Đang có clan thì ẩn khối tham gia.
public class UIClanInfoPopup : BaseUI
{
    public ClanBannerView Banner;
    public TMP_Text NameText;
    public TMP_Text DescriptionText;
    public TMP_Text JoinSettingText;
    public TMP_Text MemberCountText;
    public TMP_Text TotalPowerText;
    public TMP_Text TierText;
    public ScrollRect ScrollRect;
    public Transform ListContent;
    public ClanMemberCard MemberCardPrefab;
    public ClanLoadingView LoadingView;
    public ClanLoadingView ActionLoadingView;
    public GameObject JoinRequestSection;
    public Button JoinButton;
    public Button RequestButton;
    public GameObject RequestedState;
    public Button CloseButton;
    public TMP_Text TypeLabel;
    public TMP_Text MembersLabel;
    public TMP_Text PowerLabel;
    public TMP_Text TierLabel;
    public TMP_Text JoinButtonText;
    public TMP_Text RequestButtonText;
    public TMP_Text RequestedText;

    private ClanModel clan;
    private bool requested;
    private bool isBusy;
    private Action<bool> onRequested;
    private readonly List<ClanMemberCard> cards = new List<ClanMemberCard>();

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(TypeLabel, "Type");
        ClanUIUtil.SetText(MembersLabel, "Members");
        ClanUIUtil.SetText(PowerLabel, "Power");
        ClanUIUtil.SetText(TierLabel, "Tier");
        ClanUIUtil.SetText(JoinButtonText, "Join");
        ClanUIUtil.SetText(RequestButtonText, "Request");
        ClanUIUtil.SetText(RequestedText, "Requested");
        if (JoinButton != null) JoinButton.onClick.AddListener(OnJoinClicked);
        if (RequestButton != null) RequestButton.onClick.AddListener(OnRequestClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseClicked);
        if (MemberCardPrefab != null) MemberCardPrefab.gameObject.SetActive(false);
    }

    // ftq gốc (clan, requested, …).
    public void Setup(ClanModel clan, bool requested, Action<bool> onRequested = null)
    {
        this.clan = clan;
        this.requested = requested;
        this.onRequested = onRequested;
        isBusy = false;
        if (ActionLoadingView != null) ActionLoadingView.Hide();
        DrawHeader(clan);
        RefreshJoinSection();
        Load();
    }

    // ftr gốc: tải chi tiết (kèm thành viên).
    private void Load()
    {
        if (LoadingView != null) LoadingView.ShowLoading();
        ClanController.Instance.GetDetails(clan.ClanId, (ok, details, error) =>
        {
            if (this == null) return;
            if (!ok)
            {
                if (LoadingView != null) LoadingView.ShowError(error ?? "Failed to load clan data", Load);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
            clan = details;
            DrawHeader(details);
            DrawMembers(details);
            RefreshJoinSection();
        });
    }

    // ftt gốc.
    private void DrawHeader(ClanModel c)
    {
        if (Banner != null) Banner.SetBanner(c.Banner);
        ClanUIUtil.SetText(NameText, c.ClanName);
        ClanUIUtil.SetText(DescriptionText, c.Description);
        if (JoinSettingText != null)
        {
            JoinSettingText.text = ClanRoleExt.JoinSettingText(c.JoinSetting);
            JoinSettingText.color = ClanRoleExt.JoinSettingColor(c.JoinSetting);
        }
        ClanUIUtil.SetText(MemberCountText, c.MemberCount + "/" + StaticClanData.MAX_MEMBERS);
        ClanUIUtil.SetText(TotalPowerText, ClanUIUtil.PowerText(c.TotalPower));
        ClanUIUtil.SetText(TierText, ClanRoleExt.TierText(c.ClanTier));
    }

    private void DrawMembers(ClanModel c)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
        List<ClanMemberModel> members = new List<ClanMemberModel>(c.Members);
        members.Sort((a, b) => b.Role != a.Role ? b.Role.CompareTo(a.Role) : b.Power.CompareTo(a.Power));
        for (int i = 0; i < members.Count; i++)
        {
            ClanMemberCard card = Instantiate(MemberCardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Setup(members[i], ClanController.Instance.MyUserId, null);
            cards.Add(card);
        }
    }

    // fts gốc.
    private void RefreshJoinSection()
    {
        bool canJoin = !ClanController.Instance.HasClan;
        ClanUIUtil.SetActive(JoinRequestSection, canJoin);
        if (!canJoin) return;
        bool open = clan.JoinSetting == ClanJoinSetting.Open;
        ClanUIUtil.SetActive(JoinButton, open && !requested);
        ClanUIUtil.SetActive(RequestButton, !open && !requested);
        ClanUIUtil.SetActive(RequestedState, requested);
        if (JoinButton != null) JoinButton.interactable = !clan.IsFull;
        if (RequestButton != null) RequestButton.interactable = !clan.IsFull;
    }

    // ftv gốc.
    private void OnJoinClicked()
    {
        if (isBusy) return;
        SetBusy(true);
        ClanController.Instance.JoinOpen(clan.ClanId, (ok, joined, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error);
                return;
            }
            Close();
        });
    }

    // ftw gốc.
    private void OnRequestClicked()
    {
        if (isBusy) return;
        SetBusy(true);
        ClanController.Instance.RequestJoin(clan.ClanId, (ok, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error);
                return;
            }
            requested = true;
            RefreshJoinSection();
            onRequested?.Invoke(true);
        });
    }

    // ftx gốc.
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        if (ActionLoadingView == null) return;
        if (busy) ActionLoadingView.ShowLoading();
        else ActionLoadingView.Hide();
    }

    private void OnCloseClicked()
    {
        if (!isBusy) Close();
    }
}
