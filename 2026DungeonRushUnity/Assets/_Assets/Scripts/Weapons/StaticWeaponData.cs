using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Config tĩnh hệ Vũ khí (C1). Load toàn bộ WeaponData asset trong Resources rồi dựng
// pool theo rarity để random khi rơi đồ (chỉ gồm vũ khí người chơi). Theo pattern
// StaticGearData của StickIdle nhưng gọn lại cho DungeonRush.
public class StaticWeaponData
{
    public List<WeaponData> weapons;

    // Pool random rơi đồ cho người chơi, tách theo rarity (bỏ vũ khí quái/boss).
    private Dictionary<Rarity, List<WeaponData>> poolByRarity;

    public StaticWeaponData()
    {
        weapons = Resources.LoadAll<WeaponData>("Scriptable Objects/Gears/Weapons")
            .OrderBy(x => x.assetName).ToList();

        poolByRarity = new Dictionary<Rarity, List<WeaponData>>();
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponData wp = weapons[i];
            if (wp.isMonsterWeapon) continue;

            if (poolByRarity.ContainsKey(wp.rarity) == false)
            {
                poolByRarity[wp.rarity] = new List<WeaponData>();
            }
            poolByRarity[wp.rarity].Add(wp);
        }
    }

    // Khoá tra cứu là assetName vì itemId có thể trùng giữa các asset (VD vũ khí quái).
    public WeaponData GetData(string assetName)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].assetName == assetName)
            {
                return weapons[i];
            }
        }

        DebugCustom.Log("[StaticWeaponData] Not found=" + assetName);
        return null;
    }

    public List<WeaponData> GetPool(Rarity rarity)
    {
        if (poolByRarity.ContainsKey(rarity))
        {
            return poolByRarity[rarity];
        }

        return new List<WeaponData>();
    }

    // Random 1 vũ khí người chơi theo rarity (dùng khi rơi đồ / mở rương).
    public WeaponData GetRandom(Rarity rarity)
    {
        List<WeaponData> pool = GetPool(rarity);
        if (pool.Count > 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        return null;
    }
}
