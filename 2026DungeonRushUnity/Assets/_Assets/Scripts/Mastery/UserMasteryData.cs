using System.Collections.Generic;

// Save hệ Mastery (G1): trạng thái mở khoá + cấp đã nâng từng nhánh.
// Theo pattern UserCampaignData/UserEquipmentData : BaseUserData (1 key PlayerPrefs riêng).
//
// Quy ước: key dict = (int)MasteryUpgradeType dạng string (serialize JSON ổn định).
//   - CÓ trong dict  => nhánh ĐÃ mở khoá; value = level đã nâng (0 = mở nhưng chưa nâng cấp nào).
//   - KHÔNG có        => chưa mở khoá.
public class UserMasteryData : BaseUserData
{
    public Dictionary<string, int> levels { get; set; } = new Dictionary<string, int>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_MASTERY;
    }

    private static string Key(MasteryUpgradeType type)
    {
        return ((int)type).ToString();
    }

    public bool IsUnlocked(MasteryUpgradeType type)
    {
        return levels.ContainsKey(Key(type));
    }

    // Cấp hiện tại của nhánh (0 nếu chưa mở khoá hoặc mở mà chưa nâng).
    public int GetLevel(MasteryUpgradeType type)
    {
        return levels.TryGetValue(Key(type), out int lv) ? lv : 0;
    }

    // Mở khoá nhánh (đặt level 0). Không kiểm tra phí ở đây — MasteryService lo việc trừ Ngọc.
    public void Unlock(MasteryUpgradeType type)
    {
        if (IsUnlocked(type) == false)
        {
            levels[Key(type)] = 0;
            isDataChanged = true;
        }
    }

    // Đặt cấp mới cho nhánh (sau khi đã trừ Ngọc ở service).
    public void SetLevel(MasteryUpgradeType type, int level)
    {
        levels[Key(type)] = level;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (levels == null)
        {
            levels = new Dictionary<string, int>();
            isDataChanged = true;
        }
    }
}
