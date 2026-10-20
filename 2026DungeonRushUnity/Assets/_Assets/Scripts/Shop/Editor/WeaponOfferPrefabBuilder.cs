using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng Resources/Prefabs/UI/UIOfferPopup (Weapon Offer) từ prefab rip layout-only Prefabs/UI/Shop/OfferPopup:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (chạm nền = đóng, CloseOnClickDark gốc), gắn script, nối field.
// Chạy lại là GHI ĐÈ prefab — đã chỉnh bố cục bằng tay thì đừng dựng lại.
public static class WeaponOfferPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/Shop/OfferPopup.prefab";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/UIOfferPopup.prefab";
    private const string LEVEL_TEXT_MATERIAL = "Assets/_Assets/Fonts/NotoSans/NotoSans-SemiBold-Outline-Black.mat";
    private const string BG = "ContentTransform/Background/";

    [MenuItem("Tools/DungeonRush/Build Shop UI/Weapon Offer Popup")]
    public static string BuildOfferPopup()
    {
        GameObject root = NewRoot("UIOfferPopup", out Transform popup, out Button btBackground);
        UIOfferPopup ui = root.AddComponent<UIOfferPopup>();
        ui.isPopup = true;

        Transform element = Find(popup, BG + "ItemElementUI");
        ItemElementView view = element.gameObject.AddComponent<ItemElementView>();
        view.button = element.GetComponent<Button>();
        view.imgBackground = Find(element, "BackgroundImage").GetComponent<Image>();
        view.imgItem = Find(element, "BackgroundImage/ItemImage").GetComponent<Image>();
        view.txtLevel = Text(element, "LevelText");
        view.txtLevel.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(LEVEL_TEXT_MATERIAL);
        view.objAncient = Optional(element, "BackgroundImage/Mask/Ancient");
        view.objImmortal = Optional(element, "BackgroundImage/Mask/Immortal");
        view.objDivine = Optional(element, "BackgroundImage/Mask/Divine");
        // Bản rip mất SoftMask + RarityPatternEffect → pattern tràn khỏi ô, đứng yên; gắn lại như Forge.
        ForgePrefabBuilder.AddRarityPatternFx(element);
        foreach (string hide in new[] { "BackgroundImage/NotificationUI", "BackgroundImage/TutorialUI", "LockedImage" })
            Find(element, hide).gameObject.SetActive(false);
        ui.ItemElementUI = view;

        ui.RibbonImage = Find(popup, BG + "Ribbon").GetComponent<Image>();
        ui.WeaponNameText = Text(popup, BG + "Stats/NameText");
        ui.WeaponNameText.richText = true;
        ui.RarityText = Text(popup, BG + "RarityText");
        ui.RarityText.richText = true;
        ui.WeaponStatText = Text(popup, BG + "Stats/BaseStatText");
        Find(popup, BG + "Stats/BaseStatText/EquippedItemDamageArrow").gameObject.SetActive(false);
        ui.SubStatsText = Text(popup, BG + "Stats/SubStatText");
        ui.BuyButton = Find(popup, BG + "OfferPopupEquipButton").GetComponent<Button>();
        ui.BuyButtonPriceText = Text(popup, BG + "OfferPopupEquipButton/Content/Text");
        // ExpiredOverlay gốc trỏ cùng node LoadingImage.
        ui.ExpiredOverlay = Find(popup, BG + "LoadingImage").gameObject;
        ui.ExpiredOverlay.SetActive(false);
        ui.btBackground = btBackground;

        PrefabUtility.SaveAsPrefabAsset(root, OUT, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = "UIOfferPopup" + (ok ? " saved" : " FAILED");
        Debug.Log("[WeaponOfferPrefabBuilder] " + result);
        return result;
    }

    private static GameObject Optional(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.gameObject : null;
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
        if (t == null) throw new System.Exception("[WeaponOfferPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();
}
