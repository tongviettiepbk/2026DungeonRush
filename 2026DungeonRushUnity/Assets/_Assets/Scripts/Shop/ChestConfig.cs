using System.Collections.Generic;
using UnityEngine;

// ChestConfig gốc (GameResources.ChestConfig): danh sách rương bán ở Store + gói rương IAP.
[CreateAssetMenu(fileName = "ChestConfig", menuName = "DungOnRush/Shop/Chest Config")]
public class ChestConfig : ScriptableObject
{
    public List<ChestData> Chests = new List<ChestData>();
    public List<ChestBundleData> Bundles = new List<ChestBundleData>();
    public ChestBundleData LegendaryFeaturedBundle;
    public ChestBundleData MythicFeaturedBundle;

    // flj gốc.
    public ChestData GetChest(string chestId)
    {
        for (int i = 0; i < Chests.Count; i++)
        {
            if (Chests[i] != null && Chests[i].ChestId == chestId)
            {
                return Chests[i];
            }
        }
        return null;
    }

    // fll gốc: tìm gói theo StoreId (id sản phẩm IAP).
    public ChestBundleData GetBundleByStoreId(string storeId)
    {
        for (int i = 0; i < Bundles.Count; i++)
        {
            if (Bundles[i] != null && Bundles[i].StoreId == storeId)
            {
                return Bundles[i];
            }
        }
        return null;
    }
}
