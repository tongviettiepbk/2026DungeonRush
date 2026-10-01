using System.Collections.Generic;

// 1 dungeon: tên, loại tài nguyên thưởng, mốc mở khoá, công thức thưởng.
public class DungeonConfig
{
    public DungeonType type;
    public ModeType modeType;           // mode chơi khi bấm Enter (MapConfig.EnvironmentType gốc)
    public string displayName;
    public ItemType rewardType;
    public int unlockPlayerLevel;
    // Thưởng khi thắng màn N = rewardBase + rewardScaler * (N - 1)  (EconomyController.hcu/hct/hcv gốc).
    public int rewardBase;
    public int rewardScaler;
}

// Config tĩnh hệ Dungeon — REVERSE từ game gốc v41 (DungeonController/DungeonPopup/DungeonTabPage).
// Nhúng thẳng giá trị như CompanionSummonConfig. Nguồn: DecodedData/tables/DungeonData.json,
// GameResources (StarterScene: Dungeon*RewardBase/Scaler, CultistDungeonUnlockPlayerLevel), libil2cpp.
//
// Tóm tắt luật gốc:
// - Mỗi dungeon có key riêng = dailyKeys + bonusKeys. Sang NGÀY UTC mới → dailyKeys = 2 (không cộng dồn).
// - Xem ads: +1 bonusKey, tối đa 3 lần/ngày/dungeon (lượt ads reset cùng lúc với key).
// - Chỉ THẮNG mới tiêu key (thua/thoát không tiêu). Thắng: nhận thưởng màn hiện tại, level + 1.
// - Sweep Last: tiêu 1 key, nhận ngay thưởng màn vừa qua (level - 1), level giữ nguyên.
public class StaticDungeonData
{
    // Key miễn phí mỗi ngày (DungeonController.hbw/hbx/hby → UserController.dtp kẹp ≤ 2).
    public const int DAILY_KEYS = 2;
    // Lượt xem ads mỗi ngày mỗi dungeon (DungeonPopup.kcr, UserController.dua kẹp ≤ 3).
    public const int MAX_AD_PER_DAY = 3;
    // Level N hiển thị "{(N-1)/10+1}-{(N-1)%10+1}" (DungeonPopup.kch).
    public const int LEVELS_PER_DIFFICULTY = 10;

    public readonly List<DungeonConfig> dungeons = new List<DungeonConfig>
    {
        // Dragon's Hoard không khoá riêng (chỉ theo mốc mở tab Dungeon = PlayerLevel 4). Thưởng Hammer = LOOT_TICKET.
        new DungeonConfig { type = DungeonType.DragonBoss, modeType = ModeType.DragonBossDungeon, displayName = "Dragon's Hoard", rewardType = ItemType.LOOT_TICKET, unlockPlayerLevel = 0, rewardBase = 50, rewardScaler = 1 },
        // DungeonTabPage.ZombieHordeUnlockPlayerLevel = 5 (.cctor).
        new DungeonConfig { type = DungeonType.ZombieHorde, modeType = ModeType.ZombieHordeDungeon, displayName = "Zombie Outbreak", rewardType = ItemType.BONE, unlockPlayerLevel = 5, rewardBase = 100, rewardScaler = 2 },
        // GameResources.CultistDungeonUnlockPlayerLevel = 20.
        new DungeonConfig { type = DungeonType.Cultist, modeType = ModeType.CultistDungeon, displayName = "Cultist Ritual", rewardType = ItemType.VIAL, unlockPlayerLevel = 20, rewardBase = 100, rewardScaler = 2 },
    };

    public DungeonConfig GetData(DungeonType type)
    {
        for (int i = 0; i < dungeons.Count; i++)
        {
            if (dungeons[i].type == type)
                return dungeons[i];
        }

        DebugCustom.Log("[StaticDungeonData] Not found=" + type);
        return null;
    }

    public int GetReward(DungeonType type, int level)
    {
        DungeonConfig data = GetData(type);
        return data != null ? data.rewardBase + data.rewardScaler * (level - 1) : 0;
    }

    public static string GetDifficultyText(int level)
    {
        int index = level - 1;
        return (index / LEVELS_PER_DIFFICULTY + 1) + "-" + (index % LEVELS_PER_DIFFICULTY + 1);
    }
}
