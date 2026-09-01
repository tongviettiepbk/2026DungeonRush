using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Config tĩnh cho 5 loại trang bị catalog (C2..C6). Load toàn bộ GearItemData trong
// Resources/Scriptable Objects/Gears rồi bucket theo (slot) và (slot, rarity) để random rơi đồ.
// Theo pattern StaticWeaponData nhưng gom chung mọi slot.
public class StaticGearItemData
{
    public List<GearItemData> all;

    private Dictionary<GearSlotType, List<GearItemData>> bySlot;
    private Dictionary<GearSlotType, Dictionary<Rarity, List<GearItemData>>> poolBySlotRarity;

    public StaticGearItemData()
    {
        all = Resources.LoadAll<GearItemData>("Scriptable Objects/Gears")
            .OrderBy(x => x.slot).ThenBy(x => x.assetName).ToList();

        bySlot = new Dictionary<GearSlotType, List<GearItemData>>();
        poolBySlotRarity = new Dictionary<GearSlotType, Dictionary<Rarity, List<GearItemData>>>();

        for (int i = 0; i < all.Count; i++)
        {
            GearItemData g = all[i];

            if (bySlot.ContainsKey(g.slot) == false)
                bySlot[g.slot] = new List<GearItemData>();
            bySlot[g.slot].Add(g);

            // Pool random rơi đồ chỉ gồm đồ hero — bỏ trang bị quái/boss (bySlot/all giữ đủ
            // để EquipVisualResolver còn tra được visual đồ enemy theo assetName).
            if (g.isMonsterGear) continue;

            if (poolBySlotRarity.ContainsKey(g.slot) == false)
                poolBySlotRarity[g.slot] = new Dictionary<Rarity, List<GearItemData>>();
            if (poolBySlotRarity[g.slot].ContainsKey(g.rarity) == false)
                poolBySlotRarity[g.slot][g.rarity] = new List<GearItemData>();
            poolBySlotRarity[g.slot][g.rarity].Add(g);
        }
    }

    public List<GearItemData> GetBySlot(GearSlotType slot)
    {
        return bySlot.ContainsKey(slot) ? bySlot[slot] : new List<GearItemData>();
    }

    public GearItemData GetData(string assetName)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].assetName == assetName)
            {
                return all[i];
            }
        }

        DebugCustom.Log("[StaticGearItemData] Not found=" + assetName);
        return null;
    }

    public List<GearItemData> GetPool(GearSlotType slot, Rarity rarity)
    {
        if (poolBySlotRarity.ContainsKey(slot) && poolBySlotRarity[slot].ContainsKey(rarity))
        {
            return poolBySlotRarity[slot][rarity];
        }

        return new List<GearItemData>();
    }

    public GearItemData GetRandom(GearSlotType slot, Rarity rarity)
    {
        List<GearItemData> pool = GetPool(slot, rarity);
        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        return null;
    }
}
