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

    public void AddUnit(BaseUnit unit)
    {
        if (unit == null) return;
        activeUnits[unit.battleId] = unit;
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
            activeUnits.Remove(keyToRemove);
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

// Ranh giới map để clamp vị trí (knockback/fear). Hiện là identity.
// TODO(follow-stick): nối với map procedural của DungeonRush (StaticMapData/MapGenerator).
public class CombatMap
{
    public Vector3 ClampPointInMap(Vector3 point)
    {
        return point;
    }
}
