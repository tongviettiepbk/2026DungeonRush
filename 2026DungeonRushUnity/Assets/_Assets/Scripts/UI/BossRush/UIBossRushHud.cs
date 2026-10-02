using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD trong trận Boss Rush (BossRushUI gốc): tên boss "Boss #{0}: {1}", thanh máu đỏ + vệt trắng chạy trễ, thời gian còn lại.
// Gắn trên UIMainLobby.objBossRushUI.
public class UIBossRushHud : MonoBehaviour
{
    public TMP_Text txtBossName;
    public TMP_Text txtBossHp;
    public Image imgHpFill;          // FillRed (Image Filled) — máu hiện tại
    public Image imgWhiteFill;       // FillWhite — chạy trễ phía sau
    public TMP_Text txtRemainingTime;

    [Header("Anim (giá trị mặc định như BossRushUI)")]
    public float hpAnimationDuration = 0.15f;
    public float whiteSliderDelay = 0.3f;
    public float whiteAnimationDuration = 0.4f;

    private int bossNumber = -1;

    public void SetBoss(int number, string bossName, double hp, double maxHp)
    {
        float percent = maxHp > 0d ? (float)System.Math.Max(0d, System.Math.Min(1d, hp / maxHp)) : 0f;
        bool isNewBoss = number != bossNumber;
        bossNumber = number;

        if (txtBossName != null) txtBossName.text = "Boss #" + number + ": " + bossName;
        if (txtBossHp != null) txtBossHp.text = System.Math.Max(0d, hp).ToLetter() + " / " + maxHp.ToLetter();

        if (imgHpFill != null)
        {
            imgHpFill.DOKill();
            if (isNewBoss) imgHpFill.fillAmount = percent;
            else imgHpFill.DOFillAmount(percent, hpAnimationDuration);
        }
        if (imgWhiteFill != null)
        {
            imgWhiteFill.DOKill();
            if (isNewBoss) imgWhiteFill.fillAmount = percent;
            else imgWhiteFill.DOFillAmount(percent, whiteAnimationDuration).SetDelay(whiteSliderDelay);
        }
    }

    public void SetRemainingTime(float seconds)
    {
        if (txtRemainingTime != null)
        {
            txtRemainingTime.text = Mathf.CeilToInt(seconds) + "s";
        }
    }

    private void OnDisable()
    {
        bossNumber = -1;
    }
}
