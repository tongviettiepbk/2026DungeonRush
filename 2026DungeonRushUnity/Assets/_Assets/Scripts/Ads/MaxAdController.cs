using System;
using UnityEngine;
using UnityEngine.Events;

// Lớp SDK của MediationAds: quảng cáo có thưởng qua AppLovin MAX. Mọi chỗ gọi trong game chỉ dùng rewarded nên
// interstitial/banner chưa làm. Editor không gọi SDK: giữ nguyên hành vi base (trả Finished ngay) để test.
public class MaxAdController : MediationAds
{
    private const string MSG_AD_NOT_READY = "Ad is not ready. Please try again later.";

    private static string AdUnit => MonetizationSettings.MAX_REWARDED_AD_UNIT;

    private int retryAttempt;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    public override void Init()
    {
        if (Application.isEditor)
        {
            return;
        }

        if (string.IsNullOrEmpty(AdUnit))
        {
            DebugCustom.LogError("[MAX] MAX_REWARDED_AD_UNIT rỗng — không có quảng cáo.");
            return;
        }

        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoaded;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayed;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailed;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHidden;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedReward;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaid;

        MaxSdkCallbacks.OnSdkInitializedEvent += _ => LoadAd();
        MaxSdk.InitializeSdk();
    }

    public override void ShowRewardedVideoAd(string placement, UnityAction<ShowResultADS> callback = null)
    {
        // Đã mua No Ads thì mọi chỗ đặt đều trao thưởng ngay, không phụ thuộc từng chỗ gọi tự kiểm tra.
        if (GameData.userData.shop.isNoAdsBought)
        {
            Analytics.LogAdSkippedNoAds(placement);
            callback?.Invoke(ShowResultADS.Finished);
            return;
        }

        // Ghi kết quả trước khi trao thưởng: chỗ gọi thường Save ngay trong callback, bộ đếm rewarded phải kịp vào save.
        UnityAction<ShowResultADS> tracked = result =>
        {
            Analytics.LogAdResult(placement, result.ToString().ToLowerInvariant());
            callback?.Invoke(result);
        };

        if (Application.isEditor)
        {
            Analytics.LogAdRequest(placement, true);
            base.ShowRewardedVideoAd(placement, tracked);
            return;
        }

        bool isReady = string.IsNullOrEmpty(AdUnit) == false && MaxSdk.IsRewardedAdReady(AdUnit);
        Analytics.LogAdRequest(placement, isReady);
        AppsflyerHelper.SendShowReward(GameData.userData.player.playerLevel);

        if (isReady == false)
        {
            // Trả Failed để chỗ gọi không phát thưởng; lượt sau có thể đã nạp xong.
            UIManager.Instance.ShowToastMessage(MSG_AD_NOT_READY, isLocalize: false);
            tracked(ShowResultADS.Failed);
            return;
        }

        isRewarded = false;
        rewardAdCallback = tracked;
        IsFullscreenShowing = true;
        // Game chạy runInBackground nên nhạc không tự dừng khi quảng cáo che màn hình.
        AudioListener.pause = true;
        MaxSdk.ShowRewardedAd(AdUnit, placement);
    }

    private void LoadAd()
    {
        MaxSdk.LoadRewardedAd(AdUnit);
    }

    private void OnAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        retryAttempt = 0;
    }

    private void OnAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // AppLovin khuyên thử lại giãn dần, tối đa 64 giây.
        retryAttempt++;
        DebugCustom.LogError("[MAX] Rewarded load failed: " + errorInfo.Code + " " + errorInfo.Message);
        Invoke(nameof(LoadAd), (float)Math.Pow(2, Math.Min(6, retryAttempt)));

        // Chỉ lần hỏng đầu của một chuỗi: máy không có fill thử lại liên tục, bắn mỗi lần là ngập event.
        if (retryAttempt == 1)
        {
            FirebaseServices.LogEvent("ads_reward_load_fail");
        }
    }

    private void OnAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        FirebaseServices.LogEvent("ads_reward_show_success");
        AppsflyerHelper.SendShowRewardSuccess(GameData.userData.player.playerLevel);
    }

    private void OnAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        DebugCustom.LogError("[MAX] Rewarded display failed: " + errorInfo.Code + " " + errorInfo.Message);
        FirebaseServices.LogEvent("ads_reward_show_fail");
        UIManager.Instance.ShowToastMessage(MSG_AD_NOT_READY, isLocalize: false);
        FinishAd(ShowResultADS.Failed);
    }

    private void OnAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        FinishAd(isRewarded ? ShowResultADS.Finished : ShowResultADS.Skipped);
    }

    private void OnAdReceivedReward(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        isRewarded = true;
        FirebaseServices.LogEvent("ads_reward_complete");
    }

    private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        if (adInfo == null || adInfo.Revenue <= 0)
        {
            return;
        }

        FirebaseServices.LogAdRevenue(adInfo.NetworkName, adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Revenue);
        AppsflyerHelper.LogAdRevenue(adInfo.NetworkName, adInfo.AdUnitIdentifier, adInfo.AdFormat, adInfo.Revenue);
    }

    private void FinishAd(ShowResultADS result)
    {
        AudioListener.pause = false;
        IsFullscreenShowing = false;
        LoadAd();

        UnityAction<ShowResultADS> callback = rewardAdCallback;
        rewardAdCallback = null;
        isRewarded = false;
        callback?.Invoke(result);
    }
}
