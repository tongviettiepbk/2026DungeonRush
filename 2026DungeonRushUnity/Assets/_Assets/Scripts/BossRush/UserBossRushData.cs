// Save Boss Rush phía client (UserData gốc: BossRushTier / BossRushCurrentPoolId / BossRushLastJoinEventKey /
// BossRushUnclaimedPoolId / BossRushLastItemUpdateTime / BossRush*Tickets* / BossRushPendingReport).
// Server là nguồn sự thật — đây chỉ là bản cache để vẽ UI khi chưa gọi mạng + giữ báo cáo damage chưa gửi được.
public class UserBossRushData : BaseUserData
{
    public int tier { get; set; } = 1;
    public string currentPoolId { get; set; } = string.Empty;
    public string lastJoinEventKey { get; set; } = string.Empty;
    public string unclaimedPoolId { get; set; } = string.Empty;
    public long lastItemUpdateTime { get; set; } = -1;          // unix giây lần gửi snapshot đồ gần nhất
    public BossRushTicketsDTO tickets { get; set; } = new BossRushTicketsDTO();
    public BossRushPendingReport pendingReport { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_BOSS_RUSH;
    }

    public override void InitData()
    {
        base.InitData();
        tier = 1;
        currentPoolId = string.Empty;
        lastJoinEventKey = string.Empty;
        unclaimedPoolId = string.Empty;
        lastItemUpdateTime = -1;
        tickets = new BossRushTicketsDTO();
        pendingReport = null;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (tickets == null)
        {
            tickets = new BossRushTicketsDTO();
            isDataChanged = true;
        }
        if (tier < 1)
        {
            tier = 1;
            isDataChanged = true;
        }
        currentPoolId = currentPoolId ?? string.Empty;
        lastJoinEventKey = lastJoinEventKey ?? string.Empty;
        unclaimedPoolId = unclaimedPoolId ?? string.Empty;
    }

    // Đồng bộ vé từ server (UserController.dzh gốc).
    public void SetTickets(BossRushTicketsDTO dto)
    {
        if (dto == null)
        {
            return;
        }
        tickets = dto;
        isDataChanged = true;
    }
}
