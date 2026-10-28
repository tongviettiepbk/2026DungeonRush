using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng bảng xếp hạng (PvPLeaderboardElementUI gốc): hạng, tên, trophy; dòng của mình đổi màu nền.
public class ElementPvPLeaderboardUI : MonoBehaviour
{
    public TMP_Text txtRank;
    public TMP_Text txtName;
    public TMP_Text txtTrophy;
    public Image imgBackground;
    public Color defaultColor = Color.white;
    public Color highlightColor = new Color(1f, 0.93f, 0.6f, 1f);
    // Reskin: có sprite thì đổi nền xám ↔ xanh thay tô màu; avatar + Power như dòng Boss Rush.
    public Sprite defaultSprite;
    public Sprite highlightedSprite;
    public Image imgAvatar;
    public TMP_Text txtPower;

    public void Setup(PvPLeaderboardEntryModel entry)
    {
        // jpu gốc: "#{rank}", dòng của mình thêm " (You)" (Common.YouSuffix).
        if (txtRank != null) txtRank.text = "#" + entry.Rank;
        string name = string.IsNullOrEmpty(entry.PlayerName) ? Loc.L("Common.Unknown") : entry.PlayerName;
        if (txtName != null) txtName.text = entry.IsCurrentPlayer ? name + (" " + Loc.L("Common.YouSuffix")) : name;
        if (txtTrophy != null) txtTrophy.text = "<sprite=0>" + entry.Trophy;
        if (txtPower != null) txtPower.text = "<sprite=0>" + (entry.Power > 0d ? entry.Power.ToLetter() : "0");
        if (imgAvatar != null) imgAvatar.sprite = AvatarConfig.Get(entry.AvatarId);
        if (imgBackground != null)
        {
            if (defaultSprite != null && highlightedSprite != null)
            {
                imgBackground.sprite = entry.IsCurrentPlayer ? highlightedSprite : defaultSprite;
                imgBackground.color = Color.white;
            }
            else
            {
                imgBackground.color = entry.IsCurrentPlayer ? highlightColor : defaultColor;
            }
        }
    }
}
