using System;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

// Firebase Analytics + Crashlytics qua SDK. Auth và Cloud Functions vẫn đi REST ở FirebaseManager, không liên quan lớp này.
// Không chạy trong Editor. Event bắn trước khi SDK sẵn sàng thì bỏ: tạo Parameter sớm hơn CheckAndFixDependencies
// là ném từ static constructor của SDK, cháy một lần thì cả phiên không event nào lên được nữa.
public static class FirebaseServices
{
    private static bool isReady;
    private static string pendingUserId;

    public static void Init()
    {
        if (Application.isEditor)
        {
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled || task.Result != DependencyStatus.Available)
            {
                DebugCustom.LogError("[Firebase] Dependencies not available: " + (task.IsFaulted ? task.Exception.ToString() : task.Result.ToString()));
                return;
            }

            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            isReady = true;

            if (string.IsNullOrEmpty(pendingUserId) == false)
            {
                ApplyUserId(pendingUserId);
                pendingUserId = null;
            }
        });
    }

    // Gắn uid tài khoản vào event + báo crash; lúc đăng nhập xong SDK có thể chưa sẵn sàng nên giữ lại chờ.
    public static void SetUserId(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return;
        }

        if (isReady)
        {
            ApplyUserId(uid);
        }
        else
        {
            pendingUserId = uid;
        }
    }

    private static void ApplyUserId(string uid)
    {
        FirebaseAnalytics.SetUserId(uid);
        Crashlytics.SetUserId(uid);
    }

    public static void LogEvent(string eventName)
    {
        if (isReady == false)
        {
            return;
        }

        Safe(() => FirebaseAnalytics.LogEvent(eventName));
    }

    public static void LogEvent(string eventName, string paramName, string paramValue)
    {
        if (isReady == false)
        {
            return;
        }

        Safe(() => FirebaseAnalytics.LogEvent(eventName, paramName, paramValue));
    }

    public static void LogAdRevenue(string source, string adUnit, string format, double value)
    {
        if (isReady == false)
        {
            return;
        }

        Safe(() =>
        {
            Parameter[] parameters =
            {
                new Parameter("ad_platform", "Applovin"),
                new Parameter("ad_source", source),
                new Parameter("ad_unit_name", adUnit),
                new Parameter("ad_format", format),
                new Parameter("currency", "USD"),
                new Parameter("value", value),
            };
            FirebaseAnalytics.LogEvent(Keys.realtimeAdRevenueGoogle, parameters);
            FirebaseAnalytics.LogEvent(Keys.realtimeAdRevenue, parameters);
        });
    }

    public static void LogPurchase(string productId, double value, string currency, string transactionId)
    {
        if (isReady == false)
        {
            return;
        }

        Safe(() =>
        {
            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase,
                new Parameter(FirebaseAnalytics.ParameterItemID, productId),
                new Parameter(FirebaseAnalytics.ParameterValue, value),
                new Parameter(FirebaseAnalytics.ParameterCurrency, currency ?? string.Empty),
                new Parameter(FirebaseAnalytics.ParameterTransactionID, transactionId ?? string.Empty));
        });
    }

    // Một event hỏng không được làm chết luồng game (nhất là luồng trao hàng IAP).
    private static void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[Firebase] " + e);
        }
    }
}
