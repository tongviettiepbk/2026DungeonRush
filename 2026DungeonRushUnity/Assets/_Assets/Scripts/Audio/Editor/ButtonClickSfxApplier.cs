using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Gắn ButtonClickSfx lên đúng các nút mà prefab gốc có PressButtonUI.
// button_click_sfx.json = danh sách (prefab, đường dẫn nút trong prefab; "" = chính root), sinh bằng cách
// khớp đường dẫn nút project với prefab rip gốc (AssetRipper v41) + cảnh GameplayScene gốc.
// Chạy lại được sau khi builder dựng lại prefab: nút đã có component thì bỏ qua.
public static class ButtonClickSfxApplier
{
    private const string LIST = "Assets/_Assets/Scripts/Audio/Editor/button_click_sfx.json";

    [Serializable]
    private class Entry
    {
        public string prefab;
        public string path;
    }

    [Serializable]
    private class EntryList
    {
        public List<Entry> buttons = new List<Entry>();
    }

    [MenuItem("Tools/DungeonRush/Apply Button Click Sfx")]
    public static string Apply()
    {
        EntryList list = JsonUtility.FromJson<EntryList>(File.ReadAllText(LIST));
        Dictionary<string, List<string>> byPrefab = new Dictionary<string, List<string>>();
        foreach (Entry e in list.buttons)
        {
            if (!byPrefab.TryGetValue(e.prefab, out List<string> paths))
            {
                paths = new List<string>();
                byPrefab[e.prefab] = paths;
            }
            paths.Add(e.path);
        }

        int added = 0, existed = 0, missing = 0;
        foreach (KeyValuePair<string, List<string>> pair in byPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(pair.Key);
            if (root == null)
            {
                missing += pair.Value.Count;
                Debug.LogWarning("[ButtonClickSfx] không mở được prefab " + pair.Key);
                continue;
            }

            bool changed = false;
            foreach (string path in pair.Value)
            {
                Transform t = string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
                if (t == null || t.GetComponent<Button>() == null)
                {
                    missing++;
                    Debug.LogWarning("[ButtonClickSfx] không thấy nút " + pair.Key + " :: " + path);
                    continue;
                }

                if (t.GetComponent<ButtonClickSfx>() != null)
                {
                    existed++;
                    continue;
                }

                t.gameObject.AddComponent<ButtonClickSfx>();
                added++;
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, pair.Key);
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        string result = string.Format("[ButtonClickSfx] thêm {0}, đã có {1}, không thấy {2}", added, existed, missing);
        Debug.Log(result);
        return result;
    }
}
