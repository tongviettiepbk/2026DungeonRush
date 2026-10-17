using System.Collections.Generic;
using UnityEngine;

// Sinh chướng ngại (box) của map — port NGUYÊN thuật toán gốc `rw.itr` ("RuntimeLevelObstacleGenerator",
// reverse libil2cpp v41; gọi từ LevelController.hpv → LevelLoader.itp(level) → rw.itr(level, keepClear)).
//
// KẾT QUẢ: int[rows, cols], 0 = trống, 1 = chướng ngại. Cùng level → cùng layout (seed cố định).
//
// Toạ độ GỐC (GridManager): ô (x, y), x ∈ [-cols/2, cols - cols/2), y ∈ [0, rows). Cột giữa là x = 0.
// Đổi sang lưới của project: row = y, col = x + cols/2 (xem [[dungonrush-map-coord-convention]]).
//
// Luật gốc:
//   - Vùng cấm (LevelLoader.itq): "Player Spawn" = 3 hàng dưới (y 0..2), "Enemy Spawn" = 3 hàng trên.
//   - Ứng viên = CHỈ nửa TRÁI (x < 0) ngoài vùng cấm; bốc count = Next(5, 11) ô rồi LẬT GƯƠNG sang phải
//     (cột lẻ: x → -x, cột chẵn: x → -x-1) ⇒ map luôn đối xứng, với 9 cột thì cột giữa luôn trống.
//   - Seed: đếm số ô = Random(level×7912 + vòng×1000); bốc ô = Random(level×7912 + vòng×1000 + lần thử).
//     100 vòng × 10 lần thử, nhận layout đầu tiên còn đường 8 hướng từ (0,0) lên (0, rows-1).
//   - Hết lượt mà không ra → fallback `iua`: 6 cặp ô đối xứng ngẫu nhiên Random(level).
public static class MapGenerator
{
    private const int SEED_PER_LEVEL = 7912;     // rw.its: level × 0x1EE8
    private const int SEED_PER_ROUND = 1000;
    private const int ROUNDS = 100;              // xue
    private const int TRIES_PER_ROUND = 10;      // xud
    private const int PICK_MIN = 5;              // xub (ctor gán 5)
    private const int PICK_MAX = 10;             // xuc (ctor gán 10) — Next(PICK_MIN, PICK_MAX + 1)
    private const int FALLBACK_PAIRS = 6;        // iua

    // level: level dùng làm seed (campaign = level campaign liên tục; dungeon = level dungeon).
    // spawnObstacles = false (MapConfig.SpawnObstacles = 0: Boss/PvP/Dragon) → lưới trống.
    public static int[,] Generate(int level, int cols, int rows, bool spawnObstacles)
    {
        var grid = new int[rows, cols];
        if (!spawnObstacles)
        {
            return grid;
        }

        var shape = new Shape(cols, rows);
        List<Vector2Int> obstacles = GenerateOriginal(level, shape);
        for (int i = 0; i < obstacles.Count; i++)
        {
            Vector2Int cell = obstacles[i];
            grid[cell.y, cell.x + shape.half] = 1;
        }
        return grid;
    }

    // Gom danh sách ô chướng ngại (grid==1) theo (row, col) — tiện cho tầng spawn Instantiate prefab.
    public static List<Vector2Int> CollectWalls(int[,] grid)
    {
        var walls = new List<Vector2Int>();
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (grid[row, col] == 1) walls.Add(new Vector2Int(row, col));
            }
        }
        return walls;
    }

    // Kích thước lưới theo cách gốc tính (rw ctor): half = cols/2, xMax (loại trừ) = cols - half.
    private struct Shape
    {
        public readonly int cols, rows, half, xMax;
        public readonly bool odd;

        public Shape(int cols, int rows)
        {
            this.cols = cols;
            this.rows = rows;
            half = cols / 2;
            xMax = cols - half;
            odd = (cols & 1) == 1;
        }

        public int Mirror(int x) => odd ? -x : -x - 1;

        // Ô thuộc vùng cấm "Player Spawn" (y 0..2) / "Enemy Spawn" (y rows-3..rows-1)? (rw.itu)
        public bool InKeepClear(Vector2Int cell)
        {
            return cell.y <= StaticMapData.KEEP_CLEAR_ROWS - 1 || cell.y >= rows - StaticMapData.KEEP_CLEAR_ROWS;
        }
    }

    // rw.itr — trả list ô theo toạ độ GỐC (x, y).
    private static List<Vector2Int> GenerateOriginal(int level, Shape shape)
    {
        int baseSeed = level * SEED_PER_LEVEL;
        for (int round = 0; round < ROUNDS; round++)
        {
            int roundSeed = baseSeed + round * SEED_PER_ROUND;
            int count = new NetRandom(roundSeed).Next(PICK_MIN, PICK_MAX + 1);

            for (int attempt = 0; attempt < TRIES_PER_ROUND; attempt++)
            {
                var random = new NetRandom(roundSeed + attempt);
                List<Vector2Int> candidates = Candidates(shape);
                if (candidates.Count == 0)
                {
                    return new List<Vector2Int>();
                }

                List<Vector2Int> obstacles = Dedupe(Mirror(Pick(count, candidates, random), shape));
                if (HasPath(obstacles, shape))
                {
                    return obstacles;
                }
            }
        }

        return Fallback(level, shape);
    }

    // rw.itt: ô nửa trái (x = -half..-1), quét x ngoài / y trong, bỏ ô trong vùng cấm.
    private static List<Vector2Int> Candidates(Shape shape)
    {
        var cells = new List<Vector2Int>();
        for (int x = -shape.half; x < 0; x++)
        {
            for (int y = 0; y < shape.rows; y++)
            {
                var cell = new Vector2Int(x, y);
                if (!shape.InKeepClear(cell)) cells.Add(cell);
            }
        }
        return cells;
    }

    // rw.itv: bốc min(count, pool) ô không trùng — idx = Next(0, pool.Count), RemoveAt.
    private static List<Vector2Int> Pick(int count, List<Vector2Int> candidates, NetRandom random)
    {
        var picked = new List<Vector2Int>();
        var pool = new List<Vector2Int>(candidates);
        int total = Mathf.Min(count, pool.Count);
        for (int i = 0; i < total; i++)
        {
            int index = random.Next(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return picked;
    }

    // rw.itw: giữ ô đã bốc + thêm ô lật gương (nếu không rơi vào vùng cấm).
    private static List<Vector2Int> Mirror(List<Vector2Int> picked, Shape shape)
    {
        var result = new List<Vector2Int>(picked);
        for (int i = 0; i < picked.Count; i++)
        {
            var mirror = new Vector2Int(shape.Mirror(picked[i].x), picked[i].y);
            if (!shape.InKeepClear(mirror)) result.Add(mirror);
        }
        return result;
    }

    // rw.itx: bỏ trùng, giữ thứ tự xuất hiện.
    private static List<Vector2Int> Dedupe(List<Vector2Int> cells)
    {
        var seen = new HashSet<Vector2Int>();
        var result = new List<Vector2Int>();
        for (int i = 0; i < cells.Count; i++)
        {
            if (seen.Add(cells[i])) result.Add(cells[i]);
        }
        return result;
    }

    // rw.ity + itz: BFS 8 hướng từ (0,0) tới (0, rows-1) né chướng ngại. Đi chéo chỉ bị chặn
    // khi CẢ 2 ô kề vuông góc đều là chướng ngại.
    private static bool HasPath(List<Vector2Int> obstacles, Shape shape)
    {
        var blocked = new HashSet<Vector2Int>(obstacles);
        var start = new Vector2Int(0, 0);
        var goal = new Vector2Int(0, shape.rows - 1);
        if (blocked.Contains(start) || blocked.Contains(goal)) return false;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == goal) return true;

            for (int i = 0; i < Dirs8.Length; i++)
            {
                Vector2Int dir = Dirs8[i];
                Vector2Int next = current + dir;
                if (next.x < -shape.half || next.x >= shape.xMax || next.y < 0 || next.y >= shape.rows) continue;
                if (blocked.Contains(next) || visited.Contains(next)) continue;
                if (dir.x != 0 && dir.y != 0
                    && blocked.Contains(new Vector2Int(next.x, current.y))
                    && blocked.Contains(new Vector2Int(current.x, current.y + dir.y))) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return false;
    }

    // rw.iua: 6 lần bốc x = Next(1-half, -1), y = Next(3, rows-3) + ô lật gương.
    private static List<Vector2Int> Fallback(int level, Shape shape)
    {
        var random = new NetRandom(level);
        var cells = new List<Vector2Int>();
        for (int i = 0; i < FALLBACK_PAIRS; i++)
        {
            int x = random.Next(1 - shape.half, -1);
            int y = random.Next(StaticMapData.KEEP_CLEAR_ROWS, shape.rows - StaticMapData.KEEP_CLEAR_ROWS);
            cells.Add(new Vector2Int(x, y));
            cells.Add(new Vector2Int(shape.Mirror(x), y));
        }
        return Dedupe(cells);
    }

    // rw.itz: thứ tự hướng của gốc (4 thẳng rồi 4 chéo).
    private static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
    };
}
