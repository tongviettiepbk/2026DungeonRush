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
    public float delayEndGame = 3f;

    [Header("Màn")]
    [Tooltip("0 = dùng stageIdCurrent của người chơi; >0 = ép build stage này.")]
    public int overrideStageId = 0;

    [Header("Lưới")]
    public bool centerHorizontally = true;

    [Header("Prefab môi trường (optional)")]
    public GameObject mapPrefab;
    public GameObject obstaclePrefab;

    [Header("Prefab unit (BẮT BUỘC có rig BaseUnit)")]
    public GameObject heroPrefab;
    public GameObject enemyPrefab;

    // Chỉ số Hero giờ lấy từ tầng nền PlayerBase* trong GearStatConfig (xem CampaignMode.BuildHeroStats),
    // không còn placeholder ở đây.

    [Header("Pet")]
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

    // Quân người chơi (team A) — dùng chung mọi mode: Hero + Pet đang equip.
    protected HeroUnit hero;
    public HeroUnit Hero => hero;
    private readonly List<PetUnit> pets = new List<PetUnit>();

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

        int stageId = overrideStageId > 0 ? overrideStageId : GameData.userData.campaign.stageIdCurrent;
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
        env.transform.position = new Vector3(0, -1.5f, 0);
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

        GameController.Instance.ResetBattle();
        hero = null;
        alliesGroup = null;
        pets.Clear();
        isEndMode = false;
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

    // Thua khi HERO chết — bản sao hero (companion Clone) cũng ở team A nhưng không giữ trận.
    protected virtual void OnAllyDie(int battleId)
    {
        if (hero == null || !hero.isTargetable)
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

    // ===== SPAWN QUÂN NGƯỜI CHƠI (Hero + Pet) =====

    protected void SpawnHeroAndPets()
    {
        if (heroPrefab == null)
        {
            DebugCustom.LogWarning("[BaseMode] Chưa gán heroPrefab — bỏ qua spawn Hero/Pet.");
            return;
        }

        // Hero đứng giữa hàng spawn DƯỚI cùng (row = 0).
        Vector2Int heroCell = new Vector2Int(0, MapController.Instance.Cols / 2);
        Vector3 heroPos = MapController.Instance.CellToWorld(heroCell);

        alliesGroup = NewGroup("Allies");
        hero = SpawnUnit<HeroUnit>(heroPrefab, heroPos, alliesGroup);
        hero.SpawnInBattle(BuildHeroStats(), StaticValue.TAG_TEAM_A, heroPos);

        SyncPets();
    }

    private Transform alliesGroup;

    // Đồng bộ pet trong trận với danh sách equip (save UserCompanionData, tối đa 3), đi theo hero.
    // Gọi lúc dựng màn VÀ ngay khi đổi equip ở CompanionUI (không phải chờ màn sau):
    //   • Pet đang trong trận mà đã bỏ equip → gỡ khỏi trận.
    //   • Pet mới equip chưa có trong trận → spawn quanh vị trí hero hiện tại.
    // Prefab riêng từng con load theo assetName (CompanionData.LoadPrefab) — SetupCompanion đổ
    // data của con đang equip vào (chỉ số + cooldown + level trong save).
    public void SyncPets()
    {
        if (hero == null || isEndMode)
        {
            return;
        }

        UserCompanionData user = GameData.userData.companions;
        List<string> equipped = user.GetEquipped();

        for (int i = pets.Count - 1; i >= 0; i--)
        {
            PetUnit pet = pets[i];
            if (pet != null && pet.Data != null && equipped.Contains(pet.Data.assetName))
            {
                continue;
            }

            if (pet != null)
            {
                pet.Deactive();
                Destroy(pet.gameObject);
            }
            pets.RemoveAt(i);
        }

        Vector3 heroPos = hero.transform.position;
        for (int i = 0; i < equipped.Count; i++)
        {
            if (GetPet(equipped[i]) != null)
            {
                continue;
            }

            CompanionData data = GameData.staticData.companions.GetData(equipped[i]);
            if (data == null)
            {
                continue;
            }

            GameObject petPrefab = data.LoadPrefab();
            if (petPrefab == null)
            {
                DebugCustom.LogWarning("[BaseMode] Không tìm thấy prefab companion: " + CompanionData.PREFAB_PATH + data.assetName);
                continue;
            }

            // Rải pet quanh hero.
            float angle = (360f / equipped.Count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.8f;
            Vector3 petPos = MapController.Instance.ClampPointInMap(heroPos + offset);

            PetUnit pet = SpawnUnit<PetUnit>(petPrefab, petPos, alliesGroup);
            pet.SetupCompanion(data, hero, user.GetLevel(data.assetName), petPos);
            pets.Add(pet);
        }
    }

    // Pet trong trận hiện tại theo assetName (UI lobby đọc cooldown / bấm kích hoạt). null nếu không có.
    public PetUnit GetPet(string assetName)
    {
        for (int i = 0; i < pets.Count; i++)
        {
            if (pets[i] != null && pets[i].Data != null && pets[i].Data.assetName == assetName)
            {
                return pets[i];
            }
        }
        return null;
    }

    protected T SpawnUnit<T>(GameObject prefab, Vector3 pos, Transform parent) where T : BaseUnit
    {
        GameObject go = Instantiate(prefab, pos, Quaternion.identity, parent);
        T unit = go.GetComponent<T>();
        if (unit == null)
        {
            unit = go.AddComponent<T>();
        }
        return unit;
    }

    // Chỉ số hero = tầng NỀN gốc (PlayerBase*) từ GearStatConfig. Hero trần (chưa đồ) = Damage 10/HP 50.
    // (Cộng dồn main stat đồ đang mặc để ở bước hệ trang bị đầy đủ.)
    private static GearStatConfigData gearStatConfig;

    protected BaseStats BuildHeroStats()
    {
        if (gearStatConfig == null)
        {
            gearStatConfig = Resources.Load<GearStatConfigData>("Scriptable Objects/Gears/GearStatConfig");
        }

        if (gearStatConfig == null)
        {
            DebugCustom.LogError("[BaseMode] Thiếu GearStatConfig — dùng BaseStats rỗng cho Hero.");
            return new BaseStats();
        }

        return gearStatConfig.GetPlayerBaseStats();
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
