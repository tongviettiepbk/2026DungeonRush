using UnityEngine;

// Config nhỏ nhất của 1 map lưới: số hàng, số cột, độ rộng mỗi ô (ô vuông cạnh cellSize).
// Chỉ chứa kích thước — KHÔNG chứa wall/spawn/door (những thứ đó do MapGenerator/StaticMapData lo).
//
// Quy ước toạ độ ô: (x, y) với x = cột [0, cols), y = hàng [0, rows).
// Ma trận wall dùng chỉ số grid[x, y] (đồng bộ với Vector2Int và GridSpace toàn dự án).
[System.Serializable]
public class MapConfig
{
    public int rows;            // số hàng (trục Y, chiều cao lưới)
    public int cols;            // số cột (trục X, chiều rộng lưới)
    public float cellSize = 1f; // cạnh 1 ô vuông (mặc định 1 world unit)

    public MapConfig() { }

    public MapConfig(int rows, int cols, float cellSize = 1f)
    {
        this.rows = rows;
        this.cols = cols;
        this.cellSize = cellSize;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < cols && y >= 0 && y < rows;
    }

    public bool InBounds(Vector2Int cell)
    {
        return InBounds(cell.x, cell.y);
    }
}
