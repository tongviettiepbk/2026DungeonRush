using System;

// Config tĩnh hệ TRIỆU HỒI Companion — REVERSE từ game gốc v41 (xem DecodedData/COMPANION_MODEL.md).
// Nhúng thẳng giá trị (data cố định của game) như StaticExperienceData, tránh phụ thuộc .meta.
//
// 3 cách summon, đều RA THẺ (cộng CardCount → đủ thì lên Level companion), trang bị tối đa 3 con.
// Nguyên liệu = ItemType.BONE (reward dungeon Zombie Outbreak). KHÔNG dùng Gem.
//
// Số con mỗi lượt luôn CỘNG THÊM "Summon Capacity" (mastery CompanionSummonCount, làm tròn banker's):
//   count = round(summonCapacity) + base            (hàm gốc CompanionTabPage.guc)
// Mastery: default 0; mua lvl1 tốn 60 gem → Value 1; các cấp sau Value 2..5. Người chơi thực tế
// (đã có Value 1) thấy: ad 12 / Bone 100→16 / Bone 200→36. Account chưa mua (0): 11 / 15 / 35.
public class CompanionSummonConfig
{
    // --- Mở khoá tab Companion (TabData.UnlockPlayerLevel, TabId=5, trong GameplayScene gốc) ---
    // Gate = playerLevel >= mốc này. (Các tab khác: Store 3, Dungeon 4, Events 15, Clan 20, Battle 0.)
    public const int COMPANION_UNLOCK_PLAYER_LEVEL = 5;

    // --- Nút Bone NHỎ (SummonBoneSmallButton): cố định ---
    public const int BONE_SMALL_COST = 100;
    public const int BONE_SMALL_BASE_COUNT = 15;

    // --- Nút Bone LỚN (SummonBoneButton): nhân theo multiplier (nút ChangeSummonBoneMultiplier) ---
    // cost = multiplier * 200 ; base = multiplier * 35 ; multiplier x1 = mặc định.
    public const int BONE_BIG_COST_PER_MULTIPLIER = 200;
    public const int BONE_BIG_BASE_COUNT_PER_MULTIPLIER = 35;

    // --- Summon bằng quảng cáo (CompanionAdSummonPopup, hàm gốc eby) ---
    //   count = min( round(cap) + adBonus + AD_BASE_BONUS , round(cap) + AD_CAP_OVER_CAPACITY )
    public const int AD_BASE_BONUS = 11;
    public const int AD_CAP_OVER_CAPACITY = 35;

    // --- Mastery "Summon Capacity" (CompanionSummonCount): giá trị theo cấp đã nâng ---
    // index 0 = chưa mua (default). Value[level]. GemCost mở/nâng: 60/120/160/200/250.
    public const int MASTERY_UNLOCK_GEM_COST = 60;
    public static readonly int[] SUMMON_CAPACITY_VALUE = { 0, 1, 2, 3, 4, 5 };
    public static readonly int[] SUMMON_CAPACITY_GEM_COST = { 0, 60, 120, 160, 200, 250 };

    // Giá trị Summon Capacity hiện tại theo cấp mastery (clamp).
    public int GetSummonCapacity(int masteryLevel)
    {
        if (masteryLevel < 0) masteryLevel = 0;
        if (masteryLevel >= SUMMON_CAPACITY_VALUE.Length) masteryLevel = SUMMON_CAPACITY_VALUE.Length - 1;
        return SUMMON_CAPACITY_VALUE[masteryLevel];
    }

    // Sức chứa Summon ĐANG tác dụng, đọc trực tiếp từ hệ Mastery (nhánh CompanionSummonCount).
    // Dùng thay cho việc truyền tay masteryLevel; trả 0 khi nhánh chưa mở khoá.
    public int GetCurrentSummonCapacity()
    {
        return (int)Math.Round(MasteryService.GetCurrentValue(MasteryUpgradeType.CompanionSummonCount),
                               MidpointRounding.ToEven);
    }

    // Số con thực nhận cho 1 gói Bone (mirror CompanionTabPage.guc): round(cap) + base.
    // capacity là int (giá trị mastery) nên round = chính nó; giữ hàm Round để khớp gốc.
    public int GetSummonCount(int baseCount, int summonCapacity)
    {
        int cap = (int)Math.Round((double)summonCapacity, MidpointRounding.ToEven);
        return cap + baseCount;
    }

    // Số con của nút Bone NHỎ (100 Bone).
    public int GetBoneSmallSummonCount(int summonCapacity)
    {
        return GetSummonCount(BONE_SMALL_BASE_COUNT, summonCapacity);
    }

    // Số con của nút Bone LỚN theo multiplier (x1 mặc định). cost = multiplier*200.
    public int GetBoneBigSummonCount(int summonCapacity, int multiplier)
    {
        if (multiplier < 1) multiplier = 1;
        return GetSummonCount(BONE_BIG_BASE_COUNT_PER_MULTIPLIER * multiplier, summonCapacity);
    }

    public int GetBoneBigCost(int multiplier)
    {
        if (multiplier < 1) multiplier = 1;
        return BONE_BIG_COST_PER_MULTIPLIER * multiplier;
    }

    // Số con khi summon bằng quảng cáo (mirror UserController.eby).
    public int GetAdSummonCount(int summonCapacity, int adBonusCount)
    {
        int cap = (int)Math.Round((double)summonCapacity, MidpointRounding.ToEven);
        int a = cap + adBonusCount + AD_BASE_BONUS;
        int b = cap + AD_CAP_OVER_CAPACITY;
        return a < b ? a : b;
    }
}
