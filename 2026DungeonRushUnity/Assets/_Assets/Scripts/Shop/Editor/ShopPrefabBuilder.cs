using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Dựng Store từ bản gốc: asset rương (ChestData / ChestBundleData / ChestConfig theo DecodedData/tables/ChestCatalog.names.json),
// prefab UI từ prefab rip layout-only (Prefabs/UI/Shop) + bảng wiring GỐC shop_fieldmap.json (tools/rip_fieldmap.py) —
// class/field UI đặt TRÙNG tên gốc nên nối bằng reflection; shop_tmpsprites.json = TMP sprite asset gốc của từng dòng chữ.
// Chạy lại được (ghi đè). Menu: Tools/DungeonRush/Build Shop UI Prefabs.
public static class ShopPrefabBuilder
{
    private const string DIR = "Assets/_Assets/Scripts/Shop/Editor/";
    private const string RIP = "Assets/_Assets/Prefabs/UI/Shop/";
    private const string BUILT = "Assets/_Assets/Prefabs/UI/Shop/Built";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string DATA = "Assets/_Assets/Resources/Scriptable Objects/Shop";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";

    // Component phụ của gốc không port (hiệu ứng nhấn / nhãn localize tĩnh / lưới ảo hoá).
    private static readonly HashSet<string> SKIP = new HashSet<string>
    {
        "PressButtonUI", "LocalizedLabel", "NotificationUI", "DOTweenAnimation", "TapToCloseExtension", "VirtualizedGridScroller",
    };

    // Thẻ con dựng trước (trang / popup tham chiếu làm prefab con).
    private static readonly string[] CARDS =
    {
        "DailyDealRewardEntryUI", "ChestRewardElementUI", "ChestContentUI", "Chest_Single_Product_Vertical", "Chest_Single_Product_Horizontal",
    };

    private static readonly string[] POPUPS = { "ChestInfoPopup", "ChestRevealPopup", "ChestRewardsPopup", "Chest_Bundle_PopupOffer" };

    // Field tham chiếu prefab ngoài → thẻ đã dựng.
    private static readonly Dictionary<string, string> EXTERNAL_CARDS = new Dictionary<string, string>
    {
        { "CardPrefab", "Chest_Single_Product_Vertical" }, { "FeaturedCardPrefab", "Chest_Single_Product_Horizontal" },
        { "RewardEntryPrefab", "DailyDealRewardEntryUI" }, { "CellPrefab", "ChestRewardElementUI" }, { "ContentIconPrefab", "ChestContentUI" },
    };

    // Nút mua bằng tiền thật của StoreTabPage → id sản phẩm (đối số OnPurchaseButtonClicked trong prefab gốc).
    private static readonly Dictionary<string, string> BUY_BUTTONS = new Dictionary<string, string>
    {
        { "LifetimeBoost/LifetimeBoostBuyButton", PurchaseController.STORE_ID_LIFETIME_BOOST },
        { "NoAds/NoAdsBuyButton", PurchaseController.STORE_ID_NO_ADS },
        { "StarterPack/StarterPackBuyButton", PurchaseController.STORE_ID_STARTER_PACK },
        { "EpicPack/EpicPackBuyButton", PurchaseController.STORE_ID_EPIC_PACK },
        { "DungeonDeal/StoreTabPageBuyButton", PurchaseController.STORE_ID_DUNGEON_KEYS },
        { "ResourceDeal/ResourcePackBuyButton", PurchaseController.STORE_ID_RESOURCE_PACK },
        { "GemPacks1/Gem1/Gem1BuyButton", PurchaseController.STORE_ID_GEM1 },
        { "GemPacks1/Gem2/Gem2BuyButton", PurchaseController.STORE_ID_GEM2 },
        { "GemPacks1/Gem3/Gem3BuyButton", PurchaseController.STORE_ID_GEM3 },
        { "GemPacks2/Gem4/Gem4BuyButton", PurchaseController.STORE_ID_GEM4 },
        { "GemPacks2/Gem5/Gem5BuyButton", PurchaseController.STORE_ID_GEM5 },
        { "GemPacks2/Gem6/Gem6BuyButton", PurchaseController.STORE_ID_GEM6 },
    };
    private const string STORE_LIST = "Scroll View/Viewport/VerticalLayout/";

    // Icon dụng cụ Mining chưa có TMP sprite asset → sinh từ sprite gốc (asset → tên sprite).
    private static readonly string[,] NEW_SPRITE_ASSETS = { { "IsPickaxe", "pickaxe" }, { "IsGoldPickaxe", "gold_pickaxe" }, { "IsDrill", "drill" } };

    // Icon thưởng Daily Deal, index = (int)ShopRewardType.
    private static readonly string[] REWARD_ICONS =
    {
        "IsGem", "IsBox", "IsBone", "IsCrown", "IsRedKey", "IsGreenKey", "IsPickaxe", "IsGoldPickaxe", "IsDrill",
    };

    private static JObject map;
    private static JObject spriteMap;
    private static readonly Dictionary<string, GameObject> builtCards = new Dictionary<string, GameObject>();
    private static List<string> log;

    [MenuItem("Tools/DungeonRush/Build Shop UI Prefabs")]
    public static string BuildAll()
    {
        log = new List<string>();
        map = JObject.Parse(File.ReadAllText(DIR + "shop_fieldmap.json"));
        spriteMap = JObject.Parse(File.ReadAllText(DIR + "shop_tmpsprites.json"));
        builtCards.Clear();
        EnsureFolder(BUILT);

        for (int i = 0; i < NEW_SPRITE_ASSETS.GetLength(0); i++) EnsureSpriteAsset(NEW_SPRITE_ASSETS[i, 0], NEW_SPRITE_ASSETS[i, 1]);
        BuildChestAssets();

        foreach (string card in CARDS) BuildCard(card);
        BuildPage("StoreTabPage");
        foreach (string popup in POPUPS) BuildPopup(popup);

        AssetDatabase.SaveAssets();
        string result = string.Join("\n", log.ToArray());
        Debug.Log("[ShopPrefabBuilder]\n" + result);
        return result;
    }

    // ===== Asset rương (số liệu + tên sprite GỐC đọc từ xapk) =====

    private static void BuildChestAssets()
    {
        EnsureFolder(DATA + "/Chests");
        EnsureFolder(DATA + "/Bundles");
        JObject src = JObject.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "../../DecodedData/tables/ChestCatalog.names.json")));

        Dictionary<string, ChestData> chests = new Dictionary<string, ChestData>();
        foreach (JToken c in src["ChestData"])
        {
            string name = (string)c["m_Name"];
            ChestData chest = LoadOrCreate<ChestData>(DATA + "/Chests/" + name + ".asset");
            chest.ChestId = (string)c["ChestId"];
            chest.ChestName = (string)c["ChestName"];
            chest.Icon = FindSprite((string)c["Icon"]);
            chest.OpenIcon = FindSprite((string)c["OpenIcon"]);
            chest.MainIcon = FindSprite((string)c["MainIcon"]);
            chest.ShowcaseSprites = Sprites(c["ShowcaseSprites"]);
            chest.GemCost = (int)c["GemCost"];
            chest.RequireRarityCompletion = (int)c["RequireRarityCompletion"] != 0;
            chest.RequiredCompletedRarity = (Rarity)(int)c["RequiredCompletedRarity"];
            chest.ItemCount = (int)c["ItemCount"];
            chest.Rarity = (Rarity)(int)c["Rarity"];
            chest.PityCount = (int)c["PityCount"];
            chest.RarityOdds = new List<ChestRarityOdds>();
            foreach (JToken o in c["RarityOdds"])
            {
                chest.RarityOdds.Add(new ChestRarityOdds { Rarity = (Rarity)(int)o["Rarity"], Weight = (float)o["Weight"] });
            }
            EditorUtility.SetDirty(chest);
            chests[name] = chest;
        }

        Dictionary<string, ChestBundleData> bundles = new Dictionary<string, ChestBundleData>();
        foreach (JToken b in src["ChestBundleData"])
        {
            string name = (string)b["m_Name"];
            ChestBundleData bundle = LoadOrCreate<ChestBundleData>(DATA + "/Bundles/" + name + ".asset");
            bundle.BundleId = (string)b["BundleId"];
            bundle.StoreId = (string)b["StoreId"];
            bundle.Icon = FindSprite((string)b["Icon"]);
            bundle.GemAmount = (int)b["GemAmount"];
            bundle.ShowcaseSprites = Sprites(b["ShowcaseSprites"]);
            bundle.Chests = new List<ChestBundleEntry>();
            foreach (JToken e in b["Chests"])
            {
                bundle.Chests.Add(new ChestBundleEntry { Chest = chests[(string)e["Chest"]], Count = (int)e["Count"] });
            }
            EditorUtility.SetDirty(bundle);
            bundles[name] = bundle;
        }

        JToken cfg = src["ChestConfig"][0];
        ChestConfig config = LoadOrCreate<ChestConfig>(DATA + "/ChestConfig.asset");
        config.Chests = new List<ChestData>();
        foreach (JToken n in cfg["Chests"]) config.Chests.Add(chests[(string)n]);
        config.Bundles = new List<ChestBundleData>();
        foreach (JToken n in cfg["Bundles"]) config.Bundles.Add(bundles[(string)n]);
        config.LegendaryFeaturedBundle = bundles[(string)cfg["LegendaryFeaturedBundle"]];
        config.MythicFeaturedBundle = bundles[(string)cfg["MythicFeaturedBundle"]];
        EditorUtility.SetDirty(config);
        log.Add("ChestConfig: " + config.Chests.Count + " rương, " + config.Bundles.Count + " gói");
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    private static List<Sprite> Sprites(JToken names)
    {
        List<Sprite> list = new List<Sprite>();
        foreach (JToken n in names) list.Add(FindSprite((string)n));
        return list;
    }

    // TMP sprite asset 1 icon: chép khuôn IsGem.asset rồi đổi texture + kích thước (cùng cấu trúc với các Is*.asset có sẵn).
    private static void EnsureSpriteAsset(string assetName, string spriteName)
    {
        string path = CUR + assetName + ".asset";
        if (File.Exists(path)) return;

        Sprite sprite = FindSprite(spriteName);
        Sprite gem = AssetDatabase.LoadAssetAtPath<Sprite>(CUR + "gem.png");
        if (sprite == null || gem == null) return;

        string gemGuid = AssetDatabase.AssetPathToGUID(CUR + "gem.png");
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
        int gw = gem.texture.width, gh = gem.texture.height, w = sprite.texture.width, h = sprite.texture.height;
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        string text = File.ReadAllText(CUR + "IsGem.asset")
            .Replace(gemGuid, guid)
            .Replace("m_Name: IsGem", "m_Name: " + assetName)
            .Replace("m_Name: gem Material", "m_Name: " + spriteName + " Material")
            .Replace("m_Name: gem", "m_Name: " + spriteName)
            .Replace("m_HorizontalBearingX: " + (-gw / 2f).ToString(inv), "m_HorizontalBearingX: " + (-w / 2f).ToString(inv))
            .Replace("m_HorizontalBearingY: " + (gh / 2f).ToString(inv), "m_HorizontalBearingY: " + (h / 2f).ToString(inv))
            .Replace("m_HorizontalAdvance: " + gw, "m_HorizontalAdvance: " + w)
            .Replace("m_Width: " + gw, "m_Width: " + w)
            .Replace("m_Height: " + gh, "m_Height: " + h);
        File.WriteAllText(path, text);
        AssetDatabase.ImportAsset(path);
        log.Add("sprite asset " + assetName + " ← " + spriteName);
    }

    // ===== Dựng =====

    private static void BuildCard(string name)
    {
        GameObject root = InstantiateRip(name);
        if (root == null) return;
        Wire(name, root.transform, null);
        string path = BUILT + "/" + name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        if (ok) builtCards[name] = saved;
        log.Add(name + (ok ? " → " + path : " FAILED"));
    }

    private static void BuildPage(string name)
    {
        GameObject root = InstantiateRip(name);
        if (root == null) return;
        Wire(name, root.transform, null);

        StoreTabPage page = root.GetComponent<StoreTabPage>();
        foreach (KeyValuePair<string, string> kv in BUY_BUTTONS)
        {
            Transform node = root.transform.Find(STORE_LIST + kv.Key);
            Button button = node != null ? node.GetComponent<Button>() : null;
            if (button == null || page == null)
            {
                log.Add("  ! " + name + ": thiếu nút mua " + kv.Key);
                continue;
            }
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddStringPersistentListener(button.onClick, page.OnPurchaseButtonClicked, kv.Value);
        }

        TMP_SpriteAsset[] icons = new TMP_SpriteAsset[REWARD_ICONS.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = LoadIcon(REWARD_ICONS[i]);
        foreach (DailyDealSlotUI slot in root.GetComponentsInChildren<DailyDealSlotUI>(true)) slot.RewardSpriteAssets = icons;

        Save(root, name);
    }

    private static void BuildPopup(string name)
    {
        GameObject rip = InstantiateRip(name);
        if (rip == null) return;
        string uiName = "UI" + RootClass(name);
        GameObject root = NewRoot(uiName);
        rip.transform.SetParent(root.transform.Find("ContentAll"), false);
        List<Component> comps = Wire(name, rip.transform, root);
        BaseUI ui = root.GetComponent<BaseUI>();
        if (ui != null) ui.isPopup = true;
        if (comps.Count == 0) log.Add("  ! " + name + ": không gắn được component nào");

        // Popup.CloseOnClickDark gốc: bấm vùng tối ngoài bảng → đóng.
        if (ui != null && (string)RootEntry(name)["fields"]["CloseOnClickDark"] == "1")
        {
            Button dark = root.transform.Find("ContentAll").gameObject.AddComponent<Button>();
            dark.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(dark.onClick, ui.Close);
        }
        Save(root, uiName);
    }

    private static JObject RootEntry(string prefabName)
    {
        foreach (JObject e in (JArray)map[prefabName])
        {
            if ((string)e["path"] == "") return e;
        }
        return null;
    }

    private static string RootClass(string prefabName)
    {
        JObject root = RootEntry(prefabName);
        return root != null ? (string)root["class"] : prefabName;
    }

    // ===== Nối field theo bảng gốc =====

    // entries: [{class, path, fields}]. wrapper != null → component gốc ở root gắn lên wrapper (BaseUI, tên "UI" + class).
    private static List<Component> Wire(string prefabName, Transform ripRoot, GameObject wrapper)
    {
        JArray entries = (JArray)map[prefabName];
        List<Component> result = new List<Component>();

        // Pass 1: gắn component.
        Dictionary<JObject, Component> comps = new Dictionary<JObject, Component>();
        foreach (JObject e in entries)
        {
            string cls = (string)e["class"];
            string path = (string)e["path"];
            if (SKIP.Contains(cls)) continue;
            bool isPopupRoot = path == "" && wrapper != null;
            Type t = FindType(isPopupRoot ? "UI" + cls : cls);
            if (t == null)
            {
                log.Add("  ! " + prefabName + ": không có class " + cls);
                continue;
            }
            Transform node = FindNode(ripRoot, path);
            GameObject go = isPopupRoot ? wrapper : (node != null ? node.gameObject : null);
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
            foreach (JProperty p in ((JObject)kv.Key["fields"]).Properties())
            {
                FieldInfo fi = FindField(c.GetType(), p.Name);
                if (fi == null) continue;
                if (Assign(c, fi, p.Value, ripRoot, prefabName)) set++;
            }
            EditorUtility.SetDirty(c);
        }

        ApplySpriteAssets(prefabName, ripRoot);
        CleanButtons(ripRoot);
        // Hạt particle của gốc vẽ qua UIParticle (plugin không có trong project) — để nguyên sẽ vẽ lạc ra ngoài UI.
        foreach (ParticleSystem particle in ripRoot.GetComponentsInChildren<ParticleSystem>(true)) particle.gameObject.SetActive(false);
        log.Add(prefabName + ": " + result.Count + " component, " + set + " field");
        return result;
    }

    private static bool Assign(Component c, FieldInfo fi, JToken v, Transform ripRoot, string prefabName)
    {
        Type ft = fi.FieldType;
        if (v.Type == JTokenType.Array) return AssignList(c, fi, (JArray)v, ripRoot, prefabName);

        if (v.Type == JTokenType.Object)
        {
            JObject o = (JObject)v;
            if (o["external"] != null) return AssignExternal(c, fi, prefabName);
            if (o["path"] == null) return false;
            object val = Resolve(ft, ripRoot, (string)o["path"]);
            if (val == null)
            {
                log.Add("  ! " + prefabName + "." + c.GetType().Name + "." + fi.Name + ": node " + o["path"] + " không có " + ft.Name);
                return false;
            }
            fi.SetValue(c, val);
            return true;
        }

        string s = (string)v;
        if (string.IsNullOrEmpty(s)) return false;
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        if (ft == typeof(float) && float.TryParse(s, System.Globalization.NumberStyles.Float, inv, out float f)) fi.SetValue(c, f);
        else if (ft == typeof(int) && int.TryParse(s, out int i)) fi.SetValue(c, i);
        else if (ft.IsEnum && int.TryParse(s, out int en)) fi.SetValue(c, Enum.ToObject(ft, en));
        else if (ft == typeof(bool)) fi.SetValue(c, s == "1");
        else if (ft == typeof(Color)) fi.SetValue(c, new Color(Num(s, "r"), Num(s, "g"), Num(s, "b"), Num(s, "a")));
        else if (ft == typeof(Vector2)) fi.SetValue(c, new Vector2(Num(s, "x"), Num(s, "y")));
        else return false;
        return true;
    }

    // Mảng / List tham chiếu node trong prefab.
    private static bool AssignList(Component c, FieldInfo fi, JArray items, Transform ripRoot, string prefabName)
    {
        Type ft = fi.FieldType;
        Type et = ft.IsArray ? ft.GetElementType() : (ft.IsGenericType ? ft.GetGenericArguments()[0] : null);
        if (et == null) return false;

        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(et));
        foreach (JToken item in items)
        {
            object val = item.Type == JTokenType.Object && item["path"] != null ? Resolve(et, ripRoot, (string)item["path"]) : null;
            if (val == null) log.Add("  ! " + prefabName + "." + fi.Name + ": phần tử " + item["path"] + " không có " + et.Name);
            list.Add(val);
        }

        if (ft.IsArray)
        {
            Array array = Array.CreateInstance(et, list.Count);
            list.CopyTo(array, 0);
            fi.SetValue(c, array);
        }
        else
        {
            fi.SetValue(c, list);
        }
        return true;
    }

    private static object Resolve(Type type, Transform ripRoot, string path)
    {
        Transform node = FindNode(ripRoot, path);
        if (node == null) return null;
        if (type == typeof(GameObject)) return node.gameObject;
        if (!typeof(Component).IsAssignableFrom(type)) return null;
        Component comp = node.GetComponent(type);
        return comp != null ? comp : null;
    }

    // Tham chiếu ra ngoài prefab: thẻ con đã dựng / sprite gem.
    private static bool AssignExternal(Component c, FieldInfo fi, string prefabName)
    {
        Type ft = fi.FieldType;
        if (ft == typeof(Sprite) && fi.Name == "GemIcon")
        {
            fi.SetValue(c, AssetDatabase.LoadAssetAtPath<Sprite>(CUR + "gem.png"));
            return true;
        }
        if (typeof(Component).IsAssignableFrom(ft) && EXTERNAL_CARDS.TryGetValue(fi.Name, out string card)
            && builtCards.TryGetValue(card, out GameObject cardPrefab) && cardPrefab.GetComponent(ft) != null)
        {
            fi.SetValue(c, cardPrefab.GetComponent(ft));
            return true;
        }
        log.Add("  ! " + prefabName + "." + fi.Name + ": tham chiếu ngoài chưa map (" + ft.Name + ")");
        return false;
    }

    // Gắn TMP sprite asset cho các dòng chữ "<sprite=0>…" theo tên asset GỐC (IsGem, IsExp, IsBox…).
    private static void ApplySpriteAssets(string prefabName, Transform ripRoot)
    {
        JArray entries = (JArray)spriteMap[prefabName];
        if (entries == null) return;
        foreach (JToken e in entries)
        {
            string assetName = (string)e["spriteAsset"];
            if (string.IsNullOrEmpty(assetName)) continue;
            Transform node = FindNode(ripRoot, (string)e["path"]);
            TMP_Text text = node != null ? node.GetComponent<TMP_Text>() : null;
            TMP_SpriteAsset asset = LoadIcon(assetName);
            if (text == null || asset == null) continue;
            text.spriteAsset = asset;
            EditorUtility.SetDirty(text);
        }
    }

    // Bỏ các onClick trỏ tới component gốc đã bị gỡ.
    private static void CleanButtons(Transform ripRoot)
    {
        foreach (Button button in ripRoot.GetComponentsInChildren<Button>(true))
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (button.onClick.GetPersistentTarget(i) == null) UnityEventTools.RemovePersistentListener(button.onClick, i);
            }
        }
    }

    // ===== Helper =====

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
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIP + name + ".prefab");
        if (prefab == null)
        {
            log.Add("  ! không thấy prefab rip " + name);
            return null;
        }
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name;
        return go;
    }

    private static Transform FindNode(Transform root, string path)
    {
        return string.IsNullOrEmpty(path) ? root : root.Find(path);
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn) như UIClan*/UIPvP*.
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

    // "{r: 1, g: 0.5, b: 0, a: 1}" / "{x: 250, y: 250}" → lấy 1 thành phần.
    private static float Num(string s, string key)
    {
        int i = s.IndexOf(key + ":", StringComparison.Ordinal);
        if (i < 0) return 1f;
        int start = i + key.Length + 1;
        int end = s.IndexOfAny(new[] { ',', '}' }, start);
        float.TryParse(s.Substring(start, end - start).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v);
        return v;
    }

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite", new[] { "Assets/_Assets/_ResourceGame" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }
        log.Add("  ! thiếu sprite " + name);
        return null;
    }

    private static TMP_SpriteAsset LoadIcon(string assetName)
    {
        TMP_SpriteAsset asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + assetName + ".asset");
        if (asset == null) asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/_Assets/_ResourceGame/MasteryIcons/" + assetName + ".asset");
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
