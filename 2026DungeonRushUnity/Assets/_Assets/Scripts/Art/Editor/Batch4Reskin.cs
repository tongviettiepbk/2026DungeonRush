using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 4 – Enchantment (Relic), Wing (Kính), Cape (Tẩu) theo concept tools/concept/batch4/concept.png (kit chung PopupStyle).
// Sửa thẳng 10 popup Resources + 2 prefab ô con (CapeElementUI, SubstatUIElementPrefab), giữ GUID; chạy lại ra cùng kết quả.
// Toạ độ = đơn vị canvas (concept × 2), đo từ MÉP TRÊN panel tới TÂM (PopupStyle.PlaceTop).
public static class Batch4Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string CAPE_CELL = "Assets/_Assets/Prefabs/UINew/Cape/CapeElementUI.prefab";
    private const string SUBSTAT_ROW = "Assets/_Assets/Prefabs/UINew/Gear/SubstatUIElementPrefab.prefab";
    private const float MEDIUM = PopupStyle.WIDTH_MEDIUM;
    private const float INNER_MEDIUM = MEDIUM - 2f * PopupStyle.SIDE_INSET;                     // 708
    private const float INNER_SMALL = PopupStyle.WIDTH_SMALL - 2f * PopupStyle.SIDE_INSET;      // 572
    private const float INNER_LARGE = PopupStyle.WIDTH_LARGE - 2f * PopupStyle.SIDE_INSET;      // 860
    private const float LEFT_EDGE = -INNER_MEDIUM / 2f;                                         // -354
    private const float TOP = PopupStyle.CONTENT_TOP;                                           // 176
    private const float SLOT = 192f;
    private const float INFO_LEFT = LEFT_EDGE + SLOT + 28f;                                     // -134
    private const float INFO_WIDTH = -LEFT_EDGE - INFO_LEFT;                                    // 488
    private const float INV_CELL = 112f;

    [MenuItem("Tools/DungeonRush/Reskin/Batch 4 (Enchantment, Wing, Cape)")]
    public static string ApplyAll()
    {
        ApplyCapeCell();
        ApplySubstatRow();
        ApplyRelicInfo();
        ApplyRelicMerge();
        ApplyRelicSummon();
        ApplyRelicRates();
        ApplyWingCraft();
        ApplyWingReroll();
        ApplyCapePopup();
        ApplyCapeDetail();
        ApplyCapeSalvage();
        ApplyCapeRates();
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
        Debug.Log("[Batch4Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Ô con dùng chung
    // Ô tẩu vuông: nền rarity phủ ô, icon 72%, "Lvl N" góc dưới-trái, dải tối "Equipped" mép trên, tick xanh khi chọn.
    public static void ApplyCapeCell()
    {
        EditPrefab(CAPE_CELL, root =>
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(INV_CELL, INV_CELL);
            StyleCapeCell(root.GetComponent<CapeElementUI>());
        });
    }

    private static void StyleCapeCell(CapeElementUI cell)
    {
        Transform item = Find(cell.transform, "ItemElementUI");
        PopupStyle.Stretch(item);
        PopupStyle.Stretch(cell.imgBackground.transform);
        Icon72(cell.imgIcon);
        BottomLevel(cell.txtLevel, 26f);

        RectTransform equipped = (RectTransform)cell.objEquipped.transform;
        equipped.anchorMin = new Vector2(0f, 1f);
        equipped.anchorMax = Vector2.one;
        equipped.pivot = new Vector2(0.5f, 1f);
        equipped.sizeDelta = new Vector2(0f, 30f);
        equipped.anchoredPosition = Vector2.zero;
        Image strip = equipped.GetComponent<Image>();
        strip.sprite = null;
        strip.color = new Color(0f, 0f, 0f, 0.6f);
        TMP_Text equippedText = equipped.GetComponentInChildren<TMP_Text>(true);
        PopupStyle.Stretch(equippedText.transform);
        PopupStyle.Outline(equippedText, 20f, 14f);
        equippedText.alignment = TextAlignmentOptions.Center;
        equipped.SetAsLastSibling();

        if (cell.objCheck != null)
        {
            Image dim = cell.objCheck.GetComponent<Image>();
            PopupStyle.Stretch(cell.objCheck.transform);
            dim.sprite = null;
            dim.color = new Color(0f, 0f, 0f, 0.3f);
            Transform check = Find(cell.objCheck.transform, "CheckImage");
            ReskinPrefabUtil.Img(check, PopupStyle.Kit("ClanSearch/check_green"));
            check.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Center(check, 0f, 0f, 76f, 76f);
        }
    }

    // Dòng substat Reroll: ô khoá trái + ô nâu (khoá = panel tối) tên nâu trái, số viền phải.
    public static void ApplySubstatRow()
    {
        EditPrefab(SUBSTAT_ROW, root =>
        {
            ElementSubStatRerollUI row = root.GetComponent<ElementSubStatRerollUI>();
            ((RectTransform)root.transform).sizeDelta = new Vector2(INNER_MEDIUM, 84f);

            Transform lockBox = Find(root.transform, "SubStatCheckImg");
            ReskinPrefabUtil.Img(lockBox, PopupStyle.Kit("Mastery/box_tan"));   // input_tan trùng màu thân popup
            lockBox.GetComponent<Image>().color = Color.white;
            RectTransform lockRt = (RectTransform)lockBox;
            lockRt.anchorMin = lockRt.anchorMax = new Vector2(0f, 0.5f);
            lockRt.pivot = new Vector2(0f, 0.5f);
            lockRt.sizeDelta = new Vector2(76f, 76f);
            lockRt.anchoredPosition = Vector2.zero;
            row.imgLockIcon.preserveAspect = true;
            row.imgLockIcon.color = PopupStyle.BROWN;   // icon khoá trắng gốc không đọc được trên nền nâu nhạt
            PopupStyle.Center(row.imgLockIcon.transform, 0f, 0f, 44f, 44f);

            PopupStyle.Stretch(row.imgBackground.transform, 92f, 0f, 0f, 0f);
            row.imgBackground.type = Image.Type.Sliced;
            row.imgBackground.pixelsPerUnitMultiplier = 2f;   // panel tối 9-slice 45 cho dòng cao 84
            if (row.imgFlash != null) PopupStyle.Stretch(row.imgFlash.transform, 92f, 0f, 0f, 0f);
            row.spriteBgUnlocked = PopupStyle.Kit("Mastery/box_tan");
            row.spriteBgLocked = PopupStyle.Kit("Mastery/mastery_panel");
            row.colorTextUnlocked = PopupStyle.BROWN;
            row.colorTextLocked = new Color32(207, 205, 224, 255);
            ReskinPrefabUtil.Img(row.imgBackground.transform, row.spriteBgUnlocked);

            SideText(row.txtStat, true, 112f, 440f);
            PopupStyle.Plain(row.txtStat, PopupStyle.BROWN, 32f, 22f);
            PopupStyle.RemoveKey(row.txtStat);
            TMP_Text value = PopupStyle.GetOrCreateText(root.transform, "SubStatValueText");
            SideText(value, false, 20f, 160f);
            PopupStyle.Outline(value, 32f, 22f);
            value.text = "12.5%";
            row.txtValue = value;

            Find(root.transform, "SubStatCheckButton_1").SetAsLastSibling();   // cả dòng là nút khoá
        });
    }
    #endregion

    #region Relic info (vừa 816): header "Relic +N", thẻ ô tier + Relic Power, 3 nút màu chức năng
    private const float RELIC_BUTTON_CENTER = 471f;
    private const float RELIC_INFO_HEIGHT = 604f;

    public static void ApplyRelicInfo()
    {
        EditPrefab(UI + "UiEnchantmentInfoItem.prefab", root =>
        {
            UIEnchantmentElementInfo ui = root.GetComponent<UIEnchantmentElementInfo>();
            RectTransform panel = PanelInStretchContent(root.transform, "EnchantmentPreviewInfoPopup", MEDIUM, RELIC_INFO_HEIGHT);
            PopupStyle.Shell(panel, MEDIUM, RELIC_INFO_HEIGHT, panel, panel.Find("Header"));
            PopupStyle.Title(ui.txtTier, MEDIUM);
            PopupStyle.RemoveKey(ui.txtTier);
            Find(panel, "TitleText").gameObject.SetActive(false);   // "Enchantment" — header đã ghi "Relic +N"
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            Transform element = Find(panel, "EnchantmentElementUI");
            PopupStyle.PlaceTop(element, LEFT_EDGE + SLOT / 2f, TOP + SLOT / 2f, SLOT, SLOT);
            PopupStyle.Stretch(Find(element, "ItemElementUI"));
            PopupStyle.Stretch(ui.imgBg.transform);
            Icon72(ui.imgIcon);

            TMP_Text label = PopupStyle.GetOrCreateText(panel, "PowerLabel");
            PopupStyle.PlaceTopLeft(label.transform, INFO_LEFT, TOP + 30f, INFO_WIDTH, 48f);
            PopupStyle.Plain(label, PopupStyle.BROWN, 40f, 26f);
            label.alignment = TextAlignmentOptions.Left;
            PopupStyle.SetKey(label, "Popup.Enchantment.PowerLabel", "Relic Power");

            Reparent(ui.txtInfo.transform, panel);
            Find(panel, "DescriptonArea").gameObject.SetActive(false);
            PopupStyle.PlaceTopLeft(ui.txtInfo.transform, INFO_LEFT, TOP + 88f, INFO_WIDTH, 68f);
            PopupStyle.Outline(ui.txtInfo, 58f, 36f);
            ui.txtInfo.alignment = TextAlignmentOptions.Left;
            ui.txtInfo.enableWordWrapping = false;

            TMP_Text desc = PopupStyle.GetOrCreateText(panel, "PowerDesc");
            PopupStyle.PlaceTopLeft(desc.transform, INFO_LEFT, TOP + 158f, INFO_WIDTH, 72f);
            PopupStyle.Plain(desc, PopupStyle.MUTE, 28f, 20f);
            desc.alignment = TextAlignmentOptions.TopLeft;
            PopupStyle.SetKey(desc, "Popup.Enchantment.PowerDesc", "Boosts the main stat of the slot it is equipped in.");

            RelicButton(ui.btEquip, PopupStyle.ButtonColor.Green, -244f);
            RelicButton(ui.btMerge, PopupStyle.ButtonColor.Orange, 0f);
            RelicButton(ui.btDismantle, PopupStyle.ButtonColor.Red, 244f);
            PopupStyle.RemoveKey(ui.txtEquip);   // Equip/Unequip do script đặt
            TMP_Text dismantle = ui.btDismantle.GetComponentInChildren<TMP_Text>(true);
            PopupStyle.SetKey(dismantle, "Popup.Enchantment.Dismantle", "Dismantle");
            ui.sprEquip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprUnequip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.sprMerge = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprDismantle = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Red);
            ui.sprDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static void RelicButton(Button button, PopupStyle.ButtonColor color, float x)
    {
        PopupStyle.Button(button.transform, color, 220f, PopupStyle.BUTTON_HEIGHT, 44f);
        PopupStyle.PlaceTop(button.transform, x, RELIC_BUTTON_CENTER, 220f, PopupStyle.BUTTON_HEIGHT);
        Button(button);
    }
    #endregion

    #region Relic merge (vừa 816): 3 ô → ô kết quả trên panel tối, nút xanh, kho 5 cột
    private const float MERGE_HEIGHT = 1280f;

    public static void ApplyRelicMerge()
    {
        EditPrefab(UI + "UiEnchantmentMerge.prefab", root =>
        {
            UIEnchantmentMerge ui = root.GetComponent<UIEnchantmentMerge>();
            RectTransform outer = (RectTransform)Find(root.transform, "EnchantmentMergePopup");
            outer.sizeDelta = new Vector2(MEDIUM, MERGE_HEIGHT);
            RectTransform content = (RectTransform)Find(outer, "ContentTransform");
            PopupStyle.Center(content, 0f, 0f, MEDIUM, MERGE_HEIGHT);
            RectTransform panel = (RectTransform)Find(content, "NewItem");
            PopupStyle.Shell(panel, MEDIUM, MERGE_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.SetKey(title, "Popup.Enchantment.RelicMerge", "Relic Merge");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Panel tối: 3 ô nguyên liệu → mũi tên → ô kết quả.
            Transform area = Find(panel, "DescriptonArea");
            ReskinPrefabUtil.Img(area, PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(area, 0f, 306f, INNER_MEDIUM, 260f);
            Transform row = Find(area, "GameObject");
            PopupStyle.Center(row, 0f, 0f, 660f, 140f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.enabled = true;   // prefab cũ tắt HLG, đặt tay ô kết quả
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            foreach (ElementEnchantmentMergeUI slot in ui.listResourceNeed) RelicCell(slot, 128f);
            RelicCell(ui.mergeItemDone, 128f);
            Transform arrow = Find(row, "Arrow");
            ((RectTransform)arrow).sizeDelta = new Vector2(80f, 80f);
            arrow.GetComponent<Image>().preserveAspect = true;
            LayoutElement arrowLayout = GetOrAdd<LayoutElement>(arrow.gameObject);
            arrowLayout.ignoreLayout = false;   // prefab cũ đặt tay mũi tên → ô kết quả bị đẩy ra ngoài
            arrowLayout.preferredWidth = arrowLayout.minWidth = 80f;
            arrowLayout.preferredHeight = 80f;
            arrow.SetSiblingIndex(3);
            ui.mergeItemDone.transform.SetAsLastSibling();

            Reparent(ui.btMerge.transform, panel);
            PopupStyle.Button(ui.btMerge.transform, PopupStyle.ButtonColor.Green, 340f);
            PopupStyle.PlaceTop(ui.btMerge.transform, 0f, 539f, 340f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btMerge);
            ui.sprButtonOn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprButtonOff = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);

            SectionRow(panel, 664f, "Popup.Enchantment.Inventory", "Inventory", "Popup.Enchantment.TapToAdd", "Tap a relic to add");

            ScrollRect scroll = Find(panel, "Scroll View").GetComponent<ScrollRect>();
            InventoryGrid(scroll, 0f, 962f, INNER_MEDIUM, 520f, 5);
            RectTransform template = (RectTransform)ui.objPrefabElementInventory.transform;
            RelicCell(template.GetComponent<ElementEnchantmentMergeUI>(), INV_CELL);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Ô relic: nền tier phủ ô, icon 72%, "Lvl N" góc dưới-trái; tắt khoá/NEW/mũi tên cũ.
    private static void RelicCell(ElementEchantmentUI cell, float size)
    {
        ((RectTransform)cell.transform).sizeDelta = new Vector2(size, size);
        Transform item = cell.transform.Find("ItemElementUI");
        if (item != null) PopupStyle.Stretch(item);
        PopupStyle.Stretch(cell.imgBgTier.transform);
        Icon72(cell.imgIcon);
        foreach (string name in new[] { "LockImage", "NewTextObj", "DownArrow" })
        {
            Transform t = FindOrNull(cell.transform, name);
            if (t != null) t.gameObject.SetActive(false);
        }
        if (cell.textLvl != null) BottomLevel(cell.textLvl, size >= 128f ? 28f : 24f);
    }
    #endregion

    #region Kết quả summon relic (toàn màn): khuôn màn kết quả Đợt 3
    public static void ApplyRelicSummon()
    {
        EditPrefab(UI + "UiSumonEnchantment.prefab", root =>
        {
            UISumonEnchantment ui = root.GetComponent<UISumonEnchantment>();
            Transform panel = Find(root.transform, "ContentAll");
            ResultScreen(panel, 0f, Find(panel, "Scroll View").GetComponent<ScrollRect>(), Text(panel, "Text (TMP)"));
            RectTransform cell = (RectTransform)ui.objPrefabElement.transform;
            ElementEchantmentUI element = cell.GetComponent<ElementEchantmentUI>();
            PopupStyle.Stretch(element.imgBgTier.transform);
            Icon72(element.imgIcon);
        });
    }
    #endregion

    #region Tier Chances relic (vừa 816): khuôn Summon Rates Đợt 3
    public static void ApplyRelicRates()
    {
        EditPrefab(UI + "uiUpgradeEnchantment.prefab", root =>
        {
            UIUpgradeEnchantment ui = root.GetComponent<UIUpgradeEnchantment>();
            RectTransform panel = (RectTransform)Find(root.transform, "NewItem");
            float height = RateTable(panel, Find(panel, "VerticalLayout"), ui.txtLevelCurrent, ui.txtLevelNext, ui.listRarity);
            PopupStyle.Shell(panel, MEDIUM, height, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.SetKey(title, "Popup.Enchantment.TierRatesTitle", "Tier Chances");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);
            foreach (ElementRarityPet row in ui.listRarity) PopupStyle.RemoveKey(row.txtName);   // "Relic +N" do script đặt
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Bảng 6 dòng tỉ lệ (2 cột) bắt đầu ở CONTENT_TOP. Trả về chiều cao popup.
    private static float RateTable(Transform panel, Transform table, TMP_Text current, TMP_Text next, System.Collections.Generic.List<ElementRarityPet> rows)
    {
        const int count = 6;
        const float header = 40f, rowHeight = 60f, spacing = 10f;
        float tableHeight = header + count * (rowHeight + spacing) - spacing;
        PopupStyle.PlaceTop(table, 0f, TOP + tableHeight / 2f, INNER_MEDIUM, tableHeight);
        VerticalLayoutGroup layout = table.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Transform head = Find(table, "Header");
        PopupStyle.Invisible(head.GetComponent<Image>());
        SetHeight(head, header);
        TMP_Text tier = Text(head, "TierText");
        SideText(tier, true, 16f, 300f);
        PopupStyle.Plain(tier, PopupStyle.BROWN, 32f, 22f);
        SideText(current, false, 170f, 150f);
        SideText(next, false, 16f, 150f);
        PopupStyle.Plain(current, PopupStyle.BROWN, 32f, 22f);
        PopupStyle.Plain(next, PopupStyle.BROWN, 32f, 22f);

        for (int i = 0; i < rows.Count; i++)
        {
            SetHeight(rows[i].transform, rowHeight);
            if (i >= count) continue;
            Image image = ReskinPrefabUtil.Img(rows[i].transform, PopupStyle.Kit("RarityTable/bar_" + ((Rarity)i).ToString().ToLower()));
            image.color = Color.white;
            SideText(rows[i].txtName, true, 18f, 320f);
            PopupStyle.Outline(rows[i].txtName, 30f, 22f);
            SideText(rows[i].txtRateCurret, false, 170f, 150f);
            SideText(rows[i].txtRateNext, false, 16f, 150f);
            PopupStyle.Outline(rows[i].txtRateCurret, 30f, 22f);
            PopupStyle.Outline(rows[i].txtRateNext, 30f, 22f);
        }
        return TOP + tableHeight + PopupStyle.BOTTOM_INSET;
    }
    #endregion

    #region Chế kính (nhỏ 680): ô lớn + chi phí quặng, Cancel tím / Craft xanh
    private const float CRAFT_HEIGHT = 752f;

    public static void ApplyWingCraft()
    {
        EditPrefab(UI + "UIWingCraftPopup.prefab", root =>
        {
            UIWingCraftPopup ui = root.GetComponent<UIWingCraftPopup>();
            RectTransform content = (RectTransform)Find(root.transform, "ContentTransform");
            PopupStyle.Center(content, 0f, 0f, PopupStyle.WIDTH_SMALL, CRAFT_HEIGHT);
            RectTransform panel = (RectTransform)Find(content, "Background");
            Transform header = Find(panel, "Header");
            PopupStyle.Shell(panel, PopupStyle.WIDTH_SMALL, CRAFT_HEIGHT, panel, header);
            TMP_Text title = Text(header, "Text_1");
            PopupStyle.Title(title, PopupStyle.WIDTH_SMALL);

            Transform item = ui.imgBackground.transform;
            PopupStyle.PlaceTop(item, 0f, TOP + 110f, 220f, 220f);
            PopupStyle.Stretch(ui.imgWingIcon.transform, 30f, 30f, 30f, 30f);
            ui.imgWingIcon.preserveAspect = true;

            Transform cost = Find(panel, "ResourceArea");
            ReskinPrefabUtil.Img(cost, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(cost, 0f, 470f, 300f, 92f);
            PopupStyle.Stretch(ui.txtResource.transform, 12f, 0f, 12f, 0f);
            PopupStyle.Outline(ui.txtResource, 42f, 28f);
            ui.txtResource.alignment = TextAlignmentOptions.Center;
            ui.txtResource.enableWordWrapping = false;

            PopupStyle.Button(ui.btCancel.transform, PopupStyle.ButtonColor.Violet, 272f);
            PopupStyle.PlaceTop(ui.btCancel.transform, -148f, 619f, 272f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btCancel);
            PopupStyle.Button(ui.btCraft.transform, PopupStyle.ButtonColor.Green, 272f);
            PopupStyle.PlaceTop(ui.btCraft.transform, 148f, 619f, 272f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btCraft);
            ui.sprButtonOn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprButtonOff = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
        });
    }
    #endregion

    #region Reroll kính (vừa 816): thẻ kính + tối đa 5 dòng substat có khoá + nút xanh kèm quặng
    private const float REROLL_HEIGHT = 1166f;

    public static void ApplyWingReroll()
    {
        EditPrefab(UI + "UIWingRerollPopup.prefab", root =>
        {
            UIWingRerollPopup ui = root.GetComponent<UIWingRerollPopup>();
            RectTransform outer = (RectTransform)Find(root.transform, "WingRerollPopup");
            outer.sizeDelta = new Vector2(MEDIUM, REROLL_HEIGHT);
            RectTransform content = (RectTransform)Find(outer, "ContentTransform");
            PopupStyle.Center(content, 0f, 0f, MEDIUM, REROLL_HEIGHT);
            RectTransform panel = (RectTransform)Find(content, "Bg");
            PopupStyle.Shell(panel, MEDIUM, REROLL_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            Reparent(title.transform, panel);
            Reparent(ui.btClose.transform, panel);
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Thẻ kính: ô 192 + Lv góc, [rarity] tên màu, cột nhãn nâu / số viền.
            Transform item = Find(panel, "ItemElementUI");
            Reparent(item, panel);
            PopupStyle.PlaceTop(item, LEFT_EDGE + SLOT / 2f, TOP + SLOT / 2f, SLOT, SLOT);
            PopupStyle.Stretch(ui.imgItemBackground.transform);
            Icon72(ui.imgItemIcon);
            CornerLevel(ui.txtItemLevel, 34f);
            ui.txtItemLevel.transform.SetAsLastSibling();
            if (ui.objEquipped != null)
            {
                TMP_Text equipped = ui.objEquipped.GetComponent<TMP_Text>();
                RectTransform eq = equipped.rectTransform;
                eq.anchorMin = new Vector2(0f, 1f);
                eq.anchorMax = Vector2.one;
                eq.pivot = new Vector2(0.5f, 1f);
                eq.sizeDelta = new Vector2(0f, 36f);
                eq.anchoredPosition = new Vector2(0f, -4f);
                PopupStyle.Outline(equipped, 24f, 16f);
                equipped.alignment = TextAlignmentOptions.Center;
            }

            Reparent(ui.txtItemName.transform, panel);
            PopupStyle.PlaceTopLeft(ui.txtItemName.transform, INFO_LEFT, TOP + 30f, INFO_WIDTH, 56f);
            PopupStyle.Outline(ui.txtItemName, 40f, 26f);
            ui.txtItemName.alignment = TextAlignmentOptions.Left;
            ui.txtItemName.enableWordWrapping = false;

            TMP_Text labels = PopupStyle.GetOrCreateText(panel, "BaseStatLabels");
            PopupStyle.PlaceTopLeft(labels.transform, INFO_LEFT, TOP + 124f, 300f, 108f);
            PopupStyle.Plain(labels, PopupStyle.BROWN, 42f);
            labels.alignment = TextAlignmentOptions.Left;
            labels.enableWordWrapping = false;
            labels.text = "Health\nDamage";
            ui.txtItemBaseLabels = labels;
            Reparent(ui.txtItemBaseStat.transform, panel);
            PopupStyle.PlaceTopLeft(ui.txtItemBaseStat.transform, INFO_LEFT + 160f, TOP + 124f, 300f, 108f);
            PopupStyle.Outline(ui.txtItemBaseStat, 42f);
            ui.txtItemBaseStat.alignment = TextAlignmentOptions.Left;
            ui.txtItemBaseStat.enableWordWrapping = false;
            Find(panel, "WingDetailArea").gameObject.SetActive(false);

            // Hàng "Lock" + gợi ý mờ.
            TMP_Text lockText = Text(panel, "LockText");
            PopupStyle.PlaceTopLeft(lockText.transform, LEFT_EDGE, 416f, 220f, 40f);
            PopupStyle.Plain(lockText, PopupStyle.BROWN, 30f, 22f);
            lockText.alignment = TextAlignmentOptions.Left;
            TMP_Text hint = PopupStyle.GetOrCreateText(panel, "LockHint");
            PopupStyle.PlaceTop(hint.transform, -LEFT_EDGE - 230f, 416f, 460f, 40f);
            PopupStyle.Plain(hint, PopupStyle.MUTE, 26f, 18f);
            hint.alignment = TextAlignmentOptions.Right;
            PopupStyle.SetKey(hint, "Popup.WingReroll.LockedHint", "Locked stats stay after reroll");

            // Danh sách substat (tối đa 5 dòng × 84, cách 12).
            RectTransform list = (RectTransform)ui.transSubStatContent;
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = new Vector2(INNER_MEDIUM, 0f);
            list.anchoredPosition = new Vector2(0f, -444f);
            VerticalLayoutGroup layout = list.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Nút Reroll xanh: chữ trên, chi phí quặng dưới (bỏ nền ô tối).
            Transform bottom = Find(panel, "BottomArea");
            Reparent(ui.btReroll.transform, panel);
            bottom.gameObject.SetActive(false);
            PopupStyle.Button(ui.btReroll.transform, PopupStyle.ButtonColor.Green, 400f, 168f, 46f);
            PopupStyle.PlaceTop(ui.btReroll.transform, 0f, 1024f, 400f, 168f);
            Button(ui.btReroll);
            TMP_Text rerollText = Text(ui.btReroll.transform, "RerollText");
            PopupStyle.Center(rerollText.transform, 0f, 30f, 360f, 60f);
            PopupStyle.Outline(rerollText, 46f, 30f);
            rerollText.alignment = TextAlignmentOptions.Center;
            Transform cost = Find(ui.btReroll.transform, "ResourceArea");
            PopupStyle.Invisible(cost.GetComponent<Image>());
            PopupStyle.Center(cost, 0f, -26f, 320f, 50f);
            PopupStyle.Stretch(ui.txtRerollResource.transform);
            PopupStyle.Outline(ui.txtRerollResource, 34f, 24f);
            ui.txtRerollResource.alignment = TextAlignmentOptions.Center;
            ui.txtRerollResource.enableWordWrapping = false;
            ui.sprButtonOn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprButtonOff = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.btClose.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Trang Pipes (lớn 968): chip crown + công tắc, panel tối summon, kho 6 cột; ClaimPage + bảng kết quả
    private const float CAPE_HEIGHT = 1226f;
    private const float SUMMON_TOP = 280f;

    public static void ApplyCapePopup()
    {
        EditPrefab(UI + "UICapePopup.prefab", root =>
        {
            UICapePopup ui = root.GetComponent<UICapePopup>();
            RectTransform outer = (RectTransform)Find(root.transform, "CapePopup");
            outer.sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, 0f);
            RectTransform transform = (RectTransform)Find(outer, "ContentTransform");
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = new Vector2(0f, CAPE_HEIGHT);
            RectTransform panel = (RectTransform)Find(transform, "Content");
            Transform frame = Find(panel, "Background");
            Transform header = Find(panel, "Header");
            Reparent(header, panel);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_LARGE, CAPE_HEIGHT, frame, header);
            PopupStyle.Title(Text(panel, "HeaderText"), PopupStyle.WIDTH_LARGE);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);
            float left = -INNER_LARGE / 2f;

            // Hàng 1: chip crown trái, "Show Pipe" + công tắc kit Setting phải.
            Transform currency = Find(panel, "CurrencyArea");
            ReskinPrefabUtil.Img(currency, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTopLeft(currency, left, TOP + 40f, 240f, 80f);
            PopupStyle.Stretch(ui.txtCloak.transform, 10f, 0f, 10f, 0f);
            PopupStyle.Outline(ui.txtCloak, 40f, 26f);
            ui.txtCloak.alignment = TextAlignmentOptions.Center;
            Find(currency, "CrownIcon").gameObject.SetActive(false);

            Transform toggleArea = ui.objShowCloakRoot.transform;
            PopupStyle.PlaceTop(toggleArea, -left - 190f, TOP + 40f, 380f, 80f);
            TMP_Text toggleLabel = Text(toggleArea, "InfoText");
            PopupStyle.Center(toggleLabel.transform, -60f, 0f, 240f, 50f);
            PopupStyle.Plain(toggleLabel, PopupStyle.BROWN, 34f, 22f);
            toggleLabel.alignment = TextAlignmentOptions.Right;
            Transform toggle = ui.btShowCloak.transform;
            ReskinPrefabUtil.Img(toggle, PopupStyle.Kit("Setting/track_dark"));
            PopupStyle.Center(toggle, 130f, 0f, 120f, 56f);
            Button(ui.btShowCloak);
            Image fill = PopupStyle.NewImage(toggle, "Fill", PopupStyle.Kit("Setting/fill_green"));
            PopupStyle.Stretch(fill.transform, 4f, 4f, 30f, 4f);
            fill.transform.SetSiblingIndex(0);
            ui.objShowCloakFill = fill.gameObject;
            ui.sprKnobOn = PopupStyle.Kit("Setting/knob_on");
            ui.sprKnobOff = PopupStyle.Kit("Setting/knob_off");
            ui.knobOffset = 32f;
            ReskinPrefabUtil.Img(ui.imgShowCloak.transform, ui.sprKnobOn);
            ui.imgShowCloak.color = Color.white;
            ui.imgShowCloak.preserveAspect = true;
            PopupStyle.Center(ui.imgShowCloak.transform, 32f, 0f, 68f, 68f);
            ui.imgShowCloak.transform.SetAsLastSibling();
            ui.txtShowCloak.gameObject.SetActive(false);

            // Panel tối khu summon: Lvl + thanh + (i), nút Summon xanh + hệ số tím.
            Image summonPanel = PopupStyle.NewImage(panel, "SummonPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(summonPanel.transform, 0f, SUMMON_TOP + 150f, INNER_LARGE, 300f);
            summonPanel.transform.SetSiblingIndex(2);
            PopupStyle.PlaceTopLeft(ui.txtSummonLevel.transform, left + 40f, SUMMON_TOP + 64f, 130f, 56f);
            PopupStyle.Outline(ui.txtSummonLevel, 40f, 26f);
            ui.txtSummonLevel.alignment = TextAlignmentOptions.Left;
            ui.txtSummonLevel.enableWordWrapping = false;
            Bar(Find(panel, "ScrollProgressBar"), ui.imgSummonFill, ui.txtSummonProgress, 20f, SUMMON_TOP + 64f, 560f);
            PopupStyle.Button(ui.btInfo.transform, PopupStyle.ButtonColor.Blue, 68f, 68f, 40f);
            PopupStyle.PlaceTop(ui.btInfo.transform, -left - 40f - 34f, SUMMON_TOP + 64f, 68f, 68f);
            Button(ui.btInfo);

            PopupStyle.Button(ui.btSummon.transform, PopupStyle.ButtonColor.Green, 340f, 168f, 40f);
            PopupStyle.PlaceTop(ui.btSummon.transform, -80f, SUMMON_TOP + 196f, 340f, 168f);
            Button(ui.btSummon);
            if (ui.objSummonNotification != null)
            {
                RectTransform dot = (RectTransform)ui.objSummonNotification.transform;
                Reparent(dot, ui.btSummon.transform);
                dot.anchorMin = dot.anchorMax = Vector2.one;
                dot.pivot = new Vector2(0.5f, 0.5f);
                dot.sizeDelta = new Vector2(56f, 56f);
                dot.anchoredPosition = new Vector2(-6f, -6f);
                Transform dotImage = Find(dot, "Image");
                ReskinPrefabUtil.Img(dotImage, PopupStyle.Kit("BattlePass/icon_alert"));
                PopupStyle.Stretch(dotImage);
                dot.SetAsLastSibling();
            }
            PopupStyle.Button(ui.btMultiplier.transform, PopupStyle.ButtonColor.Violet, 160f, 128f, 44f);
            PopupStyle.PlaceTop(ui.btMultiplier.transform, 190f, SUMMON_TOP + 196f, 160f, 128f);
            Button(ui.btMultiplier);
            ui.sprButtonOn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprButtonOff = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);

            // Kho tẩu: lưới 6 cột ô 112 cuộn.
            ScrollRect inventory = Find(panel, "Inventory").GetComponent<ScrollRect>();
            InventoryGrid(inventory, 0f, 888f, INNER_LARGE, 560f, 6);
            Transform blur = FindOrNull(panel, "Blur");
            if (blur != null) blur.gameObject.SetActive(false);

            ApplyCapeClaim(ui);
            ApplyCapeSummonPanel(ui);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // ClaimPage (summon 1, toàn màn; toạ độ theo tâm popup = tâm màn): ruy băng tiêu đề, hào quang + tẩu, tên, panel tối chỉ số, Claim xanh.
    private static void ApplyCapeClaim(UICapePopup ui)
    {
        Transform page = ui.objClaimPage.transform;
        Transform headerText = Find(page, "HeaderText");
        Image ribbon = PopupStyle.NewImage(page, "TitleRibbon", PopupStyle.Kit("Gameplay/ribbon_offer"));
        PopupStyle.Center(ribbon.transform, 0f, 780f, 840f, 160f);
        Reparent(headerText, ribbon.transform);
        TMP_Text title = headerText.GetComponent<TMP_Text>();
        PopupStyle.Center(title.transform, 0f, 12f, 720f, 90f);
        PopupStyle.Outline(title, 56f, 34f);
        title.alignment = TextAlignmentOptions.Center;
        PopupStyle.SetKey(title, "Popup.Cape.NewPipe", "New Pipe Obtained!");

        if (ui.imgClaimGlow != null)
        {
            ReskinPrefabUtil.Img(ui.imgClaimGlow.transform, PopupStyle.Art("7_FX/Glow_Circle02"));
            PopupStyle.Center(ui.imgClaimGlow.transform, 0f, 200f, 840f, 840f);
        }
        ui.imgClaimIcon.preserveAspect = true;
        PopupStyle.Center(ui.imgClaimIcon.transform, 0f, 200f, 440f, 440f);

        PopupStyle.Center(ui.txtClaimName.transform, 0f, -168f, 960f, 70f);
        PopupStyle.Outline(ui.txtClaimName, 52f, 32f);
        ui.txtClaimName.alignment = TextAlignmentOptions.Center;
        ui.txtClaimName.enableWordWrapping = false;

        Image stats = PopupStyle.NewImage(page, "StatsPanel", PopupStyle.Kit("Mastery/mastery_panel"));
        PopupStyle.Center(stats.transform, 0f, -400f, 760f, 320f);
        Reparent(ui.transClaimStats, stats.transform);
        stats.transform.SetSiblingIndex(Find(page, "Background").GetSiblingIndex() + 1);   // trên nền tối toàn màn
        PopupStyle.Stretch(ui.transClaimStats, 30f, 30f, 30f, 30f);
        VerticalLayoutGroup layout = ui.transClaimStats.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        PopupStyle.Button(ui.btClaim.transform, PopupStyle.ButtonColor.Green, 400f);
        PopupStyle.Center(ui.btClaim.transform, 0f, -754f, 400f, PopupStyle.BUTTON_HEIGHT);
        Button(ui.btClaim);
    }

    // Bảng kết quả summon nhiều: phủ toàn màn, khuôn màn kết quả Đợt 3.
    private static void ApplyCapeSummonPanel(UICapePopup ui)
    {
        Transform panel = ui.objSummonPanel.transform;
        PopupStyle.Center(panel, 0f, 0f, 1080f, 2160f);
        PopupStyle.Stretch(Find(panel, "Background"), -200f, -200f, -200f, -200f);
        ScrollRect scroll = Find(panel, "Scroll View").GetComponent<ScrollRect>();
        TMP_Text tap = PopupStyle.GetOrCreateText(panel, "TapToCloseText");
        ResultScreen(panel, 0f, scroll, tap);
    }
    #endregion

    #region Pipe Info (vừa 816): khuôn Item Info, substat cố định 2 dòng, Equip xanh / Unequip tím, Upgrade cam
    private const float DETAIL_HEIGHT = 752f;

    public static void ApplyCapeDetail()
    {
        EditPrefab(UI + "UICapeDetailPopup.prefab", root =>
        {
            UICapeDetailPopup ui = root.GetComponent<UICapeDetailPopup>();
            RectTransform panel = PanelInStretchContent(root.transform, "CapeDetailPopup", MEDIUM, DETAIL_HEIGHT);
            PopupStyle.Shell(panel, MEDIUM, DETAIL_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = PopupStyle.GetOrCreateText(panel, "TitleText");
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.SetKey(title, "Popup.CapeDetail.Title", "Pipe Info");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            CapeCard(panel, ui.elementCape, ui.txtRarity, ui.txtName, out TMP_Text labels, ui.txtDamage, ui.txtHealth);
            ui.txtStatLabels = labels;

            Image box = PopupStyle.NewImage(panel, "SubStatBox", PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box.transform, 0f, 456f, INNER_MEDIUM, 120f);
            Reparent(ui.txtSubStats.transform, box.transform);
            PopupStyle.Stretch(ui.txtSubStats.transform, 22f, 12f, 200f, 12f);
            PopupStyle.Plain(ui.txtSubStats, PopupStyle.BROWN, 32f, 22f);
            ui.txtSubStats.alignment = TextAlignmentOptions.TopLeft;
            ui.txtSubStats.gameObject.SetActive(true);
            TMP_Text values = PopupStyle.GetOrCreateText(box.transform, "SubValueText");
            PopupStyle.Stretch(values.transform, 400f, 12f, 22f, 12f);
            PopupStyle.Outline(values, 32f, 22f);
            values.alignment = TextAlignmentOptions.TopRight;
            ui.txtSubValues = values;

            PopupStyle.Button(ui.btEquip.transform, PopupStyle.ButtonColor.Green, 300f);
            PopupStyle.PlaceTop(ui.btEquip.transform, -164f, 619f, 300f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btEquip);
            PopupStyle.RemoveKey(ui.txtEquip);
            PopupStyle.Button(ui.btUpgrade.transform, PopupStyle.ButtonColor.Orange, 300f);
            PopupStyle.PlaceTop(ui.btUpgrade.transform, 164f, 619f, 300f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btUpgrade);
            ui.sprEquip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprUnequip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Thẻ tẩu: ô 192 trái; phải = [rarity] (có thể null), tên, cột nhãn nâu Damage/Health + 2 ô số viền.
    private static void CapeCard(Transform panel, CapeElementUI cell, TMP_Text rarity, TMP_Text name, out TMP_Text labels, TMP_Text damage, TMP_Text health)
    {
        Reparent(cell.transform, panel);
        PopupStyle.PlaceTop(cell.transform, LEFT_EDGE + SLOT / 2f, TOP + SLOT / 2f, SLOT, SLOT);
        StyleCapeCell(cell);
        BottomLevel(cell.txtLevel, 32f);
        cell.txtLevel.enableWordWrapping = false;

        float nameTop = TOP + 30f;
        if (rarity != null)
        {
            Reparent(rarity.transform, panel);
            PopupStyle.PlaceTopLeft(rarity.transform, INFO_LEFT, TOP + 20f, INFO_WIDTH, 40f);
            PopupStyle.Outline(rarity, 32f, 22f);
            rarity.alignment = TextAlignmentOptions.Left;
            nameTop = TOP + 64f;
        }
        Reparent(name.transform, panel);
        PopupStyle.PlaceTopLeft(name.transform, INFO_LEFT, nameTop, INFO_WIDTH, 52f);
        PopupStyle.Outline(name, rarity != null ? 44f : 38f, 24f);
        name.alignment = TextAlignmentOptions.Left;
        name.enableWordWrapping = false;
        PopupStyle.RemoveKey(name);

        float statsTop = rarity != null ? TOP + 150f : TOP + 124f;
        labels = PopupStyle.GetOrCreateText(panel, "StatLabels");
        PopupStyle.PlaceTopLeft(labels.transform, INFO_LEFT, statsTop, 300f, 104f);
        PopupStyle.Plain(labels, PopupStyle.BROWN, 40f);
        labels.alignment = TextAlignmentOptions.Left;
        labels.enableWordWrapping = false;
        labels.lineSpacing = 0f;
        labels.text = "Damage\nHealth";
        foreach (TMP_Text value in new[] { damage, health })
        {
            Reparent(value.transform, panel);
            PopupStyle.PlaceTopLeft(value.transform, INFO_LEFT + 170f, statsTop + (value == damage ? -26f : 26f), 340f, 52f);
            PopupStyle.Outline(value, 40f, 26f);
            value.alignment = TextAlignmentOptions.Left;
            value.enableWordWrapping = false;
        }
    }
    #endregion

    #region Pipe Upgrade (vừa 816): thẻ tẩu + phần tăng xanh, thanh XP, chọn nguyên liệu (tick xanh)
    private const float SALVAGE_HEIGHT = 1183f;

    public static void ApplyCapeSalvage()
    {
        EditPrefab(UI + "UICapeSalvagePopup.prefab", root =>
        {
            UICapeSalvagePopup ui = root.GetComponent<UICapeSalvagePopup>();
            RectTransform panel = PanelInStretchContent(root.transform, "CapeSalvagePopup", MEDIUM, SALVAGE_HEIGHT);
            PopupStyle.Shell(panel, MEDIUM, SALVAGE_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.SetKey(title, "Popup.CapeSalvage.Title", "Pipe Upgrade");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Tên tẩu nằm trong thẻ (tiêu đề giữ cố định "Pipe Upgrade").
            TMP_Text name = ui.txtName != title ? ui.txtName : PopupStyle.GetOrCreateText(panel, "CapeNameText");
            ui.txtName = name;
            Transform area = Find(panel, "DescriptonArea");
            CapeCard(panel, ui.elementCape, null, name, out TMP_Text labels, ui.txtDamage, ui.txtHealth);
            ui.txtStatLabels = labels;

            Transform xpBar = Find(panel, "ScrollProgressBar");
            Reparent(xpBar, panel);
            Bar(xpBar, ui.imgXPFill, ui.txtXP, 0f, 422f, INNER_MEDIUM);

            TMP_Text info = Text(panel, "InfoText");
            Reparent(info.transform, panel);
            PopupStyle.PlaceTopLeft(info.transform, LEFT_EDGE, 526f, 440f, 60f);
            PopupStyle.Plain(info, PopupStyle.BROWN, 34f, 22f);
            info.alignment = TextAlignmentOptions.Left;
            PopupStyle.SetKey(info, "Popup.CapeSalvage.Select", "Select pipes to combine");
            Reparent(ui.btUpgrade.transform, panel);
            PopupStyle.Button(ui.btUpgrade.transform, PopupStyle.ButtonColor.Green, 260f, 110f, 40f);
            PopupStyle.PlaceTop(ui.btUpgrade.transform, -LEFT_EDGE - 130f, 526f, 260f, 110f);
            Button(ui.btUpgrade);
            ui.sprButtonOn = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.sprButtonOff = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            area.gameObject.SetActive(false);

            ScrollRect scroll = Find(panel, "Scroll View").GetComponent<ScrollRect>();
            InventoryGrid(scroll, 0f, 865f, INNER_MEDIUM, 520f, 5);
            ui.btClose.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Summon Rates tẩu (vừa 816): Level N giữa 2 mũi tên art, 6 dòng 1 cột %, thanh tiến độ
    private const float CAPE_RATES_HEIGHT = 840f;

    public static void ApplyCapeRates()
    {
        EditPrefab(UI + "UICapeUpgradeInfoPopup.prefab", root =>
        {
            UICapeUpgradeInfoPopup ui = root.GetComponent<UICapeUpgradeInfoPopup>();
            RectTransform panel = PanelInStretchContent(root.transform, "CapeUpgradeInfoPopup", MEDIUM, CAPE_RATES_HEIGHT);
            PopupStyle.Shell(panel, MEDIUM, CAPE_RATES_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, MEDIUM);
            PopupStyle.SetKey(title, "Popup.CapeUpgradeInfo.Info", "Summon Rates");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            Transform mid = Find(panel, "MidArea");
            PopupStyle.PlaceTop(mid, 0f, TOP + 44f, INNER_MEDIUM, 100f);
            PopupStyle.Center(ui.txtLevel.transform, 0f, 0f, 380f, 72f);
            PopupStyle.Outline(ui.txtLevel, 56f, 36f);
            ui.txtLevel.alignment = TextAlignmentOptions.Center;
            Find(mid, "InfoText").gameObject.SetActive(false);   // tiêu đề header đã ghi "Summon Rates"
            Arrow(ui.btLeft, "Wing/arrow_left", -240f);
            Arrow(ui.btRight, "Wing/arrow_right", 240f);

            Transform table = Find(panel, "VerticalLayout");
            const float rowHeight = 60f, spacing = 10f;
            float tableHeight = 6 * (rowHeight + spacing) - spacing;
            PopupStyle.PlaceTop(table, 0f, 292f + tableHeight / 2f, INNER_MEDIUM, tableHeight);
            VerticalLayoutGroup layout = table.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            for (int i = 0; i < ui.listTxtRate.Count; i++)
            {
                Transform row = ui.listTxtRate[i].transform.parent;
                SetHeight(row, rowHeight);
                Image image = ReskinPrefabUtil.Img(row, PopupStyle.Kit("RarityTable/bar_" + ((Rarity)i).ToString().ToLower()));
                image.color = Color.white;
                TMP_Text name = Text(row, "RarityText");
                SideText(name, true, 18f, 400f);
                PopupStyle.Outline(name, 30f, 22f);
                SideText(ui.listTxtRate[i], false, 18f, 200f);
                PopupStyle.Outline(ui.listTxtRate[i], 30f, 22f);
            }

            Bar(ui.objProgress.transform, ui.imgFill, ui.txtProgress, 0f, 292f + tableHeight + 28f + 26f, INNER_MEDIUM);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static void Arrow(Button button, string sprite, float x)
    {
        ReskinPrefabUtil.Img(button.transform, PopupStyle.Kit(sprite));
        button.image.color = Color.white;
        button.image.preserveAspect = true;
        PopupStyle.Center(button.transform, x, 0f, 80f, 99f);
        Button(button);
    }
    #endregion

    #region Helper bố cục
    // Khung popup nằm trong "<outer>/ContentTransform/NewItem" (outer giãn dọc, ContentTransform giãn ngang): đặt kích thước, trả về NewItem.
    private static RectTransform PanelInStretchContent(Transform root, string outerName, float width, float height)
    {
        RectTransform outer = (RectTransform)Find(root, outerName);
        outer.sizeDelta = new Vector2(width, outer.anchorMin.y == outer.anchorMax.y ? height : 0f);
        RectTransform content = (RectTransform)Find(outer, "ContentTransform");
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(content.anchorMin.x == content.anchorMax.x ? width : 0f, height);
        return (RectTransform)Find(content, "NewItem");
    }

    // Màn kết quả toàn màn (khuôn Đợt 3): ruy băng "Rewards", lưới 5 cột ô 150 cuộn, "Tap anywhere to close".
    private static void ResultScreen(Transform panel, float x, ScrollRect scroll, TMP_Text tapClose)
    {
        Image ribbon = PopupStyle.NewImage(panel, "TitleRibbon", PopupStyle.Kit("Gameplay/ribbon_offer"));
        PopupStyle.PlaceTop(ribbon.transform, x, 280f, 680f, 160f);
        TMP_Text title = PopupStyle.GetOrCreateText(ribbon.transform, "Title");
        PopupStyle.Center(title.transform, 0f, 12f, 560f, 90f);
        PopupStyle.Outline(title, 60f, 36f);
        title.alignment = TextAlignmentOptions.Center;
        PopupStyle.SetKey(title, "Popup.Level.RewardsTitle", "Rewards");

        PopupStyle.PlaceTop(scroll.transform, x, 1120f, 1000f, 1440f);
        Transform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform.Find("Viewport");
        PopupStyle.Stretch(viewport);
        viewport.GetComponent<Image>().sprite = null;   // UIMask bo mờ mép cắt viền ô
        GridLayoutGroup grid = scroll.content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150f, 150f);
        grid.spacing = new Vector2(28f, 28f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(0, 0, 24, 24);
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(scroll.content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        PopupStyle.PlaceTop(tapClose.transform, x, 1950f, 1000f, 60f);
        PopupStyle.Outline(tapClose, 34f, 24f);
        tapClose.fontStyle = FontStyles.Normal;
        tapClose.alignment = TextAlignmentOptions.Center;
        PopupStyle.SetKey(tapClose, "Common.TapToClose", "Tap anywhere to close");
        ribbon.transform.SetAsLastSibling();
    }

    // Kho cuộn trong ô nâu: lưới ô 112 cách 16, mask vuông (không sprite) lề trong 8, thanh cuộn ẩn nếu có.
    private static void InventoryGrid(ScrollRect scroll, float x, float topToCenter, float width, float height, int columns)
    {
        ReskinPrefabUtil.Img(scroll.transform, PopupStyle.Kit("Mastery/box_tan"));
        PopupStyle.PlaceTop(scroll.transform, x, topToCenter, width, height);
        Transform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform.Find("Viewport");
        PopupStyle.Stretch(viewport, 8f, 8f, 8f, 8f);
        viewport.GetComponent<Image>().sprite = null;
        GridLayoutGroup grid = scroll.content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(INV_CELL, INV_CELL);
        grid.spacing = new Vector2(16f, 16f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.padding = new RectOffset(0, 0, 16, 16);
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(scroll.content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Hàng tiêu đề khu: nhãn nâu trái + gợi ý mờ phải.
    private static void SectionRow(Transform panel, float topToCenter, string key, string text, string hintKey, string hintText)
    {
        TMP_Text label = PopupStyle.GetOrCreateText(panel, "SectionLabel");
        PopupStyle.PlaceTopLeft(label.transform, LEFT_EDGE, topToCenter, 300f, 44f);
        PopupStyle.Plain(label, PopupStyle.BROWN, 36f, 24f);
        label.alignment = TextAlignmentOptions.Left;
        PopupStyle.SetKey(label, key, text);
        TMP_Text hint = PopupStyle.GetOrCreateText(panel, "SectionHint");
        PopupStyle.PlaceTop(hint.transform, -LEFT_EDGE - 210f, topToCenter, 420f, 44f);
        PopupStyle.Plain(hint, PopupStyle.MUTE, 28f, 20f);
        hint.alignment = TextAlignmentOptions.Right;
        PopupStyle.SetKey(hint, hintKey, hintText);
    }

    // Thanh tiến độ kit: track xp_track viền đen, fill Filled thụt đều 4, chữ viền giữa.
    private static void Bar(Transform bar, Image fill, TMP_Text text, float x, float topToCenter, float width)
    {
        PopupStyle.PlaceTop(bar, x, topToCenter, width, 52f);
        Transform track = Find(bar, "FillBg_1");
        ReskinPrefabUtil.Img(track, PopupStyle.Kit("Lobby/xp_track"));
        PopupStyle.Stretch(track);
        Transform old = FindOrNull(bar, "FillBg");
        if (old != null) old.gameObject.SetActive(false);
        ReskinPrefabUtil.Img(fill.transform, PopupStyle.Kit("Lobby/xp_fill"));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = Color.white;
        PopupStyle.Stretch(fill.transform, 4f, 4f, 4f, 4f);
        PopupStyle.Stretch(text.transform);
        PopupStyle.Outline(text, 30f, 20f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.transform.SetAsLastSibling();
    }

    private static void Icon72(Image icon)
    {
        RectTransform rt = icon.rectTransform;
        rt.anchorMin = new Vector2(0.14f, 0.14f);
        rt.anchorMax = new Vector2(0.86f, 0.86f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        icon.preserveAspect = true;
    }

    // "Lvl N" chữ viền góc dưới-trái ô.
    private static void CornerLevel(TMP_Text text, float size)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(220f, size + 10f);
        rt.anchoredPosition = new Vector2(8f, 2f);
        rt.localScale = Vector3.one;
        PopupStyle.Outline(text, size, size * 0.7f);
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.enableWordWrapping = false;
        text.transform.SetAsLastSibling();
    }

    // "Lvl N" chữ viền căn giữa đáy ô (ô relic + ô tẩu — user chốt 2026-11-12).
    private static void BottomLevel(TMP_Text text, float size)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, size + 10f);
        rt.anchoredPosition = new Vector2(0f, 2f);
        rt.localScale = Vector3.one;
        PopupStyle.Outline(text, size, size * 0.7f);
        text.alignment = TextAlignmentOptions.Bottom;
        text.enableWordWrapping = false;
        text.transform.SetAsLastSibling();
    }

    private static void SideText(TMP_Text text, bool left, float inset, float width)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
        rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rt.sizeDelta = new Vector2(width, 50f);
        rt.anchoredPosition = new Vector2(left ? inset : -inset, 0f);
        rt.localScale = Vector3.one;
        text.alignment = left ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        text.enableWordWrapping = false;
    }

    // Kit: trạng thái nút bằng sprite, không tô màu khi tắt (PressButtonUI lo hiệu ứng nhấn).
    private static void Button(Button button)
    {
        if (button != null) button.transition = Selectable.Transition.None;
    }

    private static void SetHeight(Transform t, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void Reparent(Transform t, Transform parent)
    {
        if (t.parent != parent) t.SetParent(parent, false);
    }

    private static Transform FindOrNull(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        return null;
    }

    // Tìm theo đường dẫn, không có thì tìm theo tên trong cả cây (node có thể đã bị dời ở lần chạy trước).
    private static Transform Find(Transform root, string path)
    {
        Transform t = FindOrNull(root, path);
        if (t == null) throw new System.Exception("[Batch4Reskin] thiếu node " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path)
    {
        return Find(root, path).GetComponent<TMP_Text>();
    }
    #endregion
}
