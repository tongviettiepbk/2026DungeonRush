---
name: dungonrush-dungeon-map-visual
description: Hình ảnh 3 map dungeon (Zombie/Dragon/Cultist) đã dựng theo prefab rip gốc, không cửa; sprite ở _ResourceGame/Dungeon*; CHƯA mở Unity/test Play
metadata:
  type: project
  modified: 2026-10-17
---

2026-10-17 (commit de524e4): `Prefabs/Maps/{Zombie,Dragon,Cultist}MapPrefab.prefab` hết là bản sao MainMap — cây `Walls/Corners/Environments` lấy nguyên từ `AssetRipper/.../GameObject/<cùng tên>.prefab` (vị trí/rot/scale/flip/sortingOrder/màu gốc, fileID rip), **không có Doors** (gốc không có cửa).

- Giữ của project: root GO/Transform, MB `MapController` (pointStart), `PointStart` (0.52, 0.52), `MapOffset` local (4.5, 6.15). Con của MapOffset giữ vị trí tương đối gốc (gốc MapOffset = 0.48, 0.13). fileID root/MapOffset/InnerPlane rip == project nên mode prefab (`mapPrefab: {fileID: <root>}`) vẫn trỏ đúng.
- InnerPlane/OuterPlane: material grid RIÊNG từng dungeon + scale gốc (trước bị dùng material Main). Dragon đặt tên gốc `Outerplane`, `Walls_1`, `Corners_1`.
- SpriteRenderer/BoxCollider2D dựng theo khuôn Unity 6 của MainMapPrefab, chỉ chép giá trị từ rip.
- Sprite: ảnh gốc ĐÃ có sẵn trong `_ResourceGame/_CommingUse/` (trùng điểm ảnh với xapk) → `git mv` sang `_ResourceGame/Dungeon{Zombie,Dragon,Cultist}/` giữ guid (pentagram từ `_CommingUse/Fx`). Square = `_NonDefile/Square.png`. Tên sprite gốc: `tomb_stone`, `lava_corner`, `lava_middle`, `pentagram`, `wall_piece_<x>_dungeon`, `wall_corner_piece_<x>_dungeon`...
- Dragon: tường ngoài `outer_wall_piece_dragon_dungeon` ord=0 và lava ord=-25 đúng gốc.
- Kiểm: 0 fileID treo, 0 guid thiếu, render PIL khớp bố cục. CHƯA mở Unity (UnityMCP rớt) / CHƯA test Play.

Liên quan: [[dungonrush-map-grid-background]], [[dungonrush-dungeon-system]], [[dungonrush-campaign-map-original]], [[dungonrush-ripped-prefab-rewire]].
