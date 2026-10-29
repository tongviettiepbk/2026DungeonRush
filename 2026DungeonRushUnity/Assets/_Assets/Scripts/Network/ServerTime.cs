using System;
using System.Collections;
using UnityEngine;

// Giờ server cho các mốc reset theo ngày (key Dungeon, vé Events): lấy 1 lần lúc loading qua Cloud Function
// getservertime, sau đó cộng thời gian trôi bằng realtimeSinceStartup (không phụ thuộc giờ máy) → chỉnh giờ máy
// không hồi key/vé sớm được. Chưa lấy được (offline) thì tạm dùng giờ máy.
public static class ServerTime
{
    public const string FN_GET_SERVER_TIME = "getservertime";

    private static bool isSynced;
    private static long serverUtcMs;        // giờ server lúc nhận phản hồi
    private static double realtimeAnchor;   // realtimeSinceStartup lúc nhận phản hồi

    public static bool IsSynced => isSynced;

    public static DateTime UtcNow
    {
        get
        {
            if (isSynced == false)
                return DateTime.UtcNow;

            double elapsedMs = (Time.realtimeSinceStartupAsDouble - realtimeAnchor) * 1000d;
            return DateTimeOffset.FromUnixTimeMilliseconds(serverUtcMs + (long)elapsedMs).UtcDateTime;
        }
    }

    // Gọi lúc loading: chờ tối đa timeout giây, quá thì thôi (phản hồi tới muộn vẫn được nhận).
    public static IEnumerator SyncRoutine(float timeout)
    {
        bool isDone = false;
        FirebaseManager.Instance.Call<ServerTimeResponseDTO>(FN_GET_SERVER_TIME, null, (ok, res, raw) =>
        {
            isDone = true;
            if (ok == false || res == null || res.serverTimeMs <= 0)
            {
                DebugCustom.LogWarning("[ServerTime] Sync failed, dùng giờ máy: " + raw);
                return;
            }

            serverUtcMs = res.serverTimeMs;
            realtimeAnchor = Time.realtimeSinceStartupAsDouble;
            isSynced = true;
        });

        float deadline = Time.unscaledTime + timeout;
        while (isDone == false && Time.unscaledTime < deadline)
        {
            yield return null;
        }
    }
}

public class ServerTimeResponseDTO
{
    public bool success;
    public string message;
    public long serverTimeMs;
}
