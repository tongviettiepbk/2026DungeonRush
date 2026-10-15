using System.Globalization;
using TMPro;
using UnityEngine;

// Port BoostButton gốc (nút "Boost x2 +5m" cạnh nút Loot): xem quảng cáo → EXP nhận được ×AdBoostWorth
// trong AdBoostDuration giây; xem tiếp khi đang chạy thì cộng dồn, trần 12 lượt. Logic ở ShopService (AdBoost*).
//  - Nhãn (jwh): "<sprite=0>" + "Boost x{mult}\n+{thời lượng}", sprite 0 = icon quảng cáo (IsAd).
//  - Đang chạy (jwe/jwf): hiện chữ thời gian còn lại + thanh Overlay dài theo còn lại / trần.
//  - Ẩn khi playerLevel < VisibleAtPlayerLevel (3).
// Hiệu ứng ấn/nhả dùng ForgeButtonView (cùng kiểu nút với Loot gốc).
public class BoostButtonView : MonoBehaviour
{
    // jwf: chiều rộng Overlay = tỉ lệ còn lại × 220.838 + 13.712 (ykx / ykw gốc).
    private const float OVERLAY_MIN_WIDTH = 13.712f;
    private const float OVERLAY_RANGE_WIDTH = 220.838f;
    private const string LABEL_FORMAT = "Boost x{0}\n+{1}"; // UI.Boost.Label (EN)

    public ForgeButtonView pressView;
    public TMP_Text txtLabel;
    public TMP_Text txtRemainingTime;
    public RectTransform overlay;

    private bool isWaitingAd;
    private float lastMultiplier = -1f;
    private int lastDuration = -1;
    private long lastRemaining = -1;

    private void Awake()
    {
        if (pressView != null)
            pressView.onReleased += OnClickBoost;
    }

    private void OnEnable()
    {
        lastMultiplier = -1f;
        lastRemaining = -1;
        UpdateView();
    }

    private void Update()
    {
        UpdateView();
    }

    // Gọi khi playerLevel đổi (UIMainLobby.Refresh / RefreshExp).
    public void Refresh()
    {
        bool isVisible = GameData.userData.player.playerLevel >= StaticShopData.AD_BOOST_VISIBLE_LEVEL;
        if (gameObject.activeSelf != isVisible)
            gameObject.SetActive(isVisible);
    }

    private void OnClickBoost()
    {
        if (pressView != null)
            pressView.Release();
        if (isWaitingAd)
            return;

        isWaitingAd = true;
        MediationAds.Instance.ShowRewardedVideoAd("boost_button", result =>
        {
            isWaitingAd = false;
            if (result == ShowResultADS.Finished)
                ShopService.AddAdBoost();
        });
    }

    private void UpdateView()
    {
        // Nhãn chỉ đổi khi Mastery AdBoostWorth / AdBoostDuration nâng cấp.
        float multiplier = ShopService.GetAdBoostMultiplier();
        int duration = ShopService.GetAdBoostDuration();
        if (multiplier != lastMultiplier || duration != lastDuration)
        {
            lastMultiplier = multiplier;
            lastDuration = duration;
            if (txtLabel != null)
                txtLabel.text = "<sprite=0>" + string.Format(LABEL_FORMAT, FormatMultiplier(multiplier), ShopUIUtil.FormatDuration(duration));
        }

        long remaining = ShopService.GetAdBoostRemaining();
        if (remaining == lastRemaining)
            return;
        lastRemaining = remaining;

        bool isActive = remaining > 0;
        ShopUIUtil.SetActive(txtRemainingTime, isActive);
        ShopUIUtil.SetActive(overlay, isActive);
        if (!isActive)
            return;

        ShopUIUtil.SetText(txtRemainingTime, ShopUIUtil.FormatDuration(remaining));
        if (overlay != null)
        {
            float fill = Mathf.Clamp01((float)remaining / ShopService.GetAdBoostMaxDuration());
            overlay.sizeDelta = new Vector2(fill * OVERLAY_RANGE_WIDTH + OVERLAY_MIN_WIDTH, overlay.sizeDelta.y);
        }
    }

    // jwi: số gần nguyên in số nguyên ("2"), ngược lại "0.##" ("2.5").
    private static string FormatMultiplier(float value)
    {
        float rounded = Mathf.Round(value);
        if (Mathf.Abs(rounded - value) < 0.0001f)
            return ((int)rounded).ToString();
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
