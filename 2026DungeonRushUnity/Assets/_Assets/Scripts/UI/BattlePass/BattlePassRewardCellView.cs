using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 ô thưởng Battle Pass (BattlePassFreeRewardUI / BattlePassPremiumRewardUI gốc, juc / jvt):
// 2 dòng "<sprite=0>N", dấu tích khi đã nhận, khoá khi chưa mua Premium (ô Premium), màu nền theo trạng thái,
// bấm nhận khi đã tới mốc.
public class BattlePassRewardCellView : MonoBehaviour
{
    public TMP_Text RewardText1;
    public TMP_Text RewardText2;
    public GameObject CheckMarkImage;
    public GameObject LockImage;          // chỉ ô Premium
    public Image BackgroundImage;
    public Button Button;
    public GameObject NotificationUI;

    private BattlePassReward reward;
    private UIBattlePassPopup popup;

    private void Awake()
    {
        Button.onClick.AddListener(OnClicked);
    }

    public void Setup(BattlePassReward reward, UIBattlePassPopup popup)
    {
        this.reward = reward;
        this.popup = popup;
        Refresh();
    }

    public void Refresh()
    {
        popup.SetRewardText(RewardText1, reward.type1, reward.amount1);
        popup.SetRewardText(RewardText2, reward.type2, reward.amount2);

        bool isClaimed = BattlePassService.IsClaimed(reward.level, reward.isPremium);
        bool isReached = BattlePassService.IsReached(reward.level);
        bool isLocked = reward.isPremium && BattlePassService.IsPremiumBought() == false;
        bool canClaim = BattlePassService.CanClaim(reward.level, reward.isPremium);

        if (CheckMarkImage != null) CheckMarkImage.SetActive(isClaimed);
        if (LockImage != null) LockImage.SetActive(isLocked);
        BackgroundImage.sprite = isClaimed ? popup.RewardClaimedSprite : (isReached ? popup.RewardClaimableSprite : popup.RewardNotReachedSprite);
        Button.interactable = canClaim;
        if (NotificationUI != null) NotificationUI.SetActive(canClaim);
    }

    // OnButtonClicked gốc → BattlePassPopup.jva/jvb.
    private void OnClicked()
    {
        popup.Claim(reward);
    }
}
