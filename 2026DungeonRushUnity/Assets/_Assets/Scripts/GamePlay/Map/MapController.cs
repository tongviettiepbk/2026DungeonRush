using System.Collections.Generic;
using UnityEngine;

// Quản tất cả phần cơ bản của map trong 1 trận: giữ MapConfig + ma trận wall (0/1),
// chuyển đổi ô lưới ↔ world, tìm đường A*, và di chuyển liên tục né wall theo frame.
//
// Hero/enemy gọi MapController.Instance.FindPath(...) để lấy list ô ngắn nhất đi tới đích,
// hoặc ResolveMove/ClampPointInMap cho bước di chuyển trôi mượt né wall.
// (Đã hợp nhất CombatMap cũ vào đây — 1 nguồn sự thật cho map.)
public class MapController : Singleton<MapController>
{
    public Transform pointStart;
    public MapConfig Config { get; private set; }
    public int[,] Grid { get; private set; }     // grid[x, y]: 0 = trống, 1 = wall
    public bool IsReady { get; private set; }

    private GridSpace space;
    private Vector3 minWorld;                     // tâm ô (0,0) — biên dưới-trái vùng đi được
    private Vector3 maxWorld;                     // tâm ô (cols-1, rows-1) — biên trên-phải

    // Các hướng thử "lách" khi hướng thẳng bị wall chặn (độ, ± quanh hướng gốc).
    private static readonly float[] SteerAngles = { 25f, -25f, 50f, -50f, 75f, -75f, 90f, -90f };

    // Nạp map đã sinh sẵn (VD từ CampaignLevelBuilder).
    public void SetMap(MapConfig config, int[,] grid, Vector3 origin, bool centerHorizontally = true)
    {
        Config = config;
        Grid = grid;
        space = new GridSpace(origin, config.cellSize, config.cols, config.rows, centerHorizontally);
        minWorld = space.CellToWorld(0, 0);
        maxWorld = space.CellToWorld(config.cols - 1, config.rows - 1);
        IsReady = grid != null;
    }

    // Sinh map mới ngay trong controller (dùng khi không qua CampaignLevelBuilder).
    public int[,] Generate(MapConfig config, MapGenerator.Params genParams, Vector3 origin, bool centerHorizontally = true)
    {
        genParams.config = config;
        int[,] grid = MapGenerator.Generate(genParams);
        SetMap(config, grid, origin, centerHorizontally);
        return grid;
    }

    public void Clear()
    {
        IsReady = false;
        Grid = null;
        Config = null;
    }

    // ===== Truy vấn ô =====

    public bool InBounds(int x, int y) => Config != null && Config.InBounds(x, y);
    public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

    public bool IsWall(int x, int y) => IsReady && InBounds(x, y) && Grid[x, y] == 1;
    public bool IsWall(Vector2Int c) => IsWall(c.x, c.y);

    // Ô đi được: trong biên và không phải wall.
    public bool IsWalkable(int x, int y) => InBounds(x, y) && (!IsReady || Grid[x, y] == 0);
    public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);

    // ===== Chuyển đổi toạ độ =====

    public Vector3 CellToWorld(int x, int y) => space.CellToWorld(x, y);
    public Vector3 CellToWorld(Vector2Int c) => space.CellToWorld(c);
    public Vector2Int WorldToCell(Vector3 world) => space.WorldToCell(world);

    // ===== Di chuyển liên tục (né wall, trong biên) =====

    // Điểm world có đi được không: trong biên lưới và ô không phải wall.
    public bool IsWalkable(Vector3 world)
    {
        if (!IsReady) return true;
        Vector2Int cell = WorldToCell(world);
        return InBounds(cell) && Grid[cell.x, cell.y] == 0;
    }

    // Kẹp điểm vào trong biên vùng đi được.
    public Vector3 ClampPointInMap(Vector3 point)
    {
        if (!IsReady) return point;
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
        if (!IsReady) return from + dir * step;

        Vector3 straight = ClampPointInMap(from + dir * step);
        if (IsWalkable(straight)) return straight;

        for (int i = 0; i < SteerAngles.Length; i++)
        {
            Vector3 steered = Quaternion.Euler(0f, 0f, SteerAngles[i]) * dir;
            Vector3 candidate = ClampPointInMap(from + steered * step);
            if (IsWalkable(candidate)) return candidate;
        }
        return from;
    }

    // ===== Tìm đường =====

    // A* 4 hướng, heuristic Manhattan. Trả list ô từ start → goal (bao gồm cả 2 đầu).
    // Rỗng nếu không có đường (hoặc goal là wall / ngoài biên). start==goal → [start].
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (!IsReady) return path;
        if (!InBounds(start) || !InBounds(goal)) return path;
        if (IsWall(goal)) return path;
        if (start == goal) { path.Add(start); return path; }

        var open = new List<Vector2Int> { start };                 // ô cần xét (open set nhỏ, quét tuyến tính)
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };
        var closed = new HashSet<Vector2Int>();

        while (open.Count > 0)
        {
            // Lấy ô có fScore nhỏ nhất.
            int bestIdx = 0;
            int bestF = fScore[open[0]];
            for (int i = 1; i < open.Count; i++)
            {
                int f = fScore.TryGetValue(open[i], out int v) ? v : int.MaxValue;
                if (f < bestF) { bestF = f; bestIdx = i; }
            }

            Vector2Int cur = open[bestIdx];
            if (cur == goal) return Reconstruct(cameFrom, cur);

            open.RemoveAt(bestIdx);
            closed.Add(cur);

            for (int d = 0; d < Dirs.Length; d++)
            {
                Vector2Int nxt = cur + Dirs[d];
                if (!IsWalkable(nxt) || closed.Contains(nxt)) continue;

                int tentativeG = gScore[cur] + 1;
                if (gScore.TryGetValue(nxt, out int gn) && tentativeG >= gn) continue;

                cameFrom[nxt] = cur;
                gScore[nxt] = tentativeG;
                fScore[nxt] = tentativeG + Heuristic(nxt, goal);
                if (!open.Contains(nxt)) open.Add(nxt);
            }
        }
        return path; // không tới được
    }

    // Tiện dụng: tìm đường theo toạ độ world (quy về ô gần nhất).
    public List<Vector2Int> FindPath(Vector3 startWorld, Vector3 goalWorld)
    {
        return FindPath(WorldToCell(startWorld), WorldToCell(goalWorld));
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int cur)
    {
        var path = new List<Vector2Int> { cur };
        while (cameFrom.TryGetValue(cur, out Vector2Int prev))
        {
            cur = prev;
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
    };
}
