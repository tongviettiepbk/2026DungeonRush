using System.Collections.Generic;
using UnityEngine;

// Config tĩnh của map, dựng lại từ MapConfig gốc của DungeonRush (DecodedData/tables/MapConfig).
// Map trong DungeonRush KHÔNG lưu toạ độ box/enemy cố định — chỉ lưu kích thước lưới + cờ
// SpawnObstacles; layout thật sinh procedural lúc runtime (xem MapGenerator).
//
// Tham số sinh (obstacle 10-15, seed, MaxGenerationAttempts...) đọc từ instance LevelGenerator
// trong GameplayScene gốc — chi tiết ở DecodedData/MAP_AND_SPAWN_MODEL.md.
public class StaticMapData
{
    // Kích thước lưới logic (MapConfig ghi 9×12; cột thứ 9 là viền/cửa, vùng chơi thực 8×12).
    public const int GRID_WIDTH = 8;
    public const int GRID_HEIGHT = 12;

    // Khoảng số obstacle random mỗi màn (LevelGenerator: Min=10, Max=15).
    public const int MIN_OBSTACLE_COUNT = 10;
    public const int MAX_OBSTACLE_COUNT = 15;

    // Số lần thử sinh lại để đảm bảo map còn đường đi (LevelGenerator.MaxGenerationAttempts).
    public const int MAX_GENERATION_ATTEMPTS = 10;

    // Số hàng dưới cùng dành cho spawn quân người chơi, số hàng trên cùng cho enemy + cửa.
    public const int PLAYER_SPAWN_ROWS = 2;
    public const int ENEMY_SPAWN_ROWS = 2;

    // 1 cấu hình map (tương ứng 1 dòng MapConfig gốc).
    public class MapConfig
    {
        public MapEnvironmentType environment;
        public int gridWidth;
        public int gridHeight;
        public bool hasDoor;
        public bool spawnObstacles;     // false = đấu trường trống (Boss/PvP), không sinh box
        public float cameraOrthoSizeOffset;
        public Vector2 cameraPositionOffset;
    }

    private Dictionary<MapEnvironmentType, MapConfig> configByEnv;

    public StaticMapData()
    {
        // Giá trị lấy nguyên từ MapConfig gốc (7 map, tất cả 9×12, đều có cửa).
        var list = new List<MapConfig>
        {
            new MapConfig { environment = MapEnvironmentType.DefaultLevel,       gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -0.5f) },
            new MapConfig { environment = MapEnvironmentType.BossRush,           gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 0f,   cameraPositionOffset = new Vector2(0f,  0f)   },
            new MapConfig { environment = MapEnvironmentType.DragonBossDungeon,  gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 2.5f, cameraPositionOffset = new Vector2(0f, -1.5f) },
            new MapConfig { environment = MapEnvironmentType.ZombieHordeDungeon, gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 0.5f, cameraPositionOffset = new Vector2(0f,  0f)   },
            new MapConfig { environment = MapEnvironmentType.PvP,                gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -1f)   },
            new MapConfig { environment = MapEnvironmentType.ChatBattle,         gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -1f)   },
            new MapConfig { environment = MapEnvironmentType.CultistDungeon,     gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 0.5f, cameraPositionOffset = new Vector2(0f,  0f)   },
        };

        configByEnv = new Dictionary<MapEnvironmentType, MapConfig>();
        for (int i = 0; i < list.Count; i++)
        {
            configByEnv[list[i].environment] = list[i];
        }
    }

    public MapConfig GetConfig(MapEnvironmentType environment)
    {
        return configByEnv.TryGetValue(environment, out MapConfig cfg) ? cfg : null;
    }
}
