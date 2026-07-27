using System.Collections.Generic;
using UnityEngine;

// Ghép MapGenerator + EnemySpawnGenerator thành 1 màn campaign hoàn chỉnh từ stageId.
// Đây là entry point tầng gameplay sẽ gọi để dựng màn (thay cho việc load asset layout,
// vì DungeonRush sinh procedural — xem DecodedData/MAP_AND_SPAWN_MODEL.md).
public static class CampaignLevelBuilder
{
    public class CampaignLevel
    {
        public int stageId;
        public ModeType environment;
        public MapGenerator.GeneratedMap map;
        public List<EnemySpawnGenerator.EnemySpawnInfo> enemies;
    }

    public static CampaignLevel Build(int stageId, ModeType environment = ModeType.DefaultLevel)
    {
        StaticMapData.MapConfig cfg = GameData.staticData.map.GetConfig(environment);

        var genParams = new MapGenerator.GenParams
        {
            // Vùng chơi = gridWidth-1 (bỏ cột viền), gridHeight nguyên.
            width = (cfg != null ? cfg.gridWidth - 1 : StaticMapData.GRID_WIDTH),
            height = (cfg != null ? cfg.gridHeight : StaticMapData.GRID_HEIGHT),
            // Seed suy từ stageId → cùng màn luôn ra cùng layout.
            seed = SeedFromStage(stageId),
        };

        // Map không sinh box (Boss/PvP) → ép 0 obstacle.
        if (cfg != null && !cfg.spawnObstacles)
        {
            genParams.minObstacleCount = 0;
            genParams.maxObstacleCount = 0;
        }

        var map = MapGenerator.Generate(genParams);
        var enemies = EnemySpawnGenerator.Generate(stageId, map);

        return new CampaignLevel
        {
            stageId = stageId,
            environment = environment,
            map = map,
            enemies = enemies,
        };
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
