using System.Collections.Generic;
using UnityEngine;

// Quản tất cả phần cơ bản của map trong 1 trận (GridManager + PathfindingManager gốc): giữ ma trận
// chướng ngại (0/1), chuyển đổi ô lưới ↔ world, tìm đường A* 8 hướng, kiểm tra tầm nhìn (raycast).
//
// Unit gọi MapController.Instance.FindPath(...) lấy list ô tới đích; việc lái theo path do UnitMover
// (PhysicsMovementController gốc) đảm nhiệm. (Đã hợp nhất CombatMap cũ vào đây — 1 nguồn sự thật cho map.)
public class MapController : Singleton<MapController>
{
    public Transform pointStart;                  // Mốc gốc lưới: ô (0,0) neo ĐÚNG tại đây (góc DƯỚI-TRÁI). Null → dùng fallbackOrigin.
    public Transform topDoor1;                    // Cánh cửa trên bên trái (GameplayEnvironmentRefs.TopDoor1 gốc) — mở khi qua màn.
    public Transform topDoor2;                    // Cánh cửa trên bên phải (TopDoor2).
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

    // Layer chướng ngại (LevelLoader.itp gốc gán layer "Obstacle"; prefab MainObstacle... đều layer 12).
    public const int OBSTACLE_LAYER = 12;

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
            AlignGridBackground();
        }
    }

    // Nền lưới (Custom/GridShader) vẽ theo toạ độ world → dời _Offset để mỗi ô nền trùng đúng 1 ô map
    // (đường kẻ nằm ở mép ô, tâm ô = CellToWorld). Chỉ áp cho nền có cỡ ô = CELL_SIZE.
    private void AlignGridBackground()
    {
        Vector3 edge = CellToWorld(0, 0) - new Vector3(CELL_SIZE, CELL_SIZE, 0f) * 0.5f;
        Vector4 offset = new Vector4(-edge.x, -edge.y, 0f, 0f);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            Material mat = r.sharedMaterial;
            if (mat == null || mat.shader.name != "Custom/GridShader") continue;
            Vector4 size = mat.GetVector("_GridSize");
            if (!Mathf.Approximately(size.x, CELL_SIZE) || !Mathf.Approximately(size.y, CELL_SIZE)) continue;
            r.GetPropertyBlock(block);
            block.SetVector("_Offset", offset);
            r.SetPropertyBlock(block);
        }
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

    // GridManager.ijc: kẹp ô vào trong lưới.
    public Vector2Int ClampCell(Vector2Int cell)
    {
        return new Vector2Int(Mathf.Clamp(cell.x, 0, Rows - 1), Mathf.Clamp(cell.y, 0, Cols - 1));
    }

    // Ô đang có unit khác đứng (cờ "occupied" của ô GridManager gốc — chỉ tính nhân vật, không tính pet).
    public bool IsOccupiedByOther(Vector2Int cell, BaseUnit self)
    {
        var e = GameController.Instance.activeUnits.GetEnumerator();
        while (e.MoveNext())
        {
            BaseUnit unit = e.Current.Value;
            if (unit != null && unit != self && unit.isTargetable && WorldToCell(unit.Transform.position) == cell)
            {
                return true;
            }
        }
        return false;
    }

    // GridCombatController.hoz: tầm nhìn từ from tới to — raycast chỉ vào layer chướng ngại; gần (< 0.01) coi như thấy.
    public static bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.01f) return true;

        RaycastHit2D hit = Physics2D.Raycast(from, delta / distance, distance, 1 << OBSTACLE_LAYER);
        return hit.collider == null;
    }

    // ===== Tìm đường — PathfindingManager.ikb gốc =====
    // A* 8 hướng (lân cận GridManager.ijk: chéo cần cả 2 ô kề vuông góc đi được), chi phí thẳng 1 / chéo 1.414,
    // bước chéo xuất phát từ ô sát chướng ngại (ike) ×3; heuristic octile; mở nút có f nhỏ nhất (gặp trước thắng);
    // tối đa 1000 lượt mở. Đích phải đi được. Trả list ô từ start → goal (gồm cả 2 đầu); rỗng nếu không tới được.
    private const float STRAIGHT_COST = 1f;
    private const float DIAGONAL_COST = 1.414f;
    private const float WALL_DIAGONAL_PENALTY = 3f;
    private const int MAX_EXPANSIONS = 999;

    private class PathNode
    {
        public Vector2Int cell;
        public float g;
        public float h;
        public PathNode parent;
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (!IsReady || !InBounds(start) || !InBounds(goal) || IsWall(goal)) return path;

        // Dictionary như gốc: thứ tự duyệt khi chọn f nhỏ nhất (hoà thì nút gặp trước) khớp bản gốc.
        var open = new Dictionary<Vector2Int, PathNode>();
        var closed = new Dictionary<Vector2Int, PathNode>();
        var neighbors = new List<Vector2Int>(8);
        open.Add(start, new PathNode { cell = start, g = 0f, h = Heuristic(start, goal) });

        for (int expansion = 0; open.Count > 0; expansion++)
        {
            PathNode current = null;
            float bestF = float.MaxValue;
            foreach (PathNode node in open.Values)
            {
                float f = node.g + node.h;
                if (f < bestF)
                {
                    bestF = f;
                    current = node;
                }
            }

            if (current.cell == goal) return Reconstruct(current);

            open.Remove(current.cell);
            closed.Add(current.cell, current);

            MapSpawnCells.GetNeighbors8(Grid, current.cell, neighbors);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector2Int next = neighbors[i];
                if (closed.ContainsKey(next) || !IsWalkable(next)) continue;

                bool diagonal = Mathf.Abs(next.x - current.cell.x) == 1 && Mathf.Abs(next.y - current.cell.y) == 1;
                float cost = diagonal ? DIAGONAL_COST : STRAIGHT_COST;
                if (diagonal && TouchesWall(current.cell)) cost *= WALL_DIAGONAL_PENALTY;

                float g = current.g + cost;
                if (open.TryGetValue(next, out PathNode existing))
                {
                    if (g < existing.g)
                    {
                        existing.g = g;
                        existing.parent = current;
                    }
                }
                else
                {
                    open.Add(next, new PathNode { cell = next, g = g, h = Heuristic(next, goal), parent = current });
                }
            }

            if (expansion > MAX_EXPANSIONS - 1) break;
        }
        return path; // không tới được
    }

    // Tiện dụng: tìm đường theo toạ độ world (quy về ô gần nhất).
    public List<Vector2Int> FindPath(Vector3 startWorld, Vector3 goalWorld)
    {
        return FindPath(WorldToCell(startWorld), WorldToCell(goalWorld));
    }

    // PathfindingManager.ikd: octile — 1.414 × min(Δ) + |Δx − Δy|.
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dRow = Mathf.Abs(a.x - b.x);
        int dCol = Mathf.Abs(a.y - b.y);
        return DIAGONAL_COST * Mathf.Min(dRow, dCol) + Mathf.Abs(dRow - dCol);
    }

    // PathfindingManager.ike: 1 trong 4 ô kề vuông góc (trong lưới) là chướng ngại.
    private bool TouchesWall(Vector2Int cell)
    {
        for (int i = 0; i < Dirs4.Length; i++)
        {
            Vector2Int next = cell + Dirs4[i];
            if (InBounds(next) && Grid[next.x, next.y] != 0) return true;
        }
        return false;
    }

    private static List<Vector2Int> Reconstruct(PathNode node)
    {
        var path = new List<Vector2Int>();
        for (; node != null; node = node.parent)
        {
            path.Add(node.cell);
        }
        path.Reverse();
        return path;
    }

    private static readonly Vector2Int[] Dirs4 =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
    };
}
