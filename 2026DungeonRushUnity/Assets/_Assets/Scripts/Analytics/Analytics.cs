using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Cửa DUY NHẤT bắn event gameplay lên Firebase Analytics. Kế hoạch + từ điển event ở GDD/analytics/TRACKING_PLAN.md và
// events.csv: thêm event mới thì thêm dòng CSV trước rồi mới gọi Log.
//
// Port từ 2026RaceManager (Scripts/Analytics/Analytics.cs), giữ 3 ý chính:
// - Mọi event tự gắn ngữ cảnh tiến trình (plv, stage, day_n, session_n, payer), chỗ gọi không phải nhớ.
// - Không log nhịp nhanh: quặng/búa/vé gom theo phiên, loot gom theo lượt (Tick). Chỉ GEM bắn từng lượt.
// - Lý do tiền vào/ra đi bằng ambient scope: using var _ = Analytics.Scope("mastery"); thiếu scope thì ghi "unknown".
//
// Editor không gửi lên Firebase, chỉ in Console để soi tham số. Hàm ở đây không bao giờ ném lỗi ra luồng game.
public static class Analytics
{
    private const double DAY_SECONDS = 86400d;
    private const float LOOT_IDLE_FLUSH_SECONDS = 3f;
    private const float LOOT_MAX_AGE_SECONDS = 60f;

    private const string PREF_INSTALL_DAY = "an_install_day";
    private const string PREF_FIRST_VERSION = "an_first_version";
    private const string PREF_SESSION_N = "an_session_n";
    private const string PREF_LAST_DAY = "an_last_day";
    private const string PREF_DAYS_PLAYED = "an_days_played";

    public const string REASON_UNKNOWN = "unknown";

    // Save đã nạp xong: trước đó chưa có tiến trình để gắn ngữ cảnh, cộng tiền lúc tạo user mới cũng không tính.
    private static bool isUserLoaded;

    public static void MarkUserLoaded()
    {
        isUserLoaded = true;
    }

    #region Bắn event
    // Giá trị: chuỗi, số nguyên/bool/enum (thành long), số thực (thành double). null thì bỏ tham số đó.
    public static void Log(string name, params (string key, object value)[] kv)
    {
        try
        {
            List<(string key, object value)> list = new List<(string key, object value)>(kv.Length + 5);

            for (int i = 0; i < kv.Length; i++)
            {
                if (kv[i].value != null)
                {
                    list.Add(kv[i]);
                }
            }

            AppendContext(list);
            DebugCustom.Log("[Analytics] " + name + " " + Dump(list) + " · " + Reason);
            FirebaseServices.LogEvent(name, list);
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[Analytics] " + name + ": " + e);
        }
    }

    private static string Dump(List<(string key, object value)> list)
    {
        StringBuilder sb = new StringBuilder("{");

        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(list[i].key).Append('=').Append(list[i].value);
        }

        return sb.Append('}').ToString();
    }

    // Bậc log10 của một số (0 thì -1): power vượt xa khoảng đọc được nên vẽ histogram theo bậc.
    public static int Bucket(double amount)
    {
        return amount > 0d ? (int)Math.Floor(Math.Log10(amount)) : -1;
    }
    #endregion

    #region Ngữ cảnh tiến trình
    // Event tự khai trùng tên thì thắng ngữ cảnh: GA4 không nhận 2 tham số cùng tên.
    private static void AppendContext(List<(string key, object value)> list)
    {
        AppendIfMissing(list, "plv", isUserLoaded ? GameData.userData.player.playerLevel : -1);
        AppendIfMissing(list, "stage", HighestStage());
        AppendIfMissing(list, "day_n", MonetizeStats.AccountDay());
        AppendIfMissing(list, "session_n", SessionNumber);
        AppendIfMissing(list, "payer", MonetizeStats.IsPayer ? 1 : 0);
    }

    private static void AppendIfMissing(List<(string key, object value)> list, string key, object value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].key == key)
            {
                return;
            }
        }

        list.Add((key, value));
    }

    // Level campaign cao nhất đã qua, 0 = chưa qua màn nào.
    public static int HighestStage()
    {
        if (isUserLoaded == false || GameData.userData.campaign.passedStageId <= 0)
        {
            return 0;
        }

        return GameData.staticData.campaign.GetLevel(GameData.userData.campaign.passedStageId);
    }

    public static int SessionNumber { get; private set; }
    #endregion

    #region Lý do tiền vào / ra
    public static string Reason { get; private set; } = REASON_UNKNOWN;

    public readonly struct ReasonScope : IDisposable
    {
        private readonly string previous;

        public ReasonScope(string reason)
        {
            previous = Reason;
            Reason = reason;
        }

        public void Dispose()
        {
            Reason = previous;
        }
    }

    // Lồng nhau thì khối trong thắng, thoát khối trả lại lý do cũ.
    public static ReasonScope Scope(string reason)
    {
        return new ReasonScope(reason);
    }
    #endregion

    #region Ví
    private static readonly Dictionary<string, double> walletBatch = new Dictionary<string, double>();

    // Gọi từ UserItemData cho mọi lượt Receive/Consume. SetQuantity thẳng (nạp save) không đi qua đây, đúng ý.
    public static void OnWalletChange(ItemType type, double delta)
    {
        if (delta == 0d || isUserLoaded == false)
        {
            return;
        }

        string currency = type.ToString().ToLowerInvariant();

        if (type == ItemType.GEM)
        {
            LogWallet(delta > 0d, currency, Math.Abs(delta), Reason);

            if (delta < 0d)
            {
                Milestone("first_gem_spend");
            }

            return;
        }

        // Búa, quặng, cuốc... đổi liên tục: gom theo phiên, bắn từng lượt là đầy trần 1 triệu event/ngày của BigQuery.
        string key = (delta > 0d ? "1|" : "0|") + currency + "|" + Reason;
        walletBatch.TryGetValue(key, out double sum);
        walletBatch[key] = sum + Math.Abs(delta);
    }

    private static void LogWallet(bool isEarn, string currency, double amount, string reason)
    {
        if (isEarn)
        {
            Log("earn_virtual_currency", ("virtual_currency_name", currency), ("value", amount), ("source", reason));
        }
        else
        {
            Log("spend_virtual_currency", ("virtual_currency_name", currency), ("value", amount), ("item_name", reason));
        }
    }

    private static void FlushWallet()
    {
        foreach (KeyValuePair<string, double> pair in walletBatch)
        {
            string[] parts = pair.Key.Split('|');
            LogWallet(parts[0] == "1", parts[1], pair.Value, parts[2]);
        }

        walletBatch.Clear();
    }

    private static void LogWalletSnapshot()
    {
        UserItemData items = GameData.userData.items;

        Log("wallet_snapshot",
            ("gem", items.GetQuantityHave(ItemType.GEM)),
            ("hammer", items.GetQuantityHave(ItemType.LOOT_TICKET)),
            ("bone", items.GetQuantityHave(ItemType.BONE)),
            ("vial", items.GetQuantityHave(ItemType.VIAL)),
            ("cloak", items.GetQuantityHave(ItemType.CLOAK)),
            ("pickaxe", items.GetQuantityHave(ItemType.PICKAXE)));
    }
    #endregion

    #region Mốc "lần đầu"
    // Bắn ftue_milestone đúng 1 lần mỗi tài khoản, đánh dấu trong save nên cài lại máy không bắn lại.
    public static void Milestone(string key)
    {
        if (UaFunnel.TryMark("ms_" + key))
        {
            Log("ftue_milestone", ("milestone", key), ("sec_since_start", MonetizeStats.SecondsSinceStart()));
        }
    }

    public static void LogTutorialStep(FtueStep step)
    {
        Log("tutorial_step", ("step", step.ToString()), ("step_id", (int)step), ("sec_since_start", MonetizeStats.SecondsSinceStart()));
    }
    #endregion

    #region Boot
    private static float bootStepAt;

    public static void BootStep(string step)
    {
        Log("boot_step", ("step", step), ("sec", Math.Round(Time.realtimeSinceStartup - bootStepAt, 2)));
        bootStepAt = Time.realtimeSinceStartup;
    }

    public static void LogBootComplete()
    {
        Log("boot_complete", ("sec_total", Math.Round(Time.realtimeSinceStartup, 2)));
    }
    #endregion

    #region Phiên
    private static float sessionStartRealtime;
    private static float pauseRealtime = -1f;
    private static bool inSession;
    private static int screensOpened;
    private static int matchesPlayed;
    private static int adsWatched;
    private static string lastScreen = string.Empty;

    // Gọi khi vào scene MainGame và khi app quay lại từ nền.
    public static void BeginSession(bool isResume)
    {
        if (inSession || isUserLoaded == false)
        {
            return;
        }

        EnsureInstallMarks();

        inSession = true;
        sessionStartRealtime = Time.realtimeSinceStartup;
        screensOpened = 0;
        matchesPlayed = 0;
        adsWatched = 0;

        SessionNumber = PlayerPrefs.GetInt(PREF_SESSION_N, 0) + 1;
        PlayerPrefs.SetInt(PREF_SESSION_N, SessionNumber);

        string today = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string lastDay = PlayerPrefs.GetString(PREF_LAST_DAY, string.Empty);
        int daysSinceLast = 0;

        if (lastDay != today)
        {
            if (DateTime.TryParseExact(lastDay, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime last))
            {
                daysSinceLast = (int)(DateTime.UtcNow.Date - last).TotalDays;
            }

            PlayerPrefs.SetString(PREF_LAST_DAY, today);
            PlayerPrefs.SetInt(PREF_DAYS_PLAYED, PlayerPrefs.GetInt(PREF_DAYS_PLAYED, 0) + 1);
        }

        double offlineSec = isResume && pauseRealtime >= 0f ? Time.realtimeSinceStartup - pauseRealtime : 0d;

        Log("session_begin",
            ("resume", isResume),
            ("offline_sec", Math.Round(offlineSec)),
            ("days_since_last", daysSinceLast),
            ("from_push", LocalPushController.ConsumeOpenedId()),
            ("version", Application.version));

        LogWalletSnapshot();
        UaFunnel.OnSessionBegin();
        RefreshUserProperties();
    }

    // Gọi khi app xuống nền / thoát: xả phần đang gom rồi bắn session_end.
    public static void EndSession()
    {
        if (inSession == false)
        {
            return;
        }

        inSession = false;
        pauseRealtime = Time.realtimeSinceStartup;

        FlushLoot();
        FlushWallet();

        Log("session_end",
            ("duration_sec", Math.Round(Time.realtimeSinceStartup - sessionStartRealtime)),
            ("screens_opened", screensOpened),
            ("matches", matchesPlayed),
            ("ads_watched", adsWatched),
            ("last_screen", lastScreen.Length > 0 ? lastScreen : "none"));
    }

    private static void EnsureInstallMarks()
    {
        if (PlayerPrefs.HasKey(PREF_INSTALL_DAY))
        {
            return;
        }

        PlayerPrefs.SetString(PREF_INSTALL_DAY, DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(PREF_FIRST_VERSION, Application.version);
    }

    // Nhịp 1 giây từ AnalyticsTracker.
    public static void Tick()
    {
        if (lootCount > 0 && (Time.realtimeSinceStartup - lootLastAt >= LOOT_IDLE_FLUSH_SECONDS || Time.realtimeSinceStartup - lootFirstAt >= LOOT_MAX_AGE_SECONDS))
        {
            FlushLoot();
        }
    }
    #endregion

    #region Màn hình & paywall
    public const string SCREEN_SHOP_TAB = "tab_shop";

    // Popup bán hàng: mở là paywall_view, đóng là paywall_close. Tab Shop tính như paywall do người chơi tự vào.
    private static readonly HashSet<string> paywallKeys = new HashSet<string>
    {
        UIKey.OfferPopup, UIKey.ChestBundleOfferPopup, UIKey.MiningOfferPopup, UIKey.BattlePassPopup, SCREEN_SHOP_TAB,
        UIKey.StarterPackPopup, UIKey.MissingGemsPopup,
    };

    private static readonly Dictionary<string, float> screenOpenedAt = new Dictionary<string, float>();
    private static float lastIapClickRealtime = -1f;
    private static string lastIapPopup = string.Empty;

    public const string TRIGGER_MANUAL = "manual";
    public const string TRIGGER_AUTO = "auto";
    public const string TRIGGER_MISSING_GEMS = "missing_gems";

    // Đặt ngay trước khi game tự mở popup bán hàng, trả về manual ngay sau đó (LoadUI chạy đồng bộ).
    public static string PaywallTrigger { get; set; } = TRIGGER_MANUAL;

    public static void OnScreenOpen(string key)
    {
        // LoadUI gọi lại cho UI đang mở thì không tính lượt mới.
        if (screenOpenedAt.ContainsKey(key))
        {
            return;
        }

        screenOpenedAt[key] = Time.realtimeSinceStartup;
        screensOpened++;

        Log("screen_view", ("screen_name", key), ("from", lastScreen));
        lastScreen = key;

        if (paywallKeys.Contains(key))
        {
            Log("paywall_view", ("popup", key), ("trigger", PaywallTrigger));
            Milestone("first_paywall_seen");
        }
    }

    public static void OnScreenClose(string key)
    {
        bool known = screenOpenedAt.TryGetValue(key, out float openedAt);
        screenOpenedAt.Remove(key);
        double sec = known ? Math.Round(Time.realtimeSinceStartup - openedAt, 1) : 0d;

        Log("screen_close", ("screen_name", key), ("sec", sec));

        if (paywallKeys.Contains(key))
        {
            bool bought = known && lastIapClickRealtime >= openedAt;
            Log("paywall_close", ("popup", key), ("sec_open", sec), ("action", bought ? "buy" : "close"));
        }
    }
    #endregion

    #region IAP
    public static void LogIapClick(string productId)
    {
        lastIapClickRealtime = Time.realtimeSinceStartup;
        lastIapPopup = lastScreen;

        Log("iap_click", ("product_id", productId), ("price_usd", StaticShopData.GetPriceUsd(productId)), ("popup", lastIapPopup));
    }

    public static void LogIapFail(string productId, string reason)
    {
        Log("iap_fail", ("product_id", productId), ("reason", reason), ("popup", lastIapPopup));
    }

    // Đơn đã thu tiền VÀ trao hàng. Doanh thu đọc từ in_app_purchase (Firebase tự thu, đã loại đơn test): event này chỉ
    // mang ngữ cảnh mua, cố ý không đặt tên purchase và không có value + currency, nếu không doanh thu bị cộng 2 lần.
    // Gọi sau MonetizeStats.RecordPurchase để nth_purchase đã tính đơn này.
    public static void LogIapSuccess(string productId, double localPrice, string currency, string transactionId)
    {
        double priceUsd = StaticShopData.GetPriceUsd(productId);

        Log("iap_success",
            ("product_id", productId),
            ("transaction_id", transactionId ?? string.Empty),
            ("price_local", localPrice),
            ("currency_local", currency ?? string.Empty),
            ("price_usd", priceUsd),
            ("popup", lastIapPopup),
            ("nth_purchase", MonetizeStats.PurchaseCount),
            ("iap_usd_total", Math.Round(MonetizeStats.IapUsdTotal, 2)));

        UaFunnel.OnPurchase(productId, priceUsd);
        RefreshUserProperties();
    }

    // Người chơi bấm mua bằng gem mà thiếu, ngay trước khi chào gói gem (UIMissingGemsPopup).
    public static void LogGemShort(string sink)
    {
        Log("gem_short", ("sink", sink), ("have", GameData.userData.items.GetQuantityHave(ItemType.GEM)));
    }
    #endregion

    #region Quảng cáo
    public static void LogAdRequest(string placement, bool ready)
    {
        Log("ad_rw_request", ("placement", placement), ("ready", ready));
    }

    // result: finished / skipped / failed / skipped_noads.
    public static void LogAdResult(string placement, string result)
    {
        Log("ad_rw_result", ("placement", placement), ("result", result));

        if (result == "finished")
        {
            adsWatched++;
            MonetizeStats.RecordRewarded();
            UaFunnel.OnAdFinished();
        }
    }

    // Người chơi đã mua No Ads: chỗ gọi trao thưởng ngay, không qua SDK quảng cáo.
    public static void LogAdSkippedNoAds(string placement)
    {
        Log("ad_rw_result", ("placement", placement), ("result", "skipped_noads"));
    }
    #endregion

    #region Trận
    public static void LogMatchEnd(BaseMode mode, string result, double seconds, double heroHpRate)
    {
        matchesPlayed++;

        Log("match_end",
            ("mode", mode.type.ToString().ToLowerInvariant()),
            ("level", mode.AnalyticsLevel),
            ("result", result),
            ("sec", Math.Round(seconds, 1)),
            ("hero_hp", Math.Round(heroHpRate, 2)),
            ("pets", GameData.userData.companions.equipped.Count),
            ("power_b", Bucket(PlayerPower.GetCurrent())));

        if (result != "win")
        {
            return;
        }

        if (mode.type == ModeType.DefaultLevel)
        {
            UaFunnel.OnStageWon(mode.AnalyticsLevel);
        }
        else if (mode is DungeonMode)
        {
            UaFunnel.Step(UaFunnel.DUNGEON_1);
        }
    }

    public static void LogLevelUp(int level)
    {
        Log("level_up", ("level", level), ("sec_since_start", MonetizeStats.SecondsSinceStart()));
        UaFunnel.OnLevelUp(level);
        RefreshUserProperties();
    }
    #endregion

    #region Loot: gom theo lượt
    private static int lootCount;
    private static int lootSold;
    private static int lootBestRarity = -1;
    private static bool lootIsAuto;
    private static float lootFirstAt;
    private static float lootLastAt;

    // Một món vừa loot ra. Auto Loot chạy liên tục nên gom lại, im 3 giây hoặc đủ 60 giây thì bắn.
    public static void NoteLoot(Rarity rarity, bool isAuto)
    {
        if (lootCount == 0)
        {
            lootFirstAt = Time.realtimeSinceStartup;
            lootIsAuto = isAuto;
        }
        else if (lootIsAuto != isAuto)
        {
            FlushLoot();
            lootFirstAt = Time.realtimeSinceStartup;
            lootIsAuto = isAuto;
        }

        lootCount++;
        lootLastAt = Time.realtimeSinceStartup;
        lootBestRarity = Math.Max(lootBestRarity, (int)rarity);
        Milestone("first_rarity_" + rarity.ToString().ToLowerInvariant());
        UaFunnel.Step(UaFunnel.LOOT_1);
    }

    public static void NoteLootSold()
    {
        lootSold++;
    }

    private static void FlushLoot()
    {
        if (lootCount == 0)
        {
            return;
        }

        Log("loot_batch",
            ("auto", lootIsAuto),
            ("items", lootCount),
            ("sold", lootSold),
            ("best_rarity", ((Rarity)lootBestRarity).ToString().ToLowerInvariant()));

        lootCount = 0;
        lootSold = 0;
        lootBestRarity = -1;
    }
    #endregion

    #region User property
    // Đặt lại toàn bộ: lúc vào phiên, sau mua, lên level, đổi clan.
    public static void RefreshUserProperties()
    {
        if (isUserLoaded == false)
        {
            return;
        }

        try
        {
            UserData user = GameData.userData;
            int purchases = MonetizeStats.PurchaseCount;

            SetUserProperty("payer_tier", purchases == 0 ? "none" : purchases == 1 ? "1" : purchases <= 5 ? "2-5" : "6+");
            SetUserProperty("iap_tier", MonetizeStats.IapTier());
            SetUserProperty("rw_tier", MonetizeStats.RewardedTier());
            SetUserProperty("segment", MonetizeStats.Segment());
            SetUserProperty("install_day", PlayerPrefs.GetString(PREF_INSTALL_DAY, string.Empty));
            SetUserProperty("first_version", PlayerPrefs.GetString(PREF_FIRST_VERSION, string.Empty));
            SetUserProperty("days_played", PlayerPrefs.GetInt(PREF_DAYS_PLAYED, 0).ToString(CultureInfo.InvariantCulture));
            SetUserProperty("plv_bucket", (user.player.playerLevel / 5 * 5).ToString(CultureInfo.InvariantCulture));
            SetUserProperty("stage_bucket", (HighestStage() / 10 * 10).ToString(CultureInfo.InvariantCulture));
            SetUserProperty("pets_owned", user.companions.owned.Count.ToString(CultureInfo.InvariantCulture));
            SetUserProperty("in_clan", user.clan.HasClan ? "1" : "0");
            SetUserProperty("no_ads", user.shop.isNoAdsBought ? "1" : "0");
            SetUserProperty("battle_pass", user.shop.isPremiumBattlePassBought ? "1" : "0");
            SetUserProperty("lifetime_boost", user.shop.isLifetimeBoostBought ? "1" : "0");
            SetUserProperty("auth_type", FirebaseManager.Instance.IsAnonymous ? "guest" : "email");

            if (AppsflyerAttribution.Status.Length > 0)
            {
                SetUserProperty("af_status", AppsflyerAttribution.Status);
                SetUserProperty("af_media_source", AppsflyerAttribution.MediaSource);
                SetUserProperty("af_campaign", AppsflyerAttribution.Campaign);
            }
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[Analytics] user properties: " + e);
        }
    }

    public static void SetUserProperty(string name, string value)
    {
        FirebaseServices.SetUserProperty(name, value);
    }
    #endregion
}
