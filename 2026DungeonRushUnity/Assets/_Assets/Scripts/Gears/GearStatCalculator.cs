using System.Collections.Generic;
using UnityEngine;

// Tính chỉ số trang bị từ GearStatConfigData. Port đúng công thức GameResources của game gốc.
//   Main = Base(slot) × tierScaler^Rarity × (1 + levelScaler × Level)
// Chỉ số chính KHÔNG lưu trong save (chỉ substat lưu sẵn) → luôn tính lại bằng hàm này khi hiển thị.
public static class GearStatCalculator
{
    // Chỉ số chính của 1 slot trang bị (C2..C6). Trả về (giá trị, là Máu hay Sát thương).
    public static double GetGearMainStat(GearStatConfigData config, GearSlotType slot, Rarity rarity, int level, out GearMainStatKind kind)
    {
        GearBaseStatEntry entry = config.GetGearBase(slot);
        if (entry == null)
        {
            kind = GearMainStatKind.Damage;
            return 0;
        }

        kind = entry.mainStatKind;
        return Compute(config, entry.baseValue, rarity, level);
    }

    // Chỉ số chính của vũ khí (C1) — luôn là Sát thương, base khác nhau giữa cận chiến / bắn xa.
    public static double GetWeaponMainStat(GearStatConfigData config, WeaponType weaponType, Rarity rarity, int level)
    {
        float baseValue = weaponType == WeaponType.Melee ? config.weaponBaseMelee : config.weaponBaseRange;
        return Compute(config, baseValue, rarity, level);
    }

    // Lõi công thức dùng chung.
    private static double Compute(GearStatConfigData config, float baseValue, Rarity rarity, int level)
    {
        double tierMul = Mathf.Pow(config.tierScaler, (int)rarity);
        double levelMul = 1.0 + config.levelScaler * level;
        return baseValue * tierMul * levelMul;
    }

    // Roll toàn bộ substat cho 1 trang bị mới rơi ra: số dòng theo rarity, loại random theo weight
    // (không trùng), giá trị random trong (0 .. maxValue].
    public static List<GearSubStat> RollSubStats(GearStatConfigData config, Rarity rarity)
    {
        List<GearSubStat> result = new List<GearSubStat>();
        int count = config.GetSubStatCount(rarity);
        if (count <= 0)
        {
            return result;
        }

        // Bản sao pool để rút không trùng loại.
        List<SubStatPoolEntry> remaining = new List<SubStatPoolEntry>(config.subStatPool);

        for (int n = 0; n < count && remaining.Count > 0; n++)
        {
            SubStatPoolEntry picked = PickWeighted(remaining);
            remaining.Remove(picked);
            result.Add(new GearSubStat(picked.type, RollSubStatValue(picked.maxValue)));
        }

        return result;
    }

    // Random giá trị 1 substat. LƯU Ý: phân bố chính xác của game gốc (SubStatDistributionSpread)
    // chưa reverse hoàn toàn; tạm dùng uniform (0 .. maxValue]. Substat của đồ đã có thì đọc thẳng save.
    public static float RollSubStatValue(float maxValue)
    {
        return Mathf.Clamp(Random.value, 0.0001f, 1f) * maxValue;
    }

    private static SubStatPoolEntry PickWeighted(List<SubStatPoolEntry> pool)
    {
        int total = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            total += pool[i].weight;
        }

        int roll = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += pool[i].weight;
            if (roll < acc)
            {
                return pool[i];
            }
        }

        return pool[pool.Count - 1];
    }
}

[System.Serializable]
public class GearSubStat
{
    public SubStatType type;
    public float value;

    public GearSubStat() { }

    public GearSubStat(SubStatType type, float value)
    {
        this.type = type;
        this.value = value;
    }
}
