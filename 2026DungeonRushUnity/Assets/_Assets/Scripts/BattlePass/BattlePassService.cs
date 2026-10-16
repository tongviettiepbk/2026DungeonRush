using System;
using System.Collections.Generic;

// Loại thưởng Battle Pass (RewardType gốc).
public enum BattlePassRewardType
{
    Bone = 0,
    Gem = 1,
    DragonBossDungeonKey = 2,
    ZombieHordeDungeonKey = 3,
    Lootbox = 4,
    Exp = 5,
    MiningGoldenPickaxe = 6,
    MiningPickaxe = 7,
    MiningDrill = 8,
    CloakCurrency = 9,
    CultistDungeonKey = 10,
    Vial = 11,
}

// 1 thưởng của 1 mốc (BattlePassRewardModel gốc).
public class BattlePassReward
{
    public int level;
    public bool isPremium;
    public BattlePassRewardType type1;
    public int amount1;
    public BattlePassRewardType type2;
    public int amount2;
}

// Battle Pass vô hạn (BattlePassPopup / BattlePassButton gốc, reverse il2cpp juh/juw/jvc/jvd/jtx + BattlePassConfig asset):
//  - Tiến độ = HighestLevel campaign. Mốc thưởng: màn 20, 25, 30… (StartLevel 20, RewardInterval 5).
//  - Mỗi mốc 2 hàng: Free / Premium (mua lootio.battlepass), mỗi hàng 2 thưởng.
//  - Thưởng xoay vòng trong pool LevelRewards (lọc UnlockLevel ≤ mốc): bước = (mốc − 20)/5,
//    dòng = pool[bước % pool], lượng = BaseAmount × Multiplier^(bước / pool).
//  - Nút lobby hiện từ playerLevel 4 (GameResources.BattlePassUnlockPlayerLevel), chấm đỏ khi còn mốc nhận được.
public static class BattlePassService
{
    public const int UNLOCK_PLAYER_LEVEL = 4;
    public const int REWARD_INTERVAL = 5;
    public const int START_LEVEL = 20;
    public const int FUTURE_REWARD_COUNT = 10;

    private struct SingleReward
    {
        public BattlePassRewardType type;
        public int baseAmount;
        public float multiplier;
        public SingleReward(BattlePassRewardType type, int baseAmount, float multiplier = 1f)
        {
            this.type = type;
            this.baseAmount = baseAmount;
            this.multiplier = multiplier;
        }
    }

    private struct LevelRewardConfig
    {
        public int unlockLevel;
        public SingleReward free1, free2, premium1, premium2;
    }

    private static LevelRewardConfig Row(int unlock, BattlePassRewardType f1, int a1, BattlePassRewardType p1, int pa1)
    {
        // Mọi dòng gốc: Free2 = 25 hộp loot, Premium2 = 20 Gem, Multiplier 1.
        return new LevelRewardConfig
        {
            unlockLevel = unlock,
            free1 = new SingleReward(f1, a1),
            free2 = new SingleReward(BattlePassRewardType.Lootbox, 25),
            premium1 = new SingleReward(p1, pa1),
            premium2 = new SingleReward(BattlePassRewardType.Gem, 20),
        };
    }

    // BattlePassConfig.LevelRewards gốc (DecodedData/tables/BattlePassConfig.json).
    private static readonly LevelRewardConfig[] LEVEL_REWARDS =
    {
        Row(0, BattlePassRewardType.Gem, 20, BattlePassRewardType.Lootbox, 100),
        Row(0, BattlePassRewardType.Bone, 50, BattlePassRewardType.DragonBossDungeonKey, 2),
        Row(0, BattlePassRewardType.MiningDrill, 1, BattlePassRewardType.ZombieHordeDungeonKey, 2),
        Row(0, BattlePassRewardType.Bone, 50, BattlePassRewardType.MiningDrill, 3),
        Row(0, BattlePassRewardType.MiningGoldenPickaxe, 1, BattlePassRewardType.MiningGoldenPickaxe, 3),
        Row(0, BattlePassRewardType.Gem, 20, BattlePassRewardType.Lootbox, 250),
        Row(0, BattlePassRewardType.MiningPickaxe, 10, BattlePassRewardType.MiningPickaxe, 20),
        Row(0, BattlePassRewardType.Bone, 50, BattlePassRewardType.DragonBossDungeonKey, 2),
        Row(0, BattlePassRewardType.MiningDrill, 1, BattlePassRewardType.ZombieHordeDungeonKey, 2),
        Row(0, BattlePassRewardType.Bone, 50, BattlePassRewardType.MiningDrill, 3),
        Row(0, BattlePassRewardType.MiningGoldenPickaxe, 1, BattlePassRewardType.MiningGoldenPickaxe, 3),
        Row(120, BattlePassRewardType.Vial, 50, BattlePassRewardType.CultistDungeonKey, 2),
    };

    public static event Action Changed;

    private static UserShopData User => GameData.userData.shop;

    public static bool IsUnlocked()
    {
        return GameData.userData.player.playerLevel >= UNLOCK_PLAYER_LEVEL;
    }

    public static bool IsPremiumBought() => User.isPremiumBattlePassBought;

    // User.HighestLevel gốc = level campaign cao nhất đã tới (màn kế của màn cao nhất đã qua).
    public static int GetHighestLevel()
    {
        StaticCampaignData campaign = GameData.staticData.campaign;
        UserCampaignData user = GameData.userData.campaign;
        int current = campaign.GetLevel(user.stageIdCurrent);
        int reached = user.passedStageId > 0 ? campaign.GetLevel(campaign.GetNextStageId(user.passedStageId)) : 1;
        return Math.Max(current, reached);
    }

    // Mốc cuối hiển thị: mốc đã tới + FutureRewardCount mốc tương lai.
    public static int GetLastShownLevel()
    {
        int reachedStep = Math.Max(0, (GetHighestLevel() - START_LEVEL) / REWARD_INTERVAL);
        return START_LEVEL + (reachedStep + FUTURE_REWARD_COUNT) * REWARD_INTERVAL;
    }

    // juw gốc.
    public static BattlePassReward GetReward(int level, bool isPremium)
    {
        List<LevelRewardConfig> pool = new List<LevelRewardConfig>();
        foreach (LevelRewardConfig row in LEVEL_REWARDS)
        {
            if (row.unlockLevel <= level)
                pool.Add(row);
        }
        if (pool.Count == 0)
            pool.AddRange(LEVEL_REWARDS);

        int step = (level - START_LEVEL) / REWARD_INTERVAL;
        int cycle = step / pool.Count;
        LevelRewardConfig config = pool[step % pool.Count];
        SingleReward r1 = isPremium ? config.premium1 : config.free1;
        SingleReward r2 = isPremium ? config.premium2 : config.free2;
        return new BattlePassReward
        {
            level = level,
            isPremium = isPremium,
            type1 = r1.type,
            amount1 = (int)(r1.baseAmount * Math.Pow(r1.multiplier, cycle)),
            type2 = r2.type,
            amount2 = (int)(r2.baseAmount * Math.Pow(r2.multiplier, cycle)),
        };
    }

    // jve gốc.
    public static bool IsClaimed(int level, bool isPremium)
    {
        return (isPremium ? User.claimedPremiumBattlePassRewards : User.claimedFreeBattlePassRewards).Contains(level);
    }

    public static bool IsReached(int level) => GetHighestLevel() >= level;

    public static bool CanClaim(int level, bool isPremium)
    {
        return IsReached(level) && IsClaimed(level, isPremium) == false && (isPremium == false || IsPremiumBought());
    }

    // juh / BattlePassButton.jtx gốc: mốc đầu tiên còn nhận được (-1 = không có).
    public static int GetFirstClaimableLevel()
    {
        int last = GetHighestLevel() / REWARD_INTERVAL * REWARD_INTERVAL;
        for (int level = START_LEVEL; level <= last; level += REWARD_INTERVAL)
        {
            if (IsClaimed(level, false) == false || (IsPremiumBought() && IsClaimed(level, true) == false))
                return level;
        }
        return -1;
    }

    public static bool HasClaimable() => IsUnlocked() && GetFirstClaimableLevel() >= 0;

    // jvc gốc.
    public static bool Claim(int level, bool isPremium)
    {
        if (CanClaim(level, isPremium) == false)
            return false;

        BattlePassReward reward = GetReward(level, isPremium);
        Grant(reward.type1, reward.amount1);
        Grant(reward.type2, reward.amount2);
        (isPremium ? User.claimedPremiumBattlePassRewards : User.claimedFreeBattlePassRewards).Add(level);
        User.isDataChanged = true;
        GameData.Save();
        Changed?.Invoke();
        return true;
    }

    // jvd gốc.
    private static void Grant(BattlePassRewardType type, int amount)
    {
        UserItemData items = GameData.userData.items;
        switch (type)
        {
            case BattlePassRewardType.Bone: items.Receive(ItemType.BONE, amount); break;
            case BattlePassRewardType.Gem: items.Receive(ItemType.GEM, amount); break;
            case BattlePassRewardType.DragonBossDungeonKey: DungeonService.AddBonusKeys(DungeonType.DragonBoss, amount); break;
            case BattlePassRewardType.ZombieHordeDungeonKey: DungeonService.AddBonusKeys(DungeonType.ZombieHorde, amount); break;
            case BattlePassRewardType.Lootbox: items.Receive(ItemType.LOOT_TICKET, amount); break;
            case BattlePassRewardType.Exp: PlayerExpService.Grant(amount, applyMultiplier: false); break;
            case BattlePassRewardType.MiningGoldenPickaxe: items.Receive(ItemType.GOLDEN_PICKAXE, amount); break;
            case BattlePassRewardType.MiningPickaxe: items.Receive(ItemType.PICKAXE, amount); break;
            case BattlePassRewardType.MiningDrill: items.Receive(ItemType.DRILL, amount); break;
            case BattlePassRewardType.CloakCurrency: items.Receive(ItemType.CLOAK, amount); break;
            case BattlePassRewardType.CultistDungeonKey: DungeonService.AddBonusKeys(DungeonType.Cultist, amount); break;
            case BattlePassRewardType.Vial: items.Receive(ItemType.VIAL, amount); break;
        }
    }

    // PurchaseController báo đã mua Premium → UI vẽ lại.
    public static void NotifyChanged() => Changed?.Invoke();
}
