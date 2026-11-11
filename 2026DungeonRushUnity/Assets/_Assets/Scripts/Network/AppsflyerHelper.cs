using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

// Bọc AppsFlyer: nuốt lỗi SDK, không chạy trong Editor/bản Dev (DevBuild) hoặc khi chưa điền dev key (MonetizationSettings).
// Tên event là bộ ad-monetization chuẩn của AppsFlyer, đặt sai thì dashboard không gom được.
public static class AppsflyerHelper
{
    private const string EVENT_REWARD_SHOW = "af_rewarded_show";
    private const string EVENT_REWARD_SHOW_SUCCESS = "af_rewarded_displayed";

    // Giây AppsFlyer hoãn lượt gửi đầu để chờ người chơi trả lời hộp thoại ATT (MAX bật hộp này lúc init).
    private const int ATT_WAIT_SECONDS = 60;

    private static bool isStarted;

    public static void Init()
    {
        if (DevBuild.IsEnabled || string.IsNullOrEmpty(MonetizationSettings.APPSFLYER_DEV_KEY))
        {
            return;
        }

        Safe(() =>
        {
#if UNITY_IOS
            AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(ATT_WAIT_SECONDS);
#endif
            // Truyền object nghe để SDK trả nguồn cài về AppsflyerAttribution.
            GameObject listener = new GameObject(nameof(AppsflyerAttribution));
            UnityEngine.Object.DontDestroyOnLoad(listener);
            AppsFlyer.initSDK(MonetizationSettings.APPSFLYER_DEV_KEY, MonetizationSettings.APPSFLYER_IOS_APP_ID, listener.AddComponent<AppsflyerAttribution>());
            AppsFlyer.startSDK();
            isStarted = true;
        });
    }

    // Gắn uid Firebase làm Customer User ID, để nối dữ liệu AppsFlyer với tài khoản.
    public static void SetCustomerUserId(string uid)
    {
        if (isStarted == false || string.IsNullOrEmpty(uid))
        {
            return;
        }

        Safe(() => AppsFlyer.setCustomerUserId(uid));
    }

    // Bước phễu UA gửi nguyên tên, cùng tên bên Firebase (UaFunnel).
    public static void SendEvent(string eventName, Dictionary<string, string> values)
    {
        if (isStarted == false)
        {
            return;
        }

        Safe(() => AppsFlyer.sendEvent(eventName, values));
    }

    public static void SendShowReward(int level)
    {
        SendLevelEvent(EVENT_REWARD_SHOW, level);
    }

    public static void SendShowRewardSuccess(int level)
    {
        SendLevelEvent(EVENT_REWARD_SHOW_SUCCESS, level);
    }

    private static void SendLevelEvent(string eventName, int level)
    {
        if (isStarted == false)
        {
            return;
        }

        Safe(() => AppsFlyer.sendEvent(eventName, new Dictionary<string, string> { { AFInAppEvents.LEVEL, level.ToString() } }));
    }

    public static void LogAdRevenue(string source, string adUnit, string format, double value)
    {
        if (isStarted == false)
        {
            return;
        }

        Safe(() => AppsFlyer.logAdRevenue(new AFAdRevenueData(source, MediationNetwork.ApplovinMax, "USD", value),
            new Dictionary<string, string> { { "ad_unit_name", adUnit }, { "ad_format", format } }));
    }

    // Gửi đơn vừa trao hàng cho AppsFlyer xác minh với cửa hàng; đạt thì SDK tự bắn af_purchase.
    // Gọi đúng 1 lần mỗi đơn, tự bắn thêm af_purchase là doanh thu bị đếm 2 lần.
    // callbackTarget nhận onValidateAndLogComplete/Failure qua UnitySendMessage theo tên GameObject.
    public static void ValidateAndLogPurchase(Product product, Order order, MonoBehaviour callbackTarget)
    {
        if (isStarted == false || product == null || order == null || order.Info == null)
        {
            return;
        }

        Safe(() =>
        {
            string productId = product.definition.storeSpecificId;
            Dictionary<string, string> details = new Dictionary<string, string>();
#if UNITY_IOS
            AppsFlyer.setUseReceiptValidationSandbox(Debug.isDebugBuild);
            AppsFlyer.validateAndSendInAppPurchase(
                AFSDKPurchaseDetailsIOS.Init(productId, order.Info.TransactionID, AFSDKPurchaseType.OneTimePurchase),
                details, callbackTarget);
#else
            string token = ExtractGooglePurchaseToken(order.Info.Receipt);
            if (string.IsNullOrEmpty(token))
            {
                DebugCustom.LogError("[AppsFlyer] No purchase token, skip validateAndLog: " + productId);
                return;
            }

            AppsFlyer.validateAndSendInAppPurchase(new AFPurchaseDetailsAndroid(AFPurchaseType.OneTimePurchase, token, productId), details, callbackTarget);
#endif
        });
    }

    // Biên lai Unity IAP: {"Store":"GooglePlay","Payload":"{\"json\":\"{..purchaseToken..}\",\"signature\":..}"}.
    public static string ExtractGooglePurchaseToken(string receipt)
    {
        string payload = (string)JObject.Parse(receipt)["Payload"];
        string json = (string)JObject.Parse(payload)["json"];
        return (string)JObject.Parse(json)["purchaseToken"];
    }

    private static void Safe(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[AppsFlyer] " + e);
        }
    }
}
