using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 5 – Dungeon & Mining theo concept tools/concept/batch5/concept.png (kit chung PopupStyle).
// Sửa thẳng 4 popup Resources, giữ GUID; chạy lại ra cùng kết quả. KHÔNG gọi MiningPrefabBuilder (Build Mining/All).
// Toạ độ = đơn vị canvas (concept × 2), đo từ MÉP TRÊN panel tới TÂM (PopupStyle.PlaceTop).
public static class Batch5Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const float MEDIUM = PopupStyle.WIDTH_MEDIUM;
    private const float SMALL = PopupStyle.WIDTH_SMALL;
    private const float LARGE = PopupStyle.WIDTH_LARGE;
    private const float INNER_MEDIUM = MEDIUM - 2f * PopupStyle.SIDE_INSET;   // 708
    private const float TOP = PopupStyle.CONTENT_TOP;                          // 176

    [MenuItem("Tools/DungeonRush/Reskin/Batch 5 (Dungeon, Mining)")]
    public static string ApplyAll()
    {
        ApplyDungeon();
        ApplyMiningRarity();
        ApplyMiningOffer();
        ApplyMining();
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
        Debug.Log("[Batch5Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Dungeon (vừa 816): độ khó, Thưởng + Keys cạnh nhau, dòng mô tả, Sweep tím / Enter cam
    private const float DUNGEON_HEIGHT = 880f;

    public static void ApplyDungeon()
    {
        EditPrefab(UI + "UIDungeonPopup.prefab", root =>
        {
            UIDungeonPopupStart ui = root.GetComponent<UIDungeonPopupStart>();
            Transform content = Find(root.transform, "ContentTransform");
            PopupStyle.Center(content, 0f, 0f, MEDIUM, DUNGEON_HEIGHT);
            RectTransform panel = (RectTransform)Find(content, "Background");
            PopupStyle.Shell(panel, MEDIUM, DUNGEON_HEIGHT, panel, panel.Find("Header"));
            PopupStyle.Title(ui.txtTitle, MEDIUM);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Ruy băng đỏ + icon thưởng ở góc trái header (giữ từ reskin Dungeon trước, user chốt).
            Transform ribbon = Find(panel, "Ribbon");
            PopupStyle.PlaceTop(ribbon, -334f, 65f, 152f, 106f);
            ribbon.SetSiblingIndex(panel.Find("Header").GetSiblingIndex() + 1);

            PopupStyle.PlaceTop(ui.txtDifficulty.transform, 0f, 200f, 600f, 50f);
            PopupStyle.Plain(ui.txtDifficulty, PopupStyle.BROWN, 38f, 26f);
            ui.txtDifficulty.alignment = TextAlignmentOptions.Center;
            PopupStyle.PlaceTop(ui.txtLvl.transform, 0f, 284f, 560f, 130f);
            PopupStyle.Outline(ui.txtLvl, 112f, 60f);
            ui.txtLvl.alignment = TextAlignmentOptions.Center;

            // 2 ô Thưởng / Keys có nhãn nâu phía trên (nhãn là con của ô, tâm cao hơn tâm ô 90).
            Transform reward = Find(panel, "RewardParent");
            StatBox(reward, ui.imgIconReward.transform, ui.txtQuantityReward, -180f);
            LabelAbove(Text(reward, "RewardInfoText"));
            Transform keys = Find(panel, "KeyParent");
            StatBox(keys, ui.imgIconKey.transform, ui.txtQuantityKeyProcess, 180f);
            TMP_Text keysLabel = PopupStyle.GetOrCreateText(keys, "KeysLabel");
            LabelAbove(keysLabel);
            PopupStyle.SetKey(keysLabel, "Popup.Dungeon.Keys", "Keys");

            TMP_Text desc = PopupStyle.GetOrCreateText(panel, "DescriptionText");
            PopupStyle.PlaceTop(desc.transform, 0f, 603f, 660f, 80f);
            PopupStyle.Plain(desc, PopupStyle.MUTE, 30f, 22f);
            desc.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(desc, "UI.Dungeon.Description", "Keys are only consumed when you complete the dungeon.");

            // Nút: Sweep tím, Enter cam, Ads hồng (chung chỗ Enter); khoá = xám do script đổi sprite.
            DungeonButton(ui.btSweepLast, PopupStyle.ButtonColor.Violet, -166f, 44f);
            DungeonButton(ui.btEnter, PopupStyle.ButtonColor.Orange, 166f, 52f);
            DungeonButton(ui.btEnterByAds, PopupStyle.ButtonColor.Pink, 166f, 52f);
            Transform adIcon = Find(ui.btEnterByAds.transform, "Image");
            adIcon.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Center(adIcon, -62f, 6f, 80f, 58f);
            RectTransform adText = ui.txtQuantityProcessAds.rectTransform;
            adText.sizeDelta = new Vector2(150f, 100f);
            adText.anchoredPosition = new Vector2(36f, 6f);
            ui.sprSweep = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.sprEnter = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Ô box_tan 328×120: icon 72 trái, số viền canh trái ngay sau icon (cả cụm gần giữa ô).
    private static void StatBox(Transform box, Transform icon, TMP_Text amount, float x)
    {
        ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
        PopupStyle.PlaceTop(box, x, 484f, 328f, 120f);
        icon.GetComponent<Image>().preserveAspect = true;
        PopupStyle.Center(icon, -60f, 0f, 72f, 72f);
        RectTransform rt = amount.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 80f);
        rt.anchoredPosition = new Vector2(-14f, 0f);
        rt.localScale = Vector3.one;
        PopupStyle.Outline(amount, 56f, 32f);
        amount.alignment = TextAlignmentOptions.Left;
        amount.enableWordWrapping = false;
    }

    private static void LabelAbove(TMP_Text label)
    {
        PopupStyle.Center(label.transform, 0f, 90f, 328f, 50f);
        PopupStyle.Plain(label, PopupStyle.BROWN, 34f, 24f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
    }

    private static void DungeonButton(Button button, PopupStyle.ButtonColor color, float x, float fontSize)
    {
        PopupStyle.Button(button.transform, color, 300f, PopupStyle.BUTTON_HEIGHT, fontSize);
        PopupStyle.PlaceTop(button.transform, x, 745f, 300f, PopupStyle.BUTTON_HEIGHT);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.fontSizeMin = 28f;
        text.lineSpacing = -20f;
        Button(button);
    }
    #endregion

    #region Mining Rarity (nhỏ 680): 7 dòng ô nâu icon + tên nâu + 2 cột % viền
    private const int ORE_ROWS = 7;
    private const float RATE_HEADER = 40f, RATE_ROW = 72f, RATE_SPACING = 10f;

    public static void ApplyMiningRarity()
    {
        EditPrefab(UI + "UIMiningRarityPopup.prefab", root =>
        {
            UIMiningRarityPopup ui = root.GetComponent<UIMiningRarityPopup>();
            ((RectTransform)Find(root.transform, "MiningRarityPopup")).sizeDelta = new Vector2(SMALL, 0f);
            RectTransform content = (RectTransform)Find(root.transform, "ContentTransform");
            content.anchoredPosition = Vector2.zero;

            RectTransform panel = (RectTransform)Find(content, "NewItem");
            Transform table = Find(panel, "VerticalLayout");
            float tableHeight = RATE_HEADER + ORE_ROWS * (RATE_ROW + RATE_SPACING) - RATE_SPACING;
            float height = TOP + tableHeight + PopupStyle.BOTTOM_INSET;
            SetHeight(content, height);
            PopupStyle.Shell(panel, SMALL, height, panel, panel.Find("Header"));
            PopupStyle.Title(Text(panel, "TitleText"), SMALL);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            PopupStyle.PlaceTop(table, 0f, TOP + tableHeight / 2f, SMALL - 2f * PopupStyle.SIDE_INSET, tableHeight);
            VerticalLayoutGroup layout = table.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = RATE_SPACING;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform head = Find(table, "Header");
            PopupStyle.Invisible(head.GetComponent<Image>());
            SetHeight(head, RATE_HEADER);
            TMP_Text ore = Text(head, "TierText");
            SideText(ore, true, 20f, 260f);
            PopupStyle.Plain(ore, PopupStyle.BROWN, 32f, 22f);
            RateColumns(ui.txtCurrentLevel, ui.txtNextLevel);
            PopupStyle.Plain(ui.txtCurrentLevel, PopupStyle.BROWN, 32f, 22f);
            PopupStyle.Plain(ui.txtNextLevel, PopupStyle.BROWN, 32f, 22f);

            for (int i = 0; i < ui.listCurrentRate.Count; i++)
            {
                Transform row = ui.listCurrentRate[i].transform.parent;
                SetHeight(row, RATE_ROW);
                Image bg = ReskinPrefabUtil.Img(row, PopupStyle.Kit("Mastery/box_tan"));
                bg.enabled = true;
                bg.raycastTarget = false;
                Transform icon = Find(row, "Image");
                icon.GetComponent<Image>().preserveAspect = true;
                RectTransform iconRt = (RectTransform)icon;
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.sizeDelta = new Vector2(56f, 56f);
                iconRt.anchoredPosition = new Vector2(48f, 0f);
                TMP_Text name = Text(row, "RarityText");
                SideText(name, true, 84f, 220f);
                PopupStyle.Plain(name, PopupStyle.BROWN, 32f, 22f);
                RateColumns(ui.listCurrentRate[i], ui.listNextRate[i]);
                PopupStyle.Outline(ui.listCurrentRate[i], 32f, 22f);
                PopupStyle.Outline(ui.listNextRate[i], 32f, 22f);
            }
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static void RateColumns(TMP_Text current, TMP_Text next)
    {
        SideText(current, false, 168f, 140f);
        SideText(next, false, 20f, 140f);
    }
    #endregion

    #region Mining Offer (vừa 816): khuôn ưu đãi Đợt 2, panel trưng bày + 4 ô quà + dòng +cuốc tối đa + nút xanh
    private const float OFFER_HEIGHT = 1022f;
    private const float OFFER_CONTENT_TOP = 106f;

    public static void ApplyMiningOffer()
    {
        EditPrefab(UI + "UIMiningOfferPopup.prefab", root =>
        {
            UIMiningOfferPopup ui = root.GetComponent<UIMiningOfferPopup>();
            ((RectTransform)Find(root.transform, "Mining_Bundle_PopupOffer")).sizeDelta = new Vector2(MEDIUM, 0f);
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            Batch2Reskin.OfferShell(panel, MEDIUM, OFFER_HEIGHT, panel.Find("Background"), Find(panel, "Ribbon"), ui.txtTitle);
            PopupStyle.RemoveKey(ui.txtTitle);   // gắn nhầm key UI.Mining.MaxPickaxeIncrease; tiêu đề do script đặt

            // Panel trưng bày tối: hào quang + khoan / cuốc vàng / cuốc.
            Image show = PopupStyle.NewImage(panel, "ShowcasePanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(show.transform, 0f, OFFER_CONTENT_TOP + 170f, INNER_MEDIUM, 340f);
            show.transform.SetSiblingIndex(1);
            Image glow = PopupStyle.NewImage(show.transform, "Glow", PopupStyle.Art("7_FX/Glow_Circle02"));
            glow.color = new Color(1f, 1f, 1f, 0.55f);
            PopupStyle.Center(glow.transform, 0f, 0f, 500f, 500f);
            ShowTool(show.transform, "Drill", "5_Maps/Mining/drill_character", -146f, 160f, 12f);
            ShowTool(show.transform, "Pickaxe", "5_Maps/Mining/pickaxe_character", 146f, 160f, -12f);
            ShowTool(show.transform, "GoldenPickaxe", "5_Maps/Mining/gold_pickaxe_character", 0f, 220f, 0f);

            // Hàng 4 ô quà (HLG có sẵn).
            Transform row = Find(panel, "Content");
            Image rowImage = row.GetComponent<Image>();
            if (rowImage != null) rowImage.enabled = false;
            PopupStyle.PlaceTop(row, 0f, 558f, INNER_MEDIUM, 168f);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 0);
            GiftCell(Find(row, "Drill"), ui.txtDrill);
            GiftCell(Find(row, "Golden"), ui.txtGoldenPickaxe);
            GiftCell(Find(row, "Pickaxe"), ui.txtPickaxe);
            GiftCell(Find(row, "Gem"), ui.txtGem);

            // "+N cuốc tối đa" trên panel tối; ẩn cả panel khi gói không tăng (objMaxPickaxe).
            Image maxPanel = PopupStyle.NewImage(panel, "MaxPickaxePanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(maxPanel.transform, 0f, 728f, INNER_MEDIUM, 116f);
            Transform maxIcon = Find(panel, "Max+");
            Reparent(maxIcon, maxPanel.transform);
            maxIcon.GetComponent<Image>().enabled = false;
            PopupStyle.Center(maxIcon, -270f, 0f, 80f, 80f);
            Transform maxIconImage = Find(maxIcon, "Icon");
            maxIconImage.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Stretch(maxIconImage);
            Reparent(ui.txtMaxPickaxe.transform, maxPanel.transform);
            RectTransform maxText = ui.txtMaxPickaxe.rectTransform;
            maxText.anchorMin = maxText.anchorMax = new Vector2(0.5f, 0.5f);
            maxText.pivot = new Vector2(0f, 0.5f);
            maxText.sizeDelta = new Vector2(524f, 100f);
            maxText.anchoredPosition = new Vector2(-214f, 0f);
            maxText.localScale = Vector3.one;
            PopupStyle.Plain(ui.txtMaxPickaxe, Color.white, 34f, 22f);
            ui.txtMaxPickaxe.alignment = TextAlignmentOptions.Left;
            ui.objMaxPickaxe = maxPanel.gameObject;

            PopupStyle.Button(ui.btBuy.transform, PopupStyle.ButtonColor.Green, 380f);
            PopupStyle.PlaceTop(ui.btBuy.transform, 0f, 889f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btBuy);
            Transform badge = ui.btBuy.transform.Find("DiscountLabel");
            if (badge != null)
            {
                PopupStyle.Center(badge, 184f, 61f, 132f, 132f);
                badge.localRotation = Quaternion.Euler(0f, 0f, -10f);
                badge.SetAsLastSibling();
            }
            if (ui.imgLoading != null)
            {
                PopupStyle.PlaceTop(ui.imgLoading.transform, 0f, 889f, 100f, 100f);
                ui.imgLoading.transform.SetAsLastSibling();
            }

            Batch2Reskin.TapToClose(PopupStyle.GetOrCreateText(panel, "TapToClose"));
        });
    }

    private static void ShowTool(Transform show, string name, string sprite, float x, float size, float rotation)
    {
        Image image = PopupStyle.NewImage(show, name, PopupStyle.Art(sprite));
        image.preserveAspect = true;
        PopupStyle.Center(image.transform, x, 0f, size, size);
        image.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    // Ô quà box_tan 152×168: icon trên, "xN" viền dưới.
    private static void GiftCell(Transform cell, TMP_Text amount)
    {
        ReskinPrefabUtil.Img(cell, PopupStyle.Kit("Mastery/box_tan"));
        ((RectTransform)cell).sizeDelta = new Vector2(152f, 168f);
        Transform icon = Find(cell, "Icon");
        icon.GetComponent<Image>().preserveAspect = true;
        PopupStyle.Center(icon, 0f, 16f, 96f, 84f);
        PopupStyle.Center(amount.transform, 0f, -46f, 148f, 44f);
        PopupStyle.Outline(amount, 34f, 22f);
        amount.alignment = TextAlignmentOptions.Center;
        amount.enableWordWrapping = false;
    }
    #endregion

    #region Mining (lớn 968): Rarity / Lvl + thanh kit / Offer, 7 ô quặng, khung mỏ, 3 nút dụng cụ theo trạng thái
    private const float MINING_HEIGHT = 1718f;
    private const float ORE_CELL_WIDTH = 205f, ORE_CELL_HEIGHT = 80f, ORE_SPACING = 16f;

    public static void ApplyMining()
    {
        EditPrefab(UI + "UIMiningPopup.prefab", root =>
        {
            UIMiningPopup ui = root.GetComponent<UIMiningPopup>();
            ((RectTransform)Find(root.transform, "MiningPopup")).sizeDelta = new Vector2(LARGE, 0f);
            RectTransform panel = (RectTransform)Find(root.transform, "Content");
            PopupStyle.Shell(panel, LARGE, MINING_HEIGHT, panel, panel.Find("Header"));
            PopupStyle.Title(Text(panel, "NicknameText"), LARGE);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Hàng trên: nút Rarity (xám) / Lvl + thanh tiến độ kit / nút Offer (vàng) — giữ art Mining.
            PopupStyle.PlaceTop(ui.btRarity.transform, -358f, 250f, 152f, 157f);
            PopupStyle.PlaceTop(ui.btOffer.transform, 358f, 250f, 152f, 157f);
            DepthBar(ui);

            // 7 ô quặng: lưới 4 cột ô nâu kit, icon 72 trái + số viền.
            Transform grid = Find(panel, "ResourcesGridLayout");
            PopupStyle.PlaceTop(grid, 0f, 440f, 4f * ORE_CELL_WIDTH + 3f * ORE_SPACING, 2f * ORE_CELL_HEIGHT + ORE_SPACING);
            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(ORE_CELL_WIDTH, ORE_CELL_HEIGHT);
            layout.spacing = new Vector2(ORE_SPACING, ORE_SPACING);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            TMP_Text[] ores = { ui.txtStone, ui.txtCoal, ui.txtIron, ui.txtEmerald, ui.txtGold, ui.txtDiamond, ui.txtRuby };
            foreach (TMP_Text ore in ores) OreCell(ore);

            PopupStyle.PlaceTop(Find(panel, "MineWorldFrame"), 0f, 987f, 870f, 870f);

            // 3 nút dụng cụ: đang chọn cam / còn hàng tím / hết còn ads hồng / hết cả ads xám (script đổi sprite).
            ui.sprToolSelected = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.sprToolPassive = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.sprToolAd = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Pink);
            ui.sprToolEmpty = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ToolButton(ui.btGoldenPickaxe, ui.sprToolAd, -249f, 166f,
                ui.txtGoldenPickaxeCount, ui.txtGoldenPickaxeAdCount, ui.txtGoldenPickaxeDailyLimit);
            ToolButton(ui.btPickaxe, ui.sprToolSelected, 0f, 276f,
                ui.txtPickaxeCount, ui.txtPickaxeAdCount, ui.txtPickaxeDailyLimit);
            ToolButton(ui.btDrill, ui.sprToolPassive, 249f, 166f,
                ui.txtDrillCount, ui.txtDrillAdCount, ui.txtDrillDailyLimit);

            PopupStyle.PlaceTop(ui.txtPickaxeTimer.transform, 0f, 1646f, 800f, 50f);
            PopupStyle.Plain(ui.txtPickaxeTimer, PopupStyle.BROWN, 36f, 24f);
            ui.txtPickaxeTimer.alignment = TextAlignmentOptions.Center;
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Thanh độ sâu = thanh tiến độ kit (track xp_track 52, fill xp_fill thụt đều 4), "Lvl N" phía trên.
    private static void DepthBar(UIMiningPopup ui)
    {
        Transform bar = ui.sliderDepth.transform;
        PopupStyle.PlaceTop(bar, 0f, 284f, 500f, 52f);
        Transform track = Find(bar, "DurationRectTransform_1");
        ReskinPrefabUtil.Img(track, PopupStyle.Kit("Lobby/xp_track"));
        PopupStyle.Stretch(track);
        Image fill = Find(bar, "DurationRectTransform").GetComponent<Image>();
        ReskinPrefabUtil.Img(fill.transform, PopupStyle.Kit("Lobby/xp_fill"));
        // Slider tự đặt anchor fill theo value → chỉ chỉnh lề 4 mỗi phía.
        RectTransform fillRt = fill.rectTransform;
        fillRt.sizeDelta = new Vector2(-8f, -8f);
        fillRt.anchoredPosition = new Vector2(4f, 0f);
        foreach (string old in new[] { "Image", "DurationRectTransform_2" })
        {
            Transform t = bar.Find(old);
            if (t != null) t.gameObject.SetActive(false);
        }

        PopupStyle.Stretch(ui.txtDepthSlider.transform);
        PopupStyle.Outline(ui.txtDepthSlider, 30f, 20f);
        ui.txtDepthSlider.alignment = TextAlignmentOptions.Center;
        ui.txtDepthSlider.enableWordWrapping = false;
        ui.txtDepthSlider.transform.SetAsLastSibling();
        PopupStyle.Center(ui.txtMineLevel.transform, 0f, 66f, 300f, 56f);
        PopupStyle.Outline(ui.txtMineLevel, 44f, 30f);
        ui.txtMineLevel.alignment = TextAlignmentOptions.Center;
    }

    private static void OreCell(TMP_Text count)
    {
        Transform cell = count.transform.parent;
        foreach (Transform child in cell)
        {
            Image image = child.GetComponent<Image>();
            if (image == null) continue;
            RectTransform rt = (RectTransform)child;
            if (image.sprite != null && image.sprite.name == "ore_slot" || rt.anchorMax == Vector2.one && child.GetSiblingIndex() == 0)
            {
                ReskinPrefabUtil.Img(child, PopupStyle.Kit("Mastery/box_tan"));
                PopupStyle.Stretch(child);
            }
            else
            {
                image.preserveAspect = true;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(72f, 72f);
                rt.anchoredPosition = new Vector2(32f, 0f);
            }
        }
        PopupStyle.Stretch(count.transform, 80f, 0f, 8f, 0f);
        PopupStyle.Outline(count, 34f, 22f);
        count.alignment = TextAlignmentOptions.Left;
        count.enableWordWrapping = false;
    }

    private static void ToolButton(Button button, Sprite sprite, float x, float width, TMP_Text count, TMP_Text adCount, TMP_Text limit)
    {
        ReskinPrefabUtil.Img(button.transform, sprite);
        PopupStyle.PlaceTop(button.transform, x, 1523f, width, 154f);
        Button(button);
        Transform content = Find(button.transform, "Content");
        PopupStyle.Stretch(content);
        foreach (Transform child in content)
        {
            Image image = child.GetComponent<Image>();
            if (image == null) continue;
            image.preserveAspect = true;
            PopupStyle.Center(child, 0f, 18f, 80f, 80f);
        }
        ToolText(count, 0f, -40f, width - 20f, 34f);
        ToolText(limit, 0f, -40f, width - 20f, 32f);
        ToolText(adCount, width / 2f - 36f, 50f, 64f, 32f);
        PopupStyle.Center(count.transform.parent, 0f, 0f, width, 154f);
        PopupStyle.Center(limit.transform.parent, 0f, 0f, width, 154f);
    }

    private static void ToolText(TMP_Text text, float x, float y, float width, float size)
    {
        PopupStyle.Center(text.transform, x, y, width, 44f);
        PopupStyle.Outline(text, size, size * 0.7f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
    }
    #endregion

    #region Helper
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

    private static void Reparent(Transform t, Transform parent)
    {
        if (t.parent != parent) t.SetParent(parent, false);
    }

    // Tìm theo đường dẫn, không có thì tìm theo tên trong cả cây (node có thể đã bị dời ở lần chạy trước).
    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        throw new System.Exception("[Batch5Reskin] thiếu node " + root.name + "/" + path);
    }

    private static TMP_Text Text(Transform root, string path)
    {
        return Find(root, path).GetComponent<TMP_Text>();
    }
    #endregion
}
