using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup Weapon Offer (OfferPopup gốc, reverse il2cpp jkg..jkl): thẻ vũ khí level 1, tên + dải màu theo rarity,
// "<rarity>", chỉ số chính, các dòng substat cố định, nút mua (giá sản phẩm lootio.offer_XXX).
// Gói hết hạn trong lúc mở → hiện lớp phủ hết hạn, khoá nút. Mua xong: vũ khí mặc thẳng vào hero.
public class UIOfferPopup : BaseUI
{
    public ItemElementView ItemElementUI;
    public Image RibbonImage;
    public TMP_Text WeaponNameText;
    public TMP_Text RarityText;
    public TMP_Text WeaponStatText;
    public TMP_Text SubStatsText;
    public Button BuyButton;
    public TMP_Text BuyButtonPriceText;
    public GameObject ExpiredOverlay;
    public Button btBackground;

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

        Color color = UITabWing.GetRarityColor(item.rarity);
        ItemElementUI.SetItem(item);
        WeaponNameText.text = ItemTextUtil.Name(item);
        if (RibbonImage != null) RibbonImage.color = color;
        RarityText.text = ItemTextUtil.RarityLine(item.rarity);
        WeaponStatText.text = ItemTextUtil.MainStat(item);
        string subStats = ItemTextUtil.SubStats(item.subStats);
        SubStatsText.gameObject.SetActive(subStats.Length > 0);
        SubStatsText.text = subStats;

        bool isExpired = WeaponOfferService.IsExpired();
        if (ExpiredOverlay != null) ExpiredOverlay.SetActive(isExpired);
        BuyButton.interactable = isExpired == false && isBuying == false;
        BuyButtonPriceText.text = PurchaseController.Instance.GetPriceText(WeaponOfferService.GetProductId()) ?? "N/A";
    }

    // jkk gốc.
    private void OnBuyClicked()
    {
        if (isBuying || WeaponOfferService.IsExpired())
            return;
        isBuying = true;
        BuyButton.interactable = false;
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
