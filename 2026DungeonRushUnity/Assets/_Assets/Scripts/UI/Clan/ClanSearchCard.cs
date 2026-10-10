using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 clan trong kết quả tìm (ClanSearchCard gốc): banner, tên, thành viên "{n}/50", Power, Tier màu,
// nút Join (Open) / Request (Approval Only) / trạng thái Requested; bấm thẻ → ClanInfoPopup.
public class ClanSearchCard : MonoBehaviour
{
    public ClanBannerView Banner;
    public TMP_Text NameText;
    public TMP_Text MembersText;
    public TMP_Text PowerText;
    public TMP_Text TierText;
    public Button JoinButton;
    public Button RequestButton;
    public GameObject RequestedState;
    public Button CardButton;
    public ClanLoadingView ActionLoading;
    public TMP_Text MembersLabel;
    public TMP_Text JoinButtonText;
    public TMP_Text RequestButtonText;
    public TMP_Text RequestedText;

    private ClanModel clan;
    private bool requested;
    private Action<ClanModel> onJoin;
    private Action<ClanModel> onRequest;
    private Action<ClanModel> onClick;

    public ClanModel Clan => clan;

    private void Awake()
    {
        if (JoinButton != null) JoinButton.onClick.AddListener(() => onJoin?.Invoke(clan));
        if (RequestButton != null) RequestButton.onClick.AddListener(() => onRequest?.Invoke(clan));
        if (CardButton != null) CardButton.onClick.AddListener(() => onClick?.Invoke(clan));
        ClanUIUtil.SetText(MembersLabel, "Members");
        ClanUIUtil.SetText(JoinButtonText, "Join");
        ClanUIUtil.SetText(RequestButtonText, "Request");
        ClanUIUtil.SetText(RequestedText, "Requested");
    }

    // fwg gốc.
    public void Setup(ClanModel clan, bool requested, Action<ClanModel> onJoin, Action<ClanModel> onRequest, Action<ClanModel> onClick)
    {
        this.clan = clan;
        this.onJoin = onJoin;
        this.onRequest = onRequest;
        this.onClick = onClick;
        if (Banner != null) Banner.SetBanner(clan.Banner);
        ClanUIUtil.SetText(NameText, clan.ClanName);
        ClanUIUtil.SetText(MembersText, clan.MemberCount + "/" + StaticClanData.MAX_MEMBERS);
        ClanUIUtil.SetText(PowerText, ClanUIUtil.PowerText(clan.TotalPower));
        ClanUIUtil.SetText(TierText, ClanRoleExt.TierText(clan.ClanTier));
        if (ActionLoading != null) ActionLoading.Hide();
        SetRequested(requested);
    }

    // fwi gốc.
    public void SetRequested(bool requested)
    {
        this.requested = requested;
        RefreshButtons();
    }

    public void SetBusy(bool busy)
    {
        if (ActionLoading == null) return;
        if (busy) ActionLoading.ShowLoading();
        else ActionLoading.Hide();
    }

    // fwh gốc.
    private void RefreshButtons()
    {
        bool open = clan.JoinSetting == ClanJoinSetting.Open;
        ClanUIUtil.SetActive(JoinButton, open && !requested);
        ClanUIUtil.SetActive(RequestButton, !open && !requested);
        ClanUIUtil.SetActive(RequestedState, requested);
        if (JoinButton != null) JoinButton.interactable = !clan.IsFull;
        if (RequestButton != null) RequestButton.interactable = !clan.IsFull;
    }
}
