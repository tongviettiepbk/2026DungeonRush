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
}
