using System;
using System.Collections;
using UnityEngine;

// Giờ server cho các mốc reset theo ngày (key Dungeon, vé Events, lịch Boss Rush): lấy qua Cloud Function
// getservertime, sau đó cộng thời gian trôi bằng realtimeSinceStartup (không phụ thuộc giờ máy) → chỉnh giờ máy
// không hồi key/vé sớm được. Bù nửa thời gian khứ hồi mạng. Đồng bộ lại mỗi lần quay lại app; chưa lấy được
// (offline) thì tạm dùng giờ máy và tự thử lại sau RETRY_INTERVAL giây.
public static class ServerTime
{
    public const string FN_GET_SERVER_TIME = "getservertime";

    private const float RETRY_INTERVAL = 15f;  // chưa sync: tối đa 1 lần gọi / 15s
    private const float RESYNC_MIN_GAP = 10f;  // quay lại app: không gọi dồn

    private static bool isSynced;
    private static bool isRequesting;
    private static long serverUtcMs;           // giờ server tại realtimeAnchor
    private static double realtimeAnchor;      // realtimeSinceStartup lúc nhận phản hồi
    private static double lastRequestTime = -1000d;

    public static bool IsSynced => isSynced;

    public static DateTime UtcNow
    {
        get
        {
            if (isSynced == false)
            {
                TryAutoSync(RETRY_INTERVAL);
                return DateTime.UtcNow;
            }

            double elapsedMs = (Time.realtimeSinceStartupAsDouble - realtimeAnchor) * 1000d;
            return DateTimeOffset.FromUnixTimeMilliseconds(serverUtcMs + (long)elapsedMs).UtcDateTime;
        }
    }

    // Gọi lúc loading: chờ tối đa timeout giây, quá thì thôi (phản hồi tới muộn vẫn được nhận).
    public static IEnumerator SyncRoutine(float timeout)
    {
        RequestSync();

        float deadline = Time.unscaledTime + timeout;
        while (isRequesting && Time.unscaledTime < deadline)
        {
            yield return null;
        }
    }

    // Quay lại app (máy có thể đã ngủ lâu, realtimeSinceStartup có máy không đếm lúc ngủ) → lấy lại giờ server.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterFocusHook()
    {
        Application.focusChanged -= OnFocusChanged;
        Application.focusChanged += OnFocusChanged;
    }

    private static void OnFocusChanged(bool hasFocus)
    {
        if (hasFocus)
            TryAutoSync(RESYNC_MIN_GAP);
    }

    // Chỉ tự gọi khi đã đăng nhập (getservertime đi qua Call cần token) — trước đó Login.SyncRoutine lo.
    private static void TryAutoSync(float minGap)
    {
        if (Application.isPlaying == false || isRequesting)
            return;
        if (Time.realtimeSinceStartupAsDouble - lastRequestTime < minGap)
            return;
        if (FirebaseManager.Exists() == false || FirebaseManager.Instance.IsSignedIn == false)
            return;

        RequestSync();
    }

    private static void RequestSync()
    {
        if (isRequesting)
            return;

        isRequesting = true;
        double sentAt = Time.realtimeSinceStartupAsDouble;
        lastRequestTime = sentAt;

        FirebaseManager.Instance.Call<ServerTimeResponseDTO>(FN_GET_SERVER_TIME, null, (ok, res, raw) =>
        {
            isRequesting = false;
            if (ok == false || res == null || res.serverTimeMs <= 0)
            {
                DebugCustom.LogWarning("[ServerTime] Sync failed" + (isSynced ? ", giữ giờ server cũ: " : ", dùng giờ máy: ") + raw);
                return;
            }

            // Server ghi giờ khoảng giữa lượt đi-về → cộng nửa thời gian khứ hồi.
            double now = Time.realtimeSinceStartupAsDouble;
            double halfRttMs = (now - sentAt) * 500d;
            serverUtcMs = res.serverTimeMs + (long)halfRttMs;
            realtimeAnchor = now;
            isSynced = true;
            DebugCustom.Log("[ServerTime] Synced, lệch giờ máy " + (DateTime.UtcNow - UtcNow).TotalSeconds.ToString("0.0") + "s");
        });
    }
}

public class ServerTimeResponseDTO
{
    public bool success;
    public string message;
    public long serverTimeMs;
}
