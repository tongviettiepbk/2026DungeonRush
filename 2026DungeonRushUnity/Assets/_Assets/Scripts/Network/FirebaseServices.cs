using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

// Firebase Analytics + Crashlytics qua SDK. Auth và Cloud Functions vẫn đi REST ở FirebaseManager, không liên quan lớp này.
// Không chạy trong Editor. Mọi lời gọi tới SDK đợi trong hàng đợi tới khi SDK sẵn sàng: tạo Parameter sớm hơn
// CheckAndFixDependencies là ném từ static constructor của SDK, cháy một lần thì cả phiên không event nào lên được nữa.
// Event gameplay đi qua lớp Analytics; ở đây chỉ còn event của SDK quảng cáo và phần chạm SDK.
public static class FirebaseServices
{
    // Giới hạn GA4: quá là mất cả event hoặc tham số, không báo lỗi. https://support.google.com/analytics/answer/9267744
    private const int MAX_EVENT_NAME = 40;
    private const int MAX_PARAMS = 25;
    private const int MAX_PARAM_NAME = 40;
    private const int MAX_PARAM_VALUE = 100;
    private const int MAX_USER_PROPERTY_NAME = 24;
    private const int MAX_USER_PROPERTY_VALUE = 36;
    private const int QUEUE_MAX = 200;

    private static bool isReady;
    private static string pendingUserId;
    private static readonly List<Action> pendingCalls = new List<Action>();

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
            // Bản Dev mua/ads giả + cheat: event gửi lên là lẫn số liệu thật, nên tắt Analytics (Crashlytics vẫn chạy).
            // Cờ này SDK lưu qua các lần mở app → bản Product phải bật lại, phòng máy cài đè từ bản Dev.
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(DevBuild.IsEnabled == false);
            isReady = true;

            if (string.IsNullOrEmpty(pendingUserId) == false)
            {
                ApplyUserId(pendingUserId);
                pendingUserId = null;
            }

            for (int i = 0; i < pendingCalls.Count; i++)
            {
                Safe(pendingCalls[i]);
            }

            pendingCalls.Clear();
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
        Run(() => FirebaseAnalytics.LogEvent(eventName));
    }

    // Tham số của Analytics.Log. Dựng Parameter ngay trước lúc gửi, không dựng lúc xếp hàng (xem đầu file).
    public static void LogEvent(string eventName, List<(string key, object value)> values)
    {
        Run(() => FirebaseAnalytics.LogEvent(Clip(eventName, MAX_EVENT_NAME), ToParameters(values)));
    }

    public static void SetUserProperty(string name, string value)
    {
        Run(() => FirebaseAnalytics.SetUserProperty(Clip(name, MAX_USER_PROPERTY_NAME), Clip(value ?? string.Empty, MAX_USER_PROPERTY_VALUE)));
    }

    private static Parameter[] ToParameters(List<(string key, object value)> values)
    {
        int count = Math.Min(values.Count, MAX_PARAMS);
        Parameter[] parameters = new Parameter[count];

        for (int i = 0; i < count; i++)
        {
            parameters[i] = ToParameter(Clip(values[i].key, MAX_PARAM_NAME), values[i].value);
        }

        return parameters;
    }

    private static Parameter ToParameter(string key, object value)
    {
        switch (value)
        {
            case bool b: return new Parameter(key, b ? 1L : 0L);
            case int i: return new Parameter(key, (long)i);
            case long l: return new Parameter(key, l);
            case float f: return new Parameter(key, (double)f);
            case double d: return new Parameter(key, double.IsNaN(d) || double.IsInfinity(d) ? 0d : d);
            case Enum e: return new Parameter(key, Clip(e.ToString().ToLowerInvariant(), MAX_PARAM_VALUE));
            default: return new Parameter(key, Clip(value.ToString(), MAX_PARAM_VALUE));
        }
    }

    private static string Clip(string text, int max)
    {
        return text.Length <= max ? text : text.Substring(0, max);
    }

    public static void LogAdRevenue(string source, string adUnit, string format, double value)
    {
        Run(() =>
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

    private static void Run(Action action)
    {
        if (Application.isEditor)
        {
            return;
        }

        if (isReady)
        {
            Safe(action);
        }
        else if (pendingCalls.Count < QUEUE_MAX)
        {
            pendingCalls.Add(action);
        }
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
