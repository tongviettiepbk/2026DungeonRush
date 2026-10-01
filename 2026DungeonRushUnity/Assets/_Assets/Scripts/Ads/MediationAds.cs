using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Keys
{
    public const string realtimeAdRevenueGoogle = "ad_impression";
    public const string realtimeAdRevenue = "ad_revenue_sdk";
    public const string adSubRevenue = "sub_revenue";
}

public enum ShowResultADS
{
    Failed = 0,
    Skipped = 1,
    Finished = 2,
}

public class MediationAds : Singleton<MediationAds>
{
    protected bool isRewarded;
    protected bool isInterstitialLoading;
    protected UnityAction<ShowResultADS> interstitialAdCallback;
    protected UnityAction<ShowResultADS> rewardAdCallback;

    protected bool isBannerAdLoading;
    protected UnityAction<ShowResultADS> bannerAdLoadCallback;

    public virtual void Init()
    {

    }

    public virtual void Preload()
    {

    }

    public virtual void ShowInterstitialAd(string placement, UnityAction<ShowResultADS> callback = null)
    {
#if UNITY_EDITOR
        if (callback != null)
            callback(ShowResultADS.Finished);
        return;
#endif
    }

    public virtual void ShowRewardedVideoAd(string placement, UnityAction<ShowResultADS> callback = null)
    {
#if UNITY_EDITOR
        if (callback != null)
            callback(ShowResultADS.Finished);
        return;
#endif
    }

    public virtual void LoadBannerAd(UnityAction<ShowResultADS> callback = null)
    {
#if UNITY_EDITOR
        if (callback != null)
            callback(ShowResultADS.Finished);
        return;
#endif
    }

    public virtual void ShowBannerAd()
    {

    }

    public virtual void HideBannerAd()
    {

    }
}
