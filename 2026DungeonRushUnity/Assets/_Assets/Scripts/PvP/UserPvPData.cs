// Save PvP phía client. Server là nguồn sự thật — đây là cache để vẽ UI + giữ báo cáo trận chưa gửi được.
public class UserPvPData : BaseUserData
{
    public int trophy { get; set; }
    public int leagueIndex { get; set; } = 1;
    public string seasonKey { get; set; } = string.Empty;
    public PvPTicketsDTO tickets { get; set; } = new PvPTicketsDTO();
    public string lastSnapshotHash { get; set; } = string.Empty;
    public long lastSnapshotUploadTime { get; set; } = -1;
    public PvPCachedRosterModel cachedRoster { get; set; }
    public PendingPvPReport pendingReport { get; set; }
    public PendingPvPRewardSettlement pendingSettlement { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_PVP;
    }

    public override void InitData()
    {
        base.InitData();
        trophy = 0;
        leagueIndex = 1;
        seasonKey = string.Empty;
        tickets = NewTickets();
        lastSnapshotHash = string.Empty;
        lastSnapshotUploadTime = -1;
        cachedRoster = null;
        pendingReport = null;
        pendingSettlement = null;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        // Save hệ vé cũ (free + ads) không có dailyTickets.
        if (tickets == null || tickets.dailyTickets <= 0)
        {
            tickets = NewTickets();
            isDataChanged = true;
        }
        if (trophy < 0)
        {
            trophy = 0;
            isDataChanged = true;
        }
        if (leagueIndex < 1)
        {
            leagueIndex = 1;
            isDataChanged = true;
        }
        lastSnapshotHash = lastSnapshotHash ?? string.Empty;
        seasonKey = seasonKey ?? string.Empty;
    }

    public void SetTickets(PvPTicketsDTO dto)
    {
        if (dto == null)
        {
            return;
        }
        tickets = dto;
        isDataChanged = true;
    }

    public int TotalTickets => tickets != null ? tickets.remaining : 0;

    private static PvPTicketsDTO NewTickets()
    {
        return new PvPTicketsDTO { remaining = StaticPvPData.DAILY_TICKETS, dailyTickets = StaticPvPData.DAILY_TICKETS };
    }
}
