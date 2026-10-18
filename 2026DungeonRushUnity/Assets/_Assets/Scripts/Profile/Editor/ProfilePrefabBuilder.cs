using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng prefab UI nhóm Profile (Resources/Prefabs/UI/UIProfilePopup, UIPlayerPopup, UIAvatarSelectPopup + 5 popup tài khoản) từ prefab rip layout-only ở
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
    private const string CUR = "Assets/_Assets/Art/2_Icons/Currency/";
    private const string TAB_ITEM = "Assets/_Assets/Prefabs/UINew/Clan/TabSelectorItemUI.prefab";
    private const string SETTINGS = "Content/SettingsPage/VerticalLayout/";
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

    // ===== Hồ sơ của mình (Profile + Settings) =====

    [MenuItem("Tools/DungeonRush/Build Profile UI/Profile Popup")]
    public static string BuildProfilePopup()
    {
        GameObject root = NewRoot("UIProfilePopup", out Transform popup, RIP + "ProfilePopup.prefab");
        UIProfilePopup ui = root.AddComponent<UIProfilePopup>();
        ui.isPopup = true;
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.objProfilePage = Find(popup, "Content/ProfilePage").gameObject;
        ui.objSettingsPage = Find(popup, "Content/SettingsPage").gameObject;

        // Thanh tab: TabSelector sinh nút lúc chạy từ prefab TabSelectorItemUI (dùng chung với Clan).
        Transform tabs = Find(popup, "Content/TabSelectorUI");
        ui.tabSelector = tabs.gameObject.AddComponent<TabSelector>();
        SerializedObject tabSelector = new SerializedObject(ui.tabSelector);
        tabSelector.FindProperty("_selectorTransform").objectReferenceValue = Find(tabs, "Selector");
        tabSelector.FindProperty("_itemContainer").objectReferenceValue = Find(tabs, "ItemContainer");
        tabSelector.FindProperty("_itemPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TabSelectorItem>(TAB_ITEM);
        tabSelector.ApplyModifiedPropertiesWithoutUndo();

        Transform profile = ui.objProfilePage.transform;
        ui.txtName = Text(profile, "NicknameParent/NicknameText");
        ui.txtPower = Text(profile, "PowerButton/Content/PowerText");
        SetSprite(ui.txtPower, "IsPower");
        ui.imgAvatar = Find(profile, "AvatarParent/AvatarImage - Button").GetComponent<Image>();
        ui.btAvatar = Btn(profile, "AvatarParent/AvatarImage - Button");
        ui.btEditAvatar = Btn(profile, "AvatarEditButton");
        ui.btEditName = Btn(profile, "NicknameParent/EditNicknaemButton");

        ui.sliderSound = Find(popup, SETTINGS + "SoundFX/SoundFXSlider").GetComponent<Slider>();
        ui.imgSoundHandle = Find(ui.sliderSound.transform, "AudioHandle").GetComponent<Image>();
        ui.txtSoundStatus = Text(ui.sliderSound.transform, "AudioHandle/OnOffText");
        ui.sliderMusic = Find(popup, SETTINGS + "Music/MusicSlider").GetComponent<Slider>();
        ui.imgMusicHandle = Find(ui.sliderMusic.transform, "MusicHandle").GetComponent<Image>();
        ui.txtMusicStatus = Text(ui.sliderMusic.transform, "MusicHandle/OnOffText_1");

        Transform vibrate = Find(popup, SETTINGS + "Vibration/VibrationToggle");
        ui.toggleVibrate = vibrate.gameObject.AddComponent<ToggleButton>();
        ui.toggleVibrate.Button = vibrate.GetComponent<Button>();
        ui.toggleVibrate.ParentRectTransform = (RectTransform)vibrate;
        ui.toggleVibrate.ChildRectTransform = (RectTransform)Find(vibrate, "Button");
        ui.toggleVibrate.ChildButtonImage = Find(vibrate, "Button").GetComponent<Image>();
        ui.toggleVibrate.StatusText = Find(vibrate, "Text (TMP)").GetComponent<TextMeshProUGUI>();
        ui.toggleVibrate.Padding = 40f;   // ToggleButton.Padding gốc

        ui.txtUserId = Text(popup, SETTINGS + "UserID_Info/UserIdText");
        ui.btCopyUserId = Btn(popup, SETTINGS + "UserID_Info/CopyButton");
        ui.txtVersion = Text(popup, SETTINGS + "Version_Info/VersionText");
        WireAccountSection(ui, popup);

        ui.btDiscord = Btn(popup, SETTINGS + "Discord/DiscordButton");
        WireLanguageButton(ui, popup);

        // Tính năng project chưa có: giữ nguyên bố cục gốc, bấm báo "coming soon".
        ui.listButtonComingSoon = new List<Button>
        {
            Btn(profile, "RedeemCodeButton"),
            Btn(popup, SETTINGS + "Country/CountryButton"),
            Btn(popup, SETTINGS + "ReportBug/ReportButton"),
        };
        // "Redeem Code" bị ngắt 2 dòng với font project.
        Text(profile, "RedeemCodeButton/Content/RedeemCodeText").textWrappingMode = TextWrappingModes.NoWrap;
        return Finish(Save(root, "UIProfilePopup"));
    }

    // Mục Account ở trang Settings: nút "Sign In / Up" ↔ "Sign Out", dòng "email · Last sync", nút Sync Now + vòng xoay.
    private static void WireAccountSection(UIProfilePopup ui, Transform popup)
    {
        Transform account = Find(popup, SETTINGS + "AccountSection");
        ui.btAccount = Btn(account, "AccountButton");
        ui.txtAccountButton = Text(account, "AccountButton/Content/OnOffText_1");
        ui.txtAccountInfo = Text(account, "AccountInfoLabel");
        ui.btSyncNow = Btn(account, "SyncButton");
        ui.imgSyncLoading = Find(account, "LoadingImage").GetComponent<Image>();
        Text(account, "SyncButton/Content/OnOffText_1").text = "Sync Now";
    }

    // UIProfilePopup dựng TRƯỚC khi có hệ tài khoản: nối thêm mục Account + nút đổi tên vào prefab đang có, không dựng lại
    // (giữ nguyên mọi chỉnh sửa bố cục bằng tay).
    [MenuItem("Tools/DungeonRush/Build Profile UI/Profile Popup - Wire Account Only")]
    public static string WireProfileAccount()
    {
        string path = OUT + "UIProfilePopup.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        UIProfilePopup ui = root.GetComponent<UIProfilePopup>();
        Transform popup = Find(root.transform, "ContentAll/ProfilePopup");
        ui.btEditName = Btn(popup, "Content/ProfilePage/NicknameParent/EditNicknaemButton");
        WireAccountSection(ui, popup);
        ui.listButtonComingSoon.Remove(ui.btEditName);
        ui.listButtonComingSoon.Remove(ui.btAccount);
        ui.listButtonComingSoon.Remove(ui.btSyncNow);
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        PrefabUtility.UnloadPrefabContents(root);
        return Finish("UIProfilePopup account " + (ok ? "wired" : "FAILED"));
    }

    // Nút Language (EditLanguageButton / LanguageFlagImage / LanguageNameText gốc): cờ + tên vẽ lúc chạy (krj).
    private static void WireLanguageButton(UIProfilePopup ui, Transform popup)
    {
        ui.btLanguage = Btn(popup, SETTINGS + "Language/LanguageButton");
        ui.imgLanguageFlag = Find(popup, SETTINGS + "Language/LanguageButton/Content/FlagImage").GetComponent<Image>();
        ui.txtLanguageName = Text(popup, SETTINGS + "Language/LanguageButton/Content/LanguageName");
        ui.txtLanguageName.text = "English";
    }

    // Nối nút Language vào UIProfilePopup đang có (không dựng lại, giữ chỉnh sửa bố cục bằng tay).
    [MenuItem("Tools/DungeonRush/Build Profile UI/Profile Popup - Wire Language Only")]
    public static string WireProfileLanguage()
    {
        string path = OUT + "UIProfilePopup.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        UIProfilePopup ui = root.GetComponent<UIProfilePopup>();
        Transform popup = Find(root.transform, "ContentAll/ProfilePopup");
        WireLanguageButton(ui, popup);
        ui.listButtonComingSoon.Remove(ui.btLanguage);
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        PrefabUtility.UnloadPrefabContents(root);
        return Finish("UIProfilePopup language " + (ok ? "wired" : "FAILED"));
    }

    // ===== Chọn ngôn ngữ (LanguageSelectPopup gốc) =====

    private const string RIP_SETTINGS = "Assets/_Assets/Prefabs/UI/Settings/";
    private const string FLAGS = "Assets/_Assets/Art/2_Icons/Flags/Language/";

    // Languages gốc của LanguageSelectPopup (thứ tự, tên hiển thị, cờ trích từ xapk — giữ cả khoảng trắng thừa như gốc).
    private static readonly string[][] LANGUAGES =
    {
        new[] { "en", "English", "GB" },
        new[] { "tr", "Türkçe", "TR" },
        new[] { "ar", "عربي", "SA" },
        new[] { "fr", "Français", "FR" },
        new[] { "de", "Deutsch ", "DE" },
        new[] { "it", "Italiano", "IT" },
        new[] { "ja", "日本語 ", "JP" },
        new[] { "ko", "한국인", "KR" },
        new[] { "es", "Español", "ES" },
        new[] { "pt-BR", "Português", "BR" },
    };

    [MenuItem("Tools/DungeonRush/Build Profile UI/Language Select Popup")]
    public static string BuildLanguageSelectPopup()
    {
        // Ô ngôn ngữ (LanguageItemUI gốc): Image gốc = cờ, Selected = dấu tích, LanguageName = tên.
        GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RIP_SETTINGS + "LanguageItemUI.prefab"));
        PrefabUtility.UnpackPrefabInstance(item, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        ElementLanguageUI e = item.AddComponent<ElementLanguageUI>();
        e.imgFlag = item.GetComponent<Image>();
        e.txtName = Text(item.transform, "LanguageName");
        e.objSelected = Find(item.transform, "Selected").gameObject;
        e.btSelect = item.GetComponent<Button>();
        if (!AssetDatabase.IsValidFolder(BUILT)) AssetDatabase.CreateFolder("Assets/_Assets/Prefabs/UINew", "Profile");
        string itemPath = BUILT + "/LanguageItemUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(item, itemPath);
        Object.DestroyImmediate(item);

        GameObject root = NewRoot("UILanguageSelectPopup", out Transform popup, RIP_SETTINGS + "LanguageSelectPopup.prefab");
        UILanguageSelectPopup ui = root.AddComponent<UILanguageSelectPopup>();
        ui.isPopup = true;
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.gridContent = Find(popup, "Content/Scroll View/Viewport/GridLayout");
        ui.elementPrefab = AssetDatabase.LoadAssetAtPath<ElementLanguageUI>(itemPath);
        ui.languages = new List<UILanguageSelectPopup.LanguageOption>();
        foreach (string[] l in LANGUAGES)
        {
            Sprite flag = AssetDatabase.LoadAssetAtPath<Sprite>(FLAGS + l[2] + ".png");
            if (flag == null) Debug.LogWarning("[ProfilePrefabBuilder] Thiếu cờ " + l[2]);
            ui.languages.Add(new UILanguageSelectPopup.LanguageOption { code = l[0], displayName = l[1], flag = flag });
        }
        return Finish(Save(root, "UILanguageSelectPopup"));
    }

    // ===== Tài khoản: Sign In / Sign Up / chọn save / Cloud Backup / đặt tên =====

    [MenuItem("Tools/DungeonRush/Build Profile UI/Account Popups")]
    public static string BuildAccountPopups()
    {
        return BuildSignInPopup() + ", " + BuildSignUpPopup() + ", " + BuildAccountSelectPopup() + ", "
            + BuildCloudSavePopup() + ", " + BuildNicknamePopup();
    }

    private static string BuildSignInPopup()
    {
        GameObject root = NewRoot("UISignInPopup", out Transform popup, RIP + "SignInPopup.prefab");
        UISignInPopup ui = root.AddComponent<UISignInPopup>();
        ui.isPopup = true;
        ui.inputEmail = Input(popup, "EmailField", "Enter Email");
        ui.inputPassword = Input(popup, "PasswordField", "Enter Password");
        ui.btSignIn = Btn(popup, "Content/SignUpButton");
        ui.btForgotPassword = Btn(popup, "Content/ForgotPasswordButton");
        ui.btSwitchToSignUp = Btn(popup, "Content/SignUpTextButton");
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.imgLoading = Find(popup, "Content/LoadingImage").GetComponent<Image>();
        // Chữ theo bảng ngôn ngữ gốc (Popup.SignIn.*) — bản rip còn chữ mẫu của Sign Up.
        Text(popup, "Content/TitleText").text = "Sign In";
        Text(popup, "Content/SignUpButton/Content/Text").text = "Sign In";
        Text(popup, "Content/ForgotPasswordButton").text = "Forgot password?";
        return Finish(Save(root, "UISignInPopup"));
    }

    private static string BuildSignUpPopup()
    {
        GameObject root = NewRoot("UISignUpPopup", out Transform popup, RIP + "SignUpPopup.prefab");
        UISignUpPopup ui = root.AddComponent<UISignUpPopup>();
        ui.isPopup = true;
        ui.inputEmail = Input(popup, "EmailField", "Enter Email");
        ui.inputPassword = Input(popup, "PasswordField", "Enter Password");
        ui.btSignUp = Btn(popup, "Content/SignUpButton");
        ui.btSwitchToSignIn = Btn(popup, "Content/SignInTextButton");
        ui.btClose = Btn(popup, "Content/CloseButton");
        ui.imgLoading = Find(popup, "Content/LoadingImage").GetComponent<Image>();
        Text(popup, "Content/SignUpButton/Content/Text").text = "Sign Up";
        return Finish(Save(root, "UISignUpPopup"));
    }

    private static string BuildAccountSelectPopup()
    {
        GameObject root = NewRoot("UIAccountSelectPopup", out Transform popup, RIP + "AccountSelectPopup.prefab");
        UIAccountSelectPopup ui = root.AddComponent<UIAccountSelectPopup>();
        ui.isPopup = true;
        ui.txtLocalName = Text(popup, "Content/Local/NicknameText");
        ui.txtLocalProgress = Text(popup, "Content/Local/ProgressText");
        ui.btKeepLocal = Btn(popup, "Content/Local/LocalButton");
        ui.txtCloudName = Text(popup, "Content/Cloud/NicknameText");
        ui.txtCloudProgress = Text(popup, "Content/Cloud/ProgressText");
        ui.btUseCloud = Btn(popup, "Content/Cloud/CloudButton");
        Text(popup, "Content/TitleText").text = "Choose Save";
        // Bắt buộc chọn 1 trong 2 → bỏ nút đóng.
        Find(popup, "Content/CloseButton").gameObject.SetActive(false);
        return Finish(Save(root, "UIAccountSelectPopup"));
    }

    private static string BuildCloudSavePopup()
    {
        GameObject root = NewRoot("UICloudSavePopup", out Transform popup, RIP + "CloudSavePopup.prefab");
        UICloudSavePopup ui = root.AddComponent<UICloudSavePopup>();
        ui.isPopup = true;
        ui.btSave = Btn(popup, "ContentTransform/SaveButton");
        ui.btClose = Btn(popup, "ContentTransform/CloseButton");
        ui.imgLoading = Find(popup, "ContentTransform/LoadingImage").GetComponent<Image>();
        return Finish(Save(root, "UICloudSavePopup"));
    }

    private static string BuildNicknamePopup()
    {
        GameObject root = NewRoot("UINicknamePopup", out Transform popup, RIP + "NicknamePopup.prefab");
        UINicknamePopup ui = root.AddComponent<UINicknamePopup>();
        ui.isPopup = true;
        ui.inputName = Input(popup, "ContentTransform/NicknameInputField", "Enter Nickname...");
        ui.inputName.characterLimit = 15;
        ui.btEnter = Btn(popup, "ContentTransform/NicknamePopupEnterButton");
        ui.btClose = Btn(popup, "ContentTransform/NicknamePopupCloseButton");
        return Finish(Save(root, "UINicknamePopup"));
    }

    private static TMP_InputField Input(Transform root, string path, string placeholder)
    {
        TMP_InputField input = Find(root, path).GetComponent<TMP_InputField>();
        ((TMP_Text)input.placeholder).text = placeholder;
        return input;
    }

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
