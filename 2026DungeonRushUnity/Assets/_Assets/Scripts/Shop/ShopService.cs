using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChestOutcomeKind
{
    Companion = 0,
    Cape = 1,
}

// 1 món mở ra từ rương (class `hq` gốc).
public class ChestOutcome
{
    public ChestData chest;
    public ChestOutcomeKind kind;
    public string id;           // CompanionData.assetName hoặc capeId
    public string name;
    public Sprite icon;
    public Rarity rarity;
    public bool isNew;          // chưa từng sở hữu mẫu này
    public bool isPity;         // món được ép ra do đầy pity
}

// Logic Store: mua bằng gem, boost exp, mở rương, phát thưởng gói IAP. Số liệu + bằng chứng: DecodedData/SHOP_MODEL.md.
public static class ShopService
{
    private static UserShopData User => GameData.userData.shop;
    private static UserItemData Items => GameData.userData.items;
    private static ChestConfig ChestConfig => GameData.staticData.shop.chestConfig;

    public static long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static int GetGem()
    {
        return (int)Items.GetQuantityHave(ItemType.GEM);
    }

    #region Resources (StoreTabPage.ktx / kty)

    public static bool BuyDrill()
    {
        return BuyByGem(StaticShopData.DRILL_GEM_COST, ItemType.DRILL, StaticShopData.RESOURCE_BUY_AMOUNT);
    }

    public static bool BuyGoldenPickaxe()
    {
        return BuyByGem(StaticShopData.GOLDEN_PICKAXE_GEM_COST, ItemType.GOLDEN_PICKAXE, StaticShopData.RESOURCE_BUY_AMOUNT);
    }

    private static bool BuyByGem(int cost, ItemType item, int amount)
    {
        if (Items.Consume(ItemType.GEM, cost) == false)
        {
            return false;
        }

        Items.Receive(item, amount);
        GameData.Save(true);
        return true;
    }

    #endregion

    #region Boost exp (StoreTabPage.kua / kub / kuc / kud, ExperienceController.hdq)

    public static bool IsX2BoostActive()
    {
        return User.x2ExpBoostEndTime > Now();
    }

    public static bool IsX5BoostActive()
    {
        return User.x5ExpBoostEndTime > Now();
    }

    public static long GetX2BoostRemaining()
    {
        return Math.Max(0, User.x2ExpBoostEndTime - Now());
    }

    public static long GetX5BoostRemaining()
    {
        return Math.Max(0, User.x5ExpBoostEndTime - Now());
    }

    // Đang chạy thì không mua chồng. Trả false khi thiếu gem.
    public static bool BuyX2Boost()
    {
        if (IsX2BoostActive() || Items.Consume(ItemType.GEM, StaticShopData.X2_BOOST_GEM_COST) == false)
        {
            return false;
        }

        User.x2ExpBoostEndTime = Now() + StaticShopData.X2_BOOST_DURATION;
        User.isDataChanged = true;
        GameData.Save(true);
        return true;
    }

    public static bool BuyX5Boost()
    {
        if (IsX5BoostActive() || Items.Consume(ItemType.GEM, StaticShopData.X5_BOOST_GEM_COST) == false)
        {
            return false;
        }

        User.x5ExpBoostEndTime = Now() + StaticShopData.X5_BOOST_DURATION;
        User.isDataChanged = true;
        GameData.Save(true);
        return true;
    }

    // Lifetime ×2, boost 20 phút ×1.5, boost 5 phút ×2 — nhân dồn.
    public static float GetExpMultiplier()
    {
        float multiplier = User.isLifetimeBoostBought ? StaticShopData.LIFETIME_BOOST_MULTIPLIER : 1f;
        if (IsX2BoostActive()) multiplier *= StaticShopData.X2_BOOST_MULTIPLIER;
        if (IsX5BoostActive()) multiplier *= StaticShopData.X5_BOOST_MULTIPLIER;
        return multiplier;
    }

    public static int ApplyExpBoost(int amount)
    {
        float multiplier = GetExpMultiplier();
        return multiplier > 1f ? Mathf.RoundToInt(amount * multiplier) : amount;
    }

    #endregion

    #region Rương (UserController.eet / eeu / eev / eex / eey / eew)

    // StoreTabPage.ktw gốc: mục rương chỉ hiện khi đã mở tính năng Cape.
    public static bool IsChestStoreOpen()
    {
        return StaticShopData.CHEST_STORE_ENABLED && CapeService.IsUnlocked();
    }

    // ChestStoreSection.fok gốc: rương có RequireRarityCompletion chỉ bán khi đã sưu tầm đủ rarity yêu cầu.
    public static List<ChestData> GetAvailableChests()
    {
        List<ChestData> result = new List<ChestData>();
        if (ChestConfig == null)
        {
            return result;
        }

        for (int i = 0; i < ChestConfig.Chests.Count; i++)
        {
            ChestData chest = ChestConfig.Chests[i];
            if (chest == null) continue;
            if (chest.RequireRarityCompletion == false || IsRarityComplete(chest.RequiredCompletedRarity))
            {
                result.Add(chest);
            }
        }
        return result;
    }

    // Đã sở hữu MỌI pet và MỌI mẫu cape của rarity này.
    public static bool IsRarityComplete(Rarity rarity)
    {
        List<CompanionData> companions = GameData.staticData.companions.GetPool(rarity);
        for (int i = 0; i < companions.Count; i++)
        {
            if (GameData.userData.companions.IsOwned(companions[i].assetName) == false)
            {
                return false;
            }
        }

        List<CapeData> capes = GetCapePool(rarity);
        for (int i = 0; i < capes.Count; i++)
        {
            if (IsCapeOwned(capes[i].capeId) == false)
            {
                return false;
            }
        }
        return true;
    }

    public static int GetPity(Rarity rarity)
    {
        return User.GetPity(rarity);
    }

    // Số lần mở còn lại tới khi chắc chắn ra rarity cao nhất của rương.
    public static int GetPityRemaining(ChestData chest)
    {
        return Mathf.Max(1, chest.PityCount - User.GetPity(chest.Rarity));
    }

    // Mua 1 rương bằng gem rồi mở. null = thiếu gem.
    public static List<ChestOutcome> BuyChest(ChestData chest)
    {
        if (Items.Consume(ItemType.GEM, chest.GemCost) == false)
        {
            return null;
        }
        return OpenChest(chest);
    }

    public static List<ChestOutcome> OpenChest(ChestData chest)
    {
        List<ChestOutcome> results = new List<ChestOutcome>();
        RollChest(chest, results);
        GameData.Save(true);
        return results;
    }

    // Mở mọi rương trong gói (mỗi entry mở Count lần).
    public static List<ChestOutcome> OpenBundle(ChestBundleData bundle)
    {
        List<ChestOutcome> results = new List<ChestOutcome>();
        for (int i = 0; i < bundle.Chests.Count; i++)
        {
            ChestBundleEntry entry = bundle.Chests[i];
            if (entry == null || entry.Chest == null) continue;
            for (int n = 0; n < entry.Count; n++)
            {
                RollChest(entry.Chest, results);
            }
        }
        GameData.Save(true);
        return results;
    }

    // 1 lần mở: roll ItemCount rarity; chưa món nào trúng rarity cao nhất mà pity + 1 ≥ PityCount → ép món CUỐI.
    // Có món rarity cao nhất → pity về 0, không thì +1.
    private static void RollChest(ChestData chest, List<ChestOutcome> results)
    {
        int count = Mathf.Max(1, chest.ItemCount);
        int pity = User.GetPity(chest.Rarity);

        Rarity[] rarities = new Rarity[count];
        bool hasTop = false;
        for (int i = 0; i < count; i++)
        {
            rarities[i] = chest.RollRarity();
            hasTop |= rarities[i] == chest.Rarity;
        }

        bool isForced = false;
        if (hasTop == false && chest.PityCount >= 1 && pity + 1 >= chest.PityCount)
        {
            rarities[count - 1] = chest.Rarity;
            isForced = true;
        }

        bool gotTop = false;
        for (int i = 0; i < count; i++)
        {
            ChestOutcome outcome = RollOutcome(chest, rarities[i]);
            if (outcome == null) continue;
            bool isTop = outcome.rarity == chest.Rarity;
            outcome.isPity = isForced && i == count - 1 && isTop;
            gotTop |= isTop;
            results.Add(outcome);
        }

        if (chest.PityCount >= 1)
        {
            User.SetPity(chest.Rarity, gotTop ? 0 : pity + 1);
        }
    }

    // Chọn 1 pet/cape của rarity: ƯU TIÊN mẫu chưa sở hữu, hết mẫu mới thì random đều trong cả rarity.
    // Pet trùng → +1 thẻ; cape luôn là 1 bản mới (substat roll riêng).
    private static ChestOutcome RollOutcome(ChestData chest, Rarity rarity)
    {
        List<CompanionData> companions = GameData.staticData.companions.GetPool(rarity);
        List<CapeData> capes = GetCapePool(rarity);
        if (companions.Count + capes.Count == 0)
        {
            DebugCustom.LogError("[Chest] No companions or capes of rarity " + rarity + ". Falling back to any rarity.");
            companions = GameData.staticData.companions.companions;
            capes = GameData.staticData.capes.capes;
            if (companions.Count + capes.Count == 0)
            {
                return null;
            }
        }

        UserCompanionData userCompanions = GameData.userData.companions;
        List<int> newCompanions = new List<int>();
        for (int i = 0; i < companions.Count; i++)
        {
            if (userCompanions.IsOwned(companions[i].assetName) == false) newCompanions.Add(i);
        }
        List<int> newCapes = new List<int>();
        for (int i = 0; i < capes.Count; i++)
        {
            if (IsCapeOwned(capes[i].capeId) == false) newCapes.Add(i);
        }

        int companionIndex = -1;
        int capeIndex = -1;
        if (newCompanions.Count + newCapes.Count >= 1)
        {
            int pick = UnityEngine.Random.Range(0, newCompanions.Count + newCapes.Count);
            if (pick < newCompanions.Count) companionIndex = newCompanions[pick];
            else capeIndex = newCapes[pick - newCompanions.Count];
        }
        else
        {
            int pick = UnityEngine.Random.Range(0, companions.Count + capes.Count);
            if (pick < companions.Count) companionIndex = pick;
            else capeIndex = pick - companions.Count;
        }

        ChestOutcome outcome = new ChestOutcome { chest = chest };
        if (companionIndex >= 0)
        {
            CompanionData data = companions[companionIndex];
            outcome.isNew = userCompanions.IsOwned(data.assetName) == false;
            if (outcome.isNew) userCompanions.Own(data.assetName);
            else userCompanions.AddCards(data.assetName, 1);

            outcome.kind = ChestOutcomeKind.Companion;
            outcome.id = data.assetName;
            outcome.name = data.companionName;
            outcome.icon = data.icon;
            outcome.rarity = data.rarity;
        }
        else
        {
            CapeData data = capes[capeIndex];
            outcome.isNew = IsCapeOwned(data.capeId) == false;
            UserCapeData userCapes = GameData.userData.capes;
            userCapes.owned.Add(new CapeModel
            {
                instanceId = Guid.NewGuid().ToString(),
                capeId = data.capeId,
                level = 1,
                isNew = true,
                subStats = GearStatCalculator.RollSubStats(EquipmentStatResolver.LoadConfig(), data.subStatCount),
            });
            userCapes.isDataChanged = true;

            outcome.kind = ChestOutcomeKind.Cape;
            outcome.id = data.capeId.ToString();
            outcome.name = data.capeName;
            outcome.icon = data.icon;
            outcome.rarity = data.rarity;
        }
        return outcome;
    }

    private static List<CapeData> GetCapePool(Rarity rarity)
    {
        List<CapeData> pool = new List<CapeData>();
        List<CapeData> all = GameData.staticData.capes.capes;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].rarity == rarity) pool.Add(all[i]);
        }
        return pool;
    }

    private static bool IsCapeOwned(int capeId)
    {
        List<CapeModel> owned = GameData.userData.capes.owned;
        for (int i = 0; i < owned.Count; i++)
        {
            if (owned[i] != null && owned[i].capeId == capeId) return true;
        }
        return false;
    }

    // GameplayUI.kiz / kja gốc: gói rương đang chào ở lobby — Mythic khi đã sưu tầm đủ Epic, không thì Legendary.
    public static ChestBundleData GetFeaturedBundle()
    {
        if (ChestConfig == null)
        {
            return null;
        }
        return IsRarityComplete(Rarity.Epic) && ChestConfig.MythicFeaturedBundle != null
            ? ChestConfig.MythicFeaturedBundle
            : ChestConfig.LegendaryFeaturedBundle;
    }

    #endregion

    #region Thưởng (PurchaseController.dhk, ty.jtb)

    public static void Grant(ShopReward reward)
    {
        switch (reward.type)
        {
            case ShopRewardType.Gem: Items.Receive(ItemType.GEM, reward.amount); break;
            case ShopRewardType.LootBox: Items.Receive(ItemType.LOOT_TICKET, reward.amount); break;
            case ShopRewardType.Bone: Items.Receive(ItemType.BONE, reward.amount); break;
            case ShopRewardType.Cloak: Items.Receive(ItemType.CLOAK, reward.amount); break;
            case ShopRewardType.Pickaxe: Items.Receive(ItemType.PICKAXE, reward.amount); break;
            case ShopRewardType.GoldenPickaxe: Items.Receive(ItemType.GOLDEN_PICKAXE, reward.amount); break;
            case ShopRewardType.Drill: Items.Receive(ItemType.DRILL, reward.amount); break;
            case ShopRewardType.DragonKey: DungeonService.AddBonusKeys(DungeonType.DragonBoss, reward.amount); break;
            case ShopRewardType.ZombieKey: DungeonService.AddBonusKeys(DungeonType.ZombieHorde, reward.amount); break;
        }
    }

    // Phát thưởng 1 sản phẩm IAP đã thanh toán xong. Trả về danh sách món mở ra nếu là gói rương, còn lại null.
    public static List<ChestOutcome> GrantProduct(string productId)
    {
        if (productId == PurchaseController.STORE_ID_NO_ADS)
        {
            User.isNoAdsBought = true;
            User.isDataChanged = true;
            return null;
        }
        if (productId == PurchaseController.STORE_ID_LIFETIME_BOOST)
        {
            User.isLifetimeBoostBought = true;
            User.isDataChanged = true;
            return null;
        }

        ShopReward[] rewards = StaticShopData.GetProductRewards(productId);
        if (rewards != null)
        {
            for (int i = 0; i < rewards.Length; i++) Grant(rewards[i]);
            return null;
        }

        if (PurchaseController.DealProductIds.Contains(productId))
        {
            DailyDealService.OnDealPurchased(productId);
            return null;
        }

        if (PurchaseController.ChestBundleProductIds.Contains(productId))
        {
            ChestBundleData bundle = ChestConfig != null ? ChestConfig.GetBundleByStoreId(productId) : null;
            if (bundle == null)
            {
                DebugCustom.LogError("[PurchaseController] Chest bundle " + productId + " purchased but no ChestBundleData with that StoreId in ChestConfig.Bundles; rewards NOT granted.");
                return null;
            }

            // Mua gói rương khi chưa tới level mở Cape → mở sớm tính năng ("Unlocks the Cloak feature!").
            if (CapeService.IsUnlocked() == false)
            {
                User.isCapeFeatureEarlyUnlocked = true;
                User.isDataChanged = true;
            }
            Items.Receive(ItemType.GEM, bundle.GemAmount);
            return OpenBundle(bundle);
        }

        DebugCustom.Log("[PurchaseController] Sản phẩm chưa hỗ trợ: " + productId);
        return null;
    }

    #endregion
}
