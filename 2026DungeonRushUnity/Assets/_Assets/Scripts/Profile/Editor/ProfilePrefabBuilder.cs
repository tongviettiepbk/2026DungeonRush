using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng prefab UI nhóm Profile (Resources/Prefabs/UI/UIPlayerPopup, UIAvatarSelectPopup) từ prefab rip layout-only ở
// Prefabs/UI/Profile/: bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (như UIPvP*), gắn script, nối field.
// Mỗi popup 1 menu riêng (Tools/DungeonRush/Build Profile UI/...) vì chạy lại là GHI ĐÈ prefab — popup nào đã
// chỉnh bố cục bằng tay thì đừng dựng lại.
public static class ProfilePrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/Profile/";
    private const string RIP_AVATAR_ITEM = "Assets/_Assets/Prefabs/UI/Gear/AvatarItemUI.prefab";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    // Prefab dùng bằng tham chiếu trực tiếp (ô avatar).
    private const string BUILT = "Assets/_Assets/Prefabs/UINew/Profile";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private const string LAYOUT = "Content/Loaded/VerticalLayout/";
    private const float COMPANION_SIZE = 90f;   // bằng ô lưới Items của bản rip
    private const int COMPANION_SLOTS = 3;      // UserCompanionData.MAX_EQUIPPED

    // Thứ tự ô trong lưới Items của PlayerPopup gốc.
    private static readonly KeyValuePair<string, GearSlotType>[] ITEM_SLOTS =
    {
        new KeyValuePair<string, GearSlotType>("Weapon", GearSlotType.WEAPON),
        new KeyValuePair<string, GearSlotType>("Helmet", GearSlotType.HELMET),
        new KeyValuePair<string, GearSlotType>("Gloves", GearSlotType.GLOVES),
        new KeyValuePair<string, GearSlotType>("Wing", GearSlotType.WING),
        new KeyValuePair<string, GearSlotType>("Backpack", GearSlotType.BACKPACK),
        new KeyValuePair<string, GearSlotType>("Necklace", GearSlotType.NECKLACE),
        new KeyValuePair<string, GearSlotType>("Ring", GearSlotType.RING),
        new KeyValuePair<string, GearSlotType>("Cape", GearSlotType.CAPE),
    };

    // ===== Chọn avatar =====

    [MenuItem("Tools/DungeonRush/Build Profile UI/Avatar Select Popup")]
    public static string BuildAvatarSelectPopup()
    {
        // Ô avatar: prefab riêng, popup sinh mỗi avatar 1 ô lúc chạy.
        GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RIP_AVATAR_ITEM));
        PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        ElementAvatarUI e = item.AddComponent<ElementAvatarUI>();
        e.imgAvatar = Find(item.transform, "AvatarImage").GetComponent<Image>();
        e.objSelected = Find(item.transform, "AvatarImage_1").gameObject;
        e.btSelect = item.GetComponent<Button>();
        if (!AssetDatabase.IsValidFolder(BUILT)) AssetDatabase.CreateFolder("Assets/_Assets/Prefabs/UINew", "Profile");
        string itemPath = BUILT + "/AvatarItemUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(item, itemPath);
        Object.DestroyImmediate(item);

        GameObject root = NewRoot("UIAvatarSelectPopup", out Transform popup, RIP + "AvatarSelectPopup.prefab");
        UIAvatarSelectPopup ui = root.AddComponent<UIAvatarSelectPopup>();
        ui.isPopup = true;
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.gridContent = Find(popup, "Content/Avatars/Scroll View/Viewport/VerticalLayout");
        ui.elementPrefab = AssetDatabase.LoadAssetAtPath<ElementAvatarUI>(itemPath);
        return Finish(Save(root, "UIAvatarSelectPopup"));
    }

    // ===== Thông tin người chơi khác =====

    [MenuItem("Tools/DungeonRush/Build Profile UI/Player Popup")]
    public static string BuildPlayerPopup()
    {
        GameObject root = NewRoot("UIPlayerPopup", out Transform popup, RIP + "PlayerPopup.prefab");
        UIPlayerPopup ui = root.AddComponent<UIPlayerPopup>();
        ui.isPopup = true;
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.objLoaded = Find(popup, "Content/Loaded").gameObject;
        ui.objLoading = Find(popup, "Content/LoadingImage").gameObject;
        ui.txtName = Text(popup, LAYOUT + "NicknameText");
        ui.txtPower = Text(popup, LAYOUT + "PowerText");
        ui.imgAvatar = Find(popup, "Content/Loaded/AvatarParent/AvatarImage").GetComponent<Image>();
        ui.txtSubStats = Text(popup, LAYOUT + "Stats/BonusText");
        ui.rectContent = (RectTransform)Find(popup, "Content");
        ui.rectLayout = (RectTransform)Find(popup, "Content/Loaded/VerticalLayout");
        SetSprite(ui.txtPower, "IsPower");

        // Khối Stats gốc cao cố định 539 → cho cao theo số dòng chỉ số phụ để khung nền ôm vừa nội dung.
        GameObject stats = Find(popup, LAYOUT + "Stats").gameObject;
        stats.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        stats.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // RawImage gốc trỏ RenderTexture asset (không rip được) → PlayerPreviewStage tự tạo lúc chạy.
        Transform previewNode = Find(popup, LAYOUT + "PlayerPreviewArea/PlayerPreview");
        ui.preview = previewNode.gameObject.AddComponent<PlayerPreviewStage>();
        ui.preview.rawImage = Find(previewNode, "Frame/Mask/RawImage").GetComponent<RawImage>();
        ui.preview.rawImage.texture = null;
        ui.preview.rawImage.enabled = false;

        ui.listItem = new List<ElementPlayerItemUI>();
        for (int i = 0; i < ITEM_SLOTS.Length; i++)
        {
            Transform slot = Find(popup, LAYOUT + "Items/" + ITEM_SLOTS[i].Key);
            ui.listItem.Add(NewItemElement(slot, ITEM_SLOTS[i].Value));
        }

        // Hàng pet gốc sinh ô lúc chạy → tạo sẵn 3 ô, nhân bản từ ô đồ cho cùng kiểu dáng.
        Transform companions = Find(popup, LAYOUT + "Companions");
        ui.listCompanion = new List<ElementPlayerItemUI>();
        for (int i = 0; i < COMPANION_SLOTS; i++)
        {
            GameObject clone = Object.Instantiate(ui.listItem[0].gameObject, companions);
            clone.name = "Companion" + (i + 1);
            ((RectTransform)clone.transform).sizeDelta = new Vector2(COMPANION_SIZE, COMPANION_SIZE);
            ElementPlayerItemUI element = clone.GetComponent<ElementPlayerItemUI>();
            element.typeEquipment = GearSlotType.NONE;
            ui.listCompanion.Add(element);
        }

        // Block/Report/Battle của gốc thuộc PvP/chat — chưa có trong project.
        Find(popup, "Content/Loaded/BlockButton").gameObject.SetActive(false);
        Find(popup, "Content/Loaded/ReportButton").gameObject.SetActive(false);
        Find(popup, "Content/Loaded/BattleButton").gameObject.SetActive(false);
        return Finish(Save(root, "UIPlayerPopup"));
    }

    private static ElementPlayerItemUI NewItemElement(Transform slot, GearSlotType type)
    {
        ElementPlayerItemUI e = slot.gameObject.AddComponent<ElementPlayerItemUI>();
        e.typeEquipment = type;
        e.imgBackground = Find(slot, "BackgroundImage").GetComponent<Image>();
        e.imgItem = Find(slot, "BackgroundImage/ItemImage").GetComponent<Image>();
        e.objPlaceholder = Find(slot, "BackgroundImage/PlaceholderImage").gameObject;
        e.txtLevel = Text(slot, "LevelText");
        e.objTier = Find(slot, "BackgroundImage/TierIndicator_Button").gameObject;
        e.imgTierBackground = e.objTier.GetComponent<Image>();
        e.txtTier = Text(slot, "BackgroundImage/TierIndicator_Button/TierIndicatorText");
        return e;
    }

    // ===== Helper =====

    private static string Finish(string result)
    {
        AssetDatabase.SaveAssets();
        Debug.Log("[ProfilePrefabBuilder] " + result);
        return result;
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn) → bản rip đã unpack.
    private static GameObject NewRoot(string name, out Transform popup, string ripPath)
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

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ripPath);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contentAll.transform);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        popup = go.transform;
        return root;
    }

    private static string Save(GameObject root, string name)
    {
        string path = OUT + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        return name + (ok ? " saved" : " FAILED");
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[ProfilePrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();

    private static Button Btn(Transform root, string path) => Find(root, path).GetComponent<Button>();

    private static void SetSprite(TMP_Text text, string assetName)
    {
        text.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + assetName + ".asset");
    }
}
