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
    public static Vector2Int GetDoorCell(int cols, int rows)
    {
        return new Vector2Int(cols / 2, rows - 1);
    }

    // Ô spawn quân người chơi: PLAYER_SPAWN_ROWS hàng dưới cùng.
    public static List<Vector2Int> GetPlayerSpawnCells(int cols, int rows)
    {
        return BottomRows(cols, rows, PLAYER_SPAWN_ROWS);
    }

    // Ô spawn enemy khả dụng: ENEMY_SPAWN_ROWS hàng trên cùng, trừ ô wall (grid==1).
    public static List<Vector2Int> GetEnemySpawnCells(int cols, int rows, int[,] grid)
    {
        var cells = new List<Vector2Int>();
        foreach (var cell in TopRows(cols, rows, ENEMY_SPAWN_ROWS))
        {
            if (grid == null || grid[cell.x, cell.y] == 0) cells.Add(cell);
        }
        return cells;
    }

    // Ô cấm đặt wall = hàng spawn quân + hàng spawn enemy + cửa (để map luôn có lối vào/ra).
    public static HashSet<Vector2Int> GetKeepClearCells(int cols, int rows)
    {
        var set = new HashSet<Vector2Int>();
        foreach (var cell in BottomRows(cols, rows, PLAYER_SPAWN_ROWS)) set.Add(cell);
        foreach (var cell in TopRows(cols, rows, ENEMY_SPAWN_ROWS)) set.Add(cell);
        set.Add(GetDoorCell(cols, rows));
        return set;
    }

    // Ô của rowCount hàng dưới cùng (y = 0 .. rowCount-1).
    private static List<Vector2Int> BottomRows(int cols, int rows, int rowCount)
    {
        var cells = new List<Vector2Int>();
        for (int y = 0; y < rowCount && y < rows; y++)
        {
            for (int x = 0; x < cols; x++) cells.Add(new Vector2Int(x, y));
        }
        return cells;
    }

    // Ô của rowCount hàng trên cùng (y = rows-rowCount .. rows-1).
    private static List<Vector2Int> TopRows(int cols, int rows, int rowCount)
    {
        var cells = new List<Vector2Int>();
        for (int y = Mathf.Max(0, rows - rowCount); y < rows; y++)
        {
            for (int x = 0; x < cols; x++) cells.Add(new Vector2Int(x, y));
        }
        return cells;
    }
}
