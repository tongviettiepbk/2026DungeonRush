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
    // (không trùng), giá trị random trong [1 .. maxValue] theo phân bố chuẩn cắt (xem RollSubStatValue).
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
            result.Add(new GearSubStat(picked.type, RollSubStatValue(picked.maxValue, config.subStatDistributionSpread)));
        }

        return result;
    }

    // Random giá trị 1 dòng substat. Port ĐÚNG hàm GameResources.jhh(1, maxValue) của game gốc
    // (reverse từ libil2cpp v41): phân bố CHUẨN CẮT (truncated normal) trên [1 .. maxValue].
    //   mean = (1 + maxValue) / 2 ;  sigma = (maxValue - 1) * spread   (spread = SubStatDistributionSpread = 0.25)
    //   x = mean + sigma * Z (Z chuẩn N(0,1)); roll lại tới khi x nằm trong [1, maxValue]; làm tròn 2 số lẻ.
    // → giá trị dồn về giữa khoảng, hiếm khi chạm biên (biên = ±2σ). KHÔNG còn uniform như trước.
    public static float RollSubStatValue(float maxValue, float spread)
    {
        const float min = 1f;
        float mean = (min + maxValue) * 0.5f;
        float sigma = (maxValue - min) * spread;

        float x;
        do
        {
            x = mean + sigma * NextStandardNormal();
        }
        while (x < min || x > maxValue);

        // jhh làm tròn ở "không gian ×100" rồi chia lại → 2 chữ số thập phân (banker's rounding).
        return Mathf.Round(x * 100f) / 100f;
    }

    // Số ngẫu nhiên phân phối chuẩn N(0,1) — Box-Muller, đúng như GameResources.jhi() game gốc:
    //   Z = sqrt(-2 * ln(U1)) * cos(2π * U2),  U1,U2 = Random.Range(0.0001, 1).
    private static float NextStandardNormal()
    {
        float u1 = Random.Range(0.0001f, 1f);
        float u2 = Random.Range(0.0001f, 1f);
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
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
