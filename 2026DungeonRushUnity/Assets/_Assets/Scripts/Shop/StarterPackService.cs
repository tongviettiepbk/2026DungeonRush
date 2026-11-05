using System;

// Gói khởi đầu: mở 1 lần từ level 3, chào trong 48 giờ, mua được 1 lần. LỆCH GỐC có chủ ý: gốc v41 tắt gói này,
// user duyệt bật lại 2026-11-05 để mở cửa đơn đầu. Xem GDD/analytics/GROWTH_PLAN.md mục 5.3-B.
public static class StarterPackService
{
    public const int UNLOCK_PLAYER_LEVEL = 3;
    public const long DURATION_SECONDS = 48 * 3600;

    public static event Action Changed;

    private static UserShopData User => GameData.userData.shop;

    public static bool IsVisible => User.isStarterPackBought == false && User.starterPackStartTime > 0 && GetSecondsLeft() > 0;

    public static long GetSecondsLeft()
    {
        return Math.Max(0, User.starterPackStartTime + DURATION_SECONDS - ShopService.Now());
    }

    // Đủ level lần đầu thì bắt đầu đếm giờ. Trả true nếu vừa bắt đầu.
    public static bool TryStart()
    {
        if (User.starterPackStartTime > 0 || User.isStarterPackBought || GameData.userData.player.playerLevel < UNLOCK_PLAYER_LEVEL)
        {
            return false;
        }

        User.starterPackStartTime = ShopService.Now();
        User.isDataChanged = true;
        Changed?.Invoke();
        return true;
    }

    public static void OnPurchased()
    {
        User.isStarterPackBought = true;
        User.isDataChanged = true;
        Changed?.Invoke();
    }
}
