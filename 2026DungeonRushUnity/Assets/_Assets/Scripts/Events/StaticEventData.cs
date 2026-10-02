using System.Collections.Generic;

// 1 event: mốc mở khoá + số vé mỗi ngày.
public class EventConfig
{
    public EventModeType type;
    public string displayName;
    public int unlockPlayerLevel;
    public int dailyFreeTickets;      // vé miễn phí, sang ngày mới SET lại về số này
    public int maxAdTicketsPerDay;    // lượt xem ads lấy vé mỗi ngày
}

// Config tĩnh tab Events — REVERSE từ game gốc v41. Nhúng thẳng giá trị như StaticDungeonData.
// Nguồn: EventsTabPage.cctor (PvPUnlockPlayerLevel=15, CoopUnlockPlayerLevel=70, BossRushUnlockPlayerLevel=15),
// DecodedData/tables/PvPConfig.json (TicketConfig), BossRushConfig.json (MaxFreeEntries/MaxAdEntries).
// Gốc vé do server cấp (PvPTicketsDTO/BossRushTicketsDTO: freeRemaining/adRemaining/dayKey) → ở đây lưu local.
public class StaticEventData
{
    public readonly List<EventConfig> events = new List<EventConfig>
    {
        new EventConfig { type = EventModeType.PvP, displayName = "PvP", unlockPlayerLevel = 15, dailyFreeTickets = 5, maxAdTicketsPerDay = 4 },
        new EventConfig { type = EventModeType.BossRush, displayName = "Boss Rush", unlockPlayerLevel = 15, dailyFreeTickets = 3, maxAdTicketsPerDay = 3 },
    };

    public EventConfig GetData(EventModeType type)
    {
        for (int i = 0; i < events.Count; i++)
        {
            if (events[i].type == type)
                return events[i];
        }

        DebugCustom.Log("[StaticEventData] Not found=" + type);
        return null;
    }
}
