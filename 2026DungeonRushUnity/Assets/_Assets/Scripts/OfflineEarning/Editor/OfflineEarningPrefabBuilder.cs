using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng Resources/Prefabs/UI/UIOfflineEarningPopup từ prefab rip layout-only Prefabs/UI/Settings/OfflineEarningPopup:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (như UIProfilePopup), gắn script, nối field.
// Chạy lại là GHI ĐÈ prefab — đã chỉnh bố cục bằng tay thì đừng dựng lại.
// Các khối bản rip để tắt sẵn (DurationText, RewardParent, Coin/HammerPerSecond, QuickEarnings) giữ nguyên, không dùng.
public static class OfflineEarningPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/Settings/OfflineEarningPopup.prefab";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/UIOfflineEarningPopup.prefab";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private const string MASTERY_ICONS = "Assets/_Assets/_ResourceGame/MasteryIcons/";
    private const string CONTENT = "ContentTransform/Background/";

    [MenuItem("Tools/DungeonRush/Build Offline Earning UI/Offline Earning Popup")]
    public static string BuildOfflineEarningPopup()
    {
        GameObject root = NewRoot("UIOfflineEarningPopup", out Transform popup);
        UIOfflineEarningPopup ui = root.AddComponent<UIOfflineEarningPopup>();
        ui.isPopup = true;
        ui.txtSummary = Text(popup, CONTENT + "SummaryText");
        ui.txtExperience = Text(popup, CONTENT + "ExperienceText");
        ui.txtExpPerHour = Text(popup, CONTENT + "CoinPerHour/CoinPerTimeText_1");
        ui.txtLootBoxPerHour = Text(popup, CONTENT + "BoxPerHour/BoxPerHourText");
        ui.txtMaxReward = Text(popup, CONTENT + "MaxRewardInfoText");
        ui.btClose = Find(popup, CONTENT + "OfflineEarningCloseButton").GetComponent<Button>();
        ui.btDouble = Find(popup, CONTENT + "DoubleEarn").GetComponent<Button>();

        // Font project rộng hơn font gốc: cỡ chữ nhỏ nhất của bản rip (32.2) làm dòng đầu bị ngắt → tràn xuống dòng exp.
        ui.txtSummary.fontSizeMin = 24f;

        // Icon "<sprite=0>" trong chữ: rương loot, exp, quảng cáo (TMP sprite asset gốc không rip được).
        ui.txtSummary.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(MASTERY_ICONS + "IsBox.asset");
        ui.txtExperience.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + "IsExp.asset");
        Text(popup, CONTENT + "DoubleEarn/Content/Text").spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + "IsAd.asset");

        PrefabUtility.SaveAsPrefabAsset(root, OUT, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = "UIOfflineEarningPopup" + (ok ? " saved" : " FAILED");
        Debug.Log("[OfflineEarningPrefabBuilder] " + result);
        return result;
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn) → bản rip đã unpack.
    private static GameObject NewRoot(string name, out Transform popup)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIP);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contentAll.transform);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        popup = go.transform;
        return root;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[OfflineEarningPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();
}
