using UnityEngine;

// Luật popup bán hàng tự bật: tối đa 1 lần mỗi lần mở app; người chơi đóng dưới 2 giây thì im 24 giờ.
// Racing có popup tự bật 94% lượt bị đóng dưới 2 giây mà gần như không bán được, nhưng popup đầu phiên cũng là chỗ chốt đơn
// đầu, nên không tắt hẳn. Nút ở lobby vẫn luôn còn. Xem GDD/analytics/GROWTH_PLAN.md mục 5.3-D.
public static class AutoOfferGate
{
    private const float QUICK_CLOSE_SECONDS = 2f;
    private const long MUTE_SECONDS = 24 * 3600;

    private static bool isShownThisRun;
    private static float autoOpenedAt = -1f;

    public static void TryShow()
    {
        UserShopData user = GameData.userData.shop;

        if (isShownThisRun || ShopService.Now() < user.autoOfferMuteUntil)
        {
            return;
        }

        // Starter Pack chỉ có 48 giờ nên được ưu tiên hơn Weapon Offer.
        if (StarterPackService.IsVisible)
        {
            Open(UIStarterPackPopup.Show);
        }
        else if (WeaponOfferService.IsOfferVisible())
        {
            Open(UIOfferPopup.Show);
        }
    }

    private static void Open(System.Action show)
    {
        isShownThisRun = true;
        autoOpenedAt = Time.realtimeSinceStartup;
        Analytics.PaywallTrigger = Analytics.TRIGGER_AUTO;
        show();
        Analytics.PaywallTrigger = Analytics.TRIGGER_MANUAL;
    }

    // Popup bán hàng gọi khi đóng; popup do người chơi tự mở thì autoOpenedAt = -1, không tính.
    public static void OnPopupClosed()
    {
        if (autoOpenedAt < 0f)
        {
            return;
        }

        if (Time.realtimeSinceStartup - autoOpenedAt < QUICK_CLOSE_SECONDS)
        {
            UserShopData user = GameData.userData.shop;
            user.autoOfferMuteUntil = ShopService.Now() + MUTE_SECONDS;
            user.isDataChanged = true;
        }

        autoOpenedAt = -1f;
    }
}
