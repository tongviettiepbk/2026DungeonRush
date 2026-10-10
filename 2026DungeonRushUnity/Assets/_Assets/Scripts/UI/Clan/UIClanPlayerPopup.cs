using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thao tác với 1 thành viên (ClanPlayerPopup gốc). Chính mình → chỉ Leave. Người khác: Promote/Demote khi mình Leader
// và họ không phải Leader (Promote nếu họ là Member); Kick khi frk(mình, họ).
public class UIClanPlayerPopup : BaseUI
{
    public Image AvatarImage;
    public Button AvatarButton;
    public Button HiddenPlayerButton;
    public TMP_Text NameText;
    public TMP_Text PowerText;
    public TMP_Text RoleText;
    public Button PromoteDemoteButton;
    public TMP_Text PromoteDemoteLabel;
    public Button KickButton;
    public Button LeaveButton;
    public Button CloseButton;
    public ClanLoadingView LoadingView;
    public TMP_Text KickButtonText;
    public TMP_Text LeaveButtonText;

    private ClanMemberModel member;
    private ClanModel clan;
    private bool isBusy;

    protected override void Awake()
    {
        base.Awake();
        ClanUIUtil.SetText(KickButtonText, "Kick");
        ClanUIUtil.SetText(LeaveButtonText, "Leave");
        if (CloseButton != null) CloseButton.onClick.AddListener(OnCloseClicked);
        if (HiddenPlayerButton != null) HiddenPlayerButton.onClick.AddListener(OnCloseClicked);
        if (PromoteDemoteButton != null) PromoteDemoteButton.onClick.AddListener(OnPromoteDemoteClicked);
        if (KickButton != null) KickButton.onClick.AddListener(OnKickClicked);
        if (LeaveButton != null) LeaveButton.onClick.AddListener(OnLeaveClicked);
    }

    // fvd + eip gốc.
    public void Setup(ClanMemberModel member, ClanModel clan)
    {
        this.member = member;
        this.clan = clan;
        isBusy = false;
        if (LoadingView != null) LoadingView.Hide();
        Draw();
    }

    // fvf gốc.
    private void Draw()
    {
        ClanUIUtil.SetText(NameText, member.PlayerName);
        ClanUIUtil.SetText(PowerText, ClanUIUtil.PowerText(member.Power));
        ClanUIUtil.SetText(RoleText, ClanRoleExt.RoleName(member.Role));
        bool isMe = member.UserId == ClanController.Instance.MyUserId;
        if (isMe) DrawSelf();
        else DrawOther(clan != null ? clan.MyRole : ClanController.Instance.MyRole);
    }

    // fvg gốc.
    private void DrawSelf()
    {
        ClanUIUtil.SetActive(PromoteDemoteButton, false);
        ClanUIUtil.SetActive(KickButton, false);
        ClanUIUtil.SetActive(LeaveButton, true);
    }

    // fvh gốc.
    private void DrawOther(ClanRole myRole)
    {
        ClanUIUtil.SetActive(LeaveButton, false);
        bool canPromote = myRole == ClanRole.Leader && member.Role != ClanRole.Leader;
        ClanUIUtil.SetActive(PromoteDemoteButton, canPromote);
        ClanUIUtil.SetText(PromoteDemoteLabel, member.Role == ClanRole.Member ? "Promote" : "Demote");
        ClanUIUtil.SetActive(KickButton, ClanRoleExt.CanActOn(myRole, member.Role));
    }

    // fvi gốc.
    private void OnLeaveClicked()
    {
        if (isBusy) return;
        SetBusy(true);
        ClanController.Instance.Leave((ok, disbanded, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error ?? "Failed to leave clan");
                return;
            }
            Close();
        });
    }

    // fvj gốc.
    private void OnPromoteDemoteClicked()
    {
        if (isBusy) return;
        SetBusy(true);
        ClanController.Instance.PromoteOrDemote(member.UserId, (ok, role, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error ?? "Failed to change role");
                return;
            }
            member.Role = role;
            Draw();
        });
    }

    // fvk gốc.
    private void OnKickClicked()
    {
        if (isBusy) return;
        SetBusy(true);
        ClanController.Instance.Kick(member.UserId, (ok, error) =>
        {
            SetBusy(false);
            if (!ok)
            {
                ClanUIUtil.Toast(error ?? "Failed to kick member");
                return;
            }
            Close();
        });
    }

    // fvn gốc.
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
