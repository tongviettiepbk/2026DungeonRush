using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Gắn PressButtonUI lên đúng các nút mà prefab gốc có PressButtonUI.
// press_buttons.json = danh sách (prefab, đường dẫn nút trong prefab; "" = chính root) sinh bằng cách khớp
// đường dẫn nút project với prefab rip gốc (AssetRipper v41) + cảnh GameplayScene gốc.
// (Các cột pressY/ripChildY/ripChild là số lún gốc — giờ nút co/giãn cả nút nên không dùng.)
// Chạy lại được sau khi builder dựng lại prefab.
public static class PressButtonUIApplier
{
    private const string LIST = "Assets/_Assets/Scripts/UI/Common/Editor/press_buttons.json";

    [Serializable]
    private class Entry
    {
        public string prefab;
        public string path;
        public float pressY;
        public float ripChildY;
        public string ripChild;
    }

    [Serializable]
    private class EntryList
    {
        public List<Entry> buttons = new List<Entry>();
    }

    [MenuItem("Tools/DungeonRush/Apply Press Buttons")]
    public static string Apply()
    {
        EntryList list = JsonUtility.FromJson<EntryList>(File.ReadAllText(LIST));
        Dictionary<string, List<Entry>> byPrefab = new Dictionary<string, List<Entry>>();
        foreach (Entry e in list.buttons)
        {
            if (!byPrefab.TryGetValue(e.prefab, out List<Entry> entries))
            {
                entries = new List<Entry>();
                byPrefab[e.prefab] = entries;
            }
            entries.Add(e);
        }

        int applied = 0, missing = 0;
        foreach (KeyValuePair<string, List<Entry>> pair in byPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(pair.Key);
            if (root == null)
            {
                missing += pair.Value.Count;
                Debug.LogWarning("[PressButtonUI] không mở được prefab " + pair.Key);
                continue;
            }

            foreach (Entry e in pair.Value)
            {
                Transform t = string.IsNullOrEmpty(e.path) ? root.transform : root.transform.Find(e.path);
                if (t == null || t.GetComponent<Button>() == null)
                {
                    missing++;
                    Debug.LogWarning("[PressButtonUI] không thấy nút " + pair.Key + " :: " + e.path);
                    continue;
                }

                if (t.GetComponent<PressButtonUI>() == null)
                {
                    t.gameObject.AddComponent<PressButtonUI>();
                }
                applied++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, pair.Key);
            PrefabUtility.UnloadPrefabContents(root);
        }

        string result = string.Format("[PressButtonUI] gắn {0} nút, không thấy {1}", applied, missing);
        Debug.Log(result);
        return result;
    }
}
