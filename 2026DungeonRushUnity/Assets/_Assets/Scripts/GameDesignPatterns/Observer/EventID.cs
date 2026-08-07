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

    // TODO(follow-stick): thêm dần event khi port các hệ thống khác từ StickIdle.
}
