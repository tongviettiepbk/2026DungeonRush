using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 3 – Pet & Mastery theo concept tools/concept/batch3/concept.png (kit chung PopupStyle).
// Sửa thẳng 4 popup Resources + MateryPopupUpgrade trong UIMainGame2 (giữ GUID), chạy lại nhiều lần ra cùng kết quả.
// Toạ độ = đơn vị canvas (concept × 2), đo từ MÉP TRÊN panel tới TÂM (PopupStyle.PlaceTop).
public static class Batch3Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string MAIN_GAME = "Assets/_Assets/Prefabs/UI/UIMainGame2.prefab";
    private const float INNER_MEDIUM = PopupStyle.WIDTH_MEDIUM - 2f * PopupStyle.SIDE_INSET;   // 708
    private const float INNER_SMALL = PopupStyle.WIDTH_SMALL - 2f * PopupStyle.SIDE_INSET;     // 572
    private const float LEFT_EDGE = -INNER_MEDIUM / 2f;                                        // -354

    [MenuItem("Tools/DungeonRush/Reskin/Batch 3 (Pet & Mastery)")]
    public static string ApplyAll()
    {
        ApplyPetInfo();
        ApplySummonRates();
        ApplyAdSummon();
        ApplySummonResult();
        ApplyMasteryUpgrade();
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
        Debug.Log("[Batch3Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Pet info (vừa 816): thẻ pet + mô tả cố định 4 dòng + 2 nút / chữ mờ
    private const float SLOT = 192f;
    private const float CARD_TOP = PopupStyle.CONTENT_TOP;               // 176
    private const float BAR_CENTER = CARD_TOP + SLOT + 12f + 24f;        // 404
    private const float DESC_CENTER = 566f;                              // ô mô tả 456..676
    private const float DESC_HEIGHT = 220f;
    private const float PET_BUTTON_CENTER = 779f;
    private const float PET_INFO_HEIGHT = 912f;

    public static void ApplyPetInfo()
    {
        EditPrefab(UI + "UIPetInfo.prefab", root =>
        {
            UIPetInfo ui = root.GetComponent<UIPetInfo>();
            RectTransform panel = (RectTransform)Find(root.transform, "NewItem");
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, PET_INFO_HEIGHT, panel, panel.Find("Header"));
            PopupStyle.Title(ui.txtName, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Chạm nền tối để đóng (khung popup chặn raycast nên bấm trong khung không đóng).
            Image dim = Find(root.transform, "CompanionInfoPopup").GetComponent<Image>();
            dim.raycastTarget = true;
            ui.btBackground = GetOrAdd<Button>(dim.gameObject);
            ui.btBackground.targetGraphic = dim;
            Button(ui.btBackground);

            // Cột trái: ô rarity 192 + Lv góc dưới-trái, thanh thẻ ngay dưới.
            Transform element = Find(panel, "CompanionPageElementUI_1");
            PopupStyle.PlaceTop(element, LEFT_EDGE + SLOT / 2f, CARD_TOP + 126f, SLOT, 252f);
            Transform slot = Find(element, "ItemElementUI");
            PopupStyle.PlaceTop(slot, 0f, SLOT / 2f, SLOT, SLOT);
            PopupStyle.Stretch(ui.imgBackground.transform);
            ui.imgBackground.color = Color.white;
            RectTransform icon = ui.imgIcon.rectTransform;
            icon.anchorMin = new Vector2(0.14f, 0.14f);
            icon.anchorMax = new Vector2(0.86f, 0.86f);
            icon.sizeDelta = Vector2.zero;
            icon.anchoredPosition = Vector2.zero;
            ui.imgIcon.preserveAspect = true;
            PopupStyle.Center(ui.imgLock.transform, 0f, 0f, 80f, 80f);
            ui.imgLock.preserveAspect = true;
            RectTransform lv = ui.textLv.rectTransform;
            lv.anchorMin = lv.anchorMax = lv.pivot = Vector2.zero;
            lv.sizeDelta = new Vector2(176f, 44f);
            lv.anchoredPosition = new Vector2(10f, 4f);
            PopupStyle.Outline(ui.textLv, 34f, 24f);
            ui.textLv.alignment = TextAlignmentOptions.BottomLeft;
            ui.textLv.enableWordWrapping = false;
            ui.textLv.transform.SetAsLastSibling();

            Transform bar = Find(element, "FillParent");
            PopupStyle.PlaceTop(bar, 0f, BAR_CENTER - CARD_TOP, SLOT, 48f);
            Transform track = Find(bar, "FillBg_1");
            ReskinPrefabUtil.Img(track, PopupStyle.Kit("Lobby/xp_track"));
            PopupStyle.Stretch(track);
            Find(bar, "FillBg").gameObject.SetActive(false);
            Image fill = ui.imgProcess;
            ReskinPrefabUtil.Img(fill.transform, PopupStyle.Kit("Lobby/xp_fill"));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = Color.white;
            PopupStyle.Stretch(fill.transform, 4f, 4f, 4f, 4f);
            PopupStyle.Stretch(ui.textProcess.transform);
            PopupStyle.Outline(ui.textProcess, 28f, 20f);
            ui.textProcess.alignment = TextAlignmentOptions.Center;
            ui.textProcess.enableWordWrapping = false;
            ui.textProcess.transform.SetAsLastSibling();

            // Cột phải: rarity màu (script tô) + "Own effect" mờ + nhãn nâu / số viền.
            float infoLeft = LEFT_EDGE + SLOT + 28f;
            float infoWidth = -LEFT_EDGE - infoLeft;
            PopupStyle.PlaceTopLeft(ui.txtRarity.transform, infoLeft, CARD_TOP + 22f, infoWidth, 44f);
            PopupStyle.Outline(ui.txtRarity, 38f, 26f);
            ui.txtRarity.alignment = TextAlignmentOptions.Left;
            ui.txtRarity.enableWordWrapping = false;
            PopupStyle.RemoveKey(ui.txtRarity);

            TMP_Text ownTitle = PopupStyle.GetOrCreateText(panel, "OwnEffectTitle");
            PopupStyle.PlaceTopLeft(ownTitle.transform, infoLeft, CARD_TOP + 74f, infoWidth, 36f);
            PopupStyle.Plain(ownTitle, PopupStyle.MUTE, 30f, 22f);
            ownTitle.alignment = TextAlignmentOptions.Left;
            PopupStyle.SetKey(ownTitle, "Popup.Companion.OwnEffect", "Own effect");

            TMP_Text ownLabels = PopupStyle.GetOrCreateText(panel, "OwnEffectLabels");
            PopupStyle.PlaceTopLeft(ownLabels.transform, infoLeft, CARD_TOP + 146f, infoWidth, 104f);
            PopupStyle.Plain(ownLabels, PopupStyle.BROWN, 42f);
            ownLabels.alignment = TextAlignmentOptions.Left;
            ownLabels.enableWordWrapping = false;
            ownLabels.text = "Damage\nHealth";
            ui.txtOwnLabels = ownLabels;

            Transform ownArea = Find(panel, "OwnEffectArea");
            Reparent(ui.txtOwnEffect.transform, panel);
            ownArea.gameObject.SetActive(false);
            PopupStyle.PlaceTopLeft(ui.txtOwnEffect.transform, infoLeft + 170f, CARD_TOP + 146f, 300f, 104f);
            PopupStyle.Outline(ui.txtOwnEffect, 42f);
            ui.txtOwnEffect.alignment = TextAlignmentOptions.Left;
            ui.txtOwnEffect.enableWordWrapping = false;

            // Mô tả: ô nâu cao cố định 4 dòng (DE dài nhất 126 ký tự), chữ nâu, số xanh đậm (script tô).
            Transform descArea = Find(panel, "DescriptonArea");
            ReskinPrefabUtil.Img(descArea, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(descArea, 0f, DESC_CENTER, INNER_MEDIUM, DESC_HEIGHT);
            PopupStyle.Stretch(ui.txtDescription.transform, 22f, 14f, 22f, 14f);
            PopupStyle.Plain(ui.txtDescription, PopupStyle.BROWN, 34f, 24f);
            ui.txtDescription.alignment = TextAlignmentOptions.TopLeft;

            // Nút: Equip xanh lá / Unequip tím, Upgrade cam / xám (script đổi sprite + chữ).
            PetButton(ui.btEquip, PopupStyle.ButtonColor.Green, -164f);
            PetButton(ui.btUpgrade, PopupStyle.ButtonColor.Orange, 164f);
            ui.spriteEquip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.spriteUnequip = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Violet);
            ui.spriteUpgrade = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Orange);
            ui.spriteDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);

            TMP_Text hint = PopupStyle.GetOrCreateText(panel, "HintText");
            PopupStyle.PlaceTop(hint.transform, 0f, PET_BUTTON_CENTER, INNER_MEDIUM, 60f);
            PopupStyle.Plain(hint, PopupStyle.MUTE, 36f, 24f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.text = "Summon to unlock";
            hint.gameObject.SetActive(false);
            ui.txtHint = hint;

            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static void PetButton(Button button, PopupStyle.ButtonColor color, float x)
    {
        PopupStyle.Button(button.transform, color, 300f);
        PopupStyle.PlaceTop(button.transform, x, PET_BUTTON_CENTER, 300f, PopupStyle.BUTTON_HEIGHT);
        Button(button);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        PopupStyle.RemoveKey(text);   // chữ do script đặt (Equip/Unequip, Upgrade/MAX)
    }
    #endregion

    #region Tỉ lệ summon (vừa 816): khuôn bảng Rarity của Level popup, 6 dòng
    private const float RATE_ROW = 60f;
    private const float RATE_HEADER = 40f;
    private const float RATE_SPACING = 10f;

    public static void ApplySummonRates()
    {
        EditPrefab(UI + "uiUpgradePet.prefab", root =>
        {
            UIUpgradePet ui = root.GetComponent<UIUpgradePet>();
            RectTransform panel = (RectTransform)Find(root.transform, "NewItem");
            const int rows = 6;
            float tableHeight = RATE_HEADER + rows * (RATE_ROW + RATE_SPACING);
            float height = PopupStyle.CONTENT_TOP + tableHeight - RATE_SPACING + PopupStyle.BOTTOM_INSET;
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, height, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.SetKey(title, "Popup.CapeUpgradeInfo.Info", "Summon Rates");
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            Transform table = Find(panel, "VerticalLayout");
            PopupStyle.PlaceTop(table, 0f, PopupStyle.CONTENT_TOP + (tableHeight - RATE_SPACING) / 2f, INNER_MEDIUM, tableHeight - RATE_SPACING);
            VerticalLayoutGroup layout = table.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = RATE_SPACING;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Transform header = Find(table, "Header");
            PopupStyle.Invisible(header.GetComponent<Image>());
            SetHeight(header, RATE_HEADER);
            TMP_Text tier = Text(header, "TierText");
            SideText(tier, true, 16f, 300f);
            PopupStyle.Plain(tier, PopupStyle.BROWN, 32f, 22f);
            RateColumns(ui.txtLevelCurrent, ui.txtLevelNext);
            PopupStyle.Plain(ui.txtLevelCurrent, PopupStyle.BROWN, 32f, 22f);
            PopupStyle.Plain(ui.txtLevelNext, PopupStyle.BROWN, 32f, 22f);

            for (int i = 0; i < ui.listRarity.Count; i++)
            {
                ElementRarityPet row = ui.listRarity[i];
                SetHeight(row.transform, RATE_ROW);
                if (i >= rows) continue;   // Exotic / SSS / Z: pet không có, script ẩn
                Image image = ReskinPrefabUtil.Img(row.transform, PopupStyle.Kit("RarityTable/bar_" + ((Rarity)i).ToString().ToLower()));
                image.color = Color.white;
                SideText(row.txtName, true, 18f, 320f);
                PopupStyle.Outline(row.txtName, 30f, 22f);
                RateColumns(row.txtRateCurret, row.txtRateNext);
                PopupStyle.Outline(row.txtRateCurret, 30f, 22f);
                PopupStyle.Outline(row.txtRateNext, 30f, 22f);
            }
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    private static void RateColumns(TMP_Text current, TMP_Text next)
    {
        SideText(current, false, 170f, 150f);
        SideText(next, false, 16f, 150f);
    }
    #endregion

    #region Summon bằng ads (nhỏ 680): mẫu "nhận thưởng + ads", nút hồng
    private const float AD_HEIGHT = 1010f;

    public static void ApplyAdSummon()
    {
        EditPrefab(UI + "UICompanionAdSummonPopup.prefab", root =>
        {
            UICompanionAdSummonPopup ui = root.GetComponent<UICompanionAdSummonPopup>();
            RectTransform content = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)content.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_SMALL, 0f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, AD_HEIGHT);
            RectTransform panel = (RectTransform)Find(content, "NewItem");
            PopupStyle.Shell(panel, PopupStyle.WIDTH_SMALL, AD_HEIGHT, panel, panel.Find("Header"));
            TMP_Text title = Text(panel, "TitleText");
            PopupStyle.Title(title, PopupStyle.WIDTH_SMALL);
            PopupStyle.SetKey(title, "Popup.CompanionAdSummon.Title", "View Ad");
            PopupStyle.CloseButton(ui.CloseButton.transform);
            Button(ui.CloseButton);

            TMP_Text info = Text(panel, "InfoTxt");
            PopupStyle.PlaceTop(info.transform, 0f, 216f, INNER_SMALL, 80f);
            PopupStyle.Plain(info, PopupStyle.BROWN, 36f, 26f);
            info.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(info, "Popup.CompanionAdSummon.Info", "Watch an ad and summon for free!");

            // Ô thưởng lớn: sách pet + "x15".
            Transform box = Find(panel, "BackgroundImage");
            ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(box, 0f, 408f, 380f, 256f);
            Transform icon = Find(box, "IconImage");
            icon.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Center(icon, 0f, 30f, 128f, 128f);
            PopupStyle.Center(ui.SummonCountText.transform, 0f, -78f, 340f, 72f);
            PopupStyle.Outline(ui.SummonCountText, 60f, 36f);
            ui.SummonCountText.alignment = TextAlignmentOptions.Center;

            TMP_Text increase = Text(panel, "InfoTxt_1");
            PopupStyle.PlaceTop(increase.transform, 0f, 610f, INNER_SMALL, 100f);
            PopupStyle.Plain(increase, PopupStyle.BROWN, 36f, 24f);
            increase.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(increase, "Popup.CompanionAdSummon.Increase", "Each time you watch an ad, the number of summons increases!");

            PopupStyle.PlaceTop(ui.NextSummonText.transform, 0f, 684f, INNER_SMALL, 48f);
            PopupStyle.Outline(ui.NextSummonText, 38f, 26f);
            ui.NextSummonText.color = PopupStyle.ACCENT;
            ui.NextSummonText.alignment = TextAlignmentOptions.Center;
            PopupStyle.RemoveKey(ui.NextSummonText);

            PopupStyle.PlaceTop(ui.infoText.transform, 0f, 736f, INNER_SMALL, 40f);
            PopupStyle.Plain(ui.infoText, PopupStyle.MUTE, 30f, 22f);
            ui.infoText.alignment = TextAlignmentOptions.Center;
            PopupStyle.RemoveKey(ui.infoText);

            PopupStyle.Button(ui.SummonButton.transform, PopupStyle.ButtonColor.Pink, 400f, 168f, 44f);
            PopupStyle.PlaceTop(ui.SummonButton.transform, 0f, 868f, 400f, 168f);
            Button(ui.SummonButton);
            ui.SummonButtonImage.color = Color.white;
            ui.SummonButtonText.enableWordWrapping = false;
            ui.spriteAd = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Pink);
            ui.spriteFree = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.spriteDisabled = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Kết quả summon (toàn màn): khuôn màn mở rương Đợt 2 — ruy băng, lưới 5 cột ô 150, NEW đỏ
    public static void ApplySummonResult()
    {
        EditPrefab(UI + "UiSumonPet.prefab", root =>
        {
            UISumonPet ui = root.GetComponent<UISumonPet>();
            Transform panel = Find(root.transform, "ContentAll");

            Image ribbon = PopupStyle.NewImage(panel, "TitleRibbon", PopupStyle.Kit("Gameplay/ribbon_offer"));
            PopupStyle.PlaceTop(ribbon.transform, 0f, 280f, 680f, 160f);
            TMP_Text title = PopupStyle.GetOrCreateText(ribbon.transform, "Title");
            PopupStyle.Center(title.transform, 0f, 12f, 560f, 90f);
            PopupStyle.Outline(title, 60f, 36f);
            title.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(title, "Popup.Level.RewardsTitle", "Rewards");

            // Lưới cuộn (gói Bone x30 tới 1055 ô): mask vuông, nội dung tự cao.
            ScrollRect scroll = Find(panel, "Scroll View").GetComponent<ScrollRect>();
            PopupStyle.PlaceTop(scroll.transform, 0f, 1120f, 1000f, 1440f);
            Transform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform.Find("Viewport");
            PopupStyle.Stretch(viewport);
            viewport.GetComponent<Image>().sprite = null;   // UIMask bo mờ mép cắt viền ô
            GridLayoutGroup grid = ui.transParent.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 150f);
            grid.spacing = new Vector2(28f, 28f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 24, 24);
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(ui.transParent.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Ô 1 pet: nền rarity phủ ô, icon 72%, nhãn đỏ NEW nghiêng góc trên-trái.
            Transform cell = ui.objPrefabElementPet.transform;
            ElementPetSummon element = cell.GetComponent<ElementPetSummon>();
            ((RectTransform)cell).sizeDelta = new Vector2(150f, 150f);
            PopupStyle.Stretch(element.imgBackground.transform);
            RectTransform icon = element.iconPet.rectTransform;
            icon.anchorMin = new Vector2(0.14f, 0.14f);
            icon.anchorMax = new Vector2(0.86f, 0.86f);
            icon.sizeDelta = Vector2.zero;
            icon.anchoredPosition = Vector2.zero;
            element.iconPet.preserveAspect = true;

            Image tag = PopupStyle.NewImage(cell, "NewTag", PopupStyle.Kit("Events/ribbon_red"));
            RectTransform tagRt = tag.rectTransform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
            tagRt.pivot = new Vector2(0.5f, 0.5f);
            tagRt.sizeDelta = new Vector2(112f, 48f);
            tagRt.anchoredPosition = new Vector2(40f, -4f);
            tagRt.localRotation = Quaternion.Euler(0f, 0f, 10f);
            tagRt.localScale = Vector3.one;
            TMP_Text newText = Text(cell, "NewTextObj");
            Reparent(newText.transform, tag.transform);
            PopupStyle.Stretch(newText.transform, 10f, 0f, 30f, 0f);
            PopupStyle.Outline(newText, 26f);
            newText.alignment = TextAlignmentOptions.Center;
            newText.gameObject.SetActive(true);
            tag.gameObject.SetActive(false);
            tag.transform.SetAsLastSibling();
            element.objNew = tag.gameObject;

            TMP_Text tapClose = Text(panel, "Text (TMP)");
            PopupStyle.PlaceTop(tapClose.transform, 0f, 1950f, 1000f, 60f);
            PopupStyle.Outline(tapClose, 34f, 24f);
            tapClose.fontStyle = FontStyles.Normal;   // chữ cũ để Bold → lệch font kit
            tapClose.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(tapClose, "Common.TapToClose", "Tap anywhere to close");
        });
    }
    #endregion

    #region Mastery upgrade (vừa 816, trong UIMainGame2): ô icon + mô tả nâu, panel tối giá trị, nút xanh gem
    private const float MASTERY_CARD = 280f;
    private const float MASTERY_PROGRESS_CENTER = 542f;
    private const float MASTERY_BUTTON_CENTER = 703f;
    private const float MASTERY_HEIGHT = 836f;

    public static void ApplyMasteryUpgrade()
    {
        EditPrefab(MAIN_GAME, root =>
        {
            MasterUpgradeUI ui = root.GetComponentInChildren<MasterUpgradeUI>(true);
            RectTransform panel = (RectTransform)Find(ui.transform, "DetailBg");
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, MASTERY_HEIGHT, panel, panel.Find("Header"));
            PopupStyle.Title(ui.txtName, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Ô icon box_tan + "Lvl N" đáy.
            Transform iconBox = Find(panel, "IconImageBg");
            ReskinPrefabUtil.Img(iconBox, PopupStyle.Kit("Mastery/box_tan"));
            PopupStyle.PlaceTop(iconBox, LEFT_EDGE + 124f, PopupStyle.CONTENT_TOP + MASTERY_CARD / 2f, 248f, MASTERY_CARD);
            ui.imgIcon.preserveAspect = true;
            PopupStyle.Center(ui.imgIcon.transform, 0f, 26f, 168f, 168f);
            RectTransform lv = ui.txtLevel.rectTransform;
            lv.anchorMin = lv.anchorMax = new Vector2(0.5f, 0f);
            lv.pivot = new Vector2(0.5f, 0.5f);
            lv.sizeDelta = new Vector2(230f, 56f);
            lv.anchoredPosition = new Vector2(0f, 42f);
            PopupStyle.Outline(ui.txtLevel, 40f, 28f);
            ui.txtLevel.alignment = TextAlignmentOptions.Center;

            // Mô tả nâu bên phải (DE dài nhất 92 ký tự = 4 dòng).
            float descLeft = LEFT_EDGE + 248f + 28f;
            PopupStyle.PlaceTopLeft(ui.txtDescirpts.transform, descLeft, PopupStyle.CONTENT_TOP + MASTERY_CARD / 2f, -LEFT_EDGE - descLeft, MASTERY_CARD);
            PopupStyle.Plain(ui.txtDescirpts, PopupStyle.BROWN, 34f, 24f);
            ui.txtDescirpts.alignment = TextAlignmentOptions.Left;

            // Giá trị hiện tại > cấp kế trên panel tối.
            Transform progress = Find(panel, "Progress");
            ReskinPrefabUtil.Img(progress, PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(progress, 0f, MASTERY_PROGRESS_CENTER, INNER_MEDIUM, 116f);
            PopupStyle.Stretch(ui.txtProcess.transform, 30f, 0f, 30f, 0f);
            PopupStyle.Outline(ui.txtProcess, 48f, 30f);
            ui.txtProcess.alignment = TextAlignmentOptions.Center;
            ui.txtProcess.enableWordWrapping = false;

            // Nút xanh: icon gem + phí căn giữa theo cụm (HLG); MAX ẩn gem → chữ tự căn giữa.
            Button upgrade = ui.btUpgrade;
            PopupStyle.Button(upgrade.transform, PopupStyle.ButtonColor.Green, 340f);
            PopupStyle.PlaceTop(upgrade.transform, 0f, MASTERY_BUTTON_CENTER, 340f, PopupStyle.BUTTON_HEIGHT);
            Button(upgrade);
            Transform content = Find(upgrade.transform, "Content");
            // Chỉ tìm con TRỰC TIẾP (TutorialUI cũng có con tên "Image" = mũi tên).
            Transform gem = upgrade.transform.Find("Image");
            if (gem == null) gem = content.Find("Image");
            Reparent(gem, content);
            gem.SetSiblingIndex(0);
            gem.GetComponent<Image>().preserveAspect = true;
            ((RectTransform)gem).sizeDelta = new Vector2(72f, 72f);
            LayoutElement gemLayout = GetOrAdd<LayoutElement>(gem.gameObject);
            gemLayout.preferredWidth = gemLayout.minWidth = 72f;
            gemLayout.preferredHeight = 72f;
            HorizontalLayoutGroup row = GetOrAdd<HorizontalLayoutGroup>(content.gameObject);
            row.spacing = 12f;
            row.padding = new RectOffset(0, 0, 0, 12);   // lệch lên vì mép đáy nút dày
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            PopupStyle.Outline(ui.txtQuantityRequest, 56f);
            ui.txtQuantityRequest.alignment = TextAlignmentOptions.Center;
            ui.txtQuantityRequest.enableWordWrapping = false;
            ui.txtQuantityRequest.rectTransform.sizeDelta = new Vector2(160f, 110f);
            if (ui.upgradeTutorial != null) IgnoreLayout(ui.upgradeTutorial.transform);
            ui.objCostIcon = gem.gameObject;
            ui.spriteUpgrade = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.spriteMax = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.btClose.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Helper bố cục (giống Batch2Reskin)
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

    private static void IgnoreLayout(Transform t)
    {
        GetOrAdd<LayoutElement>(t.gameObject).ignoreLayout = true;
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

    // Tìm theo đường dẫn, không có thì tìm theo tên trong cả cây (node có thể đã bị dời ở lần chạy trước).
    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t != null) return t;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == path) return child;
        throw new System.Exception("[Batch3Reskin] thiếu node " + root.name + "/" + path);
    }

    private static TMP_Text Text(Transform root, string path)
    {
        return Find(root, path).GetComponent<TMP_Text>();
    }
    #endregion
}
