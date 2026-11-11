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
    // Reskin Đợt 8: chính mình = thẻ xanh (đổi sprite), không tô màu đè.
    public Sprite NormalSprite;       // Clan/card_gray
    public Sprite SelfSprite;         // Clan/card_blue

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
        if (ProfileImage != null) ProfileImage.sprite = AvatarConfig.Get(entry.avatarId);
        if (NameText != null) NameText.text = entry.playerName;
        if (PointsText != null) PointsText.text = Loc.L("Clan.War.Points", ClanWarController.FormatNumber(entry.points));
        bool self = entry.userId == myUserId;
        if (CardBackground != null)
        {
            if (NormalSprite != null && SelfSprite != null) CardBackground.sprite = self ? SelfSprite : NormalSprite;
            else CardBackground.color = self ? SelfColor : NormalColor;
        }
    }
}
