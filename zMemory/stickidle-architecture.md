---
name: stickidle-architecture
description: Kiến trúc project 2026StickIdle (idle RPG Unity) — dùng làm base/tham khảo cho các project khác
metadata: 
  node_type: memory
  type: project
  originSessionId: 2353e537-b729-4ea6-a1ea-1d48264d4ff5
  modified: 2026-07-24T04:13:09.823Z
---

Project `E:\00Work\00Project\2026StickIdle` — idle RPG Unity (Firebase Firestore + Spine + MaxSdk + AppsFlyer).

Cấu trúc lõi (tất cả code game ở `Assets/_Assets/Scripts`, ~960 file .cs, mỗi feature 1 folder):
- **GameData**: `GameData` (static class) chứa `StaticGameData` (config tĩnh, mỗi feature 1 class `StaticXxxData`) + `UserData` (save data, `[FirestoreData]`, mỗi feature 1 class `UserXxxData` kế thừa `BaseUserData`, key dạng `key_user_xxx`). Save local qua PlayerPrefs + backup Firestore ~300s.
- **GameConfig**: `Singleton<GameConfig>` — build mode, feature flags (enableXxx / FeatureStatus), remote event config.
- **Database**: `DatabaseManager` (Firebase Auth/Firestore), có auto-ban khi phát hiện hack (`ProtectedConst.XOR_INT/LONG` chống memory hack).
- **Scene flow**: Root.unity (init Firebase/DOTween) → Login.unity → Lobby.unity (gameplay chính, `LobbyManager` + footer tab views).
- **GameDesignPatterns**: Singleton, Observer (GameEvents), ObjectPooling.
- **UI**: `UIManager`, `BaseUI`, `UIViewBase`, prefix quy ước: `UI*` (màn hình), `Popup*`, `Cell*`, `Board*`, `L*` (layout con), `Box*`.
- **Battle**: BattleMechanic (AttackData, CrowdControl, DoT, Shield, StatAdjustment), Units (`BaseUnit` + Heroes/Enemies), Stats, Skills.
- Feature folders điển hình: Heroes, Gears, Relic, Trait, Mastery, Training, Quests, Shop, BattlePass, Blessings, Alchemy, Astrology, LeagueBoss, LootVillage, Treasure, Spin, CollectionBook, FeatureUnlocker, Tutorials.

Liên quan: [[dungeonrush-config-format]]
