using System;
using System.Collections.Generic;
using UnityEngine;

// 1 loại rương ở Store (ChestData gốc — tên field giữ nguyên bản gốc). Mở ra pet hoặc cape theo RarityOdds.
// Số liệu: DecodedData/SHOP_MODEL.md §2. Asset do Tools/DungeonRush/Build Shop UI Prefabs sinh.
[CreateAssetMenu(fileName = "ChestData", menuName = "DungOnRush/Shop/Chest Data")]
public class ChestData : ScriptableObject
{
    public string ChestId;
    public string ChestName;
    public Sprite Icon;
    public Sprite OpenIcon;
    public Sprite MainIcon;
    public List<Sprite> ShowcaseSprites = new List<Sprite>();
    public int GemCost;
    public bool RequireRarityCompletion;
    public Rarity RequiredCompletedRarity;
    public int ItemCount;
    public Rarity Rarity;       // Rarity cao nhất của rương — mốc pity.
    public int PityCount;       // 0 = không có pity.
    public List<ChestRarityOdds> RarityOdds = new List<ChestRarityOdds>();

    // flm gốc: ảnh lớn trên thẻ store, không có thì dùng Icon.
    public Sprite GetMainIcon()
    {
        return MainIcon != null ? MainIcon : Icon;
    }

    // flo gốc: roll rarity theo trọng số (trọng số âm coi như 0); tổng ≤ 0 → Rarity của rương.
    public Rarity RollRarity()
    {
        float total = 0f;
        for (int i = 0; i < RarityOdds.Count; i++)
        {
            total += Mathf.Max(RarityOdds[i].Weight, 0f);
        }
        if (total <= 0f)
        {
            return Rarity;
        }

        float roll = UnityEngine.Random.Range(0f, total);
        for (int i = 0; i < RarityOdds.Count; i++)
        {
            float weight = Mathf.Max(RarityOdds[i].Weight, 0f);
            if (roll < weight)
            {
                return RarityOdds[i].Rarity;
            }
            roll -= weight;
        }

        return RarityOdds[RarityOdds.Count - 1].Rarity;
    }
}

[Serializable]
public class ChestRarityOdds
{
    public Rarity Rarity;
    public float Weight;
}
