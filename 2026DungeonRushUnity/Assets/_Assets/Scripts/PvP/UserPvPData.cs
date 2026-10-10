// Save PvP phía client (UserData gốc: PvPTrophy / PvPDailyTicketsRemaining / PvPAdTicketsRemaining /
// PvPAdTicketsClaimedToday / PvPLastSnapshotHash / PvPLastSnapshotUploadTime / PvPCachedRoster / PvPPendingReport /
// PvPPendingRewardSettlement). Server là nguồn sự thật — đây là cache để vẽ UI + giữ báo cáo/thưởng chưa xử lý xong.
public class UserPvPData : BaseUserData
{
    public int trophy { get; set; } = StaticPvPData.START_TROPHY;
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
        trophy = StaticPvPData.START_TROPHY;
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
        if (tickets == null)
        {
            tickets = NewTickets();
            isDataChanged = true;
        }
        if (trophy < StaticPvPData.MINIMUM_TROPHY)
        {
            trophy = StaticPvPData.MINIMUM_TROPHY;
            isDataChanged = true;
        }
        lastSnapshotHash = lastSnapshotHash ?? string.Empty;
    }

    // Đồng bộ vé từ server (UserController.eah gốc).
    public void SetTickets(PvPTicketsDTO dto)
    {
        if (dto == null)
        {
            return;
        }
        tickets = dto;
        isDataChanged = true;
    }

    public int TotalTickets => tickets != null ? tickets.freeRemaining + tickets.adRemaining : 0;

    private static PvPTicketsDTO NewTickets()
    {
        return new PvPTicketsDTO
        {
            freeRemaining = StaticPvPData.DAILY_FREE_TICKETS,
            maxAdPerDay = StaticPvPData.MAX_AD_TICKETS_PER_DAY,
            dailyFreeTickets = StaticPvPData.DAILY_FREE_TICKETS,
        };
    }
}
