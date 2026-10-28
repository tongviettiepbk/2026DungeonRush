---
name: dungonrush-map-cell-snap
description: "MainMap bám ô (2026-10-28): lưới nền căn theo PointStart lúc chạy, tường viền = vòng ô quanh 9x12 (dày 1 ô, đoạn 2 ô), chướng ngại 1x1 ô; sprite PPU 256 scale 1"
metadata:
  type: project
---

User yêu cầu 2026-10-28: mỗi ô nền = đúng 1 ô map (9x12), chướng ngại = 1 ô nằm giữa ô, tường viền bám ô.

- Lưới nền: `Custom/GridShader` vẽ theo world → `MapController.AlignGridBackground()` (gọi trong SetMap) đặt `_Offset` qua MaterialPropertyBlock để đường kẻ nằm ở mép ô (tâm ô = CellToWorld). Chỉ áp renderer GridShader có `_GridSize` = 1. Trong Editor (chưa Play) lưới vẫn lệch, đó là bình thường.
- Ảnh `map_wall_v/h/corner/door/obstacle` sinh lại bằng `tools/reskin/gen_gameplay_map.py` với CELL=256 px = 1 ô, import **PPU 256**, transform **scale 1** (trước đó PPU 100 scale 0.44). Collider = cỡ sprite.
- MainMapPrefab: tường ở vòng ô hàng −1/12, cột −1/9. Trái/phải mỗi bên 6 đoạn × 2 ô, bulông giữa nằm trên mối nối hàng 5|6. Dưới: đoạn 0-1, 2-3, bulông cột 4 (`WallCornerPiece_MidBottom` mới), 5-6, 7-8. Trên: đoạn 0-1, bulông cột 2, cửa cột 3-5 (2 nửa, mỗi nửa 1.5 ô), bulông cột 6, đoạn 7-8. Đã xoá MainWallPiece_5/15/16. Tường dày 1 ô (mockup gốc 0.77 ô), vì user muốn bám ô.

**Why:** trước đó lưới nền neo theo toạ độ world nguyên nên lệch ô (~0.04 ngang, 0.17 dọc), tường hở ~0.06, chướng ngại 1.06 ô.
**How to apply:** muốn đổi cỡ/vị trí tường thì tính theo ô (tâm ô (0,0) = PointStart), đừng quay lại scale 0.44. Dungeon/BossRush prefab không đổi. Xem [[dungonrush-reskin-gameplay]], [[dungonrush-map-grid-background]].
