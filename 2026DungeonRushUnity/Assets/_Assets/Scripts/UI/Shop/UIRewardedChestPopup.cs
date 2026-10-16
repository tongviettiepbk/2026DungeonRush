using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup rương quảng cáo (RewardedChestPopup gốc, reverse il2cpp ksc..ksh):
//  - Tiêu đề "Gem Offer" / "Loot Box Offer", số thưởng, nút "<sprite=0>Claim" (xem quảng cáo; đã mua No Ads thì
//    bỏ icon, bấm nhận luôn); quảng cáo chưa sẵn → "Loading..." khoá nút; hết lượt ngày → thời gian tới 0h UTC.
//  - Đóng (X / chạm nền) = bỏ qua.
public class UIRewardedChestPopup : BaseUI
{
    private const string AD_PLACEMENT = "rewarded_chest";

    public TMP_Text TitleText;
    public TMP_Text RewardText;
    public Image RewardImage;
    public Sprite LootboxRewardSprite;
    public Sprite GemRewardSprite;
    public Button AdButton;
    public Image AdButtonImage;
    public TMP_Text AdButtonText;
    public Button CloseButton;
    public Button btBackground;

    private RewardedChestType chestType;
    private int amount;
    private Action onClosed;
    private bool isWaitingAd;

    public static void Show(RewardedChestType type, int amount, Action onClosed)
    {
        UIRewardedChestPopup popup = UIManager.Instance.LoadUI(UIKey.RewardedChestPopup) as UIRewardedChestPopup;
        if (popup == null)
        {
            onClosed?.Invoke();
            return;
        }
        popup.Setup(type, amount, onClosed);
    }

    protected override void Awake()
    {
        base.Awake();
        AdButton.onClick.AddListener(OnRewardedButtonClicked);
        CloseButton.onClick.AddListener(Close);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
    }

    private void Setup(RewardedChestType type, int amount, Action onClosed)
    {
        chestType = type;
        this.amount = amount;
        this.onClosed = onClosed;
        isWaitingAd = false;
        Refresh();
    }

    // ksc gốc.
    private void Refresh()
    {
        bool isGem = chestType == RewardedChestType.Gem;
        RewardImage.sprite = isGem ? GemRewardSprite : LootboxRewardSprite;
        RewardText.text = amount.ToString();
        TitleText.text = isGem ? "Gem Offer" : "Loot Box Offer";

        if (ShopService.GetRewardedChestRemaining() <= 0)
        {
            AdButton.interactable = false;
            AdButtonImage.color = UITabWing.DISABLED_BUTTON_COLOR;
            TimeSpan left = DateTime.UtcNow.Date.AddDays(1) - DateTime.UtcNow;
            AdButtonText.text = ShopUIUtil.FormatDuration((long)left.TotalSeconds);
            return;
        }

        AdButton.interactable = isWaitingAd == false;
        AdButtonImage.color = UITabWing.DEFAULT_BUTTON_COLOR;
        AdButtonText.text = isWaitingAd ? "Loading..." : (GameData.userData.shop.isNoAdsBought ? "Claim" : "<sprite=0>Claim");
    }

    // OnRewardedButtonClicked gốc.
    private void OnRewardedButtonClicked()
    {
        if (isWaitingAd || ShopService.GetRewardedChestRemaining() <= 0)
            return;

        if (GameData.userData.shop.isNoAdsBought)
        {
            Claim();
            return;
        }

        isWaitingAd = true;
        Refresh();
        MediationAds.Instance.ShowRewardedVideoAd(AD_PLACEMENT, result =>
        {
            isWaitingAd = false;
            if (result == ShowResultADS.Finished)
                Claim();
            else
                Refresh();
        });
    }

    // ksh gốc.
    private void Claim()
    {
        ShopService.ClaimRewardedChest(chestType, amount);
        ShopUIUtil.RefreshLobby();
        Close();
    }

    public override void Close()
    {
        Action callback = onClosed;
        onClosed = null;
        base.Close();
        callback?.Invoke();
    }
}
