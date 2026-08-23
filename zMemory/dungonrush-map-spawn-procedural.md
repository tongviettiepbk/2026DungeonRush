---
name: dungonrush-map-spawn-procedural
description: DungeonRush map layout & enemy spawn KHÔNG có data cố định — procedural runtime; đã điều tra kỹ 4 nguồn
metadata: 
  node_type: memory
  type: project
  originSessionId: c4c0092e-6043-46aa-b9ff-9702faa9e263
  modified: 2026-07-25T18:15:34.516Z
---

Map (vị trí box) và enemy spawn của DungeonRush **KHÔNG lưu toạ độ cố định** trong bất kỳ data nào của bản build hiện tại (xapk 41). Đã quét: AssetRipper export, `assets/bin/Data` (UnityPy, 32.5k objects — 0 instance GridLevelRuntimeData/SpawnPosition), Addressables (chỉ localization), remote config (chỉ scaler người chơi).

**Cơ chế thật:**
- Map chính sinh procedural bằng `LevelGenerator` (guid 396d866662b9cbc778a7b07df4c43850): grid vùng chơi 8×12 (MapConfig ghi 9×12 gồm viền), ObstacleCount 10, Min/Max 10/15, RandomSeed 31577, MaxGenerationAttempts 10, KeepClearZones (vùng cấm), có flood-fill kiểm tra liên thông cổng→cửa. Instance mẫu = World1/Level2.
- Enemy: `SpawnController`+`SpawnPosition` (EnemySpawnTransform+ArmySpawnTransforms), gear theo `DungeonThemeData` (Cultist/Zombie/DragonBoss), enemy dùng stat tuyệt đối (Health/AttackPower) chứ không có "level".
- Layout tay chỉ tồn tại dưới dạng schema `GridLevelRuntimeData` (Cells[]+BrushNames[]+EnemyGear[]=CreativeCharacterGearData, SpawnerGear, SnakeData) nhưng KHÔNG ship asset nào.
- Độ khó = `TierData` 8 bậc (Normal→Hell III) + scaler tiến trình (army_power_level_scaler=20, exponential=√10≈3.162, experience_level_base=49/mult=10).

Tài liệu đầy đủ: `DecodedData/MAP_AND_SPAWN_MODEL.md`. Xem thêm [[dungonrush-map-grid-background]], [[dungeonrush-config-format]].
