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
    // Kích thước lưới chơi thực: 9×12, TẤT CẢ ô đều đi được (wall nằm trong đó).
    // (Chốt gameplay 2026-07-29: vùng chơi 9×12 đầy đủ, không trừ cột viền.)
    public const int GRID_WIDTH = 9;
    public const int GRID_HEIGHT = 12;

    // Khoảng số obstacle random mỗi màn (LevelGenerator: Min=10, Max=15).
    public const int MIN_OBSTACLE_COUNT = 10;
    public const int MAX_OBSTACLE_COUNT = 15;

    // Số lần thử sinh lại để đảm bảo map còn đường đi (LevelGenerator.MaxGenerationAttempts).
    public const int MAX_GENERATION_ATTEMPTS = 10;

    // Số hàng dưới cùng dành cho spawn quân người chơi, số hàng trên cùng cho enemy + cửa.
    public const int PLAYER_SPAWN_ROWS = 2;
    public const int ENEMY_SPAWN_ROWS = 2;

    // 1 cấu hình map theo môi trường (tương ứng 1 dòng MapConfig gốc): kích thước + camera + cờ.
    // KHÁC với MapConfig (top-level) vốn chỉ là rows/cols/cellSize dùng cho sinh lưới.
    public class MapEnvironmentConfig
    {
        public ModeType environment;
        public int gridWidth;
        public int gridHeight;
        public bool hasDoor;
        public bool spawnObstacles;     // false = đấu trường trống (Boss/PvP), không sinh box
        public float cameraOrthoSizeOffset;
        public Vector2 cameraPositionOffset;
    }

    private Dictionary<ModeType, MapEnvironmentConfig> configByEnv;

    public StaticMapData()
    {
        // Giá trị lấy nguyên từ MapConfig gốc (7 map, tất cả 9×12, đều có cửa).
        var list = new List<MapEnvironmentConfig>
        {
            new MapEnvironmentConfig { environment = ModeType.DefaultLevel,       gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -0.5f) },
            new MapEnvironmentConfig { environment = ModeType.BossRush,           gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 0f,   cameraPositionOffset = new Vector2(0f,  0f)   },
            new MapEnvironmentConfig { environment = ModeType.DragonBossDungeon,  gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 2.5f, cameraPositionOffset = new Vector2(0f, -1.5f) },
            new MapEnvironmentConfig { environment = ModeType.ZombieHordeDungeon, gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 0.5f, cameraPositionOffset = new Vector2(0f,  0f)   },
            new MapEnvironmentConfig { environment = ModeType.PvP,                gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -1f)   },
            new MapEnvironmentConfig { environment = ModeType.ChatBattle,         gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = false, cameraOrthoSizeOffset = 1f,   cameraPositionOffset = new Vector2(0f, -1f)   },
            new MapEnvironmentConfig { environment = ModeType.CultistDungeon,     gridWidth = 9, gridHeight = 12, hasDoor = true, spawnObstacles = true,  cameraOrthoSizeOffset = 0.5f, cameraPositionOffset = new Vector2(0f,  0f)   },
        };

        configByEnv = new Dictionary<ModeType, MapEnvironmentConfig>();
        for (int i = 0; i < list.Count; i++)
        {
            configByEnv[list[i].environment] = list[i];
        }
    }

    public MapEnvironmentConfig GetConfig(ModeType environment)
    {
        return configByEnv.TryGetValue(environment, out MapEnvironmentConfig cfg) ? cfg : null;
    }

    // ===== Layout spawn/cửa (suy từ kích thước lưới — KHÔNG nằm trong ma trận wall) =====

    // Cửa (cũng là cổng enemy): giữa hàng trên cùng.
    public static Vector2Int GetDoorCell(MapConfig cfg)
    {
        return new Vector2Int(cfg.cols / 2, cfg.rows - 1);
    }

    // Ô spawn quân người chơi: PLAYER_SPAWN_ROWS hàng dưới cùng.
    public static List<Vector2Int> GetPlayerSpawnCells(MapConfig cfg)
    {
        var cells = new List<Vector2Int>();
        for (int y = 0; y < PLAYER_SPAWN_ROWS && y < cfg.rows; y++)
        {
            for (int x = 0; x < cfg.cols; x++) cells.Add(new Vector2Int(x, y));
        }
        return cells;
    }

    // Ô spawn enemy khả dụng: ENEMY_SPAWN_ROWS hàng trên cùng, trừ ô wall (grid==1).
    public static List<Vector2Int> GetEnemySpawnCells(MapConfig cfg, int[,] grid)
    {
        var cells = new List<Vector2Int>();
        for (int y = Mathf.Max(0, cfg.rows - ENEMY_SPAWN_ROWS); y < cfg.rows; y++)
        {
            for (int x = 0; x < cfg.cols; x++)
            {
                if (grid == null || grid[x, y] == 0) cells.Add(new Vector2Int(x, y));
            }
        }
        return cells;
    }

    // Ô cấm đặt wall = hàng spawn quân + hàng spawn enemy + cửa (để map luôn có lối vào/ra).
    public static HashSet<Vector2Int> GetKeepClearCells(MapConfig cfg)
    {
        var set = new HashSet<Vector2Int>();
        for (int y = 0; y < PLAYER_SPAWN_ROWS && y < cfg.rows; y++)
        {
            for (int x = 0; x < cfg.cols; x++) set.Add(new Vector2Int(x, y));
        }
        for (int y = Mathf.Max(0, cfg.rows - ENEMY_SPAWN_ROWS); y < cfg.rows; y++)
        {
            for (int x = 0; x < cfg.cols; x++) set.Add(new Vector2Int(x, y));
        }
        set.Add(GetDoorCell(cfg));
        return set;
    }
}
