using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng đối thủ (PvPPlayerElementUI gốc): tên, Power "<sprite=0>{power}", nút đánh "<sprite=0>+{trophy nếu thắng}".
public class ElementPvPPlayerUI : MonoBehaviour
{
    public TMP_Text txtName;
    public TMP_Text txtPower;
    public Button btBattle;
    public TMP_Text txtBattle;
    public Image imgBackground;
    public Color highlightedColor = new Color(1f, 0.93f, 0.6f, 1f);
    public Color defaultColor = Color.white;
    // Reskin: avatar + trophy-nếu-thắng tách khỏi nút (nút ghi "Battle <vé> 1").
    public Image imgAvatar;
    public TMP_Text txtWinTrophy;

    private PvPPlayerModel model;
    private Action<PvPPlayerModel> onBattle;

    private void Awake()
    {
        if (btBattle != null) btBattle.onClick.AddListener(OnClickBattle);
    }

    // jqg: winDelta = trophy cộng nếu thắng (PvPConfig.jkx theo trophy mình).
    public void Setup(PvPPlayerModel model, int winDelta, bool isMe, Action<PvPPlayerModel> onBattle)
    {
        this.model = model;
        this.onBattle = onBattle;
        if (txtName != null) txtName.text = string.IsNullOrEmpty(model.PlayerName) ? Loc.L("Common.Unknown") : model.PlayerName;
        if (txtPower != null) txtPower.text = model.Power > 0d ? "<sprite=0>" + model.Power.ToLetter() : "<sprite=0>0";
        if (imgBackground != null) imgBackground.color = isMe ? highlightedColor : defaultColor;
        if (btBattle != null)
        {
            btBattle.gameObject.SetActive(!isMe);
            btBattle.interactable = true;
        }
        if (imgAvatar != null) imgAvatar.sprite = AvatarConfig.Get(model.AvatarId);
        if (txtWinTrophy != null)
        {
            // startPvPBattle tiêu 1 vé.
            txtWinTrophy.text = "<sprite=0>+" + winDelta;
            if (txtBattle != null) txtBattle.text = Loc.L("Common.Battle") + "\n<sprite=0>1";
        }
        else if (txtBattle != null)
        {
            txtBattle.text = "<sprite=0>+" + winDelta;
        }
    }

    public void SetInteractable(bool isOn)
    {
        if (btBattle != null) btBattle.interactable = isOn;
    }

    private void OnClickBattle()
    {
        if (model != null) onBattle?.Invoke(model);
    }
}
