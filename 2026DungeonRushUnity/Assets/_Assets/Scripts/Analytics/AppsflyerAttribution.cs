using System;
using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

// Nghe nguồn cài từ AppsFlyer, giữ lại trên máy và đẩy sang Firebase, để cắt retention + doanh thu theo campaign.
// Racing thiếu phần này suốt tuần đầu: 72% lần cài trong BigQuery không rõ nguồn.
public class AppsflyerAttribution : MonoBehaviour, IAppsFlyerConversionData
{
    private const string PREF_STATUS = "af_attr_status";
    private const string PREF_SOURCE = "af_attr_source";
    private const string PREF_CAMPAIGN = "af_attr_campaign";
    private const string PREF_ADSET = "af_attr_adset";

    // organic / non_organic; rỗng = SDK chưa trả dữ liệu.
    public static string Status => PlayerPrefs.GetString(PREF_STATUS, string.Empty);
    public static string MediaSource => PlayerPrefs.GetString(PREF_SOURCE, string.Empty);
    public static string Campaign => PlayerPrefs.GetString(PREF_CAMPAIGN, string.Empty);

    // SDK trả cùng dữ liệu ở mọi lần mở app; chỉ lần đầu được ghi và bắn install_attribution.
    public void onConversionDataSuccess(string conversionData)
    {
        try
        {
            if (Status.Length > 0)
            {
                return;
            }

            Dictionary<string, object> data = AppsFlyer.CallbackStringToDictionary(conversionData);
            bool organic = string.Equals(Get(data, "af_status"), "Organic", StringComparison.OrdinalIgnoreCase);
            string status = organic ? "organic" : "non_organic";
            string source = organic ? "organic" : Get(data, "media_source");
            string campaign = Get(data, "campaign");
            string adset = Get(data, "adset");

            if (adset.Length == 0)
            {
                adset = Get(data, "af_adset");
            }

            PlayerPrefs.SetString(PREF_STATUS, status);
            PlayerPrefs.SetString(PREF_SOURCE, source);
            PlayerPrefs.SetString(PREF_CAMPAIGN, campaign);
            PlayerPrefs.SetString(PREF_ADSET, adset);
            PlayerPrefs.Save();

            Analytics.Log("install_attribution",
                ("af_status", status),
                ("media_source", source),
                ("campaign", campaign),
                ("adset", adset),
                ("is_first_launch", Get(data, "is_first_launch")));
            Analytics.RefreshUserProperties();
        }
        catch (Exception e)
        {
            DebugCustom.LogError("[AppsFlyer] conversion data: " + e);
        }
    }

    public void onConversionDataFail(string error)
    {
        DebugCustom.LogError("[AppsFlyer] conversion data failed: " + error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
    }

    public void onAppOpenAttributionFailure(string error)
    {
    }

    private static string Get(Dictionary<string, object> data, string key)
    {
        return data != null && data.TryGetValue(key, out object value) && value != null ? value.ToString() : string.Empty;
    }
}
