using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup Battle Pass (BattlePassPopup gốc): cột giữa là mốc level (màn 20, 25, …), trái hàng Free, phải hàng Premium.
// Hiện tới mốc đã đạt + FutureRewardCount mốc tương lai; thanh fill tới mốc đã đạt; mở lên cuộn tới mốc đầu còn nhận được.
// Chưa mua Premium thì hiện nút mua (lootio.battlepass).
public class UIBattlePassPopup : BaseUI
{
    // Giá trị serialized của prefab gốc.
    public float LevelElementTopPadding = 100f;
    public float LevelElementSpacing = 80f;
    public float RewardElementHeight = 150f;
    public Color RewardClaimedColor = new Color(0.6981132f, 0.6981132f, 0.6981132f, 1f);
    public Color RewardNotReachedColor = new Color(0.16862746f, 0.16862746f, 0.16862746f, 1f);
    public Color RewardClaimableColor = new Color(0.078431375f, 0.62352943f, 0.9921569f, 1f);
    public Color LevelNotReachedColor = new Color(0.16862746f, 0.16862746f, 0.16862746f, 1f);

    public ScrollRect ScrollRect;
    public RectTransform LevelsTransform;          // cột mốc giữa
    public RectTransform FreeRewardsTransform;     // cột Free (trái)
    public RectTransform PassRewardsTransform;     // cột Premium (phải)
    public RectTransform FillBarRectTransform;
    public RectTransform FillBarParentRectTransform;
    public GameObject BuyBattlePassButton;
    public Button btBuy;
    public TMP_Text BattlePassPriceText;
    public BattlePassRewardCellView FreeRewardPrefab;
    public BattlePassRewardCellView PremiumRewardPrefab;
    public BattlePassLevelView LevelElementPrefab;
    public Button btClose;
    public Button btBackground;
    // Icon "<sprite=0>" theo BattlePassRewardType (index = giá trị enum); null = không icon.
    public TMP_SpriteAsset[] rewardSpriteAssets;

    private readonly List<BattlePassRewardCellView> cells = new List<BattlePassRewardCellView>();
    private readonly List<BattlePassLevelView> levels = new List<BattlePassLevelView>();
    private bool isBuying;

    public static void Show()
    {
        if (BattlePassService.IsUnlocked() == false)
            return;
        UIBattlePassPopup popup = UIManager.Instance.LoadUI(UIKey.BattlePassPopup) as UIBattlePassPopup;
        if (popup != null)
            popup.Build();
    }

    protected override void Awake()
    {
        base.Awake();
        if (btBuy != null) btBuy.onClick.AddListener(OnBuyClicked);
        if (btClose != null) btClose.onClick.AddListener(Close);
        if (btBackground != null) btBackground.onClick.AddListener(Close);
        PurchaseController.PurchaseSuccessful += OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed += OnPurchaseFailed;
        FreeRewardPrefab.gameObject.SetActive(false);
        PremiumRewardPrefab.gameObject.SetActive(false);
        LevelElementPrefab.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        PurchaseController.PurchaseSuccessful -= OnPurchaseSuccessful;
        PurchaseController.PurchaseFailed -= OnPurchaseFailed;
    }

    // jug gốc.
    private void Build()
    {
        int last = BattlePassService.GetLastShownLevel();
        int count = (last - BattlePassService.START_LEVEL) / BattlePassService.REWARD_INTERVAL + 1;
        float rowHeight = RewardElementHeight + LevelElementSpacing;

        for (int i = 0; i < count; i++)
        {
            int level = BattlePassService.START_LEVEL + i * BattlePassService.REWARD_INTERVAL;
            float y = -(LevelElementTopPadding + i * rowHeight);
            GetLevelView(i).Setup(level, LevelNotReachedColor);
            Place(levels[i].transform, y);
            GetCell(i * 2, FreeRewardPrefab, FreeRewardsTransform).Setup(BattlePassService.GetReward(level, false), this);
            Place(cells[i * 2].transform, y);
            GetCell(i * 2 + 1, PremiumRewardPrefab, PassRewardsTransform).Setup(BattlePassService.GetReward(level, true), this);
            Place(cells[i * 2 + 1].transform, y);
        }
        for (int i = count; i < levels.Count; i++) levels[i].gameObject.SetActive(false);
        for (int i = count * 2; i < cells.Count; i++) cells[i].gameObject.SetActive(false);

        float height = LevelElementTopPadding * 2f + (count - 1) * rowHeight;
        RectTransform content = ScrollRect.content;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height);
        RefreshFill(count, rowHeight);
        RefreshBuyButton();
        ScrollToFirstClaimable(rowHeight, height);
    }

    private BattlePassLevelView GetLevelView(int index)
    {
        while (levels.Count <= index)
            levels.Add(Instantiate(LevelElementPrefab, LevelsTransform));
        levels[index].gameObject.SetActive(true);
        return levels[index];
    }

    private BattlePassRewardCellView GetCell(int index, BattlePassRewardCellView prefab, RectTransform parent)
    {
        while (cells.Count <= index)
            cells.Add(null);
        if (cells[index] == null)
            cells[index] = Instantiate(prefab, parent);
        cells[index].gameObject.SetActive(true);
        return cells[index];
    }

    private static void Place(Transform t, float y)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    // Thanh fill dọc cột mốc: dài tới mốc cao nhất đã đạt.
    private void RefreshFill(int count, float rowHeight)
    {
        if (FillBarRectTransform == null || FillBarParentRectTransform == null)
            return;
        float total = (count - 1) * rowHeight;
        FillBarParentRectTransform.sizeDelta = new Vector2(FillBarParentRectTransform.sizeDelta.x, total);
        float reached = (BattlePassService.GetHighestLevel() - BattlePassService.START_LEVEL) / (float)BattlePassService.REWARD_INTERVAL;
        FillBarRectTransform.sizeDelta = new Vector2(FillBarRectTransform.sizeDelta.x, Mathf.Clamp(reached * rowHeight, 0f, total));
    }

    private void ScrollToFirstClaimable(float rowHeight, float height)
    {
        int level = BattlePassService.GetFirstClaimableLevel();
        if (level < 0)
            level = Mathf.Max(BattlePassService.START_LEVEL, BattlePassService.GetHighestLevel());
        int index = Mathf.Max(0, (level - BattlePassService.START_LEVEL) / BattlePassService.REWARD_INTERVAL);
        float viewport = ScrollRect.viewport != null ? ScrollRect.viewport.rect.height : 0f;
        float scrollable = Mathf.Max(1f, height - viewport);
        float target = Mathf.Clamp(LevelElementTopPadding + index * rowHeight - viewport * 0.5f, 0f, scrollable);
        ScrollRect.verticalNormalizedPosition = 1f - target / scrollable;
    }

    private void RefreshBuyButton()
    {
        bool isBought = BattlePassService.IsPremiumBought();
        if (BuyBattlePassButton != null) BuyBattlePassButton.SetActive(isBought == false);
        if (BattlePassPriceText != null)
            BattlePassPriceText.text = PurchaseController.Instance.GetPriceText(PurchaseController.STORE_ID_BATTLE_PASS) ?? "N/A";
        if (btBuy != null) btBuy.interactable = isBuying == false;
    }

    // jvk gốc: "<sprite=0>{N}", icon theo loại thưởng.
    public void SetRewardText(TMP_Text text, BattlePassRewardType type, int amount)
    {
        if (text == null)
            return;
        int index = (int)type;
        TMP_SpriteAsset asset = rewardSpriteAssets != null && index < rewardSpriteAssets.Length ? rewardSpriteAssets[index] : null;
        text.spriteAsset = asset;
        text.text = (asset != null ? "<sprite=0>" : string.Empty) + ShopUIUtil.FormatAmount(amount);
    }

    // jva / jvb / jvc gốc.
    public void Claim(BattlePassReward reward)
    {
        if (BattlePassService.Claim(reward.level, reward.isPremium) == false)
            return;
        ShopUIUtil.RefreshLobby();
        foreach (BattlePassRewardCellView cell in cells)
        {
            if (cell != null && cell.gameObject.activeSelf)
                cell.Refresh();
        }
    }

    private void OnBuyClicked()
    {
        if (isBuying || BattlePassService.IsPremiumBought())
            return;
        isBuying = true;
        RefreshBuyButton();
        PurchaseController.Instance.Purchase(PurchaseController.STORE_ID_BATTLE_PASS);
    }

    private void OnPurchaseSuccessful(string productId)
    {
        if (productId != PurchaseController.STORE_ID_BATTLE_PASS)
            return;
        isBuying = false;
        BattlePassService.NotifyChanged();
        Build();
    }

    private void OnPurchaseFailed()
    {
        isBuying = false;
        RefreshBuyButton();
    }
}
