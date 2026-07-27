using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LocalizeManager : Singleton<LocalizeManager>
{
    public TextAsset defaultLanguageTextFile;
    public LanguageID testLanguage = LanguageID.Vietnamese;

    public LanguageID curLanguage { get; private set; } = LanguageID.English;

    public const string KEY_BREAKLINE = "<br>";
    public const string DATA_KEY_LANGUAGE = "LocalizeLanguage";
    public const string PATH_LOCALIZE_FILES_FULL = "Assets/_Assets/Resources/" + PATH_LOCALIZE_FILES;
    public const string PATH_LOCALIZE_FILES = "Json/Localize/Languages/";
    public const string PATH_ADDITIONAL_FILES = "Json/Localize/Categories/";

#if UNITY_ANDROID
    public const string translationApiKey = "AIzaSyBgKo2maBzaHqshF0dHvyNM2LXI5StUrc4";
#elif UNITY_IOS
    public const string translationApiKey = "AIzaSyDuhZH4b_ImwzoT0Og8vwdU_7pGY6DH6k4";
#else
    public const string translationApiKey = "AIzaSyBgKo2maBzaHqshF0dHvyNM2LXI5StUrc4";
#endif

    private bool initialized;
    private LanguageID defaultLanguage = LanguageID.English;
    public bool isTranslating { get; private set; }
    public Dictionary<string, string> defaultDictionary { get; private set; } = new Dictionary<string, string>();
    public Dictionary<string, string> curDictionary { get; private set; } = new Dictionary<string, string>();

    private void Awake()
    {
        DontDestroyOnLoad(this);
        Initialize();
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        LoadDefaultLanguage();
        LoadCurrentLanguage();

        if (curLanguage != defaultLanguage)
        {
            LoadNewLanguage(curLanguage);
        }

        initialized = true;
    }

    private void LoadDefaultLanguage()
    {
        if (defaultLanguageTextFile != null)
        {
            defaultDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(defaultLanguageTextFile.text);

            TextAsset[] additionalTextFiles = Resources.LoadAll<TextAsset>(PATH_ADDITIONAL_FILES);
            for (int i = 0; i < additionalTextFiles.Length; i++)
            {
                Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(additionalTextFiles[i].text);
                var e = dict.GetEnumerator();
                while (e.MoveNext())
                {
                    string key = e.Current.Key;
                    string value = e.Current.Value;

                    if (defaultDictionary.ContainsKey(key))
                    {
                        DebugCustom.LogWarning("Exists key=" + key);
                    }
                    else
                    {
                        defaultDictionary[key] = value;
                    }
                }
            }
        }
        else
        {
            DebugCustom.LogError("Default language text file not found");
        }
    }

    private LanguageID GetLanguageID()
    {
        switch (Application.systemLanguage)
        {
            case SystemLanguage.Vietnamese: return LanguageID.Vietnamese;
            case SystemLanguage.Korean: return LanguageID.Korean;
            case SystemLanguage.Russian: return LanguageID.Russian;
            case SystemLanguage.Chinese: return LanguageID.Chinese;
            case SystemLanguage.Japanese: return LanguageID.Japanese;
            case SystemLanguage.French: return LanguageID.French;
            case SystemLanguage.German: return LanguageID.German;
            case SystemLanguage.Portuguese: return LanguageID.Portuguese;
            case SystemLanguage.Italian: return LanguageID.Italian;
            case SystemLanguage.Indonesian: return LanguageID.Indonesian;
            case SystemLanguage.Thai: return LanguageID.Thai;
            case SystemLanguage.Polish: return LanguageID.Polish;
            case SystemLanguage.Spanish: return LanguageID.Spanish;
            case SystemLanguage.Turkish: return LanguageID.Turkish;
        }

        return LanguageID.English;
    }

    private void LoadCurrentLanguage()
    {
        LanguageID language = GetLanguageID();
        int id = PlayerPrefs.GetInt(DATA_KEY_LANGUAGE, (int)language);
        curLanguage = (LanguageID)id;

#if UNITY_EDITOR
        if (testLanguage != LanguageID.English)
        {
            curLanguage = testLanguage;
        }
#endif
    }

    public void LoadNewLanguage(LanguageID newLanguage)
    {
        if (newLanguage == defaultLanguage)
        {
            curLanguage = newLanguage;
            curDictionary = defaultDictionary;
        }
        else
        {
            string pathLanguageFile = PATH_LOCALIZE_FILES + string.Format("{0}-{1}", (int)newLanguage, newLanguage.ToString());
            DebugCustom.LogFormat("LoadLanguage={0}, Path={1}", newLanguage, pathLanguageFile);
            TextAsset textAsset = Resources.Load<TextAsset>(pathLanguageFile);

            if (textAsset != null)
            {
                try
                {
                    curLanguage = newLanguage;
                    curDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);
                }
                catch
                {
                    curLanguage = defaultLanguage;
                    curDictionary = defaultDictionary;
                    // Bản rút gọn so với StickIdle: bỏ Firebase analytics.
                    DebugCustom.LogError("bug_language_" + newLanguage);
                }
            }
            else
            {
                curLanguage = defaultLanguage;
                curDictionary = defaultDictionary;
                DebugCustom.LogError("Language JSON file not found!");
            }
        }
    }

    public string GetLocalizeText(string key, bool isToUpper = false)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        if (defaultDictionary.ContainsKey(key) == false)
        {
            DebugCustom.LogWarning("[Localize] Key not found=" + key);
        }

        string translatedWords = key;

        if (curDictionary.ContainsKey(key) && string.IsNullOrEmpty(curDictionary[key]) == false)
        {
            translatedWords = curDictionary[key];
        }

        if (isToUpper)
        {
            return translatedWords.ToUpper();
        }
        else
        {
            return translatedWords;
        }
    }

    public string GetLocalizeTextSuffix(string key, string suffix, bool isToUpper = false)
    {
        string str = GetLocalizeText(key, isToUpper);
        if (!string.IsNullOrEmpty(suffix))
        {
            str += " " + suffix;
        }
        return str;
    }

    #region Translate
    public void Translate(string text, Action<string> callback = null)
    {
        if (string.IsNullOrEmpty(text) == false)
        {
            isTranslating = true;
            string targetLang = Get2LetterISOCodeFromSystemLanguage(Application.systemLanguage);
            StartCoroutine(TranslateByGoogle(targetLang, text, 0, callback));
        }
        else
        {
            if (callback != null)
                callback(string.Empty);
        }
    }

    private IEnumerator TranslateByGoogle(string targetLang, string text, int sourceLanguageId, Action<string> callback = null)
    {
        var formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("Content-Type", "application/json; charset=utf-8"),
            new MultipartFormDataSection("target", targetLang),
            new MultipartFormDataSection("format", "text"),
            new MultipartFormDataSection("q", text)
        };

        var uri = $"https://translation.googleapis.com/language/translate/v2?key=" + translationApiKey;
        var webRequest = UnityWebRequest.Post(uri, formData);

        yield return webRequest.SendWebRequest();

        string translatedText = string.Empty;

        if (webRequest.isHttpError || webRequest.isNetworkError)
        {
            Debug.LogError(webRequest.error);
        }
        else
        {
            try
            {
                var parsedTexts = JObject.Parse(webRequest.downloadHandler.text);
                translatedText = (string)parsedTexts["data"]["translations"][0]["translatedText"];
            }
            catch { }
        }

        isTranslating = false;

        if (callback != null)
            callback(translatedText);
    }

    public void ScanFileAndTranslate()
    {
        List<TextAsset> files = Resources.LoadAll<TextAsset>(PATH_LOCALIZE_FILES).OrderBy(x => int.Parse(x.name.Split('-')[0])).ToList();

        for (int i = 0; i < files.Count; i++)
        {
            try
            {
                if (i > 1)
                {
                    string[] splits = files[i].name.Split('-');
                    string fileName = splits[1];
                    string langCode = Get2LetterISOCodeFromSystemLanguage((LanguageID)i);

                    Dictionary<string, string> dictLocalize = JsonConvert.DeserializeObject<Dictionary<string, string>>(files[i].text);
                    List<KeyValuePair<string, string>> pairNeedTranslate = dictLocalize.Where(x => string.IsNullOrEmpty(x.Key) == false && string.IsNullOrEmpty(x.Value)).ToList();
                    int total = pairNeedTranslate.Count;
                    int countCompleted = 0;

                    if (total > 0)
                    {
                        DebugCustom.LogFormat("Begin-{0}", fileName);
                        for (int j = 0; j < pairNeedTranslate.Count; j++)
                        {
                            string key = pairNeedTranslate[j].Key;
                            int languageId = i;
                            StartCoroutine(TranslateByGoogle(langCode, key, languageId, (translatedText) =>
                            {
                                translatedText = translatedText.Replace("color = ", "color=")
                                .Replace("< ", "<").Replace(" >", ">").Replace("</ ", "</")
                                .Replace(" :", ":")
                                .Replace("{ ", "{").Replace(" }", "}");

                                if (langCode == "JA")
                                {
                                    translatedText = translatedText.Replace("{0} {0}", "{0} </color>")
                                                                    .Replace(" color>", " </color>")
                                                                    .Replace("</color></color>", "</color>");
                                }
                                else if (langCode == "IN")
                                {
                                    translatedText = translatedText.Replace("<color=kuning>", "<color=yellow>");
                                }

                                dictLocalize[key] = translatedText;
                                countCompleted++;
                                if (countCompleted == total)
                                {
                                    DebugCustom.LogFormat("Done-{0}-{1}", fileName, countCompleted);

                                    string path = string.Format("{0}{1}.json", PATH_LOCALIZE_FILES_FULL, languageId + "-" + fileName);
                                    StreamWriter writer1 = new StreamWriter(path, false);
                                    writer1.WriteLine(JsonConvert.SerializeObject(dictLocalize, Formatting.Indented));
                                    writer1.Close();

#if UNITY_EDITOR
                                    AssetDatabase.ImportAsset(path);
#endif
                                }
                            }));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugCustom.Log(e.Message);
            }
        }
    }

    public string Get2LetterISOCodeFromSystemLanguage(SystemLanguage lang)
    {
        string res = "EN";
        switch (lang)
        {
            case SystemLanguage.Afrikaans: res = "AF"; break;
            case SystemLanguage.Arabic: res = "AR"; break;
            case SystemLanguage.Basque: res = "EU"; break;
            case SystemLanguage.Belarusian: res = "BY"; break;
            case SystemLanguage.Bulgarian: res = "BG"; break;
            case SystemLanguage.Catalan: res = "CA"; break;
            case SystemLanguage.Chinese: res = "ZH"; break;
            case SystemLanguage.Czech: res = "CS"; break;
            case SystemLanguage.Danish: res = "DA"; break;
            case SystemLanguage.Dutch: res = "NL"; break;
            case SystemLanguage.English: res = "EN"; break;
            case SystemLanguage.Estonian: res = "ET"; break;
            case SystemLanguage.Faroese: res = "FO"; break;
            case SystemLanguage.Finnish: res = "FI"; break;
            case SystemLanguage.French: res = "FR"; break;
            case SystemLanguage.German: res = "DE"; break;
            case SystemLanguage.Greek: res = "EL"; break;
            case SystemLanguage.Hebrew: res = "IW"; break;
            case SystemLanguage.Hungarian: res = "HU"; break;
            case SystemLanguage.Icelandic: res = "IS"; break;
            case SystemLanguage.Indonesian: res = "IN"; break;
            case SystemLanguage.Italian: res = "IT"; break;
            case SystemLanguage.Japanese: res = "JA"; break;
            case SystemLanguage.Korean: res = "KO"; break;
            case SystemLanguage.Latvian: res = "LV"; break;
            case SystemLanguage.Lithuanian: res = "LT"; break;
            case SystemLanguage.Norwegian: res = "NO"; break;
            case SystemLanguage.Polish: res = "PL"; break;
            case SystemLanguage.Portuguese: res = "PT"; break;
            case SystemLanguage.Romanian: res = "RO"; break;
            case SystemLanguage.Russian: res = "RU"; break;
            case SystemLanguage.SerboCroatian: res = "SH"; break;
            case SystemLanguage.Slovak: res = "SK"; break;
            case SystemLanguage.Slovenian: res = "SL"; break;
            case SystemLanguage.Spanish: res = "ES"; break;
            case SystemLanguage.Swedish: res = "SV"; break;
            case SystemLanguage.Thai: res = "TH"; break;
            case SystemLanguage.Turkish: res = "TR"; break;
            case SystemLanguage.Ukrainian: res = "UK"; break;
            case SystemLanguage.Unknown: res = "EN"; break;
            case SystemLanguage.Vietnamese: res = "VI"; break;
        }

        return res;
    }

    public string Get2LetterISOCodeFromSystemLanguage(LanguageID id)
    {
        SystemLanguage lang = SystemLanguage.English;

        switch (id)
        {
            case LanguageID.Vietnamese: lang = SystemLanguage.Vietnamese; break;
            case LanguageID.Korean: lang = SystemLanguage.Korean; break;
            case LanguageID.Russian: lang = SystemLanguage.Russian; break;
            case LanguageID.Chinese: lang = SystemLanguage.Chinese; break;
            case LanguageID.Japanese: lang = SystemLanguage.Japanese; break;
            case LanguageID.French: lang = SystemLanguage.French; break;
            case LanguageID.German: lang = SystemLanguage.German; break;
            case LanguageID.Portuguese: lang = SystemLanguage.Portuguese; break;
            case LanguageID.Italian: lang = SystemLanguage.Italian; break;
            case LanguageID.Indonesian: lang = SystemLanguage.Indonesian; break;
            case LanguageID.Thai: lang = SystemLanguage.Thai; break;
            case LanguageID.Polish: lang = SystemLanguage.Polish; break;
            case LanguageID.Spanish: lang = SystemLanguage.Spanish; break;
            case LanguageID.Turkish: lang = SystemLanguage.Turkish; break;
        }

        return Get2LetterISOCodeFromSystemLanguage(lang);
    }
    #endregion
}
