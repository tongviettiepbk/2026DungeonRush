using System;
using System.Collections.Generic;

// Lớp mua hàng bằng tiền thật (PurchaseController gốc). Đây là lớp BASE chưa gắn SDK — cùng kiểu MediationAds:
// mọi chỗ gọi PurchaseController.Instance.Purchase(id); khi có SDK (Unity IAP…) viết class kế thừa, override
// Purchase / GetPriceText rồi gọi OnPurchased(id) lúc store xác nhận thanh toán.
// Base: trong Editor coi như thanh toán thành công ngay để test; trên máy thật thì báo store chưa sẵn sàng.
public class PurchaseController : Singleton<PurchaseController>
{
    public const string STORE_ID_NO_ADS = "tnt.noads";
    public const string STORE_ID_BATTLE_PASS = "tnt.battlepass";
    public const string STORE_ID_RESOURCE_PACK = "tnt.resourcepack";
    public const string STORE_ID_GEM1 = "tnt.gem1";
    public const string STORE_ID_GEM2 = "tnt.gem2";
    public const string STORE_ID_GEM3 = "tnt.gem3";
    public const string STORE_ID_GEM4 = "tnt.gem4";
    public const string STORE_ID_GEM5 = "tnt.gem5";
    public const string STORE_ID_GEM6 = "tnt.gem6";
    public const string STORE_ID_DUNGEON_KEYS = "tnt.dungeonkeys";
    public const string STORE_ID_LIFETIME_BOOST = "tnt.lifetimeboost";
    public const string STORE_ID_STARTER_PACK = "tnt.starterpack";
    public const string STORE_ID_EPIC_PACK = "tnt.epicpack";
    public const string STORE_ID_DEAL_TINY = "tnt.deal_tiny";
    public const string STORE_ID_DEAL_SMALL = "tnt.deal_small";
    public const string STORE_ID_DEAL_MEDIUM = "tnt.deal_medium";
    public const string STORE_ID_DEAL_LARGE = "tnt.deal_large";
    public const string STORE_ID_CHEST_BUNDLE_LEGENDARY = "tnt.chestbundle_legendary";
    public const string STORE_ID_CHEST_BUNDLE_MYTHIC = "tnt.chestbundle_mythic";

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

        // Hàng đã trao và đã lưu: lỗi UI từ đây không được coi là trao hỏng, không thì đơn phát lại sẽ trao lần 2.
        try
        {
            PurchaseSuccessful?.Invoke(productId);

            if (outcomes != null && outcomes.Count > 0)
            {
                UIChestBundleRevealPopup popup = UIManager.Instance.LoadUI(UIKey.ChestBundleRevealPopup) as UIChestBundleRevealPopup;
                if (popup != null) popup.Setup(outcomes);
            }
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[PurchaseController] UI error after grant: " + e);
        }
    }
}
