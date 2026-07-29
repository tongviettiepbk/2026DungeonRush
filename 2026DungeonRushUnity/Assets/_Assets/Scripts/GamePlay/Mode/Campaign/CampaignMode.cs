using System.Collections.Generic;
using UnityEngine;

// Mode CAMPAIGN (MainMap — màn thường). HẤP THỤ toàn bộ logic dựng màn của CombatDirector cũ
// vào khung vòng đời BaseMode: CreateMap dựng level+lưới+wall, CreateTeamA spawn Hero+Pet,
// CreateTeamB spawn Enemy. CombatDirector (thừa) đã bị bỏ.
//
// Kế thừa BaseMode nên tự có: RoutineTimer, EndGame, Pause/Resume, OnUnitDie→thắng/thua,
// cheat Editor W/L. Phần riêng của campaign nằm ở đây (prefab, chỉ số placeholder, curStageId).
//
// YÊU CẦU PREFAB: heroPrefab/petPrefab/enemyPrefab phải có rig BaseUnit (con "Body" Collider2D,
// "FlipPoints"/"CenterBody"/"FirePoint"/"health-bar", Rigidbody2D+AudioSource+AnimationController
// ở root) — PreviewCharacter là mẫu. YÊU CẦU TAG: khai báo "TeamA"/"TeamB" trong Unity.
public class CampaignMode : BaseMode
{
    [Header("Màn")]
    [Tooltip("0 = dùng curStageId của người chơi; >0 = ép build stage này.")]
    public int overrideStageId = 0;
    public bool buildOnStart = true;

    [Header("Lưới")]
    public float cellSize = 1f;
    public bool centerHorizontally = true;

    [Header("Prefab môi trường (optional)")]
    public GameObject mapPrefab;
    public GameObject obstaclePrefab;

    [Header("Prefab unit (BẮT BUỘC có rig BaseUnit)")]
    public GameObject heroPrefab;
    public GameObject petPrefab;
    public GameObject enemyPrefab;

    [Header("Chỉ số Hero (placeholder — sau lấy từ gear/companion)")]
    public float heroMaxHp = 1000f;
    public float heroAttack = 50f;
    public float heroAttackSpeed = 1.2f;
    public float heroAttackRange = 1.4f;
    public float heroMoveSpeed = 3f;

    [Header("Pet")]
    public int petCount = 1;
    public float petMaxHp = 400f;
    public float petAttack = 25f;
    public float petAttackSpeed = 1f;
    public float petAttackRange = 1.3f;
    public float petMoveSpeed = 3.2f;

    private GridSpace grid;
    private Transform container;
    private CampaignLevelBuilder.CampaignLevel currentLevel;
    private HeroUnit hero;

    public CampaignLevelBuilder.CampaignLevel CurrentLevel => currentLevel;
    public HeroUnit Hero => hero;

    protected override void Awake()
    {
        base.Awake();
        type = ModeType.DefaultLevel;
    }

    private void Start()
    {
        if (buildOnStart)
        {
            Build();
        }
    }

    // Dựng lại màn từ đầu (Reset dọn màn cũ → Initialize chạy vòng đời BaseMode).
    [ContextMenu("Rebuild")]
    public void Build()
    {
        Reset();
        Initialize();
    }

    // ===== VÒNG ĐỜI (override hook BaseMode) =====

    // Dựng level + lưới + nạp wall vào CombatMap + environment/obstacle.
    protected override void CreateMap()
    {
        EnsureGameDataLoaded();

        int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.curStageId;
        currentLevel = CampaignLevelBuilder.Build(stageId, type);
        MapGenerator.GeneratedMap map = currentLevel.map;

        grid = new GridSpace(transform.position, cellSize, map.width, map.height, centerHorizontally);

        // Nạp biên + wall để unit di chuyển né wall.
        GameController.Instance.mode.map.Setup(grid, map);
        GameController.Instance.mode.isPause = false;

        container = new GameObject("_Combat").transform;
        container.SetParent(transform, false);

        SpawnEnvironment(map);
        SpawnObstacles(map);
    }

    protected override void CreateTeamA()
    {
        SpawnHeroAndPets(currentLevel.map);
    }

    protected override void CreateTeamB()
    {
        SpawnEnemies(currentLevel.enemies);
    }

    // Dọn màn cũ trước khi dựng lại.
    [ContextMenu("Clear")]
    public override void Reset()
    {
        base.Reset();

        GameController.Instance.ResetBattle();
        hero = null;
        isEndMode = false;

        if (container != null)
        {
            DestroyChild(container.gameObject);
            container = null;
        }
    }

    // ===== SPAWN (port từ CombatDirector) =====

    private void SpawnEnvironment(MapGenerator.GeneratedMap map)
    {
        if (mapPrefab == null) return;
        Vector3 center = grid.CellToWorld((map.width - 1) / 2, (map.height - 1) / 2);
        GameObject env = Instantiate(mapPrefab, center, Quaternion.identity, container);
        env.name = "Environment";
    }

    // Wall chỉ cần hiển thị (chặn di chuyển là logic trong CombatMap, không cần collider).
    private void SpawnObstacles(MapGenerator.GeneratedMap map)
    {
        if (obstaclePrefab == null) return;

        Transform parent = NewGroup("Walls");
        for (int i = 0; i < map.obstacles.Count; i++)
        {
            Vector3 pos = grid.CellToWorld(map.obstacles[i]);
            GameObject go = Instantiate(obstaclePrefab, pos, Quaternion.identity, parent);
            go.name = "Wall_" + map.obstacles[i].x + "_" + map.obstacles[i].y;
        }
    }

    private void SpawnHeroAndPets(MapGenerator.GeneratedMap map)
    {
        if (heroPrefab == null)
        {
            DebugCustom.LogWarning("[CampaignMode] Chưa gán heroPrefab — bỏ qua spawn Hero/Pet.");
            return;
        }

        // Hero đứng giữa hàng spawn dưới cùng.
        Vector2Int heroCell = new Vector2Int(map.width / 2, 0);
        Vector3 heroPos = grid.CellToWorld(heroCell);

        Transform parent = NewGroup("Allies");
        hero = SpawnUnit<HeroUnit>(heroPrefab, heroPos, parent);
        hero.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, heroPos);

        if (petPrefab == null || petCount <= 0)
        {
            return;
        }

        BaseStats petStats = BuildPetStats();
        for (int i = 0; i < petCount; i++)
        {
            // Rải pet quanh hero.
            float angle = (360f / petCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.8f;
            Vector3 petPos = GameController.Instance.mode.map.ClampPointInMap(heroPos + offset);

            PetUnit pet = SpawnUnit<PetUnit>(petPrefab, petPos, parent);
            pet.owner = hero;
            pet.SpawnInBattle(petStats, StaticValue.TAG_TEAM_A, petPos);
        }
    }

    private void SpawnEnemies(List<EnemySpawnGenerator.EnemySpawnInfo> enemies)
    {
        if (enemyPrefab == null)
        {
            DebugCustom.LogWarning("[CampaignMode] Chưa gán enemyPrefab — bỏ qua spawn Enemy.");
            return;
        }

        Transform parent = NewGroup("Enemies");
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemySpawnGenerator.EnemySpawnInfo info = enemies[i];
            Vector3 pos = grid.CellToWorld(info.cell);

            EnemyUnit enemy = SpawnUnit<EnemyUnit>(enemyPrefab, pos, parent);
            if (info.isBoss)
            {
                enemy.transform.localScale *= 1.6f;
            }
            enemy.SpawnEnemy(info, pos);
        }
    }

    // ===== helpers =====

    private T SpawnUnit<T>(GameObject prefab, Vector3 pos, Transform parent) where T : BaseUnit
    {
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, parent);
        T unit = go.GetComponent<T>();
        if (unit == null)
        {
            unit = go.AddComponent<T>();
        }
        return unit;
    }

    private BaseStats BuildHeroStats()
    {
        return new BaseStats
        {
            attack = heroAttack,
            attackPerSecond = heroAttackSpeed,
            attackRange = heroAttackRange,
            maxHp = heroMaxHp,
            moveSpeed = heroMoveSpeed,
        };
    }

    private BaseStats BuildPetStats()
    {
        return new BaseStats
        {
            attack = petAttack,
            attackPerSecond = petAttackSpeed,
            attackRange = petAttackRange,
            maxHp = petMaxHp,
            moveSpeed = petMoveSpeed,
        };
    }

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

    private void DestroyChild(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
