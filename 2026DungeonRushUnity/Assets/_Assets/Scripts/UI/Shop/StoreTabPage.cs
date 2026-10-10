using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang Store ở lobby (StoreTabPage gốc — tên field giữ nguyên để ShopPrefabBuilder nối tự động).
// Gồm: Lifetime Boost, rương, Daily Deals, Boosters, Resources, gói Gem. Số liệu: DecodedData/SHOP_MODEL.md.
public class StoreTabPage : MonoBehaviour
{
    public TextMeshProUGUI NoAdsPriceText;
    public TextMeshProUGUI ResourcePackPriceText;
    public TextMeshProUGUI Gem1PriceText;
    public TextMeshProUGUI Gem2PriceText;
    public TextMeshProUGUI Gem3PriceText;
    public TextMeshProUGUI Gem4PriceText;
    public TextMeshProUGUI Gem5PriceText;
    public TextMeshProUGUI Gem6PriceText;
    public TextMeshProUGUI DungeonKeysPriceText;
    public TextMeshProUGUI LifetimeBoostPriceText;
    public TextMeshProUGUI StarterPackPriceText;
    public TextMeshProUGUI EpicPackPriceText;
    public GameObject NoAdsParent;
    public GameObject OffersRibbonParent;
    public GameObject LifetimeBoostParent;
    public TextMeshProUGUI LifetimeBoostLabelText;
    public Button BuyDrillButton;
    public Button BuyGoldenPickaxeButton;
    public TextMeshProUGUI DrillCostText;
    public TextMeshProUGUI GoldenPickaxeCostText;
    public TextMeshProUGUI DrillOwnedCountText;
    public TextMeshProUGUI GoldenPickaxeOwnedCountText;
    public Button BuyX2ExpBoostButton;
    public Button BuyX5ExpBoostButton;
    public TextMeshProUGUI X2ExpBoostRemainingTimeText;
    public TextMeshProUGUI X5ExpBoostRemainingTimeText;
    public TextMeshProUGUI X2ExpBoostDurationText;
    public TextMeshProUGUI X5ExpBoostDurationText;
    public DailyDealSlotUI[] DailyDealSlots;
    public ChestStoreSection ChestStoreSection;

    private float nextTimerTick;

    private void Awake()
    {
        if (BuyDrillButton != null) BuyDrillButton.onClick.AddListener(OnBuyDrillClicked);
        if (BuyGoldenPickaxeButton != null) BuyGoldenPickaxeButton.onClick.AddListener(OnBuyGoldenPickaxeClicked);
        if (BuyX2ExpBoostButton != null) BuyX2ExpBoostButton.onClick.AddListener(OnBuyX2BoostClicked);
        if (BuyX5ExpBoostButton != null) BuyX5ExpBoostButton.onClick.AddListener(OnBuyX5BoostClicked);
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;
        DailyDealService.Changed += RefreshDailyDeals;
    }

    private void OnDestroy()
    {
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;
        DailyDealService.Changed -= RefreshDailyDeals;
    }

    // ejj gốc: mở tab.
    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTimerTick) return;
        nextTimerTick = Time.unscaledTime + 0.5f;
        RefreshBoosters();
    }

    // kts gốc.
    private void Refresh()
    {
        RefreshPrices();
        RefreshSections();
        RefreshResources();
        RefreshBoosters();
        RefreshDailyDeals();
    }

    // Nút mua bằng tiền thật trong prefab gọi hàm này kèm id sản phẩm.
    public void OnPurchaseButtonClicked(string purchaseId)
    {
        PurchaseController.Instance.Purchase(purchaseId);
    }

    private void OnPurchaseSuccessful(string productId)
    {
        Refresh();
        ShopUIUtil.RefreshLobby();
    }

    // ktv gốc: giá từng sản phẩm, chưa có giá thì "N/A".
    private void RefreshPrices()
    {
        SetPrice(NoAdsPriceText, PurchaseController.STORE_ID_NO_ADS);
        SetPrice(ResourcePackPriceText, PurchaseController.STORE_ID_RESOURCE_PACK);
        SetPrice(Gem1PriceText, PurchaseController.STORE_ID_GEM1);
        SetPrice(Gem2PriceText, PurchaseController.STORE_ID_GEM2);
        SetPrice(Gem3PriceText, PurchaseController.STORE_ID_GEM3);
        SetPrice(Gem4PriceText, PurchaseController.STORE_ID_GEM4);
        SetPrice(Gem5PriceText, PurchaseController.STORE_ID_GEM5);
        SetPrice(Gem6PriceText, PurchaseController.STORE_ID_GEM6);
        SetPrice(DungeonKeysPriceText, PurchaseController.STORE_ID_DUNGEON_KEYS);
        SetPrice(LifetimeBoostPriceText, PurchaseController.STORE_ID_LIFETIME_BOOST);
        SetPrice(StarterPackPriceText, PurchaseController.STORE_ID_STARTER_PACK);
        SetPrice(EpicPackPriceText, PurchaseController.STORE_ID_EPIC_PACK);
    }

    private static void SetPrice(TextMeshProUGUI text, string productId)
    {
        if (text == null) return;
        text.text = PurchaseController.Instance.GetPriceText(productId) ?? "N/A";
    }

    // ktw gốc: gói No Ads luôn ẩn; Lifetime Boost ẩn khi đã mua; mục rương chỉ mở khi đã có tính năng Cape.
    private void RefreshSections()
    {
        UserShopData user = GameData.userData.shop;
        if (NoAdsParent != null)
        {
            NoAdsParent.SetActive(false);
            if (OffersRibbonParent != null) OffersRibbonParent.SetActive(!user.isNoAdsBought);
        }
        if (LifetimeBoostParent != null) LifetimeBoostParent.SetActive(!user.isLifetimeBoostBought);
        if (ChestStoreSection != null) ChestStoreSection.Refresh(ShopService.IsChestStoreOpen());
    }

    // ktz gốc.
    private void RefreshResources()
    {
        UserItemData items = GameData.userData.items;
        ShopUIUtil.SetText(DrillCostText, StaticShopData.DRILL_GEM_COST.ToString());
        ShopUIUtil.SetText(GoldenPickaxeCostText, StaticShopData.GOLDEN_PICKAXE_GEM_COST.ToString());
        ShopUIUtil.SetText(DrillOwnedCountText, items.GetQuantityHave(ItemType.DRILL).ToString("0"));
        ShopUIUtil.SetText(GoldenPickaxeOwnedCountText, items.GetQuantityHave(ItemType.GOLDEN_PICKAXE).ToString("0"));
    }

    // kuf gốc: đang chạy boost → ẩn nút mua, hiện thời gian còn lại.
    private void RefreshBoosters()
    {
        bool isX2 = ShopService.IsX2BoostActive();
        bool isX5 = ShopService.IsX5BoostActive();
        ShopUIUtil.SetActive(BuyX2ExpBoostButton, !isX2);
        ShopUIUtil.SetActive(X2ExpBoostRemainingTimeText, isX2);
        ShopUIUtil.SetActive(BuyX5ExpBoostButton, !isX5);
        ShopUIUtil.SetActive(X5ExpBoostRemainingTimeText, isX5);
        if (isX2) ShopUIUtil.SetText(X2ExpBoostRemainingTimeText, ShopUIUtil.FormatDuration(ShopService.GetX2BoostRemaining()));
        if (isX5) ShopUIUtil.SetText(X5ExpBoostRemainingTimeText, ShopUIUtil.FormatDuration(ShopService.GetX5BoostRemaining()));
        ShopUIUtil.SetText(X2ExpBoostDurationText, ShopUIUtil.FormatDuration(StaticShopData.X2_BOOST_DURATION));
        ShopUIUtil.SetText(X5ExpBoostDurationText, ShopUIUtil.FormatDuration(StaticShopData.X5_BOOST_DURATION));
    }

    // ktu gốc.
    private void RefreshDailyDeals()
    {
        if (DailyDealSlots == null) return;
        DailyDealService.Refresh();
        for (int i = 0; i < DailyDealSlots.Length; i++)
        {
            if (DailyDealSlots[i] != null) DailyDealSlots[i].Refresh();
        }
    }

    // ktx gốc: 45 gem → +3 Drill.
    private void OnBuyDrillClicked()
    {
        OnGemPurchase(ShopService.BuyDrill());
        RefreshResources();
    }

    // kty gốc: 45 gem → +3 Golden Pickaxe.
    private void OnBuyGoldenPickaxeClicked()
    {
        OnGemPurchase(ShopService.BuyGoldenPickaxe());
        RefreshResources();
    }

    // kua gốc: 45 gem → boost exp ×1.5 trong 20 phút.
    private void OnBuyX2BoostClicked()
    {
        if (ShopService.IsX2BoostActive()) return;
        OnGemPurchase(ShopService.BuyX2Boost());
        RefreshBoosters();
    }

    // kub gốc: 75 gem → boost exp ×2 trong 5 phút.
    private void OnBuyX5BoostClicked()
    {
        if (ShopService.IsX5BoostActive()) return;
        OnGemPurchase(ShopService.BuyX5Boost());
        RefreshBoosters();
    }

    private static void OnGemPurchase(bool isSuccess)
    {
        if (isSuccess) ShopUIUtil.RefreshLobby();
        else ShopUIUtil.Toast(ShopUIUtil.NOT_ENOUGH_GEMS);
    }
}
