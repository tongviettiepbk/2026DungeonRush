using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng thành viên (ClanMemberCard gốc): tên, Power, vai trò, icon Leader/Captain, dòng của mình đổi màu.
public class ClanMemberCard : MonoBehaviour
{
    public Image AvatarImage;
    public TMP_Text NameText;
    public TMP_Text PowerText;
    public TMP_Text RoleText;
    public GameObject LeaderIcon;
    public GameObject CaptainIcon;
    public Button CardButton;
    public Image RowBackground;
    public Color NormalColor = Color.white;
    public Color SelfColor = new Color(1f, 0.93f, 0.6f, 1f);

    private ClanMemberModel member;
    private Action<ClanMemberModel> onClick;

    // fuh gốc.
    public void Setup(ClanMemberModel member, string myUserId, Action<ClanMemberModel> onClick)
    {
        this.member = member;
        this.onClick = onClick;
        ClanUIUtil.SetText(NameText, member.PlayerName);
        ClanUIUtil.SetText(PowerText, ClanUIUtil.PowerText(member.Power));
        ClanUIUtil.SetText(RoleText, ClanRoleExt.RoleName(member.Role));
        ClanUIUtil.SetActive(LeaderIcon, member.Role == ClanRole.Leader);
        ClanUIUtil.SetActive(CaptainIcon, member.Role == ClanRole.Captain);
        if (RowBackground != null) RowBackground.color = !string.IsNullOrEmpty(myUserId) && member.UserId == myUserId ? SelfColor : NormalColor;
        if (CardButton != null)
        {
            CardButton.onClick.RemoveAllListeners();
            CardButton.onClick.AddListener(() => this.onClick?.Invoke(this.member));
        }
    }
}
