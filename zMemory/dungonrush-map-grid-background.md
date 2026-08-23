---
name: dungonrush-map-grid-background
description: "Nền map DungOnRush là grid procedural qua Custom/GridShader + material Inner/Outer, KHÔNG phải texture bitmap; shader gốc bị mất khi rip, đã viết lại cho URP"
metadata: 
  node_type: memory
  type: project
  originSessionId: 7a58790d-9802-4f1b-93dd-a7da2419a0f5
  modified: 2026-07-25T17:44:14.562Z
---

Nền (background) của map trong DungOnRush KHÔNG phải file ảnh. Mỗi map prefab (`MainMapPrefab`, `ZombieMapPrefab`, `DragonMapPrefab`, `CultistMapPrefab`, `BossRushMapPrefab` trong `2026DungeonRushUnity/Assets/_Assets/Prefabs/Maps/`) có 2 quad dùng sprite `Square.png`:
- **InnerPlane** = sàn xám sáng có lưới
- **OuterPlane** = viền tối bao quanh

Cả hai render bằng **`Custom/GridShader`** (procedural grid: `_BackgroundColor`, `_LineColor`, `_GridSize`, `_LineWidth`, `_Padding`, `_Roundness`, noise). Tường/cửa mới là bitmap thật (`wall_piece_black_and_white.png`, `wall_corner_piece.png`, `door.png` ở `_ResourceGame/MainMap/`).

**Bẫy:** shader từ AssetRipper (`AssetRipper/.../Shader/Custom_GridShader.shader`) chỉ là `//DummyShaderTextExporter` — logic vẽ lưới đã mất, chỉ còn Properties. Đã **viết lại shader cho URP** tại `_Assets/_ResourceGame/Shaders/Custom_GridShader.shader` (guid `fdef1b5b391dcce49beb9be2d597560d` để material link được).

Đã tạo 7 material tại `_Assets/_ResourceGame/MapMaterials/` với ĐÚNG guid mà prefab trỏ (trước đó bị missing). Map guid→material (Inner→guid):
- Main: Inner `af827e28d9d8ae94a8605c7df0478a35`, Outer `293757cc2196fbf438685e6922f2c99a`
- BossRush: single `f032a4b906e78ec4b9e2f55c54423162`
- Dragon: Inner `6ab8ef1d67a476541840fa1d527f3a61`, Outer `47fc3fc43362ed34999a6cd78f4e132d`
- Zombie & Cultist (dùng chung): Inner `7aaea0337b901da4bb8b6461d0fc0716`, Outer `ecfed2bb61a51ee40814ab17138b59b5`

Màu lấy từ material AssetRipper (Main Inner bg #525252/line #5C5C5C; Main Outer bg #141414). Đã verify render qua UnityMCP execute_code (camera+quad→PNG) — khớp screenshot game. Liên quan: [[assetripper-export-no-meta]], [[dungonrush-rebuild-progress]].
