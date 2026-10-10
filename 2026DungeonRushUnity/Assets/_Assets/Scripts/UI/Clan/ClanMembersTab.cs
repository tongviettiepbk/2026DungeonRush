using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tab Members khi đã có clan (ClanMembersTab gốc): banner, tên, Type/Tier/Members/Power, mô tả, nút Settings,
// nút Requests (Captain/Leader, chấm đỏ khi có đơn), announcement (Captain/Leader bấm để sửa), danh sách thành viên
// (sắp role giảm dần rồi Power giảm dần), bấm thành viên → ClanPlayerPopup.
public class ClanMembersTab : MonoBehaviour
{
    public ClanBannerView Banner;
    public TMP_Text ClanNameText;
    public TMP_Text TierText;
    public TMP_Text JoinSettingText;
    public TMP_Text MemberCountText;
    public TMP_Text TotalPowerText;
    public TMP_Text DescriptionText;
    public Button SettingsButton;
    public Button RequestsButton;
    public TMP_Text AnnouncementText;
    public Button EditAnnouncementButton;
    public GameObject EditAnnouncementIcon;
    public GameObject AnnouncementEmpty;
    public ScrollRect ScrollRect;
    public Transform ListContent;
    public ClanMemberCard CardPrefab;
    public ClanLoadingView LoadingView;
    public TMP_Text TypeLabel;
    public TMP_Text MembersLabel;
    public TMP_Text PowerLabel;
    public TMP_Text TierLabel;
    public TMP_Text NoAnnouncementText;

    private ClanModel clan;
    private readonly List<ClanMemberCard> cards = new List<ClanMemberCard>();
    private GameObject requestsDot;

    private static ClanController Controller => ClanController.Instance;

    private void Awake()
    {
        ClanUIUtil.SetText(TypeLabel, "Type");
        ClanUIUtil.SetText(MembersLabel, "Members");
        ClanUIUtil.SetText(PowerLabel, "Power");
        ClanUIUtil.SetText(TierLabel, "Tier");
        ClanUIUtil.SetText(NoAnnouncementText, "No announcement yet");
        if (SettingsButton != null) SettingsButton.onClick.AddListener(OnSettingsClicked);
        if (RequestsButton != null) RequestsButton.onClick.AddListener(OnRequestsClicked);
        if (EditAnnouncementButton != null) EditAnnouncementButton.onClick.AddListener(OnAnnouncementClicked);
        if (CardPrefab != null) CardPrefab.gameObject.SetActive(false);
        Transform dot = RequestsButton != null ? RequestsButton.transform.Find("Content/NotificationUI") : null;
        requestsDot = dot != null ? dot.gameObject : null;
        // Khối Join/Request trong header chỉ dùng khi xem clan khác — tab của mình thì ẩn.
        Transform join = transform.Find("Header/JoinSection");
        if (join != null) join.gameObject.SetActive(false);
        ClanController.ClanUpdated += OnClanUpdated;
    }

    private void OnDestroy()
    {
        ClanController.ClanUpdated -= OnClanUpdated;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (Controller.CurrentClan != null) Draw(Controller.CurrentClan);
        Refresh();
    }

    // Refresh gốc: tải lại clan của mình.
    public void Refresh()
    {
        if (clan == null && LoadingView != null) LoadingView.ShowLoading();
        Controller.LoadMyClan((ok, loaded, error) =>
        {
            if (this == null) return;
            if (!ok)
            {
                if (LoadingView != null) LoadingView.ShowError(error ?? "Failed to load clan data", Refresh);
                return;
            }
            if (LoadingView != null) LoadingView.Hide();
        });
    }

    private void OnClanUpdated(ClanModel updated)
    {
        if (updated != null && this != null && gameObject.activeInHierarchy) Draw(updated);
    }

    // fuq/fur gốc.
    private void Draw(ClanModel c)
    {
        clan = c;
        if (Banner != null) Banner.SetBanner(c.Banner);
        ClanUIUtil.SetText(ClanNameText, c.ClanName);
        ClanUIUtil.SetText(TierText, ClanRoleExt.TierText(c.ClanTier));
        if (JoinSettingText != null)
        {
            JoinSettingText.text = ClanRoleExt.JoinSettingText(c.JoinSetting);
            JoinSettingText.color = ClanRoleExt.JoinSettingColor(c.JoinSetting);
        }
        ClanUIUtil.SetText(MemberCountText, c.MemberCount + "/" + StaticClanData.MAX_MEMBERS);
        ClanUIUtil.SetText(TotalPowerText, ClanUIUtil.PowerText(c.TotalPower));
        ClanUIUtil.SetText(DescriptionText, c.Description);

        bool canManage = ClanRoleExt.CanManage(c.MyRole);
        ClanUIUtil.SetActive(RequestsButton, canManage);
        ClanUIUtil.SetActive(SettingsButton, true);
        DrawAnnouncement(c.Announcement, canManage);
        DrawMembers(c);
        if (canManage) RefreshRequestsDot();
        else ClanUIUtil.SetActive(requestsDot, false);
    }

    // fus gốc.
    private void DrawAnnouncement(string text, bool canEdit)
    {
        bool empty = string.IsNullOrEmpty(text);
        ClanUIUtil.SetActive(AnnouncementText, !empty);
        ClanUIUtil.SetText(AnnouncementText, text);
        ClanUIUtil.SetActive(AnnouncementEmpty, empty);
        ClanUIUtil.SetActive(EditAnnouncementIcon, canEdit);
        if (EditAnnouncementButton != null) EditAnnouncementButton.interactable = canEdit;
    }

    // fut gốc.
    private void DrawMembers(ClanModel c)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) Destroy(cards[i].gameObject);
        }
        cards.Clear();
        List<ClanMemberModel> members = new List<ClanMemberModel>(c.Members);
        members.Sort((a, b) => b.Role != a.Role ? b.Role.CompareTo(a.Role) : b.Power.CompareTo(a.Power));
        string me = Controller.MyUserId;
        for (int i = 0; i < members.Count; i++)
        {
            ClanMemberCard card = Instantiate(CardPrefab, ListContent);
            card.gameObject.SetActive(true);
            card.Setup(members[i], me, OnMemberClicked);
            cards.Add(card);
        }
    }

    private void RefreshRequestsDot()
    {
        Controller.GetRequests((ok, list, error) =>
        {
            if (this != null) ClanUIUtil.SetActive(requestsDot, ok && list != null && list.Count > 0);
        });
    }

    // fuu gốc.
    private void OnMemberClicked(ClanMemberModel member)
    {
        UIClanPlayerPopup popup = UIManager.Instance.LoadUI(UIKey.ClanPlayerPopup) as UIClanPlayerPopup;
        if (popup != null) popup.Setup(member, clan);
    }

    private void OnSettingsClicked()
    {
        UIClanSettingsPopup popup = UIManager.Instance.LoadUI(UIKey.ClanSettingsPopup) as UIClanSettingsPopup;
        if (popup != null) popup.Setup(clan);
    }

    private void OnRequestsClicked()
    {
        UIClanRequestsPopup popup = UIManager.Instance.LoadUI(UIKey.ClanRequestsPopup) as UIClanRequestsPopup;
        if (popup != null) popup.Setup(() => { if (this != null) Refresh(); });
    }

    private void OnAnnouncementClicked()
    {
        if (clan == null || !ClanRoleExt.CanManage(clan.MyRole)) return;
        UIEditAnnouncementPopup popup = UIManager.Instance.LoadUI(UIKey.EditAnnouncementPopup) as UIEditAnnouncementPopup;
        if (popup != null) popup.Setup(clan.Announcement);
    }
}
