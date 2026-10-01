using System;
using TMPro;
using UnityEngine.UI;

// HUD trong trận dungeon (DungeonUI gốc): tên dungeon + level, nút Exit ("Exit does not consume keys").
public class UIDungeonHud : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtLevel;
    public Button btExit;

    private Action onExit;

    protected override void Awake()
    {
        base.Awake();
        btExit.onClick.AddListener(OnClickExit);
    }

    public void Show(DungeonType type, int level, Action onExit)
    {
        this.onExit = onExit;

        DungeonConfig data = GameData.staticData.dungeons.GetData(type);
        txtTitle.text = data != null ? data.displayName : type.ToString();
        txtLevel.text = StaticDungeonData.GetDifficultyText(level);
    }

    private void OnClickExit()
    {
        onExit?.Invoke();
    }
}
