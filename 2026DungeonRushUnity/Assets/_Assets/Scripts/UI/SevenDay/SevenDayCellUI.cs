using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô ngày trong UISevenDayPopup.
public class SevenDayCellUI : MonoBehaviour
{
    public TMP_Text txtDay;
    public Image imgIcon;
    public TMP_Text txtAmount;
    public Image imgIcon2;
    public TMP_Text txtAmount2;
    public GameObject objClaimed;
    public GameObject objToday;

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
    }
}
