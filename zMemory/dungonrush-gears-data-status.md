---
name: dungonrush-gears-data-status
description: "Trạng thái data phần Gears đã chốt (2026-08-05): base stat per-slot ở GearStatConfig, Weapons gộp vào Gears/, substat roll reverse xong"
metadata: 
  node_type: memory
  type: project
  originSessionId: 1e37f0d7-d04a-49a9-b37c-9c5835d318d2
  modified: 2026-08-05T05:44:53.164Z
---

Phần **Gears** (thư mục `_Assets/Resources/Scriptable Objects/Gears/`) — trạng thái đã chốt 2026-08-05:

- **Weapons đã gộp VÀO trong Gears/** (`Gears/Weapons/`) — vì cùng cấp tính năng. An toàn vì KHÔNG có `Resources.Load` theo path nào trong code; gear/weapon asset tham chiếu qua GUID.
- **5 loại catalog** (Helmet slot1, Gloves 2, Ring 3, Necklace 4, Backpack 5) dùng class `GearItemData` — CỐ TÌNH chỉ có tên/itemId/rarity/icon, **KHÔNG stat riêng**. Base stat là **hằng số theo SLOT** ở `GearStatConfig.asset` (`GearStatConfigData`): Helmet45,Backpack30,Necklace20(→Health) Gloves6,Ring6(→Damage); weapon melee9/range7. Main tính runtime `GearStatCalculator.GetGearMainStat` = Base×√10^rarity×(1+0.015×level). **User đã xác nhận: KHÔNG thêm per-item stat cho 5 loại này** (sẽ trùng data + lệch mô hình gốc).
- **Cape & Wing** là ngoại lệ hợp lý: nhúng healthBase/damageBase/scaler RIÊNG mỗi món (game gốc mỗi con khác nhau) + CapeConfig (XP level-up) / Wing craft-reroll-levelup ore cost.
- **Substat**: số dòng theo rarity (Rare/Epic 1, Legendary→Ancient 2, Immortal+ 3; <Rare 0) ở `subStatCountThresholds`; giá trị roll = truncated-normal đã REVERSE XONG (xem [[dungonrush-item-stats-source]]).
- Điểm có thể còn thiếu (chưa làm, khác base stat): 5 loại catalog chưa có field kinh tế craft/upgrade như Wing — chỉ làm nếu game có hệ thống đó.

Xem [[dungonrush-rebuild-progress]], [[dungonrush-item-stats-source]].
