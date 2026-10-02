using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô event trong tab Events (EventsTabPage gốc): khoá theo playerLevel, số vé, chấm báo còn vé.
public class ElementEventModeUI : MonoBehaviour
{
    public TMP_Text txtQuantityStick;   // KeyCountText — spriteAsset là icon vé của event (IsTicket/IsBossTicket)
    public GameObject objLock;          // LockOverlay (chứa LockImage)
    public GameObject objNotice;
    public Button btJoin;

    public EventModeType Type { get; private set; }

    public void Init(EventModeType type, Action<ElementEventModeUI> onClick)
    {
        Type = type;
        btJoin.onClick.RemoveAllListeners();
        btJoin.onClick.AddListener(() => onClick(this));
    }

    // Gốc EventsTabPage.ker: khoá → bật lock, ẩn text vé.
    public void Refresh()
    {
        bool isUnlocked = EventService.IsUnlocked(Type);
        EventTickets tickets = EventService.GetTickets(Type);

        objLock.SetActive(!isUnlocked);
        objNotice.SetActive(isUnlocked && tickets.TotalTickets > 0);
        txtQuantityStick.gameObject.SetActive(isUnlocked);
        txtQuantityStick.text = "<sprite=0>" + tickets.TotalTickets + "/" + GameData.staticData.events.GetData(Type).dailyFreeTickets;
    }
}
