using Spine.Unity;
using UnityEngine;

// C7 Áo choàng — 1 mẫu cape. Khác gear catalog: cape CÓ base stat + scaler theo level.
// (công thức level-up/salvage nằm ở CapeConfigData). Rarity chỉ tới 5 (Mythic), 2 mẫu/rarity.
[CreateAssetMenu(fileName = "Cape-", menuName = "DungOnRush/Cape Data")]
public class CapeData : ScriptableObject
{
    public int capeId;
    public string capeName;
    public Rarity rarity;
    public string localizationKey;
    public Sprite icon;

    // HÌNH LÊN NGƯỜI = Spine (không phải sprite). Cape gốc chia 3 skeleton theo kích thước
    // (short: tier 1-2, mid: tier 3-4, long: tier 5-6), mỗi skeleton chứa 4 skin dạng
    // "tier_XX_cloak_YY". Mặc cape = gán skeletonData + đổi skin (xem HeroVisual.WearCape).
    [Header("Hình lên người (Spine)")]
    public SkeletonDataAsset skeletonData;  // Skeleton chứa skin của cape này (short/mid/long).
    public string skinName;                 // Tên skin trong skeleton, VD "tier_02_cloak_02".

    [Header("Stat gốc + scaler theo level")]
    public float healthBase;
    public float damageBase;
    public float healthScaler;
    public float damageScaler;
    public int subStatCount;
}
