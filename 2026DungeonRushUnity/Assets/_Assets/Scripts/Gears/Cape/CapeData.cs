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

    [Header("Stat gốc + scaler theo level")]
    public float healthBase;
    public float damageBase;
    public float healthScaler;
    public float damageScaler;
    public int subStatCount;
}
