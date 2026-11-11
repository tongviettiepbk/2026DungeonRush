using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thiếu gem thì chào gói gem nhỏ nhất đủ mua món vừa bấm, thay cho toast. LỆCH GỐC có chủ ý, user duyệt 2026-11-05.
// Dựng từ khung UIMiningOfferPopup (RetentionUIBuilder).
public class UIMissingGemsPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtDesc;
    public TMP_Text txtGem;
    public Button btBuy;
    public TMP_Text txtPrice;
    public Button btBackground;
    public Button btClose;           // reskin batch2: X trong header (trước chỉ chạm nền mới đóng)

    private string productId;

    // sink: chỗ tiêu gem cho Analytics; cost: giá món vừa bấm.
    public static void Show(string sink, int cost)
    {
        int missing = Mathf.Max(1, cost - ShopService.GetGem());
        Analytics.LogGemShort(sink);

        Analytics.PaywallTrigger = Analytics.TRIGGER_MISSING_GEMS;
        UIMissingGemsPopup popup = UIManager.Instance.LoadUI(UIKey.MissingGemsPopup) as UIMissingGemsPopup;
        Analytics.PaywallTrigger = Analytics.TRIGGER_MANUAL;

        if (popup != null)
            popup.Setup(missing);
    }

    protected override void Awake()
    {
        base.Awake();
        btBuy.onClick.AddListener(OnClickBuy);
        if (btBackground != null) btBackground.onClick.AddListener(Close);
        if (btClose != null) btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed += OnPurchaseFailed;
    }

    private void OnDisable()
    {
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed -= OnPurchaseFailed;
    }

    private void Setup(int missing)
    {
        productId = StaticShopData.GetGemPackFor(missing);
        txtTitle.text = Loc.L("Errors.NotEnoughGems");
        txtDesc.text = Loc.L("Popup.MissingGems.Desc", missing);
        txtGem.text = "x" + StaticShopData.GetProductRewards(productId)[0].amount;
        txtPrice.text = PurchaseController.Instance.GetPriceText(productId) ?? "N/A";
        btBuy.interactable = true;
    }

    private void OnClickBuy()
    {
        btBuy.interactable = false;
        PurchaseController.Instance.Purchase(productId);
    }

    private void OnPurchaseSuccessful(string purchased)
    {
        if (purchased == productId)
            Close();
    }

    private void OnPurchaseFailed()
    {
        btBuy.interactable = true;
    }
}
