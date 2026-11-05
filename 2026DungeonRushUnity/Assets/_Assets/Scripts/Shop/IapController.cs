using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppsFlyerSDK;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

// Lớp SDK của PurchaseController: Unity IAP v5 (kết quả về qua sự kiện của IStoreService/IProductService/IPurchaseService).
//
// Khởi tạo: UnityServices (thiếu cũng không chặn) → Connect() cửa hàng → FetchProducts → hỏng bước nào thì thử lại.
// Mua: Purchase → cửa hàng thu tiền → OnPurchasePending → xác minh server (Android) → trao hàng + ghi save → ConfirmPurchase.
// Không xác nhận thì đơn treo, được phát lại ở lần FetchPurchases sau; Google Play tự hoàn tiền sau 3 ngày nếu vẫn không xác nhận.
//
// Đơn phát lại (app chết giữa lúc mua) trao theo mã sản phẩm nên không mất hàng. grantedTransactionIds chặn trao 2 lần
// khi đã trao xong nhưng chưa kịp khép đơn. Sổ này ghi xuống máy TRƯỚC ConfirmPurchase, vì khép đơn là lệnh bắn đi rồi thôi.
//
// Xác minh server (verifypurchase, server/functions/src/iap.ts): chỉ trên Android, trao hàng theo kết quả server; không hỏi được
// thì để đơn treo, lần sau phát lại. iOS chưa có đường xác minh (App Store Server API), trao theo cửa hàng báo về.
// Editor không mua thật: Purchase + GetPriceText giữ hành vi base (mua được ngay, giá mặc định) để test.
public class IapController : PurchaseController, IAppsFlyerValidateAndLog
{
    private const int MAX_INIT_RETRIES = 3;
    private const float INIT_RETRY_DELAY = 5f;
    private const string MSG_STORE_NOT_AVAILABLE = "Store is not available";
    private const string MSG_PURCHASE_FAILED = "Purchase failed";

    private const string FN_VERIFY_PURCHASE = "verifypurchase";
    private const string VERIFY_OK = "ok";
    private const string VERIFY_DUPLICATE = "duplicate";
    private const string VERIFY_PENDING = "pending";

    // Mua 1 lần, quyền theo tài khoản cửa hàng. Còn lại là Consumable.
    private static readonly HashSet<string> NonConsumableIds = new HashSet<string>
    {
        STORE_ID_NO_ADS, STORE_ID_BATTLE_PASS, STORE_ID_LIFETIME_BOOST,
    };

    private IStoreService storeService;
    private IProductService productService;
    private IPurchaseService purchaseService;
    private readonly CatalogProvider catalogProvider = new CatalogProvider();

    private bool storeConnected;
    private bool productsFetched;
    private bool isInitializing;
    private int initRetryCount;

    // Đơn đang chờ server: FetchPurchases có thể phát lại đúng đơn này trong lúc chờ.
    private readonly HashSet<string> verifyingTokens = new HashSet<string>();

    // Trao hàng cần UI và save của tài khoản hiện tại, cả 2 chỉ chắc chắn có ở MainGame.
    private static bool IsInGame => SceneManager.GetActiveScene().name == StaticValue.SCENE_MAINGAME;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        CreateServices();
        BuildCatalog();
        SceneManager.sceneLoaded += OnSceneLoaded;
        _ = InitializeAsync();
    }

    #region Khởi tạo
    private void CreateServices()
    {
        storeService = UnityIAPServices.DefaultStore();
        productService = UnityIAPServices.DefaultProduct();
        purchaseService = UnityIAPServices.DefaultPurchase();

        storeService.OnStoreConnected += OnStoreConnected;
        storeService.OnStoreDisconnected += OnStoreDisconnected;
        storeService.OnAuthAccountChanged += OnAuthAccountChanged;

        productService.OnProductsFetched += OnProductsFetched;
        productService.OnProductsFetchFailed += OnProductsFetchFailed;

        purchaseService.OnPurchasesFetched += OnPurchasesFetched;
        purchaseService.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        purchaseService.OnPurchasePending += OnPurchasePending;
        purchaseService.OnPurchaseConfirmed += OnPurchaseConfirmed;
        purchaseService.OnPurchaseFailed += OnPurchaseFailed;
        purchaseService.OnPurchaseDeferred += OnPurchaseDeferred;
    }

    // Đúng các mã ShopService.GrantProduct phát được thưởng, để không bán món nào thu tiền mà không trao.
    private void BuildCatalog()
    {
        List<string> ids = new List<string>
        {
            STORE_ID_NO_ADS, STORE_ID_BATTLE_PASS, STORE_ID_LIFETIME_BOOST, STORE_ID_RESOURCE_PACK,
            STORE_ID_GEM1, STORE_ID_GEM2, STORE_ID_GEM3, STORE_ID_GEM4, STORE_ID_GEM5, STORE_ID_GEM6,
            STORE_ID_DUNGEON_KEYS, STORE_ID_STARTER_PACK, STORE_ID_EPIC_PACK,
        };
        ids.AddRange(DealProductIds);
        ids.AddRange(ChestBundleProductIds);
        ids.AddRange(WeaponOfferService.AllProductIds);

        foreach (MiningOfferTier tier in MiningService.Config.MiningOfferTiers)
        {
            ids.Add(tier.StoreId);
        }

        List<ProductDefinition> definitions = new List<ProductDefinition>(ids.Count);
        foreach (string id in ids)
        {
            definitions.Add(new ProductDefinition(id, NonConsumableIds.Contains(id) ? ProductType.NonConsumable : ProductType.Consumable));
        }

        catalogProvider.AddProducts(definitions, null);
    }

    private async Task InitializeAsync()
    {
        if (productsFetched || isInitializing)
        {
            return;
        }

        isInitializing = true;

        // Cửa hàng Google/Apple không bắt buộc Unity Gaming Services; project chưa gắn Cloud Project thì hàm này ném lỗi.
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }
        }
        catch (Exception e)
        {
            DebugCustom.Log("[IAP] UnityServices init failed, continue without it: " + e.Message);
        }

        // Connect() luôn hoàn thành kể cả khi hỏng, kết quả thật báo qua OnStoreConnected/OnStoreDisconnected.
        if (storeConnected == false)
        {
            await storeService.Connect();
        }

        if (storeConnected == false)
        {
            isInitializing = false;
            RetryInitialize();
            return;
        }

        FetchProducts();
    }

    private void FetchProducts()
    {
        catalogProvider.FetchProducts(productService.FetchProductsWithNoRetries, DefaultStoreHelper.GetDefaultStoreName());
    }

    private void RetryInitialize()
    {
        if (initRetryCount >= MAX_INIT_RETRIES)
        {
            DebugCustom.Log("[IAP] Init failed after " + MAX_INIT_RETRIES + " retries.");
            return;
        }

        initRetryCount++;
        Invoke(nameof(RunInitialize), INIT_RETRY_DELAY);
    }

    private void RunInitialize()
    {
        _ = InitializeAsync();
    }

    private void OnStoreConnected()
    {
        storeConnected = true;
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        storeConnected = false;
        DebugCustom.Log("[IAP] Store disconnected: " + failure.Message);
    }

    // Unity IAP xoá kho sản phẩm và đơn trước khi bắn sự kiện này nên phải nạp lại từ đầu.
    private void OnAuthAccountChanged()
    {
        productsFetched = false;
        FetchProducts();
    }

    private void OnProductsFetched(List<Product> products)
    {
        isInitializing = false;
        initRetryCount = 0;
        productsFetched = true;
        DebugCustom.Log("[IAP] Fetched " + products.Count + " products");

        if (IsInGame)
        {
            purchaseService.FetchPurchases();
        }
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        DebugCustom.Log("[IAP] Fetch products failed: " + failure.FailureReason);
        isInitializing = false;
        RetryInitialize();
    }

    // Mỗi lần vào MainGame hỏi lại đơn: phát lại đơn treo + trả quyền mua 1 lần cho save vừa đổi (cài lại máy, đổi tài khoản).
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == StaticValue.SCENE_MAINGAME && productsFetched)
        {
            purchaseService.FetchPurchases();
        }
    }

    private void OnPurchasesFetched(Orders orders)
    {
        bool isRestored = false;

        foreach (ConfirmedOrder order in orders.ConfirmedOrders)
        {
            Product product = FirstProduct(order);

            if (product != null && product.definition.type == ProductType.NonConsumable && IsOwned(product.definition.id) == false)
            {
                DebugCustom.Log("[IAP] Restored: " + product.definition.id);
                ShopService.GrantProduct(product.definition.id);
                isRestored = true;
            }
        }

        if (isRestored)
        {
            GameData.Save(true);
        }
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        DebugCustom.Log("[IAP] Fetch purchases failed: " + failure.Message);
    }

    private static bool IsOwned(string productId)
    {
        UserShopData shop = GameData.userData.shop;

        switch (productId)
        {
            case STORE_ID_NO_ADS: return shop.isNoAdsBought;
            case STORE_ID_BATTLE_PASS: return shop.isPremiumBattlePassBought;
            case STORE_ID_LIFETIME_BOOST: return shop.isLifetimeBoostBought;
            default: return false;
        }
    }
    #endregion

    #region Mua
    public override string GetPriceText(string productId)
    {
        Product product = Application.isEditor ? null : GetProduct(productId);
        return product != null ? product.metadata.localizedPriceString : base.GetPriceText(productId);
    }

    public override void Purchase(string productId)
    {
        if (Application.isEditor)
        {
            base.Purchase(productId);
            return;
        }

        Analytics.LogIapClick(productId);
        Product product = GetProduct(productId);

        if (product == null || product.availableToPurchase == false)
        {
            DebugCustom.LogError("[IAP] Product not available: " + productId);
            Analytics.LogIapFail(productId, "not_available");
            UIManager.Instance.ShowToastMessage(MSG_STORE_NOT_AVAILABLE, isLocalize: false);
            PurchaseFailed?.Invoke();

            // Khởi tạo đã bỏ cuộc (mở app lúc mất mạng): thử lại để lần bấm sau mua được mà không phải mở lại app.
            if (productsFetched == false && initRetryCount >= MAX_INIT_RETRIES)
            {
                initRetryCount = 0;
                RunInitialize();
            }
            return;
        }

        UIManager.Instance.ShowWaiting(true);
        IsPurchasing = true;
        purchaseService.PurchaseProduct(product);
    }

    private Product GetProduct(string productId)
    {
        if (productsFetched == false)
        {
            return null;
        }

        foreach (Product product in productService.GetProducts())
        {
            if (product.definition.id == productId)
            {
                return product;
            }
        }

        return null;
    }

    // Unity IAP gọi thẳng, không bọc try: lỗi ở đây phải bắt và để đơn treo, không được khép đơn khi chưa trao hàng.
    private void OnPurchasePending(PendingOrder order)
    {
        try
        {
            ProcessPendingOrder(order);
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[IAP] Grant failed, order left pending: " + e);
            FailOrder();
        }
    }

    private void FailOrder()
    {
        Analytics.LogIapFail(string.Empty, "verify_failed");
        UIManager.Instance.ShowWaiting(false);
        UIManager.Instance.ShowToastMessage(MSG_PURCHASE_FAILED, isLocalize: false);
        PurchaseFailed?.Invoke();
    }

    private void ProcessPendingOrder(PendingOrder order)
    {
        // Đơn về tới đây thì hộp thoại thanh toán đã đóng.
        IsPurchasing = false;

        // Chưa vào game: để đơn treo, OnSceneLoaded sẽ hỏi lại và đơn được phát lại.
        if (IsInGame == false)
        {
            return;
        }

        Product product = FirstProduct(order);

        if (product == null)
        {
            DebugCustom.LogError("[IAP] Pending order without product, left pending");
            return;
        }

        string transactionId = order.Info != null ? order.Info.TransactionID : null;

        if (string.IsNullOrEmpty(transactionId) == false && GameData.userData.shop.grantedTransactionIds.Contains(transactionId))
        {
            // Đã trao rồi, chỉ là chưa khép được đơn. Không khép thì cửa hàng khoá luôn SKU này.
            purchaseService.ConfirmPurchase(order);
            UIManager.Instance.ShowWaiting(false);
            return;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            VerifyThenGrant(order, product);
        }
        else
        {
            GrantOrder(order, product);
        }
    }

    // Không có token thì coi như biên lai hỏng: không trao, không khép, Google tự hoàn tiền sau 3 ngày nếu đơn thật.
    private void VerifyThenGrant(PendingOrder order, Product product)
    {
        string productId = product.definition.id;
        string token = order.Info != null ? AppsflyerHelper.ExtractGooglePurchaseToken(order.Info.Receipt) : null;

        if (string.IsNullOrEmpty(token))
        {
            DebugCustom.LogError("[IAP] No purchase token, order left pending: " + productId);
            FailOrder();
            return;
        }

        if (verifyingTokens.Add(token) == false)
        {
            return;
        }

        // Vòng chờ lúc bấm mua có hạn 20 giây, có thể đã hết: bật lại không hạn, mọi nhánh sau đều tắt.
        UIManager.Instance.ShowWaiting(true, isTimeOut: false);

        object request = new { packageName = Application.identifier, productId, purchaseToken = token };
        FirebaseManager.Instance.Call<IapVerifyResponseDTO>(FN_VERIFY_PURCHASE, request, (ok, res, raw) =>
        {
            verifyingTokens.Remove(token);

            try
            {
                OnVerified(order, product, ok ? res?.status : null);
            }
            catch (Exception e)
            {
                DebugCustom.LogError("[IAP] Grant failed, order left pending: " + e);
                FailOrder();
            }
        });
    }

    private void OnVerified(PendingOrder order, Product product, string status)
    {
        string productId = product.definition.id;
        DebugCustom.Log("[IAP] verifypurchase " + productId + ": " + status);

        // Rời MainGame trong lúc chờ: để đơn treo, server cho hỏi lại khi đơn chưa khép.
        if (IsInGame == false)
        {
            UIManager.Instance.ShowWaiting(false);
            return;
        }

        switch (status)
        {
            case VERIFY_OK:
                GrantOrder(order, product);
                break;

            case VERIFY_DUPLICATE:
                // Biên lai đã đổi hàng rồi: khép đơn để mở khoá SKU, không trao.
                purchaseService.ConfirmPurchase(order);
                UIManager.Instance.ShowWaiting(false);
                PurchaseFailed?.Invoke();
                break;

            case VERIFY_PENDING:
                // Google chưa thu được tiền: đơn quay lại sau.
                UIManager.Instance.ShowWaiting(false);
                PurchaseFailed?.Invoke();
                break;

            default:
                // invalid, hoặc không hỏi được server (null, error): không trao, không khép.
                FailOrder();
                break;
        }
    }

    private void GrantOrder(PendingOrder order, Product product)
    {
        string productId = product.definition.id;
        string transactionId = order.Info != null ? order.Info.TransactionID : null;
        List<string> granted = GameData.userData.shop.grantedTransactionIds;
        bool hasTransactionId = string.IsNullOrEmpty(transactionId) == false;

        if (hasTransactionId)
        {
            granted.Add(transactionId);
            if (granted.Count > UserShopData.GRANTED_TRANSACTIONS_MAX)
            {
                granted.RemoveAt(0);
            }
        }

        try
        {
            // Ghi cả sổ giao dịch xuống máy trong GameData.Save(true) của OnPurchased.
            OnPurchased(productId, (double)product.metadata.localizedPrice, product.metadata.isoCurrencyCode, transactionId);
        }
        catch
        {
            if (hasTransactionId)
            {
                granted.Remove(transactionId);
            }
            throw;
        }

        AppsflyerHelper.ValidateAndLogPurchase(product, order, this);

        purchaseService.ConfirmPurchase(order);
        UIManager.Instance.ShowWaiting(false);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failed)
        {
            // Đã trao hàng mà không khép được đơn: đơn phát lại lần sau, sổ giao dịch chặn trao lần 2.
            DebugCustom.LogError("[IAP] Order confirmation failed: " + failed.FailureReason + " " + failed.Details);
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        DebugCustom.Log("[IAP] Purchase failed: " + order.FailureReason + " " + order.Details);
        IsPurchasing = false;
        Product failedProduct = FirstProduct(order);
        Analytics.LogIapFail(failedProduct != null ? failedProduct.definition.id : string.Empty, order.FailureReason.ToString().ToLowerInvariant());

        // Tự huỷ thì không báo lỗi: người chơi biết mình vừa bấm gì.
        if (order.FailureReason != PurchaseFailureReason.UserCancelled)
        {
            UIManager.Instance.ShowToastMessage(MSG_PURCHASE_FAILED, isLocalize: false);
        }

        UIManager.Instance.ShowWaiting(false);
        PurchaseFailed?.Invoke();
    }

    // Chờ người khác duyệt (Ask to Buy): chưa thu tiền. Được duyệt thì đơn quay lại qua OnPurchasePending.
    private void OnPurchaseDeferred(DeferredOrder order)
    {
        IsPurchasing = false;
        UIManager.Instance.ShowWaiting(false);
        PurchaseFailed?.Invoke();
    }

    private static Product FirstProduct(Order order)
    {
        IReadOnlyList<CartItem> items = order?.CartOrdered?.Items();
        return items != null && items.Count > 0 ? items[0].Product : null;
    }
    #endregion

    #region AppsFlyer
    public void onValidateAndLogComplete(string result)
    {
        DebugCustom.Log("[AppsFlyer] validateAndLog OK: " + result);
    }

    public void onValidateAndLogFailure(string error)
    {
        DebugCustom.LogError("[AppsFlyer] validateAndLog failed: " + error);
    }
    #endregion
}

public class IapVerifyResponseDTO
{
    public string status;
    public string reason;
    public string orderId;
}
