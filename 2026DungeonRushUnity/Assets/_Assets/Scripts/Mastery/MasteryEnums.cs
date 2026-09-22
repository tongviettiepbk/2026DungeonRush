// Enum hệ Mastery (G1) — hệ nâng cấp META vĩnh viễn bằng Ngọc (GEM).
// Giá trị = ĐÚNG enum MasteryUpgradeType của game gốc (reverse libil2cpp v41, TypeDefIndex 897).
// Data bản v41 chỉ ship 10/12 nhánh: THIẾU type 0 (GemOfferCount) và type 9 (LevelUpRewardWorth)
// — vẫn khai đủ enum để khớp giá trị số, nhưng 2 type đó không có asset.
//
// LƯU Ý: KHÁC hoàn toàn với StatModifierSource.MasteryCommon/MasteryPromotion trong
// MechanicEnums.cs — cái đó là bậc "mastery" của CARD trong battle, không liên quan hệ này.
public enum MasteryUpgradeType
{
    GemOfferCount = 0,         // (không ship trong v41)
    GemOfferChance = 1,        // % ra Ngọc khi mở Offer Chest
    AdBoostDuration = 2,       // kéo dài thời gian EXP Boost (xem quảng cáo)
    AdBoostWorth = 3,          // hệ số nhân EXP khi bật Boost
    MaxOfflineTime = 4,        // số giờ tối đa vẫn tích thưởng khi offline
    OfflineEarningWorth = 5,   // tốc độ tích tài nguyên khi offline
    PlayerMovementSpeed = 6,   // tốc chạy nhân vật trong dungeon
    CompanionSummonCount = 7,  // số companion mỗi lần triệu hồi
    AutoLootHammerCount = 8,   // số rương loot mở cùng lúc ở Auto Mode
    LevelUpRewardWorth = 9,    // (không ship trong v41)
    ForgeMaxItemLevel = 10,    // (AddedLater) nâng trần cấp tối đa trang bị khi Forge
    MiningMaxPickaxe = 11,     // (AddedLater) nâng trần số cuốc — hệ Đào mỏ
}
