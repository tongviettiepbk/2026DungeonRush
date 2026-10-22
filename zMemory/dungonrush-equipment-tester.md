---
name: dungonrush-equipment-tester
description: "Công cụ test chọn đồ mặc hero — EquipmentTester (IMGUI, Editor-only) đặt ở scene Root, nút \"Đồ\" góc dưới-trái ở MainGame"
metadata:
  node_type: memory
  type: project
  originSessionId: 217dc82a-a19e-4c75-aad8-db37024bc629
  modified: 2026-10-22T02:44:17.149Z
---

2026-10-22: dựng `Scripts/_Test/EquipmentTester.cs`, GameObject `EquipmentTester` trong scene Root (DontDestroyOnLoad). Chỉ hiện ở scene MainGame (ở Login chạm GameController.Instance sẽ tự tạo Singleton). 8 slot, nút < > lướt từng món (vị trí đầu = trống), rarity theo catalog, level ô chung, substat rỗng; mặc là Save + post EquipmentChanged từng slot + ShopUIUtil.RefreshLobby. Cape chỉ ghi slot CAPE của UserEquipmentData, không đụng kho UserCapeData.

**Why:** user cần test lần lượt đồ lên hero ([[dungonrush-hero-equipment-reskin]]).

**How to apply:** muốn thêm tính năng test đồ thì sửa file này, đừng dựng công cụ mới. HeroVisual bỏ qua EquipmentChanged không kèm GearSlotType.
