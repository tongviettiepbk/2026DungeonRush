using System.Collections.Generic;

// Save của Store: pity rương, Daily Deal, boost exp, các gói mua 1 lần.
// Field gốc trong User: ChestPityCounters, DailyDealSeed/CurrentCombos/PurchasedCombos, PendingDealCombo,
// IsNoAdsBought, IsLifetimeBoostBought, X2/X5ExpBoostEndTime, IsCapeFeatureEarlyUnlocked.
public class UserShopData : BaseUserData
{
    // key = (int)Rarity dạng string, value = số lần mở liên tiếp chưa ra rarity đó.
    public Dictionary<string, int> chestPityCounters { get; set; } = new Dictionary<string, int>();

    public int dailyDealSeed { get; set; }                                  // yyyyMMdd (UTC)
    public List<int> dailyDealCurrentCombos { get; set; } = new List<int>();   // combo = size * 10 + type
    public List<int> dailyDealPurchasedCombos { get; set; } = new List<int>();
    public int pendingDealCombo { get; set; } = -1;

    public bool isNoAdsBought { get; set; }
    public bool isLifetimeBoostBought { get; set; }
    public bool isCapeFeatureEarlyUnlocked { get; set; }    // mua gói rương khi chưa tới level mở Cape
    public long x2ExpBoostEndTime { get; set; }             // unix giây
    public long x5ExpBoostEndTime { get; set; }

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_SHOP;
    }

    public override void ValidateData()
    {
        if (chestPityCounters == null) chestPityCounters = new Dictionary<string, int>();
        if (dailyDealCurrentCombos == null) dailyDealCurrentCombos = new List<int>();
        if (dailyDealPurchasedCombos == null) dailyDealPurchasedCombos = new List<int>();
    }

    // eer gốc.
    public int GetPity(Rarity rarity)
    {
        return chestPityCounters.TryGetValue(((int)rarity).ToString(), out int count) ? count : 0;
    }

    // ees gốc.
    public void SetPity(Rarity rarity, int count)
    {
        chestPityCounters[((int)rarity).ToString()] = count;
        isDataChanged = true;
    }
}
