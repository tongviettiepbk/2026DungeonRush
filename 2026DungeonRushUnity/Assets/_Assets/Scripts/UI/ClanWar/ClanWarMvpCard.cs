using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô MVP ngày của 1 phe (ClanWarMvpCard gốc): "MVP" + tên; chưa ai đóng góp → "No Members".
public class ClanWarMvpCard : MonoBehaviour
{
    public Button CardButton;
    public GameObject ContentRoot;
    public Image ProfileImage;
    public TMP_Text NameText;
    public GameObject PlaceholderRoot;
    public TMP_Text MvpLabelText;
    public TMP_Text MvpNoMembersText;

    private Action onClick;

    private void Awake()
    {
        if (CardButton != null) CardButton.onClick.AddListener(() => onClick?.Invoke());
        if (MvpLabelText != null) MvpLabelText.text = "MVP";
        if (MvpNoMembersText != null) MvpNoMembersText.text = "No Members";
    }

    // gcr gốc.
    public void Set(ClanWarMvpDTO mvp, Action onClick)
    {
        this.onClick = onClick;
        bool has = mvp != null && !string.IsNullOrEmpty(mvp.userId);
        if (ContentRoot != null) ContentRoot.SetActive(has);
        if (PlaceholderRoot != null) PlaceholderRoot.SetActive(!has);
        if (NameText != null) NameText.text = has ? mvp.playerName : string.Empty;
        if (CardButton != null) CardButton.interactable = has && onClick != null;
    }
}
