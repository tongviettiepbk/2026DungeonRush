---
name: dungonrush-campaign-map-original
description: "Map campaign dựng lại theo thân hàm gốc 2026-10-17 — chướng ngại rw.itr (đối xứng, seed level×7912), hero ô (2,giữa), pet hàng dưới cách 2, quái ijx; CHƯA test Play; còn chuyển màn qua cửa"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-17T09:35:37.590Z
  originSessionId: 51bb02ac-00a4-4f57-8b9a-dcfdd87c3bf0
---

User yêu cầu (2026-10-17): bỏ phần map + object trong map tự dựng, làm lại ĐÚNG gốc, campaign trước.

**Đã làm (compile sạch, test thuật toán bằng harness dotnet, CHƯA test Play):**
- `MapGenerator` = port `rw.itr`: ứng viên chỉ nửa trái hàng 3..8, `count = Random(level×7912+round×1000).Next(5,11)`, bốc bằng `Random(…+try)`, lật gương, BFS 8 hướng; fallback `iua`. Map đối xứng, cột giữa luôn trống, 10–20 box. `NetRandom.cs` (tách ra từ EnemySpawnGenerator) = System.Random legacy (đã so khớp).
- Seed: campaign = `GetLevel(stageId)` (= User.Level gốc), dungeon = level dungeon → `BaseMode.GetLayoutLevel` (thay GetMapSeedKey).
- `MapSpawnCells.cs` = GridManager/CompanionController gốc: hero `ijp` (ô trống gần hàng 2 cột giữa), pet đầu màn `gmd` (hàng dưới hero, cách 2 cột, canh giữa), pet giữa trận `gmf`, quái `ijx` (hàng ≥5, flood 8 hướng, Random). DungeonMode.PickEnemyCells dùng chung.
- `CampaignLevelBuilder` chỉ còn dựng lưới (bỏ playerSpawnCells/enemySpawnCells/doorCell/enemies); `EnemySpawnGenerator.Generate(stageId, cells)` + `CampaignCount`.
- `MainMapPrefab` PointStart local (0.5,0.5) → **(0.535, 1.17)** cho ô khớp tường như gốc (gốc: ô (−4,0) = MapOffset + (−3.965, −5.63)).
- BossRush/PvP override `GetHeroSpawnCell` giữ ô cũ (hàng 0) — chưa đối chiếu gốc.

**Chưa làm (đợt sau):** chuyển màn qua cửa (`LevelTransitionController`: mở cửa, hero đi ra cửa trên, fade, màn sau vào từ cổng dưới `NextSpawnAtBottomGate`); di chuyển A* 8 hướng như PathfindingManager gốc (project đang 4 hướng); camera offset theo MapConfig; PointStart các map dungeon chưa căn; Dragon đứng (0,H−3) gốc.

Doc gốc: `DecodedData/MAP_AND_SPAWN_MODEL.md` mục 7. Thay phần đoán cũ ở [[dungonrush-map-spawn-procedural]]; quy ước toạ độ [[dungonrush-map-coord-convention]].
