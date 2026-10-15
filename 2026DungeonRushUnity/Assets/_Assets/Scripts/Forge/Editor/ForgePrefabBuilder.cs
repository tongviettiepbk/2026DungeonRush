using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng UI Loot / Auto Loot từ prefab rip layout-only (Prefabs/UI/Gear, Prefabs/UI/Forge):
//  - Resources/Prefabs/UI/UINewItemPopup   ← Gear/NewItemPopup   (slot trống: chỉ mặc)
//  - Resources/Prefabs/UI/UINewForgePopup  ← Forge/NewForgePopup (slot có đồ: mặc mới / bán)
//  - Resources/Prefabs/UI/UIItemInfoPopup  ← Gear/ItemInfoPopup
//  - Resources/Prefabs/UI/UIAutoForgePopup ← Forge/AutoForgePopup
//  - Prefabs/UINew/Forge/ItemElementUI     ← Gear/ItemElementUI (món trên bục loot)
//  - Gắn ForgeUIController vào ForgeUI của Prefabs/UI/UIMainGame2.
// Popup bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (chạm nền = đóng), như OfflineEarningPrefabBuilder.
// Chạy lại là GHI ĐÈ prefab — đã chỉnh bố cục bằng tay thì đừng dựng lại.
public static class ForgePrefabBuilder
{
    private const string GEAR = "Assets/_Assets/Prefabs/UI/Gear/";
    private const string FORGE = "Assets/_Assets/Prefabs/UI/Forge/";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string ITEM_ELEMENT_OUT = "Assets/_Assets/Prefabs/UINew/Forge/ItemElementUI.prefab";
    private const string LOBBY = "Assets/_Assets/Prefabs/UI/UIMainGame2.prefab";
    private const string BOX_SPRITE_ASSET = "Assets/_Assets/_ResourceGame/MasteryIcons/IsBox.asset";
    // Chữ "Lv.N" trên ô item: bản rip mất viền của font gốc → viền đen để đọc được trên nền rarity sáng.
    private const string LEVEL_TEXT_MATERIAL = "Assets/_Assets/Fonts/NotoSans/NotoSans-SemiBold-Outline-Black.mat";

    private static readonly string[] RARITY_ROWS =
        { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Artifact", "Ancient", "Immortal", "Divine" };

    // Tên dòng trong FilterList gốc → SubStatType.
    private static readonly (string row, SubStatType type)[] SUBSTAT_ROWS =
    {
        ("CritChance", SubStatType.CriticalChance),
        ("CritDamage", SubStatType.CriticalDamage),
        ("BlockChance", SubStatType.BlockChance),
        ("HealthRegeneration", SubStatType.HealthRegen),
        ("LifeSteal", SubStatType.Lifesteal),
        ("DoublecChance", SubStatType.DoubleHitChance),
        ("AttackSpeed", SubStatType.AttackSpeed),
        ("Damage", SubStatType.Damage),
        ("Health", SubStatType.Health),
        ("MeleeDamage", SubStatType.MeleeDamage),
        ("RangedDamage", SubStatType.RangedDamage),
        ("CompanionCooldown", SubStatType.CompanionCooldown),
        ("CompanionDamage", SubStatType.CompanionDamage),
    };

    [MenuItem("Tools/DungeonRush/Build Forge UI/All")]
    public static string BuildAll()
    {
        return BuildItemElement() + "\n" + BuildNewItemPopup() + "\n" + BuildNewForgePopup() + "\n" + BuildItemInfoPopup() + "\n"
            + BuildAutoForgePopup() + "\n" + WireLobbyForgeUI();
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/Item Element")]
    public static string BuildItemElement()
    {
        GameObject go = Instantiate(GEAR + "ItemElementUI.prefab", null);
        go.name = "ItemElementUI";
        WireItemElement(go.transform);
        // Món trên bục chỉ để nhìn, không bấm.
        go.GetComponent<Button>().interactable = false;
        return Save(go, ITEM_ELEMENT_OUT);
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/New Item Popup")]
    public static string BuildNewItemPopup()
    {
        GameObject root = NewRoot("UINewItemPopup", GEAR + "NewItemPopup.prefab", out Transform popup, out Button btBackground);
        UINewItemPopup ui = root.AddComponent<UINewItemPopup>();
        ui.isPopup = true;
        const string CARD = "ContentTransform/Equipped/";
        ui.itemElement = WireItemElement(Find(popup, CARD + "ItemElementUI"));
        ui.txtName = Text(popup, CARD + "Stats/NameText");
        ui.txtRarity = Text(popup, CARD + "RarityText");
        ui.txtBaseStat = Text(popup, CARD + "Stats/BaseStatText");
        ui.txtSubStats = Text(popup, CARD + "Stats/SubStatText");
        ui.txtName.richText = true;
        ui.txtRarity.richText = true;
        ui.btEquip = Find(popup, "ContentTransform/EquipButton").GetComponent<Button>();
        // Chỉ có nhiệm vụ mặc vào hero: chạm nền không đóng.
        btBackground.enabled = false;
        HideAll(popup, "TutorialUI");
        HideAll(popup, "NotificationUI");
        return Save(root, OUT + "UINewItemPopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/New Forge Popup")]
    public static string BuildNewForgePopup()
    {
        GameObject root = NewRoot("UINewForgePopup", FORGE + "NewForgePopup.prefab", out Transform popup, out Button btBackground);
        UINewForgePopup ui = root.AddComponent<UINewForgePopup>();
        ui.isPopup = true;
        // Phải chọn Equip hoặc Sell: chạm nền không đóng.
        btBackground.enabled = false;

        const string EQ = "ContentTransform/Equipped/";
        ui.equippedElement = WireItemElement(Find(popup, EQ + "EquippedElementUI"));
        ui.txtEquippedName = RichText(popup, EQ + "Stats/NameText");
        ui.txtEquippedRarity = RichText(popup, EQ + "RarityText");
        ui.txtEquippedBaseStat = Text(popup, EQ + "Stats/BaseStatText");
        ui.txtEquippedSubStats = Text(popup, EQ + "Stats/SubStatText");
        ui.objEquippedNew = Find(popup, EQ + "NewTextEquipped").gameObject;

        const string NEW = "ContentTransform/NewItem/";
        ui.newElement = WireItemElement(Find(popup, NEW + "NewElementUI"));
        ui.txtNewName = RichText(popup, NEW + "Stats/NameText");
        ui.txtNewRarity = RichText(popup, NEW + "RarityText_1");
        ui.txtNewBaseStat = Text(popup, NEW + "Stats/BaseStatText");
        ui.txtNewSubStats = Text(popup, NEW + "Stats/SubStatText");
        ui.objNewNew = Find(popup, NEW + "NewTextNewItem").gameObject;

        ui.btEquip = Find(popup, "ContentTransform/NewForgeEquipButton").GetComponent<Button>();
        ui.btSell = Find(popup, "ContentTransform/NewForgeSellButton").GetComponent<Button>();
        Transform confirm = Find(popup, "ContentTransform/ConfirmPanel");
        ui.objConfirm = confirm.gameObject;
        ui.confirmGroup = confirm.GetComponent<CanvasGroup>();
        ui.txtConfirm = Text(confirm, "ConfirmText");
        ui.btConfirmYes = Find(confirm, "NewForgeYesButton").GetComponent<Button>();
        ui.btConfirmNo = Find(confirm, "NewForgeNoButton").GetComponent<Button>();
        HideAll(popup, "TutorialUI");
        HideAll(popup, "NotificationUI");
        return Save(root, OUT + "UINewForgePopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/Item Info Popup")]
    public static string BuildItemInfoPopup()
    {
        GameObject root = NewRoot("UIItemInfoPopup", GEAR + "ItemInfoPopup.prefab", out Transform popup, out Button btBackground);
        UIGearInfo ui = root.AddComponent<UIGearInfo>();
        ui.isPopup = true;
        ui.btClose = Find(popup, "Content/ItemInfoCloseButton").GetComponent<Button>();
        ui.btBackground = btBackground;
        ui.itemElement = WireItemElement(Find(popup, "Content/ItemElementUI"));
        ui.txtRarity = Text(popup, "Content/RarityText");
        ui.txtInfo = Text(popup, "Content/InfoText");
        ui.txtRarity.richText = true;
        ui.txtInfo.richText = true;
        ui.txtInfo.color = Color.white;   // bản rip để đỏ mặc định; màu từng phần do rich text quyết định
        HideAll(popup, "TutorialUI");
        HideAll(popup, "NotificationUI");
        return Save(root, OUT + "UIItemInfoPopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/Auto Forge Popup")]
    public static string BuildAutoForgePopup()
    {
        GameObject root = NewRoot("UIAutoForgePopup", FORGE + "AutoForgePopup.prefab", out Transform popup, out Button btBackground);
        UIAutoForgePopup ui = root.AddComponent<UIAutoForgePopup>();
        ui.isPopup = true;
        TMP_SpriteAsset boxSprite = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(BOX_SPRITE_ASSET);

        const string CONTENT = "ContentTransform/Bg/Scroll View/Viewport/Content/";
        const string LOOT = CONTENT + "LootList/";
        ui.rarityRows = new List<GameObject>();
        ui.keepButtons = new List<Button>();
        ui.keepChecks = new List<GameObject>();
        ui.rarityRateTexts = new List<TMP_Text>();
        foreach (string row in RARITY_ROWS)
        {
            Transform t = Find(popup, LOOT + row);
            ui.rarityRows.Add(t.gameObject);
            ui.keepButtons.Add(Find(t, "CheckButton_1").GetComponent<Button>());
            ui.keepChecks.Add(Find(t, "CheckButtonImg/CheckImage").gameObject);
            ui.rarityRateTexts.Add(Text(t, "CommonNextRate"));
        }

        ui.btFilterToggle = Find(popup, LOOT + "Filter/FilterToggle").GetComponent<Button>();
        ui.objFilterOn = Find(popup, LOOT + "Filter/FilterToggle/Image").gameObject;
        ui.objFilterLock = Find(popup, LOOT + "Filter/FilterToggle/LockImage").gameObject;
        ui.objFilterList = Find(popup, CONTENT + "FilterList").gameObject;
        ui.layoutRoot = (RectTransform)Find(popup, CONTENT.TrimEnd('/'));
        ui.subStatTypes = new List<SubStatType>();
        ui.subStatButtons = new List<Button>();
        ui.subStatChecks = new List<GameObject>();
        foreach ((string row, SubStatType type) in SUBSTAT_ROWS)
        {
            Transform t = Find(popup, CONTENT + "FilterList/" + row);
            ui.subStatTypes.Add(type);
            ui.subStatButtons.Add(FindButtonStartsWith(t, "SubStatCheckButton"));
            ui.subStatChecks.Add(Find(t, "SubStatCheckImg/CheckImage").gameObject);
        }

        const string BOTTOM = "ContentTransform/Bg/BottomArea/";
        ui.btHammerCount = Find(popup, BOTTOM + "HammerCountButton").GetComponent<Button>();
        ui.txtSelectedHammer = Text(popup, BOTTOM + "HammerCountButton/HammerCountText");
        ui.txtSelectedHammer.spriteAsset = boxSprite;
        Transform panel = Find(popup, BOTTOM + "HammerOptionPanel");
        ui.hammerPanel = panel.GetComponent<CanvasGroup>();
        ui.btHammerPanelBackground = Find(panel, "Background").GetComponent<Button>();
        ui.btHammerPanelClose = Find(panel, "CloseButton").GetComponent<Button>();
        ui.txtPanelSelectedHammer = Text(panel, "CloseButton/PanelHammerCountText");
        ui.txtPanelSelectedHammer.spriteAsset = boxSprite;
        ui.hammerOptions = new List<Button>();
        ui.hammerOptionTexts = new List<TMP_Text>();
        ui.hammerOptionLocks = new List<GameObject>();
        for (int i = 0; i < ForgeService.HAMMER_OPTION_COUNT; i++)
        {
            Transform option = Find(panel, "VerticalLayout/" + (i == 0 ? "Option1" : "Option1_" + i));
            ui.hammerOptions.Add(option.GetComponent<Button>());
            TMP_Text text = Text(option, "HammerCountText");
            text.spriteAsset = boxSprite;
            ui.hammerOptionTexts.Add(text);
            ui.hammerOptionLocks.Add(Find(option, "Locked").gameObject);
        }

        ui.btStart = Find(popup, BOTTOM + "StartAutoForgeButton").GetComponent<Button>();
        ui.btClose = Find(popup, "ContentTransform/Bg/TopArea/AutoForgeCloseButton").GetComponent<Button>();
        ui.btBackground = btBackground;
        HideAll(popup, "TutorialUI");
        return Save(root, OUT + "UIAutoForgePopup.prefab");
    }

    [MenuItem("Tools/DungeonRush/Build Forge UI/Wire Lobby ForgeUI")]
    public static string WireLobbyForgeUI()
    {
        GameObject lobby = PrefabUtility.LoadPrefabContents(LOBBY);
        try
        {
            UIMainLobby owner = lobby.GetComponent<UIMainLobby>();
            Transform forge = Find(lobby.transform, "SafeArea/VerticalBottomLayout/BottomUI/ForgeUI");
            ForgeUIController ctrl = forge.GetComponent<ForgeUIController>();
            if (ctrl == null)
                ctrl = forge.gameObject.AddComponent<ForgeUIController>();

            ctrl.owner = owner;
            ctrl.btLoot = Find(forge, "LootButton").GetComponent<Button>();
            ctrl.btAuto = Find(forge, "AutoForgeButton").GetComponent<Button>();
            ctrl.objAutoButton = Find(forge, "AutoForgeButton").gameObject;
            Transform top = Find(forge, "AutoForgeButton/ButtonParent/Top");
            ctrl.objAutoRunning = Find(top, "RotateImage").gameObject;
            ctrl.objAutoIdle = Find(top, "AutoNotActive").gameObject;
            // RotateImage gốc có Button riêng → tắt để bấm vẫn đi lên nút Auto.
            Button rotateButton = ctrl.objAutoRunning.GetComponent<Button>();
            if (rotateButton != null)
                rotateButton.enabled = false;
            ctrl.itemList = Find(forge, "ItemList");
            ctrl.itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ITEM_ELEMENT_OUT).GetComponent<ItemElementView>();
            owner.forgeUI = ctrl;

            PrefabUtility.SaveAsPrefabAsset(lobby, LOBBY, out bool ok);
            string result = "UIMainGame2 ForgeUI" + (ok ? " wired" : " FAILED");
            Debug.Log("[ForgePrefabBuilder] " + result);
            return result;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(lobby);
        }
    }

    // ----- helper -----

    private static ItemElementView WireItemElement(Transform t)
    {
        ItemElementView view = t.GetComponent<ItemElementView>();
        if (view == null)
            view = t.gameObject.AddComponent<ItemElementView>();
        view.button = t.GetComponent<Button>();
        view.imgBackground = Find(t, "BackgroundImage").GetComponent<Image>();
        view.imgItem = Find(t, "BackgroundImage/ItemImage").GetComponent<Image>();
        view.txtLevel = Text(t, "LevelText");
        view.txtLevel.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(LEVEL_TEXT_MATERIAL);
        view.objAncient = Find(t, "BackgroundImage/Mask/Ancient").gameObject;
        view.objImmortal = Find(t, "BackgroundImage/Mask/Immortal").gameObject;
        view.objDivine = Find(t, "BackgroundImage/Mask/Divine").gameObject;
        HideAll(t, "TutorialUI");
        HideAll(t, "NotificationUI");
        return view;
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn, có Button để đóng) → bản rip đã unpack.
    private static GameObject NewRoot(string name, string ripPath, out Transform popup, out Button btBackground)
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

        popup = Instantiate(ripPath, contentAll.transform).transform;
        return root;
    }

    private static GameObject Instantiate(string path, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new System.Exception("[ForgePrefabBuilder] Không thấy prefab: " + path);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return go;
    }

    private static string Save(GameObject root, string path)
    {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = System.IO.Path.GetFileNameWithoutExtension(path) + (ok ? " saved" : " FAILED");
        Debug.Log("[ForgePrefabBuilder] " + result);
        return result;
    }

    private static void HideAll(Transform root, string namePrefix)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != root && t.name.StartsWith(namePrefix))
                t.gameObject.SetActive(false);
        }
    }

    private static Button FindButtonStartsWith(Transform root, string namePrefix)
    {
        foreach (Transform t in root)
        {
            if (t.name.StartsWith(namePrefix))
                return t.GetComponent<Button>();
        }
        throw new System.Exception("[ForgePrefabBuilder] Không thấy nút " + namePrefix + " trong " + root.name);
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[ForgePrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();

    private static TMP_Text RichText(Transform root, string path)
    {
        TMP_Text text = Text(root, path);
        text.richText = true;
        return text;
    }
}
