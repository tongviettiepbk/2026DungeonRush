using System;

// Lịch sự kiện Boss Rush — port 1:1 class `fn` của game gốc (mọi mốc theo UTC), giống server/functions/src/schedule.ts.
//   Mở: Thứ 3 00:00 → Chủ nhật 23:59:59. Thứ 2 nghỉ (chốt/nhận thưởng).
//   eventKey = "yyyy-MM-dd" của ngày bắt đầu (Thứ 3).
public static class BossRushSchedule
{
    public static DateTime UtcNow => GameUtils.GetTimeNow().ToUniversalTime();

    // ely: đang mở ⇔ không phải Thứ 2.
    public static bool IsActive(DateTime utc)
    {
        return utc.DayOfWeek != DayOfWeek.Monday;
    }

    // ema: bắt đầu đợt hiện tại (Thứ 2 → đợt kế).
    public static DateTime GetEventStart(DateTime utc)
    {
        if (!IsActive(utc))
        {
            return GetNextEventStart(utc);
        }
        int offset = ((int)utc.DayOfWeek + 5) % 7;
        return utc.Date.AddDays(-offset);
    }

    // emb: kết thúc = bắt đầu + 6 ngày - 1 giây (CN 23:59:59).
    public static DateTime GetEventEnd(DateTime utc)
    {
        return GetEventStart(utc).AddDays(6).AddSeconds(-1);
    }

    // emc: đợt kế.
    public static DateTime GetNextEventStart(DateTime utc)
    {
        if (utc.DayOfWeek == DayOfWeek.Monday)
        {
            return utc.Date.AddDays(1);
        }
        return GetEventStart(utc).AddDays(7);
    }

    // elz.
    public static string GetEventKey(DateTime utc)
    {
        return IsActive(utc) ? GetEventStart(utc).ToString("yyyy-MM-dd") : string.Empty;
    }

    // Đếm ngược kiểu "1d 5h" / "5h 30m" / "30m 23s".
    public static string FormatRemain(TimeSpan time)
    {
        if (time.TotalSeconds < 0) time = TimeSpan.Zero;
        if (time.Days > 0) return time.Days + "d " + time.Hours + "h";
        if (time.Hours > 0) return time.Hours + "h " + time.Minutes + "m";
        return time.Minutes + "m " + time.Seconds + "s";
    }
}
