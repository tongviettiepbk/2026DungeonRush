// Chi phí NÂNG CẤP companion — REVERSE il2cpp v41 (class helper `ly`).
//   • Max level = 100        → native `ly$$gpx()` = `mov w0, #0x64`.
//   • Cards cần để lên 1 cấp  → native `ly$$gpz(int level)`: TRA BẢNG `int[]` (index = level), khi
//     level vượt độ dài bảng thì trả FALLBACK 16 (`mov w0, #0x10`).
//   → Bảng int[] gốc nằm trong ScriptableObject "companion manager" (singleton), CHƯA trích ra
//     (giống cách trích gameresources_values.json). Tạm dùng fallback 16 cho MỌI cấp; thay bằng
//     bảng thật khi extract. Xem DecodedData/COMPANION_MODEL.md.
public static class CompanionUpgradeConfig
{
    public const int MAX_LEVEL = 100;               // ly.gpx
    public const int DEFAULT_CARDS_PER_LEVEL = 16;  // ly.gpz fallback (0x10)

    // Số thẻ để lên từ `level` → `level+1`. Đã ở max → int.MaxValue (không lên nữa).
    // TODO(reverse): thay bằng bảng int[] thật khi trích được từ companion manager asset.
    public static int GetCardsRequired(int level)
    {
        if (level >= MAX_LEVEL)
        {
            return int.MaxValue;
        }

        return DEFAULT_CARDS_PER_LEVEL;
    }
}
