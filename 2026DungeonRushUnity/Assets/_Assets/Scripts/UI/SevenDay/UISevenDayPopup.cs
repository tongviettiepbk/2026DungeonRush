using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thưởng 7 ngày, dựng từ khung UIMiningOfferPopup (RetentionUIBuilder) rồi reskin theo concept
// tools/concept/seven_day/concept.png (SevenDayPopupReskin).
public class UISevenDayPopup : BaseUI
{
    public TMP_Text txtTitle;
    public List<SevenDayCellUI> cells;
    public Button btClaim;
    public TMP_Text txtClaim;
    public Button btBackground;
    public TMP_Text txtTapToClose;
    public Sprite sprGem;
    public Sprite sprHammer;
    public Sprite sprBone;

    // Reskin: dòng phụ dưới tiêu đề, nhãn ô ngày 7, ô đếm giờ trên nút, nút đóng; nút Claim đổi sprite xanh/xám.
    public TMP_Text txtSubtitle;
    public TMP_Text txtBigReward;
    public GameObject objTimer;
    public TMP_Text txtTimer;
    public Button btClose;
    public Image imgClaim;
    public Sprite sprClaimOn;
    public Sprite sprClaimOff;

    private Action onClosed;
    private float nextTextRefreshAt;

    public static void Show(Action closed = null)
    {
        UISevenDayPopup popup = UIManager.Instance.LoadUI(UIKey.SevenDayPopup) as UISevenDayPopup;
        if (popup == null)
        {
            closed?.Invoke();
            return;
        }

        popup.onClosed = closed;
        popup.Refresh();
    }

    protected override void Awake()
    {
        base.Awake();
        btClaim.onClick.AddListener(OnClickClaim);
        if (btBackground != null) btBackground.onClick.AddListener(Close);
        if (btClose != null) btClose.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTextRefreshAt)
            return;

        nextTextRefreshAt = Time.unscaledTime + 1f;
        RefreshButton();
    }

    private void Refresh()
    {
        txtTitle.text = Loc.L("Popup.SevenDay.Title");
        if (txtTapToClose != null) txtTapToClose.text = Loc.L("Common.TapToClose");
        if (txtSubtitle != null) txtSubtitle.text = Loc.L("Popup.SevenDay.Subtitle");
        if (txtBigReward != null) txtBigReward.text = Loc.L("Popup.SevenDay.BigReward");
        int claimed = SevenDayService.ClaimedCount;
        bool canClaim = SevenDayService.CanClaim;

        for (int i = 0; i < cells.Count; i++)
        {
            ShopReward[] rewards = SevenDayService.GetRewards(i);
            cells[i].Setup(i, rewards, GetIcon(rewards[0]), rewards.Length > 1 ? GetIcon(rewards[1]) : null, i < claimed, canClaim && i == claimed);
        }

        RefreshButton();
    }

    private void RefreshButton()
    {
        bool canClaim = SevenDayService.CanClaim;
        bool isWaiting = canClaim == false && SevenDayService.ClaimedCount < SevenDayService.DAY_COUNT;
        btClaim.interactable = canClaim;
        if (imgClaim != null) imgClaim.sprite = canClaim ? sprClaimOn : sprClaimOff;
        txtClaim.text = canClaim ? Loc.L("Common.Claim") : Loc.L("Clan.War.RewardCard.Claimed");

        if (objTimer != null) objTimer.SetActive(isWaiting);
        if (isWaiting && txtTimer != null)
        {
            string time = ShopUIUtil.FormatDuration((long)SevenDayService.GetTimeToNextClaim().TotalSeconds);
            txtTimer.text = Loc.L("Popup.SevenDay.NextGift", "<color=#FFD35A>" + time + "</color>");
        }
    }

    private Sprite GetIcon(ShopReward reward)
    {
        switch (reward.type)
        {
            case ShopRewardType.Gem: return sprGem;
            case ShopRewardType.Bone: return sprBone;
            default: return sprHammer;
        }
    }

    private void OnClickClaim()
    {
        if (SevenDayService.Claim())
        {
            ShopUIUtil.RefreshLobby();
            Refresh();
        }
    }

    public override void Close()
    {
        Action closed = onClosed;
        onClosed = null;
        base.Close();
        closed?.Invoke();
    }
}
