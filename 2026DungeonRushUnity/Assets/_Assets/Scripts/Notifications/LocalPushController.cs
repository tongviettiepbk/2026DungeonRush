using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#elif UNITY_IOS && !UNITY_EDITOR
using Unity.Notifications.iOS;
#endif

// Thông báo đẩy cục bộ. LỆCH GỐC có chủ ý: gốc không có, user duyệt 2026-11-05 vì game đang thiếu lý do quay lại.
// Xuống nền thì đặt lịch theo trạng thái lúc đó, quay lại thì huỷ hết. Tối đa 2 thông báo mỗi ngày, không kêu ban đêm.
// Xem GDD/analytics/GROWTH_PLAN.md mục 5.1-B.
public class LocalPushController : Singleton<LocalPushController>
{
    private const string CHANNEL_ID = "game_default";
    private const string ID_OFFLINE_FULL = "offline_full";
    private const string ID_DAILY = "daily";
    private const string ID_COMEBACK = "comeback";

    private const string PREF_ASKED = "push_permission_asked";
    private const int ASK_AT_STAGE_LEVEL = 10;

    private const int QUIET_START_HOUR = 22;
    private const int QUIET_END_HOUR = 9;
    private const int DAILY_HOUR = 19;
    private const int COMEBACK_DAYS = 3;
    private const double MIN_HOURS_AHEAD = 8d;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        InitChannel();
        CancelAll();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            Schedule();
        }
        else
        {
            CancelAll();
        }
    }

    private void OnApplicationQuit()
    {
        Schedule();
    }

    // Xin quyền sau khi qua màn 10: đã đủ gắn bó để đồng ý, mà vẫn kịp nhắc quay lại ngày 1.
    public void TryRequestPermission(int stageLevel)
    {
        if (stageLevel < ASK_AT_STAGE_LEVEL || PlayerPrefs.GetInt(PREF_ASKED, 0) == 1)
        {
            return;
        }

        PlayerPrefs.SetInt(PREF_ASKED, 1);
        StartCoroutine(RequestPermissionRoutine());
    }

    private void Schedule()
    {
        if (GameData.userData == null)
        {
            return;
        }

        CancelAll();

        DateTime now = DateTime.Now;
        double offlineSeconds = OfflineEarningService.GetMaxSeconds();
        Send(ID_OFFLINE_FULL, "Push.OfflineFull", OutOfQuietHours(now.AddSeconds(offlineSeconds)));
        Send(ID_DAILY, "Push.Daily", NextDaily(now));
        Send(ID_COMEBACK, "Push.Comeback", now.Date.AddDays(COMEBACK_DAYS).AddHours(DAILY_HOUR));
    }

    private static DateTime NextDaily(DateTime now)
    {
        DateTime time = now.Date.AddHours(DAILY_HOUR);
        return (time - now).TotalHours >= MIN_HOURS_AHEAD ? time : time.AddDays(1);
    }

    private static DateTime OutOfQuietHours(DateTime time)
    {
        if (time.Hour >= QUIET_START_HOUR)
        {
            return time.Date.AddDays(1).AddHours(QUIET_END_HOUR);
        }

        return time.Hour < QUIET_END_HOUR ? time.Date.AddHours(QUIET_END_HOUR) : time;
    }

    private static void Send(string id, string key, DateTime fireTime)
    {
        string title = Loc.L(key + ".Title");
        string body = Loc.L(key + ".Body");
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationCenter.SendNotification(new AndroidNotification(title, body, fireTime) { IntentData = id }, CHANNEL_ID);
#elif UNITY_IOS && !UNITY_EDITOR
        iOSNotificationCenter.ScheduleNotification(new iOSNotification
        {
            Identifier = id,
            Title = title,
            Body = body,
            Data = id,
            ShowInForeground = false,
            Trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = fireTime - DateTime.Now, Repeats = false },
        });
#else
        DebugCustom.Log("[Push] " + id + " " + fireTime + " " + title + " | " + body);
#endif
    }

    private static void InitChannel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationCenter.Initialize();
        AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel(CHANNEL_ID, "Game", "Game reminders", Importance.Default));
#endif
    }

    private static void CancelAll()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS && !UNITY_EDITOR
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }

    // Id thông báo đã mở app lần này, chỉ trả 1 lần; rỗng = không mở từ thông báo.
    public static string ConsumeOpenedId()
    {
        string id = string.Empty;
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidNotificationIntentData intent = AndroidNotificationCenter.GetLastNotificationIntent();
        if (intent != null)
        {
            id = intent.Notification.IntentData;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        iOSNotification notification = iOSNotificationCenter.GetLastRespondedNotification();
        if (notification != null)
        {
            id = notification.Data;
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
        }
#endif
        if (id == lastOpenedId)
        {
            return string.Empty;
        }

        lastOpenedId = id;
        return id ?? string.Empty;
    }

    private static string lastOpenedId = string.Empty;

    private static IEnumerator RequestPermissionRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        PermissionRequest request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
        {
            yield return null;
        }
        Analytics.Log("push_permission", ("granted", request.Status == PermissionStatus.Allowed));
#elif UNITY_IOS && !UNITY_EDITOR
        using (AuthorizationRequest request = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Sound, false))
        {
            while (request.IsFinished == false)
            {
                yield return null;
            }
            Analytics.Log("push_permission", ("granted", request.Granted));
        }
#else
        yield break;
#endif
    }
}
