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

    public void Setup(PvPLeaderboardEntryModel entry)
    {
        // jpu gốc: "#{rank}", dòng của mình thêm " (You)" (Common.YouSuffix).
        if (txtRank != null) txtRank.text = "#" + entry.Rank;
        string name = string.IsNullOrEmpty(entry.PlayerName) ? "Unknown" : entry.PlayerName;
        if (txtName != null) txtName.text = entry.IsCurrentPlayer ? name + " (You)" : name;
        if (txtTrophy != null) txtTrophy.text = "<sprite=0>" + entry.Trophy;
        if (imgBackground != null) imgBackground.color = entry.IsCurrentPlayer ? highlightColor : defaultColor;
    }
}
