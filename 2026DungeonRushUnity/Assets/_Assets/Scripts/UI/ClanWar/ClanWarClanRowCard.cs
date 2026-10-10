using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 clan trong bảng xếp hạng (ClanWarClanRowCard gốc): hạng, banner (vương miện nếu Champion), tên, Tier, Power, server.
public class ClanWarClanRowCard : MonoBehaviour
{
    public Button CardButton;
    public TMP_Text RankText;
    public ClanBannerView Banner;
    public TMP_Text NameText;
    public TMP_Text TierText;
    public TMP_Text TotalPowerText;
    public TMP_Text ServerText;
    public Image BackgroundImage;
    public Color NormalColor = Color.white;
    public Color HighlightColor = new Color(1f, 0.93f, 0.6f, 1f);

    // gbl gốc: (row, isMine, isChampion, onClick).
    public void Set(ClanWarClanRowDTO row, bool isMine, bool isChampion = false, Action onClick = null)
    {
        if (RankText != null) RankText.text = row.rank > 0 ? "#" + row.rank : string.Empty;
        if (Banner != null)
        {
            Banner.SetBanner(row.Banner);
            Banner.SetCrown(isChampion);
        }
        if (NameText != null) NameText.text = row.clanName;
        if (TierText != null) TierText.text = "Tier " + ClanRoleExt.TierText(row.tier);
        if (TotalPowerText != null) TotalPowerText.text = ClanUIUtil.PowerText(row.totalPower);
        if (ServerText != null) ServerText.text = row.server;
        if (BackgroundImage != null) BackgroundImage.color = isMine ? HighlightColor : NormalColor;
        if (CardButton != null)
        {
            CardButton.onClick.RemoveAllListeners();
            if (onClick != null) CardButton.onClick.AddListener(() => onClick());
        }
    }
}
