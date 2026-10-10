using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng bảng đóng góp (ClanWarContributionCard gốc): hạng, tên, "{pts} pts", dòng của mình đổi màu.
public class ClanWarContributionCard : MonoBehaviour
{
    public Button CardButton;
    public TMP_Text RankText;
    public Image ProfileImage;
    public TMP_Text NameText;
    public TMP_Text PointsText;
    public Image CardBackground;
    public Color NormalColor = Color.white;
    public Color SelfColor = new Color(1f, 0.93f, 0.6f, 1f);

    private Action onClick;

    private void Awake()
    {
        if (CardButton != null) CardButton.onClick.AddListener(() => onClick?.Invoke());
    }

    // gbm gốc.
    public void Set(ClanWarContributionEntryDTO entry, string myUserId, Action onClick)
    {
        this.onClick = onClick;
        if (RankText != null) RankText.text = "#" + entry.rank;
        if (NameText != null) NameText.text = entry.playerName;
        if (PointsText != null) PointsText.text = ClanWarController.FormatNumber(entry.points) + " pts";
        if (CardBackground != null) CardBackground.color = entry.userId == myUserId ? SelfColor : NormalColor;
    }
}
