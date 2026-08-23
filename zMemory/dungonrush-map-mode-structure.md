---
name: dungonrush-map-mode-structure
description: Cấu trúc implementation Map/Mode hiện tại của DungeonRush sau refactor 2026-07-30; doc ở DecodedData/MAP_MODE_STRUCTURE.md
metadata: 
  node_type: memory
  type: reference
  originSessionId: 3d395973-a614-46d6-912f-7815c5510a48
  modified: 2026-07-30T17:31:31.505Z
---

Cấu trúc code Map & Mode hiện tại (implementation của mình, KHÁC [[dungonrush-map-spawn-procedural]] vốn phân tích game gốc) được ghi ở `DecodedData/MAP_MODE_STRUCTURE.md`.

**QUY ƯỚC MẢNG 2 CHIỀU (chốt 2026-07-30, theo thói quen user — xem [[dungonrush-map-coord-convention]])**: ô = `Vector2Int(x=ROW, y=COL)`; grid lưu `int[rows,cols]`, truy cập `grid[cell.x,cell.y]=grid[row,col]`. Gốc `(0,0)` = góc **DƯỚI-TRÁI** (neo tại `MapController.pointStart` wire trong PREFAB); **row tăng → đi LÊN (+Y), col tăng → sang PHẢI (+X)**. Cửa/enemy ở hàng TRÊN = row rows-1; player spawn ở hàng DƯỚI = row 0. `Rows=GetLength(0)`, `Cols=GetLength(1)`. Khác GỐC (col-major) CHỈ ở thứ tự index + anchor; hướng trục Y GIỐNG gốc.

Tóm tắt kiến trúc sau refactor (2026-07-30):
- **2 tầng map**: `MapGenerator.Generate(Params{cols,rows,...})` trả `int[rows,cols]` 0/1 (0=trống,1=wall, đảm bảo thông start→goal) → `MapController` (Singleton) quản tất cả. `MapConfig` (rows/cols/cellSize) ĐÃ XÓA (2026-07-30, theo ý user): cols/rows suy từ `Grid.GetLength`, cellSize là hằng `MapController.CELL_SIZE=1f`; kích thước lưới truyền thẳng int vào generator/helper.
- `MapController` = 1 nguồn sự thật: giữ grid (Cols/Rows là property suy từ grid), cell↔world (đã gộp `GridSpace` vào đây — struct đó ĐÃ XÓA), `FindPath` (A* 4 hướng), `ResolveMove/ClampPointInMap/IsWalkable` (đã hút từ `CombatMap` cũ — class đó ĐÃ XÓA). `SetMap(grid,fallbackOrigin,center)`: nếu `pointStart` gán → gốc = pointStart.position, tắt canh giữa; else fallbackOrigin+center (hành vi cũ). **MapController giờ là component TRÊN root của `MainMapPrefab`, `pointStart` wire tới child PointStart NGAY TRONG PREFAB** (guid 3cfed0fe…). MapController có `Awake(){instance=this;}` → khi prefab Instantiate thì tự nhận Singleton Instance (khỏi bị Singleton auto-tạo bản rỗng không PointStart).
- `BaseMode` giữ state trận (teamA/teamB/isPause) VÀ dựng map. **THỨ TỰ `CreateMap` (2026-07-30, do MapController ở trên prefab): build level → tạo container → `SpawnEnvironment()` Instantiate mapPrefab tại `transform.position` và TRẢ VỀ MapController của bản đó → `map.SetMap(grid,…)` → `SpawnObstacles(map, obstacles)`.** SpawnEnvironment KHÔNG còn đặt env theo tâm lưới (tránh vòng lặp quẩn). mapPrefab null → fallback `MapController.Instance`. Helper: `EnsureGameDataLoaded/NewGroup/DestroyChild/container/currentLevel`. Mode con chỉ override spawn team. Class `CombatMode` ĐÃ XÓA; `GameController.mode` giờ type `BaseMode`. LƯU Ý scene: đừng để 1 bản MainMapPrefab pre-place SẴN trong scene song song với runtime-instantiate (sẽ nhân đôi map + tranh Singleton).
- `CampaignMode` giờ CHỈ còn spawn quân thật (Hero/Pet/Enemy có rig). `LevelRuntimeBuilder` (preview placeholder, trùng logic dựng map) ĐÃ XÓA (2026-07-30) — placeholder no-rig bỏ luôn theo ý user; scene `_MapPreview`/`_BattlePreview` còn tham chiếu nó sẽ báo missing script.
- Đã bỏ `MapGenerator.GeneratedMap` + enum `MapCellType`. Spawn/cửa suy từ `StaticMapData` helper (`GetDoorCell/GetPlayerSpawnCells/GetEnemySpawnCells/GetKeepClearCells`, giờ nhận `(int cols,int rows[,grid])` thay vì MapConfig). Nested config đổi tên `StaticMapData.MapConfig` → `MapEnvironmentConfig`.

Còn treo: `FindPath` CHƯA nối vào AI di chuyển (unit mới chỉ `ResolveMove` trôi thẳng). Build 0 error (chỉ warning System.Net.Http của Unity). Compile-check: `dotnet build Assembly-CSharp.csproj` trong 2026DungeonRushUnity.
