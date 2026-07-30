# Cấu trúc Map & Mode hiện tại (sau refactor 2026-07-30)

> Đây là cấu trúc **implementation của mình** (khác `MAP_AND_SPAWN_MODEL.md` — cái đó là phân tích game gốc).
> Mọi đường dẫn dưới đây tương đối với `2026DungeonRushUnity/Assets/_Assets/Scripts/`.

## 0. Tóm tắt 1 dòng

Map tách làm 3 tầng rõ ràng — **config nhỏ → generator ra ma trận 0/1 → `MapController` (Singleton) quản tất cả**;
state trận gộp vào `BaseMode`; `MapController` là **1 nguồn sự thật duy nhất** cho grid/wall/di chuyển/tìm đường.

---

## 1. Sơ đồ tổng

```
MapController (Singleton MonoBehaviour)   ← QUẢN TẤT CẢ về map
  ├─ MapConfig            : rows / cols / cellSize
  ├─ int[,] grid          : 0 = trống, 1 = wall   (do MapGenerator sinh)
  ├─ GridSpace            : cell ↔ world, bounds (minWorld/maxWorld)
  ├─ IsWall / IsWalkable / InBounds
  ├─ FindPath (A* 4 hướng)                 ← hero/enemy đi tới đích (list ô ngắn nhất)
  └─ ResolveMove / ClampPointInMap         ← di chuyển liên tục né wall theo frame

BaseMode (MonoBehaviour)                  ← QUẢN state 1 trận
  ├─ teamA / teamB / isPause
  └─ Awake() tự đăng ký GameController.mode = this

GameController (Singleton)
  └─ mode : BaseMode      (registry unit + driver tick AI)
```

---

## 2. Từng file

| File | Vai trò |
|---|---|
| `GamePlay/Map/MapConfig.cs` | Config nhỏ nhất của lưới: `rows`, `cols`, `cellSize` (ô vuông). Quy ước ô `(x,y)`: x=cột, y=hàng. |
| `GamePlay/Map/MapGenerator.cs` | Sinh procedural → **`int[cols,rows]`** (0/1). Đảm bảo thông `start`→`goal`. Chỉ lo wall. |
| `GamePlay/Map/MapController.cs` | **Singleton**. Giữ config+grid, chuyển toạ độ, `FindPath` (A*), `ResolveMove/ClampPointInMap`. |
| `GamePlay/Map/StaticMapData.cs` | Bảng config theo môi trường (`MapEnvironmentConfig`) + helper layout spawn/cửa. |
| `GamePlay/Map/CampaignLevelBuilder.cs` | Ghép generator + spawn thành 1 `CampaignLevel` từ `stageId`. |
| `GamePlay/Map/EnemySpawnGenerator.cs` | Sinh enemy (nhận `enemySpawnCells` + `bossCell`, không còn phụ thuộc map object). |
| `GamePlay/Combat/GridSpace.cs` | Struct chuyển đổi ô↔world (CellSize/GridOrigin). Giữ nguyên. |
| `GamePlay/Mode/BaseMode.cs` | Vòng đời mode + **state trận** (teamA/teamB/isPause). |
| `GamePlay/GameController.cs` | `mode : BaseMode` + registry unit + tick AI. (Đã xóa `CombatMode`/`CombatMap`.) |

---

## 3. API chi tiết

### 3.1 `MapConfig`
```csharp
new MapConfig(rows, cols, cellSize = 1f);
bool InBounds(int x, int y);      // và InBounds(Vector2Int)
// grid indexed [x, y]: x ∈ [0,cols), y ∈ [0,rows)
```

### 3.2 `MapGenerator` (static)
```csharp
class Params {
    MapConfig config;
    int seed;
    int minWalls, maxWalls;               // khoảng số wall random
    int maxAttempts;                      // thử lại để còn đường
    Vector2Int start, goal;               // 2 ô luôn giữ trống + phải thông nhau
    IEnumerable<Vector2Int> keepClear;    // ô cấm đặt wall (hàng spawn, cửa)
}
int[,]  Generate(Params p);               // 0 = trống, 1 = wall
List<Vector2Int> CollectWalls(int[,] grid, MapConfig cfg);
```
Thuật toán: random `[min,max]` ô làm wall → loại ô cấm → BFS kiểm tra thông `start→goal` →
fail thì thử lại `maxAttempts` lần → fallback giảm dần số wall (cùng lắm = 0). Deterministic theo `seed`.

### 3.3 `MapController` (Singleton)
```csharp
// Nạp / sinh
void   SetMap(MapConfig config, int[,] grid, Vector3 origin, bool centerHorizontally = true);
int[,] Generate(MapConfig config, MapGenerator.Params p, Vector3 origin, bool centerHorizontally = true);
void   Clear();

// Truy vấn ô
bool InBounds(int x, int y) / (Vector2Int);
bool IsWall(int x, int y)   / (Vector2Int);
bool IsWalkable(int x, int y) / (Vector2Int) / (Vector3 world);

// Toạ độ
Vector3    CellToWorld(int x, int y) / (Vector2Int);
Vector2Int WorldToCell(Vector3 world);

// Di chuyển liên tục (né wall, trong biên)
Vector3 ClampPointInMap(Vector3 point);
Vector3 ResolveMove(Vector3 from, Vector3 dir, float step);

// Tìm đường A* (4 hướng, heuristic Manhattan)
List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal);   // gồm cả 2 đầu; rỗng nếu bí
List<Vector2Int> FindPath(Vector3 startWorld, Vector3 goalWorld);
```

### 3.4 `StaticMapData`
```csharp
// Hằng số
GRID_WIDTH = 9, GRID_HEIGHT = 12
MIN_OBSTACLE_COUNT = 10, MAX_OBSTACLE_COUNT = 15
MAX_GENERATION_ATTEMPTS = 10
PLAYER_SPAWN_ROWS = 2, ENEMY_SPAWN_ROWS = 2

// Bảng theo môi trường (7 map): camera offset, spawnObstacles, hasDoor...
class MapEnvironmentConfig { environment, gridWidth, gridHeight, hasDoor, spawnObstacles, camera... }
MapEnvironmentConfig GetConfig(ModeType env);

// Helper layout (suy từ kích thước — KHÔNG nằm trong ma trận wall)
Vector2Int        GetDoorCell(MapConfig);          // giữa hàng trên
List<Vector2Int>  GetPlayerSpawnCells(MapConfig);  // 2 hàng dưới
List<Vector2Int>  GetEnemySpawnCells(MapConfig, int[,] grid);  // 2 hàng trên, trừ wall
HashSet<Vector2Int> GetKeepClearCells(MapConfig);  // hàng spawn + cửa
```

### 3.5 `CampaignLevelBuilder.CampaignLevel`
```csharp
int stageId; ModeType environment;
MapConfig config;
int[,] grid;                       // 0/1
List<Vector2Int> obstacles;        // ô grid==1
List<Vector2Int> playerSpawnCells; // hàng dưới
List<Vector2Int> enemySpawnCells;  // hàng trên (đã trừ wall)
Vector2Int doorCell;               // = enemySpawnCell (cửa/cổng enemy)
List<EnemySpawnGenerator.EnemySpawnInfo> enemies;
```

### 3.6 `BaseMode`
```csharp
List<BaseUnit> teamA, teamB;   // list thật, BaseMode tự giữ
bool isPause;
protected virtual void Awake() { GameController.Instance.mode = this; }  // tự đăng ký
// vòng đời: Initialize → CreateMap / CreateTeamA / CreateTeamB → InitModeDone → StartGame
```

---

## 4. Luồng dựng 1 màn (CampaignMode)

```
CampaignMode.CreateMap()
 → CampaignLevelBuilder.Build(stageId, env)
     → MapConfig(rows, cols) từ MapEnvironmentConfig
     → MapGenerator.Generate(...) → int[,] 0/1   (start = giữa hàng dưới, goal = cửa)
     → CampaignLevel { config, grid, obstacles, spawn cells, door }
     → EnemySpawnGenerator.Generate(stageId, enemySpawnCells, doorCell)
 → MapController.Instance.SetMap(config, grid, origin)   // sẵn sàng pathfinding + di chuyển
 → spawn environment/obstacle theo grid/obstacles
CampaignMode.CreateTeamA() → spawn Hero/Pet (dùng MapController.ClampPointInMap)
CampaignMode.CreateTeamB() → spawn Enemy theo enemies[]
```

`BaseUnit` di chuyển gọi `MapController.Instance.ResolveMove(...)` / `ClampPointInMap(...)`.

---

## 5. Thay đổi so với trước

- **Bỏ** `MapGenerator.GeneratedMap` (object giàu cell-type) → generator trả thẳng `int[,]` 0/1.
- **Bỏ** enum `MapCellType` (mô hình cell-type cũ).
- **Bỏ** class `CombatMap` (trong GameController) → hút vào `MapController`.
- **Bỏ** class `CombatMode` → state trận gộp vào `BaseMode`.
- Đổi tên nested `StaticMapData.MapConfig` → `MapEnvironmentConfig` (nhường tên `MapConfig` cho type mới).
- `GameController.mode` : `CombatMode` → `BaseMode` (mode tự đăng ký khi Awake).

Trạng thái build: **0 error** (chỉ còn warning `System.Net.Http` của Unity, không liên quan).

---

## 6. Việc còn treo (chưa làm)

- **`FindPath` chưa nối vào AI di chuyển.** Unit hiện chỉ dùng `ResolveMove` (trôi thẳng né wall), chưa đi
  theo path A* từng ô. Đây là bước tiếp theo hợp lý.
- `mode` giờ **null cho tới khi một `BaseMode` Awake** (trước luôn non-null). Đã guard ở
  `GameController.AddUnit/RemoveUnit/ResetBattle/Update`. `BaseUnit` vẫn giả định `mode` đã set trong lúc đánh.
- `LevelRuntimeBuilder` (preview) cũng nạp `MapController` để test `FindPath` ngoài combat thật.
