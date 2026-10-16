using System;
using System.Collections.Generic;
using UnityEngine;

// A* 8 hướng của mỏ (class `st` gốc). Đi chéo phải trống CẢ 2 ô cạnh (không cắt góc), chi phí chéo 1.414;
// heuristic octile (dx+dy) + (1.414−2)·min(dx,dy). Tối đa ~500 vòng lặp.
public class MiningPathfinder
{
    private class Node
    {
        public Vector2Int cell;
        public float g;
        public float h;
        public Node parent;

        public float F => g + h;

        public Node(Vector2Int cell, float g, float h, Node parent)
        {
            this.cell = cell;
            this.g = g;
            this.h = h;
            this.parent = parent;
        }
    }

    // yac gốc: 4 hướng thẳng rồi 4 hướng chéo.
    private static readonly Vector2Int[] DIRECTIONS =
    {
        new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
    };

    private const int MAX_ITERATIONS = 500;
    private const float DIAGONAL_COST = 1.414f;

    // jdz: đường ngắn nhất từ start tới 1 trong 8 ô quanh target (target là khối cần đào). null = không có đường.
    public List<Vector2Int> FindPathToNeighbor(Vector2Int start, Vector2Int target, Func<Vector2Int, bool> isWalkable)
    {
        List<Vector2Int> best = null;
        for (int i = 0; i < DIRECTIONS.Length; i++)
        {
            Vector2Int neighbor = target + DIRECTIONS[i];
            if (isWalkable(neighbor) == false)
            {
                continue;
            }

            List<Vector2Int> path = FindPath(start, neighbor, isWalkable);
            if (path != null && (best == null || path.Count < best.Count))
            {
                best = path;
            }
        }
        return best;
    }

    // jdy: A* từ start tới goal (gồm cả 2 đầu).
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)
    {
        if (isWalkable(start) == false || isWalkable(goal) == false)
        {
            return null;
        }

        if (start == goal)
        {
            return new List<Vector2Int> { start };
        }

        Dictionary<Vector2Int, Node> open = new Dictionary<Vector2Int, Node>();
        HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
        open.Add(start, new Node(start, 0f, Heuristic(start, goal), null));

        int iterations = 0;
        while (open.Count > 0 && iterations < MAX_ITERATIONS)
        {
            iterations++;
            Node current = GetLowest(open);
            if (current.cell == goal)
            {
                return Reconstruct(current);
            }

            open.Remove(current.cell);
            closed.Add(current.cell);

            for (int i = 0; i < DIRECTIONS.Length; i++)
            {
                Vector2Int dir = DIRECTIONS[i];
                Vector2Int next = current.cell + dir;
                if (closed.Contains(next) || isWalkable(next) == false)
                {
                    continue;
                }

                float cost = 1f;
                if (dir.x != 0 && dir.y != 0)
                {
                    if (isWalkable(new Vector2Int(current.cell.x + dir.x, current.cell.y)) == false
                        || isWalkable(new Vector2Int(current.cell.x, current.cell.y + dir.y)) == false)
                    {
                        continue;
                    }
                    cost = DIAGONAL_COST;
                }

                float g = current.g + cost;
                if (open.TryGetValue(next, out Node existing))
                {
                    if (g < existing.g)
                    {
                        existing.g = g;
                        existing.parent = current;
                    }
                }
                else
                {
                    open.Add(next, new Node(next, g, Heuristic(next, goal), current));
                }
            }
        }
        return null;
    }

    // jea
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) + (DIAGONAL_COST - 2f) * Mathf.Min(dx, dy);
    }

    // jeb: F nhỏ nhất (bằng nhau giữ node gặp trước).
    private static Node GetLowest(Dictionary<Vector2Int, Node> open)
    {
        Node best = null;
        float bestF = float.MaxValue;
        foreach (Node node in open.Values)
        {
            if (node.F < bestF)
            {
                bestF = node.F;
                best = node;
            }
        }
        return best;
    }

    // jec
    private static List<Vector2Int> Reconstruct(Node node)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (node != null)
        {
            path.Add(node.cell);
            node = node.parent;
        }
        path.Reverse();
        return path;
    }
}
