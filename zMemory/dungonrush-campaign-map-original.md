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

**Chuyển màn qua cửa XONG (2026-10-17, compile sạch, CHƯA test Play):** `CampaignMode.RoutineLevelTransition` theo `LevelTransitionController.hrq`: 0.5s sau thắng → thu exp về hero + 2 cánh cửa (MapController.topDoor1/2 = MainDoor/MainDoor_1) trượt ±0.8 trong 0.8s InSine + âm rock_door 0.151 → hero đi A* tới hàng trên (cột giữa/trái giữa) → fade đen 0.5s trong lúc đi ra sau cửa (ô trên cùng giữa + (−0.5, 2.5)) → chờ loot → Build → fade sáng 0.5s. UIManager thêm FadeOut/FadeIn (giữ tối), AudioManager.PlaySfx(clip, volumeScale). **Màn sau KHÔNG vào từ cổng dưới**: NextSpawnAtBottomGate gốc luôn tắt (user từng nghĩ là có). Thua giữ luồng cũ (delayEndGame rồi Build).

**Chưa làm (đợt sau):** di chuyển A* 8 hướng như PathfindingManager gốc (project đang 4 hướng); camera offset theo MapConfig; BossRush PointStart chưa căn.

Doc gốc: `DecodedData/MAP_AND_SPAWN_MODEL.md` mục 7. Thay phần đoán cũ ở [[dungonrush-map-spawn-procedural]]; quy ước toạ độ [[dungonrush-map-coord-convention]].

**Dungeon (2026-10-17, compile sạch, CHƯA test Play):** PointStart Zombie/Dragon/Cultist (0.5,0.5) → (0.52,0.52) theo MapOffset gốc (0.48,0.13); rồng đứng cố định ô (rows−3, giữa) lệch −0.5 x (huu gốc); dungeon gốc KHÔNG chuyển màn qua cửa (LevelTransition bỏ qua dungeon). **Lệch gốc còn lại:** 3 prefab map dungeon trong project là bản sao MainMap (tường xám + CỬA) — gốc có tường/đồ trang trí riêng và không có cửa; sprite gốc nằm trong rip (atlas Zombie/Dragon, ảnh Cultist) chưa vào project.

**Fade chuyển màn chỉ tối vùng map (2026-10-21, test Play OK):** dùng `LevelProgressDarkImage` gốc — đã dời từ GameUI (đang tắt) ra SafeArea sibling 0, đáy = 830 (đáy hàng pet), gán `UIMainLobby.imgLevelProgressDark`; `UIManager.FadeOut(..., target)` nhận tấm phủ riêng, FadeIn dùng lại tấm đó. Top bar/nút offer vẫn sáng (giống gốc).
