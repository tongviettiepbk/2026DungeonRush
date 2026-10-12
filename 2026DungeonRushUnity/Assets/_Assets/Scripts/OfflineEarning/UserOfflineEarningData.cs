// Save của thưởng offline. Field gốc trong User: OfflineDurationCollectTime.
public class UserOfflineEarningData : BaseUserData
{
    // Mốc bắt đầu tính thưởng offline (unix giây UTC). 0 = chưa có mốc (lần đầu vào game).
    // Đang chơi thì lobby đẩy mốc về "bây giờ" mỗi 10s → lúc mở app lại, (now - collectTime) = thời gian vắng mặt.
    public long collectTime { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_OFFLINE_EARNING;
    }

    public override void ValidateData()
    {
        if (collectTime < 0)
        {
            collectTime = 0;
            isDataChanged = true;
        }
    }
}
