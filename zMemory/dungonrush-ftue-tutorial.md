---
name: dungonrush-ftue-tutorial
description: "Tutorial gốc = FTUE tay chỉ mềm (không chặn), 18 step int lưu User.CompletedFtueSteps; doc DecodedData/TUTORIAL_MODEL.md; Phase 0+1 core (Scripts/Tutorial) XONG 2026-10-16, Phase 2 wiring từng màn CHƯA"
metadata:
  node_type: memory
  type: project
  originSessionId: a7015a71-d3e9-49aa-b7f2-b6ec5e0b8749
  modified: 2026-10-16T16:04:12.248Z
---

Reverse il2cpp 2026-10-16. User chốt: dựng THEO GỐC (tay chỉ mềm), data/service kiểu StickIdle.
**Đã dựng (Phase 0+1, compile sạch, chưa test Unity):** `Scripts/Tutorial/` — `FtueStep` (enum = id gốc), `TutorialService` (static: IsEnabled, IsCompleted, Complete→profile.completedTutorials + Save(true) + event C# `StepCompleted`), `TutorialUI` (port kxh/kxi; meta dùng LẠI guid gốc `b523c7755b6eb4f9e804571436a7422a` → 6 node trong Prefabs/UI/Pet/CompanionTabPage.prefab tự nối). DOTween project không có SetLink → tự kill tween. Không migrate save cũ (tay chỉ mềm, bấm 1 lần là xong).
**Còn (Phase 2):** gắn TutorialUI vào node TutorialUI của từng màn (UIMainGame2 đã mất component, phải add + set _child) và gọi Show/Hide/Complete theo bảng; bỏ HideAll TutorialUI trong ForgePrefabBuilder/LevelPopupPrefabBuilder + ExperienceUI. Gốc KHÔNG có overlay chặn kiểu StickIdle: mỗi UI có node `TutorialUI` (tay chỉ, DOTween nhún/scale; `kxh`=Show, `kxi`=Hide, `kxg`=bật toàn cục theo remote config `tutorials_enabled`/GameResources.IsTutorialEnabled). `TutorialController` gốc thân rỗng (dead).
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
