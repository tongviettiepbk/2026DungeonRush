---
name: dungonrush-forge-auto-loot
description: "Loot + Auto Loot dựng lại theo gốc 2026-10-15 — ForgeUIController ở lobby, popup gốc NewItemPopup/ItemInfoPopup/AutoForgePopup, bán đồ = EXP; doc DecodedData/FORGE_MODEL.md"
metadata:
  node_type: memory
  type: project
  originSessionId: a7a13290-8879-4f3d-a10b-9bf6dc65113c
  modified: 2026-10-15T11:03:10.476Z
---

Dựng 2026-10-15 theo reverse libil2cpp (doc: `DecodedData/FORGE_MODEL.md`). User chốt: làm ĐỦ như gốc, bán đồ cộng EXP, popup 1 thẻ.
- Lobby: `ForgeUIController` gắn ở `UIMainGame2/.../BottomUI/ForgeUI` (bục ItemList, nút Loot/Auto); UIMainLobby chỉ còn `forgeUI`, `RefreshEquipment()`, `RefreshExp()`.
- Luồng: tiêu N hộp (LOOT_TICKET) → N món lên bục → từng món: auto + `ForgeService.ShouldDiscard` → bán (EXP [1,2,4,6,8,10,12,14] qua `PlayerExpService.Grant`, có boost); slot TRỐNG → UINewItemPopup (chỉ Equip vào hero); slot CÓ ĐỒ → UINewForgePopup (2 thẻ; Equip = mặc + đổi chỗ 2 thẻ trong popup; Sell = bán, rarity cao hơn đồ đang mặc thì hỏi xác nhận). User từng báo lỗi khi gộp chung 1 popup — 2 popup 2 nhiệm vụ.
- Save mới `UserForgeData` (key_user_forge): selectedAutoHammerCount, disabledRarities, subStatFilterEnabled, enabledSubStats. AutoSellLowerLevel gốc là field chết → không port.
- Số hộp/lượt: lv≥6 → Mastery AutoLootHammerCount (chưa mở = 1); lv<6 → mốc [0,10..70]. Lọc substat mở lv7; nút Auto hiện lv2, dùng lv3.
- Prefab dựng bằng `Tools/DungeonRush/Build Forge UI/All` (ForgePrefabBuilder): Resources UINewItemPopup / UINewForgePopup / UIItemInfoPopup (UIKey.InfoGear, class UIGearInfo) / UIAutoForgePopup + `Prefabs/UINew/Forge/ItemElementUI`. Chạy lại = ghi đè.
- Chữ trong popup đồ để TIẾNG ANH: font prefab gốc thiếu dấu tiếng Việt ("Mũ" → "M0").
- Đã xoá UiInfoGer / UiLootGearInfo / UILootGearInfo.cs tự dựng. Test Play pass (loot tay, equip-đổi đồ, bán, auto dừng ở món giữ); CHƯA commit.

**Why:** user muốn bỏ 2 prefab tự dựng, dùng prefab gốc và hoàn thiện auto loot.
**How to apply:** sửa loot thì sửa ForgeUIController/ForgeService; đổi layout popup bằng tay thì đừng chạy lại builder. Xem [[dungonrush-loot-forge-design]], [[dungonrush-main-ui-uimaingame2]], [[dungonrush-mastery-feature]].
