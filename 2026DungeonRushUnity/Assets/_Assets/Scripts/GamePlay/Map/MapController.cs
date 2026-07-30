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
    public Transform pointStart;                  // Mốc gốc lưới: ô (0,0) neo ĐÚNG tại đây (góc DƯỚI-TRÁI). Null → dùng fallbackOrigin.
    public int[,] Grid { get; private set; }      // Grid[row, col]: 0 = trống, 1 = wall
    public int Rows => Grid != null ? Grid.GetLength(0) : 0;
    public int Cols => Grid != null ? Grid.GetLength(1) : 0;
    public bool IsReady { get; private set; }

    // Quy ước ô lưới (theo thói quen mảng 2 chiều a[row][col]):
    //   - cell = Vector2Int(x = ROW, y = COL); truy cập Grid[cell.x, cell.y] = Grid[row, col].
    //   - (0,0) = góc DƯỚI-TRÁI (tại PointStart); row tăng → đi LÊN, col tăng → sang PHẢI.
    //   - world: col → +X (phải), row → +Y (lên); cạnh 1 ô = 1 world unit.
    private const float CELL_SIZE = 1f;           // cạnh 1 ô (world unit)
    private Vector3 gridOrigin;                   // world của ô (0,0) — góc dưới-trái
    private bool centerHorizontally;              // canh giữa map theo trục X quanh gridOrigin (tắt khi dùng PointStart)
    private Vector3 minWorld;                     // biên dưới-trái vùng đi được (componentwise min 2 góc)
    private Vector3 maxWorld;                     // biên trên-phải vùng đi được (componentwise max 2 góc)

    // Các hướng thử "lách" khi hướng thẳng bị wall chặn (độ, ± quanh hướng gốc).
    private static readonly float[] SteerAngles = { 25f, -25f, 50f, -50f, 75f, -75f, 90f, -90f };

    // MapController nằm TRÊN mapPrefab → khi prefab được Instantiate, tự nhận mình là Singleton Instance
    // (bảo đảm Instance là bản CÓ PointStart, không phải bản rỗng do Singleton auto-tạo).
    protected virtual void Awake()
    {
        instance = this;
    }

    // Nạp map đã sinh sẵn (VD từ CampaignLevelBuilder).
    // pointStart gán (Inspector) → dùng nó làm gốc (0,0) góc dưới-trái, KHÔNG canh giữa.
    // Ngược lại → dùng fallbackOrigin + cờ centerHorizontally (hành vi cũ).
    public void SetMap(int[,] grid, Vector3 fallbackOrigin, bool centerHorizontally = true)
    {
        Grid = grid;
        if (pointStart != null)
        {
            gridOrigin = pointStart.position;
            this.centerHorizontally = false;
        }
        else
        {
            gridOrigin = fallbackOrigin;
            this.centerHorizontally = centerHorizontally;
        }
        IsReady = grid != null;
        if (IsReady)
        {
            Vector3 topLeft = CellToWorld(0, 0);
            Vector3 bottomRight = CellToWorld(Rows - 1, Cols - 1);
            minWorld = Vector3.Min(topLeft, bottomRight);
            maxWorld = Vector3.Max(topLeft, bottomRight);
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

    // ===== Truy vấn ô (row, col) =====

    public bool InBounds(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Cols;
    public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

    public bool IsWall(int row, int col) => IsReady && InBounds(row, col) && Grid[row, col] == 1;
    public bool IsWall(Vector2Int c) => IsWall(c.x, c.y);

    // Ô đi được: trong biên và không phải wall.
    public bool IsWalkable(int row, int col) => InBounds(row, col) && (!IsReady || Grid[row, col] == 0);
    public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);

    // ===== Chuyển đổi toạ độ ô lưới ↔ world =====
    // col → trục X (sang phải), row → trục +Y (đi lên) tính từ gốc dưới-trái.

    public Vector3 CellToWorld(int row, int col)
    {
        float offsetX = centerHorizontally ? -(Cols - 1) * 0.5f * CELL_SIZE : 0f;
        return gridOrigin + new Vector3(offsetX + col * CELL_SIZE, row * CELL_SIZE, 0f);
    }

    public Vector3 CellToWorld(Vector2Int c) => CellToWorld(c.x, c.y);

    public Vector2Int WorldToCell(Vector3 world)
    {
        float offsetX = centerHorizontally ? -(Cols - 1) * 0.5f * CELL_SIZE : 0f;
        Vector3 local = world - gridOrigin;
        int col = Mathf.RoundToInt((local.x - offsetX) / CELL_SIZE);
        int row = Mathf.RoundToInt(local.y / CELL_SIZE);
        return new Vector2Int(row, col);
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

    // 4 hướng lân cận theo (row, col): xuống (+row), lên (-row), phải (+col), trái (-col).
    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
    };
}
