using System.Collections.Generic;
using UnityEngine;

// Runtime dựng 1 màn campaign từ stageId: gọi CampaignLevelBuilder (map procedural + enemy),
// rồi Instantiate obstacle/enemy/điểm spawn quân vào scene theo GridSpace.
//
// Chạy được ngay: kéo component vào 1 GameObject trống rồi Play — nếu chưa gán prefab thật
// thì tự tạo placeholder hình khối để nhìn thấy layout. Gán prefab ở Inspector để dùng art thật.
// (Combat thật — di chuyển/đánh nhau — làm ở bước sau; đây là khung dựng grid + spawn.)
public class LevelRuntimeBuilder : MonoBehaviour
{
    [Header("Màn")]
    [Tooltip("0 = dùng curStageId của người chơi; >0 = ép build stage này.")]
    public int overrideStageId = 0;
    public ModeType environment = ModeType.DefaultLevel;
    public bool buildOnStart = true;

    [Header("Lưới")]
    public float cellSize = 1f;
    public bool centerHorizontally = true;

    [Header("Prefab (bỏ trống = dùng placeholder)")]
    public GameObject mapPrefab;                 // nền/environment (VD MainMapPrefab)
    public GameObject obstaclePrefab;
    public GameObject enemyPrefab;
    public GameObject playerSpawnMarkerPrefab;

    private GridSpace grid;
    private Transform container;
    private CampaignLevelBuilder.CampaignLevel currentLevel;

    public CampaignLevelBuilder.CampaignLevel CurrentLevel => currentLevel;

    private void Start()
    {
        if (buildOnStart)
        {
            Build();
        }
    }

    [ContextMenu("Rebuild")]
    public void Build()
    {
        EnsureGameDataLoaded();
        Clear();

        int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.curStageId;
        currentLevel = CampaignLevelBuilder.Build(stageId, environment);

        MapConfig config = currentLevel.config;
        grid = new GridSpace(transform.position, cellSize, config.cols, config.rows, centerHorizontally);

        // Nạp map cho pathfinding A* (để test FindPath ngay trong preview).
        MapController.Instance.SetMap(config, currentLevel.grid, transform.position, centerHorizontally);

        container = new GameObject("_Level").transform;
        container.SetParent(transform, false);

        SpawnEnvironment(config);
        SpawnObstacles(currentLevel.obstacles);
        SpawnPlayerMarkers(currentLevel.playerSpawnCells);
        SpawnEnemies(currentLevel.enemies);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (container != null)
        {
            DestroyChild(container.gameObject);
            container = null;
        }
    }

    private void SpawnEnvironment(MapConfig config)
    {
        if (mapPrefab == null) return;
        // Đặt nền ở tâm lưới (đã canh giữa X quanh origin; giữa theo Y).
        Vector3 center = grid.CellToWorld((config.cols - 1) / 2, (config.rows - 1) / 2);
        GameObject env = Instantiate(mapPrefab, center, Quaternion.identity, container);
        env.name = "Environment";
    }

    private void SpawnObstacles(List<Vector2Int> obstacles)
    {
        Transform parent = NewGroup("Obstacles");
        for (int i = 0; i < obstacles.Count; i++)
        {
            Vector3 pos = grid.CellToWorld(obstacles[i]);
            GameObject go = obstaclePrefab != null
                ? Instantiate(obstaclePrefab, pos, Quaternion.identity, parent)
                : CreatePlaceholder(PrimitiveType.Cube, pos, parent, new Color(0.55f, 0.35f, 0.2f), 0.9f);
            go.name = "Obstacle_" + obstacles[i].x + "_" + obstacles[i].y;
        }
    }

    private void SpawnPlayerMarkers(List<Vector2Int> playerSpawnCells)
    {
        Transform parent = NewGroup("PlayerSpawns");
        for (int i = 0; i < playerSpawnCells.Count; i++)
        {
            Vector3 pos = grid.CellToWorld(playerSpawnCells[i]);
            GameObject go = playerSpawnMarkerPrefab != null
                ? Instantiate(playerSpawnMarkerPrefab, pos, Quaternion.identity, parent)
                : CreatePlaceholder(PrimitiveType.Quad, pos, parent, new Color(0.2f, 0.6f, 1f, 1f), 0.85f);
            go.name = "PlayerSpawn_" + i;
        }
    }

    private void SpawnEnemies(List<EnemySpawnGenerator.EnemySpawnInfo> enemies)
    {
        Transform parent = NewGroup("Enemies");
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySpawnGenerator.EnemySpawnInfo info = enemies[i];
            Vector3 pos = grid.CellToWorld(info.cell);

            GameObject go;
            if (enemyPrefab != null)
            {
                go = Instantiate(enemyPrefab, pos, Quaternion.identity, parent);
                if (info.isBoss) go.transform.localScale *= 1.6f;   // cue boss to hơn
            }
            else
            {
                float scale = info.isBoss ? 1.3f : 0.8f;
                Color color = info.isBoss ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.9f, 0.4f, 0.4f);
                go = CreatePlaceholder(PrimitiveType.Sphere, pos, parent, color, scale);
            }

            // Đây là PREVIEW map (chỉ marker vị trí). Combat thật (EnemyUnit:BaseUnit) do
            // CombatDirector spawn từ prefab có rig — không gắn ở đây để tránh vỡ placeholder.
            go.name = (info.isBoss ? "Boss_" : "Enemy_") + i + "_lv" + info.level;
        }
    }

    // ---- helpers ----

    private void EnsureGameDataLoaded()
    {
        if (GameData.staticData == null || GameData.staticData.map == null)
        {
            GameData.Init();
        }
    }

    private Transform NewGroup(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(container, false);
        return go.transform;
    }

    private GameObject CreatePlaceholder(PrimitiveType type, Vector3 pos, Transform parent, Color color, float scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;

        Collider col = go.GetComponent<Collider>();
        if (col != null) DestroyChild(col);

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            // MaterialPropertyBlock: đổi màu không tạo material instance (tránh leak ở edit mode).
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_Color", color);       // built-in/Standard
            mpb.SetColor("_BaseColor", color);   // URP Lit/Unlit
            rend.SetPropertyBlock(mpb);
        }
        return go;
    }

    private void DestroyChild(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // Vẽ lưới + cửa trong Scene view để kiểm tra layout.
    private void OnDrawGizmosSelected()
    {
        if (currentLevel == null || currentLevel.config == null) return;
        MapConfig config = currentLevel.config;
        var g = new GridSpace(transform.position, cellSize, config.cols, config.rows, centerHorizontally);

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        for (int x = 0; x < config.cols; x++)
        {
            for (int y = 0; y < config.rows; y++)
            {
                Gizmos.DrawWireCube(g.CellToWorld(x, y), Vector3.one * cellSize);
            }
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(g.CellToWorld(currentLevel.doorCell), cellSize * 0.4f);
    }
}
