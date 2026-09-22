using System.Collections.Generic;
using UnityEngine;

// Logic Forge (rèn/loot ra trang bị). Đây là PHẦN LÕI, độc lập UI:
//   Forge Level  --(bảng xác suất StaticForgeData)-->  roll ra 1 bậc Rarity.
// Bảng forge_rarity_probabilities có 10 cột (mỗi dòng tổng 100%), ánh xạ theo INDEX
// sang enum Rarity: cột i -> (Rarity)i, tức Rarity 0..9 (Common..Divine). Rarity.Ultimate(10)
// không nằm trong bảng nên forge không bao giờ roll ra.
//
// CHƯA làm ở đây (cần model trang bị + UI + save đang thiếu trong project):
//   sinh item đầy đủ (slot/level/substat), equip-vs-sell, auto-forge filter, cost/economy.
public static class ForgeController
{
    // Roll 1 bậc rarity theo Forge Level hiện tại.
    // forgeLevel được clamp trong [0, MaxForgeLevel] bởi StaticForgeData.
    public static Rarity RollRarity(int forgeLevel)
    {
        List<float> probs = GameData.staticData.forge.GetProbabilities(forgeLevel);

        float roll = Random.value * 100f;   // [0, 100)
        float acc = 0f;
        for (int i = 0; i < probs.Count; i++)
        {
            acc += probs[i];
            if (roll < acc)
            {
                return (Rarity)i;
            }
        }

        // Phòng sai số làm tròn (tổng cột < 100 do float): trả bậc cao nhất có xác suất > 0.
        for (int i = probs.Count - 1; i >= 0; i--)
        {
            if (probs[i] > 0f)
            {
                return (Rarity)i;
            }
        }
        return Rarity.Common;
    }

    // ---- LEVEL của món forge ra (port ĐÚNG game gốc qv.iaq, reverse libil2cpp v41) ----
    // Xem DecodedData/ITEM_LEVEL_MODEL.md. Hằng số + bảng offset = giá trị GameResources gốc.

    public const int ForgeMinLevel = 1;
    public const int ForgeMaxLevel = 100;            // cap gốc: + bonus mastery; base player = 100.
    public const int ForgeLowerRarityMinLevel = 90;  // tụt rarity thì level nhảy lên [90 .. cap].

    // Bảng {offset, weight} theo rarity (chỉ Common/Uncommon/Rare có bảng riêng).
    private static readonly int[][] LevelOffsetsCommon   = { new[] { 3, 10 }, new[] { 6, 30 }, new[] { 9, 40 }, new[] { 12, 15 }, new[] { 15, 5 } };
    private static readonly int[][] LevelOffsetsUncommon = { new[] { 2, 10 }, new[] { 4, 30 }, new[] { 6, 40 }, new[] { 8, 15 }, new[] { 10, 5 } };
    private static readonly int[][] LevelOffsetsRare     = { new[] { 1, 10 }, new[] { 2, 30 }, new[] { 3, 40 }, new[] { 4, 15 }, new[] { 5, 5 } };
    // Rarity >= Epic dùng bảng default (offset có thể âm).
    private static readonly int[][] LevelOffsetsDefault  = { new[] { -2, 5 }, new[] { -1, 10 }, new[] { 0, 15 }, new[] { 1, 20 }, new[] { 2, 25 }, new[] { 3, 15 }, new[] { 4, 10 } };

    // Tính level món forge/summon ra. base/inRarity = level & rarity món ĐANG MẶC ở slot đích
    // (isEmpty = slot đang trống). rolledRarity = rarity đã roll cho món mới. 4 nhánh:
    //   - slot trống HOẶC lên rarity cao hơn  -> ForgeMinLevel (reset về 1)
    //   - cùng rarity                          -> clamp(baseLevel + offset_ngẫu_nhiên(rarity), 1, cap) (cộng dồn)
    //   - tụt rarity thấp hơn                  -> Random[ForgeLowerRarityMinLevel .. cap]
    public static int RollForgeLevel(int baseLevel, Rarity inRarity, Rarity rolledRarity, bool isEmpty)
    {
        if (isEmpty || rolledRarity > inRarity)
        {
            return ForgeMinLevel;
        }

        // gốc: ForgeMaxLevel + round(bonus mastery ForgeMaxItemLevel); base player = 100.
        int cap = ForgeMaxLevel + Mathf.RoundToInt(MasteryService.GetCurrentValue(MasteryUpgradeType.ForgeMaxItemLevel));

        if (rolledRarity < inRarity)
        {
            return Random.Range(ForgeLowerRarityMinLevel, cap + 1);
        }

        // rolledRarity == inRarity: cộng dồn offset có trọng số vào level cũ.
        int level = baseLevel + WeightedLevelOffset(rolledRarity);
        return Mathf.Clamp(level, ForgeMinLevel, cap);
    }

    // Rút 1 offset theo trọng số từ bảng của rarity (fallback bảng default cho Epic trở lên).
    private static int WeightedLevelOffset(Rarity rarity)
    {
        int[][] table;
        switch (rarity)
        {
            case Rarity.Common:   table = LevelOffsetsCommon; break;
            case Rarity.Uncommon: table = LevelOffsetsUncommon; break;
            case Rarity.Rare:     table = LevelOffsetsRare; break;
            default:              table = LevelOffsetsDefault; break;
        }

        int total = 0;
        for (int i = 0; i < table.Length; i++)
        {
            total += table[i][1];
        }

        int roll = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < table.Length; i++)
        {
            acc += table[i][1];
            if (roll < acc)
            {
                return table[i][0];
            }
        }

        return table[table.Length - 1][0];
    }
}
