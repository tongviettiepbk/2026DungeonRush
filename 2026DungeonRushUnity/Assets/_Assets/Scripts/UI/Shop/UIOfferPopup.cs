using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup Weapon Offer (OfferPopup gốc, reverse il2cpp jkg..jkl): thẻ vũ khí level 1, tên tô màu rarity (ruy băng vàng cố định),
// "<rarity>", chỉ số chính, các dòng substat cố định, nút mua (giá sản phẩm tnt.offer_XXX).
// Gói hết hạn trong lúc mở → hiện lớp phủ hết hạn, khoá nút. Mua xong: vũ khí mặc thẳng vào hero.
public class UIOfferPopup : BaseUI
{
    public ItemElementView ItemElementUI;
    public ItemStatsView stats;              // reskin batch2: khối chữ món dùng chung (rarity, tên, chỉ số, substat cao cố định 3 dòng)
    public Button BuyButton;
    public Image BuyButtonImage;
    public Sprite BuySprite;                 // xanh = mua được
    public Sprite DisabledSprite;            // xám = gói hết hạn / đang mua
    public TMP_Text BuyButtonPriceText;
    public GameObject ExpiredOverlay;
    public Button btBackground;
    public TMP_Text txtTapToClose;

    private bool isBuying;

    public static void Show()
    {
        if (WeaponOfferService.IsOfferVisible() == false)
            return;
        UIOfferPopup popup = UIManager.Instance.LoadUI(UIKey.OfferPopup) as UIOfferPopup;
        if (popup != null)
            popup.Refresh();
    }

    protected override void Awake()
    {
        base.Awake();
        BuyButton.onClick.AddListener(OnBuyClicked);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed += OnPurchaseFailed;
    }

    private void OnDestroy()
    {
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed -= OnPurchaseFailed;
    }

    public override void Close()
    {
        AutoOfferGate.OnPopupClosed();
        base.Close();
    }

    private void Update()
    {
        // Gói hết hạn khi popup đang mở.
        if (ExpiredOverlay != null && ExpiredOverlay.activeSelf != WeaponOfferService.IsExpired())
            Refresh();
    }

    // jkg gốc.
    private void Refresh()
    {
        WeaponData weapon = WeaponOfferService.GetWeapon();
        if (weapon == null)
        {
            Close();
            return;
        }

        LootResult item = LootService.BuildFromRecord(GearSlotType.WEAPON, weapon.assetName, new EquippedItemData
        {
            equipId = weapon.assetName,
            rarity = weapon.rarity,
            level = 1,
            subStats = new List<GearSubStat>(WeaponOfferService.GetSubStats()),
        });
        if (item == null)
        {
            Close();
            return;
        }

        if (txtTapToClose != null) txtTapToClose.text = Loc.L("Common.TapToClose");
        ItemElementUI.SetItem(item);
        stats.Set(item);

        bool isExpired = WeaponOfferService.IsExpired();
        if (ExpiredOverlay != null) ExpiredOverlay.SetActive(isExpired);
        BuyButton.interactable = isExpired == false && isBuying == false;
        if (BuyButtonImage != null) BuyButtonImage.sprite = BuyButton.interactable ? BuySprite : DisabledSprite;
        BuyButtonPriceText.text = PurchaseController.Instance.GetPriceText(WeaponOfferService.GetProductId()) ?? "N/A";
    }

    // jkk gốc.
    private void OnBuyClicked()
    {
        if (isBuying || WeaponOfferService.IsExpired())
            return;
        isBuying = true;
        BuyButton.interactable = false;
        if (BuyButtonImage != null) BuyButtonImage.sprite = DisabledSprite;
        PurchaseController.Instance.Purchase(WeaponOfferService.GetProductId());
    }

    private void OnPurchaseSuccessful(string productId)
    {
        if (isBuying == false)
            return;
        isBuying = false;
        // jjh gốc: vũ khí đã mặc vào hero → hero + lobby vẽ lại, đóng popup.
        this.PostEvent(EventID.EquipmentChanged, GearSlotType.WEAPON);
        ShopUIUtil.RefreshLobby();
        Close();
    }

    private void OnPurchaseFailed()
    {
        isBuying = false;
        Refresh();
    }
}
