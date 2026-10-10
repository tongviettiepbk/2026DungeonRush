using System.Collections.Generic;
using UnityEngine;

// Loại thưởng của Store (gói IAP + Daily Deal).
public enum ShopRewardType
{
    Gem = 0,
    LootBox = 1,        // "loot_box" gốc = User.Hammer → ItemType.LOOT_TICKET
    Bone = 2,
    Cloak = 3,
    DragonKey = 4,      // bonus key dungeon Dragon Boss
    ZombieKey = 5,      // bonus key dungeon Zombie Horde
    Pickaxe = 6,
    GoldenPickaxe = 7,
    Drill = 8,
}

public struct ShopReward
{
    public ShopRewardType type;
    public int amount;

    public ShopReward(ShopRewardType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}

// Config tĩnh của Store (reverse v41, xem DecodedData/SHOP_MODEL.md).
public class StaticShopData
{
    public const string CHEST_CONFIG_PATH = "Scriptable Objects/Shop/ChestConfig";

    // GameResources.ChestStoreEnabled gốc.
    public const bool CHEST_STORE_ENABLED = true;

    // StoreTabPage.zbo..zbt gốc: giá gem + thời lượng boost.
    public const int DRILL_GEM_COST = 45;
    public const int GOLDEN_PICKAXE_GEM_COST = 45;
    public const int RESOURCE_BUY_AMOUNT = 3;
    public const int X2_BOOST_GEM_COST = 45;
    public const int X5_BOOST_GEM_COST = 75;
    public const int X2_BOOST_DURATION = 1200;  // giây — nhãn "x1.5 Exp Boost 20m"
    public const int X5_BOOST_DURATION = 300;   // giây — nhãn "x2 Exp Boost 5m"

    // ExperienceController.hdq gốc: hệ số exp, nhân dồn với nhau.
    public const float LIFETIME_BOOST_MULTIPLIER = 2f;
    public const float X2_BOOST_MULTIPLIER = 1.5f;
    public const float X5_BOOST_MULTIPLIER = 2f;

    public ChestConfig chestConfig;

    public StaticShopData()
    {
        chestConfig = Resources.Load<ChestConfig>(CHEST_CONFIG_PATH);
        if (chestConfig == null)
        {
            DebugCustom.Log("[StaticShopData] Thiếu Resources/" + CHEST_CONFIG_PATH + " — chạy Tools/DungeonRush/Build Shop UI Prefabs");
        }
    }

    // Thưởng gói IAP (PurchaseController.dhk gốc).
    private static readonly Dictionary<string, ShopReward[]> PRODUCT_REWARDS = new Dictionary<string, ShopReward[]>
    {
        { PurchaseController.STORE_ID_GEM1, new[] { new ShopReward(ShopRewardType.Gem, 100) } },
        { PurchaseController.STORE_ID_GEM2, new[] { new ShopReward(ShopRewardType.Gem, 255) } },
        { PurchaseController.STORE_ID_GEM3, new[] { new ShopReward(ShopRewardType.Gem, 520) } },
        { PurchaseController.STORE_ID_GEM4, new[] { new ShopReward(ShopRewardType.Gem, 1100) } },
        { PurchaseController.STORE_ID_GEM5, new[] { new ShopReward(ShopRewardType.Gem, 2700) } },
        { PurchaseController.STORE_ID_GEM6, new[] { new ShopReward(ShopRewardType.Gem, 5500) } },
        { PurchaseController.STORE_ID_RESOURCE_PACK, new[] { new ShopReward(ShopRewardType.LootBox, 1200), new ShopReward(ShopRewardType.Bone, 750), new ShopReward(ShopRewardType.Gem, 400) } },
        { PurchaseController.STORE_ID_DUNGEON_KEYS, new[] { new ShopReward(ShopRewardType.DragonKey, 4), new ShopReward(ShopRewardType.ZombieKey, 4) } },
        { PurchaseController.STORE_ID_STARTER_PACK, new[] { new ShopReward(ShopRewardType.Gem, 500), new ShopReward(ShopRewardType.LootBox, 300) } },
        { PurchaseController.STORE_ID_EPIC_PACK, new[] { new ShopReward(ShopRewardType.Gem, 2400), new ShopReward(ShopRewardType.LootBox, 1500), new ShopReward(ShopRewardType.Bone, 750) } },
    };

    public static ShopReward[] GetProductRewards(string productId)
    {
        return PRODUCT_REWARDS.TryGetValue(productId, out ShopReward[] rewards) ? rewards : null;
    }

    // Giá hiển thị mặc định khi CHƯA có SDK IAP. [SUY] — giá thật do store trả về, không nằm trong APK.
    private static readonly Dictionary<string, string> DEFAULT_PRICES = new Dictionary<string, string>
    {
        { PurchaseController.STORE_ID_GEM1, "$0.99" },
        { PurchaseController.STORE_ID_GEM2, "$2.49" },
        { PurchaseController.STORE_ID_GEM3, "$4.99" },
        { PurchaseController.STORE_ID_GEM4, "$9.99" },
        { PurchaseController.STORE_ID_GEM5, "$24.99" },
        { PurchaseController.STORE_ID_GEM6, "$49.99" },
        { PurchaseController.STORE_ID_LIFETIME_BOOST, "$4.99" },
        { PurchaseController.STORE_ID_NO_ADS, "$4.99" },
        { PurchaseController.STORE_ID_RESOURCE_PACK, "$4.99" },
        { PurchaseController.STORE_ID_DUNGEON_KEYS, "$1.99" },
        { PurchaseController.STORE_ID_STARTER_PACK, "$1.99" },
        { PurchaseController.STORE_ID_EPIC_PACK, "$9.99" },
        { PurchaseController.STORE_ID_DEAL_TINY, "$0.99" },
        { PurchaseController.STORE_ID_DEAL_SMALL, "$2.49" },
        { PurchaseController.STORE_ID_DEAL_MEDIUM, "$9.99" },
        { PurchaseController.STORE_ID_DEAL_LARGE, "$24.99" },
        { PurchaseController.STORE_ID_CHEST_BUNDLE_LEGENDARY, "$19.99" },
        { PurchaseController.STORE_ID_CHEST_BUNDLE_MYTHIC, "$29.99" },
    };

    public static string GetDefaultPrice(string productId)
    {
        return DEFAULT_PRICES.TryGetValue(productId, out string price) ? price : null;
    }

    // Tên hiển thị (strings_en "Items.Chest.Name.{ChestId}").
    private static readonly Dictionary<string, string> CHEST_NAMES = new Dictionary<string, string>
    {
        { "standard", "Standard Chest" },
        { "epic", "Epic Chest" },
        { "legendary", "Legendary Chest" },
        { "mythic", "Mythic Vault" },
    };

    public static string GetChestName(ChestData chest)
    {
        return CHEST_NAMES.TryGetValue(chest.ChestId, out string name) ? name : chest.ChestName;
    }
}
