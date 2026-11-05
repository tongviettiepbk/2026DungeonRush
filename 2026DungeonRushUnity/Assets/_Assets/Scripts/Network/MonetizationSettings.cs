// Mã của các SDK kiếm tiền (cùng kiểu FirebaseSettings). Để trống ô nào thì SDK đó không khởi động trên máy thật:
//   - Chưa có ad unit  → không init MAX, bấm xem quảng cáo báo chưa sẵn sàng, KHÔNG phát thưởng.
//   - Chưa có dev key  → không init AppsFlyer, mọi lời gọi AppsflyerHelper thành no-op.
// SDK key MAX + AdMob App ID nằm ở AppLovin > Integration Manager (AppLovinSettings.asset), không nằm ở đây.
public static class MonetizationSettings
{
#if UNITY_IOS
    public const string MAX_REWARDED_AD_UNIT = "";
    public const string MAX_INTERSTITIAL_AD_UNIT = "";
#else
    public const string MAX_REWARDED_AD_UNIT = "06cc2dcafbeac708";
    // Chưa dùng: game chưa có chỗ nào gọi interstitial.
    public const string MAX_INTERSTITIAL_AD_UNIT = "a48993637a2eb643";
#endif

    public const string APPSFLYER_DEV_KEY = "XM6HPCReBAqLH5uCaQHRDY";
    // App ID dạng số trên App Store Connect, chỉ iOS cần.
    public const string APPSFLYER_IOS_APP_ID = "";
}
