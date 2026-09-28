public enum EventID
{
    None = 0,

    // Ingame
    ResetMode,
    EndGame,
    UnitDie,

    // Localize
    ChangeLanguage,

    // Trang bị: người chơi đổi món ở 1 slot (param = GearSlotType) → Hero mặc lại slot đó.
    EquipmentChanged,

    // Companion: danh sách pet SỞ HỮU / level đổi (summon, nâng cấp) → Hero tính lại Own Effect.
    CompanionOwnedChanged,

    // TODO(follow-stick): thêm dần event khi port các hệ thống khác từ StickIdle.
}
