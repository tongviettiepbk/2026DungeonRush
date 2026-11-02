using System.Collections.Generic;

// Save chat phía client (UserData gốc: chuỗi BlockedChatUsers nối dấu phẩy + ChatBannedUntil).
// Danh sách chặn chỉ ở máy (gốc không gửi server); mốc cấm là bản sao server trả về khi gửi tin.
public class UserChatData : BaseUserData
{
    public List<string> blockedUserIds { get; set; } = new List<string>();
    public long bannedUntil { get; set; }
    public int banTier { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_CHAT;
    }

    public override void InitData()
    {
        base.InitData();
        blockedUserIds = new List<string>();
        bannedUntil = 0;
        banTier = 0;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (blockedUserIds == null)
        {
            blockedUserIds = new List<string>();
            isDataChanged = true;
        }
    }
}
