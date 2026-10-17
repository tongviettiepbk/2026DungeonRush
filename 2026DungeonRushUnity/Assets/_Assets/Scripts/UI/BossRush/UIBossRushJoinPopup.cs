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
        if (txtTitle != null) txtTitle.text = Loc.L("Popup.BossRushJoin.Title");
        Refresh();
        CheckUnclaimed();
    }

    // Thứ Hai (ngày chốt): máy chưa biết sảnh mùa trước còn thưởng hay không nếu lần hỏi server gần nhất là trước
    // lúc hết mùa → hỏi lại, có thưởng thì nút đổi thành "Join" để mở popup nhận thưởng.
    private void CheckUnclaimed()
    {
        if (BossRushSchedule.IsActive(BossRushSchedule.UtcNow) || HasUnclaimed) return;

        SetLoading(true);
        BossRushController.Instance.RefreshPool((ok, res) =>
        {
            if (this == null || !gameObject.activeInHierarchy) return;
            SetLoading(false);
            Refresh();
        });
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
        if (txtDescription != null) txtDescription.text = active ? Loc.L("Popup.BossRushJoin.Description.Active") : Loc.L("Popup.BossRushJoin.Description.Inactive");
        if (txtTimer != null)
        {
            txtTimer.text = active
                ? "Ends in: " + BossRushSchedule.FormatRemain(BossRushSchedule.GetEventEnd(now) - now)
                : "Starts in " + BossRushSchedule.FormatRemain(BossRushSchedule.GetNextEventStart(now) - now);
        }
        if (txtJoinButton != null) txtJoinButton.text = active || HasUnclaimed ? Loc.L("Popup.BossRushJoin.JoinButton") : Loc.L("Popup.BossRushJoin.OkButton");
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
                // Popup.BossRush.ResultsPreparing gốc: server đang chốt mùa cũ / dựng sảnh mùa mới.
                string message = res != null && res.preparing ? Loc.L("Popup.BossRush.ResultsPreparing") : Loc.L("Errors.BossRush.JoinFailed");
                UIManager.Instance.ShowToastMessage(message, isLocalize: false);
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
                UIManager.Instance.ShowToastMessage(Loc.L("Popup.BossRushJoin.Description.Inactive"), isLocalize: false);
            }
        });
    }

    private void SetLoading(bool isOn)
    {
        if (objLoading != null) objLoading.SetActive(isOn);
        btJoin.interactable = !isOn;
    }
}
