using UnityEngine;

// "Summon Level" Companion + tỉ lệ rarity mỗi lượt — REVERSE il2cpp v41 (static class `ly`, bảng `whm`
// dựng trong .cctor: 100 phần tử (threshold, t1..t6)). Xem DecodedData/COMPANION_MODEL.md §8.
//   • Level tính theo TỔNG số lượt đã summon (save TotalCompanionSummons), KHÔNG theo Bone đã tiêu.
//   • THRESHOLDS[i] = tổng lượt cần để VƯỢT level i+1 (lv1: 0..49, lv2: 50..104 ...). Hàng cuối = int.MaxValue → max 100.
//   • Mỗi lượt roll rarity bằng tỉ lệ của level HIỆN TẠI (tính trước khi cộng lượt đó) — ly.gpt.
public static class CompanionSummonLevelConfig
{
    public const int MAX_LEVEL = 100;
    public const int RARITY_COUNT = 6; // Common..Mythic

    private static readonly int[] THRESHOLDS =
    {
        50, 105, 165, 230, 300, 375, 455, 540, 630, 725,
        825, 930, 1040, 1155, 1275, 1400, 1530, 1665, 1805, 1950,
        2100, 2255, 2415, 2580, 2750, 2925, 3105, 3290, 3480, 3675,
        3875, 4080, 4290, 4505, 4725, 4950, 5180, 5415, 5655, 5900,
        6150, 6405, 6665, 6930, 7200, 7475, 7755, 8040, 8330, 8625,
        8925, 9230, 9540, 9855, 10175, 10500, 10830, 11165, 11505, 11850,
        12200, 12555, 12915, 13280, 13650, 14025, 14405, 14790, 15180, 15575,
        15975, 16380, 16790, 17205, 17625, 18050, 18480, 18915, 19355, 19800,
        20250, 20705, 21165, 21630, 22100, 22575, 23055, 23540, 24030, 24525,
        25025, 25530, 26040, 26555, 27075, 27600, 28130, 28665, 29205, int.MaxValue,
    };

    // RATES[i] = % ra Common, Uncommon, Rare, Epic, Legendary, Mythic ở summon level i+1 (tổng 100).
    private static readonly float[,] RATES =
    {
        { 100f, 0f, 0f, 0f, 0f, 0f },   // lv 1
        { 98f, 2f, 0f, 0f, 0f, 0f },   // lv 2
        { 97f, 3f, 0f, 0f, 0f, 0f },   // lv 3
        { 95f, 5f, 0f, 0f, 0f, 0f },   // lv 4
        { 93f, 7f, 0f, 0f, 0f, 0f },   // lv 5
        { 90f, 10f, 0f, 0f, 0f, 0f },   // lv 6
        { 86.8f, 13f, 0.2f, 0f, 0f, 0f },   // lv 7
        { 83.7f, 16f, 0.3f, 0f, 0f, 0f },   // lv 8
        { 79.5f, 20f, 0.5f, 0f, 0f, 0f },   // lv 9
        { 75.3f, 24f, 0.7f, 0f, 0f, 0f },   // lv 10
        { 71f, 28f, 1f, 0f, 0f, 0f },   // lv 11
        { 66.5f, 32f, 1.5f, 0f, 0f, 0f },   // lv 12
        { 60f, 38f, 2f, 0f, 0f, 0f },   // lv 13
        { 53f, 44f, 3f, 0f, 0f, 0f },   // lv 14
        { 45f, 50f, 5f, 0f, 0f, 0f },   // lv 15
        { 33f, 60f, 7f, 0f, 0f, 0f },   // lv 16
        { 20f, 70f, 10f, 0f, 0f, 0f },   // lv 17
        { 17.5f, 69.5f, 13f, 0f, 0f, 0f },   // lv 18
        { 17.5f, 66.5f, 16f, 0f, 0f, 0f },   // lv 19
        { 17.5f, 62.5f, 20f, 0f, 0f, 0f },   // lv 20
        { 17.5f, 58.5f, 24f, 0f, 0f, 0f },   // lv 21
        { 17.5f, 54.5f, 28f, 0f, 0f, 0f },   // lv 22
        { 17.5f, 50.5f, 32f, 0f, 0f, 0f },   // lv 23
        { 17.5f, 44.5f, 38f, 0f, 0f, 0f },   // lv 24
        { 17.5f, 38.49f, 44f, 0.01f, 0f, 0f },   // lv 25
        { 17.5f, 32.48f, 50f, 0.02f, 0f, 0f },   // lv 26
        { 17.5f, 22.47f, 60f, 0.03f, 0f, 0f },   // lv 27
        { 17.5f, 16.5f, 65.95f, 0.05f, 0f, 0f },   // lv 28
        { 17.5f, 16.5f, 65.93f, 0.07f, 0f, 0f },   // lv 29
        { 17.5f, 16.5f, 65.9f, 0.1f, 0f, 0f },   // lv 30
        { 17.5f, 16.5f, 65.85f, 0.15f, 0f, 0f },   // lv 31
        { 17.5f, 16.5f, 65.8f, 0.2f, 0f, 0f },   // lv 32
        { 17.5f, 16.5f, 65.7f, 0.3f, 0f, 0f },   // lv 33
        { 17.5f, 16.5f, 65.5f, 0.5f, 0f, 0f },   // lv 34
        { 17.5f, 16.5f, 65.3f, 0.7f, 0f, 0f },   // lv 35
        { 17.5f, 16.5f, 65f, 1f, 0f, 0f },   // lv 36
        { 17.5f, 16.5f, 64.5f, 1.5f, 0f, 0f },   // lv 37
        { 17.5f, 16.5f, 64f, 2f, 0f, 0f },   // lv 38
        { 17.5f, 16.5f, 63f, 3f, 0f, 0f },   // lv 39
        { 17.5f, 16.5f, 61f, 5f, 0f, 0f },   // lv 40
        { 17.5f, 16.5f, 59f, 7f, 0f, 0f },   // lv 41
        { 17.5f, 16.5f, 56f, 10f, 0f, 0f },   // lv 42
        { 17.5f, 16.5f, 53f, 13f, 0f, 0f },   // lv 43
        { 17.5f, 16.5f, 50f, 16f, 0f, 0f },   // lv 44
        { 17.5f, 16.5f, 46f, 20f, 0f, 0f },   // lv 45
        { 17.5f, 16.5f, 42f, 24f, 0f, 0f },   // lv 46
        { 17.5f, 16.5f, 38f, 28f, 0f, 0f },   // lv 47
        { 17.5f, 16.5f, 34f, 32f, 0f, 0f },   // lv 48
        { 17.5f, 16.5f, 28f, 38f, 0f, 0f },   // lv 49
        { 17.5f, 16.5f, 21.99f, 44f, 0.01f, 0f },   // lv 50
        { 17.5f, 16.5f, 16.5f, 49.48f, 0.02f, 0f },   // lv 51
        { 17.5f, 16.5f, 16.5f, 49.47f, 0.03f, 0f },   // lv 52
        { 17.5f, 16.5f, 16.5f, 49.45f, 0.05f, 0f },   // lv 53
        { 17.5f, 16.5f, 16.5f, 49.43f, 0.07f, 0f },   // lv 54
        { 17.5f, 16.5f, 16.5f, 49.4f, 0.1f, 0f },   // lv 55
        { 17.5f, 16.5f, 16.5f, 49.35f, 0.15f, 0f },   // lv 56
        { 17.5f, 16.5f, 16.5f, 49.3f, 0.2f, 0f },   // lv 57
        { 17.5f, 16.5f, 16.5f, 49.2f, 0.3f, 0f },   // lv 58
        { 17.5f, 16.5f, 16.5f, 49f, 0.5f, 0f },   // lv 59
        { 17.5f, 16.5f, 16.5f, 48.8f, 0.7f, 0f },   // lv 60
        { 17.5f, 16.5f, 16.5f, 48.5f, 1f, 0f },   // lv 61
        { 17.5f, 16.5f, 16.5f, 48f, 1.5f, 0f },   // lv 62
        { 17.5f, 16.5f, 16.5f, 47.5f, 2f, 0f },   // lv 63
        { 17.5f, 16.5f, 16.5f, 46.5f, 3f, 0f },   // lv 64
        { 17.5f, 16.5f, 16.5f, 44.5f, 5f, 0f },   // lv 65
        { 17.5f, 16.5f, 16.5f, 42.5f, 7f, 0f },   // lv 66
        { 17.5f, 16.5f, 16.5f, 39.5f, 10f, 0f },   // lv 67
        { 17.5f, 16.5f, 16.5f, 36.5f, 13f, 0f },   // lv 68
        { 17.5f, 16.5f, 16.5f, 33.5f, 16f, 0f },   // lv 69
        { 17.5f, 16.5f, 16.5f, 29.5f, 20f, 0f },   // lv 70
        { 17.5f, 16.5f, 16.5f, 25.5f, 24f, 0f },   // lv 71
        { 17.5f, 16.5f, 16.5f, 21.5f, 28f, 0f },   // lv 72
        { 17.5f, 16.5f, 16.5f, 17.5f, 32f, 0f },   // lv 73
        { 17.5f, 16.5f, 16.5f, 16.5f, 33f, 0f },   // lv 74
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.99f, 0.01f },   // lv 75
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.98f, 0.02f },   // lv 76
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.97f, 0.03f },   // lv 77
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.95f, 0.05f },   // lv 78
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.93f, 0.07f },   // lv 79
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.9f, 0.1f },   // lv 80
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.85f, 0.15f },   // lv 81
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.8f, 0.2f },   // lv 82
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.7f, 0.3f },   // lv 83
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.5f, 0.5f },   // lv 84
        { 17.5f, 16.5f, 16.5f, 16.5f, 32.3f, 0.7f },   // lv 85
        { 17.5f, 16.5f, 16.5f, 16.5f, 32f, 1f },   // lv 86
        { 17.5f, 16.5f, 16.5f, 16.5f, 31.5f, 1.5f },   // lv 87
        { 17.5f, 16.5f, 16.5f, 16.5f, 31f, 2f },   // lv 88
        { 17.5f, 16.5f, 16.5f, 16.5f, 30f, 3f },   // lv 89
        { 17.5f, 16.5f, 16.5f, 16.5f, 29f, 4f },   // lv 90
        { 17.5f, 16.5f, 16.5f, 16.5f, 28f, 5f },   // lv 91
        { 17.5f, 16.5f, 16.5f, 16.5f, 27f, 6f },   // lv 92
        { 17.5f, 16.5f, 16.5f, 16.5f, 26f, 7f },   // lv 93
        { 17.5f, 16.5f, 16.5f, 16.5f, 25f, 8f },   // lv 94
        { 17.5f, 16.5f, 16.5f, 16.5f, 24f, 9f },   // lv 95
        { 17.5f, 16.5f, 16.5f, 16.5f, 23f, 10f },   // lv 96
        { 17.5f, 16.5f, 16.5f, 16.5f, 22f, 11f },   // lv 97
        { 17.5f, 16.5f, 16.5f, 16.5f, 20f, 13f },   // lv 98
        { 17.5f, 16.5f, 16.5f, 16.5f, 18f, 15f },   // lv 99
        { 17.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f },   // lv 100
    };

    // Index hàng tỉ lệ ứng với tổng lượt (ly.gqb): hàng đầu tiên có threshold > total.
    private static int GetRowIndex(int totalSummons)
    {
        for (int i = 0; i < THRESHOLDS.Length; i++)
        {
            if (THRESHOLDS[i] > totalSummons)
            {
                return i;
            }
        }
        return THRESHOLDS.Length - 1;
    }

    // Summon level 1-based (ly.gpu).
    public static int GetLevel(int totalSummons)
    {
        return Mathf.Min(GetRowIndex(totalSummons) + 1, MAX_LEVEL);
    }

    // Tiến độ trong level hiện tại (ly.gpy): current/required; isMax = đã ở level cuối.
    public static void GetProgress(int totalSummons, out int current, out int required, out bool isMax)
    {
        int row = GetRowIndex(totalSummons);
        if (row >= THRESHOLDS.Length - 1)
        {
            current = 0;
            required = 0;
            isMax = true;
            return;
        }

        int prev = row == 0 ? 0 : THRESHOLDS[row - 1];
        current = totalSummons - prev;
        required = THRESHOLDS[row] - prev;
        isMax = false;
    }

    // % ra rarity (Common..Mythic) ở level ứng với tổng lượt — dùng cho popup Info.
    public static float GetRate(int totalSummons, Rarity rarity)
    {
        int r = (int)rarity;
        if (r < 0 || r >= RARITY_COUNT)
        {
            return 0f;
        }
        return RATES[GetRowIndex(totalSummons), r];
    }

    // Roll rarity cho 1 lượt (ly.gpt): Random[0,100) so cộng dồn từ Mythic xuống Common.
    public static Rarity RollRarity(int totalSummons)
    {
        int row = GetRowIndex(totalSummons);
        float roll = Random.Range(0f, 100f);
        float acc = 0f;
        for (int r = RARITY_COUNT - 1; r >= 1; r--)
        {
            acc += RATES[row, r];
            if (roll < acc)
            {
                return (Rarity)r;
            }
        }
        return Rarity.Common;
    }
}
