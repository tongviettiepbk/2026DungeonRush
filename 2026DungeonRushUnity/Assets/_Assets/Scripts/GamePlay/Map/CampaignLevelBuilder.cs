using System.Collections.Generic;
using UnityEngine;

// Ghép MapGenerator + EnemySpawnGenerator thành 1 màn campaign hoàn chỉnh từ stageId.
// Đây là entry point tầng gameplay sẽ gọi để dựng màn (thay cho việc load asset layout,
// vì DungeonRush sinh procedural — xem DecodedData/MAP_AND_SPAWN_MODEL.md).
//
// MapGenerator chỉ trả ma trận wall 0/1; spawn/cửa được suy từ StaticMapData và gói vào
// CampaignLevel để tầng scene (CampaignMode/LevelRuntimeBuilder) dùng.
public static class CampaignLevelBuilder
{
    public class CampaignLevel
    {
        public int stageId;
        public ModeType environment;

        public MapConfig config;
        public int[,] grid;                          // 0 = trống, 1 = wall
        public List<Vector2Int> obstacles;           // ô grid==1 (tiện spawn prefab)

        public List<Vector2Int> playerSpawnCells;    // hàng dưới
        public List<Vector2Int> enemySpawnCells;     // hàng trên (đã trừ wall)
        public Vector2Int doorCell;                  // cửa = cổng enemy (giữa hàng trên)
        public Vector2Int enemySpawnCell;            // alias doorCell (boss dùng)

        public List<EnemySpawnGenerator.EnemySpawnInfo> enemies;
    }

    public static CampaignLevel Build(int stageId, ModeType environment = ModeType.DefaultLevel)
    {
        StaticMapData.MapEnvironmentConfig envCfg = GameData.staticData.map.GetConfig(environment);

        // Vùng chơi = toàn bộ lưới 9×12 (chốt 2026-07-29, không trừ cột viền).
        int cols = envCfg != null ? envCfg.gridWidth : StaticMapData.GRID_WIDTH;
        int rows = envCfg != null ? envCfg.gridHeight : StaticMapData.GRID_HEIGHT;
        var config = new MapConfig(rows, cols, 1f);

        var door = StaticMapData.GetDoorCell(config);
        var start = new Vector2Int(cols / 2, 0);     // giữa hàng spawn quân dưới cùng

        int minWalls = StaticMapData.MIN_OBSTACLE_COUNT;
        int maxWalls = StaticMapData.MAX_OBSTACLE_COUNT;
        // Map không sinh box (Boss/PvP) → ép 0 wall.
        if (envCfg != null && !envCfg.spawnObstacles)
        {
            minWalls = 0;
            maxWalls = 0;
        }

        int[,] grid = MapGenerator.Generate(new MapGenerator.Params
        {
            config = config,
            seed = SeedFromStage(stageId),   // cùng màn → cùng layout
            minWalls = minWalls,
            maxWalls = maxWalls,
            start = start,
            goal = door,
            keepClear = StaticMapData.GetKeepClearCells(config),
        });

        var level = new CampaignLevel
        {
            stageId = stageId,
            environment = environment,
            config = config,
            grid = grid,
            obstacles = MapGenerator.CollectWalls(grid, config),
            playerSpawnCells = StaticMapData.GetPlayerSpawnCells(config),
            enemySpawnCells = StaticMapData.GetEnemySpawnCells(config, grid),
            doorCell = door,
            enemySpawnCell = door,
        };
        level.enemies = EnemySpawnGenerator.Generate(stageId, level.enemySpawnCells, level.enemySpawnCell);
        return level;
    }

    // Trộn stageId thành seed ổn định (không phụ thuộc hash string của .NET).
    private static int SeedFromStage(int stageId)
    {
        unchecked
        {
            int seed = stageId * 73856093;
            seed ^= (seed >> 13);
            seed *= 19349663;
            return seed & 0x7fffffff;
        }
    }
}
