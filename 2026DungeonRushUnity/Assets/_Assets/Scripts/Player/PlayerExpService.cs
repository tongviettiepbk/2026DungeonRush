using System;

// Hệ EXP người chơi — port ExperienceController gốc (hdq/hdt/hdu/hds/hdr).
// Luật gốc: exp KHÔNG tự lên level. Thanh exp dừng ở mức đầy (phần dư bị bỏ, trừ nguồn có cờ storeOverflow) →
// trạng thái "chờ lên level" (wqw): nút Level ở thanh exp sáng xanh + chấm đỏ, LevelPopup tự bật khi đang ở màn
// chính. Người chơi bấm "Complete!" trong LevelPopup mới lên level (CompleteLevelUp) + nhận gem bảng LevelPopup.ywc.
// Không tự Save — nơi gọi lo việc lưu (trừ CompleteLevelUp: bấm trong popup nên tự lưu).
public static class PlayerExpService
{
    // ExperienceController.wqt: vừa đầy thanh exp (chờ bấm lên level).
    public static event Action LevelUpReady;
    // ExperienceController.wqu: vừa lên level xong (bấm Complete trong popup).
    public static event Action LevelUpCompleted;
    // ExperienceController.wqv(amount, accumulate): số exp thật sự cộng vào thanh — ExperienceUI hiện "(+N)".
    public static event Action<int, bool> ExperienceEarned;
    // UserController.PlayerExperienceChanged / PlayerLevelChanged.
    public static event Action ExperienceChanged;
    public static event Action LevelChanged;

    // wqx gốc: exp dư (nguồn storeOverflow) để dành, cộng tiếp sau khi lên level. Không lưu save (gốc cũng vậy).
    private static int storedOverflow;
    // wrd gốc: đầy thanh khi đang mở tab khác → chờ về màn chính mới tự bật LevelPopup.
    private static bool isAutoOpenPending;

    private static UserPlayerData Player => GameData.userData.player;
    private static StaticExperienceData Config => GameData.staticData.experience;

    // UserController.eci: đã max level.
    public static bool IsMaxLevel => Player.playerLevel >= Config.MaxLevel;

    // wqw gốc (hdj tính lúc vào game: chưa max và exp >= ngưỡng). Exp luôn bị chặn ở ngưỡng nên tính trực tiếp.
    public static bool IsLevelUpReady => !IsMaxLevel && Player.playerExperience >= GetXpRequired();

    // hdk(PlayerLevel).
    public static int GetXpRequired() => Config.GetXpRequired(Player.playerLevel);

    // hdl gốc: % thanh exp (max level = đầy).
    public static float GetProgress()
    {
        if (IsMaxLevel) return 1f;
        int need = GetXpRequired();
        if (need < 1) return 1f;
        float progress = (float)Player.playerExperience / need;
        return progress < 0f ? 0f : Math.Min(progress, 1f);
    }

    // hdq gốc. accumulateDisplay: cộng dồn vào số "(+N)" đang hiện; applyMultiplier: nhân boost exp
    // (lifetime ×2, quảng cáo, X2 ×1.5, X5 ×2 — ShopService.GetExpMultiplier); storeOverflow: giữ phần dư khi đầy.
    // Gốc: mảnh exp campaign (true, true, false), bán đồ (_, true, false), offline/phần thưởng (false, false, true).
    public static void Grant(int amount, bool accumulateDisplay = false, bool applyMultiplier = true, bool storeOverflow = false)
    {
        if (IsMaxLevel) return;

        if (applyMultiplier)
            amount = ShopService.ApplyExpBoost(amount);

        UserPlayerData player = Player;
        int cur = player.playerExperience;
        int need = GetXpRequired();
        int added = Math.Min(amount, need - cur);
        player.playerExperience = cur + added;
        player.isDataChanged = true;
        ExperienceChanged?.Invoke();

        if (storeOverflow && amount - added >= 1)
            storedOverflow += amount - added;

        if (added >= 1)
            ExperienceEarned?.Invoke(added, accumulateDisplay);

        if (cur + added >= need)
        {
            isAutoOpenPending = true;
            LevelUpReady?.Invoke();
            if (IsHomeScreen())
                OpenLevelPopup();
        }
    }

    // hdu + hdt gốc (LevelPopup.kmt gọi khi bấm Complete): lên 1 level, exp về 0, rồi cộng phần dư để dành.
    public static void CompleteLevelUp()
    {
        if (!IsLevelUpReady) return;

        UserPlayerData player = Player;
        player.playerLevel = Math.Min(player.playerLevel + 1, Config.MaxLevel);
        player.playerExperience = 0;
        player.isDataChanged = true;
        isAutoOpenPending = false;
        // Clan War: điểm "Level Up" = New Level × 100 (ClanWarController.fzo gốc).
        ClanWarController.Instance.RecordLevelUp(player.playerLevel);
        LevelChanged?.Invoke();
        ExperienceChanged?.Invoke();
        LevelUpCompleted?.Invoke();

        ApplyStoredOverflow();
        GameData.Save(true);
    }

    // hdt gốc: phần dư để dành đổ vào level mới (vẫn chặn ở ngưỡng, dư nữa thì để dành tiếp).
    private static void ApplyStoredOverflow()
    {
        if (storedOverflow < 1) return;
        if (IsMaxLevel)
        {
            storedOverflow = 0;
            return;
        }

        int need = GetXpRequired();
        int added = Math.Min(storedOverflow, need);
        storedOverflow -= added;
        Player.playerExperience = added;
        Player.isDataChanged = true;
        ExperienceChanged?.Invoke();
        if (added >= need)
        {
            isAutoOpenPending = true;
            LevelUpReady?.Invoke();
        }
    }

    // OnLevelButtonClicked / hds gốc: mở LevelPopup (nút Level, cả lúc chưa đầy để xem bảng rarity).
    public static void OpenLevelPopup()
    {
        isAutoOpenPending = false;
        if (UIManager.Instance.GetActiveUI(UIKey.LevelPopup) != null) return;
        UILevelPopup popup = UIManager.Instance.LoadUI(UIKey.LevelPopup) as UILevelPopup;
        if (popup != null) popup.Show();
    }

    // hdr gốc: lobby vừa đóng hết tab (về màn chính) mà đang chờ → tự bật LevelPopup.
    public static void OnReturnHomeScreen()
    {
        if (isAutoOpenPending && IsLevelUpReady)
            OpenLevelPopup();
    }

    // TabBarUI.kvn gốc: không trang tab nào đang mở.
    private static bool IsHomeScreen()
    {
        UIMainLobby lobby = GameController.Instance != null ? GameController.Instance.uiLobby : null;
        return lobby != null && lobby.IsHomeScreen();
    }
}
