public enum EventID
{
    None = 0,

    // Ingame
    ResetMode,
    EndGame,
    UnitDie,

    // Localize
    ChangeLanguage,

    // TODO(follow-stick): thêm dần event khi port các hệ thống khác từ StickIdle.
}
