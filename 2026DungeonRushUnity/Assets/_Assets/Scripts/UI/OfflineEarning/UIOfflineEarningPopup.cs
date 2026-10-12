using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup thưởng offline "Combat Summary" (OfflineEarnningPopup gốc) — lobby bật lúc mới vào game nếu vắng ≥ 1 phút.
//  - Nút X: nhận thưởng ×1 rồi đóng (OnCollectClicked gốc — popup không có nút "bỏ qua").
//  - "Collect x2": xem quảng cáo (đã mua No Ads thì nhận luôn) → nhận ×2.
// Chuỗi hiển thị lấy từ localization gốc: Popup.OfflineEarnings.*.
public class UIOfflineEarningPopup : BaseUI
{
    private const string HIGHLIGHT = "<color=#E5F36B>";
    private const string END_COLOR = "</color>";

    public TMP_Text txtSummary;
    public TMP_Text txtExperience;
    public TMP_Text txtExpPerHour;
    public TMP_Text txtLootBoxPerHour;
    public TMP_Text txtMaxReward;
    public Button btClose;
    public Button btDouble;

    private OfflineEarningResult result;
    private Action onCollected;
    private bool isCollected;

    protected override void Awake()
    {
        base.Awake();
        btClose.onClick.AddListener(OnClickCollect);
        btDouble.onClick.AddListener(OnClickDouble);
    }

    // kpd gốc: tính thưởng 1 lần lúc mở rồi vẽ.
    public void Show(Action onCollected)
    {
        this.onCollected = onCollected;
        isCollected = false;
        result = OfflineEarningService.Calculate();

        string duration = HIGHLIGHT + ShopUIUtil.FormatDuration(result.duration) + END_COLOR;
        string enemies = HIGHLIGHT + ShopUIUtil.FormatAmount(result.enemies) + END_COLOR;
        string lootBox = "<sprite=0>" + HIGHLIGHT + ShopUIUtil.FormatAmount(result.lootBox) + END_COLOR;
        txtSummary.text = "While you were away for " + duration + "\nyour hero continued fighting\n\nDefeated " + enemies
            + " enemies\nFound " + lootBox + " loot boxes";
        txtExperience.text = "Earned <sprite=0>" + HIGHLIGHT + ShopUIUtil.FormatAmount(result.exp) + END_COLOR + " experience";
        txtExpPerHour.text = ShopUIUtil.FormatAmount(result.expPerHour) + "/h";
        txtLootBoxPerHour.text = ShopUIUtil.FormatAmount(result.lootBoxPerHour) + "/h";
        txtMaxReward.text = "Max Reward - " + ShopUIUtil.FormatDuration(result.maxSeconds);
    }

    private void OnClickCollect()
    {
        Collect(1);
    }

    // kpm gốc.
    private void OnClickDouble()
    {
        if (GameData.userData.shop.isNoAdsBought)
        {
            Collect(2);
            return;
        }

        MediationAds.Instance.ShowRewardedVideoAd(OfflineEarningService.AD_PLACEMENT, (adResult) =>
        {
            if (adResult != ShowResultADS.Finished)
                return;

            Collect(2);
        });
    }

    private void Collect(int multiplier)
    {
        if (isCollected)
            return;

        isCollected = true;
        OfflineEarningService.Collect(result, multiplier);
        Close();
        onCollected?.Invoke();
    }
}
