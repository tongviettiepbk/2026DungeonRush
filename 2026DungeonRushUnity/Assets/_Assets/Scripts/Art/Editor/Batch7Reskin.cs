using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 7 – Clan theo concept tools/concept/batch7/concept.png (kit chung PopupStyle).
// GIỮ kit Clan đã reskin (thẻ xám/xanh 1_UIKit/Clan, nút −/+ + tick 1_UIKit/ClanSearch, banner ghép); chỉ đồng bộ khung/nhãn/nút/dòng.
// User duyệt 2026-11-12: lưới chọn banner thấy hết, tên clan / tên người chơi lên header, thêm nút Clear (Popup.ClanSearch.Clear).
// KHÔNG gọi ClanPrefabBuilder (Build Clan UI Prefabs). Toạ độ = concept × 2, đo từ MÉP TRÊN panel tới TÂM. Chạy lại ra cùng kết quả.
public static class Batch7Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string CARDS = "Assets/_Assets/Prefabs/UINew/Clan/";
    private const float W = PopupStyle.WIDTH_MEDIUM;                        // 816
    private const float INNER = W - 2f * PopupStyle.SIDE_INSET;             // 708
    private const float BTN = PopupStyle.BUTTON_HEIGHT;                     // 150
    private const float BOTTOM = PopupStyle.BOTTOM_INSET;                   // 58

    // Hàng "nhãn | điều khiển": nhãn nâu 236 bên trái, điều khiển bắt đầu từ CONTROL_LEFT (toạ độ so với tâm hàng).
    private const float LABEL_WIDTH = 236f;
    private const float CONTROL_LEFT = -INNER / 2f + LABEL_WIDTH + 24f;    // -94
    private const float CONTROL_WIDTH = INNER / 2f - CONTROL_LEFT;          // 448
    private const float ROW = 108f;

    private static readonly Color INPUT_TEXT = new Color32(90, 63, 34, 255);   // #5A3F22 chữ nhập
    private static readonly Color INPUT_HINT = new Color32(154, 119, 82, 255); // #9A7752 gợi ý

    [MenuItem("Tools/DungeonRush/Reskin/Batch 7 (Clan)")]
    public static string ApplyAll()
    {
        ApplyCreate();
        ApplySettings();
        ApplyBannerEditor();
        ApplyAnnouncement();
        ApplyRequests();
        ApplyInfo();
        ApplyPlayer();
        ApplySearch();
        ApplyLeadership();
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
        Debug.Log("[Batch7Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Create / Settings / Announcement
    private const float CREATE_HEIGHT = 802f;

    public static void ApplyCreate()
    {
        EditPrefab(UI + "UICreateClanPopup.prefab", root =>
        {
            UICreateClanPopup ui = root.GetComponent<UICreateClanPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", CREATE_HEIGHT);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            Transform rows = Unlayout(Find(panel, "VerticalLayout"));

            Row(rows, "Element", 214f, ROW, ui.NameLabel);
            Input(ui.NameInput, CONTROL_LEFT + CONTROL_WIDTH / 2f, CONTROL_WIDTH, 106f, 38f, false);
            Row(rows, "Element_2", 330f, ROW, ui.PublicLabel);
            Check(ui.ApprovalOnlyToggle);
            Row(rows, "Element_3", 470f, 152f, ui.BannerLabel);
            BannerRow(ui.BannerPreview, ui.BannerButton);

            PopupStyle.Button(ui.CreateButton.transform, PopupStyle.ButtonColor.Green, 340f, 168f, 46f);
            PopupStyle.PlaceTop(ui.CreateButton.transform, 0f, 660f, 340f, 168f);
            ui.CreateButtonText.lineSpacing = -20f;
            Button(ui.CreateButton);
            Loading(ui.LoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    private const float SETTINGS_HEIGHT = 1134f;

    public static void ApplySettings()
    {
        EditPrefab(UI + "UIClanSettingsPopup.prefab", root =>
        {
            UIClanSettingsPopup ui = root.GetComponent<UIClanSettingsPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", SETTINGS_HEIGHT);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            Transform rows = Unlayout(Find(panel, "VerticalLayout"));

            Row(rows, "Element_4", 200f, 88f, ui.NameLabel);
            PopupStyle.Center(ui.ClanNameLabel.transform, CONTROL_LEFT + CONTROL_WIDTH / 2f, 0f, CONTROL_WIDTH, 88f);
            PopupStyle.Outline(ui.ClanNameLabel, 48f, 30f);
            Normal(ui.ClanNameLabel);
            ui.ClanNameLabel.alignment = TextAlignmentOptions.MidlineLeft;

            // Khối mô tả: nhãn trên + ô tan 708×284 (đủ 200 ký tự ≈ 6 dòng) + bộ đếm "n/200".
            Transform desc = Find(rows, "Element_5");
            PopupStyle.PlaceTop(desc, 0f, 425f, INNER, 338f);
            PopupStyle.PlaceTopLeft(ui.DescriptionLabel.transform, -INNER / 2f, 24f, INNER, 48f);
            Label(ui.DescriptionLabel);
            Input(ui.DescriptionInput, 0f, INNER, 284f, 32f, true);
            PopupStyle.PlaceTop(ui.DescriptionInput.transform, 0f, 196f, INNER, 284f);
            ui.CharCounter = Counter(panel, "CharCounterText", 618f, "0/200");

            Row(rows, "Element_2", 700f, ROW, ui.PublicLabel);
            Check(ui.ApprovalOnlyToggle);
            Row(rows, "Element_3", 820f, 152f, ui.BannerLabel);
            BannerRow(ui.BannerPreview, ui.BannerButton);

            MainButton(ui.SaveButton, PopupStyle.ButtonColor.Green, 1001f);
            Loading(ui.LoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    private const float ANNOUNCEMENT_HEIGHT = 832f;

    public static void ApplyAnnouncement()
    {
        EditPrefab(UI + "UIEditAnnouncementPopup.prefab", root =>
        {
            UIEditAnnouncementPopup ui = root.GetComponent<UIEditAnnouncementPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", ANNOUNCEMENT_HEIGHT);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            // Ô tan 708×380 đủ 300 ký tự ≈ 8 dòng; bộ đếm nâu nhạt dưới phải (đủ → đỏ, ClanUIUtil.SetCounter).
            Input(ui.AnnouncementInput, 0f, INNER, 380f, 32f, true);
            PopupStyle.PlaceTop(ui.AnnouncementInput.transform, 0f, 360f, INNER, 380f);
            PopupStyle.PlaceTop(ui.CharCounter.transform, 0f, 572f, INNER, 40f);
            CounterStyle(ui.CharCounter);
            MainButton(ui.SaveButton, PopupStyle.ButtonColor.Green, 699f);
            Loading(ui.LoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Banner editor
    private static readonly int[] BANNER_COLUMNS = { 8, 5, 7, 8 };          // Shape 8, Background Color 10, Emblem Shape 20, Emblem Color 8
    private static readonly float[] BANNER_CELL = { 80f, 88f, 88f, 80f };
    private const float BANNER_GAP = 12f;

    public static void ApplyBannerEditor()
    {
        EditPrefab(UI + "UIClanBannerEditorPopup.prefab", root =>
        {
            UIClanBannerEditorPopup ui = root.GetComponent<UIClanBannerEditorPopup>();
            Transform rows = Find(root.transform, "VerticalLayout");
            // Chiều cao tính từ nội dung → làm trước rồi mới dựng khung.
            float cursor = 384f;
            string[] elements = { "Element", "Element_1", "Element_2", "Element_3" };
            Transform[] contents = { ui.BackgroundTypeContent, ui.BackgroundColorContent, ui.ImageTypeContent, ui.ImageColorContent };
            int[] counts = { ui.Catalog.BackgroundTypeCount, ui.Catalog.BackgroundColorCount, ui.Catalog.ImageTypeCount, ui.Catalog.ImageColorCount };
            Unlayout(rows);
            for (int i = 0; i < 4; i++)
            {
                Transform element = Find(rows, elements[i]);
                PopupStyle.Stretch(element);
                TMP_Text label = element.GetComponentInChildren<TMP_Text>(true);
                PopupStyle.PlaceTop(label.transform, 0f, cursor + 38f, INNER, 44f);
                PopupStyle.Plain(label, PopupStyle.BROWN, 36f, 24f);
                Normal(label);
                label.alignment = TextAlignmentOptions.Center;

                int columns = BANNER_COLUMNS[i];
                float cell = BANNER_CELL[i];
                int lines = (counts[i] + columns - 1) / columns;
                float gridWidth = columns * cell + (columns - 1) * BANNER_GAP;
                float gridHeight = lines * cell + (lines - 1) * BANNER_GAP;
                float top = cursor + 68f;
                OptionGrid(contents[i], columns, cell, gridWidth, gridHeight, top);
                cursor = top + gridHeight + 10f;
            }
            float applyY = cursor - 10f + 30f + BTN / 2f;
            float height = applyY + BTN / 2f + BOTTOM;

            RectTransform panel = Panel(root, "ContentTransform", "Background", height);
            PopupStyle.Stretch(rows);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            Image box = PopupStyle.NewImage(panel, "PreviewBox", PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box.transform, 0f, 280f, 220f, 208f);
            PutBehind(box.transform, ui.Preview.transform);
            PopupStyle.PlaceTop(ui.Preview.transform, 0f, 280f, 160f, 187f);
            MainButton(ui.ApplyButton, PopupStyle.ButtonColor.Green, applyY);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanBannerOptionItem.prefab", BannerOption);
    }

    // Bỏ dải cuộn ngang → lưới cố định số cột, thấy hết (user duyệt).
    private static void OptionGrid(Transform content, int columns, float cell, float width, float height, float top)
    {
        Transform viewport = content.parent;
        Transform scroll = viewport.parent;
        PopupStyle.RemoveComponent<ScrollRect>(scroll);
        Image scrollImage = scroll.GetComponent<Image>();
        if (scrollImage != null) scrollImage.enabled = false;
        PopupStyle.PlaceTop(scroll, 0f, top + height / 2f, width, height);
        PopupStyle.RemoveComponent<Mask>(viewport);
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null) viewportImage.enabled = false;
        PopupStyle.Stretch(viewport);
        PopupStyle.RemoveComponent<ContentSizeFitter>(content);
        PopupStyle.RemoveComponent<HorizontalLayoutGroup>(content);
        PopupStyle.Stretch(content);
        GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(content.gameObject);
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(BANNER_GAP, BANNER_GAP);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset();
    }

    // Ô chọn: ô tối, ô đang chọn = viền vàng (Outline của bản sao ô) + tick xanh góc dưới phải.
    private static void BannerOption(GameObject root)
    {
        ClanBannerOptionItem item = root.GetComponent<ClanBannerOptionItem>();
        // Ô tối ĐẶC (Clan/box_dark): slot_dark là đen trong 55% → trên nền kem ra xám nhạt (Linear).
        ReskinPrefabUtil.Img(root.transform, PopupStyle.Kit("Clan/box_dark"));
        Button(item.Button);
        PopupStyle.Stretch(item.ColorPreviewImage.transform, 16f, 16f, 16f, 16f);
        PopupStyle.Stretch(item.SpritePreviewImage.transform, 6f, 6f, 6f, 6f);

        // Viền vàng = lớp trắng tô vàng to hơn ô 6, đè lại ô tối bên trong (Outline nhân màu với sprite tối → ra tối).
        Transform selected = item.SelectedIndicator.transform;
        PopupStyle.RemoveComponent<Outline>(selected);
        Image ring = ReskinPrefabUtil.Img(selected, PopupStyle.Kit("Common/BasicFrame_Square01_White"));
        ring.color = PopupStyle.ACCENT;
        ring.pixelsPerUnitMultiplier = 1.5f;
        ring.raycastTarget = false;
        PopupStyle.Stretch(selected, -6f, -6f, -6f, -6f);
        selected.SetSiblingIndex(0);
        Image inner = PopupStyle.NewImage(selected, "Inner", PopupStyle.Kit("Clan/box_dark"));
        PopupStyle.Stretch(inner.transform, 6f, 6f, 6f, 6f);
        inner.transform.SetSiblingIndex(0);
        Image check = PopupStyle.NewImage(selected, "Check", PopupStyle.Kit("ClanSearch/check_green"));
        RectTransform rt = check.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchoredPosition = new Vector2(-6f, 6f);              // tâm tick = góc dưới phải ô
    }
    #endregion

    #region Requests
    private const float REQUESTS_HEIGHT = 864f;

    public static void ApplyRequests()
    {
        EditPrefab(UI + "UIClanRequestsPopup.prefab", root =>
        {
            UIClanRequestsPopup ui = root.GetComponent<UIClanRequestsPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", REQUESTS_HEIGHT);
            ui.TitleText.text = "Requests";                         // prefab ghi nhầm "Clan Settings" (lúc chạy key Clan.Requests.Title đè)
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            ScrollList(ui.ScrollRect, 488f, 636f);

            TMP_Text empty = ui.EmptyStateText;
            Image box = PopupStyle.NewImage(panel, "EmptyBox", PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box.transform, 0f, 488f, 500f, 160f);
            Reparent(empty.transform, box.transform);
            PopupStyle.Stretch(empty.transform, 20f, 10f, 20f, 10f);
            PopupStyle.Plain(empty, PopupStyle.BROWN, 36f, 24f);
            Normal(empty);
            empty.alignment = TextAlignmentOptions.Center;
            empty.gameObject.SetActive(true);
            ui.EmptyState = box.gameObject;
            box.gameObject.SetActive(false);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanRequestCard.prefab", RequestCard);
    }

    // Thẻ xám 112: avatar ô tối, tên + power, Deny đỏ / Accept xanh lá (đang gửi → xám).
    private static void RequestCard(GameObject root)
    {
        ClanRequestCard card = root.GetComponent<ClanRequestCard>();
        CardHeight(root, PopupStyle.Kit("Clan/card_gray"), 112f);
        AvatarSlot(card.AvatarImage, 62f, 86f, 72f);
        LeftText(card.NameText, 120f, 18f, 254f, 40f, 34f);
        LeftText(card.PowerText, 120f, -20f, 254f, 36f, 30f);
        CardButton(card.AcceptButton, PopupStyle.ButtonColor.Green, -91f, 150f);
        CardButton(card.DenyButton, PopupStyle.ButtonColor.Red, -244f, 132f);
    }
    #endregion

    #region Clan info
    private const float INFO_HEIGHT = 1688f;

    public static void ApplyInfo()
    {
        EditPrefab(UI + "UIClanInfoPopup.prefab", root =>
        {
            UIClanInfoPopup ui = root.GetComponent<UIClanInfoPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", INFO_HEIGHT);
            Close(ui.CloseButton);

            // Tên clan lên header (user duyệt), ô tan còn banner + mô tả + 4 chỉ số 2 cột.
            Reparent(ui.NameText.transform, panel);
            PopupStyle.RemoveComponent<LayoutElement>(ui.NameText);
            PopupStyle.Title(ui.NameText, W);
            Normal(ui.NameText);

            Transform content = Unlayout(Find(panel, "ContentRoot"));
            Transform box = Find(content, "ClanInfoBackground");
            PopupStyle.RemoveComponent<VerticalLayoutGroup>(box);
            PopupStyle.RemoveComponent<LayoutElement>(box);
            ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box, 0f, 340f, INNER, 340f);

            TopLeft(ui.Banner.transform, 20f, 16f, 172f, 200f);
            TopLeft(ui.DescriptionText.transform, 212f, 16f, INNER - 232f, 200f);
            PopupStyle.Plain(ui.DescriptionText, PopupStyle.BROWN, 28f, 20f);
            Normal(ui.DescriptionText);
            ui.DescriptionText.alignment = TextAlignmentOptions.TopLeft;

            Transform stats = Find(box, "VerticalLayout");
            PopupStyle.RemoveComponent<VerticalLayoutGroup>(stats);
            PopupStyle.RemoveComponent<LayoutElement>(stats);
            TopLeft(stats, 20f, 232f, INNER - 40f, 92f);
            GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(stats.gameObject);
            grid.cellSize = new Vector2(316f, 44f);
            grid.spacing = new Vector2(36f, 4f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            StatPair(ui.PowerLabel, ui.TotalPowerText);
            StatPair(ui.TierLabel, ui.TierText);
            StatPair(ui.TypeLabel, ui.JoinSettingText);
            StatPair(ui.MembersLabel, ui.MemberCountText);

            // Danh sách thành viên (thẻ xám giữ nguyên) từ dưới ô tan tới trên nút.
            ScrollList(ui.ScrollRect, 991f, 930f);
            PopupStyle.RemoveComponent<LayoutElement>(ui.ScrollRect);

            Transform join = ui.JoinRequestSection.transform;
            PopupStyle.PlaceTop(join, 0f, INFO_HEIGHT - BOTTOM - BTN / 2f, INNER, BTN);
            JoinButton(ui.JoinButton);
            JoinButton(ui.RequestButton);
            Transform requested = PopupStyle.NewImage(join, "RequestedButton", null).transform;
            Reparent(ui.RequestedText.transform, requested);
            bool requestedActive = ui.RequestedState.activeSelf;
            PopupStyle.Button(requested, PopupStyle.ButtonColor.Gray, 320f);
            PopupStyle.Center(requested, 0f, 0f, 320f, BTN);
            ui.RequestedText.gameObject.SetActive(true);
            ui.RequestedState = requested.gameObject;
            requested.gameObject.SetActive(requestedActive);

            Loading(ui.ActionLoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    private static void JoinButton(Button button)
    {
        PopupStyle.Button(button.transform, PopupStyle.ButtonColor.Green, 320f);
        PopupStyle.Center(button.transform, 0f, 0f, 320f, BTN);
        Swap(button);
    }

    private static void StatPair(TMP_Text label, TMP_Text value)
    {
        RectTransform l = label.rectTransform;
        l.anchorMin = l.anchorMax = l.pivot = new Vector2(0f, 0.5f);
        l.anchoredPosition = Vector2.zero;
        l.sizeDelta = new Vector2(170f, 44f);
        PopupStyle.Plain(label, PopupStyle.BROWN, 32f, 22f);
        Normal(label);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        RectTransform v = value.rectTransform;
        v.anchorMin = v.anchorMax = v.pivot = new Vector2(1f, 0.5f);
        v.anchoredPosition = Vector2.zero;
        v.sizeDelta = new Vector2(150f, 44f);
        Color keep = value.color;
        PopupStyle.Outline(value, 32f, 22f);
        Normal(value);
        if (value.name == "PublicText") value.color = keep;          // màu Open/Approval do code tô theo loại
        value.alignment = TextAlignmentOptions.MidlineRight;
        value.enableWordWrapping = false;
    }
    #endregion

    #region Clan player
    private const float PLAYER_HEIGHT = 556f;
    private const float PLAYER_COMPACT = 382f;                               // không nút → thấp lại (Member xem người khác)
    private const float AVATAR_X = -234f;

    public static void ApplyPlayer()
    {
        EditPrefab(UI + "UIClanPlayerPopup.prefab", root =>
        {
            UIClanPlayerPopup ui = root.GetComponent<UIClanPlayerPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentAll/ClanPlayerPopup/Content");
            PopupStyle.Shell(panel, W, PLAYER_HEIGHT, panel, panel.Find("Header"));
            ui.PanelRect = panel;
            ui.FullHeight = PLAYER_HEIGHT;
            ui.CompactHeight = PLAYER_COMPACT;
            Close(ui.CloseButton);

            Transform loaded = Find(panel, "Loaded");
            PopupStyle.Stretch(loaded);
            Transform column = Unlayout(Find(loaded, "VerticalLayout"));
            PopupStyle.RemoveComponent<ContentSizeFitter>(column);
            PopupStyle.Stretch(column);

            // Tên người chơi lên header (user duyệt).
            Reparent(ui.NameText.transform, panel);
            PopupStyle.Title(ui.NameText, W);
            Normal(ui.NameText);

            Transform avatar = ui.AvatarButton.transform;
            ReskinPrefabUtil.Img(avatar, PopupStyle.Kit("Clan/box_dark"));
            PopupStyle.PlaceTop(avatar, AVATAR_X, 250f, 148f, 148f);
            PopupStyle.Stretch(ui.AvatarImage.transform, 14f, 14f, 14f, 14f);
            Button(ui.AvatarButton);

            // Hàng chức vụ: icon Leader/Captain (ẩn khi Member) + chữ, tự xếp ngang.
            const float textLeft = AVATAR_X + 74f + 28f;
            Transform roleRow = NewNode(column, "RoleRow");
            PopupStyle.PlaceTopLeft(roleRow, textLeft, 214f, 420f, 64f);
            HorizontalLayoutGroup h = GetOrAdd<HorizontalLayoutGroup>(roleRow.gameObject);
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            Image icon = PopupStyle.NewImage(roleRow, "RoleIcon", PopupStyle.Art("6_Features/Clan/clan_captain_icon"));
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(56f, 56f);
            LayoutElement iconLayout = GetOrAdd<LayoutElement>(icon.gameObject);
            iconLayout.minWidth = iconLayout.preferredWidth = 56f;
            icon.transform.SetSiblingIndex(0);
            ui.RoleIcon = icon;
            ui.LeaderIconSprite = PopupStyle.Art("6_Features/Clan/clan_leader_icon");
            ui.CaptainIconSprite = PopupStyle.Art("6_Features/Clan/clan_captain_icon");
            Reparent(ui.RoleText.transform, roleRow);
            ui.RoleText.transform.SetSiblingIndex(1);
            ui.RoleText.rectTransform.sizeDelta = new Vector2(300f, 64f);
            PopupStyle.Outline(ui.RoleText, 44f);
            Normal(ui.RoleText);
            ui.RoleText.alignment = TextAlignmentOptions.MidlineLeft;
            ui.RoleText.enableWordWrapping = false;

            PopupStyle.PlaceTopLeft(ui.PowerText.transform, textLeft, 290f, 420f, 64f);
            PopupStyle.Outline(ui.PowerText, 44f, 30f);
            Normal(ui.PowerText);
            ui.PowerText.alignment = TextAlignmentOptions.MidlineLeft;

            // Hàng nút tự căn giữa theo số nút đang bật: Leave / Kick đỏ, Promote cam ↔ Demote tím.
            Transform buttons = NewNode(loaded, "Buttons");
            PopupStyle.PlaceTop(buttons, 0f, 423f, INNER, BTN);
            HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(buttons.gameObject);
            row.spacing = 32f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            RowButton(ui.LeaveButton, buttons, PopupStyle.ButtonColor.Red, 0);
            RowButton(ui.KickButton, buttons, PopupStyle.ButtonColor.Red, 1);
            RowButton(ui.PromoteDemoteButton, buttons, PopupStyle.ButtonColor.Orange, 2);
            ui.PromoteSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.DemoteSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);

            Loading(ui.LoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    private static void RowButton(Button button, Transform parent, PopupStyle.ButtonColor color, int index)
    {
        Reparent(button.transform, parent);
        button.transform.SetSiblingIndex(index);
        PopupStyle.Button(button.transform, color, 300f);
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, BTN);
        Button(button);
    }
    #endregion

    #region Clan search
    private const float SEARCH_HEIGHT = 864f;

    public static void ApplySearch()
    {
        EditPrefab(UI + "UIAdvancedSearchPopup.prefab", root =>
        {
            UIAdvancedSearchPopup ui = root.GetComponent<UIAdvancedSearchPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", SEARCH_HEIGHT);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);
            Transform rows = Unlayout(Find(panel, "VerticalLayout"));

            Row(rows, "Element", 214f, ROW, ui.NameLabel);
            Input(ui.NameInput, CONTROL_LEFT + CONTROL_WIDTH / 2f, CONTROL_WIDTH, 106f, 38f, false);
            Row(rows, "Element_2", 330f, ROW, ui.HideApprovalLabel);
            Check(ui.HideApprovalToggle);
            Row(rows, "Element_4", 446f, ROW, ui.MinMemberLabel);
            Counter(ui.MinDecrementButton, ui.MinMemberText, ui.MinIncrementButton);
            Row(rows, "Element_5", 562f, ROW, ui.MaxMemberLabel);
            Counter(ui.MaxDecrementButton, ui.MaxMemberText, ui.MaxIncrementButton);

            // Gốc có Clear (code đã có, prefab thiếu) → nhân bản nút Search (user duyệt).
            const float buttonY = 731f;
            PopupStyle.Button(ui.SearchButton.transform, PopupStyle.ButtonColor.Orange, 300f);
            PopupStyle.PlaceTop(ui.SearchButton.transform, 166f, buttonY, 300f, BTN);
            Button(ui.SearchButton);
            if (ui.ClearButton == null)
            {
                Transform existing = panel.Find("ClearButton");
                GameObject clear = existing != null ? existing.gameObject : Object.Instantiate(ui.SearchButton.gameObject, panel);
                clear.name = "ClearButton";
                ui.ClearButton = clear.GetComponent<Button>();
            }
            PopupStyle.Button(ui.ClearButton.transform, PopupStyle.ButtonColor.Violet, 300f);
            PopupStyle.PlaceTop(ui.ClearButton.transform, -166f, buttonY, 300f, BTN);
            PopupStyle.SetKey(ui.ClearButton.GetComponentInChildren<TMP_Text>(true), "Popup.ClanSearch.Clear", "Clear");
            Button(ui.ClearButton);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }

    // Nút −/+ art giữ nguyên; số vào ô tan giữa.
    private static void Counter(Button minus, TMP_Text count, Button plus)
    {
        Transform row = minus.transform.parent;
        PopupStyle.Center(minus.transform, CONTROL_LEFT + 49f, 0f, 98f, 107f);
        Image box = PopupStyle.NewImage(row, count.name + "Box", PopupStyle.Kit("ClanSearch/input_tan"));
        PopupStyle.Center(box.transform, CONTROL_LEFT + 98f + 16f + 58f, 0f, 116f, 92f);
        Reparent(count.transform, box.transform);
        PopupStyle.Stretch(count.transform, 4f, 4f, 4f, 4f);
        PopupStyle.Outline(count, 42f, 28f);
        Normal(count);
        count.alignment = TextAlignmentOptions.Center;
        PopupStyle.Center(plus.transform, CONTROL_LEFT + 98f + 16f + 116f + 16f + 49f, 0f, 98f, 107f);
        Button(minus);
        Button(plus);
    }
    #endregion

    #region Leadership rank
    private const float LEADERSHIP_HEIGHT = 1372f;

    public static void ApplyLeadership()
    {
        EditPrefab(UI + "UILeadershipRankPopup.prefab", root =>
        {
            UILeadershipRankPopup ui = root.GetComponent<UILeadershipRankPopup>();
            RectTransform panel = Panel(root, "ContentTransform", "Background", LEADERSHIP_HEIGHT);
            Transform content = Find(panel, "Content");
            PopupStyle.Stretch(content);
            PopupStyle.Title(ui.TitleText, W);
            Close(ui.CloseButton);

            PopupStyle.PlaceTop(ui.DescriptionText.transform, 0f, 262f, INNER, 190f);
            PopupStyle.Plain(ui.DescriptionText, PopupStyle.BROWN, 30f, 22f);
            Normal(ui.DescriptionText);
            ui.DescriptionText.alignment = TextAlignmentOptions.Center;

            ScrollList(ui.ListContent.parent.parent.GetComponent<ScrollRect>(), 801f, 882f);

            TMP_Text empty = ui.NoRankingsText;
            Image box = PopupStyle.NewImage(content, "NoRankBox", PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box.transform, 0f, 801f, 600f, 220f);
            Reparent(empty.transform, box.transform);
            PopupStyle.Stretch(empty.transform, 24f, 10f, 24f, 10f);
            PopupStyle.Plain(empty, PopupStyle.BROWN, 36f, 24f);
            Normal(empty);
            empty.alignment = TextAlignmentOptions.Center;
            empty.gameObject.SetActive(true);
            ui.NoRankingsRoot = box.gameObject;

            PopupStyle.PlaceTop(ui.UpdateInfoText.transform, 0f, LEADERSHIP_HEIGHT - BOTTOM - 24f, INNER, 50f);
            PopupStyle.Plain(ui.UpdateInfoText, PopupStyle.MUTE, 34f, 24f);
            Normal(ui.UpdateInfoText);
            ui.UpdateInfoText.alignment = TextAlignmentOptions.Center;

            Loading(ui.LoadingView, true);
            ui.CloseButton.transform.SetAsLastSibling();
        });
        EditPrefab(CARDS + "ClanWarClanRowCard.prefab", ClanRow);
    }

    // Thẻ xám 160, clan của mình = thẻ xanh (đổi sprite). Dùng chung với Clan War Leaderboard (Đợt 8).
    internal static void ClanRow(GameObject root)
    {
        ClanWarClanRowCard card = root.GetComponent<ClanWarClanRowCard>();
        CardHeight(root, PopupStyle.Kit("Clan/card_gray"), 160f);
        card.NormalSprite = PopupStyle.Kit("Clan/card_gray");
        card.HighlightSprite = PopupStyle.Kit("Clan/card_blue");
        card.NormalColor = card.HighlightColor = Color.white;

        Anchored(card.RankText.transform, 0f, 62f, 0f, 96f, 70f);
        PopupStyle.Outline(card.RankText, 48f, 28f);
        Normal(card.RankText);
        card.RankText.alignment = TextAlignmentOptions.Center;
        card.RankText.enableWordWrapping = false;
        Anchored(card.Banner.transform, 0f, 158f, 0f, 104f, 124f);
        LeftText(card.NameText, 222f, 24f, 300f, 50f, 38f);
        LeftText(card.TierText, 222f, -26f, 150f, 36f, 28f);
        LeftText(card.TotalPowerText, 380f, -26f, 160f, 36f, 28f);
        card.TotalPowerText.color = PopupStyle.ACCENT;
        Anchored(card.ServerText.transform, 1f, -95f, -40f, 150f, 40f);
        PopupStyle.Outline(card.ServerText, 30f, 20f);
        Normal(card.ServerText);
        card.ServerText.alignment = TextAlignmentOptions.MidlineRight;
    }
    #endregion

    #region Helper
    // Popup chuẩn: container W×height căn giữa màn, khung (frame) phủ kín container.
    internal static RectTransform Panel(GameObject root, string containerName, string frameName, float height)
    {
        Transform container = Find(root.transform, containerName);
        RectTransform frame = (RectTransform)Find(container, frameName);
        PopupStyle.Shell(frame, W, height, frame, frame.Find("Header"));
        PopupStyle.Center(container, 0f, 0f, W, height);
        PopupStyle.Stretch(frame);
        return (RectTransform)container;
    }

    // Bỏ layout tự xếp (đặt tay theo concept), phủ kín cha để toạ độ con = toạ độ panel.
    private static Transform Unlayout(Transform node)
    {
        PopupStyle.RemoveComponent<VerticalLayoutGroup>(node);
        PopupStyle.Stretch(node);
        return node;
    }

    // Hàng "nhãn nâu | điều khiển" rộng INNER, tâm cách mép trên panel topToCenter.
    private static Transform Row(Transform rows, string name, float topToCenter, float height, TMP_Text label)
    {
        Transform row = Find(rows, name);
        PopupStyle.RemoveComponent<LayoutElement>(row);
        PopupStyle.PlaceTop(row, 0f, topToCenter, INNER, height);
        PopupStyle.Center(label.transform, -INNER / 2f + LABEL_WIDTH / 2f, 0f, LABEL_WIDTH, height);
        Label(label);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return row;
    }

    private static void Label(TMP_Text label)
    {
        PopupStyle.Plain(label, PopupStyle.BROWN, 38f, 24f);
        Normal(label);
        label.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // Ô nhập tan, chữ nhập nâu đậm, gợi ý nâu nhạt. multiline → canh trên trái.
    private static void Input(TMP_InputField input, float x, float width, float height, float size, bool multiline)
    {
        ReskinPrefabUtil.Img(input.transform, PopupStyle.Kit("ClanSearch/input_tan"));
        PopupStyle.Center(input.transform, x, 0f, width, height);
        input.transition = Selectable.Transition.None;
        Transform area = input.textViewport;
        PopupStyle.Stretch(area, 20f, 12f, 20f, 12f);
        TextAlignmentOptions align = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        TMP_Text text = input.textComponent;
        PopupStyle.Stretch(text.transform);
        PopupStyle.Plain(text, INPUT_TEXT, size);
        Normal(text);
        text.alignment = align;
        TMP_Text hint = input.placeholder as TMP_Text;
        if (hint != null)
        {
            PopupStyle.Stretch(hint.transform);
            PopupStyle.Plain(hint, INPUT_HINT, size);
            Normal(hint);
            hint.alignment = align;
        }
        input.pointSize = size;
    }

    private static TMP_Text Counter(Transform panel, string name, float topToCenter, string sample)
    {
        TMP_Text counter = PopupStyle.GetOrCreateText(panel, name);
        PopupStyle.PlaceTop(counter.transform, 0f, topToCenter, INNER, 40f);
        counter.text = sample;
        CounterStyle(counter);
        return counter;
    }

    private static void CounterStyle(TMP_Text counter)
    {
        PopupStyle.Plain(counter, PopupStyle.MUTE, 30f);
        Normal(counter);
        counter.alignment = TextAlignmentOptions.MidlineRight;
        PopupStyle.RemoveKey(counter);
    }

    // Ô tick tan + tick xanh (kit ClanSearch).
    private static void Check(CheckButton check)
    {
        Transform t = check.transform;
        ReskinPrefabUtil.Img(t, PopupStyle.Kit("Mastery/box_tan"));
        PopupStyle.Center(t, CONTROL_LEFT + 43f, 0f, 86f, 86f);
        Image mark = check.CheckImage;
        ReskinPrefabUtil.Img(mark.transform, PopupStyle.Kit("ClanSearch/check_green"));
        mark.preserveAspect = true;
        PopupStyle.Center(mark.transform, 0f, 6f, 88f, 88f);
        Button(t.GetComponent<Button>());
    }

    // Banner xem trước + Browse tím (khoá → xám).
    private static void BannerRow(ClanBannerView banner, Button browse)
    {
        PopupStyle.Center(banner.transform, CONTROL_LEFT + 60f, 0f, 120f, 140f);
        PopupStyle.Button(browse.transform, PopupStyle.ButtonColor.Violet, 236f, 112f, 40f);
        PopupStyle.Center(browse.transform, INNER / 2f - 118f, 0f, 236f, 112f);
        Swap(browse);
    }

    // Danh sách cuộn: mask chữ nhật (không sprite UIMask bo mờ), lề trong 8.
    internal static void ScrollList(ScrollRect scroll, float topToCenter, float height)
    {
        PopupStyle.PlaceTop(scroll.transform, 0f, topToCenter, INNER, height);
        Image bg = scroll.GetComponent<Image>();
        if (bg != null) bg.enabled = false;
        RectTransform viewport = scroll.viewport;
        PopupStyle.Stretch(viewport);
        PopupStyle.RemoveComponent<Mask>(viewport);
        GetOrAdd<RectMask2D>(viewport.gameObject);
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null) PopupStyle.Invisible(viewportImage);
        VerticalLayoutGroup layout = scroll.content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        foreach (ClanLoadingView loading in scroll.GetComponentsInChildren<ClanLoadingView>(true))
            Loading(loading, false);
    }

    // Lớp tải/lỗi: icon xoay tông nâu, chữ lỗi đỏ, Retry cam. belowHeader → phủ phần thân popup.
    internal static void Loading(ClanLoadingView view, bool belowHeader)
    {
        if (view == null) return;
        if (belowHeader) PopupStyle.Stretch(view.transform, 14f, 14f, 14f, PopupStyle.HEADER_HEIGHT);
        else PopupStyle.Stretch(view.transform);
        if (view.LoadingImage != null)
        {
            PopupStyle.Center(view.LoadingImage.transform, 0f, 0f, 76f, 76f);
            view.LoadingImage.color = new Color32(122, 92, 60, 220);
        }
        if (view.ErrorText != null)
        {
            PopupStyle.Center(view.ErrorText.transform, 0f, 89f, 640f, 100f);
            PopupStyle.Outline(view.ErrorText, 44f, 28f);
            Normal(view.ErrorText);
            view.ErrorText.color = PopupStyle.DOWN;
            view.ErrorText.alignment = TextAlignmentOptions.Center;
        }
        if (view.RetryButton != null)
        {
            PopupStyle.Button(view.RetryButton.transform, PopupStyle.ButtonColor.Orange, 320f);
            PopupStyle.Center(view.RetryButton.transform, 0f, -98f, 320f, BTN);
            Button(view.RetryButton);
        }
    }

    private static void MainButton(Button button, PopupStyle.ButtonColor color, float topToCenter)
    {
        PopupStyle.Button(button.transform, color, 300f);
        PopupStyle.PlaceTop(button.transform, 0f, topToCenter, 300f, BTN);
        Button(button);
    }

    // Nút trong thẻ (neo phải), khoá → xám.
    private static void CardButton(Button button, PopupStyle.ButtonColor color, float x, float width)
    {
        PopupStyle.Button(button.transform, color, width, 84f, 30f);
        Anchored(button.transform, 1f, x, 2f, width, 84f);
        Swap(button);
    }

    internal static void CardHeight(GameObject root, Sprite sprite, float height)
    {
        ReskinPrefabUtil.Img(root.transform, sprite);
        RectTransform rt = (RectTransform)root.transform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout != null) layout.minHeight = layout.preferredHeight = height;
    }

    // Avatar trong ô tối (ô mới chèn phía sau ảnh avatar).
    internal static void AvatarSlot(Image avatar, float x, float slot, float size)
    {
        Image frame = PopupStyle.NewImage(avatar.transform.parent, "AvatarSlot", PopupStyle.Kit("PvP/slot_dark"));
        PutBehind(frame.transform, avatar.transform);
        Anchored(frame.transform, 0f, x, 0f, slot, slot);
        Anchored(avatar.transform, 0f, x, 0f, size, size);
    }

    // Chữ trắng viền canh trái, mép trái cách mép trái thẻ `left`.
    internal static void LeftText(TMP_Text text, float left, float y, float width, float height, float size)
    {
        Anchored(text.transform, 0f, left + width / 2f, y, width, height);
        PopupStyle.Outline(text, size, size * 0.65f);
        Normal(text);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
    }

    // Neo theo cạnh trái (ax=0) / phải (ax=1), giữa theo chiều dọc.
    internal static void Anchored(Transform t, float ax, float x, float y, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(ax, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = Vector3.one;
    }

    // Neo góc trên trái của cha.
    private static void TopLeft(Transform t, float left, float top, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(left, -top);
        rt.localScale = Vector3.one;
    }

    // Đặt `back` ngay sau lưng `front` (cùng cha) — chạy lại không đảo thứ tự.
    internal static void PutBehind(Transform back, Transform front)
    {
        int index = front.GetSiblingIndex();
        if (back.GetSiblingIndex() > index) back.SetSiblingIndex(index);
    }

    internal static Transform NewNode(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    // Chữ cũ hay để Bold (bẫy Đợt 3) → về Normal.
    internal static void Normal(TMP_Text text)
    {
        text.fontStyle = FontStyles.Normal;
    }

    internal static void Close(Button button)
    {
        if (button == null) return;
        bool active = button.gameObject.activeSelf;
        PopupStyle.CloseButton(button.transform);
        button.gameObject.SetActive(active);
        Button(button);
    }

    // Kit: không tô màu khi bấm/tắt (PressButtonUI lo hiệu ứng nhấn).
    internal static void Button(Button button)
    {
        if (button != null) button.transition = Selectable.Transition.None;
    }

    // Nút có trạng thái khoá: đổi sang sprite xám khi interactable = false.
    internal static void Swap(Button button)
    {
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { disabledSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray) };
    }

    internal static void Reparent(Transform t, Transform parent)
    {
        if (t.parent != parent) t.SetParent(parent, false);
    }

    internal static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    internal static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        throw new System.Exception("[Batch7Reskin] thiếu node " + root.name + "/" + path);
    }
    #endregion
}
