using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Công cụ Localization theo game gốc (Unity Localization + LocalizedLabel):
//  1. Build Fallback Fonts: tạo font TMP động từ font Noto gốc (Arabic/JP/KR) rồi gắn vào fallback toàn cục
//     của TMP Settings — gốc cũng dùng fallback toàn cục (10 font Noto) cho chữ ngoài bảng Latin.
//  2. Apply Original Label Keys: gốc gắn LocalizedLabel (Table + Key) lên các nhãn tĩnh (263 chỗ, dữ liệu trích ở
//     DecodedData/localization/labels_original.json). Tool dò từng TMP trong prefab UI của project: cùng tên node +
//     cùng chữ tiếng Anh với nhãn gốc → gắn LocalizeText.key. Trùng nhiều key thì lấy nhãn có đường dẫn cha khớp dài nhất.
//     Chạy lại an toàn (chỉ sửa chỗ chưa có / sai key).
public static class LocalizationBuilder
{
    private const string FONT_DIR = "Assets/_Assets/Fonts/NotoSans/";
    private const string TMP_SETTINGS = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private static readonly string[] FALLBACK_FONTS = { "NotoSansArabic-Bold", "NotoSansJP-Bold", "NotoSansKR-Bold" };

    // Prefab UI đang dùng: lobby (Prefabs/UI/*.prefab cấp 1), popup Resources, prefab tham chiếu trực tiếp UINew.
    private static readonly string[] PREFAB_ROOTS = { "Assets/_Assets/Resources/Prefabs/UI", "Assets/_Assets/Prefabs/UINew" };
    private const string LOBBY_DIR = "Assets/_Assets/Prefabs/UI";

    [MenuItem("Tools/DungeonRush/Localization/Build Fallback Fonts")]
    public static string BuildFallbackFonts()
    {
        List<TMP_FontAsset> fonts = new List<TMP_FontAsset>();
        foreach (string name in FALLBACK_FONTS)
        {
            string outPath = FONT_DIR + name + " SDF.asset";
            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (fa == null)
            {
                Font font = AssetDatabase.LoadAssetAtPath<Font>(FONT_DIR + name + ".ttf");
                if (font == null) throw new System.Exception("[LocalizationBuilder] Thiếu font " + name + ".ttf");
                // Thông số gần font gốc (NotoSansJP-Bold SDF: point 87, padding 9, động, nhiều atlas).
                fa = TMP_FontAsset.CreateFontAsset(font, 87, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fa.name = name + " SDF";
                AssetDatabase.CreateAsset(fa, outPath);
                fa.atlasTextures[0].name = name + " SDF Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
                fa.material.name = name + " SDF Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            // Atlas động không mang vào bản build (sinh lại lúc chạy) để repo/bản build không phình.
            SerializedObject so = new SerializedObject(fa);
            so.FindProperty("m_ClearDynamicDataOnBuild").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fa);
            fonts.Add(fa);
        }

        SerializedObject settings = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(TMP_SETTINGS));
        SerializedProperty list = settings.FindProperty("m_fallbackFontAssets");
        list.arraySize = fonts.Count;
        for (int i = 0; i < fonts.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = fonts[i];
        }
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        string result = "fallback fonts: " + string.Join(", ", fonts.Select(x => x.name).ToArray());
        Debug.Log("[LocalizationBuilder] " + result);
        return result;
    }

    private class Label
    {
        public string file;
        public string go;
        public string path;
        public string key;
        public string text;
        public string en;
    }

    [MenuItem("Tools/DungeonRush/Localization/Apply Original Label Keys")]
    public static string ApplyOriginalLabelKeys()
    {
        string jsonPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../DecodedData/localization/labels_original.json"));
        List<Label> labels = JsonConvert.DeserializeObject<List<Label>>(File.ReadAllText(jsonPath));
        Dictionary<string, List<Label>> index = new Dictionary<string, List<Label>>();
        foreach (Label l in labels)
        {
            foreach (string t in new[] { l.text, l.en })
            {
                if (string.IsNullOrEmpty(t)) continue;
                string k = l.go + "|" + Norm(t);
                if (!index.ContainsKey(k)) index[k] = new List<Label>();
                if (!index[k].Contains(l)) index[k].Add(l);
            }
        }

        List<string> prefabs = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", PREFAB_ROOTS))
            prefabs.Add(AssetDatabase.GUIDToAssetPath(guid));
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { LOBBY_DIR }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(p).Replace('\\', '/') == LOBBY_DIR) prefabs.Add(p);
        }

        StringBuilder report = new StringBuilder();
        int total = 0;
        foreach (string path in prefabs.Distinct())
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int changed = 0;
            foreach (TMP_Text tmp in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Nhãn nằm trong prefab lồng → để prefab nguồn tự gắn (tránh override trùng).
                if (PrefabUtility.IsPartOfPrefabInstance(tmp)) continue;
                List<Label> cands;
                if (!index.TryGetValue(tmp.gameObject.name + "|" + Norm(tmp.text), out cands)) continue;
                Label best = Pick(cands, GetPath(tmp.transform), Path.GetFileNameWithoutExtension(path));
                LocalizeText lt = tmp.GetComponent<LocalizeText>();
                if (lt != null && lt.key == best.key && lt.sourceText == tmp.text) continue;
                if (lt == null) lt = tmp.gameObject.AddComponent<LocalizeText>();
                lt.key = best.key;
                lt.sourceText = tmp.text;
                changed++;
            }
            if (changed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine(Path.GetFileName(path) + ": " + changed);
                total += changed;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
        string result = "gắn key " + total + " nhãn\n" + report;
        Debug.Log("[LocalizationBuilder] " + result);
        return result;
    }

    // Nhãn gốc khớp nhất: nhiều đoạn đường dẫn cha trùng nhất, rồi tên file gốc nằm trong tên prefab project.
    private static Label Pick(List<Label> cands, string path, string prefabName)
    {
        string[] mine = path.Split('/');
        string lowerName = prefabName.ToLowerInvariant();
        Label best = null;
        int bestScore = -1;
        foreach (Label l in cands)
        {
            string[] rip = (l.path ?? l.go).Split('/');
            int score = 0;
            while (score < mine.Length && score < rip.Length && mine[mine.Length - 1 - score] == rip[rip.Length - 1 - score]) score++;
            string stem = Path.GetFileNameWithoutExtension(l.file).Replace("Popup", "").ToLowerInvariant();
            score = score * 2 + (stem.Length > 0 && lowerName.Contains(stem) ? 1 : 0);
            if (score > bestScore)
            {
                bestScore = score;
                best = l;
            }
        }
        return best;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            p = t.name + "/" + p;
        }
        return p;
    }

    private static string Norm(string s)
    {
        return Regex.Replace((s ?? "").Trim(), @"\s+", " ").ToLowerInvariant();
    }
}
