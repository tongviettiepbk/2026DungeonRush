using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Dựng prefab UI chat từ prefab rip layout-only (Prefabs/UI/Chat, Prefabs/UI/Settings/ReportPopup) theo đường dẫn field GỐC
// (tools/rip_fieldmap.py trên ChatTabPage / ChatMessageItemUI / ReportPopup / PlayerPopup), rồi nối vào prefab đang dùng:
//   UINew/Chat/ChatMessageItemUI, Resources UIChatTabPage + UIReportPopup,
//   UIPlayerPopup (chỉ gán btBlock/btReport/txtBlock — KHÔNG đụng layout user đã chỉnh),
//   UIMainGame2 (ChatButtonUI trên VerticalBottomLayout/ChatButton + UIMainLobby.chatButton).
// Chạy lại được (ghi đè). Menu: Tools/DungeonRush/Build Chat UI.
public static class ChatPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/";
    private const string BUILT = "Assets/_Assets/Prefabs/UINew/Chat";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string TAB_ITEM = "Assets/_Assets/Prefabs/UINew/Clan/TabSelectorItemUI.prefab";
    private const string PLAYER_POPUP = OUT + "UIPlayerPopup.prefab";
    private const string LOBBY = "Assets/_Assets/Prefabs/UI/UIMainGame2.prefab";

    private static List<string> log;

    [MenuItem("Tools/DungeonRush/Build Chat UI")]
    public static string BuildAll()
    {
        log = new List<string>();
        EnsureFolder(BUILT);
        ChatMessageItem item = BuildMessageItem();
        BuildChatTabPage(item);
        BuildReportPopup();
        WirePlayerPopup();
        WireLobby();
        AssetDatabase.SaveAssets();
        string result = string.Join("\n", log);
        Debug.Log("[ChatPrefabBuilder]\n" + result);
        return result;
    }

    // ===== Dòng tin =====

    private static ChatMessageItem BuildMessageItem()
    {
        GameObject root = InstantiateRip("Chat/ChatMessageItemUI");
        ChatMessageItem c = root.AddComponent<ChatMessageItem>();
        SerializedObject so = new SerializedObject(c);
        Set(so, "_avatarButton", Get<Button>(root, "AvatarImage"));
        Set(so, "_avatarImage", Get<Image>(root, "AvatarImage"));
        Set(so, "_usernameText", Get<TextMeshProUGUI>(root, "UsernameText"));
        Set(so, "_messageText", Get<TextMeshProUGUI>(root, "MessageText"));
        Set(so, "_timeText", Get<TextMeshProUGUI>(root, "TimeText"));
        Set(so, "_canvasGroup", root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>());
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = BUILT + "/ChatMessageItemUI.prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        log.Add("ChatMessageItemUI " + (ok ? "→ " + path : "FAILED"));
        return saved != null ? saved.GetComponent<ChatMessageItem>() : null;
    }

    // ===== Trang chat =====

    private static void BuildChatTabPage(ChatMessageItem itemPrefab)
    {
        GameObject rip = InstantiateRip("Chat/ChatTabPage");
        GameObject root = NewRoot("UIChatTabPage", false);
        rip.transform.SetParent(root.transform, false);
        Stretch((RectTransform)rip.transform);

        TabSelector selector = Node(rip, "PanelRoot/Bottom/TabSelectorUI").gameObject.AddComponent<TabSelector>();
        SerializedObject sel = new SerializedObject(selector);
        Set(sel, "_selectorTransform", Node(rip, "PanelRoot/Bottom/TabSelectorUI/Selector"));
        Set(sel, "_itemContainer", Node(rip, "PanelRoot/Bottom/TabSelectorUI/ItemContainer"));
        GameObject tabItem = AssetDatabase.LoadAssetAtPath<GameObject>(TAB_ITEM);
        Set(sel, "_itemPrefab", tabItem != null ? tabItem.GetComponent<TabSelectorItem>() : null);
        sel.ApplyModifiedPropertiesWithoutUndo();

        UIChatTabPage page = root.AddComponent<UIChatTabPage>();
        page.isPopup = true;
        SerializedObject so = new SerializedObject(page);
        Set(so, "_scrollRect", Get<ScrollRect>(rip, "PanelRoot/Scroll View"));
        Set(so, "_contentParent", Node(rip, "PanelRoot/Scroll View/Viewport/VerticalLayout"));
        Set(so, "_messagePrefab", itemPrefab);
        Set(so, "_inputField", Get<TMP_InputField>(rip, "PanelRoot/Bottom/MessageInputField"));
        Set(so, "_sendButton", Get<Button>(rip, "PanelRoot/Bottom/ChatTabPageSpeedBoostButton"));
        Set(so, "_closeButton", Get<Button>(rip, "ChatTabPageCloseButton"));
        Set(so, "_panelRoot", Node(rip, "PanelRoot"));
        Set(so, "_loadingImage", Get<Image>(rip, "LoadingImage"));
        Set(so, "_discordButton", Get<Button>(rip, "ChatTabPageDiscordButton"));
        Set(so, "_channelSelector", selector);
        Set(so, "_titleText", Get<TMP_Text>(rip, "PanelRoot/TopOverlay/f_1/TitleText"));
        so.ApplyModifiedPropertiesWithoutUndo();
        Save(root, "UIChatTabPage");
    }

    // ===== Báo cáo =====

    private static void BuildReportPopup()
    {
        GameObject rip = InstantiateRip("Settings/ReportPopup");
        GameObject root = NewRoot("UIReportPopup", true);
        rip.transform.SetParent(root.transform.Find("ContentAll"), false);

        UIReportPopup popup = root.AddComponent<UIReportPopup>();
        popup.isPopup = true;
        string[] rows = { "Harrasment", "Harrasment_1", "Harrasment_2", "Harrasment_3", "Harrasment_4", "Harrasment_5", "Harrasment_6" };
        popup.ReasonCheckButtons = new List<CheckButton>();
        foreach (string row in rows)
        {
            string basePath = "ContentTransform/ReportOptions/" + row;
            Transform button = Node(rip, basePath + "/InvisibleButton");
            if (button == null) continue;
            CheckButton check = button.gameObject.GetComponent<CheckButton>() ?? button.gameObject.AddComponent<CheckButton>();
            check.CheckImage = Get<Image>(rip, basePath + "/CheckButtonImg/CheckImage");
            popup.ReasonCheckButtons.Add(check);
        }
        popup.SubmitButton = Get<Button>(rip, "ContentTransform/EnterButton");
        popup.CloseButton = Get<Button>(rip, "ContentTransform/CloseButton");
        popup.LoadingImage = Get<Image>(rip, "ContentTransform/LoadingImage");
        EditorUtility.SetDirty(popup);
        log.Add("UIReportPopup: " + popup.ReasonCheckButtons.Count + " lý do");
        Save(root, "UIReportPopup");
    }

    // ===== Nối vào prefab có sẵn =====

    private static void WirePlayerPopup()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PLAYER_POPUP);
        UIPlayerPopup popup = root.GetComponent<UIPlayerPopup>();
        const string loaded = "ContentAll/PlayerPopup/Content/Loaded/";
        popup.btBlock = Get<Button>(root, loaded + "BlockButton");
        popup.txtBlock = Get<TMP_Text>(root, loaded + "BlockButton/Content/Text");
        popup.btReport = Get<Button>(root, loaded + "ReportButton");
        EditorUtility.SetDirty(popup);
        string summary = "UIPlayerPopup: block=" + (popup.btBlock != null) + " report=" + (popup.btReport != null);
        PrefabUtility.SaveAsPrefabAsset(root, PLAYER_POPUP, out bool ok);
        PrefabUtility.UnloadPrefabContents(root);
        log.Add(summary + (ok ? "" : " SAVE FAILED"));
    }

    private static void WireLobby()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(LOBBY);
        Transform node = root.transform.Find("SafeArea/VerticalBottomLayout/ChatButton");
        if (node == null)
        {
            log.Add("  ! UIMainGame2: không thấy SafeArea/VerticalBottomLayout/ChatButton");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        ChatButtonUI button = node.GetComponent<ChatButtonUI>() ?? node.gameObject.AddComponent<ChatButtonUI>();
        button.ChatButton = node.GetComponent<Button>();
        // Dòng trên = tin cũ hơn (gốc MessageItems 2 dòng, tin mới nhất ở dưới).
        List<ChatButtonMessageItemUI> items = new List<ChatButtonMessageItemUI>();
        foreach (Transform child in node.Cast<Transform>().Where(t => t.name.StartsWith("ChatButtonMessageItemUI")).OrderByDescending(t => t.position.y))
        {
            ChatButtonMessageItemUI line = child.GetComponent<ChatButtonMessageItemUI>() ?? child.gameObject.AddComponent<ChatButtonMessageItemUI>();
            SerializedObject so = new SerializedObject(line);
            Set(so, "_text", Get<TextMeshProUGUI>(child.gameObject, "MessageText"));
            so.ApplyModifiedPropertiesWithoutUndo();
            items.Add(line);
        }
        button.MessageItems = items.ToArray();
        EditorUtility.SetDirty(button);

        UIMainLobby lobby = root.GetComponentInChildren<UIMainLobby>(true);
        if (lobby != null)
        {
            lobby.chatButton = button;
            EditorUtility.SetDirty(lobby);
        }
        string summary = "UIMainGame2: ChatButtonUI " + items.Count + " dòng xem trước, lobby=" + (lobby != null);
        PrefabUtility.SaveAsPrefabAsset(root, LOBBY, out bool ok);
        PrefabUtility.UnloadPrefabContents(root);
        log.Add(summary + (ok ? "" : " SAVE FAILED"));
    }

    // ===== Helper =====

    private static GameObject InstantiateRip(string relPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIP + relPath + ".prefab");
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = Path.GetFileName(relPath);
        return go;
    }

    private static Transform Node(GameObject root, string path)
    {
        Transform t = string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
        if (t == null) log.Add("  ! " + root.name + ": thiếu node " + path);
        return t;
    }

    private static T Get<T>(GameObject root, string path) where T : Component
    {
        Transform t = Node(root, path);
        T c = t != null ? t.GetComponent<T>() : null;
        if (t != null && c == null) log.Add("  ! " + root.name + "/" + path + ": không có " + typeof(T).Name);
        return c;
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty p = so.FindProperty(field);
        if (p == null)
        {
            log.Add("  ! " + so.targetObject.GetType().Name + ": không có field " + field);
            return;
        }
        p.objectReferenceValue = value;
    }

    // Root Canvas (1080×2160) như popup Clan/PvP; dimmed → thêm ContentAll nền mờ toàn màn.
    private static GameObject NewRoot(string name, bool dimmed)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // màn dài thì giữ rộng 1080, màn thấp (16:9, iPad) thì giữ cao 2160 → không bị cắt
        if (!dimmed) return root;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        Stretch(rt);
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        return root;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Save(GameObject root, string name)
    {
        string path = OUT + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        log.Add(name + (ok ? " → " + path : " FAILED"));
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
