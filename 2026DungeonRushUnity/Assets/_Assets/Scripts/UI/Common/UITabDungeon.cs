using System;
using TMPro;
using UnityEngine;

// Tab Dungeon ở lobby (DungeonTabPage gốc): 3 ô dungeon + đếm ngược tới lần hồi key (0h UTC).
// Bấm ô đã mở → popup UIDungeonPopup của dungeon đó.
public class UITabDungeon : MonoBehaviour
{
    public TMP_Text txtTimeRemain;
    public ElementDungeonUI dragonHoard;
    public ElementDungeonUI zombieOutBreak;
    public ElementDungeonUI cultistDungeon;

    private float timerTick;

    private void Awake()
    {
        dragonHoard.Init(DungeonType.DragonBoss, OnClickDungeon);
        zombieOutBreak.Init(DungeonType.ZombieHorde, OnClickDungeon);
        cultistDungeon.Init(DungeonType.Cultist, OnClickDungeon);
    }

    private void OnEnable()
    {
        Refresh();
    }

    // Cập nhật đếm ngược mỗi giây; qua 0h UTC thì key được hồi → vẽ lại các ô.
    private void Update()
    {
        timerTick -= Time.unscaledDeltaTime;
        if (timerTick > 0f)
            return;

        timerTick = 1f;
        if (DungeonService.CheckDailyReset())
            Refresh();
        else
            UpdateTimeRemain();
    }

    public void Refresh()
    {
        dragonHoard.Refresh();
        zombieOutBreak.Refresh();
        cultistDungeon.Refresh();
        UpdateTimeRemain();
    }

    // "Replenishing in: 5h 30m" / "30m 23s" (UI.Dungeon.ReplenishingIn gốc).
    private void UpdateTimeRemain()
    {
        TimeSpan time = DungeonService.GetTimeToReset();
        string remain = time.Hours > 0
            ? time.Hours + "h " + time.Minutes + "m"
            : time.Minutes + "m " + time.Seconds + "s";
        txtTimeRemain.text = "Replenishing in: " + remain;
    }

    private void OnClickDungeon(ElementDungeonUI element)
    {
        if (!DungeonService.IsUnlocked(element.Type))
        {
            int level = GameData.staticData.dungeons.GetData(element.Type).unlockPlayerLevel;
            UIManager.Instance.ShowToastMessage("Mở ở level " + level, isLocalize: false);
            return;
        }

        UIDungeonPopupStart popup = UIManager.Instance.LoadUI(UIKey.DungeonPopup) as UIDungeonPopupStart;
        if (popup != null)
            popup.Show(element.Type, Refresh);
    }
}
