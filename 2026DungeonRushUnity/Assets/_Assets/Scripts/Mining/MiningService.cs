using System;
using UnityEngine;

// Logic save/kinh tế hệ Đào mỏ (phần không dính scene của MiningController + UserController gốc).
// Stateless — thao tác trên GameData.staticData.mining + GameData.userData.mining/items.
// Reverse il2cpp v41: MiningController.jbe/jbi/jbj/jci/jck..jcm, MiningConfig.izn, UserController.ecz.
public static class MiningService
{
    public static MiningConfig Config => GameData.staticData.mining;
    private static UserMiningData User => GameData.userData.mining;
    private static UserItemData Items => GameData.userData.items;

    public static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    // ----- Lần đầu vào mỏ (jbe gốc) -----

    // Seed == 0: tạo seed map Random.Range(1, int.MaxValue), mốc hồi cuốc = bây giờ, tặng StartingPickaxeCount cuốc.
    public static void InitIfNeeded()
    {
        if (User.seed != 0)
        {
            return;
        }

        User.seed = UnityEngine.Random.Range(1, int.MaxValue);
        User.lastPickaxeRegenTime = Now();
        Items.Receive(ItemType.PICKAXE, Config.StartingPickaxeCount);
        User.isDataChanged = true;
    }

    // ----- Dụng cụ -----

    public static ItemType ToItemType(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return ItemType.DRILL;
            case MiningToolType.GoldenPickaxe: return ItemType.GOLDEN_PICKAXE;
            default: return ItemType.PICKAXE;
        }
    }

    // jcn/jco/jcp
    public static int GetToolCount(MiningToolType tool)
    {
        return (int)Items.GetQuantityHave(ToItemType(tool));
    }

    public static bool ConsumeTool(MiningToolType tool)
    {
        bool ok = Items.Consume(ToItemType(tool), 1);
        if (ok)
        {
            User.isDataChanged = true;
        }
        return ok;
    }

    // izn: trần cuốc = MaxPickaxeCount + MaxPickaxeIncrease của các gói offer đã mua + Mastery MiningMaxPickaxe (làm tròn chẵn).
    public static int GetMaxPickaxe()
    {
        int max = Config.MaxPickaxeCount;
        int bought = Mathf.Min(User.offerPurchasedCount, Config.MiningOfferTiers.Count);
        for (int i = 0; i < bought; i++)
        {
            max += Config.MiningOfferTiers[i].MaxPickaxeIncrease;
        }

        if (GameData.userData.mastery.IsUnlocked(MasteryUpgradeType.MiningMaxPickaxe))
        {
            max += (int)Math.Round(MasteryService.GetCurrentValue(MasteryUpgradeType.MiningMaxPickaxe), MidpointRounding.ToEven);
        }
        return max;
    }

    // jbi: hồi cuốc theo thời gian (PickaxeRegenIntervalMinutes/cuốc), chỉ hồi tới trần; đầy thì dời mốc về hiện tại.
    // Trả true nếu số cuốc tăng.
    public static bool RegenPickaxes()
    {
        long now = Now();
        if (User.lastPickaxeRegenTime <= 0)
        {
            User.lastPickaxeRegenTime = now;
            User.isDataChanged = true;
            return false;
        }

        int pickaxe = GetToolCount(MiningToolType.Pickaxe);
        int max = GetMaxPickaxe();
        if (pickaxe >= max)
        {
            User.lastPickaxeRegenTime = now;
            User.isDataChanged = true;
            return false;
        }

        long interval = Config.PickaxeRegenIntervalMinutes * 60L;
        if (interval < 1)
        {
            return false;
        }

        long count = (now - User.lastPickaxeRegenTime) / interval;
        if (count < 1)
        {
            return false;
        }

        int newCount = (int)Math.Min(pickaxe + count, max);
        Items.SetQuantity(ItemType.PICKAXE, newCount);
        User.lastPickaxeRegenTime += count * interval;
        User.isDataChanged = true;
        return true;
    }

    // Số cuốc tính cả phần hồi tới hiện tại (không ghi save) — WingCraftTabPage.kzk gốc.
    public static int GetPickaxeWithRegen()
    {
        int pickaxe = GetToolCount(MiningToolType.Pickaxe);
        int max = GetMaxPickaxe();
        long interval = Config.PickaxeRegenIntervalMinutes * 60L;
        if (pickaxe >= max || interval < 1 || User.lastPickaxeRegenTime <= 0)
        {
            return pickaxe;
        }
        long count = (Now() - User.lastPickaxeRegenTime) / interval;
        return (int)Math.Min(pickaxe + Math.Max(0, count), max);
    }

    // Chấm đỏ nút Mining (WingCraftTabPage.kzj gốc): chưa vào mỏ lần nào, hoặc cuốc đã hồi đầy.
    public static bool IsNotify()
    {
        if (Config == null)
        {
            return false;
        }
        return User.seed == 0 || GetPickaxeWithRegen() >= GetMaxPickaxe();
    }

    // jbj: số giây tới cuốc kế tiếp (0 khi đã đầy).
    public static int GetSecondsToNextPickaxe()
    {
        if (GetToolCount(MiningToolType.Pickaxe) >= GetMaxPickaxe())
        {
            return 0;
        }

        long interval = Config.PickaxeRegenIntervalMinutes * 60L;
        if (interval < 1)
        {
            return 0;
        }

        long elapsed = Now() - User.lastPickaxeRegenTime;
        long left = interval - elapsed % interval;
        return (int)Math.Max(0, left);
    }

    // ----- Quảng cáo nhận dụng cụ (jci/jcj..jcm + MiningPopup.jeq/jer) -----

    // jci: sang ngày mới (UTC) thì reset 3 bộ đếm ads.
    public static void CheckDailyAdReset()
    {
        long now = Now();
        if (User.lastMiningAdResetTime != -1)
        {
            DateTime last = DateTimeOffset.FromUnixTimeSeconds(User.lastMiningAdResetTime).UtcDateTime.Date;
            DateTime today = DateTimeOffset.FromUnixTimeSeconds(now).UtcDateTime.Date;
            if (today <= last)
            {
                return;
            }
        }

        User.pickaxeAdDailyCount = 0;
        User.drillAdDailyCount = 0;
        User.goldenPickaxeAdDailyCount = 0;
        User.lastMiningAdResetTime = now;
        User.isDataChanged = true;
    }

    // jcl
    public static int GetAdLimit(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return Config.DailyDrillAdLimit;
            case MiningToolType.GoldenPickaxe: return Config.DailyGoldenPickaxeAdLimit;
            default: return Config.DailyPickaxeAdLimit;
        }
    }

    // jcm
    public static int GetAdCount(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return User.drillAdDailyCount;
            case MiningToolType.GoldenPickaxe: return User.goldenPickaxeAdDailyCount;
            default: return User.pickaxeAdDailyCount;
        }
    }

    // jcj
    public static int GetAdRemaining(MiningToolType tool)
    {
        return Mathf.Max(0, GetAdLimit(tool) - GetAdCount(tool));
    }

    // MiningPopup.jew: số dụng cụ nhận mỗi lượt ads.
    public static int GetAdRewardCount(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: return Config.DrillAdRewardCount;
            case MiningToolType.GoldenPickaxe: return Config.GoldenPickaxeAdRewardCount;
            default: return Config.PickaxeAdRewardCount;
        }
    }

    // MiningPopup.jer: xem xong ads → +1 lượt đã xem (jck) + tặng dụng cụ (jey).
    public static void OnAdWatched(MiningToolType tool)
    {
        switch (tool)
        {
            case MiningToolType.Drill: User.drillAdDailyCount++; break;
            case MiningToolType.GoldenPickaxe: User.goldenPickaxeAdDailyCount++; break;
            default: User.pickaxeAdDailyCount++; break;
        }
        Items.Receive(ToItemType(tool), GetAdRewardCount(tool));
        User.isDataChanged = true;
        GameData.Save();
    }

    // ----- Quặng (UserController.ecz gốc) -----

    public static int GetOre(MineOreType ore)
    {
        return WingService.GetOre(ore);
    }

    // Đập vỡ khối quặng nhặt được (IsCollectible) → +quặng, ghi điểm Clan War (fzp gốc nghe OreMined).
    public static void AddOre(MineOreType ore, int amount)
    {
        Items.Receive(WingService.ToItemType(ore), amount);
        ClanWarController.Instance.RecordMining(GetClanOreKey(ore), amount);
    }

    // Tên quặng trong ClanWarAction.mining (StaticClanData.ORES).
    private static string GetClanOreKey(MineOreType ore)
    {
        switch (ore)
        {
            case MineOreType.CoalMine: return "Coal";
            case MineOreType.DiamondMine: return "Diamond";
            case MineOreType.EmeraldMine: return "Emerald";
            case MineOreType.GoldMine: return "Gold";
            case MineOreType.IronMine: return "Iron";
            case MineOreType.RubyMine: return "Ruby";
            case MineOreType.Stone: return "Stone";
            default: return ore.ToString();
        }
    }

    // ----- Level mỏ -----

    // jcb: level mỏ theo độ sâu, chặn ở level cao nhất (60).
    public static int GetMineLevel(int depth)
    {
        return Mathf.Min(Config.GetMineLevelFromDepth(depth), Config.GetMaxMineLevel());
    }

    // jcs
    public static bool IsMaxMineLevel()
    {
        return User.mineLevel >= Config.GetMaxMineLevel();
    }

    // ----- Gói Mining Offer (MiningOfferPopup + PurchaseController gốc) -----

    public static MiningOfferTier GetNextOffer()
    {
        return Config.GetOfferTier(Config.GetOfferTierIndex(User.offerPurchasedCount));
    }

    public static bool IsOfferProduct(string productId)
    {
        return Config != null && Config.GetOfferTierByStoreId(productId) != null;
    }

    // Mua gói: tặng khoan/cuốc vàng/cuốc/gem + tăng trần cuốc (qua offerPurchasedCount).
    public static void GrantOffer(string productId)
    {
        MiningOfferTier tier = Config.GetOfferTierByStoreId(productId);
        if (tier == null)
        {
            return;
        }

        Items.Receive(ItemType.DRILL, tier.Drill);
        Items.Receive(ItemType.GOLDEN_PICKAXE, tier.GoldenPickaxe);
        Items.Receive(ItemType.PICKAXE, tier.Pickaxe);
        Items.Receive(ItemType.GEM, tier.Gem);
        User.offerPurchasedCount++;
        User.isDataChanged = true;
    }
}
