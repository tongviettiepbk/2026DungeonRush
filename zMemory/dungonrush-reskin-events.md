---
name: dungonrush-reskin-events
description: "Reskin tab Events 2026-10-28 theo mockup _Concept/Events: EventPage trong UIMainGame2, 2 thẻ PvP + Boss Rush (Co-op giữ TẮT như gốc — user chốt), kit 1_UIKit/Events; chưa test Play, đã commit"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-28T16:13:47.467Z
  originSessionId: 19c05334-2085-4f4a-acd9-0433d04a5cf4
---

Mockup + art giao: `ArtIncoming/_Concept/Events/` (nguồn Telegram "Events"), `_art/` = 3 thẻ 990x486 (Layer 738 = PvP, copy = Co-op, copy 2 = Boss Rush). Ảnh sinh bằng `tools/reskin/gen_events.py` → `Art/1_UIKit/Events/` (card_pvp/coop/boss_rush; ribbon_* = bản sao ruy băng kit Dungeon, 9-slice L20/R52 để giãn theo chữ; tag_* = lật ngang, 9-slice L52/R20). So sánh: `_Concept/Events/compare_page.png`.

**User chốt:** thẻ CO-OP trong mockup → GIỮ TẮT như gốc (chỉ 2 thẻ PvP, Boss Rush ở vị trí thẻ 1-2 mockup); card_coop.png có sẵn nhưng chưa dùng.

**Đã làm (sửa prefab trực tiếp qua MCP):** `SafeArea/Pages/EventPage`: nền #403F51, tắt VerticalLayoutGroup (con đặt tay, neo giữa, anchoredY = 1107.4 − y mockup — giống Dungeon, trang game cao hơn mockup ~59px vì header thấp hơn); Title/RefreshTime Asap+viền đen 62/38; nút gốc alpha 0, 990x486; Mask = card + component Mask, ảnh minh hoạ cũ tắt; node mới `Ribbon` (PvP rộng 152, Boss Rush 286) + TitleText giữa thân ruy băng (52, autosize); node mới `KeyTag` 222x106 sát mép phải + KeyCountText ở giữa (vé IsTicket/IsBossTicket giữ nguyên).
Chụp bằng preview scene tự dựng (Instantiate riêng node EventPage, khung Pages y 104.8→1992) vì lúc đó Unity đang Play.

Xem [[dungonrush-reskin-dungeon]], [[dungonrush-events-tab]], [[dungonrush-reskin-ui-howto]].
