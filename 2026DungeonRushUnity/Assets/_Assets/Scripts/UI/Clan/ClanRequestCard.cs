using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 yêu cầu xin vào clan (ClanRequestCard gốc): tên, Power, Accept / Deny.
public class ClanRequestCard : MonoBehaviour
{
    public Image AvatarImage;
    public TMP_Text NameText;
    public TMP_Text PowerText;
    public Button AcceptButton;
    public Button DenyButton;
    public Button CardButton;
    public TMP_Text AcceptButtonText;
    public TMP_Text DenyButtonText;

    private ClanRequestModel request;
    private Action<ClanRequestModel> onAccept;
    private Action<ClanRequestModel> onDeny;
    private Action<ClanRequestModel> onClick;

    private void Awake()
    {
        if (AcceptButton != null) AcceptButton.onClick.AddListener(() => onAccept?.Invoke(request));
        if (DenyButton != null) DenyButton.onClick.AddListener(() => onDeny?.Invoke(request));
        if (CardButton != null) CardButton.onClick.AddListener(() => onClick?.Invoke(request));
        ClanUIUtil.SetText(AcceptButtonText, "Accept");
        ClanUIUtil.SetText(DenyButtonText, "Deny");
    }

    // fvr gốc.
    public void Setup(ClanRequestModel request, Action<ClanRequestModel> onAccept, Action<ClanRequestModel> onDeny, Action<ClanRequestModel> onClick)
    {
        this.request = request;
        this.onAccept = onAccept;
        this.onDeny = onDeny;
        this.onClick = onClick;
        ClanUIUtil.SetText(NameText, request.PlayerName);
        ClanUIUtil.SetText(PowerText, ClanUIUtil.PowerText(request.Power));
        SetInteractable(true);
    }

    public void SetInteractable(bool on)
    {
        if (AcceptButton != null) AcceptButton.interactable = on;
        if (DenyButton != null) DenyButton.interactable = on;
    }
}
