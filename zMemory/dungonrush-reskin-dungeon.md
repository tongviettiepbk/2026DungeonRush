---
name: dungonrush-reskin-dungeon
description: "Reskin Dungeon 2026-10-28 theo mockup _Concept/Dungeon (8 demo): trang DungeonPage trong UIMainGame2 + UIDungeonPopup + UIDungeonEndPopup + tấm DungeonUI trong trận; kit 1_UIKit/Dungeon; đổi tên EN Boss Raid/Zombie Horde/Dark Ritual; test Play OK, đã commit daaf59f; đợt 2 2026-11-07 đổi art 3 thẻ, chưa test Play, đã commit"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-27T17:57:39.102Z
  originSessionId: e139c75d-12a0-4072-8ff9-963226ba8bdd
---

Mockup + art giao: `ArtIncoming/_Concept/Dungeon/` (nguồn Telegram "Dungeon (2)"), `_art/` = 3 thẻ 990x486 + 3 ruy băng 152x106. Ảnh sinh bằng `tools/reskin/gen_dungeon.py` → `Art/1_UIKit/Dungeon/` (card_boss_raid/street_rush/mafia_crew, ribbon_red/purple/blue, tag_* = ruy băng lật ngang). So sánh: `_Concept/Dungeon/compare_page.png`, `compare_popups.png`. Test Play OK (tên, icon, số key), console 0 lỗi. Đã commit daaf59f (2026-10-28).

**User chốt:** đổi tên tiếng Anh: Dragon's Hoard→Boss Raid; 2026-11-09 đổi lại Street Rush→**Zombie Horde**, Mafia Crew→**Dark Ritual** (tên cũ không khớp lối chơi: bầy zombie cận chiến / giáo phái dao+cung); 9 ngôn ngữ khác cũng dịch theo tên mới (vd FR Horde de Zombies / Rituel Sombre); GIỮ icon chìa trên thẻ (đặt bên trái tag 2/2, mockup bỏ).

**Đã làm:**
- `SafeArea/Pages/DungeonPage` (trong UIMainGame2): nền #403F51, y = mockup −60 (header mockup cao hơn); thẻ = ảnh card đặt vào node `Mask` (thêm component Mask — node cũ thiếu), ảnh minh hoạ cũ tắt, Image gốc nút alpha 0; ruy băng theo màu thẻ đỏ/tím/xanh; thêm node `KeyTag`.
- `SafeArea/DungeonUI`: nền #998783, vạch đen + #AFA19E, Exit = Lobby/btn_red.
- 2 popup dùng kit Mastery (frame/header/X/box_tan/btn_blue); ruy băng popup luôn ĐỎ như mockup, icon ruy băng = icon thưởng theo dungeon (field mới `UIDungeonPopupStart.imgIconRibbon`). Nút Sweep/Enter/Okay transition SpriteSwap, disabled = Lobby/btn_gray (mockup Sweep xám = trạng thái khoá ở màn 1-1). Nút Ads = Lobby/btn_green.
- EndPopup: thêm `RewardLabel` ("Reward", ẩn khi thua qua field `objRewardLabel`) + ô `RewardBox` box_tan; chữ thưởng "<sprite=0><space=40>n".
- Phần mockup bỏ: hàng "+" pet trên map (game để ở thanh dưới), chữ nút "Enter" ở popup kết quả (giữ "Okay").

**Đợt 2 (2026-11-07, đã commit, chưa test Play):** mockup mới `_unRar/Dungeon/` (Dungeon demo (2) + Mode 1/2/3 = art thẻ Boss Raid/Street Rush/Mafia Crew 990x486, cổng đá/hầm mộ/ngục). Bố cục mockup y hệt đợt 1, nên chỉ chép đè art vào `_Concept/Dungeon/` + chạy gen_dungeon.py, giữ GUID, KHÔNG sửa prefab. Ảnh card chỉ dùng trong UIMainGame2. Giữ như cũ: icon thưởng trong ruy băng + icon chìa cạnh tag (mockup trống, user chốt đợt 1), chữ gốc đúng chính tả (mockup ghi sai "comsumed", "conplate"). Ảnh so: `_Concept/Dungeon/compare_page.png` (chụp prefab bằng batchmode, ổ khoá + đầu lâu trong ảnh là trạng thái mặc định prefab, code tự bật tắt).

Xem [[dungonrush-reskin-ui-howto]], [[dungonrush-dungeon-system]], [[dungonrush-reskin-plan]].

**2026-11-09:** ruy băng 2/2 kéo dài 152→260 (KeyTag x 361, Image Sliced, tag_*.png spriteBorder trái 34/phải 12), IconKey dời vào trong ruy băng (x −113); đã commit, chưa test Play (UnityMCP rớt), đã đo chữ 10 ngôn ngữ vừa ô 700px bằng font thật.
