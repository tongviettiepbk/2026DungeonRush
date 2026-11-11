using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Reskin UISevenDayPopup theo concept tools/concept/seven_day/concept.png (concept.html cùng thư mục).
// Sửa thẳng prefab có sẵn (giữ GUID + phần căn giữa/"Tap anywhere to close"), chạy lại nhiều lần vẫn ra cùng kết quả.
// Toạ độ = đơn vị canvas 1080×2160, gốc ở tâm ContentTransform; số đo lấy từ concept HTML × 2.
public static class SevenDayPopupReskin
{
    public const string PREFAB = "Assets/_Assets/Resources/Prefabs/UI/UISevenDayPopup.prefab";
    private const string DAY7_BG = "Assets/_Assets/Art/1_UIKit/SevenDay/day7_bg.png";
    private const string IMPORT_TEMPLATE = "Assets/_Assets/Art/1_UIKit/Mastery/popup_frame.png";

    private const float WIDTH = 968f;
    private const float HEIGHT = 1626f;
    private const float HEADER_HEIGHT = 138f;
    private const float FRAME_UNDER_HEADER = 12f;
    private const float CELL_WIDTH = 276f;
    private const float CELL_HEIGHT = 300f;
    private const float CELL_STEP = 300f;         // rộng ô + khoảng cách 24
    private const float ROW1_Y = 411f;
    private const float ROW2_Y = 87f;
    private const float GRAND_WIDTH = 876f;
    private const float GRAND_HEIGHT = 336f;
    private const float GRAND_Y = -259f;
    private const float SUBTITLE_Y = 613f;
    private const float FOOTER_Y = -613f;

    private static readonly Color SUBTITLE_COLOR = new Color32(122, 92, 60, 255);

    [MenuItem("Tools/DungeonRush/Reskin/Seven Day Popup")]
    public static string Apply()
    {
        ImportDay7Background();
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        try
        {
            UISevenDayPopup popup = root.GetComponent<UISevenDayPopup>();
            RectTransform panel = (RectTransform)FindDeep(root.transform, "ContentTransform");
            panel.sizeDelta = new Vector2(WIDTH, HEIGHT);
            TMP_Text template = popup.txtTitle;

            BuildFrame(panel, popup);
            BuildSubtitle(panel, popup, template);
            Transform content = BuildContent(panel, popup);
            for (int i = 0; i < popup.cells.Count - 1; i++)
            {
                float x = (i % 3 - 1) * CELL_STEP;
                BuildDayCell(popup.cells[i], content, x, i < 3 ? ROW1_Y : ROW2_Y);
            }
            BuildGrandCell(popup.cells[popup.cells.Count - 1], content, popup, template);
            BuildFooter(panel, popup, template);
            SetRaycasts(panel, popup);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[SevenDayPopupReskin] done");
        return "ok";
    }

    // Ảnh nền ngày 7 (tools/reskin/gen_day7_bg.py) import giống sprite UI có sẵn, không 9-slice.
    private static void ImportDay7Background()
    {
        AssetDatabase.ImportAsset(DAY7_BG);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(DAY7_BG);
        if (importer.textureType == TextureImporterType.Sprite) return;

        TextureImporter template = (TextureImporter)AssetImporter.GetAtPath(IMPORT_TEMPLATE);
        TextureImporterSettings settings = new TextureImporterSettings();
        template.ReadTextureSettings(settings);
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.textureCompression = template.textureCompression;
        importer.SaveAndReimport();
    }

    #region Khung
    private static void BuildFrame(RectTransform panel, UISevenDayPopup popup)
    {
        RectTransform background = (RectTransform)panel.Find("Background");
        ReskinPrefabUtil.Img(background, ReskinPrefabUtil.UIKit("Mastery/popup_frame"));
        Stretch(background);
        background.offsetMax = new Vector2(0f, -(HEADER_HEIGHT - FRAME_UNDER_HEADER));
        background.SetSiblingIndex(0);

        // Ruy băng cũ → thanh tiêu đề tím (kit Mastery).
        RectTransform header = (RectTransform)(panel.Find("Header") ?? panel.Find("Ribbon"));
        header.name = "Header";
        ReskinPrefabUtil.Img(header, ReskinPrefabUtil.UIKit("Mastery/popup_header"));
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = Vector2.one;
        header.pivot = new Vector2(0.5f, 1f);
        header.sizeDelta = new Vector2(0f, HEADER_HEIGHT);
        header.anchoredPosition = Vector2.zero;
        header.SetSiblingIndex(1);

        TMP_Text title = popup.txtTitle;
        Box(title.rectTransform, 0f, 0f, 760f, 100f);
        title.enableAutoSizing = false;
        title.fontSize = 68f;
        title.alignment = TextAlignmentOptions.Center;

        // Nút X đỏ (ảnh có sẵn dấu X) nằm gọn trong thanh tiêu đề, sát mép phải, cách đều trên/dưới/phải.
        RectTransform close = (RectTransform)(panel.Find("CloseButton") ?? panel.Find("ChestPopupOfferCloseButton"));
        close.name = "CloseButton";
        close.gameObject.SetActive(true);
        ReskinPrefabUtil.Img(close, ReskinPrefabUtil.UIKit("Mastery/btn_close"));
        close.anchorMin = close.anchorMax = Vector2.one;
        close.pivot = new Vector2(0.5f, 0.5f);
        close.sizeDelta = new Vector2(104f, 104f);
        close.anchoredPosition = new Vector2(-HEADER_HEIGHT / 2f, -HEADER_HEIGHT / 2f);
        Transform icon = close.Find("CloseIcon");
        if (icon != null) icon.gameObject.SetActive(false);
        close.SetAsLastSibling();
        popup.btClose = close.GetComponent<Button>();
    }

    private static void BuildSubtitle(RectTransform panel, UISevenDayPopup popup, TMP_Text template)
    {
        TMP_Text subtitle = GetOrCreateText(panel, "Subtitle", template);
        Box(subtitle.rectTransform, 0f, SUBTITLE_Y, GRAND_WIDTH, 56f);
        SetPlain(subtitle, SUBTITLE_COLOR, 38f, 26f);
        popup.txtSubtitle = subtitle;
    }
    #endregion

    #region Ô ngày
    // Content cũ (box_tan + layout 2 hàng) → khung trống phủ cả popup; ô đặt toạ độ tay.
    private static Transform BuildContent(RectTransform panel, UISevenDayPopup popup)
    {
        RectTransform content = (RectTransform)panel.Find("Content");
        RemoveComponent<VerticalLayoutGroup>(content);
        RemoveComponent<ContentSizeFitter>(content);
        RemoveComponent<Image>(content);
        Stretch(content);

        foreach (SevenDayCellUI cell in popup.cells) cell.transform.SetParent(content, false);
        foreach (string row in new[] { "Row1", "Row2" })
        {
            Transform t = content.Find(row);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        return content;
    }

    private static void BuildDayCell(SevenDayCellUI cell, Transform content, float x, float y)
    {
        RectTransform rt = (RectTransform)cell.transform;
        Box(rt, x, y, CELL_WIDTH, CELL_HEIGHT);
        cell.imgBackground = ReskinPrefabUtil.Img(rt, ReskinPrefabUtil.UIKit("BattlePass/cell_gray"));
        cell.sprWaiting = cell.imgBackground.sprite;
        cell.sprClaimable = ReskinPrefabUtil.UIKit("BattlePass/cell_green");
        cell.sprClaimed = ReskinPrefabUtil.UIKit("BattlePass/cell_blue");
        cell.isFixedBackground = false;

        Box(cell.txtDay.rectTransform, 0f, 108f, 260f, 56f);
        cell.txtDay.fontSize = 42f;
        Box(cell.imgIcon.rectTransform, 0f, 4f, 124f, 124f);
        cell.imgIcon.preserveAspect = true;
        Box(cell.txtAmount.rectTransform, 0f, -106f, 260f, 64f);
        cell.txtAmount.fontSize = 52f;

        BuildClaimed(cell, 0f, 4f);
        BuildTodayBadge(cell);
    }

    private static void BuildGrandCell(SevenDayCellUI cell, Transform content, UISevenDayPopup popup, TMP_Text template)
    {
        RectTransform rt = (RectTransform)cell.transform;
        Box(rt, 0f, GRAND_Y, GRAND_WIDTH, GRAND_HEIGHT);
        cell.imgBackground = ReskinPrefabUtil.Img(rt, AssetDatabase.LoadAssetAtPath<Sprite>(DAY7_BG));
        cell.isFixedBackground = true;

        // "Day 7" trên ruy băng vàng lấn lên mép trên thẻ.
        RectTransform ribbon = GetOrCreate(rt, "DayRibbon");
        ReskinPrefabUtil.Img(ribbon, ReskinPrefabUtil.UIKit("Gameplay/ribbon_offer"));
        Box(ribbon, 0f, 154f, 460f, 108f);
        cell.txtDay.transform.SetParent(ribbon, false);
        Box(cell.txtDay.rectTransform, 0f, 10f, 400f, 70f);
        cell.txtDay.fontSize = 46f;

        // Nhãn đỏ "BIG REWARD" nghiêng, lấn ra mép trái.
        RectTransform tag = GetOrCreate(rt, "BigRewardTag");
        ReskinPrefabUtil.Img(tag, ReskinPrefabUtil.UIKit("Events/ribbon_red"));
        Box(tag, -316f, 16f, 290f, 80f);
        tag.localEulerAngles = new Vector3(0f, 0f, 8f);
        TMP_Text tagText = GetOrCreateText(tag, "Text", template);
        Box(tagText.rectTransform, -14f, 2f, 236f, 60f);
        tagText.enableAutoSizing = true;
        tagText.fontSizeMin = 20f;
        tagText.fontSizeMax = 32f;
        tagText.text = "BIG REWARD";
        popup.txtBigReward = tagText;

        // 2 ô quà khung cam + dấu cộng ở giữa.
        BuildSlot(rt, "Slot1", -32f, cell.imgIcon, cell.txtAmount);
        BuildSlot(rt, "Slot2", 256f, cell.imgIcon2, cell.txtAmount2);
        TMP_Text plus = GetOrCreateText(rt, "Plus", template);
        Box(plus.rectTransform, 112f, -14f, 60f, 80f);
        plus.enableAutoSizing = false;
        plus.fontSize = 68f;
        plus.text = "+";

        ribbon.SetAsLastSibling();
        BuildClaimed(cell, 112f, -14f);
        BuildTodayBadge(cell);
    }

    private static void BuildSlot(RectTransform card, string name, float x, Image icon, TMP_Text amount)
    {
        RectTransform slot = GetOrCreate(card, name);
        ReskinPrefabUtil.Img(slot, ReskinPrefabUtil.UIKit("BattlePass/btn_orange"));
        Box(slot, x, -14f, 200f, 200f);
        icon.transform.SetParent(slot, false);
        Box(icon.rectTransform, 0f, 18f, 112f, 112f);
        icon.preserveAspect = true;
        amount.transform.SetParent(slot, false);
        Box(amount.rectTransform, 0f, -56f, 190f, 56f);
        amount.fontSize = 42f;
    }

    // Đã nhận: bỏ lớp phủ tối cũ, chỉ còn dấu tick xanh (kit Battle Pass) đè lên icon đã làm mờ.
    private static void BuildClaimed(SevenDayCellUI cell, float x, float y)
    {
        RectTransform claimed = (RectTransform)cell.objClaimed.transform;
        RemoveComponent<Image>(claimed);
        Stretch(claimed);
        RectTransform check = (RectTransform)claimed.Find("Check");
        Image image = ReskinPrefabUtil.Img(check, ReskinPrefabUtil.UIKit("BattlePass/icon_check"));
        image.preserveAspect = true;
        Box(check, x, y, 132f, 106f);
        claimed.SetAsLastSibling();
    }

    // Nhận được hôm nay: chấm "!" đỏ ở góc trên phải (thay lớp vàng cũ).
    private static void BuildTodayBadge(SevenDayCellUI cell)
    {
        RectTransform today = (RectTransform)cell.objToday.transform;
        ReskinPrefabUtil.Img(today, ReskinPrefabUtil.UIKit("BattlePass/icon_alert"));
        today.anchorMin = today.anchorMax = Vector2.one;
        today.pivot = new Vector2(0.5f, 0.5f);
        today.sizeDelta = new Vector2(60f, 60f);
        today.anchoredPosition = new Vector2(-2f, -2f);
        today.localEulerAngles = Vector3.zero;
        today.SetAsLastSibling();
    }
    #endregion

    #region Nút
    // Footer xếp dọc: ô đếm giờ (chỉ hiện khi đã nhận hôm nay) + nút Claim; tắt ô đếm giờ thì nút tự về giữa.
    private static void BuildFooter(RectTransform panel, UISevenDayPopup popup, TMP_Text template)
    {
        RectTransform footer = GetOrCreate(panel, "Footer");
        Box(footer, 0f, FOOTER_Y, 600f, 284f);
        VerticalLayoutGroup layout = footer.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        RectTransform timer = GetOrCreate(footer, "Timer");
        ReskinPrefabUtil.Img(timer, ReskinPrefabUtil.UIKit("Mastery/mastery_panel"));
        timer.sizeDelta = new Vector2(560f, 96f);
        TMP_Text timerText = GetOrCreateText(timer, "Text", template);
        Stretch(timerText.rectTransform);
        timerText.rectTransform.offsetMin = new Vector2(24f, 0f);
        timerText.rectTransform.offsetMax = new Vector2(-24f, 0f);
        SetPlain(timerText, Color.white, 38f, 24f);
        timerText.text = "Next gift in <color=#FFD35A>16h 3m</color>";
        timer.SetSiblingIndex(0);
        popup.objTimer = timer.gameObject;
        popup.txtTimer = timerText;

        RectTransform claim = (RectTransform)popup.btClaim.transform;
        claim.SetParent(footer, false);
        claim.SetSiblingIndex(1);
        claim.localScale = Vector3.one;
        claim.sizeDelta = new Vector2(480f, 164f);
        popup.imgClaim = ReskinPrefabUtil.Img(claim, ReskinPrefabUtil.UIKit("Lobby/btn_green"));
        popup.sprClaimOn = popup.imgClaim.sprite;
        popup.sprClaimOff = ReskinPrefabUtil.UIKit("Lobby/btn_gray");
        popup.btClaim.transition = Selectable.Transition.None;   // nút khoá đổi sprite xám, không tô thêm
        popup.txtClaim.rectTransform.sizeDelta = new Vector2(420f, 100f);
        popup.txtClaim.enableAutoSizing = false;
        popup.txtClaim.fontSize = 68f;
    }

    // Bấm đâu cũng đóng (rơi xuống nền) trừ nút Claim và nút X.
    private static void SetRaycasts(RectTransform panel, UISevenDayPopup popup)
    {
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            Transform t = graphic.transform;
            graphic.raycastTarget = t == popup.btClaim.transform || t == popup.btClose.transform;
        }
    }
    #endregion

    #region Tiện ích
    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        throw new System.Exception("[SevenDayPopupReskin] Không thấy node " + name);
    }

    private static RectTransform GetOrCreate(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return (RectTransform)t;
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, TMP_Text template)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.GetComponent<TMP_Text>();
        TMP_Text text = Object.Instantiate(template, parent);
        text.name = name;
        foreach (LocalizeText localize in text.GetComponents<LocalizeText>()) Object.DestroyImmediate(localize);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    // Chữ không viền (material mặc định của font Asap), tự co khi bản dịch dài.
    private static void SetPlain(TMP_Text text, Color color, float size, float minSize)
    {
        text.fontSharedMaterial = text.font.material;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = minSize;
        text.alignment = TextAlignmentOptions.Center;
    }

    private static void Box(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void RemoveComponent<T>(Component target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component);
    }
    #endregion
}
