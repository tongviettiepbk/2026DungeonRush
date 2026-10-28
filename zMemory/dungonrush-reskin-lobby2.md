---
name: dungonrush-reskin-lobby2
description: "Lobby đợt 3 theo mockup \"lobby demo 4\" (2026-10-28) — giữ map, nén phần dưới; 10 khung rarity mới; thanh rương, tab có chữ + ô vàng; hộp avatar/gem CẮT TỪ MOCKUP chờ art thật; đã commit 3b12c24"
metadata:
  node_type: memory
  type: project
  originSessionId: 157c2389-4f43-463d-bd38-4181a28959e3
  modified: 2026-10-27T17:27:57.812Z
---

Mockup + art giao: `ArtIncoming/_Concept/Lobby2/` (nguồn `/Volumes/Work/_AssetReskin/DungeonRush/lobby (2)/`). Ảnh sinh bằng `tools/reskin/gen_lobby2.py`. Ảnh so: `_Concept/Lobby2/compare_mockup_vs_game.png`. Test Play OK, console 0 lỗi; ĐÃ commit 3b12c24 (2026-10-28).

**User chốt:**
- Mockup đặt hàng pet cao hơn game ~190px → GIỮ map (camera không đổi), nén phần dưới: tab 168, chat 62, BottomUI 600 (đặt tay, KHÔNG theo y mockup).
- Loot thường = btn_red, khoá = btn_gray (ForgeButtonView.lockedTopSprite).
- Thanh tab: thêm chữ (LocalizeText key gốc `UI.TabBar.Store/Companion/Dungeon/Events/Clan`), tab mở = ô vàng `tab_selected` (node CloseButton = objSelect, icon X ẩn).

**Đã làm:** 10 khung rarity ghi đè (bậc 7/8 có art mới copy 15/16; bậc 11 pha từ khung tím mới → #6E4BC4), auto-forge + icon_boost = art 09_auto/10_boost, panel_companion đổi xám #706F7D, ô đồ cột 218/414/608/853 (ẩn vạch chia cột 3|4), 3 nút 272x136, ChestBar (con ForgeUI ngay sau Platform — để trong BottomUI bị Platform che) chứa số vé loot + giờ Boost, chat = chat_off, ô gem GemBg, nắm đấm cạnh power (BossRush/icon_fist), vé Battle Pass 96px.

**Cắt ghép tạm (báo user chờ art):** `1_UIKit/Lobby/profile_box.png` (cắt mép + cột fill từ mockup), `gem_box.png` (nửa phải ô mockup lật đôi), tier_11 pha màu.
**Chưa dùng:** rương xanh đóng/mở (exec-0761/fa26), bong bóng chat thường (Ellipse 1132 copy 2); huy chương leaderboard trong mockup bỏ vì LeaderboardButton gốc tắt.

**Why:** user re-layout lobby bằng mockup mới. **How to apply:** sửa tiếp lobby thì sửa prefab UIMainGame2 trực tiếp, KHÔNG chạy lại builder; xem [[dungonrush-reskin-ui-howto]], [[dungonrush-main-ui-uimaingame2]].
