using UnityEngine;

// Kết quả tính thưởng offline của 1 lần mở popup (OfflineEarnningPopup.kpd/kpe gốc).
public struct OfflineEarningResult
{
    public int duration;        // giây vắng mặt được tính (đã chặn trần maxSeconds)
    public int maxSeconds;      // trần thời gian tích thưởng
    public int enemies;         // số quái "đã hạ" — chỉ để hiển thị
    public int lootBox;         // LOOT_TICKET nhận được
    public int exp;
    public int expPerHour;
    public int lootBoxPerHour;
}

// Thưởng offline "Combat Summary" (OfflineEarnningPopup + GameplayUI.kjc/kjd gốc, reverse libil2cpp v41).
// Số gốc lấy từ GameResources: MaxOfflineSeconds = 14400, HammerPerMinute = 1; exp mỗi phút = exp thắng 1 màn hiện tại.
// Gốc dùng giờ server (TimeController); project chưa có nên dùng giờ UTC của máy như ShopService.
public static class OfflineEarningService
{
    public const int MAX_OFFLINE_SECONDS = 14400;   // GameResources.MaxOfflineSeconds (4h), nhân Mastery MaxOfflineTime
    public const int HAMMER_PER_MINUTE = 1;         // GameResources.HammerPerMinute
    public const int MIN_SECONDS_TO_SHOW = 60;      // GameplayUI.kjc: vắng dưới 1 phút thì không bật popup
    public const float HEARTBEAT_INTERVAL = 10f;    // GameplayUI.Update: chu kỳ đẩy mốc khi đang chơi
    public const string AD_PLACEMENT = "2x_offline_collect";

    private const float ENEMY_PER_SECOND = 0.15f;   // OfflineEarnningPopup.yxu
    private const float ENEMY_RANDOM_MIN = 0.98f;
    private const float ENEMY_RANDOM_MAX = 1.02f;

    private static UserOfflineEarningData User => GameData.userData.offlineEarning;

    // GameplayUI.kjc: gọi 1 lần lúc vào game. Chưa có mốc (lần đầu) → đặt mốc = bây giờ, không bật popup.
    public static bool ShouldShowOnStart()
    {
        long now = ShopService.Now();
        if (User.collectTime > 0)
        {
            return now - User.collectTime >= MIN_SECONDS_TO_SHOW;
        }

        SetCollectTime(now);
        return false;
    }

    // GameplayUI.kjd: đang chơi (không có popup chờ nhận) → mốc = bây giờ. AutoSaveManager lo ghi save.
    public static void Heartbeat()
    {
        SetCollectTime(ShopService.Now());
    }

    public static OfflineEarningResult Calculate()
    {
        float worth = GetMastery(MasteryUpgradeType.OfflineEarningWorth);
        int expPerMinute = GetExpPerMinute();

        OfflineEarningResult result = new OfflineEarningResult();
        result.maxSeconds = Mathf.RoundToInt(MAX_OFFLINE_SECONDS * GetMastery(MasteryUpgradeType.MaxOfflineTime));
        // Giờ máy bị chỉnh lùi → không tính âm.
        result.duration = (int)System.Math.Max(0, System.Math.Min(ShopService.Now() - User.collectTime, result.maxSeconds));

        int minutes = result.duration / 60;
        result.exp = Mathf.RoundToInt(worth * (expPerMinute * minutes));
        result.lootBox = Mathf.RoundToInt(worth * (HAMMER_PER_MINUTE * minutes));
        result.enemies = Mathf.RoundToInt(Random.Range(ENEMY_RANDOM_MIN, ENEMY_RANDOM_MAX) * ENEMY_PER_SECOND * result.duration);
        result.expPerHour = Mathf.RoundToInt(worth * (expPerMinute * 60));
        result.lootBoxPerHour = Mathf.RoundToInt(worth * (HAMMER_PER_MINUTE * 60));
        return result;
    }

    // OfflineEarnningPopup.kpl: multiplier = 1 (đóng popup) hoặc 2 (xem quảng cáo).
    public static void Collect(OfflineEarningResult result, int multiplier)
    {
        // Giữ lại số giây lẻ chưa đủ 1 phút cho lần sau.
        long now = ShopService.Now();
        long elapsed = now - User.collectTime;
        SetCollectTime(elapsed > 0 ? now - elapsed % 60 : now);

        // Gốc cộng exp offline KHÔNG qua boost exp của Store, dư thì để dành sau khi lên level
        // (ExperienceController.hdq(exp, false, false, true)).
        PlayerExpService.Grant(result.exp * multiplier, applyMultiplier: false, storeOverflow: true);

        GameData.userData.items.Receive(ItemType.LOOT_TICKET, result.lootBox * multiplier);
        GameData.Save(true);
    }

    // Exp mỗi phút = exp thắng 1 màn campaign hiện tại (ExperienceLevelBase + Scaler × User.Level), tối thiểu 1.
    // Đã max playerLevel thì không tích exp nữa (UserController.eci).
    private static int GetExpPerMinute()
    {
        StaticExperienceData experience = GameData.staticData.experience;
        if (GameData.userData.player.playerLevel >= experience.MaxLevel)
        {
            return 0;
        }

        int level = GameData.staticData.campaign.GetLevel(GameData.userData.campaign.stageIdCurrent);
        return Mathf.Max(1, experience.GetStageExp(level));
    }

    // Hệ Mastery tắt thì hệ số = 1 (kpd/kpf gốc).
    private static float GetMastery(MasteryUpgradeType type)
    {
        return MasteryService.IsEnabled ? MasteryService.GetCurrentValue(type) : 1f;
    }

    private static void SetCollectTime(long time)
    {
        User.collectTime = time;
        User.isDataChanged = true;
    }
}
