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
// Lưu ý naming: GameController.mode ở DungeonRush là CombatMode (state trận: teamA/teamB/map),
// KHÁC StickIdle (ở Stick mode == BaseMode). Base này ĐỌC state qua GameController.Instance.mode,
// không chiếm chỗ đó — tránh ripple vào BaseUnit.
public class BaseMode : MonoBehaviour
{
    [Header("Định danh mode")]
    public ModeType type;
    public AudioClip bgm;

    [Header("Thời gian trận (0 = không giới hạn)")]
    public int defaultBattleTime;
    public float delayEndGame = 1.5f;

    // ----- Trạng thái trận -----
    public bool isEndMode { get; protected set; }
    public bool isPause { get; set; }
    public bool isWin { get; protected set; }
    public bool flagBattleTimer { get; protected set; }
    public float battleTimer { get; protected set; }

    // Có bỏ qua tick AI/hành vi không (đang pause hoặc trận đã kết thúc).
    public bool isSkipUpdateBehaviour => isPause || isEndMode;

    // Truy cập nhanh 2 team qua state trận (CombatMode). KHÔNG tự giữ list để tránh lệch nguồn.
    public List<BaseUnit> teamA => GameController.Instance.mode.teamA;
    public List<BaseUnit> teamB => GameController.Instance.mode.teamB;

    protected int remainingEnemies;

    protected virtual void Awake() { }

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

    // Dựng màn: build level + grid + nạp wall vào CombatMap + environment/obstacle.
    // TODO(follow-stick): mode con (CampaignMode) HẤP THỤ logic Build của CombatDirector vào đây.
    protected virtual void CreateMap() { }

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
        GameController.Instance.mode.isPause = false;
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
        GameController.Instance.mode.isPause = true;
        ActiveBattleTimer(false);

        DebugCustom.LogFormat("[EndGame] Win={0}", isWin);
        EventDispatcher.Instance.PostEvent(EventID.EndGame, this, isWin);

        SaveData();
        CalculateResult(isWin);
    }

    protected virtual void SaveData() { }

    protected virtual void CalculateResult(bool isWin) { }

    // Dọn trạng thái mode để dựng lại. Mode con hủy object đã spawn của mình.
    public virtual void Reset()
    {
        StopAllCoroutines();
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
        GameController.Instance.mode.isPause = true;
        ActiveBattleTimer(false);
        PauseUnits();
    }

    public virtual void Resume()
    {
        isPause = false;
        GameController.Instance.mode.isPause = false;
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

    // ===== ÂM THANH =====

    // TODO(follow-stick): AudioManager DungeonRush hiện chỉ có PlaySfx (bản slim). Khi port hệ audio
    // đầy đủ (nhạc nền/mixer) thì phát bgm ở đây.
    public virtual void PlayMusic() { }
}
