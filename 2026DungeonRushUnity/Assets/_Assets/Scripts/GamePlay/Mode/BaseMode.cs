using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Lớp BASE quản lý một MODE CHƠI — bám khung StickIdle (GameModes/BaseMode.cs) nhưng ADAPT cho
// DungeonRush (RPG action trên lưới, KHÔNG phải side-scroller wave). Đây là "core base": chỉ khung
// vòng đời + trạng thái trận; mọi phần side-scroller của Stick (ActionNextWave/DOMoveX/
// nextWavePositionX/spawn-ahead) ĐÃ BỎ vì sai thể loại ([[dungonrush-genre-rpg-action]]).
//
// Mỗi mode cụ thể (Campaign/BossRush/Dungeon...) sẽ KẾ THỪA lớp này và HẤP THỤ phần dựng màn
// hiện nằm ở CombatDirector — override CreateMap/CreateTeamA/CreateTeamB. Bước "chỉ base" này
// CHƯA đụng CombatDirector; CreateMap/CreateTeamA/CreateTeamB để trống cho mode con điền sau.
//
// State trận (isPause + teamA/teamB) do CHÍNH BaseMode giữ — đã gộp CombatMode cũ vào đây.
// GameController.mode trỏ về mode đang chơi (mode tự đăng ký ở Awake). Map/wall/di chuyển
// tách hẳn sang MapController.Instance.
public class BaseMode : MonoBehaviour
{
    [Header("Định danh mode")]
    public ModeType type;
    public AudioClip bgm;

    [Header("Thời gian trận (0 = không giới hạn)")]
    public int defaultBattleTime;
    public float delayEndGame = 1.5f;

    [Header("Màn")]
    [Tooltip("0 = dùng curStageId của người chơi; >0 = ép build stage này.")]
    public int overrideStageId = 0;

    [Header("Lưới")]
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

    // ----- Trạng thái trận -----
    public bool isEndMode { get; protected set; }
    public bool isPause { get; set; }
    public bool isWin { get; protected set; }
    public bool flagBattleTimer { get; protected set; }
    public float battleTimer { get; protected set; }

    // Có bỏ qua tick AI/hành vi không (đang pause hoặc trận đã kết thúc).
    public bool isSkipUpdateBehaviour => isPause || isEndMode;

    // 2 team của trận — BaseMode tự giữ (GameController.mode == mode này nên GameController đọc trực tiếp).
    public List<BaseUnit> teamA { get; } = new List<BaseUnit>();
    public List<BaseUnit> teamB { get; } = new List<BaseUnit>();

    protected int remainingEnemies;

    protected GameController GameController;

    // Level đang dựng (map procedural + enemy) + gốc chứa object đã spawn — dùng chung cho spawn team.
    protected CampaignLevelBuilder.CampaignLevel currentLevel;
    protected Transform container;

    public CampaignLevelBuilder.CampaignLevel CurrentLevel => currentLevel;

    public virtual void Init(GameController controller, ModeType typeModeInput = ModeType.DefaultLevel)
    {
        this.GameController = controller;
    }

    protected virtual void OnEnable()
    {
        EventDispatcher.Instance.RegisterListener(EventID.UnitDie, OnUnitDie);
        EventDispatcher.Instance.RegisterListener(EventID.ResetMode, OnResetMode);
    }

    protected virtual void OnDisable()
    {
        EventDispatcher.Instance.RemoveListener(EventID.UnitDie, OnUnitDie);
        EventDispatcher.Instance.RemoveListener(EventID.ResetMode, OnResetMode);
    }

    protected virtual void Update()
    {
#if UNITY_EDITOR
        // Cheat phím tắt như StickIdle: W = thắng, L = thua (chỉ trong Editor).
        if (Input.GetKeyUp(KeyCode.W))
        {
            EndGame(true);
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {
            EndGame(false);
        }
#endif
    }

    // ===== VÒNG ĐỜI =====

    // Điểm vào của mode — mirror StickIdle.Initialize nhưng theo bước dựng lưới của DungeonRush.
    public virtual void Initialize()
    {
        LoadEnemyFiles();
        PlayMusic();
        CreateMap();
        CreateTeamA();
        CreateTeamB();
        InitModeDone();
        StartGame();
    }

    // Nạp file/config enemy riêng của mode (nếu có). Mode con override.
    protected virtual void LoadEnemyFiles() { }

    // Dựng màn: build level procedural từ stageId → nạp map (wall/A*/toạ độ ô↔world) vào
    // MapController → dựng environment + obstacle. Mode con thường KHÔNG cần override (chỉ khác ở team).
    protected virtual void CreateMap()
    {
        EnsureGameDataLoaded();

        int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.curStageId;
        currentLevel = CampaignLevelBuilder.Build(stageId, type);

        container = new GameObject("_Combat").transform;
        container.SetParent(transform, false);

        // MapController nằm TRÊN mapPrefab → dựng environment TRƯỚC để có MapController + PointStart
        // (PointStart định nghĩa gốc ô (0,0)), rồi mới nạp grid vào chính bản đó.
        MapController map = SpawnEnvironment();

        // 1 nguồn sự thật cho map: pathfinding A* + di chuyển né wall + chuyển đổi ô↔world (gốc = PointStart).
        map.SetMap(currentLevel.grid, transform.position, centerHorizontally);
        isPause = false;

        SpawnObstacles(map, currentLevel.obstacles);
    }

    // Dựng environment (mapPrefab mang MapController + PointStart). Đặt prefab tại transform.position;
    // gốc ô (0,0) do PointStart trong prefab quyết định. Trả về MapController của bản vừa dựng.
    // mapPrefab null → fallback MapController.Instance (không có PointStart → canh giữa như cũ).
    protected MapController SpawnEnvironment()
    {
        if (mapPrefab == null) return MapController.Instance;

        GameObject env = Instantiate(mapPrefab, transform.position, Quaternion.identity, container);
        env.name = "Environment";

        MapController map = env.GetComponentInChildren<MapController>();
        return map != null ? map : MapController.Instance;
    }

    // Wall chỉ cần hiển thị (chặn di chuyển là logic MapController, không cần collider).
    protected void SpawnObstacles(MapController map, List<Vector2Int> obstacles)
    {
        if (obstaclePrefab == null) return;

        Transform parent = NewGroup("Walls");
        for (int i = 0; i < obstacles.Count; i++)
        {
            Vector3 pos = map.CellToWorld(obstacles[i]);
            GameObject go = Instantiate(obstaclePrefab, pos, Quaternion.identity, parent);
            go.name = "Wall_" + obstacles[i].x + "_" + obstacles[i].y;
        }
    }

    // Spawn quân người chơi (Hero + Pet) — team A. Mode con điền.
    protected virtual void CreateTeamA() { }

    // Spawn enemy — team B. Mode con điền.
    protected virtual void CreateTeamB() { }

    // Chốt sau khi dựng xong: reload lại stat mọi unit rồi mở khoá pause.
    protected virtual void InitModeDone()
    {
        ReloadTeamStats(teamA);
        ReloadTeamStats(teamB);

        isPause = false;
    }

    private void ReloadTeamStats(List<BaseUnit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null)
            {
                units[i].ReloadStats();
            }
        }
    }

    public virtual void StartGame()
    {
        isEndMode = false;
        isPause = false;
        remainingEnemies = teamB.Count;

        if (defaultBattleTime > 0)
        {
            StartCoroutine(RoutineTimer());
        }
    }

    // Đồng hồ trận: đếm tới defaultBattleTime rồi xử thua (hết giờ). Không có wave/di chuyển màn.
    protected virtual IEnumerator RoutineTimer()
    {
        battleTimer = 0f;
        ActiveBattleTimer(true);

        while (isEndMode == false)
        {
            if (flagBattleTimer)
            {
                battleTimer += Time.deltaTime * GameController.Instance.gameSpeed;
                UpdateBattleTime(battleTimer, defaultBattleTime);
                if (battleTimer >= defaultBattleTime)
                {
                    EndGame(false);
                    yield break;
                }
            }

            yield return null;
        }
    }

    // Hook cho UI cập nhật đồng hồ. Mode/UI con override.
    protected virtual void UpdateBattleTime(float timer, int totalTime) { }

    public virtual void ActiveBattleTimer(bool isOn)
    {
        flagBattleTimer = isOn;
    }

    // ===== KẾT THÚC / RESET =====

    public virtual void EndGame(bool isWin)
    {
        if (isEndMode)
        {
            return;
        }

        isEndMode = true;
        this.isWin = isWin;
        isPause = true;
        ActiveBattleTimer(false);

        DebugCustom.LogFormat("[EndGame] Win={0}", isWin);
        EventDispatcher.Instance.PostEvent(EventID.EndGame, this, isWin);

        SaveData();
        CalculateResult(isWin);
    }

    protected virtual void SaveData() { }

    protected virtual void CalculateResult(bool isWin) { }

    // Dọn trạng thái mode để dựng lại: dừng coroutine + huỷ mọi object đã spawn (container).
    public virtual void Reset()
    {
        StopAllCoroutines();

        if (container != null)
        {
            DestroyChild(container.gameObject);
            container = null;
        }
    }

    protected virtual void OnResetMode(object obj)
    {
        try
        {
            StopAllCoroutines();
        }
        catch { }
    }

    // ===== PAUSE / RESUME =====

    public virtual void Pause()
    {
        isPause = true;
        ActiveBattleTimer(false);
        PauseUnits();
    }

    public virtual void Resume()
    {
        isPause = false;
        ActiveBattleTimer(true);
        ResumeUnits();
    }

    protected virtual void PauseUnits()
    {
        var e = GameController.Instance.activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current.Value != null)
            {
                e.Current.Value.Pause();
            }
        }
    }

    protected virtual void ResumeUnits()
    {
        var e = GameController.Instance.activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current.Value != null)
            {
                e.Current.Value.Resume();
            }
        }
    }

    // ===== SỰ KIỆN UNIT CHẾT =====

    protected virtual void OnUnitDie(object obj)
    {
        int battleId = (int)obj;
        BaseUnit unit = GameController.Instance.GetUnitByBattleId(battleId);
        if (unit == null)
        {
            return;
        }

        if (unit.isTeamA)
        {
            OnAllyDie(battleId);
        }
        else
        {
            OnEnemyDie(battleId);
        }
    }

    // Mặc định: hết sạch quân team A → thua. Mode con tinh chỉnh (vd chỉ tính Hero).
    protected virtual void OnAllyDie(int battleId)
    {
        if (CountAlive(teamA) == 0)
        {
            EndGame(false);
        }
    }

    protected virtual void OnEnemyDie(int battleId)
    {
        BaseUnit unit = GameController.Instance.GetUnitByBattleId(battleId);
        if (unit != null && teamB.Contains(unit))
        {
            remainingEnemies--;
            CheckRemainingEnemies();
        }
    }

    // Mặc định: hết sạch enemy → thắng. Mode con override cho luật riêng (boss/nhiều wave...).
    protected virtual void CheckRemainingEnemies()
    {
        if (CountAlive(teamB) == 0)
        {
            EndGame(true);
        }
    }

    protected int CountAlive(List<BaseUnit> units)
    {
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].isTargetable)
            {
                count++;
            }
        }
        return count;
    }

    // ===== HELPER DỰNG MÀN =====

    protected void EnsureGameDataLoaded()
    {
        if (GameData.staticData == null || GameData.staticData.map == null)
        {
            GameData.Init();
        }
    }

    protected Transform NewGroup(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(container, false);
        return go.transform;
    }

    protected void DestroyChild(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    // ===== ÂM THANH =====

    // TODO(follow-stick): AudioManager DungeonRush hiện chỉ có PlaySfx (bản slim). Khi port hệ audio
    // đầy đủ (nhạc nền/mixer) thì phát bgm ở đây.
    public virtual void PlayMusic() { }
}
