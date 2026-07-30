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
        public MapConfig config;
        public int seed;
        public int minWalls = StaticMapData.MIN_OBSTACLE_COUNT;
        public int maxWalls = StaticMapData.MAX_OBSTACLE_COUNT;
        public int maxAttempts = StaticMapData.MAX_GENERATION_ATTEMPTS;
        public Vector2Int start;                  // ô đầu (luôn giữ trống, phải tới được goal)
        public Vector2Int goal;                   // ô cuối (luôn giữ trống)
        public IEnumerable<Vector2Int> keepClear; // ô không được đặt wall (hàng spawn, cửa...)
    }

    // Trả int[cols, rows]: 0 = trống, 1 = wall.
    public static int[,] Generate(Params p)
    {
        MapConfig cfg = p.config;
        int cols = cfg.cols;
        int rows = cfg.rows;
        var rng = new System.Random(p.seed);

        // Ô cấm đặt wall: start + goal + keepClear.
        var blocked = new HashSet<Vector2Int> { p.start, p.goal };
        if (p.keepClear != null)
        {
            foreach (var c in p.keepClear) blocked.Add(c);
        }

        // Ứng viên đặt wall = mọi ô trong lưới trừ ô cấm.
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var c = new Vector2Int(x, y);
                if (!blocked.Contains(c)) candidates.Add(c);
            }
        }

        int target = rng.Next(p.minWalls, p.maxWalls + 1);
        target = Mathf.Min(target, candidates.Count);

        // Thử tối đa maxAttempts lần để ra layout còn thông start→goal.
        List<Vector2Int> chosen = null;
        int attempts = Mathf.Max(1, p.maxAttempts);
        for (int a = 0; a < attempts; a++)
        {
            var pick = PickRandom(candidates, target, rng);
            if (IsConnected(cfg, pick, p.start, p.goal))
            {
                chosen = pick;
                break;
            }
        }

        // Fallback: giảm dần số wall cho tới khi thông (cùng lắm = 0 wall).
        if (chosen == null)
        {
            for (int count = target - 1; count >= 0 && chosen == null; count--)
            {
                var pick = PickRandom(candidates, count, rng);
                if (IsConnected(cfg, pick, p.start, p.goal)) chosen = pick;
            }
            chosen = chosen ?? new List<Vector2Int>();
        }

        var grid = new int[cols, rows];
        for (int i = 0; i < chosen.Count; i++)
        {
            grid[chosen[i].x, chosen[i].y] = 1;
        }
        return grid;
    }

    // Gom danh sách ô wall (grid==1) — tiện cho tầng spawn Instantiate prefab.
    public static List<Vector2Int> CollectWalls(int[,] grid, MapConfig cfg)
    {
        var walls = new List<Vector2Int>();
        for (int x = 0; x < cfg.cols; x++)
        {
            for (int y = 0; y < cfg.rows; y++)
            {
                if (grid[x, y] == 1) walls.Add(new Vector2Int(x, y));
            }
        }
        return walls;
    }

    // Chọn ngẫu nhiên count ô khác nhau từ pool (Fisher–Yates một phần, deterministic theo rng).
    private static List<Vector2Int> PickRandom(List<Vector2Int> pool, int count, System.Random rng)
    {
        var copy = new List<Vector2Int>(pool);
        count = Mathf.Clamp(count, 0, copy.Count);
        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, count);
    }

    // BFS 4 hướng: từ start có tới được goal mà không đi qua ô wall không?
    private static bool IsConnected(MapConfig cfg, List<Vector2Int> walls, Vector2Int start, Vector2Int goal)
    {
        var blocked = new HashSet<Vector2Int>(walls);
        if (blocked.Contains(goal) || blocked.Contains(start)) return false;
        if (start == goal) return true;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            for (int d = 0; d < Dirs.Length; d++)
            {
                var nxt = cur + Dirs[d];
                if (nxt == goal) return true;
                if (!cfg.InBounds(nxt) || blocked.Contains(nxt) || !visited.Add(nxt)) continue;
                queue.Enqueue(nxt);
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
