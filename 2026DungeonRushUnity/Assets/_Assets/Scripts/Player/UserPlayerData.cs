// Hệ EXP/LEVEL người chơi (class User gốc: PlayerLevel / PlayerExperience).
// playerLevel = LEVEL hiển thị ở popup "Rarity Table" (cái vương miện Level N): nó vừa là mốc
// thanh exp, vừa là INDEX bảng xác suất rarity (row 0-based = playerLevel - 1) + mốc thưởng mỗi
// lần lên level. Cộng exp / lên level / thưởng: PlayerExpService (ExperienceController gốc) — exp dừng ở
// ngưỡng experience_required_per_level, bấm Complete trong LevelPopup mới playerLevel++.
public class UserPlayerData : BaseUserData
{
    // 1-indexed như gốc: level 1 = mới bắt đầu (User.PlayerLevel khởi điểm = 1).
    public int playerLevel { get; set; }
    // Exp tích trong level hiện tại (chưa trừ ngưỡng để lên level tiếp).
    public int playerExperience { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_PLAYER;
    }

    public override void InitData()
    {
        base.InitData();
        playerLevel = 1;
        playerExperience = 0;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (playerLevel < 1)
        {
            playerLevel = 1;
            isDataChanged = true;
        }

        if (playerExperience < 0)
        {
            playerExperience = 0;
            isDataChanged = true;
        }
    }
}
