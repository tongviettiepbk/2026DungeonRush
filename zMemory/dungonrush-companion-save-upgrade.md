---
name: dungonrush-companion-save-upgrade
description: Save & nâng cấp companion — schema CompanionModel + UserCompanionData + card-per-level
metadata:
  type: project
---

Save + level companion (reverse il2cpp v41, xem DecodedData/COMPANION_MODEL.md §7). Khác hệ meta
unlock/summon ở [[dungonrush-companion-unlock-summon]]; dùng cho level pet in-battle [[dungonrush-companion-battle-classes]].

**Schema GỐC:** `CompanionModel { CompanionId(assetName), Level, CardCount, IsNew }` ;
`User.OwnedCompanions: List<CompanionModel>` ; `User.EquippedCompanions: List<string>` (tối đa 3, khoá=assetName).
Lên cấp bằng THẺ (tích CardCount), KHÔNG có XP.

**Nâng cấp (class helper `ly`):** max level **100** (`ly.gpx`); cards/level = BẢNG int[] (`ly.gpz`, index=level),
fallback **16** khi vượt bảng. Bảng int[] nằm trong ScriptableObject companion manager, **CHƯA trích** → tạm fallback 16.

**ĐÃ dựng trong project (compile sạch):**
- `Companions/CompanionModel.cs`, `Companions/UserCompanionData.cs` (owned/equipped + GetLevel/Own/AddCards/Equip/Unequip),
  `Companions/CompanionUpgradeConfig.cs` (MAX_LEVEL 100 + GetCardsRequired fallback 16).
- Đăng ký ở `UserData` (DATA_KEY_COMPANION + field `companions` + Load + ValidateData list) — pattern BaseUserData.
- `CampaignMode.SpawnHeroAndPets`: pet level lấy từ `GameData.userData.companions.GetLevel(assetName)` (chưa sở hữu → 1).
- Cheat test ở `_Test/GameDataTester.cs`: "Companion - Add 16 Cards (DPS)" / "Log Owned".

**CÒN LẠI:** (a) trích bảng int[] card-per-level thật; (b) disasm `UserController.edi` (summon apply);
(c) spawn theo EquippedCompanions thật (giờ vẫn dùng petPrefab gán tay ở mode inspector).
