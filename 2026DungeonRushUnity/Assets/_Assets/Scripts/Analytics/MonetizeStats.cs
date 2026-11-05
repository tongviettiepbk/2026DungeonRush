using System;

// Bộ đếm kiếm tiền nằm trong save (UserProfileData.monetize), theo tài khoản, sống qua cài lại máy và đổi máy.
public class MonetizeStatsData
{
    public int iapCount { get; set; }
    public double iapUsdTotal { get; set; }     // giá niêm yết USD của SKU, không phải tiền thật thu (xem StaticShopData.GetPriceUsd)
    public double lastIapMs { get; set; }
    public int rewardedTotal { get; set; }
    public double lastRewardedMs { get; set; }
}

// Ghi đơn mua + lượt rewarded vào save và suy ra nhóm monetize. Port từ 2026RaceManager/Analytics/MonetizeStats.cs,
// bỏ phần bù dữ liệu bản lưu cũ vì game chưa phát hành, chưa có save cũ.
public static class MonetizeStats
{
    public const string SEGMENT_PAYER = "payer";
    public const string SEGMENT_AD_WATCHER = "ad_watcher";
    public const string SEGMENT_NON_MONETIZER = "non_monetizer";

    private const double DAY_MS = 86400000d;
    // Racing: qua ngày đầu mà chưa xem quảng cáo thì gần như không xem nữa, nên cửa sổ 7 ngày là đủ phân nhóm.
    private const int AD_WATCHER_WINDOW_DAYS = 7;

    private static UserProfileData Profile => GameData.userData.profile;
    private static MonetizeStatsData Data => Profile.monetize;

    // Cùng nguồn giờ với firstTimeLogin (UserProfileData.ValidateData).
    private static double NowMs => GameUtils.GetTimeNow().ToMiliseconds();

    public static int PurchaseCount => Data.iapCount;
    public static double IapUsdTotal => Data.iapUsdTotal;
    public static int RewardedCount => Data.rewardedTotal;
    public static bool IsPayer => PurchaseCount > 0;

    public static void RecordPurchase(string productId)
    {
        Data.iapCount++;
        Data.iapUsdTotal += StaticShopData.GetPriceUsd(productId);
        Data.lastIapMs = NowMs;
        Profile.isDataChanged = true;
    }

    public static void RecordRewarded()
    {
        Data.rewardedTotal++;
        Data.lastRewardedMs = NowMs;
        Profile.isDataChanged = true;
    }

    // Ngày thứ mấy của tài khoản (1 = 24 giờ đầu), theo save chứ không theo máy.
    public static int AccountDay()
    {
        return Math.Max(1, (int)Math.Floor(SecondsSinceStart() * 1000d / DAY_MS) + 1);
    }

    public static double SecondsSinceStart()
    {
        double first = Profile.firstTimeLogin;
        return first > 0d ? Math.Round(Math.Max(0d, (NowMs - first) / 1000d)) : 0d;
    }

    // Payer thắng mọi nhóm; còn lại xét có xem hết rewarded trong 7 ngày gần nhất không.
    public static string Segment()
    {
        if (IsPayer)
        {
            return SEGMENT_PAYER;
        }

        bool watched = Data.lastRewardedMs > 0d && NowMs - Data.lastRewardedMs < AD_WATCHER_WINDOW_DAYS * DAY_MS;
        return watched ? SEGMENT_AD_WATCHER : SEGMENT_NON_MONETIZER;
    }

    public static string IapTier()
    {
        double usd = IapUsdTotal;

        if (usd <= 0d)
        {
            return "0";
        }

        return usd < 5d ? "lt5" : usd < 20d ? "5_20" : usd < 100d ? "20_100" : "100p";
    }

    public static string RewardedTier()
    {
        int n = RewardedCount;
        return n == 0 ? "0" : n < 5 ? "1_4" : n < 20 ? "5_19" : n < 50 ? "20_49" : "50p";
    }
}
