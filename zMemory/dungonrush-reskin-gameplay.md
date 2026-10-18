---
name: dungonrush-reskin-gameplay
description: "Reskin gameplay 2026-10-18 (CHƯA commit) — map campaign tường/sàn vẽ code, quái vịt đen ChatGPT trên rig hero, 3 popup NewItem/NewForge/WeaponOffer kit Mastery"
metadata:
  node_type: memory
  type: project
  originSessionId: 818567fe-3961-45c6-8305-ebbdb5f8065c
  modified: 2026-10-18T15:24:04.404Z
---

Mockup + 4 ảnh art giao ở `ArtIncoming/_Concept/Gameplay/` (gameplay 1/2 demo, Weapon offer demo; art = 2 mũi tên, tag Equipped, ruy băng cam). Ảnh so: `compare_map.png`, `compare_popups.png`. Làm 2026-10-18, CHƯA commit, user chưa duyệt.

- MAP (chỉ MainMapPrefab; BossRush/dungeon dùng ảnh cũ, KHÔNG đổi): `tools/reskin/gen_gameplay_map.py` vẽ khối vát theo số đo mockup (nền #848798, viền đen, vát sáng trên-trái/tối dưới-phải, bulông) → `Art/5_Maps/Campaign/map_wall_v|map_wall_h|map_wall_corner|map_door|map_obstacle.png` (giữ đúng cỡ pixel sprite cũ). Tường ngang bỏ xoay 90 → sprite ngang riêng + đổi trục BoxCollider2D (sprite dọc xoay thì vát sáng nằm sai phía). Màu tường bỏ tint #333333 → trắng. Thêm 2 khối bulông giữa cạnh trái/phải (WallCornerPiece_MidLeft/Right). Material Main Inner bg #76767E line #84848A width 0.04; Outer #998783. `MainObstacle.prefab` → map_obstacle (sprite stone_obstacle cũ còn dùng ở MiningConfig).
- QUÁI: ChatGPT (codex-batch, đính ref = crop mockup) vẽ nguyên con vịt mũ ngược/kính/áo da/dây chuyền → `Art/4_Characters/Enemies/enemy_duck.png` (PPU 270). `00Enemy` dựng lại theo rig hero: bỏ Body/tay top-down, chép `shadow` + `main` của 00Hero (đồ trang bị để sprite rỗng), Animator → unit_all, thêm UnitAnimatorController, collider offset 0.75 + **Soldier localPosition (0,-0.75) như hero** (thiếu thì quái nổi cao 0.75 ô). Prompt ở `_Concept/Gameplay/gen/`.
- POPUP (Resources/Prefabs/UI): khung `Mastery/popup_frame`, thẻ món mới `Mastery/box_tan` (Image NewItem bị TẮT sẵn trong prefab → phải bật), nút Equip `Mastery/btn_blue`, Sell `1_UIKit/Gameplay/btn_red` (fit màu từ Equip→Sell trên mockup, giữ viền tối), tag/ruy băng/mũi tên = ảnh art ở `1_UIKit/Gameplay/` (`tools/reskin/gen_gameplay_popups.py`). NewItem + NewForge bỏ lớp tối (mockup không có), Offer giữ. `UIOfferPopup.RibbonImage = null` → ruy băng cam cố định (gốc tô màu rarity). Mũi tên so chỉ số do code đặt sau chữ → pivot trái. Mockup có mũi tên ở CẢ thẻ đang mặc, code chỉ có 1 (thẻ mới) → chưa thêm.

**Why:** user giao mockup gameplay, bảo vẽ phần thiếu bằng ChatGPT; icon pet để user tự làm sau.
**How to apply:** đừng chạy lại `ForgePrefabBuilder`/`WeaponOfferPrefabBuilder` (ghi đè). Pet trên map vẫn sprite cũ (chờ user). Xem [[dungonrush-reskin-plan]], [[dungonrush-reskin-ui-howto]], [[dungonrush-forge-auto-loot]].
