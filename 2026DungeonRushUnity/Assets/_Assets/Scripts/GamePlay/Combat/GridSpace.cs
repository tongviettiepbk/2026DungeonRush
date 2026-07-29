using UnityEngine;

// Chuyển đổi toạ độ ô lưới ↔ world. Mirror CellSize/GridOrigin của LevelGenerator gốc
// (CellSize=1, gốc lưới ở góc dưới; ô (x,y): x sang phải, y đi lên tới cửa).
public struct GridSpace
{
    public Vector3 origin;
    public float cellSize;
    public int width;
    public int height;
    public bool centerHorizontally;   // canh giữa map theo trục X quanh origin

    public GridSpace(Vector3 origin, float cellSize, int width, int height, bool centerHorizontally = true)
    {
        this.origin = origin;
        this.cellSize = cellSize;
        this.width = width;
        this.height = height;
        this.centerHorizontally = centerHorizontally;
    }

    public Vector3 CellToWorld(int x, int y)
    {
        float offsetX = centerHorizontally ? -(width - 1) * 0.5f * cellSize : 0f;
        return origin + new Vector3(offsetX + x * cellSize, y * cellSize, 0f);
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return CellToWorld(cell.x, cell.y);
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        float offsetX = centerHorizontally ? -(width - 1) * 0.5f * cellSize : 0f;
        Vector3 local = world - origin;
        int x = Mathf.RoundToInt((local.x - offsetX) / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        return new Vector2Int(x, y);
    }
}
