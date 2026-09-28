---
name: dungonrush-companion-save-upgrade
description: Save/nâng cấp/summon-level companion + UI CompanionUI — bảng whn & whm ĐÃ trích, nâng cấp THỦ CÔNG
metadata:
  type: project
  modified: 2026-09-28
---

Save + level + summon companion (reverse il2cpp v41, xem DecodedData/COMPANION_MODEL.md §7-8). Khác hệ meta
unlock/summon cost ở [[dungonrush-companion-unlock-summon]]; level pet in-battle [[dungonrush-companion-battle-classes]].

**Schema GỐC:** `CompanionModel { CompanionId(assetName), Level, CardCount, IsNew }` ; OwnedCompanions ;
EquippedCompanions (tối đa 3) ; `TotalCompanionSummons` (→ `UserCompanionData.totalSummons`).

**ĐÃ reverse (2026-09-25):**
- Thẻ/level `ly.gpz` = `whn {0,2,3,3,3,4,4,5,5,6,7,8,10,11,13}`, level≥15 → 16 (KHÔNG phải SO companion manager như ghi cũ).
- Summon Level = bảng `ly.whm` 100 hàng (threshold tổng lượt + 6 tỉ lệ Common..Mythic) → `CompanionSummonLevelConfig.cs`.
- `UserController.edi`: con mới = sở hữu 0 thẻ + IsNew; trùng = +1 thẻ, **KHÔNG auto lên cấp** → nâng cấp THỦ CÔNG (user chốt).
- Quick equip = sort rarity↓ rồi level↓, lấy 3 (`UserController.ep.dnc`).
- Trích bằng emulator capstone nhỏ (track w/s register, bắt `ValueTuple.ctor`) + đọc InitializeArray blob theo
  `/*Metadata offset*/` của `__StaticArrayInitTypeSize=N` trong dump.cs. Xem [[dungonrush-reverse-native-il2cpp]].

**Code (compile sạch):** `CompanionService` (static: Summon/TrySummonByBone/TryUpgrade/UpgradeAll/QuickEquip),
`UserCompanionData` (AddCards không level, Upgrade, ClearNew, totalSummons), `CompanionUpgradeConfig` (bảng whn),
UI `UI/Companions/CompanionUI.cs` + `ElementPetEquimentUI.cs` (clone template trong Content; slot trống=objAddArea).

**Popup info pet + nút ô pet (2026-09-28):** ô pet: btInfo (cả ô) → `UIPetInfo` (Resources/Prefabs/UI, key `UIKey.PetInfo`);
nút Add chỉ hiện khi sở hữu + chưa equip + CÒN slot; Remove khi đang equip. Click ô KHÔNG còn tự nâng cấp — nâng ở nút
Upgrade trong popup. Popup gốc = `CompanionInfoPopup.grk`; mô tả gốc do `lu.gox(CompanionData,level,float)` dựng
(CHƯA disasm) → map `<Value>` theo CompanionType là SUY LUẬN từ field các lớp PetCompanion*, đã khớp 2 ảnh game thật
(Flame Wing lv1=252/Dmg47 HP63, Magic Wool lv6=78/Dmg6 HP65). Giá trị hiển thị = Base+Scaler×lv, KHÔNG nhân CompanionDamage;
số cắt phần lẻ, ≥1000 rút gọn k/M/B/T; màu số `#E5F36B` (từ prefab). Own Effect → [[dungonrush-companion-own-effect]].

**CÒN LẠI:** màu chữ rarity trong popup (chưa có bảng màu 10 rarity — cần ảnh/màu từ user);
NRE CompanionUI.RefreshInventory khi OnEnable lúc GameData.staticData chưa Load (có sẵn, chưa sửa);
 ads thật + giới hạn/ngày ad summon; panel kết quả summon; btInfo (popup tỉ lệ — CompanionUpgradeInfoPopup);
ObjDownArrow; imgProcess summon ở scene đang Image Type=Sliced (phải đổi Filled mới chạy fillAmount);
spawn theo EquippedCompanions thật.
