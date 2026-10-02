using System;

// Logic tab Events: mở khoá theo playerLevel, reset vé theo ngày. Stateless — thao tác trên
// GameData.staticData.events + GameData.userData.events (pattern DungeonService).
public static class EventService
{
    private static StaticEventData Static => GameData.staticData.events;
    private static UserEventData User => GameData.userData.events;

    // Sang NGÀY UTC mới → freeTickets = dailyFreeTickets, adClaimedToday = 0 cho mọi event.
    // Lần đầu (lastResetTime = -1) cũng reset. Trả true nếu vừa reset.
    public static bool CheckDailyReset()
    {
        DateTime now = GetUtcNow();
        if (User.lastResetTime >= 0 && now.Date <= DateTimeOffset.FromUnixTimeSeconds(User.lastResetTime).UtcDateTime.Date)
            return false;

        foreach (EventModeType type in Enum.GetValues(typeof(EventModeType)))
        {
            EventTickets tickets = User.Get(type);
            tickets.freeTickets = Static.GetData(type).dailyFreeTickets;
            tickets.adClaimedToday = 0;
        }

        User.lastResetTime = new DateTimeOffset(now).ToUnixTimeSeconds();
        User.isDataChanged = true;
        GameData.Save();
        return true;
    }

    // Thời gian tới lần reset kế (0h UTC hôm sau) — "Replenishing in" ở tab Events.
    public static TimeSpan GetTimeToReset()
    {
        DateTime now = GetUtcNow();
        return now.Date.AddDays(1) - now;
    }

    private static DateTime GetUtcNow()
    {
        return GameUtils.GetTimeNow().ToUniversalTime();
    }

    public static bool IsUnlocked(EventModeType type)
    {
        EventConfig data = Static.GetData(type);
        return data != null && GameData.userData.player.playerLevel >= data.unlockPlayerLevel;
    }

    public static EventTickets GetTickets(EventModeType type)
    {
        CheckDailyReset();
        return User.Get(type);
    }
}
