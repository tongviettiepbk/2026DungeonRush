using System;

// Config tĩnh hệ EXP/LEVEL người chơi — REVERSE từ game gốc (xem DecodedData/EXP_MODEL.md).
//   - Exp thắng 1 màn (clear sạch quái) = round(ExperienceLevelBase + ExperienceLevelScaler * level).
//   - Ngưỡng lên level = experience_required_per_level[level-1] (bảng 100 dòng, KHÔNG cộng dồn).
// Nhúng thẳng giá trị (data cố định của game) thay vì load asset — tránh phụ thuộc .meta.
public class StaticExperienceData
{
    // GameResources.ExperienceLevelBase / ExperienceLevelScaler (remote config trùng: 49 / 1).
    public const float EXPERIENCE_LEVEL_BASE = 49f;
    public const float EXPERIENCE_LEVEL_SCALER = 1f;

    // experience_required_per_level (100 level). Nguồn: DecodedData/tables/experience_required_per_level.csv
    // (remote config ≡ APK). xpRequired[i] = exp cần để đi từ level (i+1) lên (i+2).
    private static readonly int[] xpRequired =
    {
        200, 600, 1200, 3000, 4500, 6000, 9000, 10800, 12600, 14400,
        16200, 18000, 19800, 21600, 23400, 25200, 27000, 28800, 30600, 36000,
        38000, 40000, 42000, 44000, 50600, 52800, 55000, 57200, 59400, 61600,
        63700, 69800, 76200, 82900, 89900, 97200, 104800, 112700, 121000, 129500,
        138400, 147700, 157200, 167200, 177400, 188000, 199000, 210300, 222000, 234000,
        246400, 259200, 272400, 285900, 299900, 314200, 328900, 344000, 359500, 375400,
        391700, 408400, 425500, 443100, 461000, 479400, 498200, 517500, 537100, 557200,
        577800, 598700, 620200, 642000, 664400, 687100, 710400, 734100, 758200, 782800,
        807900, 833500, 859500, 886000, 913000, 940500, 968500, 996900, 1025900, 1055300,
        1085200, 1115700, 1146600, 1178000, 1210000, 1242500, 1275400, 1308900, 1343000, 1377500,
    };

    // Level tối đa = số dòng bảng (100). Cũng = số dòng bảng rarity ForgeData → level cao nhất
    // rarity table còn cải thiện.
    public int MaxLevel => xpRequired.Length;

    // Exp cần để lên level tiếp (mirror ExperienceController.hdk: clamp index [0, len-1]).
    public int GetXpRequired(int level)
    {
        int index = level - 1;
        if (index < 0) index = 0;
        if (index >= xpRequired.Length) index = xpRequired.Length - 1;
        return xpRequired[index];
    }

    // Tổng exp nhận khi thắng 1 màn combatLevel = level (mirror ExperienceController.hdm).
    public int GetStageExp(int level)
    {
        double raw = EXPERIENCE_LEVEL_BASE + EXPERIENCE_LEVEL_SCALER * level;
        return (int)Math.Round(raw, MidpointRounding.ToEven);
    }
}
