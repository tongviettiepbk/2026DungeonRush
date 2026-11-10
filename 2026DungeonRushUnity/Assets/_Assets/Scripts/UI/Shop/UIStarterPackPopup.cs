using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup Starter Pack: dựng từ khung UIMiningOfferPopup (RetentionUIBuilder). Mua qua PurchaseController.
public class UIStarterPackPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtDesc;
    public TMP_Text txtGem;
    public TMP_Text txtHammer;
    public Button btBuy;
    public TMP_Text txtPrice;
    public Button btBackground;
    public TMP_Text txtTapToClose;

    private long lastSecondsLeft = -1;

    public static void Show()
    {
        if (StarterPackService.IsVisible == false)
            return;

        UIStarterPackPopup popup = UIManager.Instance.LoadUI(UIKey.StarterPackPopup) as UIStarterPackPopup;
        if (popup != null)
            popup.Refresh();
    }

    protected override void Awake()
    {
        base.Awake();
        btBuy.onClick.AddListener(OnClickBuy);
        if (btBackground != null) btBackground.onClick.AddListener(Close);
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

    private void Update()
    {
        long left = StarterPackService.GetSecondsLeft();
        if (left == lastSecondsLeft)
            return;

        lastSecondsLeft = left;
        txtDesc.text = Loc.L("Popup.StarterPack.Desc", ShopUIUtil.FormatDuration(left));
        if (left <= 0)
            Close();
    }

    private void Refresh()
    {
        ShopReward[] rewards = StaticShopData.GetProductRewards(PurchaseController.STORE_ID_STARTER_PACK);
        txtTitle.text = Loc.L("Popup.StarterPack.Title");
        if (txtTapToClose != null) txtTapToClose.text = Loc.L("Common.TapToClose");
        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i].type == ShopRewardType.Gem) txtGem.text = "x" + rewards[i].amount;
            else if (rewards[i].type == ShopRewardType.LootBox) txtHammer.text = "x" + rewards[i].amount;
        }

        txtPrice.text = PurchaseController.Instance.GetPriceText(PurchaseController.STORE_ID_STARTER_PACK) ?? "N/A";
        btBuy.interactable = true;
    }

    private void OnClickBuy()
    {
        btBuy.interactable = false;
        PurchaseController.Instance.Purchase(PurchaseController.STORE_ID_STARTER_PACK);
    }

    private void OnPurchaseSuccessful(string productId)
    {
        if (productId == PurchaseController.STORE_ID_STARTER_PACK)
            Close();
    }

    private void OnPurchaseFailed()
    {
        btBuy.interactable = true;
    }

    public override void Close()
    {
        AutoOfferGate.OnPopupClosed();
        base.Close();
    }
}
