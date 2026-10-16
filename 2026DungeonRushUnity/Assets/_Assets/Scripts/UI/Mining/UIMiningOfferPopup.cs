using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gói chào Mining Offer (MiningOfferPopup gốc): gói kế tiếp theo số gói đã mua (2 gói: thường / lớn).
// Hiện khoan / cuốc vàng / cuốc / gem + "Increase Maximum Pickaxe Storage +N"; mua qua PurchaseController.
// Bấm nền tối để đóng (bản rip tắt sẵn nút X).
public class UIMiningOfferPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtDrill;
    public TMP_Text txtGoldenPickaxe;
    public TMP_Text txtPickaxe;
    public GameObject objGem;
    public TMP_Text txtGem;
    public GameObject objMaxPickaxe;
    public TMP_Text txtMaxPickaxe;
    public Button btBuy;
    public TMP_Text txtPrice;
    public Image imgLoading;
    public Button btBackground;

    private MiningOfferTier tier;
    private Tween loadingTween;

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
        SetLoading(false);
    }

    // eip/fop
    public void Setup()
    {
        tier = MiningService.GetNextOffer();
        if (tier == null)
        {
            Close();
            return;
        }

        txtTitle.text = tier.TitleKey == "UI.Mining.OfferTitleLarge" ? "Large Mining Offer" : "Mining Offer";
        txtDrill.text = "x" + tier.Drill;
        txtGoldenPickaxe.text = "x" + tier.GoldenPickaxe;
        txtPickaxe.text = "x" + tier.Pickaxe;
        if (objGem != null) objGem.SetActive(tier.Gem > 0);
        if (txtGem != null) txtGem.text = "x" + tier.Gem;
        if (objMaxPickaxe != null) objMaxPickaxe.SetActive(tier.MaxPickaxeIncrease > 0);
        if (txtMaxPickaxe != null) txtMaxPickaxe.text = "Increase Maximum Pickaxe Storage +" + tier.MaxPickaxeIncrease;

        string price = PurchaseController.Instance.GetPriceText(tier.StoreId);
        txtPrice.text = string.IsNullOrEmpty(price) ? "N/A" : price;
        SetLoading(false);
    }

    private void OnClickBuy()
    {
        if (tier == null)
        {
            return;
        }
        SetLoading(true);
        PurchaseController.Instance.Purchase(tier.StoreId);
    }

    private void OnPurchaseSuccessful(string productId)
    {
        if (tier != null && productId == tier.StoreId)
        {
            Close();
        }
    }

    private void OnPurchaseFailed()
    {
        SetLoading(false);
    }

    // fov: vòng xoay chờ store.
    private void SetLoading(bool loading)
    {
        if (btBuy != null) btBuy.interactable = !loading;
        if (imgLoading == null)
        {
            return;
        }

        imgLoading.gameObject.SetActive(loading);
        if (loadingTween != null)
        {
            loadingTween.Kill();
            loadingTween = null;
        }
        if (loading)
        {
            loadingTween = imgLoading.transform.DORotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
                .SetLoops(-1).SetEase(Ease.Linear);
        }
    }
}
