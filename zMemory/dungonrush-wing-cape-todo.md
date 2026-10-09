---
name: dungonrush-wing-cape-todo
description: "Wing (C8) + Cape (C7) ĐÃ có UI/logic theo native (2026-10-09, chưa test Unity); quặng Wing chưa có Mining, Cloak (Cape) chưa có nguồn thật — chỉ cheat phím O / K"
metadata:
  node_type: memory
  type: project
  originSessionId: a7efa578-0468-4619-8603-dbc4649c84e0
  modified: 2026-10-09T12:07:15.228Z
---

Trạng thái 2026-10-09:
- **Wing** xong code + wire YAML: `UserWingData` (key_user_wing), `WingService`, UI `UITabWing` (PageWing trong UiMainGame), `UIWingCraftPopup`, `UIWingRerollPopup`, `WingElementUI`, `ElementSubStatRerollUI`. Ô Wing lobby (GearSlotType.WING=7) → trang, mở khoá lv6. Doc `DecodedData/WING_MODEL.md`. Quặng = `ItemType.COAL_ORE + (int)MineOreType` (7..14); **Mining chưa có** (nút Mining chỉ toast); cheat phím O.
- **Cape** xong code + prefab (commit chưa): `CapeSummonConfig` (bảng ft 50 level × 6 rarity), `UserCapeData` (key_user_cape, cape theo INSTANCE Guid), `CapeService` (summon/XP eql-eqm/salvage/equip/show cloak), UI `UICapePopup` + `UICapeDetailPopup` + `UICapeSalvagePopup` + `UICapeUpgradeInfoPopup` (Resources/Prefabs/UI, bọc Canvas+ContentAll từ prefab gốc Prefabs/UI/Cape/), `CapeElementUI` (Prefabs/UINew/Cape). Ô Cape lobby (CAPE=6) → popup, mở khoá lv15. Cloak = `ItemType.CLOAK` (15), **chưa có nguồn thật**; cheat phím K +1000. HeroVisual ẩn áo khi ShowCloak tắt. Doc `DecodedData/CAPE_MODEL.md`.
- Wing/Cape đang mặc vẫn copy vào UserEquipmentData (slot WING/CAPE: equipId = wingId/capeId, level, subStats) → service đồng bộ khi lên cấp/reroll/salvage.
- `RarityBackgroundConfig` (Resources/Scriptable Objects/UI) = nền ô item theo rarity, dùng chung (Cape đang dùng; Wing dùng list riêng trên UITabWing).
- Bỏ qua so với gốc: CloseOnClickDark, animation ClaimPage chi tiết, auto-scroll bảng summon nhiều, NotificationUI, TMP sprite icon tiền tệ (`<sprite=0>`).

**How to apply:** mở Unity kiểm tra 4 popup Cape + PageWing trước khi làm tiếp; nguồn Cloak/quặng (Mining, BattlePass, shop…) là bước kế. Xem [[dungonrush-power-model]], [[dungonrush-enchantment-system]], [[dungonrush-reverse-native-il2cpp]], [[dungonrush-ripped-prefab-rewire]].
