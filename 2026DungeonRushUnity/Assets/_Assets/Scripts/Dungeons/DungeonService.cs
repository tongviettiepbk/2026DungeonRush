using System;

// Logic hệ Dungeon: reset key/ads theo ngày, sweep, xem ads, thắng màn. Stateless — thao tác trên
// GameData.staticData.dungeons + GameData.userData.dungeons (pattern CompanionService/MasteryService).
public static class DungeonService
{
    private static StaticDungeonData Static => GameData.staticData.dungeons;
    private static UserDungeonData User => GameData.userData.dungeons;

    #region Reset theo ngày

    // Sang NGÀY UTC mới (gốc ev.ehv dựng DateTime Kind=Utc từ unix time rồi so .Date) → dailyKeys = 2,
    // adWatchCount = 0 cho mọi dungeon. Lần đầu (lastResetTime = -1) cũng reset. Trả true nếu vừa reset.
    public static bool CheckDailyReset()
    {
        DateTime now = GetUtcNow();
        if (User.lastResetTime >= 0 && now.Date <= DateTimeOffset.FromUnixTimeSeconds(User.lastResetTime).UtcDateTime.Date)
            return false;

        foreach (DungeonProgress progress in User.dungeons.Values)
        {
            progress.dailyKeys = StaticDungeonData.DAILY_KEYS;
            progress.adWatchCount = 0;
        }

        User.lastResetTime = new DateTimeOffset(now).ToUnixTimeSeconds();
        User.isDataChanged = true;
        GameData.Save();
        return true;
    }

    // Thời gian tới lần reset kế (0h UTC hôm sau) — "Replenishing in" ở tab Dungeon.
    public static TimeSpan GetTimeToReset()
    {
        DateTime now = GetUtcNow();
        return now.Date.AddDays(1) - now;
    }

    private static DateTime GetUtcNow()
    {
        return GameUtils.GetTimeNow().ToUniversalTime();
    }

    #endregion

    public static bool IsUnlocked(DungeonType type)
    {
#if UNITY_EDITOR
        // Editor: mở sẵn Cultist (gốc cần PlayerLevel 20) để test trận.
        if (type == DungeonType.Cultist)
            return true;
#endif
        DungeonConfig data = Static.GetData(type);
        return data != null && GameData.userData.player.playerLevel >= data.unlockPlayerLevel;
    }

    public static DungeonProgress GetProgress(DungeonType type)
    {
        CheckDailyReset();
        return User.Get(type);
    }

    #region Sweep / Ads / Thắng

    // Sweep Last (DungeonPopup.kci): cần ≥ 1 key và đã thắng ít nhất 1 màn.
    public static bool CanSweep(DungeonType type)
    {
        DungeonProgress progress = GetProgress(type);
        return progress.TotalKeys > 0 && progress.level > 1;
    }

    // Tiêu 1 key, nhận thưởng màn vừa qua (level - 1), level giữ nguyên. Trả số tài nguyên nhận (0 = không sweep được).
    public static int Sweep(DungeonType type)
    {
        if (!CanSweep(type))
            return 0;

        DungeonProgress progress = User.Get(type);
        ConsumeKey(progress);
        int amount = GrantReward(type, progress.level - 1);
        GameData.Save(true);
        return amount;
    }

    public static bool CanWatchAd(DungeonType type)
    {
        return GetProgress(type).adWatchCount < StaticDungeonData.MAX_AD_PER_DAY;
    }

    // Xem ads xong (DungeonPopup.kcw): +1 bonus key, +1 lượt ads hôm nay.
    public static void OnAdWatched(DungeonType type)
    {
        DungeonProgress progress = GetProgress(type);
        progress.bonusKeys++;
        progress.adWatchCount++;
        User.isDataChanged = true;
        GameData.Save(true);
    }

    // Thắng màn dungeon (DungeonController.hcd): tiêu 1 key, nhận thưởng màn hiện tại, level + 1.
    // Thua/thoát KHÔNG gọi hàm này (không tiêu key). Mode chơi dungeon sẽ gọi khi có.
    public static int CompleteDungeon(DungeonType type)
    {
        DungeonProgress progress = GetProgress(type);
        ConsumeKey(progress);
        int amount = GrantReward(type, progress.level);
        progress.level++;
        User.isDataChanged = true;
        GameData.Save(true);
        return amount;
    }

    #endregion

    // Tiêu key miễn phí trước, hết mới tới key bonus (UserController.dtv).
    private static void ConsumeKey(DungeonProgress progress)
    {
        if (progress.dailyKeys > 0)
            progress.dailyKeys--;
        else if (progress.bonusKeys > 0)
            progress.bonusKeys--;

        User.isDataChanged = true;
    }

    private static int GrantReward(DungeonType type, int level)
    {
        int amount = Static.GetReward(type, level);
        GameData.userData.items.Receive(Static.GetData(type).rewardType, amount);
        return amount;
    }
}
