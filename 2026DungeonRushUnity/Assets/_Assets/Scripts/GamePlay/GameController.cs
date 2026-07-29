using System.Collections.Generic;
using UnityEngine;

// Bản rút gọn so với StickIdle (GamePlay/GameController.cs). Chỉ giữ registry unit + tham chiếu
// "mode/map/team" mà BaseUnit cần để chọn mục tiêu và giới hạn vị trí.
// TODO(follow-stick): nối vòng lặp game / wave / spawn thật của DungeonRush vào đây.
public class GameController : Singleton<GameController>
{
    public float gameSpeed = 1f;
    public CombatMode mode = new CombatMode();

    // battleId -> unit đang hoạt động trong trận.
    public Dictionary<int, BaseUnit> activeUnits { get; private set; } = new Dictionary<int, BaseUnit>();

    // Cấp battleId duy nhất cho từng unit khi vào trận.
    private int battleIdCounter;

    // Buffer để tick không bị lỗi "sửa dictionary khi đang duyệt" (unit chết/tự deactive giữa vòng).
    private readonly List<BaseUnit> tickBuffer = new List<BaseUnit>();

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
        if (unit == null) return;
        activeUnits[unit.battleId] = unit;

        List<BaseUnit> team = unit.isTeamA ? mode.teamA : mode.teamB;
        if (!team.Contains(unit))
        {
            team.Add(unit);
        }
    }

    // Dọn sạch trạng thái trận trước khi dựng màn mới (CombatDirector gọi).
    public void ResetBattle()
    {
        activeUnits.Clear();
        mode.teamA.Clear();
        mode.teamB.Clear();
        mode.map.Clear();
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
            mode.teamA.Remove(unit);
            mode.teamB.Remove(unit);
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

    public List<BaseUnit> GetAliveUnits(bool isTeamA)
    {
        List<BaseUnit> result = new List<BaseUnit>();
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

// "mode" trận đấu — rút gọn từ hệ mode/wave của StickIdle.
public class CombatMode
{
    public bool isPause;
    public CombatMap map = new CombatMap();
    public List<BaseUnit> teamA = new List<BaseUnit>();
    public List<BaseUnit> teamB = new List<BaseUnit>();
}

// Ranh giới + wall của map để giới hạn di chuyển. Được CombatDirector nạp sau khi dựng màn.
// Wall = ô Obstacle (mỗi wall 1 ô); unit di chuyển liên tục nhưng không được vào ô wall
// và không ra ngoài biên lưới (chốt gameplay 2026-07-29).
public class CombatMap
{
    private bool hasBounds;
    private GridSpace grid;
    private readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
    private int width;
    private int height;
    private Vector3 minWorld;
    private Vector3 maxWorld;

    // Các hướng thử "lách" khi hướng thẳng bị wall chặn (độ, ± quanh hướng gốc).
    private static readonly float[] SteerAngles = { 25f, -25f, 50f, -50f, 75f, -75f, 90f, -90f };

    public void Setup(GridSpace grid, MapGenerator.GeneratedMap map)
    {
        this.grid = grid;
        width = map.width;
        height = map.height;

        walls.Clear();
        for (int i = 0; i < map.obstacles.Count; i++)
        {
            walls.Add(map.obstacles[i]);
        }

        minWorld = grid.CellToWorld(0, 0);
        maxWorld = grid.CellToWorld(width - 1, height - 1);
        hasBounds = true;
    }

    public void Clear()
    {
        hasBounds = false;
        walls.Clear();
    }

    public bool IsWall(Vector2Int cell)
    {
        return walls.Contains(cell);
    }

    // Ô đi được: trong biên lưới và không phải wall.
    public bool IsWalkable(Vector3 world)
    {
        if (!hasBounds)
        {
            return true;
        }

        Vector2Int cell = grid.WorldToCell(world);
        if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
        {
            return false;
        }

        return !walls.Contains(cell);
    }

    public Vector3 ClampPointInMap(Vector3 point)
    {
        if (!hasBounds)
        {
            return point;
        }

        point.x = Mathf.Clamp(point.x, minWorld.x, maxWorld.x);
        point.y = Mathf.Clamp(point.y, minWorld.y, maxWorld.y);
        return point;
    }

    // Giải quyết 1 bước di chuyển liên tục có né wall:
    //   - đi thẳng theo dir nếu ô đích đi được;
    //   - nếu bị chặn, thử lách sang các hướng lệch dần (SteerAngles);
    //   - không hướng nào đi được → đứng yên.
    public Vector3 ResolveMove(Vector3 from, Vector3 dir, float step)
    {
        if (!hasBounds)
        {
            return from + dir * step;
        }

        Vector3 straight = ClampPointInMap(from + dir * step);
        if (IsWalkable(straight))
        {
            return straight;
        }

        for (int i = 0; i < SteerAngles.Length; i++)
        {
            Vector3 steered = Quaternion.Euler(0f, 0f, SteerAngles[i]) * dir;
            Vector3 candidate = ClampPointInMap(from + steered * step);
            if (IsWalkable(candidate))
            {
                return candidate;
            }
        }

        return from;
    }
}
