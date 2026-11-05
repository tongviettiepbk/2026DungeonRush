using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút thưởng 7 ngày ở lobby: có ô nhận được thì sáng hiệu ứng, chưa thì đếm tới ngày kế.
public class SevenDayButtonView : MonoBehaviour
{
    public Button Button;
    public TMP_Text TimerText;
    public GameObject objReady;

    private float nextRefreshAt;

    private void Awake()
    {
        Button.onClick.AddListener(() => UISevenDayPopup.Show());
        SevenDayService.Changed += Refresh;
    }

    private void OnDestroy()
    {
        SevenDayService.Changed -= Refresh;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshAt)
            return;

        nextRefreshAt = Time.unscaledTime + 1f;
        RefreshTimer();
    }

    public void Refresh()
    {
        gameObject.SetActive(SevenDayService.IsActive);
        if (objReady != null)
            objReady.SetActive(SevenDayService.CanClaim);
        RefreshTimer();
    }

    private void RefreshTimer()
    {
        if (TimerText == null)
            return;

        bool canClaim = SevenDayService.CanClaim;
        TimerText.gameObject.SetActive(canClaim == false);
        if (canClaim == false)
            TimerText.text = ShopUIUtil.FormatDuration((long)SevenDayService.GetTimeToNextClaim().TotalSeconds);
    }
}
