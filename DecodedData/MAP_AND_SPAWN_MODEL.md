# Dungeon Rush — Mô hình Map & Sinh Enemy (reconstructed)

> Tổng hợp từ: `DecodedData/tables/*` (config đã decode), scripts trong `AssetRipper/ExportedProject/.../Assembly-CSharp/*.cs` (field schema, body bị strip il2cpp), `GameplayScene.unity` (giá trị serialized thật), `remote_config_live.json`, và quét trực tiếp `assets/bin/Data` trong xapk bằng UnityPy.

## 0. Kết luận quan trọng nhất

**Không có "bản đồ" nào lưu toạ độ box/enemy cố định theo từng level.** Đã kiểm tra 4 nguồn:

| Nguồn | Kết quả |
|---|---|
| AssetRipper export (.asset) | 0 file `GridLevelRuntimeData`; `LevelGenerator` chỉ có 1 instance test trong scene |
| xapk `assets/bin/Data` (UnityPy, 32.525 objects) | 0 instance `GridLevelRuntimeData` / `SpawnPosition` / per-level config; chỉ có controller singleton trong GameplayScene |
| Addressables `assets/aa/` | Chỉ có localization bundle |
| Remote config | Chỉ có scaler tiến trình **người chơi**, không có bảng enemy/level |

→ Map chính **sinh procedural lúc runtime** từ seed + số lượng obstacle. Enemy **spawn procedural** và stat **scale theo tiến trình**, không có "level enemy" cố định từng màn. Layout thiết kế tay chỉ tồn tại dưới dạng **hệ thống** (`GridLevelRuntimeData` painter) nhưng **không ship asset nào** trong bản này.

---

## 1. Map config (dữ liệu cụ thể — `MapConfig`)

7 map, tất cả grid **9×12**, đều có cửa (`HasDoor=1`).

| Map | EnvironmentType | Grid | SpawnObstacles | CamOrthoOffset | CamPosOffset | #ObstaclePrefab |
|---|---|---|---|---|---|---|
| **MainMap** | DefaultLevel (0) | 9×12 | ✅ 1 | 1 | (0, −0.5) | 1 |
| BossRushMap | BossRush (1) | 9×12 | 0 | 0 | (0, 0) | 1 |
| DragonMap | DragonBossDungeon (2) | 9×12 | 0 | 2.5 | (0, −1.5) | 0 |
| ZombieMap | ZombieHordeDungeon (3) | 9×12 | ✅ 1 | 0.5 | (0, 0) | 1 |
| PvpMap | PvP (4) | 9×12 | 0 | 1 | (0, −1) | 1 |
| ChatPvpMap | ChatBattle (5) | 9×12 | 0 | 1 | (0, −1) | 1 |
| CultistMap | CultistDungeon (6) | 9×12 | ✅ 1 | 0.5 | (0, 0) | 1 |

- `SpawnObstacles=1` → map dùng bộ sinh procedural (obstacle/box). Boss & PvP map = 0 (đấu trường trống).
- Mỗi map trỏ tới `EnvironmentPrefab` (guid) và `ObstaclePrefabs` (guid) riêng.

---

## 2. Sinh obstacle/box procedural — `LevelGenerator`

Schema field (`LevelGenerator.cs`) + **giá trị thật** đọc từ instance trong `GameplayScene.unity` (đây là màn test World 1 / Level 2):

| Field | Giá trị test | Ý nghĩa |
|---|---|---|
| `GridWidth` × `GridHeight` | **8 × 12** | vùng chơi (MapConfig ghi 9 = có thêm cột viền/cửa) |
| `CellSize` | 1 | kích thước 1 ô = 1 world unit |
| `GridResolution` | 1 | mật độ chia ô |
| `GridOrigin` | (0, 0) | gốc toạ độ lưới |
| `ObstacleCount` | 10 | số box mục tiêu |
| `MinObstacleCount` / `MaxObstacleCount` | 10 / 15 | khoảng random số box |
| `RandomSeed` | 31577 | seed → cùng seed = cùng layout |
| `MaxGenerationAttempts` | 10 | số lần thử để đảm bảo map hợp lệ (có đường đi) |
| `KeepClearZones` | `[]` | vùng cấm sinh box (dạng `BottomLeft`/`TopRight` Vector2Int) |
| `ObstaclePrefabs` | 1 prefab | prefab box (đổi theo `TierData`) |
| `ProceduralLevelId` / `WorldNo` / `LevelNo` | 1 / 1 / 2 | định danh màn |
| `TierData` | (theo độ khó) | quyết định bộ prefab obstacle |

**Thuật toán (suy ra từ tên method đã bị strip body):** chọn ngẫu nhiên `[Min,Max]` ô làm box → lọc bỏ ô nằm trong `KeepClearZones` (cổng spawn, cửa) → kiểm tra liên thông bằng BFS/flood-fill (`itc`/`itd` nhận `HashSet<Vector2Int>`) để chắc chắn còn đường từ cổng dưới lên cửa trên → nếu fail thì thử lại tối đa `MaxGenerationAttempts`. Vị trí cuối là `List<Vector2Int>` → đổi sang world qua `CellSize`/`GridOrigin`. **Không lưu ra file.**

### KeepClearZone (schema vùng cấm)
`Name`, `BottomLeft: Vector2Int`, `TopRight: Vector2Int`, `ShowGizmo`, `GizmoColor` — hình chữ nhật ô được giữ trống.

---

## 3. Mô hình spawn enemy

### 3a. Runtime chính — `SpawnController` + `SpawnPosition`
- `SpawnPosition`: `EnemySpawnTransform` (cổng enemy) + `List<Transform> ArmySpawnTransforms` (điểm spawn quân của người chơi). Là Transform trong prefab environment, không phải bảng.
- `SpawnController` phát/nhận enemy, boss (`hvo(BossGateBossDefinition, int)`), PvP (`hvh(..., Vector2Int)`), BossRush (`hvl(List<BossRushPlayerModel>, List<Vector2Int>, ...)`).

### 3b. Enemy theo dungeon — `DungeonThemeData` (dữ liệu cụ thể)
Định nghĩa **trang bị/animator enemy theo loại dungeon** (không phải toạ độ):

| Theme | DungeonType | EnemyCanMove | ColliderRadius× | Có ranged? |
|---|---|---|---|---|
| CultistDungeonThemeData | Cultist (2) | ✅ | 1.0 | ✅ (weapon+helmet+gloves ranged) |
| ZombieInvasionData | ZombieHorde (1) | ✅ | 1.0 | ❌ (chỉ helmet+gloves) |
| DragonHordeData | DragonBoss (0) | ❌ (đứng yên) | 1.1 | ❌ |

Mỗi theme trỏ `EnemyAnimator`, `EnemyWeapon/Helmet/Gloves` (+ biến thể `Ranged`) qua guid.

### 3c. Dungeon & phần thưởng — `DungeonData`
| Dungeon | Type | Tên hiển thị | Reward |
|---|---|---|---|
| CultistDungeon | Cultist | Cultist Dungeon | CultistDungeonKey |
| DragonHorde | DragonBoss | Dragon's Hoard | ZombieHordeDungeonKey |
| ZombieInvasion | ZombieHorde | Zombie Outbreak | Bone |

---

## 4. Hệ thống level thiết kế tay — `GridLevelRuntimeData` (schema; KHÔNG ship asset)

Đây là "grid painter" — **nếu** có asset thì đây chính là nơi lưu toạ độ box/enemy. Schema:

```
GridLevelRuntimeData (ScriptableObject)
├─ Width, Height                         // kích thước lưới
├─ BrushNames : string[]                 // bảng "cọ vẽ" (Box/Enemy/Wall/Player/Spawner...)
├─ Cells      : int[]                     // lưới phẳng, mỗi ô = index vào BrushNames  ← BẢN ĐỒ VỊ TRÍ
├─ PlayerGear : CreativeCharacterGearData[]
├─ EnemyGear  : CreativeCharacterGearData[]   // định nghĩa enemy (xem dưới)
├─ SpawnerGear: CreativeSpawnerData[]
├─ SnakeData  : CreativeSnakeData[]
├─ DropSettings, CameraSettings, WallSettings, ... (nhiều Creative*Settings)
└─ Disable{Enemy,Player}HpUI, DisableDamageUI
```
`GridPainterConfig` map `BrushName → Prefab`. Đọc ô: `Cells[y*Width + x]` → tên brush → prefab.

### `CreativeCharacterGearData` (định nghĩa 1 enemy/player)
Trang bị: `Weapon, Helmet, Backpack, Wing, Cape, Sets[]`.
Stat **tuyệt đối** (KHÔNG phải "level"): `Health, AttackPower, AttackSpeed, RangeDistance, MoveSpeed, DetectionRange`.
Cờ hành vi: `CanMove, CanPatrol, IsStaticBody, EnableOneHitPerEnemy`.
Boss: `IsBoss, BossDefinitionIndex, BossHPOverride (double)`.

### `CreativeSpawnerData` (điểm phun enemy liên tục)
`SpawnInterval, SpawnDelay, TotalSpawnCount, Weapon/Helmet/Backpack, Health, AttackPower, AttackSpeed, RangeDistance, MoveSpeed, PathfindingActivationRange`.

### `CreativeSnakeData` (rắn enemy đi theo path)
`HeadCell: Vector2Int, Length, SegmentHp, Speed, PathCells: Vector2Int[]`.

---

## 5. Độ khó & "level" enemy

**Không có bảng "enemy level theo màn".** Độ khó đến từ:

### 5a. Tier độ khó — `TierData` (8 bậc)
Tier1 *Normal* → Tier2 *Hard* → Tier3 *Extreme* → Tier4 *Overkill* → Tier5 *Impossible* → Tier6 *Hell I* → Tier7 *Hell II* → Tier8 *Hell III*. Mỗi tier có `ObstaclePrefabs` + màu riêng.

### 5b. Scaler tiến trình (remote config — dùng cho sức mạnh/level người chơi, enemy scale bám theo)
| Key | Giá trị |
|---|---|
| `army_power_level_scaler` | 20 |
| `army_power_exponential_scaler` | 3.16227766 (≈ √10) |
| `experience_level_base` | 49 |
| `experience_level_mult` | 10 |
| `experience_level_scaler` | 1 |

`army_power_segments`: max_power ≤80 → segment 20; ≤200 → 30; còn lại → 40.

### 5c. XP mỗi level (`experience_required_per_level.csv`)
lvl1=200, lvl2=600, lvl3=1200, lvl4=3000, lvl5=4500, lvl6=6000, lvl7=9000, lvl8=10800, lvl9=12600, lvl10=14400 … (sau đó +1800/level).

---

## 6. Muốn có layout cố định thì làm gì

Bản build này **không chứa** asset `GridLevelRuntimeData`. Nếu bản game khác (hoặc content update tải về) có, chúng sẽ nằm trong Addressables bundle. Cách lấy:
1. Tải catalog Addressables (`catalog_*.json`) + bundle content của game.
2. Parse bằng UnityPy → tìm MonoBehaviour class `GridLevelRuntimeData` → đọc `Width/Height/BrushNames/Cells/EnemyGear/...`.
3. Dựng lại lưới: `for y,x: brush = BrushNames[Cells[y*Width+x]]`.

Còn với bản hiện tại, để tái tạo gameplay chỉ cần: **grid 9×12 (vùng chơi 8×12) + LevelGenerator procedural (params ở mục 2) + KeepClearZones cho cổng/cửa + DungeonThemeData cho gear enemy + Tier cho độ khó.**
