using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Đợt 2 – Shop & ưu đãi theo concept tools/concept/batch2/concept.png (kit chung PopupStyle).
// Sửa thẳng 9 popup + 2 prefab ô con (giữ GUID), chạy lại nhiều lần ra cùng kết quả. Các builder cũ
// (ShopPrefabBuilder, RetentionUIBuilder, WeaponOfferPrefabBuilder, RewardedChestPrefabBuilder, BattlePassPrefabBuilder)
// gọi lại tool này ở cuối. Toạ độ = đơn vị canvas (concept × 2), đo từ MÉP TRÊN panel tới TÂM (PopupStyle.PlaceTop).
public static class Batch2Reskin
{
    private const string UI = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string UINEW_SHOP = "Assets/_Assets/Prefabs/UINew/Shop/";
    private const float INNER_MEDIUM = PopupStyle.WIDTH_MEDIUM - 2f * PopupStyle.SIDE_INSET;   // 708

    // Popup ưu đãi: không header, nội dung bắt đầu dưới ruy băng (viền 14 + lề 92).
    private const float OFFER_CONTENT_TOP = 106f;
    private const float RIBBON_CENTER = 18f;
    private const float RIBBON_WIDTH = 660f;
    private const float RIBBON_HEIGHT = 156f;

    [MenuItem("Tools/DungeonRush/Reskin/Batch 2 (Shop & Offers)")]
    public static string ApplyAll()
    {
        ApplyChestCells();
        ApplyChestInfo();
        ApplyChestReveal();
        ApplyBundleReveal();
        ApplyBundleOffer();
        ApplyRewardedChest();
        ApplyOffer();
        ApplyStarterPack();
        ApplyMissingGems();
        ApplyBattlePass();
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
        Debug.Log("[Batch2Reskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Ô con dùng chung (ô món mở rương, ô nội dung gói)
    public static void ApplyChestCells()
    {
        // Ô 1 món mở ra: nền rarity (script gán), icon 72% ô, nhãn đỏ NEW nghiêng góc trên-trái (thay chữ NEW trơn).
        EditPrefab(UINEW_SHOP + "ChestRewardElementUI.prefab", root =>
        {
            ChestRewardElementUI ui = root.GetComponent<ChestRewardElementUI>();
            Transform icon = Find(root.transform, "ItemImage");
            RectTransform rt = (RectTransform)icon;
            rt.anchorMin = new Vector2(0.14f, 0.14f);
            rt.anchorMax = new Vector2(0.86f, 0.86f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            icon.GetComponent<Image>().preserveAspect = true;

            Image tag = PopupStyle.NewImage(root.transform, "NewTag", PopupStyle.Kit("Events/ribbon_red"));
            RectTransform tagRt = tag.rectTransform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
            tagRt.pivot = new Vector2(0.5f, 0.5f);
            tagRt.sizeDelta = new Vector2(112f, 48f);
            tagRt.anchoredPosition = new Vector2(40f, -4f);
            tagRt.localRotation = Quaternion.Euler(0f, 0f, 10f);
            tagRt.localScale = Vector3.one;
            TMP_Text newText = Text(root.transform, "NewTextObj");
            Reparent(newText.transform, tag.transform);
            PopupStyle.Stretch(newText.transform, 10f, 0f, 30f, 0f);
            PopupStyle.Outline(newText, 26f);
            newText.alignment = TextAlignmentOptions.Center;
            newText.gameObject.SetActive(true);
            tag.gameObject.SetActive(false);
            tag.transform.SetAsLastSibling();
            ui.NewBadge = tag.gameObject;
        });

        // Ô nội dung gói rương: nền nâu box_tan, icon trên, "x20" chữ viền dưới, dấu "?" góc trên-phải ở ô rương.
        EditPrefab(UINEW_SHOP + "ChestContentUI.prefab", root =>
        {
            ChestBundleOfferContentIcon ui = root.GetComponent<ChestBundleOfferContentIcon>();
            ReskinPrefabUtil.Img(root.transform, PopupStyle.Kit("Mastery/box_tan"));
            ((RectTransform)root.transform).sizeDelta = new Vector2(152f, 168f);
            Transform icon = root.transform.Find("Icon");
            icon.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Center(icon, 0f, 18f, 100f, 88f);
            TMP_Text count = ui.CountText != null ? ui.CountText : root.GetComponentInChildren<TMP_Text>(true);
            PopupStyle.Center(count.transform, 0f, -50f, 144f, 44f);
            PopupStyle.Outline(count, 34f, 24f);
            count.alignment = TextAlignmentOptions.Center;
            count.enableWordWrapping = false;

            Image badge = PopupStyle.NewImage(root.transform, "InfoBadge", PopupStyle.Kit("Common/question"));
            RectTransform badgeRt = badge.rectTransform;
            badgeRt.anchorMin = badgeRt.anchorMax = Vector2.one;
            badgeRt.pivot = new Vector2(0.5f, 0.5f);
            badgeRt.sizeDelta = new Vector2(48f, 48f);
            badgeRt.anchoredPosition = Vector2.zero;
            badge.transform.SetAsLastSibling();
            ui.InfoBadge = badge.gameObject;
        });
    }
    #endregion

    #region Chest Info (vừa 816): cao tự co theo số dòng (Mythic 6 dòng + pity … Standard 3 dòng)
    private const float ODDS_ROW = 60f;

    public static void ApplyChestInfo()
    {
        EditPrefab(UI + "UIChestInfoPopup.prefab", root =>
        {
            UIChestInfoPopup ui = root.GetComponent<UIChestInfoPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            Transform frame = panel.Find("Background");
            Transform header = Find(panel, "Header");
            Reparent(header, panel);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, 900f, frame, header);
            PopupStyle.Title(ui.ChestNameText, PopupStyle.WIDTH_MEDIUM);
            PopupStyle.CloseButton(ui.CloseButton.transform);
            Button(ui.CloseButton);
            foreach (Transform t in new[] { frame, header, ui.ChestNameText.transform, ui.CloseButton.transform })
                IgnoreLayout(t);

            // Cột dọc tự xếp + popup tự cao theo nội dung (dòng rarity ẩn thì thấp lại — fna gốc).
            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
            layout.padding = new RectOffset((int)PopupStyle.SIDE_INSET, (int)PopupStyle.SIDE_INSET, (int)PopupStyle.CONTENT_TOP, (int)PopupStyle.BOTTOM_INSET);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(panel.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ui.ChestIcon.preserveAspect = true;
            ((RectTransform)ui.ChestIcon.transform).sizeDelta = new Vector2(232f, 216f);
            GetOrAdd<LayoutElement>(ui.ChestIcon.gameObject).preferredHeight = 216f;

            // Panel tối: "Contains …" + dòng pity, chữ trắng (tên rarity tô màu đọc được trên nền tối).
            Image info = PopupStyle.NewImage(panel, "InfoPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            ((RectTransform)info.transform).sizeDelta = new Vector2(INNER_MEDIUM, 120f);
            VerticalLayoutGroup infoLayout = GetOrAdd<VerticalLayoutGroup>(info.gameObject);
            infoLayout.padding = new RectOffset(40, 40, 30, 30);
            infoLayout.spacing = 10f;
            infoLayout.childAlignment = TextAnchor.UpperCenter;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;
            foreach (TMP_Text text in new[] { ui.DescriptionText, ui.PityCountText })
            {
                Reparent(text.transform, info.transform);
                PopupStyle.Plain(text, Color.white, 34f);
                text.alignment = TextAlignmentOptions.Center;
            }
            ui.PityCountText.transform.SetAsLastSibling();
            ui.PityCountColor = PopupStyle.ACCENT;

            // Bảng tỉ lệ: nhãn nâu + thanh rarity (sprite bảng Level, không tô màu đè).
            Transform chances = panel.Find("Chances");
            ((RectTransform)chances).sizeDelta = new Vector2(INNER_MEDIUM, 400f);
            VerticalLayoutGroup chanceLayout = chances.GetComponent<VerticalLayoutGroup>();
            chanceLayout.padding = new RectOffset(0, 0, 0, 0);
            chanceLayout.spacing = 10f;
            chanceLayout.childAlignment = TextAnchor.UpperCenter;
            chanceLayout.childControlWidth = true;
            chanceLayout.childControlHeight = false;
            chanceLayout.childForceExpandWidth = true;
            chanceLayout.childForceExpandHeight = false;
            TMP_Text chancesText = Text(chances, "ChancesText");
            SetHeight(chancesText.transform, 44f);
            PopupStyle.Plain(chancesText, PopupStyle.BROWN, 36f, 24f);
            chancesText.alignment = TextAlignmentOptions.Center;
            foreach (ChestOddsRow row in ui.RarityRows)
            {
                SetHeight(row.transform, ODDS_ROW);
                ReskinPrefabUtil.Img(row.transform, PopupStyle.Kit("RarityTable/bar_" + row.Rarity.ToString().ToLower()));
                TMP_Text name = Text(row.transform, "RarityText");
                SideText(name, true, 24f, 420f);
                PopupStyle.Outline(name, 30f, 22f);
                SideText(row.PercentText, false, 24f, 220f);
                PopupStyle.Outline(row.PercentText, 30f, 22f);
            }

            // Thứ tự xếp: icon rương → panel tối → bảng tỉ lệ.
            ui.ChestIcon.transform.SetSiblingIndex(2);
            info.transform.SetSiblingIndex(3);
            chances.SetSiblingIndex(4);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Mở rương (toàn màn 1080×2160): rương đóng → lật từng món → tổng kết
    private const float REVEAL_CHEST_Y = 1290f;
    private const float REVEAL_BUTTON_Y = 1675f;

    public static void ApplyChestReveal()
    {
        EditPrefab(UI + "UIChestRevealPopup.prefab", root =>
        {
            UIChestRevealPopup ui = root.GetComponent<UIChestRevealPopup>();
            Transform panel = Find(root.transform, "ContentTransform");

            // Tên rương trên ruy băng vàng.
            Image ribbon = PopupStyle.NewImage(panel, "NameRibbon", PopupStyle.Kit("Gameplay/ribbon_offer"));
            PopupStyle.PlaceTop(ribbon.transform, 0f, 340f, 680f, 160f);
            Reparent(ui.ChestNameText.transform, ribbon.transform);
            PopupStyle.Center(ui.ChestNameText.transform, 0f, 12f, 560f, 90f);
            PopupStyle.Outline(ui.ChestNameText, 60f, 36f);
            ui.ChestNameText.alignment = TextAlignmentOptions.Center;

            TMP_Text tapOpen = ui.TapToOpenHint.GetComponent<TMP_Text>();
            PopupStyle.PlaceTop(tapOpen.transform, 0f, 840f, 1000f, 100f);
            PopupStyle.Outline(tapOpen, 68f, 40f);
            tapOpen.alignment = TextAlignmentOptions.Center;

            // Hào quang sau rương.
            Image glow = PopupStyle.NewImage(panel, "ChestGlow", PopupStyle.Art("7_FX/Glow_Circle02"));
            glow.color = new Color(1f, 1f, 1f, 0.5f);
            PopupStyle.PlaceTop(glow.transform, 0f, REVEAL_CHEST_Y, 840f, 840f);
            glow.transform.SetSiblingIndex(ui.ChestIcon.transform.GetSiblingIndex());
            ui.ChestIcon.preserveAspect = true;
            PopupStyle.PlaceTop(ui.ChestIcon.transform, 0f, REVEAL_CHEST_Y, 256f, 256f);

            // Thẻ đếm số món còn lại (thay football-card): thẻ kit nghiêng cạnh rương.
            Transform card = Find(panel, "ItemCounterBackgroundCard");
            ReskinPrefabUtil.Img(card, PopupStyle.Kit("Mastery/card_unlocked"));
            PopupStyle.PlaceTop(card, 250f, REVEAL_CHEST_Y - 100f, 116f, 140f);
            card.localRotation = Quaternion.Euler(0f, 0f, -8f);
            TMP_Text counter = ui.ItemsRemainingText;
            counter.transform.localScale = Vector3.one;
            PopupStyle.Center(counter.transform, 0f, 4f, 100f, 80f);
            PopupStyle.Outline(counter, 56f, 36f);
            counter.alignment = TextAlignmentOptions.Center;
            PopupStyle.RemoveKey(counter);
            ui.ItemCounterCard = card.gameObject;

            // Open all = bỏ qua → nút tím.
            Button skip = ui.SkipAllButton;
            PopupStyle.Button(skip.transform, PopupStyle.ButtonColor.Violet, 320f);
            PopupStyle.PlaceTop(skip.transform, 0f, REVEAL_BUTTON_Y, 320f, PopupStyle.BUTTON_HEIGHT);
            Button(skip);

            // Ô món: 1 ô 250 khi lật, tổng kết tối đa 6 ô 150 trên 1 hàng.
            PopupStyle.PlaceTop(ui.CellContainer, 0f, 865f, 1080f, 350f);
            ui.CellGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            ui.CellGrid.constraintCount = 6;
            ui.CellGrid.spacing = new Vector2(20f, 20f);
            ui.CellGrid.childAlignment = TextAnchor.MiddleCenter;
            ui.SingleItemCellSize = new Vector2(250f, 250f);
            ui.SummaryCellSize = new Vector2(150f, 150f);

            // "Tap anywhere to close" ở bảng tổng kết rương cuối (chỗ nút Open all).
            TMP_Text tapClose = PopupStyle.GetOrCreateText(panel, "TapToCloseText");
            PopupStyle.PlaceTop(tapClose.transform, 0f, REVEAL_BUTTON_Y, 1000f, 60f);
            PopupStyle.Outline(tapClose, 34f, 24f);
            tapClose.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(tapClose, "Common.TapToClose", "Tap anywhere to close");
            tapClose.gameObject.SetActive(false);
            ui.TapToCloseHint = tapClose.gameObject;
        });
    }

    public static void ApplyBundleReveal()
    {
        EditPrefab(UI + "UIChestBundleRevealPopup.prefab", root =>
        {
            UIChestBundleRevealPopup ui = root.GetComponent<UIChestBundleRevealPopup>();
            Transform panel = Find(root.transform, "ChestRewardsPopup");

            Image ribbon = PopupStyle.NewImage(panel, "TitleRibbon", PopupStyle.Kit("Gameplay/ribbon_offer"));
            PopupStyle.PlaceTop(ribbon.transform, 0f, 280f, 680f, 160f);
            TMP_Text title = PopupStyle.GetOrCreateText(ribbon.transform, "Title");
            PopupStyle.Center(title.transform, 0f, 12f, 560f, 90f);
            PopupStyle.Outline(title, 60f, 36f);
            title.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(title, "Popup.Level.RewardsTitle", "Rewards");

            // Lưới 5 cột ô 150, cuộn theo món mới; nội dung tự cao (có ContentSizeFitter mới cuộn được).
            Transform scroll = ui.ScrollRect.transform;
            PopupStyle.PlaceTop(scroll, 0f, 1120f, 1000f, 1440f);
            Transform viewport = ui.ScrollRect.viewport != null ? ui.ScrollRect.viewport : scroll.Find("Viewport");
            PopupStyle.Stretch(viewport);
            viewport.GetComponent<Image>().sprite = null;   // mask vuông — UIMask bo mờ mép cắt viền ô
            RectTransform content = ui.ScrollRect.content;
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150f, 150f);
            grid.spacing = new Vector2(28f, 28f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(0, 0, 24, 24);
            ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text tapClose = PopupStyle.GetOrCreateText(panel, "TapToCloseText");
            PopupStyle.PlaceTop(tapClose.transform, 0f, 1950f, 1000f, 60f);
            PopupStyle.Outline(tapClose, 34f, 24f);
            tapClose.alignment = TextAlignmentOptions.Center;
            PopupStyle.SetKey(tapClose, "Common.TapToClose", "Tap anywhere to close");
            tapClose.gameObject.SetActive(false);
            ui.TapToCloseHint = tapClose.gameObject;
        });
    }
    #endregion

    #region Gói rương (vừa 816): ruy băng vàng, panel trưng bày, 3 ô nội dung, panel mở khoá Pipe, nút xanh giá
    private const float BUNDLE_HEIGHT = 1130f;

    public static void ApplyBundleOffer()
    {
        EditPrefab(UI + "UIChestBundleOfferPopup.prefab", root =>
        {
            UIChestBundleOfferPopup ui = root.GetComponent<UIChestBundleOfferPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            OfferShell(panel, PopupStyle.WIDTH_MEDIUM, BUNDLE_HEIGHT, panel.Find("Background"), Find(panel, "Ribbon"), ui.TitleText);

            // Panel trưng bày tối: hào quang + ảnh gói + 5 ảnh pet/cape quanh (vị trí theo concept).
            Image show = PopupStyle.NewImage(panel, "ShowcasePanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(show.transform, 0f, OFFER_CONTENT_TOP + 230f, INNER_MEDIUM, 460f);
            show.transform.SetSiblingIndex(1);
            Transform showcase = Find(panel, "ShowcaseParent");
            Reparent(showcase, show.transform);
            PopupStyle.Center(showcase, 0f, 0f, 100f, 100f);
            PopupStyle.Center(showcase.Find("Center_glow_big"), 0f, -8f, 600f, 600f);
            PopupStyle.Center(showcase.Find("Center_glow_small"), 0f, -8f, 340f, 340f);
            RectTransform particle = (RectTransform)showcase.Find("UIParticle");
            particle.anchoredPosition = new Vector2(0f, -8f);
            ui.ChestIcon.preserveAspect = true;
            PopupStyle.Center(ui.ChestIcon.transform, 0f, -8f, 320f, 201f);
            Vector2[] spots = { new Vector2(-236f, -96f), new Vector2(236f, -96f), new Vector2(-180f, 116f), new Vector2(0f, 148f), new Vector2(180f, 116f) };
            for (int i = 0; i < ui.ShowcaseImages.Count && i < spots.Length; i++)
            {
                ui.ShowcaseImages[i].preserveAspect = true;
                PopupStyle.Center(ui.ShowcaseImages[i].transform, spots[i].x, spots[i].y, 124f, 124f);
            }

            // Hàng 3 ô nội dung (gem + từng loại rương).
            Transform content = ui.ContentContainer;
            Image contentImage = content.GetComponent<Image>();
            if (contentImage != null) contentImage.enabled = false;
            PopupStyle.PlaceTop(content, 0f, 678f, INNER_MEDIUM, 168f);
            HorizontalLayoutGroup row = content.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 24f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            row.padding = new RectOffset(0, 0, 0, 0);

            // "Unlocks the Pipe feature!" trong panel tối có icon Pipe.
            Image cloak = PopupStyle.NewImage(panel, "CloakPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(cloak.transform, 0f, 842f, 600f, 104f);
            Image cloakIcon = PopupStyle.NewImage(cloak.transform, "Icon", PopupStyle.Art("3_Equipment/07_Cape_Pipe/12-Cape"));
            cloakIcon.preserveAspect = true;
            PopupStyle.Center(cloakIcon.transform, -236f, 0f, 64f, 64f);
            Reparent(ui.CloakUnlockText.transform, cloak.transform);
            PopupStyle.Center(ui.CloakUnlockText.transform, 36f, 0f, 460f, 64f);
            PopupStyle.Plain(ui.CloakUnlockText, Color.white, 34f, 22f);
            ui.CloakUnlockText.alignment = TextAlignmentOptions.Left;
            ui.CloakUnlockText.enableWordWrapping = false;
            ui.CloakUnlockPanel = cloak.gameObject;

            // Nút xanh giá + huy hiệu giảm giá (badge_discount, không tô màu).
            PopupStyle.Button(ui.BuyButton.transform, PopupStyle.ButtonColor.Green, 380f);
            PopupStyle.PlaceTop(ui.BuyButton.transform, 0f, 997f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.BuyButton);
            Transform badge = ui.BuyButton.transform.Find("DiscountLabel");
            if (badge != null)
            {
                ReskinPrefabUtil.Img(badge, PopupStyle.Kit("Mining/badge_discount"));
                PopupStyle.Center(badge, 184f, 61f, 132f, 132f);
                badge.localRotation = Quaternion.Euler(0f, 0f, -10f);
                TMP_Text badgeText = badge.GetComponentInChildren<TMP_Text>(true);
                PopupStyle.Stretch(badgeText.transform, 16f, 16f, 16f, 16f);
                PopupStyle.Outline(badgeText, 34f, 22f);
                badgeText.alignment = TextAlignmentOptions.Center;
                badge.SetAsLastSibling();
            }
            if (ui.LoadingImage != null) PopupStyle.PlaceTop(ui.LoadingImage.transform, 0f, 997f, 100f, 100f);

            TapToClose(PopupStyle.GetOrCreateText(panel, "TapToClose"));
        });
    }
    #endregion

    #region Rương quảng cáo (nhỏ 680): header + X, 1 ô thưởng lớn, nút hồng ads
    private const float SMALL_REWARD_HEIGHT = 690f;

    public static void ApplyRewardedChest()
    {
        EditPrefab(UI + "UIRewardedChestPopup.prefab", root =>
        {
            UIRewardedChestPopup ui = root.GetComponent<UIRewardedChestPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_SMALL, 0f);
            Transform frame = panel.Find("Background");
            Transform header = Find(panel, "Header");
            Reparent(header, panel);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_SMALL, SMALL_REWARD_HEIGHT, frame, header);
            PopupStyle.Title(ui.TitleText, PopupStyle.WIDTH_SMALL);
            PopupStyle.CloseButton(ui.CloseButton.transform);
            Button(ui.CloseButton);

            TMP_Text desc = Text(panel, "DescriptionText");
            Description(desc, PopupStyle.WIDTH_SMALL);

            Transform box = Find(panel, "RewardParent");
            RewardBox(box, ui.RewardImage.transform, ui.RewardText, 380f, 200f);
            PopupStyle.PlaceTop(box, 0f, 354f, 380f, 200f);

            PopupStyle.Button(ui.AdButton.transform, PopupStyle.ButtonColor.Pink, 380f);
            PopupStyle.PlaceTop(ui.AdButton.transform, 0f, 557f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.AdButton);
            ui.AdButtonSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Pink);
            ui.ClaimButtonSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.DisabledButtonSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            ui.CloseButton.transform.SetAsLastSibling();
        });
    }
    #endregion

    #region Weapon Offer (vừa 816): thẻ món như Item Info, substat cố định 3 dòng (Divine tối đa 3)
    private const float OFFER_HEIGHT = 760f;

    public static void ApplyOffer()
    {
        EditPrefab(UI + "UIOfferPopup.prefab", root =>
        {
            UIOfferPopup ui = root.GetComponent<UIOfferPopup>();
            RectTransform outer = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)outer.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            PopupStyle.Center(outer, 0f, 0f, PopupStyle.WIDTH_MEDIUM, OFFER_HEIGHT);
            RectTransform card = (RectTransform)outer.Find("Background");
            Transform ribbon = card.Find("Ribbon");
            OfferShell(card, PopupStyle.WIDTH_MEDIUM, OFFER_HEIGHT, card, ribbon, Text(ribbon, "Text_1"));
            PopupStyle.Stretch(card);

            Transform stats = card.Find("Stats");
            ui.stats = Batch1Reskin.BuildStats(card, ui.ItemElementUI.transform, Text(card, "RarityText"), Text(card, "NameText"),
                Text(card, "BaseStatText"), Find(card, "SubStatText").GetComponent<TMP_Text>(), new Batch1Reskin.StatsLayout
                {
                    slotX = -258f, slotTop = OFFER_CONTENT_TOP + 96f, infoLeft = -134f, infoWidth = 488f, twoMainLines = false,
                    subTop = 326f, subWidth = INNER_MEDIUM, subLines = 3, subBox = true,
                });
            if (stats != null) stats.gameObject.SetActive(false);

            PopupStyle.Button(ui.BuyButton.transform, PopupStyle.ButtonColor.Green, 380f);
            PopupStyle.PlaceTop(ui.BuyButton.transform, 0f, 627f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.BuyButton);
            ui.BuyButtonImage = ui.BuyButton.GetComponent<Image>();
            ui.BuySprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Green);
            ui.DisabledSprite = PopupStyle.ButtonSprite(PopupStyle.ButtonColor.Gray);
            if (ui.ExpiredOverlay != null) PopupStyle.PlaceTop(ui.ExpiredOverlay.transform, 0f, 627f, 100f, 100f);

            if (ui.txtTapToClose != null) TapToClose(ui.txtTapToClose);
            ribbon.SetAsLastSibling();
        });
    }
    #endregion

    #region Starter Pack (vừa 816): đếm giờ panel tối, 2 ô thưởng lớn, nút xanh giá
    private const float STARTER_HEIGHT = 702f;

    public static void ApplyStarterPack()
    {
        EditPrefab(UI + "UIStarterPackPopup.prefab", root =>
        {
            UIStarterPackPopup ui = root.GetComponent<UIStarterPackPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            OfferShell(panel, PopupStyle.WIDTH_MEDIUM, STARTER_HEIGHT, panel.Find("Background"), Find(panel, "Ribbon"), ui.txtTitle);

            Image timer = PopupStyle.NewImage(panel, "TimerPanel", PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(timer.transform, 0f, OFFER_CONTENT_TOP + 52f, 640f, 104f);
            Image clock = PopupStyle.NewImage(timer.transform, "Icon", PopupStyle.Kit("Common/clock"));
            clock.preserveAspect = true;
            PopupStyle.Center(clock.transform, -262f, 0f, 56f, 56f);
            Reparent(ui.txtDesc.transform, timer.transform);
            PopupStyle.Center(ui.txtDesc.transform, 30f, 0f, 500f, 64f);
            PopupStyle.Plain(ui.txtDesc, Color.white, 34f, 22f);
            ui.txtDesc.alignment = TextAlignmentOptions.Left;
            ui.txtDesc.enableWordWrapping = false;
            PopupStyle.RemoveKey(ui.txtDesc);
            ui.txtDesc.text = "One-time offer! Ends in 23:59:12";

            Transform content = Find(panel, "Content");
            RewardRow(content, 352f, 320f, 220f);
            RewardBox(Find(content, "Hammer"), Find(content, "Hammer").Find("Icon"), ui.txtHammer, 320f, 220f);
            RewardBox(Find(content, "Gem"), Find(content, "Gem").Find("Icon"), ui.txtGem, 320f, 220f);

            PopupStyle.Button(ui.btBuy.transform, PopupStyle.ButtonColor.Green, 380f);
            PopupStyle.PlaceTop(ui.btBuy.transform, 0f, 569f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btBuy);
            if (ui.txtTapToClose != null) TapToClose(ui.txtTapToClose);
        });
    }
    #endregion

    #region Thiếu gem (vừa 816 — tiêu đề ES dài 29 ký tự): header + X, 1 ô gói gem, nút xanh giá
    public static void ApplyMissingGems()
    {
        EditPrefab(UI + "UIMissingGemsPopup.prefab", root =>
        {
            UIMissingGemsPopup ui = root.GetComponent<UIMissingGemsPopup>();
            RectTransform panel = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)panel.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_MEDIUM, 0f);
            PopupStyle.Shell(panel, PopupStyle.WIDTH_MEDIUM, SMALL_REWARD_HEIGHT, panel.Find("Background"), panel.Find("Header"));

            // Tiêu đề từ ruy băng cũ sang header; ruy băng ẩn.
            Transform ribbon = panel.Find("Ribbon");
            Reparent(ui.txtTitle.transform, panel);
            PopupStyle.Title(ui.txtTitle, PopupStyle.WIDTH_MEDIUM);
            if (ribbon != null) ribbon.gameObject.SetActive(false);

            // X: dùng lại nút đóng (đang tắt) của khung cũ.
            Transform close = Find(panel, "ChestPopupOfferCloseButton");
            PopupStyle.CloseButton(close);
            ui.btClose = close.GetComponent<Button>();
            Button(ui.btClose);

            Description(ui.txtDesc, PopupStyle.WIDTH_MEDIUM);
            Transform content = Find(panel, "Content");
            RewardRow(content, 354f, 380f, 200f);
            RewardBox(Find(content, "Gem"), Find(content, "Gem").Find("Icon"), ui.txtGem, 380f, 200f);

            PopupStyle.Button(ui.btBuy.transform, PopupStyle.ButtonColor.Green, 380f);
            PopupStyle.PlaceTop(ui.btBuy.transform, 0f, 557f, 380f, PopupStyle.BUTTON_HEIGHT);
            Button(ui.btBuy);
            close.SetAsLastSibling();
        });
    }
    #endregion

    #region Battle Pass (lớn 968): header + X, cột FREE / PREMIUM, danh sách trên panel tối
    private const float BP_HEIGHT = 1370f;
    private const float BP_COLUMN_X = 265f;
    private const float BP_CELL_WIDTH = 256f;
    private const float BP_CELL_HEIGHT = 156f;

    public static void ApplyBattlePass()
    {
        EditPrefab(UI + "UIBattlePassPopup.prefab", root =>
        {
            UIBattlePassPopup ui = root.GetComponent<UIBattlePassPopup>();
            RectTransform outer = (RectTransform)Find(root.transform, "ContentTransform");
            ((RectTransform)outer.parent).sizeDelta = new Vector2(PopupStyle.WIDTH_LARGE, 0f);
            PopupStyle.Center(outer, 0f, 0f, PopupStyle.WIDTH_LARGE, BP_HEIGHT);
            RectTransform bg = (RectTransform)outer.Find("Background");
            PopupStyle.Shell(bg, PopupStyle.WIDTH_LARGE, BP_HEIGHT, bg, bg.Find("Header"));
            PopupStyle.Stretch(bg);

            // Tiêu đề từ ruy băng sang header; X lên header (gốc ở đáy).
            Transform ribbon = bg.Find("Ribbon");
            TMP_Text title = Text(bg, "TitleText");
            Reparent(title.transform, bg);
            PopupStyle.Title(title, PopupStyle.WIDTH_LARGE);
            if (ribbon != null) ribbon.gameObject.SetActive(false);
            PopupStyle.CloseButton(ui.btClose.transform);
            Button(ui.btClose);

            // Nhãn cột: FREE nâu; PREMIUM vàng viền + vương miện (HLG để vương miện bám sát chữ theo ngôn ngữ).
            TMP_Text free = Text(bg, "Text_2");
            PopupStyle.PlaceTop(free.transform, -BP_COLUMN_X, 200f, 330f, 50f);
            PopupStyle.Plain(free, PopupStyle.BROWN, 40f, 26f);
            free.alignment = TextAlignmentOptions.Center;

            Transform premiumTitle = bg.Find("PremiumTitle");
            if (premiumTitle == null)
            {
                GameObject go = new GameObject("PremiumTitle", typeof(RectTransform));
                go.layer = bg.gameObject.layer;
                go.transform.SetParent(bg, false);
                premiumTitle = go.transform;
            }
            PopupStyle.PlaceTop(premiumTitle, BP_COLUMN_X, 200f, 360f, 50f);
            HorizontalLayoutGroup titleRow = GetOrAdd<HorizontalLayoutGroup>(premiumTitle.gameObject);
            titleRow.spacing = 8f;
            titleRow.childAlignment = TextAnchor.MiddleCenter;
            titleRow.childControlWidth = true;
            titleRow.childControlHeight = false;
            titleRow.childForceExpandWidth = titleRow.childForceExpandHeight = false;
            Image crown = PopupStyle.NewImage(premiumTitle, "Crown", PopupStyle.Kit("Common/crown_icon"));
            crown.preserveAspect = true;
            ((RectTransform)crown.transform).sizeDelta = new Vector2(48f, 40f);
            LayoutElement crownLayout = GetOrAdd<LayoutElement>(crown.gameObject);
            crownLayout.preferredWidth = 48f;
            crown.transform.SetAsFirstSibling();
            Transform premiumNode = bg.Find("Text") != null ? bg.Find("Text") : premiumTitle.Find("Text");
            TMP_Text premium = premiumNode.GetComponent<TMP_Text>();
            Reparent(premium.transform, premiumTitle);
            ((RectTransform)premium.transform).sizeDelta = new Vector2(300f, 50f);
            PopupStyle.Outline(premium, 40f);
            premium.color = PopupStyle.ACCENT;
            premium.enableWordWrapping = false;
            premium.alignment = TextAlignmentOptions.Center;
            premium.transform.SetAsLastSibling();
            RectTransform particle = (RectTransform)bg.Find("UIParticle");
            if (particle != null)
            {
                particle.anchorMin = particle.anchorMax = new Vector2(0.5f, 1f);
                particle.anchoredPosition = new Vector2(BP_COLUMN_X, -200f);
            }

            // Nút mua Premium xanh lá (mua) dưới nhãn; đã mua → tick xanh cùng chỗ.
            Transform buy = ui.BuyBattlePassButton.transform;
            PopupStyle.Button(buy, PopupStyle.ButtonColor.Green, 280f, 112f, 40f);
            PopupStyle.PlaceTop(buy, BP_COLUMN_X, 292f, 280f, 112f);
            Button(buy.GetComponent<Button>());
            Image bought = PopupStyle.NewImage(bg, "PremiumBoughtMark", PopupStyle.Kit("BattlePass/icon_check"));
            bought.preserveAspect = true;
            PopupStyle.PlaceTop(bought.transform, BP_COLUMN_X, 292f, 84f, 68f);
            bought.gameObject.SetActive(false);
            ui.PremiumBoughtMark = bought.gameObject;

            // Danh sách trên panel tối; mặt nạ vuông chừa viền 16.
            Transform scroll = ui.ScrollRect.transform;
            ReskinPrefabUtil.Img(scroll, PopupStyle.Kit("Mastery/mastery_panel"));
            PopupStyle.PlaceTop(scroll, 0f, 372f + 470f, 860f, 940f);
            Transform viewport = scroll.Find("Viewport");
            PopupStyle.Stretch(viewport, 16f, 16f, 16f, 16f);
            viewport.GetComponent<Image>().sprite = null;

            StyleRewardCell(ui.FreeRewardPrefab);
            StyleRewardCell(ui.PremiumRewardPrefab);

            // Ô mốc: tên độ khó + chương-màn 2 dòng (BattlePassLevelView), ô 224×128.
            BattlePassLevelView level = ui.LevelElementPrefab;
            ((RectTransform)level.transform).sizeDelta = new Vector2(224f, 128f);
            PopupStyle.Stretch(level.LevelText.transform, 16f, 12f, 16f, 12f);
            PopupStyle.Outline(level.LevelText, 38f, 22f);
            level.LevelText.alignment = TextAlignmentOptions.Center;
            level.LevelText.enableWordWrapping = false;
            level.LevelText.lineSpacing = -8f;
            level.LevelText.text = "<size=80%>Normal</size>\n3-5";

            // Hàng đầu cách đỉnh vùng cuộn 122 (concept 61), bước hàng 150 + 80 giữ như gốc.
            ui.LevelElementTopPadding = 122f;
            ui.LevelElementSpacing = 80f;
            ui.RewardElementHeight = 150f;
            ui.btClose.transform.SetAsLastSibling();
        });
    }

    // Ô thưởng 256×156: 2 dòng "<sprite> số" ±25, tick giữa ô, chấm đỏ + khoá lấn góc trên-phải.
    private static void StyleRewardCell(BattlePassRewardCellView cell)
    {
        ((RectTransform)cell.transform).sizeDelta = new Vector2(BP_CELL_WIDTH, BP_CELL_HEIGHT);
        PopupStyle.Center(cell.RewardText1.transform, 0f, 25f, 220f, 50f);
        PopupStyle.Center(cell.RewardText2.transform, 0f, -25f, 220f, 50f);
        foreach (TMP_Text text in new[] { cell.RewardText1, cell.RewardText2 })
        {
            PopupStyle.Outline(text, 38f);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
        }
        if (cell.CheckMarkImage != null)
        {
            cell.CheckMarkImage.GetComponent<Image>().preserveAspect = true;
            PopupStyle.Center(cell.CheckMarkImage.transform, 0f, 0f, 100f, 80f);
            cell.CheckMarkImage.transform.SetAsLastSibling();
        }
        if (cell.NotificationUI != null) Corner(cell.NotificationUI.transform, 4f, 4f, 56f, 56f);
        if (cell.LockImage != null) Corner(cell.LockImage.transform, 2f, 0f, 52f, 60f);
        Button(cell.Button);
    }
    #endregion

    #region Helper bố cục
    // Khung ưu đãi: thân kem không header, ruy băng vàng lấn mép trên (tâm cách mép trên 18), tiêu đề trắng viền.
    internal static void OfferShell(RectTransform panel, float width, float height, Transform frame, Transform ribbon, TMP_Text title)
    {
        PopupStyle.Center(panel, 0f, 0f, width, height);
        ReskinPrefabUtil.Img(frame, PopupStyle.Kit("Mastery/popup_frame"));
        if (frame != panel)
        {
            PopupStyle.Stretch(frame);
            frame.SetSiblingIndex(0);
        }

        ReskinPrefabUtil.Img(ribbon, PopupStyle.Kit("Gameplay/ribbon_offer"));
        PopupStyle.PlaceTop(ribbon, 0f, RIBBON_CENTER, RIBBON_WIDTH, RIBBON_HEIGHT);
        foreach (Transform child in ribbon)
            if (child != title.transform && child.GetComponent<TMP_Text>() == null) child.gameObject.SetActive(false);
        Reparent(title.transform, ribbon);
        PopupStyle.Center(title.transform, 0f, 12f, 540f, 84f);
        PopupStyle.Outline(title, 54f, 32f);
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
        ribbon.SetAsLastSibling();
    }

    // "Tap anywhere to close" trắng viền ngay dưới khung (neo đáy panel).
    internal static void TapToClose(TMP_Text text)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(760f, 50f);
        rt.anchoredPosition = new Vector2(0f, -58f);
        rt.localScale = Vector3.one;
        PopupStyle.Outline(text, 32f, 22f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        PopupStyle.SetKey(text, "Common.TapToClose", "Tap anywhere to close");
    }

    // Dòng mô tả nâu dưới header (tâm cách mép trên 201).
    private static void Description(TMP_Text text, float popupWidth)
    {
        PopupStyle.PlaceTop(text.transform, 0f, 201f, popupWidth - 2f * PopupStyle.SIDE_INSET, 56f);
        PopupStyle.Plain(text, PopupStyle.BROWN, 40f, 26f);
        text.alignment = TextAlignmentOptions.Center;
    }

    // Hàng ô thưởng (HLG có sẵn): nền cũ ẩn, các ô căn giữa cách 28.
    private static void RewardRow(Transform row, float topToCenter, float boxWidth, float boxHeight)
    {
        Image image = row.GetComponent<Image>();
        if (image != null) image.enabled = false;
        PopupStyle.PlaceTop(row, 0f, topToCenter, INNER_MEDIUM, boxHeight);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 28f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);
    }

    // Ô thưởng lớn box_tan: icon trái 112 + số chữ viền 56 bên phải.
    private static void RewardBox(Transform box, Transform icon, TMP_Text amount, float width, float height)
    {
        ReskinPrefabUtil.Img(box, PopupStyle.Kit("Mastery/box_tan"));
        ((RectTransform)box).sizeDelta = new Vector2(width, height);
        icon.GetComponent<Image>().preserveAspect = true;
        PopupStyle.Center(icon, -width / 2f + 92f, 0f, 112f, 112f);
        Reparent(amount.transform, box);
        RectTransform rt = amount.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width - 172f, 80f);
        rt.anchoredPosition = new Vector2(-width / 2f + 160f, 0f);
        rt.localScale = Vector3.one;
        PopupStyle.Outline(amount, 56f, 32f);
        amount.alignment = TextAlignmentOptions.Left;
        amount.enableWordWrapping = false;
    }

    // Nhãn/số canh mép trái hoặc phải của dòng.
    private static void SideText(TMP_Text text, bool left, float inset, float width)
    {
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
        rt.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rt.sizeDelta = new Vector2(width, 50f);
        rt.anchoredPosition = new Vector2(left ? inset : -inset, 0f);
        text.alignment = left ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        text.enableWordWrapping = false;
    }

    // Phần tử neo góc trên-phải, tâm lệch (x, y) so với góc.
    private static void Corner(Transform t, float x, float y, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = Vector3.one;
        t.SetAsLastSibling();
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
        throw new System.Exception("[Batch2Reskin] thiếu node " + root.name + "/" + path);
    }

    private static TMP_Text Text(Transform root, string path)
    {
        return Find(root, path).GetComponent<TMP_Text>();
    }
    #endregion
}
