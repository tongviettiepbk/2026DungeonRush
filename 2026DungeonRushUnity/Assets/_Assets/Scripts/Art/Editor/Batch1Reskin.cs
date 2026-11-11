using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 1 – Trang bị & Loot theo concept tools/concept/batch1/concept.png (kit chung PopupStyle).
// Sửa thẳng 6 prefab có sẵn (giữ GUID), chạy lại nhiều lần ra cùng kết quả. Các builder cũ (ForgePrefabBuilder,
// LevelPopupPrefabBuilder, OfflineEarningPrefabBuilder) gọi lại tool này ở cuối.
// Toạ độ = đơn vị canvas, đo từ MÉP TRÊN panel tới TÂM phần tử (PopupStyle.PlaceTop).
public static class Batch1Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";

    // Ô substat: mỗi dòng 50, lề trên/dưới trong ô nâu 24.
    private const float SUB_LINE = 50f;
    private const float SUB_FONT = 34f;
    private const float SUB_BOX_PAD = 24f;
    private const float SLOT = 192f;

    [MenuItem("Tools/DungeonRush/Reskin/Batch 1 (Gear & Loot)")]
    public static string ApplyAll()
    {
        ApplyItemInfo();
        ApplyNewItem();
        ApplyNewForge();
        ApplyAutoForge();
        ApplyLevel();
        ApplyOfflineEarning();
        return "ok";
    }

    private delegate void Edit(GameObject root);

    private static void EditPrefab(string name, Edit edit)
    {
        string path = UI + name + ".prefab";
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
        Debug.Log("[Batch1Reskin] " + name + " done");
    }

    #region Khối chữ 1 món (ItemStatsView)
    internal struct StatsLayout
    {
        public float slotX;
        public float slotTop;        // từ mép trên card tới tâm ô đồ
        public float infoLeft;
        public float infoWidth;
        public bool twoMainLines;    // Wing/Cape có 2 chỉ số chính
        public float subTop;         // mép trên khối substat
        public float subWidth;
        public int subLines;
        public bool subBox;          // có ô nâu box_tan (NewForge thì card đã là ô nâu)
    }

    internal static ItemStatsView BuildStats(Transform card, Transform element, TMP_Text rarity, TMP_Text name,
        TMP_Text mainValues, TMP_Text subNames, StatsLayout l)
    {
        PopupStyle.PlaceTop(element, l.slotX, l.slotTop, SLOT, SLOT);

        float infoTop = l.slotTop - (l.twoMainLines ? 102f : 76f);
        Reparent(rarity.transform, card);
        PopupStyle.PlaceTopLeft(rarity.transform, l.infoLeft, infoTop + 22f, l.infoWidth, 44f);
        PopupStyle.Outline(rarity, 34f, 24f);
        rarity.alignment = TextAlignmentOptions.Left;
        rarity.enableWordWrapping = false;
        PopupStyle.RemoveKey(rarity);

        Reparent(name.transform, card);
        PopupStyle.PlaceTopLeft(name.transform, l.infoLeft, infoTop + 72f, l.infoWidth, 56f);
        PopupStyle.Outline(name, 48f, 28f);
        name.alignment = TextAlignmentOptions.Left;
        name.enableWordWrapping = false;
        PopupStyle.RemoveKey(name);

        float mainHeight = l.twoMainLines ? 104f : 52f;
        float mainCenter = infoTop + 100f + mainHeight / 2f;
        TMP_Text mainLabels = PopupStyle.GetOrCreateText(card, "MainStatLabels");
        PopupStyle.PlaceTopLeft(mainLabels.transform, l.infoLeft, mainCenter, 260f, mainHeight);
        PopupStyle.Plain(mainLabels, PopupStyle.BROWN, 44f);
        mainLabels.alignment = TextAlignmentOptions.TopLeft;
        mainLabels.enableWordWrapping = false;
        mainLabels.text = "Health";

        Reparent(mainValues.transform, card);
        PopupStyle.PlaceTopLeft(mainValues.transform, l.infoLeft + 150f, mainCenter, 300f, mainHeight);
        PopupStyle.Outline(mainValues, 44f);
        mainValues.alignment = TextAlignmentOptions.TopLeft;
        mainValues.enableWordWrapping = false;
        PopupStyle.RemoveKey(mainValues);
        mainLabels.lineSpacing = mainValues.lineSpacing = LineSpacing(44f, 52f);

        // Khối substat cao cố định theo số dòng tối đa (popup không nhảy kích thước).
        float subHeight = l.subLines * SUB_LINE + (l.subBox ? SUB_BOX_PAD * 2f : 0f);
        Transform subRoot = card.Find("SubStats");
        if (subRoot == null)
        {
            GameObject go = new GameObject("SubStats", typeof(RectTransform));
            go.layer = card.gameObject.layer;
            go.transform.SetParent(card, false);
            subRoot = go.transform;
        }
        PopupStyle.PlaceTop(subRoot, 0f, l.subTop + subHeight / 2f, l.subWidth, subHeight);
        if (l.subBox)
            PopupStyle.NewImage(subRoot.parent, "SubStats", PopupStyle.Kit("Mastery/box_tan"));
        else
            PopupStyle.RemoveComponent<Image>(subRoot);

        float inset = l.subBox ? 32f : 4f;
        float pad = l.subBox ? SUB_BOX_PAD : 0f;
        Reparent(subNames.transform, subRoot);
        PopupStyle.Stretch(subNames.transform, inset, pad, inset + 170f, pad);
        PopupStyle.Plain(subNames, PopupStyle.BROWN, SUB_FONT);
        subNames.alignment = TextAlignmentOptions.TopLeft;
        subNames.enableWordWrapping = false;
        PopupStyle.RemoveKey(subNames);

        TMP_Text subValues = PopupStyle.GetOrCreateText(subRoot, "SubStatValues");
        PopupStyle.Stretch(subValues.transform, l.subWidth - inset - 200f, pad, inset, pad);
        PopupStyle.Outline(subValues, SUB_FONT);
        subValues.alignment = TextAlignmentOptions.TopRight;
        subValues.enableWordWrapping = false;
        subNames.lineSpacing = subValues.lineSpacing = LineSpacing(SUB_FONT, SUB_LINE);

        TMP_Text noSub = PopupStyle.GetOrCreateText(subRoot, "NoSubStats");
        PopupStyle.Stretch(noSub.transform);
        PopupStyle.Plain(noSub, PopupStyle.MUTE, 32f);
        noSub.alignment = TextAlignmentOptions.Center;
        PopupStyle.SetKey(noSub, "Popup.Item.NoBonusStats", "No bonus stats");
        noSub.gameObject.SetActive(false);

        ItemStatsView view = card.GetComponent<ItemStatsView>();
        if (view == null) view = card.gameObject.AddComponent<ItemStatsView>();
        view.txtRarity = rarity;
        view.txtName = name;
        view.txtMainLabels = mainLabels;
        view.txtMainValues = mainValues;
        view.txtSubNames = subNames;
        view.txtSubValues = subValues;
        view.txtNoSub = noSub;
        return view;
    }

    // TMP lineSpacing đơn vị 0.01 em: chỉnh để mỗi dòng cao đúng `lineHeight` đơn vị canvas.
    private static float LineSpacing(float fontSize, float lineHeight)
    {
        UnityEngine.TextCore.FaceInfo face = PopupStyle.Font.faceInfo;
        float natural = face.lineHeight / face.pointSize * fontSize;
        return (lineHeight - natural) / fontSize * 100f;
    }

    // Nhãn đỏ "NEW" nghiêng ở góc trên-trái ô đồ; chữ NEW cũ thành con của nhãn. Trả về nhãn (để script bật/tắt).
    internal static GameObject NewTag(Transform card, TMP_Text newText, float slotX, float slotTop)
    {
        Image tag = PopupStyle.NewImage(card, "NewTag", PopupStyle.Kit("Events/ribbon_red"));
        PopupStyle.PlaceTop(tag.transform, slotX - SLOT / 2f + 30f, slotTop - SLOT / 2f + 10f, 132f, 56f);
        tag.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
        Reparent(newText.transform, tag.transform);
        PopupStyle.Stretch(newText.transform, 14f, 0f, 34f, 0f);
        PopupStyle.Outline(newText, 28f);
        newText.alignment = TextAlignmentOptions.Center;
        newText.gameObject.SetActive(true);
        tag.transform.SetAsLastSibling();
        return tag.gameObject;
    }

    private static void Reparent(Transform t, Transform parent)
    {
        if (t.parent != parent) t.SetParent(parent, false);
    }
    #endregion

    #region Item Info (vừa 816): Wing tối đa 2 chỉ số chính + 5 substat
    private const float INFO_HEIGHT = 818f;

    public static void ApplyItemInfo()
    {
        EditPrefab("UIItemInfoPopup", root =>
        {
            UIGearInfo ui = root.GetComponent<UIGearInfo>();
            RectTransform panel = (RectTransform)Find(root.transform, "Content");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, INFO_HEIGHT, panel.Find("Frame"), panel.Find("Header"));

            TMP_Text title = PopupStyle.GetOrCreateText(panel, "Title");
            PopupStyle.Title(title, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.SetKey(title, "Popup.ItemInfo.Title", "Item Info");
            PopupStyle.CloseButton(ui.btClose.transform);

            TMP_Text rarity = Text(panel, "RarityText");
            TMP_Text name = Text(panel, "InfoText");
            TMP_Text mainValues = PopupStyle.GetOrCreateText(panel, "MainStatValues");
            TMP_Text subNames = Find(panel, "SubStats/SubStatNames") != null
                ? Text(panel, "SubStats/SubStatNames") : PopupStyle.GetOrCreateText(panel, "SubStatNames");
            ui.stats = BuildStats(panel, ui.itemElement.transform, rarity, name, mainValues, subNames, new StatsLayout
            {
                slotX = -258f, slotTop = 272f, infoLeft = -134f, infoWidth = 488f, twoMainLines = true,
                subTop = 396f, subWidth = 708f, subLines = 5, subBox = true,
            });

            TMP_Text tap = PopupStyle.GetOrCreateText(panel, "TapToClose");
            PopupStyle.PlaceTop(tap.transform, 0f, 741f, 708f, 40f);
            PopupStyle.Plain(tap, PopupStyle.MUTE, 30f, 22f);
            tap.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(tap, "Common.TapToClose", "Tap anywhere to close");
            ui.btClose.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region New Item (vừa 816): gear 1 chỉ số chính + tối đa 3 substat
    private const float NEW_ITEM_HEIGHT = 838f;

    public static void ApplyNewItem()
    {
        EditPrefab("UINewItemPopup", root =>
        {
            UINewItemPopup ui = root.GetComponent<UINewItemPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            Transform frame = panel.Find("Equipped");
            // Mọi nội dung của khung cũ ra làm con trực tiếp của panel, khung chỉ còn là ảnh nền.
            MoveChildren(frame, panel);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, NEW_ITEM_HEIGHT, frame, panel.Find("Header"));

            TMP_Text title = PopupStyle.GetOrCreateText(panel, "Title");
            PopupStyle.Title(title, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.SetKey(title, "Popup.NewItem.Title", "New Item!");

            Transform stats = panel.Find("Stats");
            TMP_Text mainValues = FindText(panel, "BaseStatText", stats);
            TMP_Text subNames = FindText(panel, "SubStatText", stats);
            ui.stats = BuildStats(panel, ui.itemElement.transform, Text(panel, "RarityText"), FindText(panel, "NameText", stats),
                mainValues, subNames, new StatsLayout
                {
                    slotX = -258f, slotTop = 272f, infoLeft = -134f, infoWidth = 488f, twoMainLines = false,
                    subTop = 396f, subWidth = 708f, subLines = 3, subBox = true,
                });
            if (stats != null) stats.gameObject.SetActive(false);

            Transform newTag = panel.Find("NewTag");
            TMP_Text newText = newTag != null ? Text(newTag, "NewTextEquipped") : Text(panel, "NewTextEquipped");
            NewTag(panel, newText, -258f, 272f);

            PopupStyle.Button(ui.btEquip.transform, PopupStyle.ButtonColor.Green, 340f);
            PopupStyle.PlaceTop(ui.btEquip.transform, 0f, 705f, 340f, PopupStyle.BUTTON_HEIGHT);
        });
    }
    #endregion

    #region New Forge (lớn 968): 2 thẻ cao bằng nhau, mỗi thẻ tối đa 3 substat
    private const float FORGE_HEIGHT = 1392f;
    private const float CARD_WIDTH = 860f;
    private const float CARD_HEIGHT = 470f;
    private const float CONFIRM_HEIGHT = 570f;

    public static void ApplyNewForge()
    {
        EditPrefab("UINewForgePopup", root =>
        {
            UINewForgePopup ui = root.GetComponent<UINewForgePopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, 0f);
            Transform frame = panel.Find("Equipped");

            // Thẻ trên (đồ đang mặc): ô nâu mới, nhận lại mọi nội dung của khung cũ.
            Image equippedCard = PopupStyle.NewImage(panel, "EquippedCard", PopupStyle.Kit("Mastery/box_tan"));
            MoveChildren(frame, equippedCard.transform);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_LARGE, FORGE_HEIGHT, frame, panel.Find("Header"));

            TMP_Text title = PopupStyle.GetOrCreateText(panel, "Title");
            PopupStyle.Title(title, PopupStyle.WIDTH_LARGE);
            PopupStyle.SetKey(title, "Popup.NewItem.Title", "New Item!");

            Transform newCard = panel.Find("NewItem");
            ReskinPrefabUtil.Img(newCard, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(equippedCard.transform, 0f, 411f, CARD_WIDTH, CARD_HEIGHT);
            PopupStyle.PlaceTop(newCard, 0f, 917f, CARD_WIDTH, CARD_HEIGHT);
            equippedCard.transform.SetSiblingIndex(2);
            newCard.SetSiblingIndex(3);

            StatsLayout card = new StatsLayout
            {
                slotX = -298f, slotTop = 176f, infoLeft = -174f, infoWidth = 568f, twoMainLines = false,
                subTop = 288f, subWidth = 788f, subLines = 3, subBox = false,
            };
            Transform eq = equippedCard.transform;
            Transform eqStats = eq.Find("Stats");
            ui.equippedStats = BuildStats(eq, ui.equippedElement.transform, Text(eq, "RarityText"),
                FindText(eq, "NameText", eqStats), FindText(eq, "BaseStatText", eqStats), FindText(eq, "SubStatText", eqStats), card);
            if (eqStats != null) eqStats.gameObject.SetActive(false);

            Transform nwStats = newCard.Find("Stats");
            TMP_Text newValues = FindText(newCard, "BaseStatText", nwStats);
            ui.newStats = BuildStats(newCard, ui.newElement.transform, Text(newCard, "RarityText_1"),
                FindText(newCard, "NameText", nwStats), newValues, FindText(newCard, "SubStatText", nwStats), card);
            if (nwStats != null) nwStats.gameObject.SetActive(false);

            // Mũi tên so sánh: con của ô giá trị, neo đúng pivot chữ để script đặt theo textBounds.
            RectTransform arrow = ui.imgCompareArrow.rectTransform;
            Reparent(arrow, newValues.transform);
            arrow.anchorMin = arrow.anchorMax = newValues.rectTransform.pivot;
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.sizeDelta = new Vector2(36f, 36f);

            // Nhãn xanh "Equipped" lấn mép trên-trái thẻ trên.
            Transform label = eq.Find("EquippedLabel");
            ReskinPrefabUtil.Img(label, PopupStyle.Kit("Gameplay/tag_equipped"));
            PopupStyle.PlaceTop(label, -CARD_WIDTH / 2f - 20f + 96f, 2f, 192f, 67f);
            TMP_Text labelText = label.GetComponentInChildren<TMP_Text>(true);
            PopupStyle.Center(labelText.transform, -14f, 3f, 150f, 50f);
            PopupStyle.Outline(labelText, 28f, 20f);
            labelText.alignment = TextAlignmentOptions.Center;
            label.SetAsLastSibling();

            ui.objEquippedNew = NewTag(eq, FindNewText(eq, "NewTextEquipped"), card.slotX, card.slotTop);
            ui.objNewNew = NewTag(newCard, FindNewText(newCard, "NewTextNewItem"), card.slotX, card.slotTop);

            PopupStyle.Button(ui.btSell.transform, PopupStyle.ButtonColor.Red, 340f);
            PopupStyle.PlaceTop(ui.btSell.transform, -200f, 1259f, 340f, PopupStyle.BUTTON_HEIGHT);
            PopupStyle.Button(ui.btEquip.transform, PopupStyle.ButtonColor.Green, 340f);
            PopupStyle.PlaceTop(ui.btEquip.transform, 200f, 1259f, 340f, PopupStyle.BUTTON_HEIGHT);

            ApplyConfirm(ui);
        });
    }

    // Hộp hỏi bán món mạnh hơn: popup nhỏ 680 chồng giữa màn. Phụ (Cancel tím) trái, chính (bán đỏ) phải.
    private static void ApplyConfirm(UINewForgePopup ui)
    {
        RectTransform confirm = (RectTransform)ui.objConfirm.transform;
        Transform frame = confirm.Find("Background");
        Transform dark = confirm.Find("Dark");
        PopupStyle.Shell(confirm, PopupStyle.WIDTH_SMALL, CONFIRM_HEIGHT, frame, confirm.Find("Header"));
        if (dark != null)
        {
            PopupStyle.Center(dark, 0f, 0f, 4000f, 6000f);
            dark.SetAsFirstSibling();
        }
        confirm.SetAsLastSibling();

        TMP_Text title = PopupStyle.GetOrCreateText(confirm, "Title");
        PopupStyle.Title(title, PopupStyle.WIDTH_SMALL);
        PopupStyle.SetKey(title, "Popup.NewForge.SellTitle", "Sell Item?");

        PopupStyle.PlaceTop(ui.txtConfirm.transform, 0f, 251f, 572f, 150f);
        PopupStyle.Plain(ui.txtConfirm, PopupStyle.BROWN, 40f, 28f);
        ui.txtConfirm.alignment = TextAlignmentOptions.Center;

        PopupStyle.Button(ui.btConfirmNo.transform, PopupStyle.ButtonColor.Violet, 280f);
        PopupStyle.PlaceTop(ui.btConfirmNo.transform, -160f, 437f, 280f, PopupStyle.BUTTON_HEIGHT);
        PopupStyle.Button(ui.btConfirmYes.transform, PopupStyle.ButtonColor.Red, 280f);
        PopupStyle.PlaceTop(ui.btConfirmYes.transform, 160f, 437f, 280f, PopupStyle.BUTTON_HEIGHT);
    }

    private static TMP_Text FindNewText(Transform card, string name)
    {
        Transform tag = card.Find("NewTag");
        return tag != null && tag.Find(name) != null ? Text(tag, name) : Text(card, name);
    }
    #endregion

    #region Auto Loot (lớn 968): vùng giữa cuộn (10 dòng rarity + 13 ô lọc), số hộp + nút ghim đáy
    private const float AUTO_HEIGHT = 1640f;
    private const float AUTO_BOTTOM = 332f;      // khu đáy: hàng số hộp 104 + cách 20 + nút 150 + lề 58
    private const float ROW_HEIGHT = 60f;
    private const float CHECK_SIZE = 60f;
    private const float BAR_LEFT = 76f;
    private const float SCROLL_PAD = 8f;
    private static readonly string[] RARITY_BARS =
        { "common", "uncommon", "rare", "epic", "legendary", "mythic", "artifact", "ancient", "immortal", "divine" };

    public static void ApplyAutoForge()
    {
        EditPrefab("UIAutoForgePopup", root =>
        {
            UIAutoForgePopup ui = root.GetComponent<UIAutoForgePopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, 0f);
            Transform bg = panel.Find("Bg");
            PopupStyle.Center(panel, 0f, 0f, PopupStyle.WIDTH_LARGE, AUTO_HEIGHT);
            PopupStyle.Stretch(bg);
            ReskinPrefabUtil.Img(bg, PopupStyle.Kit("Mastery/popup_frame"));
            PopupStyle.Shell((RectTransform)bg, PopupStyle.WIDTH_LARGE, AUTO_HEIGHT, bg, bg.Find("Header"));
            PopupStyle.Stretch(bg);

            // Tiêu đề + X.
            Transform top = bg.Find("TopArea");
            PopupStyle.Stretch(top);
            TMP_Text title = Text(top, "TitleText");
            PopupStyle.Title(title, PopupStyle.WIDTH_LARGE);
            PopupStyle.CloseButton(ui.btClose.transform);

            // Vùng cuộn: từ dưới header tới trên khu đáy.
            Transform scroll = bg.Find("Scroll View");
            float scrollHeight = AUTO_HEIGHT - PopupStyle.CONTENT_TOP - AUTO_BOTTOM - 20f;
            float innerWidth = PopupStyle.WIDTH_LARGE - 2f * PopupStyle.SIDE_INSET;
            PopupStyle.PlaceTop(scroll, 0f, PopupStyle.CONTENT_TOP + scrollHeight / 2f, innerWidth, scrollHeight);
            // Mặt nạ cắt vuông (sprite UIMask bo mờ mép 10 làm đứt viền đen ô tick/thanh) + chừa lề 8 hai bên.
            Transform viewport = scroll.Find("Viewport");
            PopupStyle.Stretch(viewport);
            viewport.GetComponent<Image>().sprite = null;
            Transform content = viewport.Find("Content");
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            SetVertical(contentLayout, 24f);
            contentLayout.padding = new RectOffset((int)SCROLL_PAD, (int)SCROLL_PAD, 6, 6);

            Transform loot = content.Find("LootList");
            SetVertical(loot.GetComponent<VerticalLayoutGroup>(), 10f);
            StyleLootHeader(loot.Find("Header"));
            for (int i = 0; i < ui.rarityRows.Count; i++)
                StyleRarityRow(ui.rarityRows[i].transform, RARITY_BARS[i]);
            StyleFilterRow(ui, loot.Find("Filter"));

            Transform filterList = content.Find("FilterList");
            PopupStyle.RemoveComponent<VerticalLayoutGroup>(filterList);
            GridLayoutGroup grid = filterList.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = filterList.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2((innerWidth - 2f * SCROLL_PAD - 16f) / 2f, 56f);
            grid.spacing = new Vector2(16f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.padding = new RectOffset(0, 0, 0, 0);
            foreach (Transform row in filterList)
                StyleSubStatRow(row);

            ApplyAutoBottom(ui, bg);
        });
    }

    private static void SetVertical(VerticalLayoutGroup layout, float spacing)
    {
        if (layout == null) return;
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);
    }

    private static void StyleLootHeader(Transform header)
    {
        PopupStyle.Invisible(header.GetComponent<Image>());
        SetHeight(header, 44f);
        TMP_Text keep = Text(header, "CurrentLevelText_1");
        TMP_Text prob = Text(header, "NextLevelText");
        LeftText(keep, 0f, 400f);
        PopupStyle.Plain(keep, PopupStyle.BROWN, 36f, 24f);
        RightText(prob, -16f, 300f);
        PopupStyle.Plain(prob, PopupStyle.MUTE, 30f, 22f);
    }

    private static void StyleRarityRow(Transform row, string bar)
    {
        SetHeight(row, ROW_HEIGHT);
        Transform background = row.Find("RarityBackground");
        ReskinPrefabUtil.Img(background, PopupStyle.Kit("RarityTable/bar_" + bar));
        PopupStyle.Stretch(background, BAR_LEFT);
        // Hoạ tiết Ancient/Immortal/Divine: mặt nạ theo đúng thanh.
        foreach (Transform child in row)
        {
            if (child.GetComponent<Mask>() == null) continue;
            ReskinPrefabUtil.Img(child, PopupStyle.Kit("RarityTable/bar_mask"));
            PopupStyle.Stretch(child, BAR_LEFT);
        }
        StyleCheck(row.Find("CheckButtonImg"), CHECK_SIZE);
        LeftText(Text(row, "RarityText"), BAR_LEFT + 18f, 420f);
        PopupStyle.Outline(Text(row, "RarityText"), 30f, 22f);
        RightText(Text(row, "CommonNextRate"), -18f, 220f);
        PopupStyle.Outline(Text(row, "CommonNextRate"), 30f, 22f);
        foreach (Transform child in row)
            if (child.name.StartsWith("CheckButton") && child.GetComponent<Button>() != null)
                PopupStyle.Invisible(child.GetComponent<Image>());
    }

    // Ô tick: nền tan vuông + dấu tick xanh to hơn ô (lấn ra như concept).
    private static void StyleCheck(Transform box, float size)
    {
        ReskinPrefabUtil.Img(box, PopupStyle.Kit("ClanSearch/input_tan"));
        RectTransform rt = (RectTransform)box;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(size / 2f, 0f);
        Transform check = box.Find("CheckImage");
        ReskinPrefabUtil.Img(check, PopupStyle.Kit("ClanSearch/check_green"));
        PopupStyle.Center(check, 4f, 8f, size * 1.07f, size * 1.07f);
    }

    // Hàng "Filter": chữ nâu trái, công tắc phải (track tối + núm; bật = fill xanh + núm), khoá = ổ khoá + "Lv 7".
    private static void StyleFilterRow(UIAutoForgePopup ui, Transform row)
    {
        SetHeight(row, 88f);
        TMP_Text label = Text(row, "FilterText");
        LeftText(label, 0f, 500f);
        PopupStyle.Plain(label, PopupStyle.BROWN, 36f, 24f);

        // Công tắc 180×88: vùng bấm trong suốt, rãnh tối cao 64; tắt = núm xám trái, bật = fill xanh + núm phải.
        Transform toggle = ui.btFilterToggle.transform;
        PopupStyle.Invisible(toggle.GetComponent<Image>());
        RectTransform rt = (RectTransform)toggle;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(180f, 88f);
        rt.anchoredPosition = Vector2.zero;

        Image track = PopupStyle.NewImage(toggle, "Track", PopupStyle.Kit("Setting/track_dark"));
        PopupStyle.Stretch(track.transform, 0f, 12f, 0f, 12f);
        track.transform.SetAsFirstSibling();

        Image knobOff = PopupStyle.NewImage(toggle, "KnobOff", PopupStyle.Kit("Setting/knob_off"));
        PopupStyle.Center(knobOff.transform, -51f, 0f, 77f, 88f);
        ui.objFilterOff = knobOff.gameObject;

        Transform on = ui.objFilterOn.transform;
        ReskinPrefabUtil.Img(on, PopupStyle.Kit("Setting/fill_green"));
        PopupStyle.Stretch(on, 0f, 12f, 0f, 12f);
        Image knobOn = PopupStyle.NewImage(on, "KnobOn", PopupStyle.Kit("Setting/knob_on"));
        PopupStyle.Center(knobOn.transform, 51f, 0f, 77f, 88f);
        on.SetAsLastSibling();

        Transform lockImage = ui.objFilterLock.transform;
        ReskinPrefabUtil.Img(lockImage, PopupStyle.Kit("BattlePass/icon_lock"));
        rt = (RectTransform)lockImage;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(36f, 42f);
        rt.anchoredPosition = new Vector2(-150f, 0f);
        TMP_Text lockText = PopupStyle.GetOrCreateText(lockImage, "LockText");
        rt = lockText.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(110f, 44f);
        rt.anchoredPosition = new Vector2(8f, 0f);
        PopupStyle.Plain(lockText, PopupStyle.MUTE, 30f, 20f);
        lockText.alignment = TextAlignmentOptions.Left;
        lockText.text = "Lv 7";
        ui.txtFilterLock = lockText;
    }

    private static void StyleSubStatRow(Transform row)
    {
        PopupStyle.Invisible(row.Find("SubStatBackground").GetComponent<Image>());
        StyleCheck(row.Find("SubStatCheckImg"), 52f);
        TMP_Text name = Text(row, "SubStatNameText");
        LeftText(name, 66f, 340f);
        PopupStyle.Plain(name, PopupStyle.BROWN, 30f, 18f);
        name.enableWordWrapping = false;
        foreach (Transform child in row)
            if (child.name.StartsWith("SubStatCheckButton"))
                PopupStyle.Invisible(child.GetComponent<Image>());
    }

    private static void ApplyAutoBottom(UIAutoForgePopup ui, Transform bg)
    {
        RectTransform bottom = (RectTransform)bg.Find("BottomArea");
        bottom.anchorMin = bottom.anchorMax = bottom.pivot = new Vector2(0.5f, 0f);
        bottom.sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, AUTO_BOTTOM);
        bottom.anchoredPosition = Vector2.zero;
        Transform line = bottom.Find("Line");
        if (line != null) line.gameObject.SetActive(false);

        const float rowY = PopupStyle.BOTTOM_INSET + PopupStyle.BUTTON_HEIGHT + 20f + 52f;
        TMP_Text info = Text(bottom, "InfoText");
        RectTransform rt = info.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(520f, 60f);
        rt.anchoredPosition = new Vector2(PopupStyle.SIDE_INSET, rowY);
        PopupStyle.Plain(info, PopupStyle.BROWN, 36f, 24f);
        info.alignment = TextAlignmentOptions.Left;

        Transform count = ui.btHammerCount.transform;
        ReskinPrefabUtil.Img(count, PopupStyle.Kit("Common/btn_violet"));
        rt = (RectTransform)count;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(240f, 104f);
        rt.anchoredPosition = new Vector2(-PopupStyle.SIDE_INSET, rowY);
        PopupStyle.Center(ui.txtSelectedHammer.transform, 0f, 4f, 200f, 70f);
        PopupStyle.Outline(ui.txtSelectedHammer, 40f);
        ui.txtSelectedHammer.alignment = TextAlignmentOptions.Center;
        Transform arrow = count.Find("Image");
        if (arrow != null) arrow.gameObject.SetActive(false);

        Transform start = ui.btStart.transform;
        PopupStyle.Button(start, PopupStyle.ButtonColor.Orange, 400f);
        rt = (RectTransform)start;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, PopupStyle.BOTTOM_INSET + PopupStyle.BUTTON_HEIGHT / 2f);

        ApplyHammerPanel(ui, (RectTransform)bg.parent);
    }

    // Bảng chọn số hộp: popup vừa 816 giữa màn, lưới 4×2 ô vuông (đổi sprite theo trạng thái trong script).
    private const float HAMMER_HEIGHT = 506f;
    private const float HAMMER_CELL = 128f;

    private static void ApplyHammerPanel(UIAutoForgePopup ui, RectTransform popup)
    {
        RectTransform panel = (RectTransform)ui.hammerPanel.transform;
        Reparent(panel, popup);
        Transform bgButton = panel.Find("Background");
        PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, HAMMER_HEIGHT, panel.Find("Frame"), panel.Find("Header"));
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null) panelImage.enabled = false;
        if (bgButton != null)
        {
            PopupStyle.Center(bgButton, 0f, 0f, 4000f, 6000f);
            bgButton.SetAsFirstSibling();
        }
        panel.SetAsLastSibling();

        TMP_Text title = PopupStyle.GetOrCreateText(panel, "Title");
        PopupStyle.Title(title, PopupStyle.WIDTH_MEDIUM);
        PopupStyle.SetKey(title, "Popup.AutoLoot.BoxCountInfo", "Boxes used at once");

        Transform close = ui.btHammerPanelClose.transform;
        PopupStyle.CloseButton(close);
        foreach (Transform child in close) child.gameObject.SetActive(false);

        Transform options = panel.Find("VerticalLayout");
        PopupStyle.RemoveComponent<VerticalLayoutGroup>(options);
        GridLayoutGroup grid = options.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = options.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(HAMMER_CELL, HAMMER_CELL);
        grid.spacing = new Vector2(16f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;
        float gridWidth = 4f * HAMMER_CELL + 3f * 16f;
        float gridHeight = 2f * HAMMER_CELL + 16f;
        PopupStyle.PlaceTop(options, 0f, PopupStyle.CONTENT_TOP + gridHeight / 2f, gridWidth, gridHeight);
        foreach (Transform child in options)
        {
            if (child.GetComponent<Button>() != null) continue;
            // Mũi tên tutorial cũ không phải ô lưới.
            LayoutElement element = child.GetComponent<LayoutElement>();
            if (element == null) element = child.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
        }

        // Tay chỉ tutorial (FTUE step 20) trỏ ô 2 búa = ô thứ 2 hàng trên của lưới.
        if (ui.hammerOptionTutorial != null)
        {
            RectTransform tutorial = (RectTransform)ui.hammerOptionTutorial.transform;
            Vector2 size = tutorial.sizeDelta;
            PopupStyle.PlaceTop(tutorial, -gridWidth / 2f + HAMMER_CELL * 1.5f + 16f, PopupStyle.CONTENT_TOP + HAMMER_CELL / 2f, size.x, size.y);
        }

        ui.sprOptionSelected = PopupStyle.Kit("BattlePass/btn_orange");
        ui.sprOptionDefault = PopupStyle.Kit("Lobby/btn_blue");
        ui.sprOptionLocked = PopupStyle.Kit("Lobby/btn_gray");
        for (int i = 0; i < ui.hammerOptions.Count; i++)
        {
            Image image = ReskinPrefabUtil.Img(ui.hammerOptions[i].transform, ui.sprOptionDefault);
            image.color = Color.white;
            TMP_Text text = ui.hammerOptionTexts[i];
            PopupStyle.Center(text.transform, 0f, 8f, 110f, 70f);
            PopupStyle.Outline(text, 40f, 26f);
            text.alignment = TextAlignmentOptions.Center;
            text.gameObject.SetActive(true);
            Transform locked = ui.hammerOptionLocks[i].transform;
            ReskinPrefabUtil.Img(locked, PopupStyle.Kit("BattlePass/icon_lock"));
            PopupStyle.Center(locked, 48f, 48f, 44f, 51f);
        }
    }

    private static void SetHeight(Transform t, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private static void LeftText(TMP_Text text, float x, float width)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, 56f);
        rt.anchoredPosition = new Vector2(x, 0f);
        text.alignment = TextAlignmentOptions.Left;
    }

    private static void RightText(TMP_Text text, float x, float width)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(width, 56f);
        rt.anchoredPosition = new Vector2(x, 0f);
        text.alignment = TextAlignmentOptions.Right;
    }
    #endregion

    #region Level / Rarity Table (lớn 968)
    private const float LEVEL_HEIGHT = 1364f;

    public static void ApplyLevel()
    {
        EditPrefab("UILevelPopup", root =>
        {
            UILevelPopup ui = root.GetComponent<UILevelPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "LevelPopup/ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, 0f);
            Transform bg = panel.Find("Background");
            PopupStyle.Center(panel, 0f, 0f, PopupStyle.WIDTH_LARGE, LEVEL_HEIGHT);
            PopupStyle.Shell((RectTransform)bg, PopupStyle.WIDTH_LARGE, LEVEL_HEIGHT, bg, bg.Find("Header"));
            PopupStyle.Stretch(bg);

            PopupStyle.Title(ui.LevelText, PopupStyle.WIDTH_LARGE);
            PopupStyle.CloseButton(ui.CloseButton.transform);

            TMP_Text subtitle = Text(bg, "InfoText");
            PopupStyle.PlaceTop(subtitle.transform, 0f, 198f, 600f, 48f);
            PopupStyle.Plain(subtitle, PopupStyle.BROWN, 36f, 24f);
            subtitle.alignment = TextAlignmentOptions.Center;

            const float innerWidth = PopupStyle.WIDTH_LARGE - 2f * PopupStyle.SIDE_INSET;
            Transform table = bg.Find("RarityTable");
            PopupStyle.Invisible(table.GetComponent<Image>());
            PopupStyle.PlaceTop(table, 0f, 236f + 390f, innerWidth, 780f);
            Transform layoutRoot = table.Find("VerticalLayout");
            PopupStyle.Stretch(layoutRoot);
            VerticalLayoutGroup layout = layoutRoot.GetComponent<VerticalLayoutGroup>();
            SetVertical(layout, 10f);

            Transform header = layoutRoot.Find("Header");
            PopupStyle.Invisible(header.GetComponent<Image>());
            SetHeight(header, 40f);
            LeftText(Text(header, "TierText"), 16f, 300f);
            PopupStyle.Plain(Text(header, "TierText"), PopupStyle.BROWN, 32f, 22f);
            StyleRateColumns(ui.CurrentLevelText, ui.NextLevelText, true);

            for (int i = 0; i < ui.RarityRowElements.Count; i++)
            {
                Transform row = ui.RarityRowElements[i];
                SetHeight(row, ROW_HEIGHT);
                ((RectTransform)row).pivot = new Vector2(0.5f, 0.5f);
                TMP_Text name = Text(row, "RarityText");
                LeftText(name, 18f, 320f);
                PopupStyle.Outline(name, 30f, 22f);
                StyleRateColumns(ui.CurrentLevelRarityProbabilityTexts[i], ui.NextLevelRarityProbabilityTexts[i], false);
            }

            // Thưởng lên level: nhãn nâu bên trái ô tan.
            Transform rewards = bg.Find("Rewards");
            ReskinPrefabUtil.Img(rewards, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(rewards, 130f, 1082f, 260f, 100f);
            TMP_Text label = Text(rewards, "Label");
            RectTransform rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 56f);
            rt.anchoredPosition = new Vector2(-20f, 0f);
            PopupStyle.Plain(label, PopupStyle.BROWN, 36f, 24f);
            label.alignment = TextAlignmentOptions.Right;
            PopupStyle.Stretch(ui.GemCountText.transform);
            PopupStyle.Outline(ui.GemCountText, 48f, 30f);
            ui.GemCountText.alignment = TextAlignmentOptions.Center;

            // Thanh exp (track viền đen + fill thụt 4) và nút Complete/MAXED cùng chỗ.
            Transform slider = ui.ExperienceSlider.transform;
            PopupStyle.PlaceTop(slider, 0f, 1182f, innerWidth, 52f);
            Transform track = slider.Find("DurationRectTransform_1");
            ReskinPrefabUtil.Img(track, PopupStyle.Kit("Lobby/xp_track"));
            PopupStyle.Stretch(track);
            RectTransform fill = ui.ExperienceSlider.fillRect;
            ReskinPrefabUtil.Img(fill, PopupStyle.Kit("Lobby/xp_fill"));
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(-4f, -4f);
            PopupStyle.Center(ui.PercentageText.transform, 0f, 1f, innerWidth, 52f);
            PopupStyle.Outline(ui.PercentageText, 32f, 22f);
            ui.PercentageText.alignment = TextAlignmentOptions.Center;

            ui.CompleteButtonSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.MaxedButtonSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            PopupStyle.Button(ui.CompleteButton.transform, PopupStyle.ButtonColor.Green, 440f);
            PopupStyle.PlaceTop(ui.CompleteButton.transform, 0f, 1231f, 440f, PopupStyle.BUTTON_HEIGHT);
            ui.CompleteButtonImage.color = Color.white;
        });
    }

    private static void StyleRateColumns(TMP_Text current, TMP_Text next, bool isHeader)
    {
        RightText(current, -170f, 150f);
        RightText(next, -16f, 150f);
        if (isHeader)
        {
            PopupStyle.Plain(current, PopupStyle.BROWN, 32f, 22f);
            PopupStyle.Plain(next, PopupStyle.BROWN, 32f, 22f);
            return;
        }
        PopupStyle.Outline(current, 30f, 22f);
        PopupStyle.Outline(next, 30f, 22f);
    }
    #endregion

    #region Offline earning / Combat Summary (vừa 816)
    private const float OFFLINE_HEIGHT = 856f;
    private const float REWARD_BOX_WIDTH = 356f;
    private const float REWARD_BOX_HEIGHT = 184f;

    public static void ApplyOfflineEarning()
    {
        EditPrefab("UIOfflineEarningPopup", root =>
        {
            UIOfflineEarningPopup ui = root.GetComponent<UIOfflineEarningPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            Transform bg = panel.Find("Background");
            PopupStyle.Center(panel, 0f, 0f, PopupStyle.WIDTH_MEDIUM, OFFLINE_HEIGHT);
            PopupStyle.Shell((RectTransform)bg, PopupStyle.WIDTH_MEDIUM, OFFLINE_HEIGHT, bg, bg.Find("Header"));
            PopupStyle.Stretch(bg);

            PopupStyle.Title(Text(bg, "TitleText"), PopupStyle.WIDTH_MEDIUM);
            PopupStyle.CloseButton(ui.btClose.transform);

            const float innerWidth = PopupStyle.WIDTH_MEDIUM - 2f * PopupStyle.SIDE_INSET;
            PopupStyle.PlaceTop(ui.txtSummary.transform, 0f, 230f, innerWidth, 108f);
            PopupStyle.Plain(ui.txtSummary, PopupStyle.BROWN, 40f, 28f);
            ui.txtSummary.alignment = TextAlignmentOptions.Center;

            // 2 ô thưởng lớn: icon + số nhận (chữ viền), tốc độ /h nâu bên dưới.
            ui.txtExperience = RewardBox(bg.Find("CoinPerHour"), "GoldIcon", ui.txtExperience, ui.txtExpPerHour, -192f, "ExpAmount");
            ui.txtLootBox = RewardBox(bg.Find("BoxPerHour"), "BoxIcon", PopupStyle.GetOrCreateText(bg.Find("BoxPerHour"), "LootBoxAmount"),
                ui.txtLootBoxPerHour, 192f, "LootBoxAmount");

            // Trần thời gian: panel tối bo tròn, icon đồng hồ + chữ trắng (giờ vàng).
            Image dark = PopupStyle.NewImage(bg, "MaxRewardPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(dark.transform, 0f, 566f, 400f, 92f);
            Image clock = PopupStyle.NewImage(dark.transform, "Icon", PopupStyle.Art("2_Icons/Mastery/mastery_MaxOfflineTime"));
            clock.preserveAspect = true;
            PopupStyle.Center(clock.transform, -140f, 0f, 56f, 56f);
            Reparent(ui.txtMaxReward.transform, dark.transform);
            PopupStyle.Center(ui.txtMaxReward.transform, 36f, 0f, 280f, 60f);
            PopupStyle.Plain(ui.txtMaxReward, Color.white, 36f, 24f);
            ui.txtMaxReward.alignment = TextAlignmentOptions.Left;
            PopupStyle.RemoveKey(ui.txtMaxReward);

            // Nút: Claim tím (= X, nhận ×1) trái, x2 hồng (xem quảng cáo) phải.
            Transform doubleButton = ui.btDouble.transform;
            Transform collect = bg.Find("CollectButton");
            if (collect == null)
            {
                collect = Object.Instantiate(doubleButton.gameObject, bg).transform;
                collect.name = "CollectButton";
            }
            ui.btCollect = collect.GetComponent<Button>();
            PopupStyle.Button(collect, PopupStyle.ButtonColor.Violet, 320f);
            PopupStyle.PlaceTop(collect, -184f, 723f, 320f, PopupStyle.BUTTON_HEIGHT);
            TMP_Text collectText = collect.GetComponentInChildren<TMP_Text>(true);
            collectText.spriteAsset = null;
            PopupStyle.SetKey(collectText, "Common.Claim", "Claim");

            PopupStyle.Button(doubleButton, PopupStyle.ButtonColor.Pink, 320f);
            PopupStyle.PlaceTop(doubleButton, 184f, 723f, 320f, PopupStyle.BUTTON_HEIGHT);
            TMP_Text doubleText = doubleButton.GetComponentInChildren<TMP_Text>(true);
            PopupStyle.RemoveKey(doubleText);
            doubleText.text = "<sprite=0> x2";

            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static TMP_Text RewardBox(Transform box, string iconName, TMP_Text amount, TMP_Text perHour, float x, string amountName)
    {
        ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
        PopupStyle.PlaceTop(box, x, 308f + REWARD_BOX_HEIGHT / 2f, REWARD_BOX_WIDTH, REWARD_BOX_HEIGHT);
        Transform icon = box.Find(iconName);
        icon.GetComponent<Image>().preserveAspect = true;
        PopupStyle.Center(icon, -100f, 22f, 88f, 88f);

        Reparent(amount.transform, box);
        amount.name = amountName;
        RectTransform rt = amount.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(220f, 70f);
        rt.anchoredPosition = new Vector2(-46f, 22f);
        PopupStyle.Outline(amount, 56f, 32f);
        amount.alignment = TextAlignmentOptions.Left;
        amount.enableWordWrapping = false;
        amount.spriteAsset = null;
        amount.text = "110.2K";
        PopupStyle.RemoveKey(amount);

        PopupStyle.Center(perHour.transform, 0f, -50f, REWARD_BOX_WIDTH - 40f, 44f);
        PopupStyle.Plain(perHour, PopupStyle.BROWN, 30f, 22f);
        perHour.alignment = TextAlignmentOptions.Center;
        return amount;
    }
    #endregion

    #region Tìm node
    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        return null;
    }

    private static TMP_Text Text(Transform root, string path)
    {
        Transform t = Find(root, path);
        if (t == null) throw new System.Exception("[Batch1Reskin] thiếu node " + root.name + "/" + path);
        return t.GetComponent<TMP_Text>();
    }

    // Node có thể đã được dời khỏi `oldParent` ở lần chạy trước → tìm cả trong card.
    private static TMP_Text FindText(Transform card, string name, Transform oldParent)
    {
        if (oldParent != null && oldParent.Find(name) != null) return Text(oldParent, name);
        return Text(card, name);
    }

    private static void MoveChildren(Transform from, Transform to)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in from) children.Add(child);
        foreach (Transform child in children) child.SetParent(to, false);
    }
    #endregion
}
