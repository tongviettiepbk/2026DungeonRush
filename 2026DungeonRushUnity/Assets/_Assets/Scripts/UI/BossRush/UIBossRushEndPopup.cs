using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup cuối trận (BossRushEndPopup gốc): "Fight Complete!", "Team Damage: {0}" + "Damage Dealt: {0}", nút Continue.
// Báo damage lỗi → "Failed to get battle results." + Retry (gửi lại BossRushPendingReport).
public class UIBossRushEndPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtInfo;
    public TMP_Text txtError;
    public Button btContinue;
    public TMP_Text txtContinue;
    public Button btRetry;
    public Button btClose;
    public GameObject objLoading;

    private Action onClose;

    protected override void Awake()
    {
        base.Awake();
        btContinue.onClick.AddListener(OnClickContinue);
        if (btClose != null) btClose.onClick.AddListener(OnClickContinue);
        if (btRetry != null) btRetry.onClick.AddListener(OnClickRetry);
    }

    public void Show(long teamDamage, long ownDamage, bool reported, Action onClose)
    {
        this.onClose = onClose;
        if (txtTitle != null) txtTitle.text = "Fight Complete!";
        txtInfo.text = "Team Damage: " + teamDamage.ToLetter() + "\nDamage Dealt: " + ownDamage.ToLetter();
        if (txtContinue != null) txtContinue.text = "Continue";
        SetError(!reported);
        if (objLoading != null) objLoading.SetActive(false);
    }

    private void SetError(bool isOn)
    {
        if (txtError != null)
        {
            txtError.gameObject.SetActive(isOn);
            txtError.text = "Failed to get battle results.";
        }
        if (btRetry != null) btRetry.gameObject.SetActive(isOn && GameData.userData.bossRush.pendingReport != null);
    }

    private void OnClickRetry()
    {
        if (objLoading != null) objLoading.SetActive(true);
        BossRushController.Instance.RetryPendingReport(() =>
        {
            if (objLoading != null) objLoading.SetActive(false);
            SetError(GameData.userData.bossRush.pendingReport != null);
        });
    }

    private void OnClickContinue()
    {
        Close();
        onClose?.Invoke();
    }
}
