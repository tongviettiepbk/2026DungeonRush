using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Vé của 1 event (PvPTicketsDTO/BossRushTicketsDTO gốc: freeRemaining/adRemaining/adClaimedToday).
public class EventTickets
{
    public int freeTickets { get; set; }      // vé miễn phí, reset mỗi ngày
    public int adTickets { get; set; }        // vé từ ads, KHÔNG reset
    public int adClaimedToday { get; set; }   // lượt ads đã xem hôm nay

    [JsonIgnore] public int TotalTickets => freeTickets + adTickets;
}

// Save tab Events (module BaseUserData → 1 key PlayerPrefs JSON, xem UserData).
// Key dict là (int)EventModeType dạng string (giống UserDungeonData).
public class UserEventData : BaseUserData
{
    public Dictionary<string, EventTickets> events { get; set; } = new Dictionary<string, EventTickets>();
    // Unix giây (UTC) lần reset vé gần nhất; -1 = chưa reset lần nào → lần đầu sẽ được cấp đủ vé.
    public long lastResetTime { get; set; } = -1;

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_EVENT;
    }

    public override void InitData()
    {
        base.InitData();
        events = new Dictionary<string, EventTickets>();
        lastResetTime = -1;
        ValidateData();
        isDataChanged = true;
    }

    // Đảm bảo đủ entry cho mọi EventModeType.
    public override void ValidateData()
    {
        if (events == null)
        {
            events = new Dictionary<string, EventTickets>();
            isDataChanged = true;
        }

        foreach (EventModeType type in Enum.GetValues(typeof(EventModeType)))
        {
            string id = ((int)type).ToString();
            if (!events.ContainsKey(id) || events[id] == null)
            {
                events[id] = new EventTickets();
                isDataChanged = true;
            }
        }
    }

    public EventTickets Get(EventModeType type)
    {
        return events[((int)type).ToString()];
    }
}
