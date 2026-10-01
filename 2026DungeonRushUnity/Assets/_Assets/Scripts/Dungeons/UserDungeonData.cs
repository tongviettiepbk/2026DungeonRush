using Newtonsoft.Json;
using System;
using System.Collections.Generic;

// Tiến độ + key của 1 dungeon. Gốc lưu rời trong User: DragonBossLevel/HighestLevel/DailyKeys/BonusKeys/
// RewardedAdWatchCount (tương tự ZombieHorde*, Cultist*).
public class DungeonProgress
{
    public int level { get; set; } = 1;                                  // màn sẽ đánh tiếp, bắt đầu 1
    public int dailyKeys { get; set; } = StaticDungeonData.DAILY_KEYS;   // key miễn phí, reset mỗi ngày
    public int bonusKeys { get; set; }                                   // key từ ads, KHÔNG reset
    public int adWatchCount { get; set; }                                // lượt ads đã xem hôm nay

    [JsonIgnore] public int TotalKeys => dailyKeys + bonusKeys;
}

// Save hệ Dungeon (module BaseUserData → 1 key PlayerPrefs JSON, xem UserData).
// Key dict là (int)DungeonType dạng string (giống UserItemData).
public class UserDungeonData : BaseUserData
{
    public Dictionary<string, DungeonProgress> dungeons { get; set; } = new Dictionary<string, DungeonProgress>();
    // Unix giây (UTC) lần reset key/ads gần nhất; -1 = chưa reset lần nào (Last*KeyResetTime gốc).
    public long lastResetTime { get; set; } = -1;

    protected override string GetDataKey()
    {
        return UserData.DATA_KEY_DUNGEON;
    }

    public override void InitData()
    {
        base.InitData();
        dungeons = new Dictionary<string, DungeonProgress>();
        lastResetTime = -1;
        ValidateData();
        isDataChanged = true;
    }

    // Đảm bảo đủ entry cho mọi DungeonType, level >= 1.
    public override void ValidateData()
    {
        if (dungeons == null)
        {
            dungeons = new Dictionary<string, DungeonProgress>();
            isDataChanged = true;
        }

        foreach (DungeonType type in Enum.GetValues(typeof(DungeonType)))
        {
            string id = ((int)type).ToString();
            if (!dungeons.ContainsKey(id) || dungeons[id] == null)
            {
                dungeons[id] = new DungeonProgress();
                isDataChanged = true;
            }
            else if (dungeons[id].level < 1)
            {
                dungeons[id].level = 1;
                isDataChanged = true;
            }
        }
    }

    public DungeonProgress Get(DungeonType type)
    {
        return dungeons[((int)type).ToString()];
    }
}
