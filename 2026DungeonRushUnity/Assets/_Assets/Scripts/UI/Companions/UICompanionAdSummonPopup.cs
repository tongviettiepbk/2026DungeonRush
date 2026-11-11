using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Popup summon pet bằng quảng cáo (CompanionAdSummonPopup gốc, reverse il2cpp gqd..gqn) — mở từ nút Ads trang Companion.
//  - "x{N}" = số con lượt này (bonus +1 sau mỗi lần xem, trần round(cap)+35), "Next summon {N+1}", "(Max {trần} times)".
//  - Nút: "<sprite=0>{còn lại}/3\nSummon x{N}" (đã mua No Ads thì bỏ icon, bấm là nhận luôn);
//    hết 3 lượt trong ngày → khoá nút, hiện thời gian tới 0h UTC.
public class UICompanionAdSummonPopup : BaseUI
{
    private const string AD_PLACEMENT = "companion_summon_ad";

    public Button SummonButton;
    public Image SummonButtonImage;
    public TMP_Text SummonButtonText;
    public TMP_Text NextSummonText;
    public TMP_Text SummonCountText;
    public TMP_Text infoText;
    public Button CloseButton;
    public Button btBackground;

    // Trạng thái nút bằng sprite (Batch3Reskin gán): hồng = xem ads, xanh lá = đã mua No Ads, xám = hết lượt.
    public Sprite spriteAd;
    public Sprite spriteFree;
    public Sprite spriteDisabled;

    private Action<List<CompanionSummonResult>> onSummoned;
    private bool isWaitingAd;

    protected override void Awake()
    {
        base.Awake();
        SummonButton.onClick.AddListener(OnClickSummon);
        CloseButton.onClick.AddListener(Close);
        if (btBackground != null)
            btBackground.onClick.AddListener(Close);
    }

    // gqc + eip gốc.
    public void Show(Action<List<CompanionSummonResult>> onSummoned)
    {
        this.onSummoned = onSummoned;
        isWaitingAd = false;
        Refresh();
    }

    // gqd gốc.
    private void Refresh()
    {
        int max = CompanionService.GetAdSummonMaxCount();
        int count = CompanionService.GetAdSummonCount();
        int next = Mathf.Min(count + 1, max);
        int remaining = CompanionService.GetAdSummonRemainingToday();
        bool isNoAds = GameData.userData.shop.isNoAdsBought;

        infoText.text = Loc.L("Popup.CompanionAdSummon.Max", max);
        SummonCountText.text = "x" + count;
        NextSummonText.text = Loc.L("Popup.CompanionAdSummon.Next", next);

        if (remaining <= 0)
        {
            SummonButton.interactable = false;
            SetButtonSprite(spriteDisabled);
            SummonButtonText.text = ShopUIUtil.FormatDuration((long)CompanionService.GetTimeToAdSummonReset().TotalSeconds);
            return;
        }

        SummonButton.interactable = true;
        SetButtonSprite(isNoAds ? spriteFree : spriteAd);
        SummonButtonText.text = (isNoAds ? string.Empty : "<sprite=0>") + remaining + "/" + CompanionSummonConfig.AD_DAILY_LIMIT
            + "\n" + Loc.L("UI.Companion.SummonX", count);
    }

    private void SetButtonSprite(Sprite sprite)
    {
        SummonButtonImage.color = Color.white;
        if (sprite != null)
            SummonButtonImage.sprite = sprite;
    }

    // gqg gốc: hết lượt thì thôi; No Ads → nhận luôn, không thì xem quảng cáo xong mới nhận.
    private void OnClickSummon()
    {
        if (isWaitingAd || CompanionService.GetAdSummonRemainingToday() <= 0)
            return;

        if (GameData.userData.shop.isNoAdsBought)
        {
            Analytics.LogAdSkippedNoAds(AD_PLACEMENT);
            Summon();
            return;
        }

        isWaitingAd = true;
        MediationAds.Instance.ShowRewardedVideoAd(AD_PLACEMENT, result =>
        {
            isWaitingAd = false;
            if (result == ShowResultADS.Finished)
                Summon();
        });
    }

    // gqh gốc.
    private void Summon()
    {
        List<CompanionSummonResult> results = CompanionService.SummonByAd();
        Action<List<CompanionSummonResult>> callback = onSummoned;
        onSummoned = null;
        Close();
        callback?.Invoke(results);
    }
}
