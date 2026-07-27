#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using System;

public class LocalizeEditorUtils : Editor
{
    [MenuItem("Editor Utils/Localize/Scan Texts In Scene")]
    public static void ScanTextInScene()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(LocalizeManager.PATH_LOCALIZE_FILES + "0-English");
        Dictionary<string, string> curDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text);

        Text[] texts = Resources.FindObjectsOfTypeAll<Text>() as Text[];
        for (int i = 0; i < texts.Length; i++)
        {
            Text tx = texts[i];

            if (tx.hideFlags == HideFlags.NotEditable || tx.hideFlags == HideFlags.HideAndDontSave)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(tx.gameObject))
            {
                continue;
            }

            if (string.IsNullOrEmpty(tx.text))
            {
                continue;
            }

            int number = 0;
            if (int.TryParse(tx.text, System.Globalization.NumberStyles.AllowThousands, null, out number))
            {
                continue;
            }

            if (curDictionary.ContainsKey(tx.text) == false)
            {
                DebugCustom.Log("NewKey=" + tx.text);
                curDictionary.Add(tx.text, string.Empty);
            }

            LocalizeText localizeComponent = tx.GetComponent<LocalizeText>();
            if (localizeComponent == null)
            {
                tx.gameObject.AddComponent<LocalizeText>();
            }
        }

        WriteLanguageFile(JsonConvert.SerializeObject(curDictionary));
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Editor Utils/Localize/Validate Localize Files")]
    public static void ValidateLocalizeFiles()
    {
        string path = LocalizeManager.PATH_LOCALIZE_FILES;
        TextAsset[] files = Resources.LoadAll<TextAsset>(path);
        TextAsset jsonEnglish = Resources.Load<TextAsset>(path + "0-English");
        Dictionary<string, string> dictEnglish = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonEnglish.text);

        TextAsset[] additionalFiles = Resources.LoadAll<TextAsset>(LocalizeManager.PATH_ADDITIONAL_FILES);
        for (int i = 0; i < additionalFiles.Length; i++)
        {
            Dictionary<string, string> dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(additionalFiles[i].text);
            var e = dict.GetEnumerator();
            while (e.MoveNext())
            {
                string key = e.Current.Key;
                string value = e.Current.Value;

                if (key.StartsWith("/*"))
                {
                    continue;
                }

                if (dictEnglish.ContainsKey(key))
                {
                    DebugCustom.LogError("Exists key=" + key);
                }

                dictEnglish[key] = value;
            }
        }

        for (int i = 0; i < files.Length; i++)
        {
            try
            {
                string[] splits = files[i].name.Split('-');
                string id = splits[0];
                string fileName = splits[1];

                if (fileName == "English")
                {
                    //writer.WriteLine(JsonConvert.SerializeObject(dictEnglish, Formatting.Indented));
                    //writer.Close();
                    //AssetDatabase.ImportAsset(savePath);
                    continue;
                }

                DebugCustom.Log("Validate " + fileName);
                Dictionary<string, string> dictLocalize = JsonConvert.DeserializeObject<Dictionary<string, string>>(files[i].text);
                Dictionary<string, string> verifyDict = new Dictionary<string, string>();
                List<string> emptyKeys = new List<string>();

                var e = dictEnglish.GetEnumerator();
                while (e.MoveNext())
                {
                    string key = e.Current.Key;

                    if (dictLocalize.ContainsKey(key))
                    {
                        string value = dictLocalize[key];

                        if (string.IsNullOrEmpty(key) == false && string.IsNullOrEmpty(value))
                        {
                            emptyKeys.Add(key);
                        }
                        else
                        {
                            verifyDict[key] = dictLocalize[key];
                        }
                    }
                    else
                    {
                        emptyKeys.Add(key);
                    }
                }

                for (int j = 0; j < emptyKeys.Count; j++)
                {
                    verifyDict[emptyKeys[j]] = "";
                }

                string savePath = string.Format("{0}{1}.json", LocalizeManager.PATH_LOCALIZE_FILES_FULL, id + "-" + fileName);
                StreamWriter writer = new StreamWriter(savePath, false);
                writer.WriteLine(JsonConvert.SerializeObject(verifyDict, Formatting.Indented));
                writer.Close();
                AssetDatabase.ImportAsset(savePath);
            }
            catch
            {
                Debug.LogError("Cannot validate =" + files[i]);
            }
        }
    }

    private static void WriteLanguageFile(string content)
    {
#if UNITY_EDITOR
        string path = LocalizeManager.PATH_LOCALIZE_FILES_FULL + "0-English.json";
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(content);
        writer.Close();
        AssetDatabase.ImportAsset(path);
#endif
    }

    [MenuItem("Editor Utils/Localize/Scan File And Translate")]
    public static void ScanFileAndTranslate()
    {
        string msg = string.Empty;

        List<TextAsset> files = Resources.LoadAll<TextAsset>(LocalizeManager.PATH_LOCALIZE_FILES).OrderBy(x => int.Parse(x.name.Split('-')[0])).ToList();

        for (int i = 0; i < files.Count; i++)
        {
            if (i > 1)
            {
                string[] splits = files[i].name.Split('-');
                string fileName = splits[1];

                Dictionary<string, string> dictLocalize = JsonConvert.DeserializeObject<Dictionary<string, string>>(files[i].text);
                List<KeyValuePair<string, string>> pairNeedTranslate = dictLocalize.Where(x => string.IsNullOrEmpty(x.Key) == false && string.IsNullOrEmpty(x.Value)).ToList();

                if (pairNeedTranslate.Count > 0)
                {
                    msg += string.Format("{0} - {1} lines", fileName, pairNeedTranslate.Count);
                    msg += "\n";
                }
            }
        }

        if (EditorUtility.DisplayDialog("Auto Translate", msg, "Ok", "Cancel"))
        {
            LocalizeManager.Instance.ScanFileAndTranslate();
        }
    }
}
#endif