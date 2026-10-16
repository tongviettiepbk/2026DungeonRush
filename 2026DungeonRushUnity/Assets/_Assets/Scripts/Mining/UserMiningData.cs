using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// Ô đã đào (SerializableVector2Int gốc).
public class MiningCellData
{
    public int x { get; set; }
    public int y { get; set; }

    public MiningCellData() { }

    public MiningCellData(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    [JsonIgnore] public Vector2Int Cell => new Vector2Int(x, y);
}

// Khối đang đập dở (DamagedBlockData gốc).
public class MiningDamagedBlockData
{
    public int x { get; set; }
    public int y { get; set; }
    public int currentHits { get; set; }

    public MiningDamagedBlockData() { }

    public MiningDamagedBlockData(int x, int y, int currentHits)
    {
        this.x = x;
        this.y = y;
        this.currentHits = currentHits;
    }

    [JsonIgnore] public Vector2Int Cell => new Vector2Int(x, y);
}

// Save hệ Đào mỏ (MiningData gốc, User.MiningData). Khác gốc: số cuốc/khoan/cuốc vàng + quặng
// nằm ở UserItemData (ItemType.PICKAXE/DRILL/GOLDEN_PICKAXE, COAL_ORE..STONE_ORE) vì Shop/Wing đã dùng.
public class UserMiningData : BaseUserData
{
    public int seed { get; set; }                 // 0 = chưa vào mỏ lần nào (jbe gốc tạo seed + tặng StartingPickaxeCount)
    public int currentDepth { get; set; }         // hàng trên cùng đang thấy (mét)
    public int mineLevel { get; set; } = 1;
    public int characterGridX { get; set; }
    public int characterGridY { get; set; }
    public List<MiningCellData> minedPositions { get; set; } = new List<MiningCellData>();
    public List<MiningDamagedBlockData> damagedBlocks { get; set; } = new List<MiningDamagedBlockData>();
    public long lastPickaxeRegenTime { get; set; }   // unix giây
    public int pickaxeAdDailyCount { get; set; }
    public int drillAdDailyCount { get; set; }
    public int goldenPickaxeAdDailyCount { get; set; }
    public long lastMiningAdResetTime { get; set; } = -1;   // unix giây (UTC), -1 = chưa reset lần nào
    public int offerPurchasedCount { get; set; }      // User.MiningOfferPurchasedCount gốc

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_MINING;
    }

    public override void InitData()
    {
        base.InitData();
        mineLevel = 1;
        lastMiningAdResetTime = -1;
        isDataChanged = true;
    }

    public override void ValidateData()
    {
        if (minedPositions == null)
        {
            minedPositions = new List<MiningCellData>();
            isDataChanged = true;
        }
        if (damagedBlocks == null)
        {
            damagedBlocks = new List<MiningDamagedBlockData>();
            isDataChanged = true;
        }
        if (mineLevel < 1)
        {
            mineLevel = 1;
            isDataChanged = true;
        }
        if (currentDepth < 0)
        {
            currentDepth = 0;
            isDataChanged = true;
        }
    }
}
