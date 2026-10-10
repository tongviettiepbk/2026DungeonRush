using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup chào bán gói rương bằng tiền thật (ChestBundleOfferPopup gốc, prefab Chest_Bundle_PopupOffer):
// "{rarity} Chest Bundle", các món trong gói (gem + từng loại rương), ảnh pet/cape tiêu biểu lắc lư, nút mua.
// Mở bằng UIChestBundleOfferPopup.Show(ShopService.GetFeaturedBundle()) — nút chào ở lobby (GameplayUI gốc) chưa có.
public class UIChestBundleOfferPopup : BaseUI
{
    public Image ChestIcon;
    public Image RibbonImage;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI CloakUnlockText;
    public ChestBundleOfferContentIcon ContentIconPrefab;
    public Transform ContentContainer;
    public Sprite GemIcon;
    public List<Image> ShowcaseImages;
    public float ShowcaseScaleAmount;
    public float ShowcaseRotateAngle;
    public Vector2 ShowcaseIdleDuration;
    public Button BuyButton;
    public TextMeshProUGUI BuyButtonPriceText;
    public Image LoadingImage;
    public Button CloseButton;

    private ChestBundleData bundle;

    public static void Show(ChestBundleData bundle)
    {
        if (bundle == null) return;
        UIChestBundleOfferPopup popup = UIManager.Instance.LoadUI(UIKey.ChestBundleOfferPopup) as UIChestBundleOfferPopup;
        if (popup != null) popup.Setup(bundle);
    }

    protected override void Awake()
    {
        base.Awake();
        if (BuyButton != null) BuyButton.onClick.AddListener(OnBuyClicked);
        if (CloseButton != null) CloseButton.onClick.AddListener(Close);
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;
    }

    private void OnDestroy()
    {
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;
    }

    // fls + eip gốc.
    public void Setup(ChestBundleData data)
    {
        bundle = data;
        ChestData top = GetTopChest(bundle);
        if (ChestIcon != null) ChestIcon.sprite = bundle.Icon;
        if (top != null)
        {
            ShopUIUtil.SetText(TitleText, string.Format("{0} Chest Bundle", top.Rarity));
            if (RibbonImage != null) RibbonImage.color = UITabWing.GetRarityColor(top.Rarity);
        }
        ShopUIUtil.SetActive(CloakUnlockText, !CapeService.IsUnlocked());
        ShopUIUtil.SetText(CloakUnlockText, "Unlocks the Cloak feature!");
        ShopUIUtil.SetText(BuyButtonPriceText, PurchaseController.Instance.GetPriceText(bundle.StoreId) ?? "N/A");
        if (LoadingImage != null) LoadingImage.gameObject.SetActive(false);

        FillContent();
        SetupShowcase();
    }

    // fly gốc: rương có rarity cao nhất trong gói đặt tên cho gói.
    private static ChestData GetTopChest(ChestBundleData data)
    {
        ChestData top = null;
        for (int i = 0; i < data.Chests.Count; i++)
        {
            ChestData chest = data.Chests[i].Chest;
            if (chest != null && (top == null || chest.Rarity > top.Rarity)) top = chest;
        }
        return top;
    }

    private void FillContent()
    {
        for (int i = ContentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(ContentContainer.GetChild(i).gameObject);
        }

        if (bundle.GemAmount > 0)
        {
            Instantiate(ContentIconPrefab, ContentContainer).Setup(GemIcon, "x" + ShopUIUtil.FormatAmount(bundle.GemAmount), null);
        }
        for (int i = 0; i < bundle.Chests.Count; i++)
        {
            ChestBundleEntry entry = bundle.Chests[i];
            if (entry == null || entry.Chest == null) continue;
            Instantiate(ContentIconPrefab, ContentContainer).Setup(entry.Chest.Icon, "x" + entry.Count, entry.Chest);
        }
    }

    // Ảnh tiêu biểu phóng nhẹ + nghiêng qua lại, mỗi ảnh 1 nhịp ngẫu nhiên trong ShowcaseIdleDuration.
    private void SetupShowcase()
    {
        if (ShowcaseImages == null) return;
        for (int i = 0; i < ShowcaseImages.Count; i++)
        {
            Image image = ShowcaseImages[i];
            if (image == null) continue;
            bool has = i < bundle.ShowcaseSprites.Count && bundle.ShowcaseSprites[i] != null;
            image.gameObject.SetActive(has);
            if (!has) continue;

            image.sprite = bundle.ShowcaseSprites[i];
            Transform t = image.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            float duration = Random.Range(ShowcaseIdleDuration.x, ShowcaseIdleDuration.y);
            t.DOScale(1f + ShowcaseScaleAmount, duration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            t.DOLocalRotate(new Vector3(0f, 0f, ShowcaseRotateAngle), duration).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }
    }

    // flt gốc.
    private void OnBuyClicked()
    {
        if (bundle != null) PurchaseController.Instance.Purchase(bundle.StoreId);
    }

    private void OnPurchaseSuccessful(string productId)
    {
        if (bundle != null && productId == bundle.StoreId && gameObject.activeInHierarchy) Close();
    }

    public override void Close()
    {
        if (ShowcaseImages != null)
        {
            for (int i = 0; i < ShowcaseImages.Count; i++)
            {
                if (ShowcaseImages[i] != null) ShowcaseImages[i].transform.DOKill();
            }
        }
        base.Close();
    }
}
