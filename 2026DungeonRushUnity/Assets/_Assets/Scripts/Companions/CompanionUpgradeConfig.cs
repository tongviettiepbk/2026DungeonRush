// Chi phí NÂNG CẤP companion — REVERSE il2cpp v41 (static class `ly`).
//   • Max level = 100        → native `ly$$gpx()` = `mov w0, #0x64`.
//   • Cards cần để lên 1 cấp  → native `ly$$gpz(int level)`: tra mảng static `whn` (int[15], InitializeArray
//     trong ly.cctor, data ở global-metadata offset 0x79DBC8); level <= 0 → whn[1]; level >= 15 → 16.
//   • Có nâng được không    → `ly$$gqa(level, cards)` = cards >= gpz(level). Nâng cấp THỦ CÔNG, summon chỉ +thẻ.
// Xem DecodedData/COMPANION_MODEL.md §7-8.
public static class CompanionUpgradeConfig
{
    public const int MAX_LEVEL = 100;               // ly.gpx
    public const int DEFAULT_CARDS_PER_LEVEL = 16;  // ly.gpz fallback (0x10)

    // ly.whn — index = level hiện tại (index 0 không dùng).
    private static readonly int[] CARDS_PER_LEVEL = { 0, 2, 3, 3, 3, 4, 4, 5, 5, 6, 7, 8, 10, 11, 13 };

    // Số thẻ để lên từ `level` → `level+1`. Đã ở max → int.MaxValue (không lên nữa).
    public static int GetCardsRequired(int level)
    {
        if (level >= MAX_LEVEL)
        {
            return int.MaxValue;
        }

        if (level <= 0)
        {
            return CARDS_PER_LEVEL[1];
        }

        if (level >= CARDS_PER_LEVEL.Length)
        {
            return DEFAULT_CARDS_PER_LEVEL;
        }

        return CARDS_PER_LEVEL[level];
    }

    // ly.gqa
    public static bool CanUpgrade(int level, int cardCount)
    {
        return cardCount >= GetCardsRequired(level);
    }
}
