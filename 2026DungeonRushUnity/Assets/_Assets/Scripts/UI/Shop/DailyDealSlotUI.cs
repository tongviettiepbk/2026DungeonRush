using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô Daily Deal (DailyDealSlotUI gốc): tiêu đề "{loại} Deal", lưới thưởng "<sprite=0>số", giá, nút mua.
public class DailyDealSlotUI : MonoBehaviour
{
    public int SlotIndex;
    public GameObject RootContainer;
    public TextMeshProUGUI TitleText;
    public GridLayoutGroup RewardGrid;
    public DailyDealRewardEntryUI RewardEntryPrefab;
    public TextMeshProUGUI PriceText;
    public Button BuyButton;
    public RectTransform ExpandableContentRect;
    public float ContentHeightForLessThan5Rewards;
    public float ContentHeightFor5Rewards;
    public int DefaultConstraintCount;
    public int ConstraintCountForMoreThan4Rewards;
    // Reskin Shop 2026-11-12: lưới luôn 2 hàng, chỉ đổi bề rộng ô (≤ 4 món = 2 cột rộng, 5 món = 3 cột hẹp) → thẻ cao cố định.
    public float CellWidthForLessThan5Rewards;
    public float CellWidthFor5Rewards;
    public TMP_SpriteAsset CompanionSpriteAssetOverride;

    // Icon từng loại thưởng, index = (int)ShopRewardType (GameResources gốc giữ các sprite asset này).
    public TMP_SpriteAsset[] RewardSpriteAssets;

    // Key tên loại deal theo thứ tự enum DealType (strings_en "Common.DealType.*").
    private static readonly string[] TYPE_KEYS = { "Common.DealType.DungeonKey", "Common.DealType.Gem", "Common.DealType.Cloak", "Common.DealType.Mining", "Common.DealType.Companion" };

    private void Awake()
    {
        if (BuyButton != null) BuyButton.onClick.AddListener(() => DailyDealService.BuyDeal(SlotIndex));
    }

    public void Refresh()
    {
        if (DailyDealService.TryGetDeal(SlotIndex, out DealSize size, out DealType type) == false)
        {
            if (RootContainer != null) RootContainer.SetActive(false);
            return;
        }

        if (RootContainer != null) RootContainer.SetActive(true);
        ShopUIUtil.SetText(TitleText, string.Format(Loc.L("UI.Store.DailyDeal.Title"), Loc.L(TYPE_KEYS[(int)type])));
        int count = FillRewards(size, type);
        ApplyLayout(count);
        ShopUIUtil.SetText(PriceText, PurchaseController.Instance.GetPriceText(DailyDealService.GetProductId(size)) ?? "N/A");
    }

    // kam gốc.
    private int FillRewards(DealSize size, DealType type)
    {
        if (RewardGrid == null || RewardEntryPrefab == null) return 0;

        Transform grid = RewardGrid.transform;
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Destroy(grid.GetChild(i).gameObject);
        }

        List<ShopReward> rewards = DailyDealService.GetRewards(size, type);
        for (int i = 0; i < rewards.Count; i++)
        {
            DailyDealRewardEntryUI entry = Instantiate(RewardEntryPrefab, grid);
            entry.Setup(GetSpriteAsset(rewards[i].type), rewards[i].amount);
        }
        return rewards.Count;
    }

    private TMP_SpriteAsset GetSpriteAsset(ShopRewardType type)
    {
        if (type == ShopRewardType.Bone && CompanionSpriteAssetOverride != null) return CompanionSpriteAssetOverride;
        int index = (int)type;
        return RewardSpriteAssets != null && index < RewardSpriteAssets.Length ? RewardSpriteAssets[index] : null;
    }

    // kan gốc: 5 thưởng (deal Mining) → lưới 3 cột + ô cao hơn.
    private void ApplyLayout(int rewardCount)
    {
        bool isMany = rewardCount > 4;
        if (RewardGrid != null)
        {
            RewardGrid.constraintCount = isMany ? ConstraintCountForMoreThan4Rewards : DefaultConstraintCount;
            float cellWidth = isMany ? CellWidthFor5Rewards : CellWidthForLessThan5Rewards;
            if (cellWidth > 0f) RewardGrid.cellSize = new Vector2(cellWidth, RewardGrid.cellSize.y);
        }
        if (ExpandableContentRect != null)
        {
            Vector2 size = ExpandableContentRect.sizeDelta;
            size.y = isMany ? ContentHeightFor5Rewards : ContentHeightForLessThan5Rewards;
            ExpandableContentRect.sizeDelta = size;
        }
    }
}
