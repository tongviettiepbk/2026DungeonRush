using DG.Tweening;

// Rate Us — port UserController.dox/dpg + ForgeController.hex + DungeonController.hcd + PopupController.dgi gốc.
// Doc: DecodedData/RATE_US_MODEL.md.
//  - Chỉ hiện khi: chưa Submit, chưa hiện trong phiên này, đã hiện < 3 lần.
//  - Trigger: lần đầu mặc đồ Rare (xanh lá) / Epic (tím) từ forge, lần đầu thắng Dragon dungeon.
//  - Đang dungeon / PvP / Boss Rush thì không hiện (cờ trigger vẫn bị đánh dấu như gốc).
//  - Qua được điều kiện: +1 lần hiện (lưu ngay), chờ 2s mới mở popup.
public static class RateUsService
{
    public const int MAX_SHOW_COUNT = 3;
    public const float SHOW_DELAY = 2f;
    public const int STORE_MIN_STARS = 4;

    // UserController.ugc gốc — không lưu, chỉ trong phiên.
    private static bool isShownThisSession;

    private static UserPlayerData Player => GameData.userData.player;

    // dox gốc.
    public static bool CanShow()
    {
        return Player.isRateUsSubmitted == false && isShownThisSession == false && Player.rateUsShowCount < MAX_SHOW_COUNT;
    }

    // ForgeController.hex gốc: gọi sau khi mặc món vừa forge.
    public static void OnItemEquipped(Rarity rarity)
    {
        if (CanShow() == false)
            return;

        if (rarity == Rarity.Rare && Player.isRateUsFirstGreenEquipShown == false)
        {
            Player.isRateUsFirstGreenEquipShown = true;
            TryShow();
        }
        else if (rarity == Rarity.Epic && Player.isRateUsFirstPurpleEquipShown == false)
        {
            Player.isRateUsFirstPurpleEquipShown = true;
            TryShow();
        }
    }

    // DungeonController.hcd gốc: thắng dungeon Dragon lần đầu. Lúc này vẫn đang ở mode dungeon nên TryShow
    // dừng lại — gốc cũng vậy (save gốc: cờ Dragon = true mà số lần hiện không tăng).
    public static void OnDungeonWin(DungeonType type)
    {
        if (type != DungeonType.DragonBoss || Player.isRateUsFirstDragonWinShown || CanShow() == false)
            return;

        Player.isRateUsFirstDragonWinShown = true;
        TryShow();
    }

    // UserController.dou gốc: bấm Submit khi đã chọn sao.
    public static void MarkSubmitted()
    {
        Player.isRateUsSubmitted = true;
        Player.isDataChanged = true;
        GameData.Save();
    }

    // dpg gốc.
    private static void TryShow()
    {
        Player.isDataChanged = true;
        if (CanShow() == false || IsInSpecialMode(true))
        {
            GameData.Save();
            return;
        }

        isShownThisSession = true;
        Player.rateUsShowCount++;
        GameData.Save();
        DOVirtual.DelayedCall(SHOW_DELAY, Show);
    }

    // PopupController.dgi gốc: tới lúc mở mà đang PvP / Boss Rush thì bỏ.
    private static void Show()
    {
        if (IsInSpecialMode(false))
            return;

        UIRateUsPopup popup = UIManager.Instance.LoadUI(UIKey.RateUsPopup) as UIRateUsPopup;
        if (popup != null)
            popup.Show();
    }

    // Cờ GameController gốc: wtp = dungeon, wtr/wts = PvP, wue = Boss Rush.
    private static bool IsInSpecialMode(bool includeDungeon)
    {
        switch (GameController.Instance.modeType)
        {
            case ModeType.PvP:
            case ModeType.BossRush:
                return true;
            case ModeType.DragonBossDungeon:
            case ModeType.ZombieHordeDungeon:
            case ModeType.CultistDungeon:
                return includeDungeon;
            default:
                return false;
        }
    }
}
