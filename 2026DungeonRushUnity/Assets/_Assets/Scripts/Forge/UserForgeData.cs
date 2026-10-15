using System.Collections.Generic;

// Save cài đặt Auto Forge (class User gốc: SelectedAutoForgeHammerCount, AutoForgeDisabledRarities,
// AutoForgeSubStatFilterEnabled, AutoForgeEnabledSubStats). Búa = ItemType.LOOT_TICKET (UserItemData).
// AutoSellLowerLevel / MaxAutoForgeHammerCount gốc là field chết (vòng forge không đọc) → không port.
// Xem DecodedData/FORGE_MODEL.md.
public class UserForgeData : BaseUserData
{
    // Số búa tiêu mỗi lượt auto (1..8, kẹp theo ForgeService.GetMaxHammerCount khi dùng).
    public int selectedAutoHammerCount { get; set; } = 1;

    // Rarity KHÔNG giữ → auto bán ngay (lưu int của enum Rarity như gốc).
    public List<int> disabledRarities { get; set; } = new List<int>();

    public bool subStatFilterEnabled { get; set; }

    // Substat muốn giữ khi bật lọc (int của SubStatType).
    public List<int> enabledSubStats { get; set; } = new List<int>();

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_FORGE;
    }

    public override void ValidateData()
    {
        if (selectedAutoHammerCount < 1)
        {
            selectedAutoHammerCount = 1;
            isDataChanged = true;
        }

        if (disabledRarities == null)
        {
            disabledRarities = new List<int>();
            isDataChanged = true;
        }

        if (enabledSubStats == null)
        {
            enabledSubStats = new List<int>();
            isDataChanged = true;
        }
    }

    // dqn gốc: rarity có được giữ không.
    public bool IsRarityKept(Rarity rarity)
    {
        return disabledRarities.Contains((int)rarity) == false;
    }

    public void SetRarityKept(Rarity rarity, bool kept)
    {
        int value = (int)rarity;
        if (kept)
            disabledRarities.Remove(value);
        else if (disabledRarities.Contains(value) == false)
            disabledRarities.Add(value);
        isDataChanged = true;
    }

    // dqu gốc.
    public bool IsSubStatEnabled(SubStatType type)
    {
        return enabledSubStats.Contains((int)type);
    }

    public void SetSubStatEnabled(SubStatType type, bool enabled)
    {
        int value = (int)type;
        if (enabled == false)
            enabledSubStats.Remove(value);
        else if (enabledSubStats.Contains(value) == false)
            enabledSubStats.Add(value);
        isDataChanged = true;
    }
}
