using System.Collections.Generic;
using UnityEngine;

// C7 Áo choàng — config chung của hệ cape (1 asset duy nhất): chi phí triệu hồi,
// số substat, và tham số công thức XP salvage / level-up theo từng rarity.
[CreateAssetMenu(fileName = "CapeConfig", menuName = "DungOnRush/Cape Config")]
public class CapeConfigData : ScriptableObject
{
    public int maxLevel;
    public int summonCost;      // tốn CloakCurrency để triệu hồi.
    public int subStatCount;

    public List<CapeRarityConfig> rarityConfigs = new List<CapeRarityConfig>();

    public CapeRarityConfig GetRarityConfig(Rarity rarity)
    {
        for (int i = 0; i < rarityConfigs.Count; i++)
        {
            if (rarityConfigs[i].rarity == rarity)
            {
                return rarityConfigs[i];
            }
        }

        return null;
    }
}

[System.Serializable]
public class CapeRarityConfig
{
    public Rarity rarity;
    public float salvageBaseXP;   // XP nhận khi salvage cape thừa.
    public float levelUpBaseXP;    // công thức XP lên cấp (đoạn 1).
    public float levelUpScaler;
    public float levelUpExpo;
    public float levelUpBaseXP2;   // công thức XP lên cấp (đoạn 2, level cao).
    public float levelUpScaler2;
}
