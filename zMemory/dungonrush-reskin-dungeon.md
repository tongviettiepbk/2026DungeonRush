---
name: dungonrush-reskin-dungeon
description: "Reskin Dungeon 2026-10-28 theo mockup _Concept/Dungeon (8 demo): trang DungeonPage trong UIMainGame2 + UIDungeonPopup + UIDungeonEndPopup + tấm DungeonUI trong trận; kit 1_UIKit/Dungeon; đổi tên EN Boss Raid/Street Rush/Mafia Crew; test Play OK, đã commit daaf59f"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-27T17:57:39.102Z
  originSessionId: e139c75d-12a0-4072-8ff9-963226ba8bdd
---

Mockup + art giao: `ArtIncoming/_Concept/Dungeon/` (nguồn Telegram "Dungeon (2)"), `_art/` = 3 thẻ 990x486 + 3 ruy băng 152x106. Ảnh sinh bằng `tools/reskin/gen_dungeon.py` → `Art/1_UIKit/Dungeon/` (card_boss_raid/street_rush/mafia_crew, ribbon_red/purple/blue, tag_* = ruy băng lật ngang). So sánh: `_Concept/Dungeon/compare_page.png`, `compare_popups.png`. Test Play OK (tên, icon, số key), console 0 lỗi. Đã commit daaf59f (2026-10-28).

**User chốt:** đổi tên tiếng Anh theo mockup (Dragon's Hoard→Boss Raid, Zombie Outbreak→Street Rush, Cultist Ritual→Mafia Crew; 9 ngôn ngữ khác giữ tên cũ); GIỮ icon chìa trên thẻ (đặt bên trái tag 2/2, mockup bỏ).

**Đã làm:**
- `SafeArea/Pages/DungeonPage` (trong UIMainGame2): nền #403F51, y = mockup −60 (header mockup cao hơn); thẻ = ảnh card đặt vào node `Mask` (thêm component Mask — node cũ thiếu), ảnh minh hoạ cũ tắt, Image gốc nút alpha 0; ruy băng theo màu thẻ đỏ/tím/xanh; thêm node `KeyTag`.
- `SafeArea/DungeonUI`: nền #998783, vạch đen + #AFA19E, Exit = Lobby/btn_red.
- 2 popup dùng kit Mastery (frame/header/X/box_tan/btn_blue); ruy băng popup luôn ĐỎ như mockup, icon ruy băng = icon thưởng theo dungeon (field mới `UIDungeonPopupStart.imgIconRibbon`). Nút Sweep/Enter/Okay transition SpriteSwap, disabled = Lobby/btn_gray (mockup Sweep xám = trạng thái khoá ở màn 1-1). Nút Ads = Lobby/btn_green.
- EndPopup: thêm `RewardLabel` ("Reward", ẩn khi thua qua field `objRewardLabel`) + ô `RewardBox` box_tan; chữ thưởng "<sprite=0><space=40>n".
- Phần mockup bỏ: hàng "+" pet trên map (game để ở thanh dưới), chữ nút "Enter" ở popup kết quả (giữ "Okay").

Xem [[dungonrush-reskin-ui-howto]], [[dungonrush-dungeon-system]], [[dungonrush-reskin-plan]].
