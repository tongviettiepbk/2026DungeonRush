using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút Starter Pack ở lobby, cùng cột nút chào hàng: đếm ngược, hết giờ hoặc mua rồi thì ẩn.
public class StarterPackButtonView : MonoBehaviour
{
    public Button Button;
    public TMP_Text TimerText;

    private long lastSecondsLeft = -1;

    private void Awake()
    {
        Button.onClick.AddListener(UIStarterPackPopup.Show);
        StarterPackService.Changed += Refresh;
    }

    private void OnDestroy()
    {
        StarterPackService.Changed -= Refresh;
    }

    private void Update()
    {
        long left = StarterPackService.GetSecondsLeft();
        if (left == lastSecondsLeft)
            return;

        lastSecondsLeft = left;
        if (TimerText != null)
            TimerText.text = ShopUIUtil.FormatDuration(left);
        if (left <= 0)
            Refresh();
    }

    public void Refresh()
    {
        gameObject.SetActive(StarterPackService.IsVisible);
    }
}
