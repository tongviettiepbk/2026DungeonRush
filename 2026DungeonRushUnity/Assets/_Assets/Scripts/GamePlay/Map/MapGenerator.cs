using System.Collections.Generic;
using UnityEngine;

// Sinh map procedural — tái tạo thuật toán LevelGenerator gốc của DungeonRush.
//
// Quy trình (suy ra từ field + tên method của LevelGenerator.cs, body bị strip il2cpp):
//   1. Random số box trong [MinObstacleCount, MaxObstacleCount].
//   2. Rải box vào các ô trống, tránh KeepClearZones (cổng spawn, cửa) + hàng spawn.
//   3. BFS kiểm tra còn đường từ hàng spawn quân (dưới) tới cửa (trên).
//   4. Nếu bị chặn → thử lại, tối đa MaxGenerationAttempts lần.
// Cùng seed → cùng layout (deterministic). Xem DecodedData/MAP_AND_SPAWN_MODEL.md.
public static class MapGenerator
{
    // Vùng chữ nhật giữ trống, mirror KeepClearZone gốc (BottomLeft/TopRight, bao gồm cả 2 biên).
    public struct KeepClearZone
    {
        public Vector2Int bottomLeft;
        public Vector2Int topRight;

        public KeepClearZone(Vector2Int bottomLeft, Vector2Int topRight)
        {
            this.bottomLeft = bottomLeft;
            this.topRight = topRight;
        }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= bottomLeft.x && cell.x <= topRight.x
                && cell.y >= bottomLeft.y && cell.y <= topRight.y;
        }
    }

    public class GenParams
    {
        public int width = StaticMapData.GRID_WIDTH;
        public int height = StaticMapData.GRID_HEIGHT;
        public int minObstacleCount = StaticMapData.MIN_OBSTACLE_COUNT;
        public int maxObstacleCount = StaticMapData.MAX_OBSTACLE_COUNT;
        public int maxGenerationAttempts = StaticMapData.MAX_GENERATION_ATTEMPTS;
        public int seed;
        public List<KeepClearZone> keepClearZones = new List<KeepClearZone>();
    }

    public class GeneratedMap
    {
        public int width;
        public int height;
        public MapCellType[,] cells;            // cells[x, y]
        public List<Vector2Int> obstacles = new List<Vector2Int>();
        public Vector2Int doorCell;
        public Vector2Int enemySpawnCell;       // cổng enemy (giữa hàng trên)
        public List<Vector2Int> playerSpawnCells = new List<Vector2Int>();
        public int usedAttempts;

        public MapCellType Get(int x, int y)
        {
            return (x >= 0 && x < width && y >= 0 && y < height) ? cells[x, y] : MapCellType.Obstacle;
        }
    }

    public static GeneratedMap Generate(GenParams p)
    {
        int w = p.width;
        int h = p.height;
        var rng = new System.Random(p.seed);

        // Ô cố định: cửa + cổng enemy ở giữa hàng trên, quân người chơi ở hàng dưới cùng.
        int midX = w / 2;
        var doorCell = new Vector2Int(midX, h - 1);
        var enemySpawnCell = new Vector2Int(midX, h - 1);

        var playerSpawnCells = new List<Vector2Int>();
        for (int y = 0; y < StaticMapData.PLAYER_SPAWN_ROWS; y++)
        {
            for (int x = 0; x < w; x++)
            {
                playerSpawnCells.Add(new Vector2Int(x, y));
            }
        }

        // Ô cấm đặt box: KeepClearZones + hàng spawn quân + hàng spawn enemy/cửa.
        var blocked = new HashSet<Vector2Int>(playerSpawnCells);
        for (int y = h - StaticMapData.ENEMY_SPAWN_ROWS; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                blocked.Add(new Vector2Int(x, y));
            }
        }
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var c = new Vector2Int(x, y);
                for (int z = 0; z < p.keepClearZones.Count; z++)
                {
                    if (p.keepClearZones[z].Contains(c)) { blocked.Add(c); break; }
                }
            }
        }

        // Danh sách ô ứng viên đặt box.
        var candidates = new List<Vector2Int>();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                var c = new Vector2Int(x, y);
                if (!blocked.Contains(c)) candidates.Add(c);
            }
        }

        int targetCount = rng.Next(p.minObstacleCount, p.maxObstacleCount + 1);
        targetCount = Mathf.Min(targetCount, candidates.Count);

        List<Vector2Int> chosen = null;
        int attempt = 0;
        for (; attempt < Mathf.Max(1, p.maxGenerationAttempts); attempt++)
        {
            var pick = PickRandom(candidates, targetCount, rng);
            if (IsConnected(w, h, pick, playerSpawnCells, doorCell))
            {
                chosen = pick;
                break;
            }
        }

        // Fallback: không tìm được layout hợp lệ → giảm dần số box cho tới khi thông.
        if (chosen == null)
        {
            for (int count = targetCount - 1; count >= 0 && chosen == null; count--)
            {
                var pick = PickRandom(candidates, count, rng);
                if (IsConnected(w, h, pick, playerSpawnCells, doorCell)) chosen = pick;
            }
            chosen = chosen ?? new List<Vector2Int>();
        }

        // Dựng lưới kết quả.
        var map = new GeneratedMap
        {
            width = w,
            height = h,
            cells = new MapCellType[w, h],
            doorCell = doorCell,
            enemySpawnCell = enemySpawnCell,
            playerSpawnCells = playerSpawnCells,
            usedAttempts = attempt + 1,
        };
        for (int i = 0; i < chosen.Count; i++)
        {
            map.cells[chosen[i].x, chosen[i].y] = MapCellType.Obstacle;
            map.obstacles.Add(chosen[i]);
        }
        for (int i = 0; i < playerSpawnCells.Count; i++)
        {
            map.cells[playerSpawnCells[i].x, playerSpawnCells[i].y] = MapCellType.PlayerSpawn;
        }
        map.cells[enemySpawnCell.x, enemySpawnCell.y] = MapCellType.EnemySpawn;
        map.cells[doorCell.x, doorCell.y] = MapCellType.Door;
        return map;
    }

    // Chọn ngẫu nhiên count ô khác nhau từ pool (Fisher–Yates một phần).
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

    // BFS: từ bất kỳ ô spawn quân nào có tới được ô cửa mà không đi qua box không?
    private static bool IsConnected(int w, int h, List<Vector2Int> obstacles, List<Vector2Int> starts, Vector2Int goal)
    {
        var blocked = new HashSet<Vector2Int>(obstacles);
        if (blocked.Contains(goal)) return false;

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        for (int i = 0; i < starts.Count; i++)
        {
            if (!blocked.Contains(starts[i]) && visited.Add(starts[i]))
            {
                queue.Enqueue(starts[i]);
            }
        }

        var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            if (cur == goal) return true;
            for (int d = 0; d < dirs.Length; d++)
            {
                var nxt = cur + dirs[d];
                if (nxt.x < 0 || nxt.x >= w || nxt.y < 0 || nxt.y >= h) continue;
                if (blocked.Contains(nxt) || !visited.Add(nxt)) continue;
                queue.Enqueue(nxt);
            }
        }
        return false;
    }
}
