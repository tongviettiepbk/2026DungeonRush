---
name: dungonrush-ftue-tutorial
description: "Tutorial gốc = FTUE tay chỉ mềm (không chặn), 18 step int lưu User.CompletedFtueSteps; doc DecodedData/TUTORIAL_MODEL.md; 17/18 step đã gắn 2026-10-17 (USER CHƯA TEST); còn 21 chờ tính năng; 22 gắn nhưng nút Boss luôn ẩn (Boss Gate tắt)"
metadata:
  node_type: memory
  type: project
  originSessionId: a7015a71-d3e9-49aa-b7f2-b6ec5e0b8749
  modified: 2026-10-16T16:46:51.126Z
---

Reverse il2cpp 2026-10-16. User chốt: dựng THEO GỐC (tay chỉ mềm), data/service kiểu StickIdle.
**Đã dựng (Phase 0+1, compile sạch, chưa test Unity):** `Scripts/Tutorial/` — `FtueStep` (enum = id gốc), `TutorialService` (static: IsEnabled, IsCompleted, Complete→profile.completedTutorials + Save(true) + event C# `StepCompleted`), `TutorialUI` (port kxh/kxi; meta dùng LẠI guid gốc `b523c7755b6eb4f9e804571436a7422a` → 6 node trong Prefabs/UI/Pet/CompanionTabPage.prefab tự nối). DOTween project không có SetLink → tự kill tween. Không migrate save cũ (tay chỉ mềm, bấm 1 lần là xong).
**Phase 2 nhóm Forge/Level XONG 2026-10-16 (Unity compile 0 lỗi, chưa Play):** step 1 ForgeUIController.lootTutorial, 3+12/20 autoForgeTutorial, 2 ExperienceUI.LevelTutorialUI + UILevelPopup.LevelUpTutorialUI, 4 BoostButtonView, 5+12 UIAutoForgePopup.startAutoForgeTutorial. Gắn component bằng vá YAML (script scratchpad add_tutorial_ui.py: thêm MonoBehaviour guid gốc, bật node, tay scale 0, gán field) + builder dùng `TutorialUIBuilderUtil.Setup(node)` (Scripts/Tutorial/Editor) để dựng lại không mất.
**Phase 2 nhóm còn lại XONG 2026-10-17 (Unity compile 0 lỗi, kiểm 18 field tay chỉ OK, USER CHƯA TEST Play):** 9 MiningController.Init, 10 UIWingCraftPopup, 11 StoreTabPage (x2/x5), 6/7 UITabDungeon+ElementDungeonUI.tutorial+UIDungeonPopupStart.enterTutorial, tab bar ElementTabMenuUILobby.tutorial (UIMainLobby.RefreshTabTutorials: Dungeon / Pet), 8/13 CompanionUI (CompanionService.IsQuickEquipChanged), 15/17 MasteryUI.unlockTutorial + ElementMasteryUI.tutorial + MasterUpgradeUI.upgradeTutorial + UITabCompanion.masteryTabTutorial (2 node cuối bị mất trong UIMainGame2 → dựng lại bằng execute_code, copy node Unlock), 20 UIAutoForgePopup hammerCount/hammerOption.
**Step 22 XONG 2026-10-17 (Unity compile 0 lỗi, field kiểm OK, chưa Play):** `UI/MainLobby/BossButtonUI.cs` (meta = guid gốc 277ce148…) gắn TopBarUI/BossButton (node bật, ẩn bằng CanvasGroup), UIMainLobby.bossButton + RefreshBossButton trong Refresh. Nút Boss gốc thuộc **Boss Gate** (cổng boss mỗi 10 level, thua thì lùi level, nút để đánh lại) mà live `boss_gate_enabled=false` → `BossButtonUI.CanRetryBossGate()` trả false, nút không bao giờ hiện, bấm chỉ log (chưa có BossGateController). Doc: DecodedData/BOSS_BUTTON_MODEL.md.
**Enchantment "đeo vào đây" XONG 2026-10-17 (chưa test Play):** 8 ô ElementEnchantmentEquipUI.tutorialEquipHere (thay objTutEquipHere), hye gốc: bật node + Show(0.5) / tắt node + Hide, moveDistance 15, node để tắt sẵn.
**Step 21 XONG 2026-10-17 (chưa test Play):** chạm nền NewItemPopup/NewForgePopup khi chưa chọn = bỏ qua (CloseOnClickDark gốc) → món về đầu bục, dừng xử lý; tay chỉ ItemElementView.dismissedTutorial; bấm món = mở lại + Complete(21).
**Chưa làm (thiếu tính năng):** MasteryButton lobby tắt/không dùng. Gốc KHÔNG có overlay chặn kiểu StickIdle: mỗi UI có node `TutorialUI` (tay chỉ, DOTween nhún/scale; `kxh`=Show, `kxi`=Hide, `kxg`=bật toàn cục theo remote config `tutorials_enabled`/GameResources.IsTutorialEnabled). `TutorialController` gốc thân rỗng (dead).
Lưu: `User.CompletedFtueSteps` (List<int>); `UserController.efw(id)`=đã xong, `efx(id)`=đánh dấu + event `FtueStepCompleted`.

Step (id → nghĩa / nơi):
- 1 Loot lần đầu (ForgeUI.LootTutorialUI) · 2 Lên level (ExperienceUI.LevelTutorialUI → LevelPopup Complete)
- 3 bấm AutoForge lần đầu `auto_clicked` · 4 bấm Boost `boost_clicked` · 5 bỏ tick Keep AutoForge `auto_unchecked` (3/4/5 chỉ đánh dấu)
- 6 Dragon dungeon · 7 Zombie Horde (tab Dungeon sáng tay, DungeonTabPage + DungeonPopup.EnterTutorialUI; 7 cần level ≥ mốc)
- 8 summon pet bằng Bone (tab Companion, SummonBoneTutorialUI khi Bone ≥100) · 13 Quick Equip pet
- 9 vào mỏ `entered_mine` · 10 chế Wing `crafted_wing` · 11 mua boost Store `buy_boost` (chỉ đánh dấu)
- 12 Start AutoForge (tay ở nút AutoForge sau step 1) · 20 chọn số búa AutoForge (sau 17)
- 15 mở khoá Mastery · 17 nâng Mastery (MasteryController.iuj: level ≥ mốc && !17)
- 21 bấm item bị loại trong list Forge (ItemElementUI.DismissedItemTutorialUI) · 22 nút Boss campaign (BossButtonUI)
- Không có 14/16/18/19. EnchantmentSlotUI tay chỉ khi chọn slot, không có step.

Project: các node TutorialUI CÒN trong prefab rip (UIMainGame2, CompanionTabPage, DungeonTabPage, LevelPopup...) nhưng builder Forge/LevelPopup + ExperienceUI đang tắt sẵn; `UserProfileData.completedTutorials` có sẵn nhưng chưa ai gọi. Tool dump: `ftue_sites.py` (quét call efw/efx/kxh/kxi). Xem [[dungonrush-reverse-native-il2cpp]], [[dungonrush-follow-stickidle]].
