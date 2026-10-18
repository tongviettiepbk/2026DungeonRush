---
name: dungonrush-rate-us
description: Popup Rate Us dựng + reskin 2026-10-19 theo gốc (reverse il2cpp) + mockup BallIdle; trigger đồ Rare/Epic lần đầu, max 3 lần; đã commit
metadata:
  type: project
---
Rate Us dựng 2026-10-19 (trước đó game chưa có, chỉ có rip `Prefabs/UI/Settings/RateUsPopup1.prefab`). Doc gốc: `DecodedData/RATE_US_MODEL.md`.
- Code: `Scripts/RateUs/RateUsService.cs` (dox/dpg/hex/hcd/dgi gốc), `Scripts/UI/RateUs/UIRateUsPopup.cs`, `UIKey.RateUsPopup`, 5 field save ở `UserPlayerData` (isRateUsSubmitted, rateUsShowCount, isRateUsFirst{Green,Purple}EquipShown, isRateUsFirstDragonWinShown).
- Nối: `ForgeUIController.Equip` → `OnItemEquipped(rarity)`; `DungeonService.CompleteDungeon` → `OnDungeonWin` (gốc: trigger Dragon đánh cờ nhưng KHÔNG hiện vì còn ở mode dungeon — giữ đúng vậy).
- Prefab `Resources/Prefabs/UI/UIRateUsPopup.prefab` dựng bằng execute_code (không có builder): kit Mastery (popup_frame, header, btn_close = nút Later, btn_blue = Submit), 2 sao art giao ở `Art/1_UIKit/RateUs/star_{filled,empty}.png` 114x113. Không cần ChatGPT (art đủ).
- User chốt: theo mockup — bỏ dòng mô tả, X thay nút LATER; nút xanh ghi "SUBMIT" (chữ cứng như gốc), Submit tắt thì nút tô xám 0.7.
- Test Play đạt (trigger Rare, chọn sao, Submit/Later). Ảnh so: `ArtIncoming/_Concept/RateUs/compare_mockup_vs_game.png`, `play_rateus.png`. iOS: field `iosAppId` trên prefab còn TRỐNG.

**Why:** user giao mockup Rate Us (BallIdle) + chọn "dựng đủ theo gốc".
**How to apply:** sửa popup thì chỉnh prefab trực tiếp. Xem [[dungonrush-reskin-plan]], [[dungonrush-follow-original-strict]].
