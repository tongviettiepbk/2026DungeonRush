using System;
using TMPro;
using UnityEngine;

// Tab Events ở lobby (EventsTabPage gốc): ô PvP + Boss Rush + đếm ngược tới lần hồi vé (0h UTC).
public class UITabEvent : MonoBehaviour
{
    public TMP_Text txtTimeRemain;
    public ElementEventModeUI elementEventPvp;
    public ElementEventModeUI elementEventBossRush;

    private float timerTick;

    private void Awake()
    {
        elementEventPvp.Init(EventModeType.PvP, OnClickEvent);
        elementEventBossRush.Init(EventModeType.BossRush, OnClickEvent);
    }

    private void OnEnable()
    {
        Refresh();
    }

    // Cập nhật đếm ngược mỗi giây; qua 0h UTC thì vé được hồi → vẽ lại các ô.
    private void Update()
    {
        timerTick -= Time.unscaledDeltaTime;
        if (timerTick > 0f)
            return;

        timerTick = 1f;
        if (EventService.CheckDailyReset())
            Refresh();
        else
            UpdateTimeRemain();
    }

    public void Refresh()
    {
        elementEventPvp.Refresh();
        elementEventBossRush.Refresh();
        UpdateTimeRemain();
    }

    // "Replenishing in: 5h 30m" / "30m 23s" (UI.Events.ReplenishIn gốc).
    private void UpdateTimeRemain()
    {
        TimeSpan time = EventService.GetTimeToReset();
        string remain = time.Hours > 0
            ? time.Hours + "h " + time.Minutes + "m"
            : time.Minutes + "m " + time.Seconds + "s";
        txtTimeRemain.text = "Replenishing in: " + remain;
    }

    private void OnClickEvent(ElementEventModeUI element)
    {
        EventConfig data = GameData.staticData.events.GetData(element.Type);
        if (!EventService.IsUnlocked(element.Type))
        {
            UIManager.Instance.ShowToastMessage("Mở ở level " + data.unlockPlayerLevel, isLocalize: false);
            return;
        }

        if (element.Type == EventModeType.BossRush)
        {
            UIManager.Instance.LoadUI(UIKey.BossRushJoinPopup);
            return;
        }

        if (element.Type == EventModeType.PvP)
        {
            UIManager.Instance.LoadUI(UIKey.PvPPopup);
            return;
        }

        UIManager.Instance.ShowToastMessage(data.displayName + " coming soon", isLocalize: false);
    }
}
