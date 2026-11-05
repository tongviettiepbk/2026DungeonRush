using System;
using System.Collections.Generic;
using System.Globalization;

// Dấu "đã bắn" của mốc một lần, nằm trong save (UserProfileData.funnel).
public class FunnelStatsData
{
    public List<string> marks { get; set; } = new List<string>();
}

// Phễu UA: mỗi bước bắn 1 lần mỗi tài khoản, cùng tên lên Firebase và AppsFlyer để mạng quảng cáo tối ưu theo.
// Các mốc dưới đây là phỏng đoán theo mốc mở tính năng; có số liệu thật thì chọn lại theo luật ở TRACKING_PLAN.md mục 5.
public static class UaFunnel
{
    public const string LOOT_1 = "funnel_loot_1";
    public const string STAGE_1 = "funnel_stage_1";
    public const string STAGE_10 = "funnel_stage_10";
    public const string STAGE_30 = "funnel_stage_30";
    public const string LEVEL_5 = "funnel_level_5";
    public const string LEVEL_15 = "funnel_level_15";
    public const string DUNGEON_1 = "funnel_dungeon_1";
    public const string AD_1 = "funnel_ad_1";
    public const string RETURN_D2 = "funnel_return_d2";
    public const string RETURN_D7 = "funnel_return_d7";
    public const string FIRST_PURCHASE = "first_purchase";

    private static UserProfileData Profile => GameData.userData.profile;

    public static void OnSessionBegin()
    {
        int day = MonetizeStats.AccountDay();

        if (day >= 2)
        {
            Step(RETURN_D2);
        }

        if (day >= 7)
        {
            Step(RETURN_D7);
        }
    }

    public static void OnStageWon(int level)
    {
        Step(STAGE_1);

        if (level >= 10)
        {
            Step(STAGE_10);
        }

        if (level >= 30)
        {
            Step(STAGE_30);
        }
    }

    public static void OnLevelUp(int level)
    {
        if (level >= 5)
        {
            Step(LEVEL_5);
        }

        if (level >= 15)
        {
            Step(LEVEL_15);
        }
    }

    public static void OnAdFinished()
    {
        Step(AD_1);
    }

    // Gọi sau MonetizeStats.RecordPurchase; chỉ đơn đầu của tài khoản mới bắn.
    public static void OnPurchase(string productId, double priceUsd)
    {
        if (MonetizeStats.PurchaseCount == 1)
        {
            Step(FIRST_PURCHASE, ("item_id", productId), ("price_usd", priceUsd));
        }
    }

    // Đánh dấu một mốc; false nếu đã có.
    public static bool TryMark(string mark)
    {
        if (Profile.funnel.marks.Contains(mark))
        {
            return false;
        }

        Profile.funnel.marks.Add(mark);
        Profile.isDataChanged = true;
        return true;
    }

    public static void Step(string name, params (string key, object value)[] extra)
    {
        if (TryMark(name) == false)
        {
            return;
        }

        (string key, object value)[] kv = new (string key, object value)[extra.Length + 1];
        kv[0] = ("sec_since_start", MonetizeStats.SecondsSinceStart());
        Array.Copy(extra, 0, kv, 1, extra.Length);
        Analytics.Log(name, kv);

        Dictionary<string, string> values = new Dictionary<string, string>();

        for (int i = 0; i < kv.Length; i++)
        {
            values[kv[i].key] = Convert.ToString(kv[i].value, CultureInfo.InvariantCulture);
        }

        AppsflyerHelper.SendEvent(name, values);
    }
}
