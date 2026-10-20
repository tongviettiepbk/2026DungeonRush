---
name: dungonrush-world-sorting-under-ui
description: Canvas lobby UIMainGame2 ở layer Default order 100 (gốc) → mọi renderer trong world phải Default & order < 100, không thì vẽ đè UI
metadata:
  type: project
---
Canvas lobby UIMainGame2 là ScreenSpaceCamera, sorting layer **Default order 100** (giá trị gốc rip). Popup UIManager ở layer UI (trên lobby). Bất kỳ renderer world nào ở layer sau Default (Ground/Unit/Projectile/Fx/UI…) hoặc Default order ≥ 100 sẽ vẽ ĐÈ lên trang lobby (Events, Shop…).

Sửa 2026-10-20: thanh máu (health-bar.prefab + 11 BossRush*) về Default 50/51/52; CombatText Default 60; LineRenderer pet (CompanionFx) Default 40; hằng StaticValue.SORTING_ORDER_WORLD_HUD = 50.

**Why:** user báo FX + thanh máu hiện đè UI Events; nguyên nhân là HP bar ở layer UI, line pet order 500.
**How to apply:** FX/HUD world mới luôn đặt Default, order < 100 (dùng SORTING_ORDER_WORLD_HUD làm mốc); đừng dùng layer UI/Fx cho thứ trong world.
