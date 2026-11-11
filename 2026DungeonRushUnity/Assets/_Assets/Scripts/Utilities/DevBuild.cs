// Cờ loại bản build. Bật trong Editor và bản Dev (define DEV_BUILD do AndroidBuilder.BuildDev thêm lúc build):
// hiện tool/cheat, mua IAP và xem quảng cáo luôn thành công (không gọi store/MAX), tắt gửi Analytics/AppsFlyer.
// Bản Product (AndroidBuilder.BuildRelease) không có DEV_BUILD → mọi code dưới `#if UNITY_EDITOR || DEV_BUILD` bị loại.
public static class DevBuild
{
#if UNITY_EDITOR || DEV_BUILD
    public static readonly bool IsEnabled = true;
#else
    public static readonly bool IsEnabled = false;
#endif
}
