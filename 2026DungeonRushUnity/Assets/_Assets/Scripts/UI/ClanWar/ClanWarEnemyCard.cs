using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 đối thủ ngày 6 (ClanWarEnemyCard gốc): tên, Power, nút Battle "+{pts}" / "Defeated" / "No Tickets".
public class ClanWarEnemyCard : MonoBehaviour
{
    public Button CardButton;
    public Image ProfileImage;
    public TMP_Text NameText;
    public TMP_Text PowerText;
    public Button BattleButton;
    public GameObject BattleIconRoot;
    public TMP_Text RewardValueText;
    public TMP_Text BattleButtonText;
    public TMP_Text DefeatedText;
    public ClanLoadingView BattleLoading;
    public Image BattleButtonBackground;
    public Color BattleButtonNormalColor = Color.white;
    public Color BattleButtonDisabledColor = Color.gray;

    private Action onBattle;
    private Action onClick;

    private void Awake()
    {
        if (BattleButton != null) BattleButton.onClick.AddListener(() => onBattle?.Invoke());
        if (CardButton != null) CardButton.onClick.AddListener(() => onClick?.Invoke());
    }

    // gcc gốc: (target, canAttack, hasTickets, points, onBattle, onClick).
    public void Set(ClanWarPvpTargetDTO target, bool canAttack, bool hasTickets, int points, Action onBattle, Action onClick)
    {
        this.onBattle = onBattle;
        this.onClick = onClick;
        if (NameText != null) NameText.text = target.playerName;
        if (PowerText != null) PowerText.text = ClanUIUtil.PowerText(target.power);
        bool enabled = !target.defeated && canAttack && hasTickets;
        if (BattleButton != null) BattleButton.interactable = enabled;
        if (BattleButtonBackground != null) BattleButtonBackground.color = enabled ? BattleButtonNormalColor : BattleButtonDisabledColor;
        if (BattleIconRoot != null) BattleIconRoot.SetActive(!target.defeated && hasTickets);
        if (RewardValueText != null)
        {
            RewardValueText.gameObject.SetActive(!target.defeated && hasTickets);
            RewardValueText.text = "+" + ClanWarController.FormatNumber(points);
        }
        if (BattleButtonText != null)
        {
            BattleButtonText.gameObject.SetActive(target.defeated || !hasTickets);
            BattleButtonText.text = target.defeated ? "Defeated" : "No Tickets";
        }
        if (DefeatedText != null)
        {
            DefeatedText.gameObject.SetActive(target.defeated);
            DefeatedText.text = "Defeated";
        }
        SetBusy(false);
    }

    // gcd gốc.
    public void SetBusy(bool busy)
    {
        if (BattleLoading == null) return;
        if (busy) BattleLoading.ShowLoading();
        else BattleLoading.Hide();
    }
}
