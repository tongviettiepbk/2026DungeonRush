---
name: assetripper-export-no-meta
description: AssetRipper ExportedProject không có file .meta — GUID reference phải khôi phục bằng UnityPy đọc data gốc trong xapk
metadata: 
  node_type: memory
  type: project
  originSessionId: f6f65ac4-f26e-409c-bfa6-97606843026f
  modified: 2026-07-23T07:56:21.470Z
---

`AssetRipper/ExportedProject` (rip của Dungeon Rush v41) **không có file .meta nào** → mọi reference guid trong prefab/material bị "mồ côi". `AuxiliaryFiles/path_id_map.json` KHÔNG chứa guid mapping cho Material/GameObject/Sprite (chỉ Texture2D/MonoScript/Mesh/AudioClip, và "Name" trong đó là tên file bundle, không phải guid export).

**Cách khôi phục (đã dùng thành công 2026-07-23):** extract `assets/bin/Data` từ `com.lavalabs.dungeonrush.apk` (trong xapk) → UnityPy 1.25.2 đọc GameObject/Renderer/Material gốc theo tên → ghép với YAML export theo (prefab, GO name, slot) → suy ra guid→file → tự sinh .meta với guid đó trong project đích. Phân biệt file trùng tên bằng float properties (material) / kích thước PNG (texture, vd `circle` = `circle_0.png` 256×256).

Đã copy vào `2026DungeonRushUnity/Assets/_Assets`: 564 texture theo tính năng vào `_ResourceGame/` (26 thư mục), 34 prefab particle + 16 material vào `Prefabs/Fx/{Mining,Enchantment,Combat,Projectile,Materials}`. Material dùng shader built-in `Particles/Standard Unlit` → project URP cần chạy Render Pipeline Converter kẻo magenta. Xem [[dungeonrush-config-format]].
