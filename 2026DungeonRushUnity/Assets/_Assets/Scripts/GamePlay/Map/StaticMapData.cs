using System.Collections.Generic;
using UnityEngine;

// Config tĩnh của map, dựng lại từ MapConfig gốc của DungeonRush (DecodedData/tables/MapConfig).
// Map trong DungeonRush KHÔNG lưu toạ độ box/enemy cố định — chỉ lưu kích thước lưới + cờ
// SpawnObstacles; layout thật sinh procedural lúc runtime (xem MapGenerator = rw.itr gốc).
//
// Luật đặt ô/quân gốc (LevelLoader.itq, GridManager.ijp/ijx) — chi tiết ở DecodedData/MAP_AND_SPAWN_MODEL.md.
public class StaticMapData
{
    // Kích thước lưới chơi thực: 9×12 (MapConfig.GridWidth/GridHeight → GridManager.iiy).
    public const int GRID_WIDTH = 9;
    public const int GRID_HEIGHT = 12;

    // Vùng cấm đặt chướng ngại (LevelLoader.itq): "Player Spawn" = 3 hàng dưới, "Enemy Spawn" = 3 hàng trên.
    public const int KEEP_CLEAR_ROWS = 3;

    // Ô gốc sinh hero (GridManager.ijp: ô trống gần (x 0, y 2) nhất) — hàng 2, cột giữa.
    public const int HERO_SPAWN_ROW = 2;

    // Quái chỉ đứng từ hàng 5 trở lên (GridManager.ijx).
    public const int ENEMY_MIN_ROW = 5;

    // (Tự đặt, CHƯA đối chiếu gốc) số hàng dưới Boss Rush dùng xếp ghost đồng đội.
    public const int PLAYER_SPAWN_ROWS = 2;

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
}
