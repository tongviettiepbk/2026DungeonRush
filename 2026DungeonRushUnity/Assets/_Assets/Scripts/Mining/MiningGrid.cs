using System.Collections.Generic;
using UnityEngine;

// Lưới khối của mỏ (MiningGrid gốc). Toạ độ ô: x = cột 0..GridWidth-1, y = hàng tính theo độ sâu (y tăng = đi XUỐNG).
// World = BlockParent.position + (x·CellSize, −y·CellSize).
// Sinh map KHÔNG lưu: mỗi ô quyết định bằng System.Random seed theo (x, y, seed người chơi):
//   - hàng 0 luôn trống; ô khác rỗng nếu EmptinessThreshold > NextDouble (seed x·0x127409F ^ y·0x4F9FFB7 ^ seed·0x466F45D)
//   - còn lại roll quặng theo trọng số bảng tỉ lệ của level mỏ (seed x·0x1A5D215 ^ y·0x2752387 ^ seed·0x359AF13).
// Chỉ lưu ô đã đào (minedPositions) + khối đập dở (damagedBlocks).
public class MiningGrid : MonoBehaviour
{
    private MiningConfig config;
    private Transform blockParent;
    private Transform hpCanvas;
    private readonly Dictionary<Vector2Int, MiningBlock> blocks = new Dictionary<Vector2Int, MiningBlock>();
    private readonly HashSet<Vector2Int> mined = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> damaged = new Dictionary<Vector2Int, int>();
    private readonly HashSet<Vector2Int> empty = new HashSet<Vector2Int>();
    private readonly Stack<MiningBlock> pool = new Stack<MiningBlock>();
    private int seed;
    private int minGeneratedRow = int.MaxValue;
    private int maxGeneratedRow = int.MinValue;
    private int currentDepth;
    private int layer = -1;

    // Số hàng sinh thêm dưới vùng nhìn thấy (xzz gốc = 3 nhưng thực tế dùng VisibleRows + 2).
    private const int EXTRA_ROWS = 2;

    // jda
    public void Init(MiningConfig cfg, Transform parent, int mapSeed, List<MiningCellData> minedPositions,
        List<MiningDamagedBlockData> damagedBlocks, Camera cam)
    {
        config = cfg;
        blockParent = parent;
        seed = mapSeed;
        layer = parent != null ? parent.gameObject.layer : -1;
        minGeneratedRow = int.MaxValue;
        maxGeneratedRow = int.MinValue;
        CreateHpCanvas(cam);

        mined.Clear();
        damaged.Clear();
        empty.Clear();
        if (minedPositions != null)
        {
            for (int i = 0; i < minedPositions.Count; i++)
            {
                mined.Add(minedPositions[i].Cell);
            }
        }
        if (damagedBlocks != null)
        {
            for (int i = 0; i < damagedBlocks.Count; i++)
            {
                damaged[damagedBlocks[i].Cell] = damagedBlocks[i].currentHits;
            }
        }
    }

    public int GridWidth => config.GridWidth;

    // jdb: sinh các hàng depth .. depth + VisibleRows + 2 còn chưa sinh.
    public void GenerateRows(int depth, int mineLevel)
    {
        currentDepth = depth;
        int end = depth + config.VisibleRows + EXTRA_ROWS;
        for (int row = depth; row <= end; row++)
        {
            if (row < minGeneratedRow || row > maxGeneratedRow)
            {
                GenerateRow(row, mineLevel);
            }
        }
        minGeneratedRow = Mathf.Min(minGeneratedRow, depth);
        maxGeneratedRow = Mathf.Max(maxGeneratedRow, end);
    }

    // jdc: bỏ khối ngoài vùng + dọn dữ liệu các hàng đã trôi lên trên.
    public void CleanupAbove(int depth)
    {
        DespawnOutside(depth, depth + config.VisibleRows + EXTRA_ROWS);
        ForgetRowsAbove(depth);
    }

    // jdd
    private void GenerateRow(int row, int mineLevel)
    {
        MineRarityLevel rarity = config.GetRarityLevel(mineLevel);
        if (rarity == null)
        {
            return;
        }

        float totalWeight = 0f;
        for (int i = 0; i < rarity.OreWeights.Count; i++)
        {
            totalWeight += rarity.OreWeights[i].Weight;
        }

        for (int x = 0; x < config.GridWidth; x++)
        {
            Vector2Int cell = new Vector2Int(x, row);
            if (mined.Contains(cell))
            {
                continue;
            }

            if (row == 0)
            {
                empty.Add(cell);
                continue;
            }

            System.Random random = new System.Random(unchecked((x * 0x127409F) ^ (row * 0x4F9FFB7) ^ (seed * 0x466F45D)));
            if (config.EmptinessThreshold > (float)random.NextDouble())
            {
                empty.Add(cell);
            }
            else
            {
                SpawnBlock(cell, rarity, totalWeight);
            }
        }
    }

    // jde
    private void SpawnBlock(Vector2Int cell, MineRarityLevel rarity, float totalWeight)
    {
        if (blocks.ContainsKey(cell) || rarity.OreWeights.Count == 0)
        {
            return;
        }

        System.Random random = new System.Random(unchecked((cell.x * 0x1A5D215) ^ (cell.y * 0x2752387) ^ (seed * 0x359AF13)));
        float roll = (float)random.NextDouble() * totalWeight;
        MineOreWeight picked = rarity.OreWeights[0];
        float sum = 0f;
        for (int i = 0; i < rarity.OreWeights.Count; i++)
        {
            sum += rarity.OreWeights[i].Weight;
            if (roll < sum)
            {
                picked = rarity.OreWeights[i];
                break;
            }
        }

        Sprite sprite = config.GetOreSprite(picked.OreType);
        if (sprite == null || config.BlockPrefab == null)
        {
            return;
        }

        int hits = config.GetHitsRequired(picked.OreType);
        bool collectible = config.IsCollectible(picked.OreType);
        MiningBlock block = SpawnFromPool();
        block.transform.position = GridToWorld(cell);
        block.Setup(picked.OreType, collectible, hits, sprite, cell, config.CellSize * config.BlockScale);

        if (damaged.TryGetValue(cell, out int curHits))
        {
            if (curHits >= block.HitsRequired)
            {
                // Save cũ đã đập đủ mà chưa ghi mined → coi như đã đào.
                ReturnToPool(block);
                mined.Add(cell);
                damaged.Remove(cell);
                return;
            }

            block.SetCurrentHits(curHits);
            if (block.HitsRequired >= 2)
            {
                ShowHp(block, 1f - (float)curHits / block.HitsRequired);
            }
        }
        blocks[cell] = block;
    }

    // jdf: trả về pool khối nằm ngoài [minRow, maxRow].
    private void DespawnOutside(int minRow, int maxRow)
    {
        List<Vector2Int> remove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, MiningBlock> kv in blocks)
        {
            if (kv.Key.y < minRow || kv.Key.y > maxRow)
            {
                remove.Add(kv.Key);
            }
        }

        for (int i = 0; i < remove.Count; i++)
        {
            ReturnToPool(blocks[remove[i]]);
            blocks.Remove(remove[i]);
        }
    }

    // jdg: bỏ dữ liệu mined/empty/damaged của các hàng < row (không bao giờ quay lên).
    private void ForgetRowsAbove(int row)
    {
        mined.RemoveWhere(c => c.y < row);
        empty.RemoveWhere(c => c.y < row);
        List<Vector2Int> remove = new List<Vector2Int>();
        foreach (Vector2Int cell in damaged.Keys)
        {
            if (cell.y < row)
            {
                remove.Add(cell);
            }
        }
        for (int i = 0; i < remove.Count; i++)
        {
            damaged.Remove(remove[i]);
        }
        minGeneratedRow = Mathf.Max(minGeneratedRow, row);
    }

    // jdh
    public Vector3 GridToWorld(Vector2Int cell)
    {
        Vector3 origin = blockParent.position;
        return new Vector3(origin.x + cell.x * config.CellSize, origin.y - cell.y * config.CellSize, origin.z);
    }

    // jdi: làm tròn kiểu banker's như Math.Round gốc.
    public Vector2Int WorldToGrid(Vector3 world)
    {
        Vector3 origin = blockParent.position;
        int x = (int)System.Math.Round((world.x - origin.x) / config.CellSize);
        int y = (int)System.Math.Round(-(world.y - origin.y) / config.CellSize);
        return new Vector2Int(x, y);
    }

    // jdj: ô đi được = trong vùng nhìn thấy + (đã đào hoặc vốn rỗng).
    public bool IsWalkable(Vector2Int cell)
    {
        if (IsInVisibleArea(cell) == false)
        {
            return false;
        }
        return mined.Contains(cell) || empty.Contains(cell);
    }

    // jdk
    public bool HasBlock(Vector2Int cell)
    {
        return blocks.ContainsKey(cell);
    }

    // jdl
    public MiningBlock GetBlock(Vector2Int cell)
    {
        blocks.TryGetValue(cell, out MiningBlock block);
        return block;
    }

    // jdm: đập khối 1 nhát. true = vỡ (đã xoá khỏi lưới + ghi mined).
    public bool HitBlock(Vector2Int cell)
    {
        if (blocks.TryGetValue(cell, out MiningBlock block) == false)
        {
            return false;
        }

        if (block.Hit())
        {
            RemoveBlock(cell);
            return true;
        }

        damaged[cell] = block.CurrentHits;
        if (block.HitsRequired >= 2 && block.HasHpUI == false)
        {
            ShowHp(block, 1f - (float)block.CurrentHits / block.HitsRequired);
        }
        return false;
    }

    // jdn/jdo: xoá khối khỏi lưới (khoan/cuốc vàng phá thẳng), trả về loại quặng (không có khối → Dirt).
    public MineOreType RemoveBlock(Vector2Int cell)
    {
        if (blocks.TryGetValue(cell, out MiningBlock block) == false)
        {
            return MineOreType.Dirt;
        }

        MineOreType ore = block.OreType;
        ReturnToPool(block);
        blocks.Remove(cell);
        mined.Add(cell);
        damaged.Remove(cell);
        return ore;
    }

    // jdp
    public List<MiningCellData> GetMinedPositions()
    {
        List<MiningCellData> list = new List<MiningCellData>();
        foreach (Vector2Int cell in mined)
        {
            list.Add(new MiningCellData(cell.x, cell.y));
        }
        return list;
    }

    // jdq
    public List<MiningDamagedBlockData> GetDamagedBlocks()
    {
        List<MiningDamagedBlockData> list = new List<MiningDamagedBlockData>();
        foreach (KeyValuePair<Vector2Int, int> kv in damaged)
        {
            list.Add(new MiningDamagedBlockData(kv.Key.x, kv.Key.y, kv.Value));
        }
        return list;
    }

    // jdr: dọn toàn bộ khối (đóng popup).
    public void ClearAll()
    {
        foreach (MiningBlock block in blocks.Values)
        {
            ReturnToPool(block);
        }
        blocks.Clear();
        minGeneratedRow = int.MaxValue;
        maxGeneratedRow = int.MinValue;
    }

    // jdt: hàng không còn khối nào.
    public bool IsRowCleared(int row)
    {
        for (int x = 0; x < config.GridWidth; x++)
        {
            if (blocks.ContainsKey(new Vector2Int(x, row)))
            {
                return false;
            }
        }
        return true;
    }

    // jdu: trong vùng nhìn thấy (cột hợp lệ, hàng currentDepth .. currentDepth + VisibleRows − 1).
    public bool IsInVisibleArea(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < config.GridWidth
            && cell.y >= currentDepth && cell.y < currentDepth + config.VisibleRows;
    }

    // jdv
    private void ShowHp(MiningBlock block, float value)
    {
        if (config.BlockHpUIPrefab == null || hpCanvas == null)
        {
            return;
        }

        GameObject go = Instantiate(config.BlockHpUIPrefab, hpCanvas);
        SetLayerRecursive(go, layer);
        MiningBlockHpUI ui = go.GetComponent<MiningBlockHpUI>();
        if (ui == null)
        {
            Destroy(go);
            return;
        }
        ui.Follow(block.transform);
        ui.SetValue(value);
        block.SetHpUI(ui);
    }

    // jdw: canvas world-space chứa thanh máu khối (sortingOrder 12, scale 0.01).
    private void CreateHpCanvas(Camera cam)
    {
        if (hpCanvas != null)
        {
            return;
        }

        GameObject go = new GameObject("HpUICanvas");
        go.transform.SetParent(blockParent, false);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.sortingOrder = 12;
        go.transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        SetLayerRecursive(go, layer);
        hpCanvas = go.transform;
    }

    private MiningBlock SpawnFromPool()
    {
        MiningBlock block = null;
        while (pool.Count > 0 && block == null)
        {
            block = pool.Pop();
        }

        if (block == null)
        {
            GameObject go = Instantiate(config.BlockPrefab, blockParent);
            SetLayerRecursive(go, layer);
            block = go.GetComponent<MiningBlock>();
        }
        block.gameObject.SetActive(true);
        return block;
    }

    private void ReturnToPool(MiningBlock block)
    {
        if (block == null)
        {
            return;
        }
        block.OnDespawn();
        block.gameObject.SetActive(false);
        pool.Push(block);
    }

    // jdx
    public static void SetLayerRecursive(GameObject go, int newLayer)
    {
        if (newLayer < 0)
        {
            return;
        }
        go.layer = newLayer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }
}
