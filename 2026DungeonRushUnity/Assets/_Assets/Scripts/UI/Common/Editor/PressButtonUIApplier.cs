using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Gắn PressButtonUI lên đúng các nút mà prefab gốc có PressButtonUI, kèm PressYValue gốc.
// press_buttons.json = danh sách (prefab, đường dẫn nút trong prefab; "" = chính root) sinh bằng cách khớp
// đường dẫn nút project với prefab rip gốc (AssetRipper v41) + cảnh GameplayScene gốc:
//   pressY = PressYValue gốc, ripChildY = y gốc của con đầu tiên, ripChild = tên con đầu tiên gốc.
// PressYValue gán = y hiện tại của con đầu tiên trong project + (pressY - ripChildY): bố cục giữ như gốc thì
// ra đúng số gốc, reskin đổi y con thì vẫn lún cùng một khoảng.
// Chạy lại được sau khi builder dựng lại prefab (ghi đè PressYValue theo bảng).
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

        int applied = 0, missing = 0, noChild = 0, otherChild = 0;
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

                PressButtonUI press = t.GetComponent<PressButtonUI>();
                if (press == null)
                {
                    press = t.gameObject.AddComponent<PressButtonUI>();
                }

                RectTransform child = t.childCount > 0 ? t.GetChild(0) as RectTransform : null;
                if (child == null)
                {
                    noChild++;
                    press.PressYValue = e.pressY;
                    Debug.LogWarning("[PressButtonUI] nút không có con để lún: " + pair.Key + " :: " + e.path);
                }
                else
                {
                    press.PressYValue = child.anchoredPosition.y + (e.pressY - e.ripChildY);
                    if (!string.IsNullOrEmpty(e.ripChild) && child.name != e.ripChild)
                    {
                        otherChild++;
                        Debug.LogWarning("[PressButtonUI] con đầu tiên khác gốc (" + child.name + " ≠ " + e.ripChild + "): " + pair.Key + " :: " + e.path);
                    }
                }
                applied++;
            }

            PrefabUtility.SaveAsPrefabAsset(root, pair.Key);
            PrefabUtility.UnloadPrefabContents(root);
        }

        string result = string.Format("[PressButtonUI] gắn {0} nút, không thấy {1}, không có con {2}, con khác gốc {3}",
            applied, missing, noChild, otherChild);
        Debug.Log(result);
        return result;
    }
}
