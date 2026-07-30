using System.Collections.Generic;
using UnityEngine;

// Quản tất cả phần cơ bản của map trong 1 trận: giữ ma trận wall (0/1),
// chuyển đổi ô lưới ↔ world, tìm đường A*, và di chuyển liên tục né wall theo frame.
//
// Hero/enemy gọi MapController.Instance.FindPath(...) để lấy list ô ngắn nhất đi tới đích,
// hoặc ResolveMove/ClampPointInMap cho bước di chuyển trôi mượt né wall.
// (Đã hợp nhất CombatMap cũ vào đây — 1 nguồn sự thật cho map.)
public class MapController : Singleton<MapController>
{
    public Transform pointStart;                  // CHƯA DÙNG — chừa để wire mốc gốc lưới từ Inspector sau
    public int[,] Grid { get; private set; }      // grid[x, y]: 0 = trống, 1 = wall
    public int Cols => Grid != null ? Grid.GetLength(0) : 0;
    public int Rows => Grid != null ? Grid.GetLength(1) : 0;
    public bool IsReady { get; private set; }

    // Neo lưới trong world (mirror CellSize/GridOrigin của LevelGenerator gốc: cellSize=1,
    // gốc ở góc dưới, ô (x,y) x sang phải / y đi lên tới cửa).
    private const float CELL_SIZE = 1f;           // cạnh 1 ô (world unit)
    private Vector3 gridOrigin;
    private bool centerHorizontally;              // canh giữa map theo trục X quanh gridOrigin
    private Vector3 minWorld;                     // tâm ô (0,0) — biên dưới-trái vùng đi được
    private Vector3 maxWorld;                     // tâm ô (cols-1, rows-1) — biên trên-phải

    // Các hướng thử "lách" khi hướng thẳng bị wall chặn (độ, ± quanh hướng gốc).
    private static readonly float[] SteerAngles = { 25f, -25f, 50f, -50f, 75f, -75f, 90f, -90f };

    // Nạp map đã sinh sẵn (VD từ CampaignLevelBuilder).
    public void SetMap(int[,] grid, Vector3 origin, bool centerHorizontally = true)
    {
        Grid = grid;
        gridOrigin = origin;
        this.centerHorizontally = centerHorizontally;
        IsReady = grid != null;
        if (IsReady)
        {
            minWorld = CellToWorld(0, 0);
            maxWorld = CellToWorld(Cols - 1, Rows - 1);
        }
    }

    // Sinh map mới ngay trong controller (dùng khi không qua CampaignLevelBuilder).
    public int[,] Generate(MapGenerator.Params genParams, Vector3 origin, bool centerHorizontally = true)
    {
        int[,] grid = MapGenerator.Generate(genParams);
        SetMap(grid, origin, centerHorizontally);
        return grid;
    }

    public void Clear()
    {
        IsReady = false;
        Grid = null;
    }

    // ===== Truy vấn ô =====

    public bool InBounds(int x, int y) => x >= 0 && x < Cols && y >= 0 && y < Rows;
    public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

    public bool IsWall(int x, int y) => IsReady && InBounds(x, y) && Grid[x, y] == 1;
    public bool IsWall(Vector2Int c) => IsWall(c.x, c.y);

    // Ô đi được: trong biên và không phải wall.
    public bool IsWalkable(int x, int y) => InBounds(x, y) && (!IsReady || Grid[x, y] == 0);
    public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);

    // ===== Chuyển đổi toạ độ ô lưới ↔ world =====

    public Vector3 CellToWorld(int x, int y)
    {
        float offsetX = centerHorizontally ? -(Cols - 1) * 0.5f * CELL_SIZE : 0f;
        return gridOrigin + new Vector3(offsetX + x * CELL_SIZE, y * CELL_SIZE, 0f);
    }

    public Vector3 CellToWorld(Vector2Int c) => CellToWorld(c.x, c.y);

    public Vector2Int WorldToCell(Vector3 world)
    {
        float offsetX = centerHorizontally ? -(Cols - 1) * 0.5f * CELL_SIZE : 0f;
        Vector3 local = world - gridOrigin;
        int x = Mathf.RoundToInt((local.x - offsetX) / CELL_SIZE);
        int y = Mathf.RoundToInt(local.y / CELL_SIZE);
        return new Vector2Int(x, y);
    }

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
            int bestIndex = 0;
            int bestFScore = fScore[open[0]];
            for (int i = 1; i < open.Count; i++)
            {
                int candidateF = fScore.TryGetValue(open[i], out int value) ? value : int.MaxValue;
                if (candidateF < bestFScore) { bestFScore = candidateF; bestIndex = i; }
            }

            Vector2Int current = open[bestIndex];
            if (current == goal) return Reconstruct(cameFrom, current);

            open.RemoveAt(bestIndex);
            closed.Add(current);

            for (int dirIndex = 0; dirIndex < Dirs.Length; dirIndex++)
            {
                Vector2Int next = current + Dirs[dirIndex];
                if (!IsWalkable(next) || closed.Contains(next)) continue;

                int tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(next, out int neighborG) && tentativeG >= neighborG) continue;

                cameFrom[next] = current;
                gScore[next] = tentativeG;
                fScore[next] = tentativeG + Heuristic(next, goal);
                if (!open.Contains(next)) open.Add(next);
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

    private static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out Vector2Int previous))
        {
            current = previous;
            path.Add(current);
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
