using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Config tĩnh hệ Companion (D1). Load toàn bộ CompanionData asset trong Resources rồi
// dựng pool theo rarity (mở rương/gacha companion) và index theo type. Theo pattern
// StaticWeaponData/StaticGearItemData của DungeonRush.
public class StaticCompanionData
{
    public List<CompanionData> companions;

    private Dictionary<Rarity, List<CompanionData>> poolByRarity;

    public StaticCompanionData()
    {
        companions = Resources.LoadAll<CompanionData>("Scriptable Objects/Companions")
            .OrderBy(x => x.assetName).ToList();

        poolByRarity = new Dictionary<Rarity, List<CompanionData>>();
        for (int i = 0; i < companions.Count; i++)
        {
            CompanionData cp = companions[i];
            if (poolByRarity.ContainsKey(cp.rarity) == false)
            {
                poolByRarity[cp.rarity] = new List<CompanionData>();
            }
            poolByRarity[cp.rarity].Add(cp);
        }
    }

    // Khoá tra cứu là assetName (companion không có itemId số).
    public CompanionData GetData(string assetName)
    {
        for (int i = 0; i < companions.Count; i++)
        {
            if (companions[i].assetName == assetName)
            {
                return companions[i];
            }
        }

        DebugCustom.Log("[StaticCompanionData] Not found=" + assetName);
        return null;
    }

    public CompanionData GetByType(CompanionType type)
    {
        for (int i = 0; i < companions.Count; i++)
        {
            if (companions[i].type == type)
            {
                return companions[i];
            }
        }

        return null;
    }

    public List<CompanionData> GetPool(Rarity rarity)
    {
        if (poolByRarity.ContainsKey(rarity))
        {
            return poolByRarity[rarity];
        }

        return new List<CompanionData>();
    }

    // Random 1 companion theo rarity (dùng khi mở rương/gacha).
    public CompanionData GetRandom(Rarity rarity)
    {
        List<CompanionData> pool = GetPool(rarity);
        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        return null;
    }
}
