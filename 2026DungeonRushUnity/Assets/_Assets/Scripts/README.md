# DungOnRush — Core Base (theo kiến trúc 2026StickIdle)

Base được bóc từ 2026StickIdle, cắt Firebase/Analytics/MasterInfo. Lộ trình 6 phần:

| # | Phần | Trạng thái |
|---|------|-----------|
| 1 | Scene flow (Root → Login → Lobby) | ⬜ |
| 2 | **Data (GameData / StaticGameData / UserData)** | ✅ phần này |
| 3 | GameConfig + feature flags | ⬜ |
| 4 | Gameplay & combat (Units, BattleMechanic) | ⬜ |
| 5 | UI (UIManager, BaseUI, Popup) | ⬜ |
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
