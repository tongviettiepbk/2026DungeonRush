---
name: dungonrush-reskin-events
description: "Reskin tab Events 2026-10-28, đợt 2 2026-11-07 đổi art 2 thẻ + Boss Rush sang ruy băng xanh dương: EventPage trong UIMainGame2, 2 thẻ PvP + Boss Rush (Co-op giữ TẮT như gốc — user chốt), kit 1_UIKit/Events; chưa test Play, đợt 2 đã commit"
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

**Đợt 2 (2026-11-07, đã commit, chưa test Play):** mockup mới `_unRar/Event/` (Events demo + Mode 1/2 = art thẻ PvP/Boss Rush 990x486, đã chép đè vào `_Concept/Events/`, chạy lại gen_events.py). Bố cục mockup vẫn 2 thẻ, khớp quyết định tắt Co-op. Thay đổi: card_pvp + card_boss_rush art mới (nghĩa trang cờ, đền cổng xoáy; giữ GUID, không sửa prefab); Boss Rush ruy băng + ô vé từ tím sang ribbon_blue/tag_blue; Ribbon 286→271 (x -359), TitleText 218 (x -372); KeyTag + KeyCountText thẻ Boss Rush xuống 5px (-150.5 / -147). Sửa thẳng YAML prefab vì MCP tắt. card_coop vẫn art thành phố cũ; ribbon_purple, tag_purple giờ không dùng.
Giữ khác mockup có chủ ý: KeyTag rộng 222 (mockup 151, ô trống không có vé) để chứa icon + số vé; toàn trang cao hơn mockup 59px vì header lobby thấp hơn (đo được 59 bằng so ảnh). Chụp: `Assets/Editor` script tạm + `UICaptureTool` qua `Unity -batchmode -executeMethod ... -quit` khi Unity đã đóng. Nút đầu lâu BossButton trong ảnh preview là node của prefab, đừng nhầm là phần mới.

Xem [[dungonrush-reskin-dungeon]], [[dungonrush-events-tab]], [[dungonrush-reskin-ui-howto]].
