---
name: dungonrush-reskin-dungeon-maps
description: "3 map dungeon (Boss Raid/Zombie Horde/Dark Ritual) 2026-11-07 đổi sang art liền tấm: sàn + tường 1 ảnh, nền 1 ảnh, bỏ node trang trí cũ; test Play OK, chưa commit; 20 asset cũ của Zombie/Cultist chưa xoá"
metadata:
  type: project
  modified: 2026-11-07
---

Art user giao: `ArtIncoming/_Concept/Dungeon{Dragon,Zombie,Cultist}Map/` (`map.*` ~620x790 = lưới 9x12 + vòng tường 1 ô, ô ~56 px; `bg.*` rộng 1080). Sinh bằng `tools/reskin/gen_dungeon_map.py <repo> dragon|zombie|cultist` → `Art/5_Maps/Dungeon*/<tên>_map.png` (1424x1808, 128 px = 1 ô, tâm ảnh = tâm lưới, PPU 128) + `<tên>_map_bg.png` (1620x1620, PPU 64.2857). Bảng `MAPS` trong script giữ số đo mép lưới từng map.

**Số đo màn:** camera cố định ortho 16.8, Game view 1:2 → rộng 16.8 ô, vùng thấy map (giữa HUD và thanh dưới) ~16.8×17 ô → phần ảnh gốc rộng đúng 16.8 ô, dải hai bên map vừa đồ trang trí. Tâm nền world (4.5, 5.1).

**Prefab:** dùng lại node `InnerPlane` (ảnh map, order -23, local (0.02,-0.13) dưới MapOffset) + `OuterPlane`/`Outerplane` (ảnh nền, order -26, local (0,0.45)), scale 1, material Sprites-Default. Xoá mọi node trang trí trong Environments. `Walls`/`Corners` giữ BoxCollider2D, bỏ SpriteRenderer.

**Chướng ngại:** Zombie/Cultist đổi màu theo map bằng cách cắt 1 cột tường trong map.png, ghi đè `zombie_dungeon_obstacle.png`/`cultist_dungeon_obstacle.png` giữ cỡ ảnh + guid (hộp `post` trong script, cắt sát viền đen kẻo dính lưới/dải vàng).

**Rìa nền:** phản chiếu nhìn giả trên iPad → Codex outpaint từ ảnh đặt giữa canvas hồng 1620² (`gen/prompts.txt`, ref + out trong `gen/`), script chỉnh rìa theo trung vị màu ảnh gốc (Codex lệch ~5 mức, không chỉnh thì lộ khung chữ nhật) rồi dán lại ảnh gốc vào giữa, mép mờ 32 px. Street Rush bg chỉ cao 889 → điện thoại thấy cả phần Codex vẽ.

**Bẫy test:** ảnh gửi giữa lượt có thể không xuống thư mục images của phiên → lấy base64 trong transcript `~/.claude/projects/<project>/<session>.jsonl`. Trận dungeon thắng rất nhanh, `CompleteDungeon` ghi save thật → đặt `UserData.isDisableSaveDataLocal = true` SAU Login trước khi vào, dừng Play phải set lại false. `execute_code` chặn `AssetDatabase.DeleteAsset`, chỉ dùng `safety_checks=false` khi user đã đồng ý xoá.

**Còn lại:** đã xoá art cũ Dragon. Chưa xoá 8 ảnh Zombie, 10 ảnh Cultist + `_ZombieGridMaterial_Inner/Outer` (cả 2 map dùng chung), đã mồ côi, chờ user. BossRushMapPrefab chưa đổi. Lỗi riêng: NullReference `GameController.InitGame:54` mỗi lần vào MainGame, stack không qua code map, game vẫn chạy, chưa tra.

Xem [[dungonrush-dungeon-map-visual]], [[dungonrush-reskin-dungeon]], [[dungonrush-reskin-ui-howto]].
