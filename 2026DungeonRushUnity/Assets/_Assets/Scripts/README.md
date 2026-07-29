# DungOnRush — Core Base (theo kiến trúc 2026StickIdle)

Base được bóc từ 2026StickIdle, cắt Firebase/Analytics/MasterInfo. Lộ trình 6 phần:

| # | Phần | Trạng thái |
|---|------|-----------|
| 1 | Scene flow (Root → Login → Lobby) | ✅ bản rút gọn (không Firebase/UIManager) |
| 2 | **Data (GameData / StaticGameData / UserData)** | ✅ phần này |
| 3 | GameConfig + feature flags | ⬜ |
| 4 | Gameplay & combat (Units, BattleMechanic) | 🟡 grid+spawn + foundation combat (BaseUnit + BattleMechanic) xong; managers còn slim/stub |
| 5 | UI (UIManager, BaseUI, Popup) | ⬜ |
| + | Localize (LocalizeManager, LocalizeText, 15 ngôn ngữ) | ✅ port + data JSON |
| 6 | Patterns (Singleton, Observer/EventDispatcher, ObjectPooling) | ⬜ |

## Kiến trúc Data

```
GameData (static hub)
├── staticData : StaticGameData      // config tĩnh — load 1 lần
│   ├── items    : StaticItemData    // từ ScriptableObject trong Resources
│   └── campaign : StaticCampaignData// formula placeholder
└── userData   : UserData            // save người chơi — PlayerPrefs (JSON per-module)
    ├── profile  : UserProfileData   (key_user_profile)
    ├── campaign : UserCampaignData  (key_user_campaign)
    ├── items    : UserItemData      (key_user_items)
    └── settings : UserSettingData   (key_user_settings)
```

- Gọi `GameData.Init()` **1 lần** lúc mở game (sau này ở scene Login).
- Mỗi module save kế thừa `BaseUserData`: chỉ ghi PlayerPrefs khi `isDataChanged = true`,
  có vòng đời `InitData()` (user mới) → `ValidateData()` (sửa data hỏng/migrate) →
  `RefreshNewDay/Week/Month/Year()` (reset theo thời gian).
- `UserData.Save()` throttle 1 giây; gọi `Save(true)` để ép ghi ngay (pause/quit).

## Thêm 1 module save mới (VD: Heroes)

1. Tạo `Scripts/Heroes/UserHeroData.cs : BaseUserData`, override `GetDataKey()`.
2. Trong `UserData.cs`: thêm `DATA_KEY_HEROES`, property `heroes`,
   thêm vào `listData` trong `ValidateData()`, thêm dòng `LoadModule` trong `Load()`.
3. Config tĩnh đi kèm: tạo `StaticHeroData` rồi đăng ký trong `StaticGameData.Load()`.

## Thêm 1 item mới

1. Thêm giá trị vào enum `ItemType` (`Common/GameEnums.cs`).
2. Tạo asset: chuột phải trong `Assets/_Assets/Resources/Scriptable Objects/Items`
   → Create > DungOnRush > Item Data.

## Test

Kéo `_Test/GameDataTester.cs` vào GameObject trống, Play, xem log.
Chuột phải component → Cheat/Add 1000 Gold, Pass Stage, Clear Save.

## Khác với StickIdle (chủ đích)

- Bỏ Firebase (`[FirestoreData]`, backup Firestore, auto-ban) — thêm lại sau nếu cần online.
- `UserData.Load()` dùng generic `LoadModule<T>()` thay vì ~30 hàm `LoadXxx()` lặp code.
- Thời gian dùng `GameUtils.GetTimeNow()` (giờ máy) — sau này có server time chỉ sửa 1 chỗ.
- Chưa post event (NewDay, ItemChanged...) — chờ phần 6 EventDispatcher, đã đánh dấu TODO.

## Combat foundation (port từ StickIdle — "gọn + adapt")

Bám kiến trúc StickIdle nhưng cắt xuống mức tối thiểu để compile + làm nền cho combat.

**Port thật (giữ nguyên logic):**
- `Stats/` — Stats, StatModifier (bỏ UI-string ext + Firebase), StatUtils (bỏ HandNavigationType), BaseStats.
- `BattleMechanic/` — 13 file: CC, DamageOverTime, Shield, Buff/Debuff, AttackData/TakeDamage/ProcessedAttack. (`TextDamageStatus` dùng lại bản ở `Common/GameEnums.cs`.)
- `Unit/BaseUnit.cs` — cắt: bỏ check UIGamePlay/UIManager trong sfx; `PostEvent` dùng extension `this.PostEvent`.

**Slim/stub (chỉ đủ bề mặt, đánh dấu `TODO(follow-stick)`):**
- `Unit/AnimationController.cs` — thay bản Spine 533 dòng, hầu hết no-op.
- `Unit/HealthBar.cs`, `Unit/CameraController.cs`, `Unit/AudioManager.cs`, `Unit/BaseBullet.cs`.
- `Unit/GameController.cs` — registry unit + vòng lặp tick AI (`Update`→`UpdateBehavior`) + `CombatMode`/`CombatMap` (biên + wall, né wall).

**Đã có sẵn, tái dùng:** `Effects/FxController` (đủ fx CC/heal + ShowDamage/ShowStatus), `Effects/BaseFx`, `Utilities/Yielder`, `Utilities/DebugCustom`, DOTween, Spine.

**MainMap gameplay (2026-07-29):** map 9×12 đầy đủ; di chuyển liên tục né wall (`BaseUnit.Moving`+`CombatMap.ResolveMove`); 3 unit trên `BaseUnit`: `HeroUnit` (chủ động tìm enemy gần nhất), `EnemyUnit` (thụ động, chỉ đánh trong `aggroRange`), `PetUnit` (đi theo hero + đánh trong `engageRange`). Phần chung (nạp stat từ baseStats, SpawnInBattle, đánh tạm ở OnAttackEnd, helper tìm enemy) gộp trong `BaseUnit` (region "Map unit"). Entry chạy trận: mode `CampaignMode : BaseMode` (`GamePlay/Mode/`, hấp thụ CombatDirector cũ — đã bỏ; spawn từ prefab có rig, nạp wall qua vòng đời BaseMode). Unity compile 0 lỗi; tag TeamA/TeamB đã thêm. **CHƯA chạy play-mode.**

**Cần làm khi tiếp tục:** khai báo tag `TeamA`/`TeamB` trong Unity; gán prefab unit có rig BaseUnit vào `CampaignMode`; AnimationController Spine thật; Hero mặc gear hiển thị (Spine slot); verify LIVE Unity. Compile-check: `dotnet build Assembly-CSharp.csproj`.
