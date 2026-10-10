using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanh so điểm ngày giữa 2 clan (ClanWarProgressSection gốc): banner + tên + tier + server mỗi bên, thanh điểm ngày,
// MVP ngày; bấm banner → bảng đóng góp của clan đó. Header thường / Championship.
public class ClanWarProgressSection : MonoBehaviour
{
    public GameObject NormalHeaderRoot;
    public GameObject LeadershipHeaderRoot;
    public RectTransform BarBackgroundImage;
    public RectTransform MyFillRect;
    public RectTransform EnemyFillRect;
    public TMP_Text MyDailyTotalText;
    public TMP_Text EnemyDailyTotalText;
    public ClanBannerView MyBanner;
    public ClanBannerView EnemyBanner;
    public Button MyBannerButton;
    public Button EnemyBannerButton;
    public TMP_Text MyClanNameText;
    public TMP_Text MyTierText;
    public TMP_Text MyServerText;
    public TMP_Text EnemyClanNameText;
    public TMP_Text EnemyTierText;
    public TMP_Text EnemyServerText;
    public ClanWarMvpCard MyMvpCard;
    public ClanWarMvpCard EnemyMvpCard;
    public TMP_Text ProgressTitleText;
    public TMP_Text ProgressDayText;

    // gcw gốc.
    public void Set(ClanWarWireDTO war, Action<ClanWarClanSideDTO> onMyBanner, Action<ClanWarClanSideDTO> onEnemyBanner)
    {
        if (NormalHeaderRoot != null) NormalHeaderRoot.SetActive(!war.IsLeadership);
        if (LeadershipHeaderRoot != null) LeadershipHeaderRoot.SetActive(war.IsLeadership);
        if (ProgressTitleText != null) ProgressTitleText.text = war.IsLeadership ? "Championship War" : "War Progress";
        if (ProgressDayText != null) ProgressDayText.text = "Day " + Mathf.Clamp(war.activeDay, 1, 6);

        SetSide(war.myClan, MyBanner, MyClanNameText, MyTierText, MyServerText);
        SetSide(war.enemyClan, EnemyBanner, EnemyClanNameText, EnemyTierText, EnemyServerText);
        Bind(MyBannerButton, war.myClan, onMyBanner);
        Bind(EnemyBannerButton, war.enemyClan, onEnemyBanner);

        int my = war.liveBar != null ? war.liveBar.myDaily : 0;
        int enemy = war.liveBar != null ? war.liveBar.enemyDaily : 0;
        if (MyDailyTotalText != null) MyDailyTotalText.text = ClanWarController.FormatNumber(my);
        if (EnemyDailyTotalText != null) EnemyDailyTotalText.text = ClanWarController.FormatNumber(enemy);
        SetBar(my, enemy);

        if (MyMvpCard != null) MyMvpCard.Set(war.myMvp, null);
        if (EnemyMvpCard != null) EnemyMvpCard.Set(war.enemyMvp, null);
    }

    // Tỉ lệ thanh: phần mình từ trái, địch từ phải (cùng 0 → chia đôi).
    private void SetBar(int my, int enemy)
    {
        if (BarBackgroundImage == null) return;
        float width = BarBackgroundImage.rect.width;
        float ratio = my + enemy > 0 ? (float)my / (my + enemy) : 0.5f;
        if (MyFillRect != null) MyFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * ratio);
        if (EnemyFillRect != null) EnemyFillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width * (1f - ratio));
    }

    // gcx gốc.
    private static void Bind(Button button, ClanWarClanSideDTO side, Action<ClanWarClanSideDTO> onClick)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(side));
    }

    // gcy gốc.
    public static void SetSide(ClanWarClanSideDTO side, ClanBannerView banner, TMP_Text name, TMP_Text tier, TMP_Text server)
    {
        if (side == null) return;
        if (banner != null) banner.SetBanner(side.Banner);
        if (name != null) name.text = side.clanName;
        if (tier != null) tier.text = "Tier " + ClanRoleExt.TierText(side.rewardTier);
        if (server != null) server.text = side.server;
    }
}
