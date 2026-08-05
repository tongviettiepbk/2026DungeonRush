using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

// Toàn bộ save data của người chơi, chia module theo tính năng.
//
// Thêm 1 module mới gồm 4 bước:
//   1. Tạo class UserXxxData : BaseUserData (folder tính năng riêng)
//   2. Thêm const DATA_KEY_XXX + property xxx ở đây
//   3. Thêm vào listData trong ValidateData()
//   4. Thêm dòng load trong Load()
public class UserData
{
    public const string DATA_KEY_PROFILE = "key_user_profile";
    public const string DATA_KEY_CAMPAIGN = "key_user_campaign";
    public const string DATA_KEY_ITEMS = "key_user_items";
    public const string DATA_KEY_SETTING = "key_user_settings";
    public const string DATA_KEY_EQUIPMENT = "key_user_equipment";

    public UserProfileData profile { get; set; } = new UserProfileData();
    public UserCampaignData campaign { get; set; } = new UserCampaignData();
    public UserItemData items { get; set; } = new UserItemData();
    public UserSettingData settings { get; set; } = new UserSettingData();
    public UserEquipmentData equipment { get; set; } = new UserEquipmentData();

    private List<BaseUserData> listData;
    private float lastTimeSaveData;

    public static bool isDisableSaveDataLocal;

    [JsonIgnore] public bool isNewUser { get; private set; }

    #region Load
    public void Load()
    {
        isNewUser = false;

        // Profile load riêng vì cần đánh dấu isNewUser + đặt tên mới
        profile = LoadModule<UserProfileData>(DATA_KEY_PROFILE, out bool isFirstTime);
        if (isFirstTime)
        {
            isNewUser = true;
            profile.userName = GameUtils.GetNewUserName();
        }

        campaign = LoadModule<UserCampaignData>(DATA_KEY_CAMPAIGN, out _);
        items = LoadModule<UserItemData>(DATA_KEY_ITEMS, out _);
        settings = LoadModule<UserSettingData>(DATA_KEY_SETTING, out _);
        equipment = LoadModule<UserEquipmentData>(DATA_KEY_EQUIPMENT, out _);

        LoadDone();
    }

    // Load 1 module từ PlayerPrefs; save hỏng hoặc chưa có thì tạo mới + InitData
    private T LoadModule<T>(string key, out bool isFirstTime) where T : BaseUserData, new()
    {
        isFirstTime = false;
        string prefs = PlayerPrefs.GetString(key);

        if (string.IsNullOrEmpty(prefs))
        {
            isFirstTime = true;
            T data = new T();
            data.InitData();
            return data;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(prefs);
        }
        catch
        {
            DebugCustom.LogError("load_error_" + key);
            T data = new T();
            data.InitData();
            return data;
        }
    }

    private void LoadDone()
    {
        ValidateData();
        isDisableSaveDataLocal = false;

        if (isNewUser)
        {
            RefreshNewDay();
            Save(true);
        }
        else
        {
            CheckNewDay();
        }
    }
    #endregion

    #region Validate & Save
    public void ValidateData()
    {
        if (listData == null)
        {
            listData = new List<BaseUserData>();
            listData.Add(profile);
            listData.Add(campaign);
            listData.Add(items);
            listData.Add(settings);
            listData.Add(equipment);
        }

        for (int i = 0; i < listData.Count; i++)
        {
            try
            {
                listData[i].ValidateData();
            }
            catch (Exception e)
            {
                DebugCustom.LogError(e.Message);
                continue;
            }
        }
    }

    public void Save(bool isForceSave = false)
    {
        if (isDisableSaveDataLocal)
        {
            return;
        }

        float interval = 1f;

        if (isForceSave || Time.realtimeSinceStartup - lastTimeSaveData > interval)
        {
            lastTimeSaveData = Time.realtimeSinceStartup;
            bool savePrefs = false;

            for (int i = 0; i < listData.Count; i++)
            {
                if (listData[i].Save(isForceSave))
                {
                    savePrefs = true;
                }
            }

            if (savePrefs)
            {
                PlayerPrefs.Save();
            }
        }
    }
    #endregion

    #region Time
    public void CheckNewDay()
    {
        DateTime now = GameUtils.GetTimeNow();
        if (now.Date > profile.lastDayLogin.ToDateTime().Date)
        {
            DebugCustom.LogFormat("[NewDay] lastDayLogin={0}, now={1}", profile.lastDayLogin, now);
            RefreshNewDay();
        }
    }

    public void RefreshNewDay()
    {
        DateTime now = GameUtils.GetTimeNow();
        DateTime lastDayLogin = profile.lastDayLogin.ToDateTime();

        if (now.Year != lastDayLogin.Year)
        {
            RefreshNewYear();
        }

        if (now.Year != lastDayLogin.Year || now.Month != lastDayLogin.Month)
        {
            RefreshNewMonth();
        }

        if (GetWeekOfYear(now) != GetWeekOfYear(lastDayLogin) || now.Year != lastDayLogin.Year)
        {
            RefreshNewWeek();
        }

        for (int i = 0; i < listData.Count; i++)
        {
            listData[i].RefreshNewDay();
        }

        // TODO: khi có EventDispatcher (phần Patterns) thì post EventID.NewDay ở đây
    }

    public void RefreshNewWeek()
    {
        DebugCustom.Log("[UserData] Refresh New Week");

        for (int i = 0; i < listData.Count; i++)
        {
            listData[i].RefreshNewWeek();
        }
    }

    public void RefreshNewMonth()
    {
        DebugCustom.Log("[UserData] Refresh New Month");

        for (int i = 0; i < listData.Count; i++)
        {
            listData[i].RefreshNewMonth();
        }
    }

    public void RefreshNewYear()
    {
        DebugCustom.Log("[UserData] Refresh New Year");

        for (int i = 0; i < listData.Count; i++)
        {
            listData[i].RefreshNewYear();
        }
    }

    private static int GetWeekOfYear(DateTime time)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        return culture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
    #endregion
}
