---
name: dungonrush-wing-cape-todo
description: "Wing (C8) ĐÃ có trang craft/upgrade/reroll/equip (2026-10-09, chưa test Unity); quặng = ItemType 7..14 nhưng CHƯA có Mining; Cape (C7) vẫn chưa có cách nhận"
metadata:
  node_type: memory
  type: project
  originSessionId: a7efa578-0468-4619-8603-dbc4649c84e0
  modified: 2026-10-09T11:27:06.131Z
---

Trạng thái 2026-10-09:
- Wing XONG phần code + wire YAML (chưa mở Unity verify): save `UserWingData` (key_user_wing, owned List<WingModel>), `WingService` (craft/level up/reroll/equip theo native), UI `UITabWing` (PageWing trong UiMainGame, carousel + ClaimPage), `UIWingCraftPopup`, `UIWingRerollPopup`, `WingElementUI`, `ElementSubStatRerollUI` (gắn vào Prefabs/UI/Gear/SubstatUIElementPrefab). Ô Wing lobby (GearSlotType.WING=7) bấm → mở trang, mở khoá PlayerLevel ≥ 6. Doc: `DecodedData/WING_MODEL.md`.
- Wing đang mặc vẫn lưu ở UserEquipmentData slot WING (copy level/subStats) → WingService.SyncEquipped ghi lại khi lên cấp/reroll.
- Quặng: `ItemType.COAL_ORE + (int)MineOreType` (7..14). **Hệ Mining chưa có** → nút Mining chỉ toast; cheat editor phím O = +1000 mỗi quặng. Text giá đang là "Tên quặng có/cần" (gốc dùng `<sprite=0>` + TMP sprite asset từng quặng — chưa tạo).
- Bỏ qua so với gốc: nền rarity sprite (GameResources.RarityBackgroundSprites), animation ClaimPage chi tiết, NotificationUI Mining.
- Cape vẫn CHƯA CÓ: triệu hồi bằng CloakCurrency (SummonCost 20), salvage lấy XP, lên cấp XP (CapeRarityConfig), SubStatCount 2, CapeDetailPopup, mở khoá CapeUnlockPlayerLevel = 15, nút ShowCloak.

**How to apply:** mở Unity kiểm tra PageWing + 2 popup trước khi làm tiếp; Mining là bước kế để có nguồn quặng thật. Xem [[dungonrush-power-model]], [[dungonrush-enchantment-system]], [[dungonrush-reverse-native-il2cpp]].
