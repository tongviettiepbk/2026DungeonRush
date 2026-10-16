using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng Resources/Prefabs/UI/UIRewardedChestPopup từ prefab rip layout-only Prefabs/UI/Shop/RewardedChestPopup:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (chạm nền = đóng, CloseOnClickDark gốc), gắn script, nối field.
// Chạy lại là GHI ĐÈ prefab — đã chỉnh bố cục bằng tay thì đừng dựng lại.
public static class RewardedChestPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/Shop/RewardedChestPopup.prefab";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/UIRewardedChestPopup.prefab";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private const string CONTENT = "ContentTransform/";

    [MenuItem("Tools/DungeonRush/Build Shop UI/Rewarded Chest Popup")]
    public static string BuildRewardedChestPopup()
    {
        GameObject root = NewRoot("UIRewardedChestPopup", out Transform popup, out Button btBackground);
        UIRewardedChestPopup ui = root.AddComponent<UIRewardedChestPopup>();
        ui.isPopup = true;
        ui.TitleText = Text(popup, CONTENT + "TitleText");
        ui.RewardText = Text(popup, CONTENT + "RewardParent/RewardText");
        ui.RewardImage = Find(popup, CONTENT + "RewardParent/Image").GetComponent<Image>();
        ui.LootboxRewardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CUR + "box_icon.png");
        ui.GemRewardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CUR + "gem.png");
        ui.AdButton = Find(popup, CONTENT + "AdButton").GetComponent<Button>();
        ui.AdButtonImage = Find(popup, CONTENT + "AdButton").GetComponent<Image>();
        ui.AdButtonText = Text(popup, CONTENT + "AdButton/Content/Text");
        ui.CloseButton = Find(popup, CONTENT + "CloseButton").GetComponent<Button>();
        ui.btBackground = btBackground;

        // Icon quảng cáo "<sprite=0>" trên nút (TMP sprite asset gốc không rip được).
        ui.AdButtonText.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + "IsAd.asset");

        PrefabUtility.SaveAsPrefabAsset(root, OUT, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = "UIRewardedChestPopup" + (ok ? " saved" : " FAILED");
        Debug.Log("[RewardedChestPrefabBuilder] " + result);
        return result;
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn, có Button để đóng) → bản rip đã unpack.
    private static GameObject NewRoot(string name, out Transform popup, out Button btBackground)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        btBackground = contentAll.GetComponent<Button>();
        btBackground.transition = Selectable.Transition.None;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIP);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contentAll.transform);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        // Bản rip còn component script gốc không có trong project (Popup...) → Unity không cho lưu prefab có script hỏng.
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        popup = go.transform;
        return root;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[RewardedChestPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();
}
