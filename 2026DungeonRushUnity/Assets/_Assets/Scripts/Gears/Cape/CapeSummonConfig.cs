using UnityEngine;

// Bảng summon Cape — mirror static class `ft` gốc (il2cpp v41, đọc từ .cctor).
//   - Summon Level = số mốc TotalCapeSummons đã vượt + 1 (ft.eqp), tối đa 50.
//   - Mỗi level 1 hàng tỉ lệ % cho 6 rarity Common..Mythic (ft.eqq / eqr), tổng 100.
//   - Roll: Random.Range(0,100) cộng dồn từ rarity CAO xuống thấp (ft.eqo).
//   - Multiplier nút summon: [1,2,3,5,10,20,30] (CapePopup.uoj), chỉ hiện khi Cloak > 99.
// Số liệu đầy đủ: DecodedData/CAPE_MODEL.md.
public static class CapeSummonConfig
{
    public static readonly int[] MULTIPLIERS = { 1, 2, 3, 5, 10, 20, 30 };
    public const int MULTIPLIER_UNLOCK_CLOAK = 100;

    private struct Row
    {
        public readonly int threshold;   // TotalCapeSummons < threshold → đang ở hàng này
        public readonly float[] rates;   // % theo Rarity 0..5

        public Row(int threshold, params float[] rates)
        {
            this.threshold = threshold;
            this.rates = rates;
        }
    }

    private static readonly Row[] ROWS =
    {
        new Row(2, 100f, 0f, 0f, 0f, 0f, 0f),
        new Row(7, 99.5f, 0.5f, 0f, 0f, 0f, 0f),
        new Row(15, 99.31f, 0.69f, 0f, 0f, 0f, 0f),
        new Row(26, 99.05f, 0.95f, 0f, 0f, 0f, 0f),
        new Row(40, 98.69f, 1.31f, 0f, 0f, 0f, 0f),
        new Row(57, 98.19f, 1.81f, 0f, 0f, 0f, 0f),
        new Row(77, 97.5f, 2.5f, 0f, 0f, 0f, 0f),
        new Row(100, 96.55f, 3.45f, 0f, 0f, 0f, 0f),
        new Row(126, 95f, 5f, 0f, 0f, 0f, 0f),
        new Row(155, 92.9f, 7f, 0.1f, 0f, 0f, 0f),
        new Row(187, 90.02f, 9.8f, 0.18f, 0f, 0f, 0f),
        new Row(222, 85.96f, 13.72f, 0.32f, 0f, 0f, 0f),
        new Row(260, 80.21f, 19.21f, 0.58f, 0f, 0f, 0f),
        new Row(301, 72.06f, 26.89f, 1.05f, 0f, 0f, 0f),
        new Row(345, 60.46f, 37.65f, 1.89f, 0f, 0f, 0f),
        new Row(392, 43.89f, 52.71f, 3.4f, 0f, 0f, 0f),
        new Row(442, 35.11f, 59.89f, 5f, 0f, 0f, 0f),
        new Row(495, 28.09f, 64.81f, 7f, 0.1f, 0f, 0f),
        new Row(551, 22.47f, 67.55f, 9.8f, 0.18f, 0f, 0f),
        new Row(610, 17.98f, 67.98f, 13.72f, 0.32f, 0f, 0f),
        new Row(672, 16.5f, 63.71f, 19.21f, 0.58f, 0f, 0f),
        new Row(737, 16.5f, 55.56f, 26.89f, 1.05f, 0f, 0f),
        new Row(805, 16.5f, 43.96f, 37.65f, 1.89f, 0f, 0f),
        new Row(876, 16.5f, 27.39f, 52.71f, 3.4f, 0f, 0f),
        new Row(950, 16.5f, 16.5f, 62f, 5f, 0f, 0f),
        new Row(1027, 16.5f, 16.5f, 59.9f, 7f, 0.1f, 0f),
        new Row(1107, 16.5f, 16.5f, 57.02f, 9.8f, 0.18f, 0f),
        new Row(1190, 16.5f, 16.5f, 52.96f, 13.72f, 0.32f, 0f),
        new Row(1276, 16.5f, 16.5f, 47.21f, 19.21f, 0.58f, 0f),
        new Row(1365, 16.5f, 16.5f, 39.06f, 26.89f, 1.05f, 0f),
        new Row(1457, 16.5f, 16.5f, 27.46f, 37.65f, 1.89f, 0f),
        new Row(1552, 16.5f, 16.5f, 16.5f, 47.1f, 3.4f, 0f),
        new Row(1650, 16.5f, 16.5f, 16.5f, 45.5f, 5f, 0f),
        new Row(1782, 16.5f, 16.5f, 16.5f, 43.4f, 7f, 0.1f),
        new Row(1948, 16.5f, 16.5f, 16.5f, 40.52f, 9.8f, 0.18f),
        new Row(2148, 16.5f, 16.5f, 16.5f, 36.46f, 13.72f, 0.32f),
        new Row(2382, 16.5f, 16.5f, 16.5f, 30.71f, 19.21f, 0.58f),
        new Row(2650, 16.5f, 16.5f, 16.5f, 22.56f, 26.89f, 1.05f),
        new Row(2952, 16.5f, 16.5f, 16.5f, 16.5f, 32.11f, 1.89f),
        new Row(3288, 16.5f, 16.5f, 16.5f, 16.5f, 30.6f, 3.4f),
        new Row(3658, 16.5f, 16.5f, 16.5f, 16.5f, 29f, 5f),
        new Row(4062, 16.5f, 16.5f, 16.5f, 16.5f, 28.3f, 5.7f),
        new Row(4500, 16.5f, 16.5f, 16.5f, 16.5f, 27.5f, 6.5f),
        new Row(4972, 16.5f, 16.5f, 16.5f, 16.5f, 26.59f, 7.41f),
        new Row(5478, 16.5f, 16.5f, 16.5f, 16.5f, 25.56f, 8.44f),
        new Row(6018, 16.5f, 16.5f, 16.5f, 16.5f, 24.37f, 9.63f),
        new Row(6592, 16.5f, 16.5f, 16.5f, 16.5f, 23.03f, 10.97f),
        new Row(7200, 16.5f, 16.5f, 16.5f, 16.5f, 21.49f, 12.51f),
        new Row(7842, 16.5f, 16.5f, 16.5f, 16.5f, 19.74f, 14.26f),
        new Row(int.MaxValue, 17.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f),
    };

    public static int MaxLevel => ROWS.Length;

    // ft.eqp: level 1-based (vượt hết mốc → MaxLevel).
    public static int GetLevel(int totalSummons)
    {
        for (int i = 0; i < ROWS.Length; i++)
        {
            if (ROWS[i].threshold > totalSummons)
            {
                return i + 1;
            }
        }
        return ROWS.Length;
    }

    // ft.equ: tiến độ trong level hiện tại (current/required); vượt mốc cuối (không tính hàng int.Max) → isMax.
    public static void GetProgress(int totalSummons, out int current, out int required, out bool isMax)
    {
        for (int i = 0; i < ROWS.Length - 1; i++)
        {
            if (ROWS[i].threshold > totalSummons)
            {
                int prev = i > 0 ? ROWS[i - 1].threshold : 0;
                current = totalSummons - prev;
                required = ROWS[i].threshold - prev;
                isMax = false;
                return;
            }
        }

        current = 0;
        required = 0;
        isMax = true;
    }

    // ft.eqr: tỉ lệ của level (1-based, kẹp trong 1..MaxLevel).
    public static float[] GetRates(int level)
    {
        return ROWS[Mathf.Clamp(level - 1, 0, ROWS.Length - 1)].rates;
    }

    // ft.eqo: roll rarity theo hàng của TotalCapeSummons hiện tại.
    public static Rarity RollRarity(int totalSummons)
    {
        float[] rates = GetRates(GetLevel(totalSummons));
        float roll = Random.Range(0f, 100f);
        float acc = 0f;
        for (int i = rates.Length - 1; i >= 0; i--)
        {
            acc += rates[i];
            if (roll < acc)
            {
                return (Rarity)i;
            }
        }
        return Rarity.Common;
    }
}
