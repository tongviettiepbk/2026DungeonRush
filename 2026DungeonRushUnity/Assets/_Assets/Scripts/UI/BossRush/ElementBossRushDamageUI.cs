using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng bảng damage trong trận Boss Rush: hạng, avatar, tên, thanh đỏ = phần damage trong tổng đội, damage; dòng của mình tô xanh.
public class ElementBossRushDamageUI : MonoBehaviour
{
    public TMP_Text txtRank;
    public Image imgAvatar;
    public TMP_Text txtPlayerName;
    public RectTransform barFill;        // anchorMax.x = phần damage (0..1)
    public TMP_Text txtDamage;
    public Image imgBackground;
    public Color highlightedBackgroundColor = new Color(0.231f, 0.592f, 0.871f, 1f);
    public Color defaultBackgroundColor = new Color(0.094f, 0.094f, 0.094f, 1f);
    // Reskin: có sprite thì đổi sprite (xám / xanh của mình) thay cho tô màu
    public Sprite highlightedSprite;
    public Sprite defaultSprite;

    public RectTransform RectTransform => (RectTransform)transform;

    public void Show(BossRushDamageEntry entry)
    {
        txtPlayerName.text = entry.playerName;
        if (imgAvatar != null) imgAvatar.sprite = AvatarConfig.Get(entry.avatarId);
        if (imgBackground != null)
        {
            Sprite sprite = entry.isMine ? highlightedSprite : defaultSprite;
            if (sprite != null)
            {
                imgBackground.sprite = sprite;
                imgBackground.color = Color.white;
            }
            else imgBackground.color = entry.isMine ? highlightedBackgroundColor : defaultBackgroundColor;
        }
    }

    public void SetDamage(int rank, long damage, float share)
    {
        txtRank.text = rank.ToString();
        txtDamage.text = damage.ToLetter();
        if (barFill != null) barFill.anchorMax = new Vector2(Mathf.Clamp01(share), barFill.anchorMax.y);
    }
}
