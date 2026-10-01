using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô dungeon trong tab Dungeon (DungeonTabPage gốc): khoá theo playerLevel, số key, chấm báo còn key.
public class ElementDungeonUI : MonoBehaviour
{
    public GameObject objLock;
    public GameObject objNotice;
    public TMP_Text txtQuantityProcess;
    public Button btRequestJoin;

    public DungeonType Type { get; private set; }

    public void Init(DungeonType type, Action<ElementDungeonUI> onClick)
    {
        Type = type;
        btRequestJoin.onClick.RemoveAllListeners();
        btRequestJoin.onClick.AddListener(() => onClick(this));
    }

    public void Refresh()
    {
        bool isUnlocked = DungeonService.IsUnlocked(Type);
        DungeonProgress progress = DungeonService.GetProgress(Type);

        objLock.SetActive(!isUnlocked);
        objNotice.SetActive(isUnlocked && progress.TotalKeys > 0);
        txtQuantityProcess.text = progress.TotalKeys + "/" + StaticDungeonData.DAILY_KEYS;
    }
}
