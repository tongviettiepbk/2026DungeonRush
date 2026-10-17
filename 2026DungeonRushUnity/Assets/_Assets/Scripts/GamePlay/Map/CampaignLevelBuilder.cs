using System.Collections.Generic;
using UnityEngine;

// Dựng lưới 1 màn (thay cho việc load asset layout, vì DungeonRush sinh procedural —
// xem DecodedData/MAP_AND_SPAWN_MODEL.md). Gốc: EnvironmentController lấy MapConfig theo môi trường →
// GridManager.iiy(GridWidth, GridHeight) → LevelLoader.itp(level) sinh chướng ngại (MapGenerator).
//
// Chỗ đứng hero/pet/quái KHÔNG nằm ở đây — tầng mode chọn lúc spawn (MapSpawnCells), như SpawnController gốc.
public static class CampaignLevelBuilder
{
    public class CampaignLevel
    {
        public int stageId;
        public ModeType environment;
        public int layoutLevel;                      // level làm seed chướng ngại (campaign: level liên tục; dungeon: level dungeon)

        public int[,] grid;                          // 0 = trống, 1 = chướng ngại
        public List<Vector2Int> obstacles;           // ô grid==1 (tiện spawn prefab)
    }

    public static CampaignLevel Build(int stageId, ModeType environment, int layoutLevel)
    {
        StaticMapData.MapEnvironmentConfig envCfg = GameData.staticData.map.GetConfig(environment);

        int cols = envCfg != null ? envCfg.gridWidth : StaticMapData.GRID_WIDTH;
        int rows = envCfg != null ? envCfg.gridHeight : StaticMapData.GRID_HEIGHT;
        bool spawnObstacles = envCfg == null || envCfg.spawnObstacles;

        int[,] grid = MapGenerator.Generate(layoutLevel, cols, rows, spawnObstacles);
        return new CampaignLevel
        {
            stageId = stageId,
            environment = environment,
            layoutLevel = layoutLevel,
            grid = grid,
            obstacles = MapGenerator.CollectWalls(grid),
        };
    }
}
