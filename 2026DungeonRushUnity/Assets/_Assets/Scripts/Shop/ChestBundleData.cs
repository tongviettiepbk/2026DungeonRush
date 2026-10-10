using System;
using System.Collections.Generic;
using UnityEngine;

// Gói rương bán bằng tiền thật (ChestBundleData gốc): GemAmount gem + mở sẵn các rương trong Chests.
[CreateAssetMenu(fileName = "ChestBundleData", menuName = "DungOnRush/Shop/Chest Bundle Data")]
public class ChestBundleData : ScriptableObject
{
    public string BundleId;
    public string StoreId;
    public Sprite Icon;
    public int GemAmount;
    public List<ChestBundleEntry> Chests = new List<ChestBundleEntry>();
    public List<Sprite> ShowcaseSprites = new List<Sprite>();
}

[Serializable]
public class ChestBundleEntry
{
    public ChestData Chest;
    public int Count;
}
