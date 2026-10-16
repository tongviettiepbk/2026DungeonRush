using System;
using System.Collections.Generic;

// Weapon Offer — gói bán 1 vũ khí có sẵn substat (OfferController gốc, reverse il2cpp v41 jiv..jjw, .cctor):
//  - Cấu hình remote "weapon_offer_config" = "giá,itemId|..." (live: 2,1301|10,1333|20,1364|50,1430|100,1432);
//    giá → sản phẩm lootio.offer_XXX. Mở từ playerLevel 3 (OfferUnlockPlayerLevel), lần đầu sau khi thắng 1 màn campaign.
//  - Mỗi gói sống 72h; hết hạn thì sinh lại CÙNG gói (offerIndex chỉ tăng khi mua). Hết danh sách thì không còn gói.
//  - Substat cố định theo tier = clamp(rarity − 4, 0, 6) của OfferSubStatTiers, giá trị = MaxValue × 0.75.
//  - Mua: vũ khí mặc thẳng vào slot WEAPON (level 1, thay vũ khí cũ), sang gói kế.
public static class WeaponOfferService
{
    public const int UNLOCK_PLAYER_LEVEL = 3;
    public const long DURATION_SECONDS = 72 * 3600;
    private const float SUBSTAT_FACTOR = 0.75f;
    private const string REMOTE_CONFIG = "2,1301|10,1333|20,1364|50,1430|100,1432";

    // ycl gốc: mốc giá → sản phẩm IAP.
    private static readonly Dictionary<int, string> PRODUCT_BY_PRICE = new Dictionary<int, string>
    {
        { 1, "lootio.offer_099" }, { 2, "lootio.offer_199" }, { 3, "lootio.offer_299" }, { 5, "lootio.offer_499" },
        { 10, "lootio.offer_999" }, { 15, "lootio.offer_1499" }, { 20, "lootio.offer_1999" }, { 30, "lootio.offer_2999" },
        { 50, "lootio.offer_4999" }, { 100, "lootio.offer_9999" },
    };

    // OfferController.OfferSubStatTiers (giá trị serialized đọc từ APK).
    private static readonly SubStatType[][] SUBSTAT_TIERS =
    {
        new[] { SubStatType.AttackSpeed },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed, SubStatType.MeleeDamage },
        new[] { SubStatType.DoubleHitChance, SubStatType.AttackSpeed, SubStatType.MeleeDamage },
    };

    public static event Action OfferChanged;

    private static UserShopData User => GameData.userData.shop;

    public static bool IsOfferProduct(string productId)
    {
        return PRODUCT_BY_PRICE.ContainsValue(productId);
    }

    // jjj gốc.
    public static bool HasOffer()
    {
        return string.IsNullOrEmpty(User.offerWeaponId) == false;
    }

    // jjk gốc.
    public static bool IsExpired()
    {
        return User.offerStartTime < 0 || ShopService.Now() >= User.offerStartTime + DURATION_SECONDS;
    }

    // jja gốc: nút lobby hiện khi đã chào lần đầu, còn gói và chưa hết hạn.
    public static bool IsOfferVisible()
    {
        return User.isOfferFirstShown && HasOffer() && IsExpired() == false;
    }

    // jjl gốc.
    public static long GetSecondsLeft()
    {
        if (User.offerStartTime < 0)
            return 0;
        return Math.Max(0, User.offerStartTime + DURATION_SECONDS - ShopService.Now());
    }

    public static WeaponData GetWeapon()
    {
        return HasOffer() ? GameData.staticData.weapons.GetData(User.offerWeaponId) : null;
    }

    public static string GetProductId() => User.offerProductId;

    public static List<GearSubStat> GetSubStats() => User.offerSubStats;

    // jiw gốc (thắng 1 màn campaign): lần đầu đủ level 3 → bật gói. Trả true nếu vừa bật lần đầu (để mở popup).
    public static bool OnCampaignWon()
    {
        if (User.isOfferFirstShown || GameData.userData.player.playerLevel < UNLOCK_PLAYER_LEVEL)
            return false;

        User.isOfferFirstShown = true;
        User.isDataChanged = true;
        EnsureOffer();
        return HasOffer();
    }

    // jjb gốc: chưa có gói → sinh; hết hạn → sinh lại; còn hạn → cập nhật substat nếu cấu hình đổi (jjc).
    public static void EnsureOffer()
    {
        if (HasOffer() == false || IsExpired())
        {
            Generate();
            return;
        }

        WeaponData weapon = GetWeapon();
        if (weapon == null)
            return;
        List<GearSubStat> subStats = BuildSubStats(weapon.rarity);
        if (SameSubStats(subStats, User.offerSubStats) == false)
        {
            User.offerSubStats = subStats;
            User.isDataChanged = true;
        }
    }

    // jje gốc.
    private static void Generate()
    {
        List<(int price, int itemId)> entries = ParseConfig(REMOTE_CONFIG);
        for (int i = User.offerIndex; i < entries.Count; i++)
        {
            (int price, int itemId) = entries[i];
            WeaponData weapon = FindWeaponByItemId(itemId);
            if (itemId <= 0 || PRODUCT_BY_PRICE.TryGetValue(price, out string productId) == false || weapon == null)
            {
                DebugCustom.Log("[WeaponOffer] Bỏ qua gói " + i + " (giá " + price + ", item " + itemId + ")");
                continue;
            }

            User.offerIndex = i;
            User.offerWeaponId = weapon.assetName;
            User.offerProductId = productId;
            User.offerStartTime = ShopService.Now();
            User.offerSubStats = BuildSubStats(weapon.rarity);
            User.isDataChanged = true;
            GameData.Save();
            OfferChanged?.Invoke();
            return;
        }

        // "All offers exhausted."
        Clear();
    }

    // jjh gốc: mặc vũ khí (level 1) → sang gói kế.
    public static void OnPurchased(string productId)
    {
        WeaponData weapon = GetWeapon();
        if (weapon != null && productId == User.offerProductId)
        {
            GameData.userData.equipment.Equip(GearSlotType.WEAPON, weapon.assetName, weapon.rarity, 1,
                new List<GearSubStat>(User.offerSubStats));
        }

        User.offerIndex++;
        Clear();
        EnsureOffer();
        OfferChanged?.Invoke();
    }

    private static void Clear()
    {
        User.offerWeaponId = string.Empty;
        User.offerProductId = string.Empty;
        User.offerStartTime = -1;
        User.offerSubStats = new List<GearSubStat>();
        User.isDataChanged = true;
        GameData.Save();
    }

    // jjf gốc.
    private static List<GearSubStat> BuildSubStats(Rarity rarity)
    {
        int tier = Math.Max(0, Math.Min((int)rarity - 4, SUBSTAT_TIERS.Length - 1));
        List<GearSubStat> result = new List<GearSubStat>();
        foreach (SubStatType type in SUBSTAT_TIERS[tier])
            result.Add(new GearSubStat(type, LootService.GetSubStatMaxValue(type) * SUBSTAT_FACTOR));
        return result;
    }

    private static bool SameSubStats(List<GearSubStat> a, List<GearSubStat> b)
    {
        if (a == null || b == null || a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].type != b[i].type || Math.Abs(a[i].value - b[i].value) > 0.001f)
                return false;
        }
        return true;
    }

    private static WeaponData FindWeaponByItemId(int itemId)
    {
        foreach (WeaponData weapon in GameData.staticData.weapons.weapons)
        {
            if (weapon.itemId == itemId)
                return weapon;
        }
        return null;
    }

    private static List<(int, int)> ParseConfig(string config)
    {
        List<(int, int)> result = new List<(int, int)>();
        foreach (string part in config.Split('|'))
        {
            string[] pair = part.Split(',');
            if (pair.Length == 2 && int.TryParse(pair[0], out int price) && int.TryParse(pair[1], out int itemId))
                result.Add((price, itemId));
        }
        return result;
    }
}
