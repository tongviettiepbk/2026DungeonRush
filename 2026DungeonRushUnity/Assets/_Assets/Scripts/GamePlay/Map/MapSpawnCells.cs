using System.Collections.Generic;
using UnityEngine;

// Chọn ô đứng cho hero / pet / quái — port các hàm GridManager + CompanionController gốc (reverse libil2cpp v41).
// Làm việc trên lưới int[rows, cols] của project (0 = trống, 1 = chướng ngại), ô = Vector2Int(row, col).
// Ô gốc (x, y) ↔ project (row = y, col = x + cols/2) — cột giữa gốc x = 0 là col = cols/2.
public static class MapSpawnCells
{
    private const int PET_JOIN_SEARCH = 20;   // CompanionController.gmf: ijo(start, 20)
    private const int PET_SPACING = 2;        // CompanionController.gmd: pet cách nhau 2 cột

    // GridManager.ijp → SpawnController.hus: hero = ô trống gần ô (hàng 2, cột giữa) nhất.
    public static Vector2Int GetHeroSpawnCell(int[,] grid, ICollection<Vector2Int> occupied = null)
    {
        var center = new Vector2Int(StaticMapData.HERO_SPAWN_ROW, grid.GetLength(1) / 2);
        List<Vector2Int> cells = GetNearestFreeCells(grid, center, 1, occupied);
        return cells.Count > 0 ? cells[0] : center;
    }

    // CompanionController.gmd (đầu màn): N pet xếp 1 hàng ngay DƯỚI hero, cách nhau 2 cột, canh giữa
    // (N=1: 0 | N=2: 0,+2 | N=3: -2,0,+2), kẹp vào lưới (không né chướng ngại — hàng đó luôn trống).
    public static Vector2Int GetPetStartCell(int[,] grid, Vector2Int heroCell, int index, int petCount)
    {
        int offset = -((Mathf.Max(petCount, 1) - 1) / 2);
        var cell = new Vector2Int(heroCell.x - 1, heroCell.y + (index + offset) * PET_SPACING);
        return Clamp(grid, cell);
    }

    // CompanionController.gmf (đổi pet giữa trận): ô trống gần ô ngay dưới hero nhất, chưa bị pet khác giữ.
    public static Vector2Int GetPetJoinCell(int[,] grid, Vector2Int heroCell, ICollection<Vector2Int> taken)
    {
        Vector2Int start = Clamp(grid, new Vector2Int(heroCell.x - 1, heroCell.y));
        List<Vector2Int> cells = GetNearestFreeCells(grid, start, PET_JOIN_SEARCH);
        for (int i = 0; i < cells.Count; i++)
        {
            if (taken == null || !taken.Contains(cells[i])) return cells[i];
        }
        return cells.Count > 0 ? cells[0] : start;
    }

    // GridManager.ijx: ô đứng của quái — ô trống từ hàng 5 trở lên, nằm trong vùng đi tới được từ ô
    // (hàng 0, cột giữa) (ijw), bốc ngẫu nhiên không trùng (UnityEngine.Random). Thiếu ô → trả ít hơn count.
    public static List<Vector2Int> FindEnemySpawnCells(int[,] grid, int count, ICollection<Vector2Int> occupied = null)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        HashSet<Vector2Int> reachable = GetReachableCells(grid, new Vector2Int(0, cols / 2));

        var candidates = new List<Vector2Int>();
        for (int col = 0; col < cols; col++)
        {
            for (int row = StaticMapData.ENEMY_MIN_ROW; row < rows; row++)
            {
                var cell = new Vector2Int(row, col);
                if (IsFree(grid, cell, occupied) && reachable.Contains(cell)) candidates.Add(cell);
            }
        }

        var result = new List<Vector2Int>();
        int total = Mathf.Min(count, candidates.Count);
        for (int i = 0; i < total; i++)
        {
            int index = Random.Range(0, candidates.Count);
            result.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        return result;
    }

    // GridManager.ijo: count ô trống gần center nhất (khoảng cách Euclid). Quét cột ngoài / hàng trong
    // rồi List.Sort như gốc để thứ tự các ô bằng khoảng cách ra y hệt.
    public static List<Vector2Int> GetNearestFreeCells(int[,] grid, Vector2Int center, int count, ICollection<Vector2Int> occupied = null)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);
        var cells = new List<Vector2Int>();
        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                var cell = new Vector2Int(row, col);
                if (IsFree(grid, cell, occupied)) cells.Add(cell);
            }
        }

        cells.Sort((a, b) => Distance(a, center).CompareTo(Distance(b, center)));
        return cells.GetRange(0, Mathf.Min(count, cells.Count));
    }

    // GridManager.ijw: flood-fill 8 hướng từ start (ijk có chéo; đi chéo cần CẢ 2 ô kề vuông góc đi được — ijj).
    public static HashSet<Vector2Int> GetReachableCells(int[,] grid, Vector2Int start)
    {
        var visited = new HashSet<Vector2Int>();
        if (!IsWalkable(grid, start)) return visited;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited.Add(start);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int i = 0; i < Dirs8.Length; i++)
            {
                Vector2Int dir = Dirs8[i];
                Vector2Int next = current + dir;
                if (!InBounds(grid, next)) continue;
                if (dir.x != 0 && dir.y != 0
                    && !(IsWalkable(grid, new Vector2Int(current.x, next.y)) && IsWalkable(grid, new Vector2Int(next.x, current.y)))) continue;
                if (visited.Contains(next) || !IsWalkable(grid, next)) continue;

                visited.Add(next);
                queue.Enqueue(next);
            }
        }
        return visited;
    }

    // GridManager.ijc: kẹp ô vào trong lưới.
    public static Vector2Int Clamp(int[,] grid, Vector2Int cell)
    {
        return new Vector2Int(Mathf.Clamp(cell.x, 0, grid.GetLength(0) - 1), Mathf.Clamp(cell.y, 0, grid.GetLength(1) - 1));
    }

    private static float Distance(Vector2Int a, Vector2Int b)
    {
        float dRow = a.x - b.x;
        float dCol = a.y - b.y;
        return Mathf.Sqrt(dCol * dCol + dRow * dRow);
    }

    private static bool InBounds(int[,] grid, Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < grid.GetLength(0) && cell.y >= 0 && cell.y < grid.GetLength(1);
    }

    private static bool IsWalkable(int[,] grid, Vector2Int cell)
    {
        return InBounds(grid, cell) && grid[cell.x, cell.y] == 0;
    }

    private static bool IsFree(int[,] grid, Vector2Int cell, ICollection<Vector2Int> occupied)
    {
        return IsWalkable(grid, cell) && (occupied == null || !occupied.Contains(cell));
    }

    // GridManager.ijk: 4 hướng thẳng rồi 4 hướng chéo, theo (row, col) tương ứng thứ tự gốc (x, y).
    private static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1),
    };
}
