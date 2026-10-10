using System;
using System.Collections.Generic;

// Lớp mua hàng bằng tiền thật (PurchaseController gốc). Đây là lớp BASE chưa gắn SDK — cùng kiểu MediationAds:
// mọi chỗ gọi PurchaseController.Instance.Purchase(id); khi có SDK (Unity IAP…) viết class kế thừa, override
// Purchase / GetPriceText rồi gọi OnPurchased(id) lúc store xác nhận thanh toán.
// Base: trong Editor coi như thanh toán thành công ngay để test; trên máy thật thì báo store chưa sẵn sàng.
public class PurchaseController : Singleton<PurchaseController>
{
    public const string STORE_ID_NO_ADS = "lootio.noads";
    public const string STORE_ID_RESOURCE_PACK = "lootio.resourcepack";
    public const string STORE_ID_GEM1 = "lootio.gem1";
    public const string STORE_ID_GEM2 = "lootio.gem2";
    public const string STORE_ID_GEM3 = "lootio.gem3";
    public const string STORE_ID_GEM4 = "lootio.gem4";
    public const string STORE_ID_GEM5 = "lootio.gem5";
    public const string STORE_ID_GEM6 = "lootio.gem6";
    public const string STORE_ID_DUNGEON_KEYS = "lootio.dungeonkeys";
    public const string STORE_ID_LIFETIME_BOOST = "lootio.lifetimeboost";
    public const string STORE_ID_STARTER_PACK = "lootio.starterpack";
    public const string STORE_ID_EPIC_PACK = "lootio.epicpack";
    public const string STORE_ID_DEAL_TINY = "lootio.deal_tiny";
    public const string STORE_ID_DEAL_SMALL = "lootio.deal_small";
    public const string STORE_ID_DEAL_MEDIUM = "lootio.deal_medium";
    public const string STORE_ID_DEAL_LARGE = "lootio.deal_large";
    public const string STORE_ID_CHEST_BUNDLE_LEGENDARY = "lootio.chestbundle_legendary";
    public const string STORE_ID_CHEST_BUNDLE_MYTHIC = "lootio.chestbundle_mythic";

    public static readonly HashSet<string> DealProductIds = new HashSet<string>
    {
        STORE_ID_DEAL_TINY, STORE_ID_DEAL_SMALL, STORE_ID_DEAL_MEDIUM, STORE_ID_DEAL_LARGE,
    };

    public static readonly HashSet<string> ChestBundleProductIds = new HashSet<string>
    {
        STORE_ID_CHEST_BUNDLE_LEGENDARY, STORE_ID_CHEST_BUNDLE_MYTHIC,
    };

    public static Action<string> PurchaseSuccessful;
    public static Action PurchaseFailed;

    // Giá hiển thị trên nút mua. null = chưa có giá (UI hiện "N/A").
    public virtual string GetPriceText(string productId)
    {
        return StaticShopData.GetDefaultPrice(productId);
    }

    public virtual void Purchase(string productId)
    {
#if UNITY_EDITOR
        OnPurchased(productId);
#else
        UIManager.Instance.ShowToastMessage("Store is not available", isLocalize: false);
        PurchaseFailed?.Invoke();
#endif
    }

    // Store xác nhận đã thanh toán (dhk gốc): phát thưởng → lưu → báo UI; gói rương thì mở bảng kết quả.
    protected void OnPurchased(string productId)
    {
        List<ChestOutcome> outcomes = ShopService.GrantProduct(productId);
        GameData.Save(true);
        PurchaseSuccessful?.Invoke(productId);

        if (outcomes != null && outcomes.Count > 0)
        {
            UIChestBundleRevealPopup popup = UIManager.Instance.LoadUI(UIKey.ChestBundleRevealPopup) as UIChestBundleRevealPopup;
            if (popup != null) popup.Setup(outcomes);
        }
    }
}
