using System.Collections.Generic;
using UnityEngine;

// Sinh map procedural — tái tạo thuật toán LevelGenerator gốc của DungeonRush.
//
// KẾT QUẢ: ma trận 2 chiều int[cols, rows] với 0 = ô trống (đi được), 1 = ô có wall.
// Bảo đảm luôn có đường đi từ ô start tới ô goal (BFS + thử lại, fallback giảm số wall).
// Cùng seed → cùng layout (deterministic). Xem DecodedData/MAP_AND_SPAWN_MODEL.md.
//
// Generator CHỈ lo wall. Spawn quân / cổng enemy / cửa nằm ở StaticMapData (layout helper),
// không nhét vào ma trận này nữa.
public static class MapGenerator
{
    public class Params
    {
        public int cols;
        public int rows;
        public int seed;
        public int minWalls = StaticMapData.MIN_OBSTACLE_COUNT;
        public int maxWalls = StaticMapData.MAX_OBSTACLE_COUNT;
        public int maxAttempts = StaticMapData.MAX_GENERATION_ATTEMPTS;
        public Vector2Int start;                  // ô đầu (luôn giữ trống, phải tới được goal)
        public Vector2Int goal;                   // ô cuối (luôn giữ trống)
        public IEnumerable<Vector2Int> keepClear; // ô không được đặt wall (hàng spawn, cửa...)
    }

    // Trả int[cols, rows]: 0 = trống, 1 = wall.
    public static int[,] Generate(Params parameters)
    {
        int cols = parameters.cols;
        int rows = parameters.rows;
        var random = new System.Random(parameters.seed);

        // Ô cấm đặt wall: start + goal + keepClear.
        var blocked = new HashSet<Vector2Int> { parameters.start, parameters.goal };
        if (parameters.keepClear != null)
        {
            foreach (var cell in parameters.keepClear) blocked.Add(cell);
        }

        // Ứng viên đặt wall = mọi ô trong lưới trừ ô cấm.
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!blocked.Contains(cell)) candidates.Add(cell);
            }
        }

        int targetWallCount = random.Next(parameters.minWalls, parameters.maxWalls + 1);
        targetWallCount = Mathf.Min(targetWallCount, candidates.Count);

        // Thử tối đa maxAttempts lần để ra layout còn thông start→goal.
        List<Vector2Int> chosenWalls = null;
        int attempts = Mathf.Max(1, parameters.maxAttempts);
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            var pickedWalls = PickRandom(candidates, targetWallCount, random);
            if (IsConnected(cols, rows, pickedWalls, parameters.start, parameters.goal))
            {
                chosenWalls = pickedWalls;
                break;
            }
        }

        // Fallback: giảm dần số wall cho tới khi thông (cùng lắm = 0 wall).
        if (chosenWalls == null)
        {
            for (int count = targetWallCount - 1; count >= 0 && chosenWalls == null; count--)
            {
                var pickedWalls = PickRandom(candidates, count, random);
                if (IsConnected(cols, rows, pickedWalls, parameters.start, parameters.goal)) chosenWalls = pickedWalls;
            }
            chosenWalls = chosenWalls ?? new List<Vector2Int>();
        }

        var grid = new int[cols, rows];
        for (int i = 0; i < chosenWalls.Count; i++)
        {
            grid[chosenWalls[i].x, chosenWalls[i].y] = 1;
        }
        return grid;
    }

    // Gom danh sách ô wall (grid==1) — tiện cho tầng spawn Instantiate prefab.
    public static List<Vector2Int> CollectWalls(int[,] grid)
    {
        var walls = new List<Vector2Int>();
        int cols = grid.GetLength(0);
        int rows = grid.GetLength(1);
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] == 1) walls.Add(new Vector2Int(x, y));
            }
        }
        return walls;
    }

    // Chọn ngẫu nhiên count ô khác nhau từ pool (Fisher–Yates một phần, deterministic theo random).
    private static List<Vector2Int> PickRandom(List<Vector2Int> pool, int count, System.Random random)
    {
        var copy = new List<Vector2Int>(pool);
        count = Mathf.Clamp(count, 0, copy.Count);
        for (int i = 0; i < count; i++)
        {
            int j = random.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, count);
    }

    // BFS 4 hướng: từ start có tới được goal mà không đi qua ô wall không?
    private static bool IsConnected(int cols, int rows, List<Vector2Int> walls, Vector2Int start, Vector2Int goal)
    {
        var blocked = new HashSet<Vector2Int>(walls);
        if (blocked.Contains(goal) || blocked.Contains(start)) return false;
        if (start == goal) return true;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            for (int dirIndex = 0; dirIndex < Dirs.Length; dirIndex++)
            {
                var next = current + Dirs[dirIndex];
                if (next == goal) return true;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows
                    || blocked.Contains(next) || !visited.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        return false;
    }

    private static readonly Vector2Int[] Dirs =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1),
    };
}
