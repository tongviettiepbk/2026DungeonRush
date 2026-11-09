using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút thưởng 7 ngày ở lobby: luôn sáng hiệu ứng như các nút offer; chưa nhận được thì đếm tới ngày kế.
public class SevenDayButtonView : MonoBehaviour
{
    public Button Button;
    public TMP_Text TimerText;

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
