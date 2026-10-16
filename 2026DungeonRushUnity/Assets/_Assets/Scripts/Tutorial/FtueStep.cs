// Id step tutorial (FTUE) — giá trị = số nguyên game gốc truyền vào UserController.efw/efx (reverse il2cpp v41).
// Gốc không có enum, lưu thẳng int vào User.CompletedFtueSteps → giữ ĐÚNG số để đối chiếu.
// v41 không dùng id 14 / 16 / 18 / 19. Xem DecodedData/TUTORIAL_MODEL.md.
public enum FtueStep
{
    Loot = 1,                   // bấm Loot lần đầu
    LevelUp = 2,                // bấm Complete trong LevelPopup
    AutoForgeClicked = 3,       // bấm nút AutoForge lần đầu (chỉ đánh dấu)
    BoostClicked = 4,           // bấm Boost lần đầu (chỉ đánh dấu)
    AutoForgeKeepUnchecked = 5, // bỏ tick Keep trong AutoForgePopup (chỉ đánh dấu)
    EnterDragonDungeon = 6,
    EnterZombieDungeon = 7,
    SummonCompanion = 8,
    EnterMine = 9,              // chỉ đánh dấu
    CraftWing = 10,             // chỉ đánh dấu
    BuyStoreBoost = 11,         // chỉ đánh dấu
    StartAutoForge = 12,
    QuickEquipCompanion = 13,
    UnlockMastery = 15,         // mở khoá nhánh Mastery AutoLootHammerCount
    UpgradeMastery = 17,        // nâng nhánh AutoLootHammerCount lên level ≥ 2
    SelectHammerCount = 20,     // chọn ≥ 2 búa trong AutoForgePopup
    ReopenDismissedItem = 21,   // bấm lại item bị bỏ qua trong list Forge
    BossButton = 22,            // bấm nút Boss campaign
}
