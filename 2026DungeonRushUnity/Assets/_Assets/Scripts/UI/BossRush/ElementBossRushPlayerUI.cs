using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng trong danh sách nhóm (BossRushPlayerElementUI gốc): hạng, tên, damage (TotalDamagePoints), power; dòng của mình highlight.
public class ElementBossRushPlayerUI : MonoBehaviour
{
    public TMP_Text txtPosition;
    public TMP_Text txtPlayerName;
    public TMP_Text txtDamagePoints;
    public TMP_Text txtPower;
    public Image imgBackground;
    public Button btRow;
    public Color highlightedBackgroundColor = new Color(1f, 0.85f, 0.4f, 1f);
    public Color defaultBackgroundColor = Color.white;

    private BossRushPlayerModel model;
    private Action<BossRushPlayerModel> onClick;

    private void Awake()
    {
        if (btRow != null) btRow.onClick.AddListener(() => onClick?.Invoke(model));
    }

    public void Show(BossRushPlayerModel player, bool isMine, Action<BossRushPlayerModel> onClick)
    {
        model = player;
        this.onClick = onClick;
        txtPosition.text = player.Position.ToString();
        txtPlayerName.text = player.PlayerName;
        txtDamagePoints.text = "<sprite=0>" + player.TotalDamagePoints.ToLetter();
        if (txtPower != null) txtPower.text = "<sprite=0>" + player.Power.ToLetter();
        if (imgBackground != null) imgBackground.color = isMine ? highlightedBackgroundColor : defaultBackgroundColor;
    }
}
