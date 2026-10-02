using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup vào Boss Rush (BossRushJoinPopup gốc): tên tier, mô tả, đếm ngược "Ends in"/"Starts in", nút Join.
// Join → joinBossRush → có nhóm thì mở UIBossRushPopup; còn thưởng đợt trước → mở UIBossRushClaimPopup.
public class UIBossRushJoinPopup : BaseUI
{
    public TMP_Text txtTitle;
    public TMP_Text txtTierName;
    public TMP_Text txtDescription;
    public TMP_Text txtTimer;
    public Button btJoin;
    public TMP_Text txtJoinButton;
    public Button btClose;
    public GameObject objLoading;

    private float tick;
    private bool isJoining;

    protected override void Awake()
    {
        base.Awake();
        btJoin.onClick.AddListener(OnClickJoin);
        if (btClose != null) btClose.onClick.AddListener(Close);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        isJoining = false;
        SetLoading(false);
        if (txtTitle != null) txtTitle.text = "Boss Rush";
        Refresh();
    }

    private void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 1f;
        Refresh();
    }

    private void Refresh()
    {
        DateTime now = BossRushSchedule.UtcNow;
        bool active = BossRushSchedule.IsActive(now);

        if (txtTierName != null) txtTierName.text = GameData.staticData.bossRush.GetTierName(GameData.userData.bossRush.tier);
        if (txtDescription != null) txtDescription.text = active ? "Fight with an 8-player team and challenge bosses." : "No events today.";
        if (txtTimer != null)
        {
            txtTimer.text = active
                ? "Ends in: " + BossRushSchedule.FormatRemain(BossRushSchedule.GetEventEnd(now) - now)
                : "Starts in " + BossRushSchedule.FormatRemain(BossRushSchedule.GetNextEventStart(now) - now);
        }
        if (txtJoinButton != null) txtJoinButton.text = active || HasUnclaimed ? "Join" : "Ok";
    }

    private static bool HasUnclaimed => string.IsNullOrEmpty(GameData.userData.bossRush.unclaimedPoolId) == false;

    private void OnClickJoin()
    {
        if (isJoining) return;

        if (!BossRushSchedule.IsActive(BossRushSchedule.UtcNow) && !HasUnclaimed)
        {
            Close();
            return;
        }

        isJoining = true;
        SetLoading(true);
        BossRushController.Instance.Join((ok, res) =>
        {
            isJoining = false;
            SetLoading(false);
            if (!ok || res == null)
            {
                UIManager.Instance.ShowToastMessage("Failed to join Boss Rush. Please try again.", isLocalize: false);
                return;
            }

            Close();
            if (res.hasUnclaimed)
            {
                UIBossRushClaimPopup.Open(res.unclaimedPoolId);
            }
            else if (!string.IsNullOrEmpty(res.poolId))
            {
                UIManager.Instance.LoadUI(UIKey.BossRushPopup);
            }
            else
            {
                UIManager.Instance.ShowToastMessage("No events today.", isLocalize: false);
            }
        });
    }

    private void SetLoading(bool isOn)
    {
        if (objLoading != null) objLoading.SetActive(isOn);
        btJoin.interactable = !isOn;
    }
}
