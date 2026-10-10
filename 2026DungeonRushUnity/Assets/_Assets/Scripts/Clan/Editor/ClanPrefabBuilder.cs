using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Dựng prefab UI Clan + Clan War từ prefab rip layout-only (Prefabs/UI/Clan|ClanWar|Forge|Profile|Common) và bảng wiring
// GỐC clan_fieldmap.json (tools/rip_fieldmap.py trích từ AssetRipper: mỗi MonoBehaviour game → field → đường dẫn node).
// Class/field UI project đặt TRÙNG tên gốc nên nối tự động bằng reflection. Chạy lại được (ghi đè).
// Menu: Tools/DungeonRush/Build Clan UI Prefabs.
public static class ClanPrefabBuilder
{
    private const string MAP = "Assets/_Assets/Scripts/Clan/Editor/clan_fieldmap.json";
    private const string BUILT = "Assets/_Assets/Prefabs/UI/Clan/Built";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string CATALOG = "Assets/_Assets/Resources/Scriptable Objects/UI/ClanBannerCatalog.asset";
    private const string RES = "Assets/_Assets/_ResourceGame/";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private static readonly string[] RIP_DIRS =
    {
        "Assets/_Assets/Prefabs/UI/Clan/", "Assets/_Assets/Prefabs/UI/ClanWar/", "Assets/_Assets/Prefabs/UI/Forge/",
        "Assets/_Assets/Prefabs/UI/Profile/", "Assets/_Assets/Prefabs/UI/Common/",
    };

    // Component phụ của gốc không port (hiệu ứng nhấn / nhãn localize tĩnh / chấm đỏ — xử lý thẳng trong script).
    private static readonly HashSet<string> SKIP = new HashSet<string> { "PressButtonUI", "LocalizedLabel", "NotificationUI" };

    // Thẻ con dựng trước (được popup/tab tham chiếu làm template).
    private static readonly string[] CARDS =
    {
        "TabSelectorItemUI", "ClanBannerOptionItem", "ClanMemberCard", "ClanRequestCard", "ClanSearchCard", "ClanWarActionCard",
        "ClanWarRewardCell", "ClanWarClanRowCard", "ClanWarContributionCard", "ClanWarDayCard", "ClanWarEnemyCard", "ClanWarMilestoneCard",
    };

    private static readonly string[] POPUPS =
    {
        "CreateClanPopup", "ClanSettingsPopup", "ClanBannerEditorPopup", "EditAnnouncementPopup", "ClanRequestsPopup", "ClanInfoPopup",
        "ClanPlayerPopup", "AdvancedSearchPopup", "ClanContributionLeaderboardPopup", "ClanWarDaysAndResultPopup", "ClanWarLeaderboardPopup",
        "ClanWarRewardsPopup", "LeadershipRankPopup",
    };

    // Field icon thưởng → tên sprite GỐC (đọc từ xapk). coin_icon (Bone) không có trong project → bone_icon.
    private static readonly Dictionary<string, string> ICONS = new Dictionary<string, string>
    {
        { "LootBoxIcon", "box_icon" }, { "BonesIcon", "bone_icon" }, { "PickaxeIcon", "pickaxe" }, { "ExperienceIcon", "exp-shard" },
        { "CloakCurrencyIcon", "crown_icon" }, { "GoldenPickaxeIcon", "gold_pickaxe" }, { "DrillIcon", "drill" }, { "VialIcon", "vial_icon" },
    };

    private static JObject map;
    private static ClanBannerCatalog catalog;
    private static readonly Dictionary<Type, GameObject> builtCards = new Dictionary<Type, GameObject>();
    private static List<string> log;

    [MenuItem("Tools/DungeonRush/Build Clan UI Prefabs")]
    public static string BuildAll()
    {
        log = new List<string>();
        map = JObject.Parse(File.ReadAllText(MAP));
        builtCards.Clear();
        EnsureFolder(BUILT);
        catalog = BuildCatalog();

        foreach (string card in CARDS) BuildCard(card);
        BuildPage("ClanTabPage");
        foreach (string popup in POPUPS) BuildPopup(popup);

        AssetDatabase.SaveAssets();
        string result = string.Join("\n", log.ToArray());
        Debug.Log("[ClanPrefabBuilder]\n" + result);
        return result;
    }

    // ===== Catalog banner (thứ tự gốc DecodedData/tables/ClanBannerCatalog.names.json) =====

    private static ClanBannerCatalog BuildCatalog()
    {
        ClanBannerCatalog c = AssetDatabase.LoadAssetAtPath<ClanBannerCatalog>(CATALOG);
        if (c == null)
        {
            c = ScriptableObject.CreateInstance<ClanBannerCatalog>();
            AssetDatabase.CreateAsset(c, CATALOG);
        }
        // Thứ tự sprite + màu GỐC (UnityPy đọc ClanBannerCatalog từ xapk).
        JObject src = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "../../DecodedData/tables/ClanBannerCatalog.names.json")));
        string dir = RES + "Clan/";
        c.backgroundSprites = src["backgroundSprites"].Select(n => LoadSprite(dir + (string)n + ".png")).ToArray();
        c.backgroundOverlaySprites = src["backgroundOverlaySprites"].Select(n => LoadSprite(dir + (string)n + ".png")).ToArray();
        c.imageSprites = src["imageSprites"].Select(n => LoadSprite(dir + (string)n + ".png")).ToArray();
        c.backgroundColors = src["backgroundColors"].Select(ToColor).ToArray();
        c.imageColors = src["imageColors"].Select(ToColor).ToArray();
        EditorUtility.SetDirty(c);
        log.Add("ClanBannerCatalog: " + c.backgroundSprites.Count(s => s != null) + " bg, " + c.imageSprites.Count(s => s != null) + " icon");
        return c;
    }

    private static Color ToColor(JToken t) => new Color((float)t["r"], (float)t["g"], (float)t["b"], (float)t["a"]);

    // ===== Dựng =====

    private static void BuildCard(string name)
    {
        GameObject root = InstantiateRip(name);
        if (root == null) return;
        List<Component> comps = Wire(name, root.transform, null);
        Type t = comps.Count > 0 ? comps[0].GetType() : null;
        string path = BUILT + "/" + name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        if (ok && t != null) builtCards[t] = saved;
        log.Add(name + (ok ? " → " + path : " FAILED"));
    }

    private static void BuildPage(string name)
    {
        GameObject root = InstantiateRip(name);
        if (root == null) return;
        Wire(name, root.transform, null);
        Save(root, name);
    }

    private static void BuildPopup(string name)
    {
        GameObject rip = InstantiateRip(name);
        if (rip == null) return;
        string uiName = UiNameOf(name);
        GameObject root = NewRoot(uiName);
        rip.transform.SetParent(root.transform.Find("ContentAll"), false);
        List<Component> comps = Wire(name, rip.transform, root);
        BaseUI ui = root.GetComponent<BaseUI>();
        if (ui != null) ui.isPopup = true;
        if (comps.Count == 0) log.Add("  ! " + name + ": không gắn được component nào");
        Save(root, uiName);
    }

    private static string UiNameOf(string ripName)
    {
        return ripName == "ClanWarDaysAndResultPopup" ? "UIClanWarDaysAndResultsPopup" : "UI" + ripName;
    }

    // ===== Nối field theo bảng gốc =====

    // entries: [{class, path, fields}] sắp theo độ sâu. wrapper != null → component gốc ở root gắn lên wrapper (BaseUI).
    private static List<Component> Wire(string prefabName, Transform ripRoot, GameObject wrapper)
    {
        JArray entries = (JArray)map[prefabName];
        List<Component> result = new List<Component>();
        if (entries == null)
        {
            log.Add("  ! thiếu " + prefabName + " trong fieldmap");
            return result;
        }

        // Pass 1: gắn component.
        Dictionary<JObject, Component> comps = new Dictionary<JObject, Component>();
        foreach (JObject e in entries)
        {
            string cls = (string)e["class"];
            string path = (string)e["path"];
            if (SKIP.Contains(cls)) continue;
            Type t = FindType(MapClass(cls, path == "" && wrapper != null));
            if (t == null)
            {
                log.Add("  ! " + prefabName + ": không có class " + cls);
                continue;
            }
            GameObject go = path == "" && wrapper != null ? wrapper : FindNode(ripRoot, path)?.gameObject;
            if (go == null)
            {
                log.Add("  ! " + prefabName + ": không thấy node " + path);
                continue;
            }
            Component c = go.GetComponent(t) ?? go.AddComponent(t);
            comps[e] = c;
            result.Add(c);
        }

        // Pass 2: gán field.
        int set = 0;
        foreach (KeyValuePair<JObject, Component> kv in comps)
        {
            Component c = kv.Value;
            Type t = c.GetType();
            foreach (JProperty p in ((JObject)kv.Key["fields"]).Properties())
            {
                FieldInfo fi = FindField(t, p.Name);
                if (fi == null) continue;
                if (Assign(c, fi, p.Value, ripRoot, prefabName)) set++;
            }
            EditorUtility.SetDirty(c);
        }
        log.Add(prefabName + ": " + result.Count + " component, " + set + " field");
        return result;
    }

    private static bool Assign(Component c, FieldInfo fi, JToken v, Transform ripRoot, string prefabName)
    {
        Type ft = fi.FieldType;
        if (v.Type == JTokenType.Object)
        {
            JObject o = (JObject)v;
            if (o["null"] != null) return false;
            if (o["path"] != null)
            {
                Transform node = FindNode(ripRoot, (string)o["path"]);
                if (node == null)
                {
                    log.Add("  ! " + prefabName + "." + fi.Name + ": thiếu node " + o["path"]);
                    return false;
                }
                object val = ft == typeof(GameObject) ? node.gameObject : (object)(typeof(Component).IsAssignableFrom(ft) ? node.GetComponent(ft) : null);
                if (val == null || (val is Object uo && uo == null))
                {
                    log.Add("  ! " + prefabName + "." + c.GetType().Name + "." + fi.Name + ": node " + o["path"] + " không có " + ft.Name);
                    return false;
                }
                fi.SetValue(c, val);
                AfterAssign(fi, val);
                return true;
            }
            if (o["external"] != null) return AssignExternal(c, fi, prefabName);
            return false;
        }

        string s = (string)v;
        if (string.IsNullOrEmpty(s)) return false;
        if (ft == typeof(float) && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f)) fi.SetValue(c, f);
        else if (ft == typeof(int) && int.TryParse(s, out int i)) fi.SetValue(c, i);
        else if (ft == typeof(bool)) fi.SetValue(c, s == "1");
        else if (ft == typeof(Color)) fi.SetValue(c, ParseColor(s));
        else return false;
        return true;
    }

    // Tham chiếu ra ngoài prefab: catalog banner / sprite icon / template thẻ con.
    private static bool AssignExternal(Component c, FieldInfo fi, string prefabName)
    {
        Type ft = fi.FieldType;
        if (ft == typeof(ClanBannerCatalog))
        {
            fi.SetValue(c, catalog);
            return true;
        }
        if (ft == typeof(Sprite))
        {
            if (!ICONS.TryGetValue(fi.Name, out string spriteName)) return false;
            Sprite sp = FindSprite(spriteName);
            if (sp == null) log.Add("  ! " + prefabName + "." + fi.Name + ": không thấy sprite " + spriteName);
            fi.SetValue(c, sp);
            return sp != null;
        }
        if (typeof(Component).IsAssignableFrom(ft) && builtCards.TryGetValue(ft, out GameObject cardPrefab))
        {
            // Template ẩn ngay dưới component (Instantiate(template, ListContent) ở runtime) — không trỏ asset để SetActive an toàn.
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, c.transform);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = ft.Name + "Template";
            go.SetActive(false);
            fi.SetValue(c, go.GetComponent(ft));
            return true;
        }
        log.Add("  ! " + prefabName + "." + fi.Name + ": tham chiếu ngoài chưa map (" + ft.Name + ")");
        return false;
    }

    // Chữ có "<sprite=0>" cần TMP sprite asset.
    private static void AfterAssign(FieldInfo fi, object val)
    {
        if (!(val is TMP_Text text)) return;
        string n = fi.Name;
        if (n.Contains("Power")) text.spriteAsset = LoadIcon("IsPower");
        else if (n == "PvpTicketText") text.spriteAsset = LoadIcon("IsTicket");
        else if (n == "CreateButtonText") text.spriteAsset = LoadIcon("IsGem");
        if (n.EndsWith("Text")) text.raycastTarget = false;
    }

    // ===== Helper =====

    private static string MapClass(string cls, bool isRootOfPopup)
    {
        if (isRootOfPopup) return cls == "ClanWarDaysAndResultsPopup" ? "UIClanWarDaysAndResultsPopup" : "UI" + cls;
        return cls;
    }

    private static Type FindType(string name)
    {
        foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!a.GetName().Name.StartsWith("Assembly-CSharp")) continue;
            Type t = a.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    private static FieldInfo FindField(Type t, string name)
    {
        for (Type x = t; x != null && x != typeof(MonoBehaviour); x = x.BaseType)
        {
            FieldInfo fi = x.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (fi != null) return fi;
        }
        return null;
    }

    private static GameObject InstantiateRip(string name)
    {
        foreach (string dir in RIP_DIRS)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dir + name + ".prefab");
            if (prefab == null) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            return go;
        }
        log.Add("  ! không thấy prefab rip " + name);
        return null;
    }

    private static Transform FindNode(Transform root, string path)
    {
        return string.IsNullOrEmpty(path) ? root : root.Find(path);
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn) như UIPvP*/UIBossRush*.
    private static GameObject NewRoot(string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        return root;
    }

    private static void Save(GameObject root, string name)
    {
        string path = OUT + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        log.Add(name + (ok ? " saved → " + path : " FAILED"));
    }

    private static Color ParseColor(string s)
    {
        // "{r: 1, g: 0.5, b: 0, a: 1}"
        float Get(string key)
        {
            int i = s.IndexOf(key + ":", StringComparison.Ordinal);
            if (i < 0) return 1f;
            int start = i + key.Length + 1;
            int end = s.IndexOfAny(new[] { ',', '}' }, start);
            float.TryParse(s.Substring(start, end - start).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v);
            return v;
        }
        return new Color(Get("r"), Get("g"), Get("b"), Get("a"));
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) log.Add("  ! thiếu sprite " + path);
        return s;
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/_Assets/_ResourceGame" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        return null;
    }

    private static TMP_SpriteAsset LoadIcon(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + assetName + ".asset");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
