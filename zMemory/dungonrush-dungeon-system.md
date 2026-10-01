---
name: dungonrush-dungeon-system
description: Hệ Dungeon (3 mode lấy tài nguyên) — luật key/ads/thưởng đã reverse, data+service+UI XONG; Dragon's Hoard CHƠI ĐƯỢC (ChangeMode + DragonDungeonMode), Zombie/Cultist chưa có trận
metadata:
  type: project
  modified: 2026-10-01
---

Reverse đầy đủ ở `DecodedData/DUNGEON_MODEL.md`. Luật: 2 key miễn phí/ngày (reset 0h **UTC**, SET không cộng), ads +1 bonus key tối đa 3 lần/ngày/dungeon, CHỈ THẮNG mới tiêu key, thắng → thưởng `base+scaler*(level-1)` rồi level+1; Sweep Last = tiêu 1 key lấy thưởng màn level-1. Dragon's Hoard→Hammer(=LOOT_TICKET) 50/1, Zombie Outbreak→Bone 100/2 (mở lv5), Cultist Ritual→Vial 100/2 (mở lv20). Độ khó hiển thị "c-s", 10 level/độ khó.

**Code (2026-10-01, compile sạch):** `Scripts/Dungeons/` StaticDungeonData (nhúng hằng số, DungeonConfig) + UserDungeonData (key `key_user_dungeon`, dict theo (int)DungeonType, `lastResetTime` unix giây UTC) + DungeonService (CheckDailyReset/Sweep/OnAdWatched/CompleteDungeon). `ItemType.VIAL=6`. UI: UITabDungeon (countdown, 3 ElementDungeonUI) → `UIKey.DungeonPopup`="UIDungeonPopup" (UIDungeonPopupStart, icon qua mảng `rewardIcons/keyIcons` đã gán trong prefab: box/bone/vial, red/green/white key). Ads placement "dungeon_key".

**Dragon's Hoard XONG (2026-10-01, test Play mode):** GameController.ChangeMode(ModeType) kiểu StickIdle — prefab mode ở `Resources/Prefabs/Game Modes/` (CampaignMode, DragonDungeonMode; scene vẫn giữ CampaignMode ban đầu). Hero/Pet spawn (SpawnHeroAndPets/SyncPets/GetPet/OnAllyDie theo hero) đã DỜI từ CampaignMode lên BaseMode; UI dùng `GameController.Instance.mode` (BaseMode). `DungeonMode` (chung: level, HUD `UIDungeonHud`, thắng→CompleteDungeon, popup `UIDungeonEndPopup`, Exit→về campaign không tiêu key) + `DragonDungeonMode` (1 rồng ô ngẫu nhiên row≥5). Rồng = `Prefabs/Units/Enemies/DragonEnemy` (variant 00Enemy: animator Dragon, Soldier quay 180°, collider×1.1, helmet=thân rồng, 2 tay=cánh, aggro 100, DragonWeaponData; đạn Projectile_Dragon_Red speed 12 = ProjectileSpeed gốc — các đạn khác vẫn 5 vì WeaponData.projectileSpeed chưa được đọc). Chỉ số: `EnemySpawnGenerator.GenerateDragon` — rồng là role RANGE (r=2, dmg×0.8), KHÔNG phải boss ratio 10 (doc cũ sai, đã sửa ENEMY_STATS_MODEL). Popup Enter/Ads chung chỗ (kch gốc): còn key→Enter, hết key+còn ads→Ads. Đang trong dungeon bấm Enter → toast "Dungeon in progress".

**CHƯA LÀM:** Zombie/Cultist mode (kế thừa DungeonMode, override CreateTeamB). Camera offset theo MapConfig (Dragon ortho+2.5) chưa áp ở mọi mode. Hiệu ứng LootDrop khi thắng (gốc có). Tab Dungeon chưa khoá PlayerLevel 4. Lobby vẫn thao tác được trong trận dungeon (loot/forge).
Lỗi CÓ SẴN thấy khi test: đạn pet bay tiếp sau khi campaign Build lại màn → MissingReference PetCompanionAoeSlow (Reset không post ResetMode); Projectile_Weapon_Arrow4/Arrow9 thiếu BaseBullet.Transform.
Xem [[dungonrush-mediation-ads]], [[dungonrush-companion-unlock-summon]].

**Mẹo:** Khi tool UnityMCP không load trong phiên nhưng server 8080 chạy → gọi JSON-RPC thẳng (initialize → tools/call execute_code). Play mode cần Unity được focus; Login phải bấm Tap to Play (gọi Login.ClickBtTapToPlay qua reflection); đừng gọi ChangeMode khi còn ở Login (Fade bị chiếm → kẹt). UnityMCP có thể đang nối project KHÁC (2026IdleSava) → check `mcpforunity://instances` trước; compile-check offline trên Mac bằng csc trong `/Volumes/Work/installed/6000.3.9f1/Unity.app/Contents/Resources/Scripting/` (DotNetSdkRoslyn/csc.dll + NetCoreRuntime/dotnet), nhớ thêm file .cs mới chưa có trong csproj.
