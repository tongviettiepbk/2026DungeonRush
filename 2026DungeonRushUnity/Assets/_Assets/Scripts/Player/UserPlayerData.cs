// Hệ EXP/LEVEL người chơi (class User gốc: PlayerLevel / PlayerExperience).
// playerLevel = LEVEL hiển thị ở popup "Rarity Table" (cái vương miện Level N): nó vừa là mốc
// thanh exp, vừa là INDEX bảng xác suất rarity (row 0-based = playerLevel - 1) + mốc thưởng mỗi
// lần lên level. Thắng campaign → +exp (round(49 + level màn), xem DecodedData/EXP_MODEL.md) →
// đầy ngưỡng experience_required_per_level thì playerLevel++.
// (LOGIC cộng exp / lên level / thưởng làm ở Phase 2-3.)
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

    // Cộng exp (khi thắng màn) + đẩy playerLevel qua các ngưỡng experience_required_per_level.
    // Trả về SỐ LEVEL vừa lên (0 nếu không lên) — dùng cho việc bật LevelPopup + thưởng ở Phase 3.
    public int AddExperience(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        StaticExperienceData config = GameData.staticData.experience;
        playerExperience += amount;
        isDataChanged = true;

        int levelsGained = 0;
        // Dừng ở MaxLevel: hết bảng thì rarity không cải thiện thêm, giữ exp dư lại.
        while (playerLevel < config.MaxLevel && playerExperience >= config.GetXpRequired(playerLevel))
        {
            playerExperience -= config.GetXpRequired(playerLevel);
            playerLevel++;
            levelsGained++;
        }

        return levelsGained;
    }
}
