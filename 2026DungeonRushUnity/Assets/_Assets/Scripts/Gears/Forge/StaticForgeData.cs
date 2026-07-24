using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// C9 Forge — bảng xác suất ra rarity theo cấp forge (0..99). Mỗi dòng là 10 cột xác suất
// (tổng = 100). Cột theo THỨ TỰ forge (khác tên hệ rarity item):
//   0 Common,1 Uncommon,2 Rare,3 Epic,4 Legendary,5 Mythic,6 Divine,7 Celestial,8 Immortal,9 Eternal
// Lưu dạng ma trận positional (JSON trong Resources) — theo pattern JSON-in-Resources của StickIdle.
// Giá trị APK và Remote Config live TRÙNG NHAU 100%.
public class StaticForgeData
{
    public const int COLUMN_COUNT = 10;

    public List<List<float>> probabilities;   // [forgeLevel][column]

    public StaticForgeData()
    {
        TextAsset file = Resources.Load<TextAsset>("Json/forge_rarity_probabilities");
        probabilities = JsonConvert.DeserializeObject<List<List<float>>>(file.text);
    }

    public int MaxForgeLevel => probabilities.Count - 1;

    // Trả về mảng 10 xác suất cho 1 cấp forge (clamp trong [0, Max]).
    public List<float> GetProbabilities(int forgeLevel)
    {
        if (forgeLevel < 0) forgeLevel = 0;
        if (forgeLevel > MaxForgeLevel) forgeLevel = MaxForgeLevel;
        return probabilities[forgeLevel];
    }
}
