using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô ngày trong UISevenDayPopup. Nền đổi theo quy ước ô thưởng Battle Pass: nhận được = xanh lá, đã nhận = xanh dương
// (icon mờ + dấu tick), chưa tới = xám. Ô ngày 7 giữ nền riêng (isFixedBackground).
public class SevenDayCellUI : MonoBehaviour
{
    private const float TODAY_SCALE = 1.06f;
    private const float CLAIMED_ALPHA = 0.5f;

    public TMP_Text txtDay;
    public Image imgIcon;
    public TMP_Text txtAmount;
    public Image imgIcon2;
    public TMP_Text txtAmount2;
    public GameObject objClaimed;
    public GameObject objToday;

    public Image imgBackground;
    public Sprite sprClaimable;
    public Sprite sprClaimed;
    public Sprite sprWaiting;
    public bool isFixedBackground;

    public void Setup(int dayIndex, ShopReward[] rewards, Sprite icon, Sprite icon2, bool isClaimed, bool isToday)
    {
        txtDay.text = Loc.L("Popup.SevenDay.Day", dayIndex + 1);
        imgIcon.sprite = icon;
        txtAmount.text = ShopUIUtil.FormatAmount(rewards[0].amount);

        bool hasSecond = rewards.Length > 1 && imgIcon2 != null;
        if (imgIcon2 != null) imgIcon2.gameObject.SetActive(hasSecond);
        if (txtAmount2 != null) txtAmount2.gameObject.SetActive(hasSecond);
        if (hasSecond)
        {
            imgIcon2.sprite = icon2;
            txtAmount2.text = ShopUIUtil.FormatAmount(rewards[1].amount);
        }

        objClaimed.SetActive(isClaimed);
        objToday.SetActive(isToday);

        if (imgBackground != null && isFixedBackground == false)
        {
            imgBackground.sprite = isClaimed ? sprClaimed : (isToday ? sprClaimable : sprWaiting);
            transform.localScale = Vector3.one * (isToday ? TODAY_SCALE : 1f);
        }

        float alpha = isClaimed ? CLAIMED_ALPHA : 1f;
        SetAlpha(imgIcon, alpha);
        SetAlpha(txtAmount, alpha);
        SetAlpha(imgIcon2, alpha);
        SetAlpha(txtAmount2, alpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
