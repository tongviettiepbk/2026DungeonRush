using System;
using System.Collections.Generic;

public enum DealSize
{
    Tiny = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
}

public enum DealType
{
    DungeonKey = 0,
    Gem = 1,
    Cloak = 2,
    Mining = 3,
    Companion = 4,
}

// Daily Deals (DailyDealController + class `ty` gốc): mỗi ngày (UTC) 3 deal khác size VÀ khác loại, sinh bằng
// System.Random(seed * 31 + số deal đã mua) nên mọi máy cùng ngày ra cùng deal. Mua hết 3 → sinh 3 deal mới.
// combo = size * 10 + type.
public static class DailyDealService
{
    public const int SIZE_COUNT = 4;
    public const int TYPE_COUNT = 5;
    public const int SLOT_COUNT = 3;

    // Bảng thưởng theo size (ty.yiv..yje gốc).
    private static readonly int[] DRAGON_KEYS = { 1, 2, 3, 5 };
    private static readonly int[] ZOMBIE_KEYS = { 1, 2, 3, 5 };
    private static readonly int[] GEMS = { 200, 500, 2000, 5000 };
    private static readonly int[] CLOAKS = { 20, 50, 200, 500 };
    private static readonly int[] PICKAXES = { 10, 20, 100, 200 };
    private static readonly int[] GOLDEN_PICKAXES = { 1, 2, 10, 20 };
    private static readonly int[] DRILLS = { 1, 2, 10, 20 };
    private static readonly int[] BONES = { 400, 1000, 4000, 10000 };
    private static readonly int[] LOOT_BOXES = { 100, 300, 1500, 3000 };    // deal nào cũng kèm
    private static readonly int[] BONUS_GEMS = { 50, 150, 750, 1500 };      // deal KHÔNG phải loại Gem kèm thêm

    public static event Action Changed;

    private static UserShopData User => GameData.userData.shop;
    private static int cachedSeed;

    // Thưởng của 1 deal, đúng thứ tự hiển thị (DailyDealSlotUI.kam) = thứ tự phát (ty.jtb).
    public static List<ShopReward> GetRewards(DealSize size, DealType type)
    {
        int s = (int)size;
        List<ShopReward> rewards = new List<ShopReward>();
        switch (type)
        {
            case DealType.DungeonKey:
                rewards.Add(new ShopReward(ShopRewardType.DragonKey, DRAGON_KEYS[s]));
                rewards.Add(new ShopReward(ShopRewardType.ZombieKey, ZOMBIE_KEYS[s]));
                break;
            case DealType.Gem:
                rewards.Add(new ShopReward(ShopRewardType.Gem, GEMS[s]));
                break;
            case DealType.Cloak:
                rewards.Add(new ShopReward(ShopRewardType.Cloak, CLOAKS[s]));
                break;
            case DealType.Mining:
                rewards.Add(new ShopReward(ShopRewardType.Pickaxe, PICKAXES[s]));
                rewards.Add(new ShopReward(ShopRewardType.GoldenPickaxe, GOLDEN_PICKAXES[s]));
                rewards.Add(new ShopReward(ShopRewardType.Drill, DRILLS[s]));
                break;
            case DealType.Companion:
                rewards.Add(new ShopReward(ShopRewardType.Bone, BONES[s]));
                break;
        }

        rewards.Add(new ShopReward(ShopRewardType.LootBox, LOOT_BOXES[s]));
        if (type != DealType.Gem)
        {
            rewards.Add(new ShopReward(ShopRewardType.Gem, BONUS_GEMS[s]));
        }
        return rewards;
    }

    // ty.jtc gốc.
    public static string GetProductId(DealSize size)
    {
        switch (size)
        {
            case DealSize.Tiny: return PurchaseController.STORE_ID_DEAL_TINY;
            case DealSize.Small: return PurchaseController.STORE_ID_DEAL_SMALL;
            case DealSize.Medium: return PurchaseController.STORE_ID_DEAL_MEDIUM;
            default: return PurchaseController.STORE_ID_DEAL_LARGE;
        }
    }

    // ty.jtd gốc.
    private static bool TryGetSize(string productId, out DealSize size)
    {
        for (int i = 0; i < SIZE_COUNT; i++)
        {
            if (GetProductId((DealSize)i) == productId)
            {
                size = (DealSize)i;
                return true;
            }
        }
        size = DealSize.Tiny;
        return false;
    }

    // jsm gốc: sang ngày mới → xoá danh sách đã mua + sinh lại; cùng ngày mà chưa có deal → sinh.
    public static void Refresh()
    {
        int today = GetTodaySeed();
        if (cachedSeed == today && User.dailyDealCurrentCombos.Count > 0)
        {
            return;
        }

        if (User.dailyDealSeed != today)
        {
            User.dailyDealPurchasedCombos = new List<int>();
            User.dailyDealSeed = today;
            User.isDataChanged = true;
            Generate(today);
        }
        else if (User.dailyDealCurrentCombos.Count == 0)
        {
            Generate(today);
        }
        cachedSeed = today;
    }

    // jsi gốc.
    public static bool TryGetDeal(int slot, out DealSize size, out DealType type)
    {
        size = DealSize.Tiny;
        type = DealType.DungeonKey;
        List<int> combos = User.dailyDealCurrentCombos;
        if (slot < 0 || slot >= combos.Count)
        {
            return false;
        }

        size = (DealSize)(combos[slot] / 10);
        type = (DealType)(combos[slot] % 10);
        return true;
    }

    // jsk gốc: ghi deal đang chờ thanh toán rồi gọi mua sản phẩm theo size.
    public static void BuyDeal(int slot)
    {
        if (TryGetDeal(slot, out DealSize size, out DealType type) == false)
        {
            DebugCustom.Log("[DailyDealController] BuyDeal: invalid slot " + slot);
            return;
        }

        User.pendingDealCombo = (int)size * 10 + (int)type;
        User.isDataChanged = true;
        PurchaseController.Instance.Purchase(GetProductId(size));
    }

    // jsl gốc: thanh toán xong → phát thưởng, đánh dấu đã mua, gỡ khỏi danh sách hiện tại.
    public static void OnDealPurchased(string productId)
    {
        int combo = User.pendingDealCombo;
        if (combo < 0)
        {
            // Mất PendingDealCombo (app tắt giữa chừng) → dò lại theo size của sản phẩm.
            if (TryGetSize(productId, out DealSize recovered) == false || TryFindCurrentBySize(recovered, out combo) == false)
            {
                DebugCustom.LogError("[DailyDealController] Cannot resolve deal combo for product " + productId + "; reward skipped.");
                return;
            }
        }

        DealSize size = (DealSize)(combo / 10);
        DealType type = (DealType)(combo % 10);
        List<ShopReward> rewards = GetRewards(size, type);
        for (int i = 0; i < rewards.Count; i++)
        {
            ShopService.Grant(rewards[i]);
        }

        if (User.dailyDealPurchasedCombos.Contains(combo) == false)
        {
            User.dailyDealPurchasedCombos.Add(combo);
        }
        User.pendingDealCombo = -1;

        // jsq gốc.
        User.dailyDealCurrentCombos.Remove(combo);
        User.isDataChanged = true;
        if (User.dailyDealCurrentCombos.Count == 0)
        {
            Generate(GetTodaySeed());
        }
        else
        {
            Changed?.Invoke();
        }
    }

    // jsr gốc.
    private static bool TryFindCurrentBySize(DealSize size, out int combo)
    {
        List<int> combos = User.dailyDealCurrentCombos;
        for (int i = 0; i < combos.Count; i++)
        {
            if (combos[i] / 10 == (int)size)
            {
                combo = combos[i];
                return true;
            }
        }
        combo = -1;
        return false;
    }

    // jsn gốc: yyyyMMdd theo ngày UTC.
    private static int GetTodaySeed()
    {
        DateTime today = DateTime.UtcNow.Date;
        return today.Year * 10000 + today.Month * 100 + today.Day;
    }

    // jso gốc: không đủ 3 deal (đã mua gần hết 20 combo) → xoá danh sách đã mua rồi sinh lại.
    private static void Generate(int seed)
    {
        List<int> combos = Pick(seed, User.dailyDealPurchasedCombos);
        if (combos.Count < SLOT_COUNT)
        {
            User.dailyDealPurchasedCombos = new List<int>();
            combos = Pick(seed, User.dailyDealPurchasedCombos);
        }

        User.dailyDealCurrentCombos = combos;
        User.isDataChanged = true;
        Changed?.Invoke();
    }

    // jsp gốc: gom combo chưa mua → xáo Fisher-Yates → lấy lần lượt, bỏ combo trùng size hoặc trùng loại, đủ 3 thì dừng.
    private static List<int> Pick(int seed, List<int> purchased)
    {
        Random random = new Random(seed * 31 + purchased.Count);
        List<int> pool = new List<int>(SIZE_COUNT * TYPE_COUNT);
        for (int size = 0; size < SIZE_COUNT; size++)
        {
            for (int type = 0; type < TYPE_COUNT; type++)
            {
                int combo = size * 10 + type;
                if (purchased.Contains(combo) == false) pool.Add(combo);
            }
        }

        for (int n = pool.Count; n > 1; n--)
        {
            int j = random.Next(n);
            int temp = pool[j];
            pool[j] = pool[n - 1];
            pool[n - 1] = temp;
        }

        List<int> result = new List<int>(SLOT_COUNT);
        HashSet<int> usedSizes = new HashSet<int>();
        HashSet<int> usedTypes = new HashSet<int>();
        for (int i = 0; i < pool.Count && result.Count < SLOT_COUNT; i++)
        {
            int size = pool[i] / 10;
            int type = pool[i] % 10;
            if (usedSizes.Contains(size) || usedTypes.Contains(type)) continue;
            result.Add(pool[i]);
            usedSizes.Add(size);
            usedTypes.Add(type);
        }

        // [SUY] gốc sort bằng 1 lambda chưa đọc — xếp tăng dần (deal nhỏ ở trên).
        result.Sort();
        return result;
    }
}
