---
name: dungonrush-events-tab
description: Tab Events (PvP + Boss Rush) — vé theo server + reset 0h UTC, icon vé TMP IsTicket/IsBossTicket; cả 2 mode PvP/BossRush đã có
metadata:
  type: project
  modified: 2026-10-02
---

**Tab Events (2026-10-02, test Play mode OK):** `Scripts/Events/` StaticEventData (EventConfig nhúng hằng số) + UserEventData (key `key_user_event`, dict theo (int)`EventModeType` {PvP=0, BossRush=1}, `lastResetTime` unix giây UTC) + EventService (CheckDailyReset/GetTimeToReset/IsUnlocked/GetTickets) — cùng pattern Dungeon.
Số liệu GỐC: unlock PvP **15**, Co-op 70, BossRush **15** (EventsTabPage.cctor đọc từ libil2cpp); PvP 5 vé free/ngày + 4 ads (PvPConfig.TicketConfig); BossRush 3 free + 3 ads (BossRushConfig.MaxFreeEntries/MaxAdEntries). Gốc vé do SERVER cấp (PvPTicketsDTO/BossRushTicketsDTO) → ở đây lưu local.
UI: `UITabEvent` (txtTimeRemain = RefreshTime — trước gán nhầm TitleText) + 2 `ElementEventModeUI` (txtQuantityStick=KeyCountText, objLock=LockOverlay, objNotice=NotificationUI, btJoin). Text `<sprite=0>{free+ad}/{dailyFree}` như prefab gốc; khoá → bật lock, ẩn text vé (gốc `ker`). Icon vé = TMP sprite asset `_ResourceGame/Currency/IsTicket` (ticket.png vàng vương miện, PvP) + `IsBossTicket` (bossticket.png đỏ đầu lâu), metrics glyph theo bản rip (scale 1.5, bearingY 94.4).
**CHƯA LÀM:** ~~mode PvP~~ ĐÃ CÓ 2026-10-10 (Join PvP → UIPvPPopup, vé PvP theo server, xem [[dungonrush-pvp-mode]]); BossRush ĐÃ có — vé BossRush lấy theo server, xem [[dungonrush-boss-rush-model]]), tiêu vé, ads lấy vé, Co-op.
Xem [[dungonrush-dungeon-system]].
