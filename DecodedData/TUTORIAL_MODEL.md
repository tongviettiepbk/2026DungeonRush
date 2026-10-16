# Tutorial (FTUE) — tay chỉ dẫn mềm (reverse il2cpp v41)

Nguồn: `UserController.efw/efx`, `User.CompletedFtueSteps`, `TutorialUI` (kxf..kxj), `GameResources.IsTutorialEnabled`,
`RemoteConfigController` (`tutorials_enabled`), cùng toàn bộ hàm gọi efw/efx/kxh/kxi trong libil2cpp.so
(quét bằng `ftue_sites.py`) + giá trị serialized `TutorialUI` trong prefab rip.

## Mô hình
- KHÔNG có overlay chặn / màn tối / hộp thoại. Mỗi màn có sẵn node `TutorialUI` (hình tay chỉ) đặt cạnh nút cần bấm; code của màn đó tự Show/Hide theo trạng thái step.
- `TutorialController` gốc là code chết (hwo/hwp/hwq thân rỗng `ret`).
- Step = số nguyên, không có enum. Lưu `User.CompletedFtueSteps` (List<int>).
- `efw(id)` = `CompletedFtueSteps.Contains(id)`.
- `efx(id)`: chưa có thì Add → `Save(true)` → bắn `UserController.FtueStepCompleted(id)`.
- Mỗi lần hoàn thành gọi `co.csm(id, "<tên>")` (analytics tutorial, tên ở bảng dưới).

## TutorialUI (component tay chỉ)
- Field: `_child` (Transform hình tay), `_moveDistance`, `_moveDuration`, `_scaleDuration`, `_moveEase`, `_scaleEase`.
- Giá trị gốc: moveDistance 30 (8 node dùng 15), moveDuration 0.5, scaleDuration 0.25, moveEase 4 = InOutSine, scaleEase 27 = OutBack.
- Awake: nhớ `localPosition` gốc của `_child`, đặt `_child.localScale = 0`.
- `IsShowing` (kxf) = cờ đang hiện.
- `Enabled()` (kxg, static): true nếu chưa có ControllerManager; ngược lại false khi `GameResources.IsTutorialEnabled = false` hoặc remote `tutorials_enabled = false`.
- `Show(scale = 1)` (kxh): bỏ qua nếu !Enabled hoặc đang hiện. Kill tween cũ → trả `_child` về vị trí gốc, scale 0 → Sequence `DOScale(one × scale, scaleDuration).SetEase(scaleEase)` → xong thì `DOLocalMoveY(gốc.y + moveDistance, moveDuration).SetEase(moveEase).SetLoops(-1, Yoyo)`.
- `Hide()` (kxi): bỏ qua nếu không hiện. Kill 2 tween → `DOScale(0, scaleDuration).SetEase(scaleEase)`.
- Mọi tween `SetLink(gameObject)`. OnDestroy kill tween.

## Bảng step
| Id | Tên analytics | Tay chỉ hiện ở | Điều kiện hiện tay | Hoàn thành khi |
|---|---|---|---|---|
| 1 | loot_button_clicked | ForgeUI.LootTutorialUI | !1 | bấm nút Loot |
| 2 | level_up | ExperienceUI.LevelTutorialUI → LevelPopup.LevelUpTutorialUI | exp đầy (chờ lên level) && !2 | bấm Complete trong LevelPopup |
| 3 | auto_clicked | — (ẩn AutoForgeTutorialUI) | — | bấm nút AutoForge lần đầu (đã đủ level) |
| 4 | boost_clicked | — | — | bấm nút Boost lần đầu |
| 5 | auto_unchecked | — | — | bấm Keep (bỏ tick rarity) trong AutoForgePopup |
| 6 | entered_dragon | tab Dungeon + DungeonTabPage.DragonBossTutorialUI + DungeonPopup.EnterTutorialUI | !6 | vào Dragon dungeon |
| 7 | entered_zombie | tab Dungeon + ZombieHordeTutorialUI + EnterTutorialUI | 6 && !7 && PlayerLevel ≥ 5 | vào Zombie Horde |
| 8 | summoned_companion | tab Companion + CompanionTabPage.SummonBoneTutorialUI | 7 && !8 && Bone ≥ 100 | summon pet (ads / thường / Bone) khi đang sở hữu ≤ 2 pet |
| 9 | entered_mine | — | — | vào mỏ lần đầu |
| 10 | crafted_wing | — | — | chế Wing lần đầu |
| 11 | buy_boost | — | — | mua x2 boost bằng gem ở Store |
| 12 | auto_forge_started | AutoForgeButton.AutoForgeTutorialUI → AutoForgePopup.StartAutoForgeTutorialUI | 1 && !12 | bấm Start trong AutoForgePopup |
| 13 | — | CompanionTabPage.QuickEquipTutorialUI | 8 && !13 && có pet để quick equip | bấm Quick Equip |
| 15 | mastery_unlock_clicked | MasteryButton / tab Mastery / MasteryUpgradeCard / UnlockTutorialUI | điều kiện Mastery bên dưới && !15 | mở khoá nhánh AutoLootHammerCount (8) |
| 17 | mastery_upgrade_purchased | MasteryUpgradeDetailPanel.UpgradeButtonTutorialUI | 15 && !17 | nâng nhánh 8 lên level ≥ 2 |
| 20 | mastery_hammer_{n}_selected | AutoForgeButton + AutoForgePopup.HammerCountTutorialUI + HammerOptionElementUI.Tutorial | 17 && !20 | chọn số búa ≥ 2 trong AutoForgePopup |
| 21 | forge_dismissed_item_reopened | ItemElementUI.DismissedItemTutorialUI | !21 khi có item bị bỏ qua chèn lại vào list Forge | bấm item đó |
| 22 | boss_button_clicked | BossButtonUI._tutorialUI | !22 | bấm nút Boss campaign |
- Không có id 14 / 16 / 18 / 19 trong v41.
- Step 3, 4, 5, 9, 10, 11 chỉ đánh dấu + analytics, không có tay chỉ.

## Điều kiện chi tiết
- **Thanh tab (TabBarUI.kvb)**: tab Dungeon (TabIds 2) sáng tay khi `!6` hoặc `(6 && !7 && PlayerLevel ≥ DungeonTabPage.ZombieHordeUnlockPlayerLevel = 5)`.
- Tab Companion (TabIds 5) sáng tay khi `(7 && !8)` hoặc Mastery cần dẫn (`MasteryController.iuj`).
- Tab chỉ hiện tay khi tab đã mở khoá và không phải tab đang chọn.
- **Mastery cần dẫn (iuj)**: `MasteryConfig` bật && `PlayerLevel ≥ GameResources.MasteryUnlockPlayerLevel` && `!17`.
- **AutoForgeButton (iay)**: hiện tay khi `((1 && !12) || (17 && !20))` && đã đủ `UsableAtPlayerLevel` && nút đang hiển thị.
- **AutoForgePopup mở (eip)**: nếu `17 && !20` mà số búa đang chọn ≥ 2 thì hoàn thành 20 luôn (analytics `mastery_hammer_already_known`).
- Tay Start hiện khi `!(17 && !20) && !12`.
- Tay HammerCount hiện khi `17 && !20`; tay trên từng HammerOption hiện khi option ≥ 2 và chưa khoá.
- **DungeonPopup (kcg)**: tay Enter hiện khi `(type Dragon && !6)` hoặc `(type Zombie && 6 && !7)`.
- **Mastery khi mở popup/tab (ivd/ivr)**: nhánh 8 level ≥ 1 thì hoàn thành 15; level ≥ 2 thì hoàn thành 17.
- **Item bị bỏ qua (ForgeController.het)**: khi chèn lại item bị dismiss vào đầu list Forge, `!21` thì hiện tay trên ItemElementUI đó.
- **Enchantment**: `EnchantmentSlotUI.hye(true)` bật TutorialUI trên slot khi đang chọn slot gắn relic — không có step.
