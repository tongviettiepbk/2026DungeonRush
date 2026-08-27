using System.Collections.Generic;
using UnityEngine;

// Bản rút gọn so với StickIdle (GamePlay/GameController.cs). Chỉ giữ registry unit + tham chiếu
// "mode/map/team" mà BaseUnit cần để chọn mục tiêu và giới hạn vị trí.
// TODO(follow-stick): nối vòng lặp game / wave / spawn thật của DungeonRush vào đây.
public class GameController : Singleton<GameController>
{
    public float gameSpeed = 1f;
    public UIMainLobby uiLobby;

    public ModeType modeType;
    // Mode đang chơi (BaseMode giữ luôn state trận: isPause + teamA/teamB). Do chính mode
    // tự đăng ký khi Awake. Map/wall/di chuyển nằm ở MapController.Instance.
    public BaseMode mode;

    // battleId -> unit đang hoạt động trong trận.
    public Dictionary<int, BaseUnit> activeUnits { get; private set; } = new Dictionary<int, BaseUnit>();



    // Cấp battleId duy nhất cho từng unit khi vào trận.
    private int battleIdCounter;

    // Buffer để tick không bị lỗi "sửa dictionary khi đang duyệt" (unit chết/tự deactive giữa vòng).
    private readonly List<BaseUnit> tickBuffer = new List<BaseUnit>();

    private void Awake()
    {
        if (mode == null)
        {
            Debug.LogError("GameController: mode chưa được gán trong inspector!");
        }
        else
        {
            InitGame();
        }

    }
    public void InitGame()
    {
        mode.Init(this, modeType);
        uiLobby.Refresh();
    }

    public int NextBattleId()
    {
        return ++battleIdCounter;
    }



    // Vòng lặp AI: mỗi frame gọi UpdateBehavior của mọi unit đang sống.
    // Đây là DRIVER còn thiếu trước đây — state machine trong BaseUnit giờ mới thực sự chạy.
    private void Update()
    {
        if (mode != null && mode.isPause)
        {
            return;
        }

        tickBuffer.Clear();
        var e = activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current.Value != null)
            {
                tickBuffer.Add(e.Current.Value);
            }
        }

        for (int i = 0; i < tickBuffer.Count; i++)
        {
            if (tickBuffer[i] != null)
            {
                tickBuffer[i].UpdateBehavior();
            }
        }
    }

    public void AddUnit(BaseUnit unit)
    {
        if (unit == null || mode == null) return;
        activeUnits[unit.battleId] = unit;

        List<BaseUnit> team = unit.isTeamA ? mode.teamA : mode.teamB;
        if (!team.Contains(unit))
        {
            team.Add(unit);
        }
    }

    // Dọn sạch trạng thái trận trước khi dựng màn mới.
    public void ResetBattle()
    {
        activeUnits.Clear();
        if (mode != null)
        {
            mode.teamA.Clear();
            mode.teamB.Clear();
        }
        if (MapController.Exists()) MapController.Instance.Clear();
        battleIdCounter = 0;
    }

    public void RemoveUnit(GameObject unitObject)
    {
        if (unitObject == null) return;

        int keyToRemove = int.MinValue;
        var e = activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current.Value != null && e.Current.Value.gameObject == unitObject)
            {
                keyToRemove = e.Current.Key;
                break;
            }
        }

        if (keyToRemove != int.MinValue)
        {
            BaseUnit unit = activeUnits[keyToRemove];
            activeUnits.Remove(keyToRemove);
            if (mode != null)
            {
                mode.teamA.Remove(unit);
                mode.teamB.Remove(unit);
            }
        }
    }

    public BaseUnit GetUnitByBattleId(int battleId)
    {
        if (activeUnits.TryGetValue(battleId, out BaseUnit unit))
        {
            return unit;
        }
        return null;
    }

    // Buffer tái sử dụng cho GetAliveUnits, tách theo team để 1 unit có thể lấy đồng thời
    // list đồng minh (team mình) và list kẻ địch (team kia) mà không đè lên nhau.
    // Hàm này bị gọi mỗi frame cho từng unit khi tìm mục tiêu -> tránh new List() gây rác GC.
    private readonly List<BaseUnit> aliveTeamABuffer = new List<BaseUnit>();
    private readonly List<BaseUnit> aliveTeamBBuffer = new List<BaseUnit>();

    public List<BaseUnit> GetAliveUnits(bool isTeamA)
    {
        List<BaseUnit> result = isTeamA ? aliveTeamABuffer : aliveTeamBBuffer;
        result.Clear();
        var e = activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            BaseUnit unit = e.Current.Value;
            if (unit != null && unit.isTargetable && unit.isTeamA == isTeamA)
            {
                result.Add(unit);
            }
        }
        return result;
    }
}
