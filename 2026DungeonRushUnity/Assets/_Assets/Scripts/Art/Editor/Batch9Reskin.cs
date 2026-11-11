using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 9 – Hồ sơ & tài khoản theo concept tools/concept/batch9/concept.png (kit chung PopupStyle + helper Batch7Reskin).
// Profile/Settings/Avatar/Rate Us đã reskin theo mockup BallIdle → GIỮ bố cục + art, chỉ đồng bộ khung 816/X/nút/chữ; popup giữ
// nguyên chỗ trên màn (không kéo về giữa). User duyệt 2026-11-12: cam = chính, tím = phụ, nhãn nâu, Avatar cao 916, Language 626,
// gợi ý tên, Sign In sắp lại + link xanh gạch chân, bỏ dòng phụ Sign Up, Choose Save chữ nâu + tên màn, Rate Us Submit cam/xám.
// KHÔNG gọi ProfilePrefabBuilder / Account popups builder. Toạ độ = concept × 2, đo từ MÉP TRÊN khung tới TÂM. Chạy lại ra cùng kết quả.
public static class Batch9Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string ITEMS = "Assets/_Assets/Prefabs/UINew/Profile/";
    private const float W = PopupStyle.WIDTH_MEDIUM;                        // 816
    private const float INNER = W - 2f * PopupStyle.SIDE_INSET;             // 708
    private const float BOTTOM = PopupStyle.BOTTOM_INSET;                   // 58
    private const float BTN = PopupStyle.BUTTON_HEIGHT;                     // 150

    private static readonly Color INPUT_TEXT = new Color32(90, 63, 34, 255);   // #5A3F22 chữ nhập
    private static readonly Color INPUT_HINT = new Color32(154, 119, 82, 255); // #9A7752 gợi ý
    private static readonly Color LINK = new Color32(47, 127, 193, 255);       // #2F7FC1 link

    [MenuItem("Tools/DungeonRush/Reskin/Batch 9 (Profile & Account)")]
    public static string ApplyAll()
    {
        ApplyProfile();
        ApplyAvatar();
        ApplyLanguage();
        ApplyNickname();
        ApplySignIn();
        ApplySignUp();
        ApplyAccountSelect();
        ApplyCloudSave();
        ApplyRateUs();
        return "ok";
    }

    private delegate void Edit(GameObject root);

    private static void EditPrefab(string path, Edit edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[Batch9Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Profile
    public static void ApplyProfile()
    {
        EditPrefab(UI + "UIProfilePopup.prefab", root =>
        {
            UIProfilePopup ui = root.GetComponent<UIProfilePopup>();
            RectTransform frame = (RectTransform)Batch7Reskin.Find(root.transform, "Content");
            frame.sizeDelta = new Vector2(W, frame.sizeDelta.y);
            Batch7Reskin.Close(ui.btClose);

            Transform profile = ui.objProfilePage.transform;
            Transform settings = ui.objSettingsPage.transform;
            PopupStyle.Title(profile.Find("TitleText").GetComponent<TMP_Text>(), W);
            PopupStyle.Title(settings.Find("TitleText").GetComponent<TMP_Text>(), W);

            // Redeem Code (coming soon) = nút phụ tím.
            Transform redeem = Batch7Reskin.Find(profile, "RedeemCodeButton");
            PopupStyle.Button(redeem, PopupStyle.ButtonColor.Violet, 282f, 138f, 38f);
            Batch7Reskin.Button(redeem.GetComponent<Button>());

            // Settings (khối ×1.21): nhãn nâu không viền; nút tài khoản cam/đỏ, Sync Now + Report Bug tím. Giữ ppu 9-slice của khối.
            string[] labels = { "SoundFXInfo", "MusicInfo", "VibrationInfo", "DiscordInfo", "CountryInfo", "LanguageInfo", "RestoreInfo", "AccountLabel", "ReportLabel" };
            foreach (string name in labels)
            {
                TMP_Text label = Batch7Reskin.Find(settings, name).GetComponent<TMP_Text>();
                PopupStyle.Plain(label, PopupStyle.BROWN, 32f, 20f);
                Batch7Reskin.Normal(label);
            }
            PopupStyle.Plain(ui.txtAccountInfo, PopupStyle.BROWN, 26f, 18f);
            Batch7Reskin.Normal(ui.txtAccountInfo);
            ui.txtAccountInfo.alignment = TextAlignmentOptions.Center;
            PopupStyle.Plain(ui.txtUserId, PopupStyle.BROWN, 28f, 18f);
            Batch7Reskin.Normal(ui.txtUserId);
            PopupStyle.Plain(ui.txtVersion, PopupStyle.MUTE, 28f, 18f);
            Batch7Reskin.Normal(ui.txtVersion);
            Image copy = ui.btCopyUserId.GetComponent<Image>();
            copy.color = PopupStyle.BROWN;

            ui.sprAccountSignIn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprAccountSignOut = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Red);
            KeepSlice(ui.btAccount.image, ui.sprAccountSignIn);
            KeepSlice(ui.btSyncNow.image, PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet));
            KeepSlice(Batch7Reskin.Find(settings, "ReportButton").GetComponent<Image>(), PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet));
            ui.imgSyncLoading.color = new Color32(122, 92, 60, 220);
        });
    }

    // Đổi sprite nút nằm trong khối phóng ×1.21: giữ type Sliced + pixelsPerUnitMultiplier đã chỉnh (Img đặt lại về 1).
    private static void KeepSlice(Image image, Sprite sprite)
    {
        float ppu = image.pixelsPerUnitMultiplier;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.pixelsPerUnitMultiplier = ppu;
    }
    #endregion

    #region Avatar / Language
    private const float AVATAR_HEIGHT = 916f;
    private const float LANGUAGE_HEIGHT = 626f;

    public static void ApplyAvatar()
    {
        EditPrefab(UI + "UIAvatarSelectPopup.prefab", root =>
        {
            UIAvatarSelectPopup ui = root.GetComponent<UIAvatarSelectPopup>();
            RectTransform frame = (RectTransform)Batch7Reskin.Find(root.transform, "Content");
            frame.sizeDelta = new Vector2(W, AVATAR_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(frame, "NicknameText").GetComponent<TMP_Text>(), W);
            // 22 avatar = 5 hàng vừa khít ô tan 690 → không cuộn, khung thấp lại.
            PopupStyle.PlaceTop(Batch7Reskin.Find(frame, "Avatars"), 0f, 168f + 345f, 720f, 690f);
        });
    }

    public static void ApplyLanguage()
    {
        EditPrefab(UI + "UILanguageSelectPopup.prefab", root =>
        {
            UILanguageSelectPopup ui = root.GetComponent<UILanguageSelectPopup>();
            RectTransform container = (RectTransform)Batch7Reskin.Find(root.transform, "LanguageSelectPopup");
            container.sizeDelta = new Vector2(W, container.sizeDelta.y);
            RectTransform frame = (RectTransform)container.Find("Content");
            frame.sizeDelta = new Vector2(frame.sizeDelta.x, LANGUAGE_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(frame, "TitleText").GetComponent<TMP_Text>(), W);

            // Ô tan 708×396 ôm 2 hàng cờ 108 + tên bên dưới; mask chữ nhật.
            ScrollRect scroll = Batch7Reskin.Find(frame, "Scroll View").GetComponent<ScrollRect>();
            PopupStyle.PlaceTop(scroll.transform, 0f, 168f + 198f, INNER, 396f);
            RectTransform viewport = scroll.viewport;
            PopupStyle.RemoveComponent<Mask>(viewport);
            Batch7Reskin.GetOrAdd<RectMask2D>(viewport.gameObject);
            PopupStyle.Invisible(viewport.GetComponent<Image>());
            GridLayoutGroup grid = ((RectTransform)ui.gridContent).GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(108f, 108f);
            grid.spacing = new Vector2(30f, 80f);
            grid.padding = new RectOffset(0, 0, 20, 80);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
        });
        EditPrefab(ITEMS + "LanguageItemUI.prefab", root =>
        {
            Transform selected = root.transform.Find("Selected");
            Image tick = ReskinPrefabUtil.Img(selected, PopupStyle.Kit("Profile/icon_check"));
            tick.preserveAspect = true;
            PopupStyle.Center(selected, 34f, -34f, 56f, 56f);
            TMP_Text name = root.transform.Find("LanguageName").GetComponent<TMP_Text>();
            PopupStyle.Center(name.transform, 0f, -86f, 136f, 56f);
            PopupStyle.Outline(name, 30f, 18f);
            Batch7Reskin.Normal(name);
            name.alignment = TextAlignmentOptions.Center;
            name.enableWordWrapping = false;
        });
    }
    #endregion

    #region Nickname / Sign in / Sign up / Cloud save
    private const float NICKNAME_HEIGHT = 596f;
    private const float SIGNIN_HEIGHT = 790f;
    private const float SIGNUP_HEIGHT = 738f;
    private const float CLOUD_HEIGHT = 626f;

    public static void ApplyNickname()
    {
        EditPrefab(UI + "UINicknamePopup.prefab", root =>
        {
            UINicknamePopup ui = root.GetComponent<UINicknamePopup>();
            RectTransform panel = (RectTransform)Batch7Reskin.Find(root.transform, "ContentTransform");
            panel.sizeDelta = new Vector2(W, NICKNAME_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(panel, "TitleText").GetComponent<TMP_Text>(), W);

            InputStyle(ui.inputName, 238f);
            TMP_Text hint = PopupStyle.GetOrCreateText(panel, "HintText");
            PopupStyle.PlaceTop(hint.transform, 0f, 332f, INNER, 40f);
            PopupStyle.Plain(hint, PopupStyle.MUTE, 32f, 22f);
            Batch7Reskin.Normal(hint);
            hint.alignment = TextAlignmentOptions.Center;
            hint.enableWordWrapping = false;
            PopupStyle.SetKey(hint, "Popup.Nickname.Error.Length", "Name must be 3-15 characters");

            MainButton(ui.btEnter, PopupStyle.ButtonColor.Orange, 463f, 300f);
            Loading(Batch7Reskin.Find(panel, "LoadingImage"), 463f);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    public static void ApplySignIn()
    {
        EditPrefab(UI + "UISignInPopup.prefab", root =>
        {
            UISignInPopup ui = root.GetComponent<UISignInPopup>();
            RectTransform frame = Frame(root, "SignInPopup", SIGNIN_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(frame, "TitleText").GetComponent<TMP_Text>(), W);

            // Ô nhập đang nằm ngoài khung (con của container) → đưa vào khung.
            Batch7Reskin.Reparent(ui.inputEmail.transform, frame);
            Batch7Reskin.Reparent(ui.inputPassword.transform, frame);
            InputStyle(ui.inputEmail, 238f);
            InputStyle(ui.inputPassword, 366f);

            // Email → Password → Forgot (phải) → Sign In cam → "Don't have an account? Sign up" ở đáy.
            Link(ui.btForgotPassword.GetComponent<TMP_Text>(), 462f, 32f, TextAlignmentOptions.MidlineRight);
            MainButton(ui.btSignIn, PopupStyle.ButtonColor.Orange, 587f, 340f);
            Loading(ui.imgLoading.transform, 587f);
            Link(ui.btSwitchToSignUp.GetComponent<TMP_Text>(), 707f, 36f, TextAlignmentOptions.Center);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    public static void ApplySignUp()
    {
        EditPrefab(UI + "UISignUpPopup.prefab", root =>
        {
            UISignUpPopup ui = root.GetComponent<UISignUpPopup>();
            RectTransform frame = Frame(root, "SignUpPopup", SIGNUP_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(frame, "TitleText").GetComponent<TMP_Text>(), W);
            // Dòng phụ dùng trùng key tiêu đề → bỏ.
            Batch7Reskin.Find(frame, "InfoText").gameObject.SetActive(false);

            Batch7Reskin.Reparent(ui.inputEmail.transform, frame);
            Batch7Reskin.Reparent(ui.inputPassword.transform, frame);
            InputStyle(ui.inputEmail, 238f);
            InputStyle(ui.inputPassword, 366f);
            MainButton(ui.btSignUp, PopupStyle.ButtonColor.Orange, 535f, 340f);
            Loading(ui.imgLoading.transform, 535f);
            Link(ui.btSwitchToSignIn.GetComponent<TMP_Text>(), 655f, 36f, TextAlignmentOptions.Center);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    public static void ApplyCloudSave()
    {
        EditPrefab(UI + "UICloudSavePopup.prefab", root =>
        {
            UICloudSavePopup ui = root.GetComponent<UICloudSavePopup>();
            RectTransform container = (RectTransform)Batch7Reskin.Find(root.transform, "CloudSavePopup");
            container.sizeDelta = new Vector2(W, container.sizeDelta.y);
            RectTransform panel = (RectTransform)container.Find("ContentTransform");
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, CLOUD_HEIGHT);
            Batch7Reskin.Close(ui.btClose);
            PopupStyle.Title(Batch7Reskin.Find(panel, "TitleText").GetComponent<TMP_Text>(), W);

            TMP_Text desc = Batch7Reskin.Find(panel, "DescriptionText").GetComponent<TMP_Text>();
            PopupStyle.PlaceTop(desc.transform, 0f, 250f, INNER, 132f);
            PopupStyle.Plain(desc, PopupStyle.BROWN, 36f, 24f);
            Batch7Reskin.Normal(desc);
            desc.alignment = TextAlignmentOptions.Center;

            MainButton(ui.btSave, PopupStyle.ButtonColor.Orange, 493f, 300f);
            Loading(ui.imgLoading.transform, 493f);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Khung = node "Content" (ảnh khung) giãn ngang theo container → đặt rộng container 816, cao khung `height`.
    private static RectTransform Frame(GameObject root, string containerName, float height)
    {
        RectTransform container = (RectTransform)Batch7Reskin.Find(root.transform, containerName);
        container.sizeDelta = new Vector2(W, container.sizeDelta.y);
        RectTransform frame = (RectTransform)container.Find("Content");
        frame.sizeDelta = new Vector2(frame.sizeDelta.x, height);
        return frame;
    }

    // Ô nhập tan rộng INNER cao 108, chữ nhập nâu đậm, gợi ý nâu nhạt.
    private static void InputStyle(TMP_InputField input, float topToCenter)
    {
        ReskinPrefabUtil.Img(input.transform, PopupStyle.Kit("ClanSearch/input_tan"));
        PopupStyle.PlaceTop(input.transform, 0f, topToCenter, INNER, 108f);
        input.transition = Selectable.Transition.None;
        PopupStyle.Stretch(input.textViewport, 24f, 12f, 24f, 12f);
        TMP_Text text = input.textComponent;
        PopupStyle.Stretch(text.transform);
        PopupStyle.Plain(text, INPUT_TEXT, 40f);
        Batch7Reskin.Normal(text);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        TMP_Text hint = input.placeholder as TMP_Text;
        if (hint != null)
        {
            PopupStyle.Stretch(hint.transform);
            PopupStyle.Plain(hint, INPUT_HINT, 40f);
            Batch7Reskin.Normal(hint);
            hint.alignment = TextAlignmentOptions.MidlineLeft;
            hint.enableWordWrapping = false;
        }
        input.pointSize = 40f;
    }

    // Link chữ xanh gạch chân, không viền (TMP có Button bấm được).
    private static void Link(TMP_Text text, float topToCenter, float size, TextAlignmentOptions align)
    {
        PopupStyle.PlaceTop(text.transform, 0f, topToCenter, INNER, 56f);
        PopupStyle.Plain(text, LINK, size, 22f);
        text.fontStyle = FontStyles.Underline;
        text.alignment = align;
        text.enableWordWrapping = false;
        text.raycastTarget = true;
        Batch7Reskin.Button(text.GetComponent<Button>());
    }

    private static void MainButton(Button button, PopupStyle.ButtonColor color, float topToCenter, float width)
    {
        PopupStyle.Button(button.transform, color, width, BTN, 52f);
        PopupStyle.PlaceTop(button.transform, 0f, topToCenter, width, BTN);
        Batch7Reskin.Button(button);
    }

    // Icon xoay tông nâu thế chỗ nút khi đang xử lý.
    private static void Loading(Transform loading, float topToCenter)
    {
        PopupStyle.PlaceTop(loading, 0f, topToCenter, 76f, 76f);
        loading.GetComponent<Image>().color = new Color32(122, 92, 60, 220);
        loading.SetAsLastSibling();
    }
    #endregion

    #region Account select
    private const float SELECT_HEIGHT = 598f;
    private const float CARD_W = 342f, CARD_H = 372f, CARD_X = 183f, CARD_TOP = 168f + CARD_H / 2f;

    public static void ApplyAccountSelect()
    {
        EditPrefab(UI + "UIAccountSelectPopup.prefab", root =>
        {
            UIAccountSelectPopup ui = root.GetComponent<UIAccountSelectPopup>();
            RectTransform frame = Frame(root, "AccountSelectPopup", SELECT_HEIGHT);
            TMP_Text title = Batch7Reskin.Find(frame, "TitleText").GetComponent<TMP_Text>();
            PopupStyle.Title(title, W);
            PopupStyle.SetKey(title, "Popup.AccountSelect.Title", "Choose Save");
            Batch7Reskin.Find(frame, "TitleText_1").gameObject.SetActive(false);

            Card(frame, "Image", "Local", -CARD_X, ui.txtLocalName, ui.txtLocalProgress, ui.btKeepLocal, PopupStyle.ButtonColor.Violet);
            Card(frame, "Image_1", "Cloud", CARD_X, ui.txtCloudName, ui.txtCloudProgress, ui.btUseCloud, PopupStyle.ButtonColor.Orange);
        });
    }

    // Ô tan 342×372: tên trắng viền, 3 dòng nâu (level / tên màn / lưu lúc), nút chọn.
    private static void Card(Transform frame, string boxName, string groupName, float x, TMP_Text name, TMP_Text progress, Button button, PopupStyle.ButtonColor color)
    {
        PopupStyle.PlaceTop(Batch7Reskin.Find(frame, boxName), x, CARD_TOP, CARD_W, CARD_H);
        Transform group = Batch7Reskin.Find(frame, groupName);
        PopupStyle.RemoveComponent<VerticalLayoutGroup>(group);
        PopupStyle.PlaceTop(group, x, CARD_TOP, CARD_W, CARD_H);

        PopupStyle.Center(name.transform, 0f, 140f, CARD_W - 32f, 48f);
        PopupStyle.Outline(name, 38f, 22f);
        Batch7Reskin.Normal(name);
        name.alignment = TextAlignmentOptions.Center;
        name.enableWordWrapping = false;

        // 3 dòng; dòng dài (PT "Último salvamento: desconhecido") xuống dòng trước rồi mới co, để cả khối không bé đi.
        PopupStyle.Center(progress.transform, 0f, 44f, CARD_W - 24f, 140f);
        PopupStyle.Plain(progress, PopupStyle.BROWN, 30f, 22f);
        Batch7Reskin.Normal(progress);
        progress.alignment = TextAlignmentOptions.Center;
        progress.enableWordWrapping = true;
        progress.lineSpacing = 0f;

        PopupStyle.Button(button.transform, color, 280f, 112f, 38f);
        PopupStyle.Center(button.transform, 0f, -100f, 280f, 112f);
        Batch7Reskin.Button(button);
    }
    #endregion

    #region Rate us
    public static void ApplyRateUs()
    {
        EditPrefab(UI + "UIRateUsPopup.prefab", root =>
        {
            UIRateUsPopup ui = root.GetComponent<UIRateUsPopup>();
            RectTransform popup = (RectTransform)Batch7Reskin.Find(root.transform, "Popup");
            popup.sizeDelta = new Vector2(W, popup.sizeDelta.y);
            Batch7Reskin.Close(ui.LaterButton);
            PopupStyle.Title(Batch7Reskin.Find(popup, "TitleText").GetComponent<TMP_Text>(), W);

            // Submit cam; chưa chọn sao = sprite xám (script đổi), không tô màu.
            ui.SubmitOnSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.SubmitOffSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            Transform submit = ui.SubmitButton.transform;
            Vector2 pos = ((RectTransform)submit).anchoredPosition;
            PopupStyle.Button(submit, PopupStyle.ButtonColor.Orange, 300f, BTN, 56f);
            ((RectTransform)submit).anchoredPosition = pos;
            Batch7Reskin.Button(ui.SubmitButton);
            ui.LaterButton.transform.SetAsLastSibling();
        });
    }
    #endregion
}
