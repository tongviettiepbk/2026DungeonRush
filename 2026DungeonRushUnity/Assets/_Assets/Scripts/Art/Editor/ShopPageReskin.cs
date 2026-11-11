using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Reskin trang Shop (StoreTabPage) theo concept tools/concept/shop/concept.png (user chốt 2026-11-12).
// Thẻ ưu đãi lớn = nền sinh bằng tools/reskin/gen_shop_cards.py (Starter đỏ, No Ads xanh, Boost tím, deal cam/tím, rương vàng);
// tiêu đề mục = dải popup_header + icon; ô nhỏ (rương thường, Booster, Resources, gói Gem) = thẻ kem + header như popup;
// Daily Deal cao cố định 302 (5 món = 3 cột, ≤ 4 = 2 cột); Boost đang chạy = panel tối + đồng hồ.
// Sửa prefab tại chỗ, chạy lại ra cùng kết quả. KHÔNG gọi ShopPrefabBuilder (Build Shop UI Prefabs).
// Toạ độ = concept × 2 (1 px CSS = 2 đơn vị canvas). Thẻ rộng 1006 (lề trang 37).
public static class ShopPageReskin
{
    private const string STORE = "Assets/_Assets/Resources/Prefabs/UI/StoreTabPage.prefab";
    private const string SHOP = "Assets/_Assets/Prefabs/UINew/Shop/";
    private const string ART_SHOP = "Assets/_Assets/Art/1_UIKit/Shop/";
    private static readonly string[] CARD_ART = { "card_boost", "deal_orange", "deal_purple", "offer_red", "offer_blue", "chest_gold" };

    private const float CARD_HEIGHT = 208f;      // Starter / No Ads / Boost
    private const float DEAL_HEIGHT = 302f;      // Daily Deal + rương nổi bật
    private const float HEADER = 80f;            // header thẻ kem
    private const float HEADER_OVERLAP = 10f;
    private const float OFFER_BUTTON_W = 234f, OFFER_BUTTON_H = 130f;
    private const float TILE_BUTTON_H = 116f;
    private const float TILE_BUTTON_BOTTOM = 92f;  // mép đáy tới tâm nút

    [MenuItem("Tools/DungeonRush/Reskin/Shop Page")]
    public static string ApplyAll()
    {
        PrepareCardArt();
        ApplyRewardEntry();
        ApplyChestVertical();
        ApplyChestHorizontal();
        ApplyStore();
        return "ok";
    }

    // Nền thẻ mới có tia sáng → không 9-slice, Image Simple đúng kích thước.
    private static void PrepareCardArt()
    {
        foreach (string name in CARD_ART)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ART_SHOP + name + ".png") as TextureImporter;
            if (importer == null) { Debug.LogError("[ShopPageReskin] thiếu ảnh " + name); continue; }
            bool dirty = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                         || importer.spriteBorder != Vector4.zero || importer.mipmapEnabled;
            if (!dirty) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = Vector4.zero;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
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
        Debug.Log("[ShopPageReskin] " + System.IO.Path.GetFileNameWithoutExtension(path) + " done");
    }

    #region Trang Store
    private static void ApplyStore()
    {
        EditPrefab(STORE, root =>
        {
            StoreTabPage page = root.GetComponent<StoreTabPage>();
            Transform list = Batch7Reskin.Find(root.transform, "VerticalLayout");
            VerticalLayoutGroup vlg = list.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(37, 37, 24, 40);
            vlg.spacing = 24f;

            Starter(Batch7Reskin.Find(list, "StarterPack"));
            NoAds(Batch7Reskin.Find(list, "NoAds"));
            LifetimeBoost(Batch7Reskin.Find(list, "LifetimeBoost"));

            Transform chests = Batch7Reskin.Find(list, "Single_Chests");
            VerticalLayoutGroup chestVlg = chests.GetComponent<VerticalLayoutGroup>();
            chestVlg.padding = new RectOffset(0, 0, 0, 0);
            chestVlg.spacing = 24f;
            SectionHeader(chests.Find("ChestsRibbon"), PopupStyle.Kit("Common/chest"));
            GridLayoutGroup chestGrid = chests.Find("ChestProducts").GetComponent<GridLayoutGroup>();
            chestGrid.cellSize = new Vector2(490f, 608f);
            chestGrid.spacing = new Vector2(26f, 24f);
            chestGrid.padding = new RectOffset(0, 0, 0, 0);

            SectionHeader(list.Find("OffersRibbon"), PopupStyle.Kit("Common/clock"));
            Subtitle(list.Find("Title_3").GetComponent<TMP_Text>());
            foreach (DailyDealSlotUI slot in root.GetComponentsInChildren<DailyDealSlotUI>(true)) Deal(slot);

            SectionHeader(list.Find("BoostersRibbon"), PopupStyle.Kit("Lobby/icon_boost"));
            Subtitle(list.Find("Title_1").GetComponent<TMP_Text>());
            TileRow(list.Find("Boosters"), 392f, 26f, true);
            page.X2ExpBoostTimerPanel = BoosterTile(page.BuyX2ExpBoostButton, page.X2ExpBoostRemainingTimeText);
            page.X5ExpBoostTimerPanel = BoosterTile(page.BuyX5ExpBoostButton, page.X5ExpBoostRemainingTimeText);

            SectionHeader(list.Find("ResourcesRibbon"), PopupStyle.Art("5_Maps/Mining/pickaxe"));
            TileRow(list.Find("Resources"), 392f, 26f, true);
            page.GoldenPickaxeOwnedCountText = ResourceTile(page.BuyGoldenPickaxeButton);
            page.DrillOwnedCountText = ResourceTile(page.BuyDrillButton);

            SectionHeader(list.Find("GemsRibon"), PopupStyle.Art("2_Icons/Currency/gem"));
            foreach (string row in new[] { "GemPacks1", "GemPacks2" })
            {
                Transform packs = list.Find(row);
                TileRow(packs, 380f, 26f, false);
                foreach (Transform tile in packs) GemTile(tile);
            }
            EditorUtility.SetDirty(page);
        });
    }

    // Thẻ ưu đãi: Starter đỏ, chip thưởng 2 cột, nút giá bên phải.
    private static void Starter(Transform card)
    {
        CardBase(card, CARD_HEIGHT, ART_SHOP + "offer_red");
        CardTitle(card.Find("TitleText").GetComponent<TMP_Text>(), 56f, 600f);
        Transform grid = card.Find("VerticalLayout");
        TopLeftCenter(grid, 32f, 112f + 29f, 500f, 58f);
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(240f, 58f);
        layout.spacing = new Vector2(10f, 10f);
        layout.childAlignment = TextAnchor.UpperLeft;
        foreach (Transform chip in grid) Chip(chip, chip.GetComponentInChildren<TMP_Text>(true));
        OfferButton(card.Find("StarterPackBuyButton"), CARD_HEIGHT / 2f - 5f);
    }

    private static void NoAds(Transform card)
    {
        CardBase(card, CARD_HEIGHT, ART_SHOP + "offer_blue");
        CardTitle(card.Find("TitleText").GetComponent<TMP_Text>(), 56f, 600f);
        Transform group = card.Find("VerticalLayout");
        TopLeftCenter(group, 36f, 128f, 640f, 50f);
        TMP_Text desc = group.Find("TitleText_1").GetComponent<TMP_Text>();
        ((RectTransform)desc.transform).sizeDelta = new Vector2(640f, 50f);
        PopupStyle.Outline(desc, 34f, 24f);
        Batch7Reskin.Normal(desc);
        desc.alignment = TextAlignmentOptions.MidlineLeft;
        desc.enableWordWrapping = false;
        OfferButton(card.Find("NoAdsBuyButton"), CARD_HEIGHT / 2f - 5f);
    }

    // Boost Exp x2 Forever: icon tia sét bên trái thay <sprite> trong chữ; chữ dùng key dịch có sẵn.
    private static void LifetimeBoost(Transform card)
    {
        CardBase(card, CARD_HEIGHT, ART_SHOP + "card_boost");
        Image icon = PopupStyle.NewImage(card, "Icon", PopupStyle.Kit("Lobby/icon_boost"));
        icon.preserveAspect = true;
        TopLeftCenter(icon.transform, 36f, 96f, 104f, 104f);
        TMP_Text title = card.Find("TitleText").GetComponent<TMP_Text>();
        PopupStyle.SetKey(title, "UI.Store.Buy2xExp.Label", "Boost Exp x2 Forever");
        TopLeftCenter(title.transform, 156f, 96f, 520f, 80f);
        PopupStyle.Outline(title, 54f, 34f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.enableWordWrapping = false;
        OfferButton(card.Find("LifetimeBoostBuyButton"), CARD_HEIGHT / 2f - 5f);
    }

    // Daily Deal: cao cố định 302, lưới luôn 2 hàng (≤ 4 món ô 300, 5 món ô 196).
    private static void Deal(DailyDealSlotUI slot)
    {
        Transform card = slot.transform;
        Image bg = card.Find("Bg").GetComponent<Image>();
        CardBase(card, DEAL_HEIGHT, AssetDatabase.GetAssetPath(bg.sprite).Replace(".png", ""));
        CardTitle(slot.TitleText, 56f, 620f);
        Transform grid = slot.RewardGrid.transform;
        TopLeftCenter(grid, 32f, 112f + 63f, 640f, 126f);
        slot.RewardGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        slot.RewardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        slot.RewardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        slot.RewardGrid.childAlignment = TextAnchor.UpperLeft;
        slot.RewardGrid.cellSize = new Vector2(300f, 58f);
        slot.RewardGrid.spacing = new Vector2(10f, 10f);
        slot.DefaultConstraintCount = 2;
        slot.ConstraintCountForMoreThan4Rewards = 2;
        slot.ContentHeightForLessThan5Rewards = DEAL_HEIGHT;
        slot.ContentHeightFor5Rewards = DEAL_HEIGHT;
        slot.CellWidthForLessThan5Rewards = 300f;
        slot.CellWidthFor5Rewards = 196f;
        foreach (Transform chip in grid) Chip(chip, chip.GetComponentInChildren<TMP_Text>(true));
        OfferButton(slot.BuyButton.transform, 146f);
        EditorUtility.SetDirty(slot);
    }

    // Booster: thẻ kem, icon tia sét + "Duration"/thời lượng; đang chạy → panel tối + đồng hồ thay nút.
    private static GameObject BoosterTile(Button buy, TMP_Text timeText)
    {
        Transform tile = buy.transform.parent;
        Tile(tile, tile.Find("TitleText").GetComponent<TMP_Text>());
        tile.Find("TitleText_1").gameObject.SetActive(false);
        Image icon = PopupStyle.NewImage(tile, "Icon", PopupStyle.Kit("Lobby/icon_boost"));
        icon.preserveAspect = true;
        PopupStyle.PlaceTop(icon.transform, -90f, 176f, 128f, 128f);
        TMP_Text label = PopupStyle.GetOrCreateText(tile, "Label");
        PopupStyle.SetKey(label, "UI.Store.Duration", "Duration");
        TileLabel(label);
        TileValue(tile.Find("TitleText_2").GetComponent<TMP_Text>());
        TileButton(buy.transform, 430f);

        Image panel = PopupStyle.NewImage(tile, "TimerPanel", PopupStyle.Kit("Mastery/mastery_panel"));
        BottomCenter(panel.transform, 0f, TILE_BUTTON_BOTTOM, 430f, TILE_BUTTON_H);
        Image clock = PopupStyle.NewImage(panel.transform, "Clock", PopupStyle.Kit("Common/clock"));
        clock.preserveAspect = true;
        PopupStyle.Center(clock.transform, -90f, 2f, 52f, 52f);
        Batch7Reskin.Reparent(timeText.transform, panel.transform);
        PopupStyle.Center(timeText.transform, 55f, 2f, 220f, 70f);
        PopupStyle.Outline(timeText, 44f, 30f);
        Batch7Reskin.Normal(timeText);
        timeText.color = PopupStyle.ACCENT;
        timeText.alignment = TextAlignmentOptions.MidlineLeft;
        timeText.enableWordWrapping = false;
        timeText.gameObject.SetActive(true);
        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    // Resources: thẻ kem, ảnh cuốc/khoan + "Owned: n" (nối vào StoreTabPage) + "x3".
    private static TextMeshProUGUI ResourceTile(Button buy)
    {
        Transform tile = buy.transform.parent;
        Tile(tile, tile.Find("TitleText").GetComponent<TMP_Text>());
        Image icon = tile.Find("Image").GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        PopupStyle.PlaceTop(icon.transform, -90f, 176f, 128f, 128f);
        TextMeshProUGUI owned = (TextMeshProUGUI)PopupStyle.GetOrCreateText(tile, "OwnedText");
        owned.text = "Owned: 0";
        TileLabel(owned);
        TileValue(tile.Find("TitleText_2").GetComponent<TMP_Text>());
        TileButton(buy.transform, 430f);
        return owned;
    }

    // Gói gem: thẻ kem 318, header = icon gem + số gem, ảnh gói có quầng sáng, nút giá.
    private static void GemTile(Transform tile)
    {
        TMP_Text title = tile.Find("TitleText") != null ? tile.Find("TitleText").GetComponent<TMP_Text>()
                                                        : Batch7Reskin.Find(tile, "TitleText").GetComponent<TMP_Text>();
        ((RectTransform)tile).sizeDelta = new Vector2(318f, 380f);
        Tile(tile, null);
        Transform row = Batch7Reskin.NewNode(tile, "TitleRow");
        PopupStyle.PlaceTop(row, 0f, HEADER / 2f, 300f, 70f);
        HorizontalLayoutGroup hlg = Batch7Reskin.GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8f;
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        Image gem = PopupStyle.NewImage(row, "Icon", PopupStyle.Art("2_Icons/Currency/gem"));
        gem.preserveAspect = true;
        LayoutElement gemSize = Batch7Reskin.GetOrAdd<LayoutElement>(gem.gameObject);
        gemSize.preferredWidth = gemSize.preferredHeight = 40f;
        Batch7Reskin.Reparent(title.transform, row);
        PopupStyle.Outline(title, 42f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;

        Image pack = tile.Find("PackIcon").GetComponent<Image>();
        pack.preserveAspect = true;
        PopupStyle.PlaceTop(pack.transform, 0f, 190f, 170f, 140f);
        Image glow = PopupStyle.NewImage(tile, "Glow", PopupStyle.Art("7_FX/Glow_Circle02"));
        glow.color = new Color(1f, 1f, 1f, 0.5f);
        PopupStyle.PlaceTop(glow.transform, 0f, 190f, 280f, 280f);
        Batch7Reskin.PutBehind(glow.transform, pack.transform);
        foreach (Button button in tile.GetComponentsInChildren<Button>(true)) TileButton(button.transform, 286f, 40f);
        row.SetAsLastSibling();
    }
    #endregion

    #region Thẻ rương (prefab con của mục Chests)
    // Rương thường: thẻ kem 490×608, header tên rương, ảnh rương + quầng, panel tối "Contains…" + pity, nút mua + nút i.
    private static void ApplyChestVertical()
    {
        EditPrefab(SHOP + "Chest_Single_Product_Vertical.prefab", root =>
        {
            ChestStoreCard card = root.GetComponent<ChestStoreCard>();
            ((RectTransform)root.transform).sizeDelta = new Vector2(490f, 608f);
            Tile(root.transform, card.NameText);
            card.NameText.fontSize = 40f;
            card.NameText.fontSizeMax = 40f;

            Image icon = card.Icon;
            icon.preserveAspect = true;
            PopupStyle.PlaceTop(icon.transform, 0f, 185f, 208f, 172f);
            Image glow = PopupStyle.NewImage(root.transform, "Glow", PopupStyle.Art("7_FX/Glow_Circle02"));
            glow.color = new Color(1f, 1f, 1f, 0.5f);
            PopupStyle.PlaceTop(glow.transform, 0f, 185f, 340f, 340f);
            Batch7Reskin.PutBehind(glow.transform, icon.transform);

            RectTransform panel = InfoPanel(root.transform, card, TextAlignmentOptions.Center);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -430f);
            panel.sizeDelta = new Vector2(450f, panel.sizeDelta.y);

            TileButton(card.BuyButton.transform, 326f);
            ((RectTransform)card.BuyButton.transform).anchoredPosition = new Vector2(-52f, TILE_BUTTON_BOTTOM);
            InfoButton(card.InfoButton.transform, 92f, 104f);
            BottomCenter(card.InfoButton.transform, 169f, TILE_BUTTON_BOTTOM, 92f, 104f);
        });
    }

    // Rương Legendary+: thẻ ngang vàng cam, showcase thu nhỏ bên trái, tên + panel tối, nút i góc trên phải, nút mua bên phải.
    private static void ApplyChestHorizontal()
    {
        EditPrefab(SHOP + "Chest_Single_Product_Horizontal.prefab", root =>
        {
            ChestStoreCard card = root.GetComponent<ChestStoreCard>();
            Transform t = root.transform;
            ((RectTransform)t).sizeDelta = new Vector2(1006f, DEAL_HEIGHT);
            ReskinPrefabUtil.Img(t.Find("Bg"), PopupStyle.Art("1_UIKit/Shop/chest_gold"));
            PopupStyle.Stretch(t.Find("Bg"));

            RectTransform showcase = (RectTransform)t.Find("ShowcaseParent");
            showcase.anchorMin = showcase.anchorMax = new Vector2(0f, 0.5f);
            showcase.pivot = new Vector2(0.5f, 0.5f);
            showcase.anchoredPosition = new Vector2(150f, 0f);
            showcase.localScale = new Vector3(0.6f, 0.6f, 1f);

            CardTitle(card.NameText, 56f, 420f);
            TopLeftCenter(card.NameText.transform, 300f, 56f, 420f, 70f);

            RectTransform panel = InfoPanel(t, card, TextAlignmentOptions.Left);
            panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(292f, -100f);
            panel.sizeDelta = new Vector2(400f, panel.sizeDelta.y);

            OfferButton(card.BuyButton.transform, 187f);
            InfoButton(card.InfoButton.transform, 80f, 88f);
            RectTransform info = (RectTransform)card.InfoButton.transform;
            info.anchorMin = info.anchorMax = Vector2.one;
            info.pivot = new Vector2(1f, 0.5f);
            info.anchoredPosition = new Vector2(-28f, -60f);
            card.InfoButton.transform.SetAsLastSibling();
        });
    }

    // Panel tối co theo chữ: "Contains …" (tên rarity tô màu) + dòng pity — trên nền tối mới đọc được màu rarity.
    private static RectTransform InfoPanel(Transform card, ChestStoreCard store, TextAlignmentOptions align)
    {
        Image panel = PopupStyle.NewImage(card, "InfoPanel", PopupStyle.Kit("Mastery/mastery_panel"));
        panel.pixelsPerUnitMultiplier = 2f;
        VerticalLayoutGroup vlg = Batch7Reskin.GetOrAdd<VerticalLayoutGroup>(panel.gameObject);
        vlg.padding = new RectOffset(18, 18, 14, 16);
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter fitter = Batch7Reskin.GetOrAdd<ContentSizeFitter>(panel.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        foreach (TMP_Text text in new[] { store.ContainsText, store.PityText })
        {
            Batch7Reskin.Reparent(text.transform, panel.transform);
            PopupStyle.Plain(text, Color.white, 26f);
            Batch7Reskin.Normal(text);
            text.alignment = align;
            text.enableWordWrapping = true;
        }
        store.ContainsText.transform.SetSiblingIndex(0);
        store.PityText.transform.SetSiblingIndex(1);
        return (RectTransform)panel.transform;
    }

    private static void InfoButton(Transform button, float width, float height)
    {
        PopupStyle.Button(button, PopupStyle.ButtonColor.Blue, width, height, 44f);
        Batch7Reskin.Button(button.GetComponent<Button>());
    }
    #endregion

    #region Phần tử dùng chung
    // Ô thưởng deal (prefab con): ô tối trong suốt, "<sprite>số" trắng viền canh trái.
    private static void ApplyRewardEntry()
    {
        EditPrefab(SHOP + "DailyDealRewardEntryUI.prefab", root =>
        {
            DailyDealRewardEntryUI entry = root.GetComponent<DailyDealRewardEntryUI>();
            Chip(root.transform, root.GetComponentInChildren<TMP_Text>(true));
            EditorUtility.SetDirty(entry);
        });
    }

    private static void Chip(Transform chip, TMP_Text text)
    {
        ReskinPrefabUtil.Img(chip, PopupStyle.Kit("Shop/reward_slot"));
        if (text == null) return;
        PopupStyle.Stretch(text.transform, 10f, 0f, 4f, 0f);
        PopupStyle.Outline(text, 34f, 22f);
        Batch7Reskin.Normal(text);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
    }

    // Tiêu đề mục: dải header tím xám cao 100 + icon trước chữ (hàng tự căn giữa theo độ dài chữ từng ngôn ngữ).
    private static void SectionHeader(Transform ribbon, Sprite icon)
    {
        ReskinPrefabUtil.Img(ribbon, PopupStyle.Kit("Mastery/popup_header"));
        RectTransform rt = (RectTransform)ribbon;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 100f);
        Transform row = Batch7Reskin.NewNode(ribbon, "TitleRow");
        PopupStyle.Stretch(row);
        HorizontalLayoutGroup hlg = Batch7Reskin.GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 14f;
        hlg.padding = new RectOffset(0, 0, 0, 4);
        hlg.childControlWidth = hlg.childControlHeight = true;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        Image image = PopupStyle.NewImage(row, "Icon", icon);
        image.preserveAspect = true;
        LayoutElement size = Batch7Reskin.GetOrAdd<LayoutElement>(image.gameObject);
        size.preferredWidth = size.preferredHeight = 64f;
        TMP_Text title = ribbon.Find("Title") != null ? ribbon.Find("Title").GetComponent<TMP_Text>() : row.Find("Title").GetComponent<TMP_Text>();
        Batch7Reskin.Reparent(title.transform, row);
        PopupStyle.Outline(title, 50f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
        image.transform.SetSiblingIndex(0);
        title.transform.SetSiblingIndex(1);
    }

    private static void Subtitle(TMP_Text text)
    {
        RectTransform rt = text.rectTransform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 64f);
        PopupStyle.Outline(text, 32f, 20f);
        Batch7Reskin.Normal(text);
        text.alignment = TextAlignmentOptions.Center;
    }

    // Thẻ ưu đãi: chiều cao + nền sinh mới (Simple, phủ kín).
    private static void CardBase(Transform card, float height, string spritePath)
    {
        RectTransform rt = (RectTransform)card;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        Transform bg = card.Find("Bg");
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath + ".png");
        if (sprite == null) Debug.LogError("[ShopPageReskin] thiếu sprite " + spritePath);
        ReskinPrefabUtil.Img(bg, sprite);
        PopupStyle.Stretch(bg);
    }

    private static void CardTitle(TMP_Text title, float topToCenter, float width)
    {
        TopLeftCenter(title.transform, 36f, topToCenter, width, 70f);
        PopupStyle.Outline(title, 52f, 34f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.enableWordWrapping = false;
    }

    private static void OfferButton(Transform button, float topToCenter)
    {
        PopupStyle.Button(button, PopupStyle.ButtonColor.Green, OFFER_BUTTON_W, OFFER_BUTTON_H, 44f);
        RectTransform rt = (RectTransform)button;
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-32f, -topToCenter);
        Batch7Reskin.Button(button.GetComponent<Button>());
    }

    // Thẻ kem + header tím (như popup mini). title = null thì chỉ dựng khung.
    private static void Tile(Transform tile, TMP_Text title)
    {
        Transform bg = tile.Find("Bg");
        ReskinPrefabUtil.Img(bg, PopupStyle.Kit("Mastery/popup_frame"));
        PopupStyle.Stretch(bg, 0f, 0f, 0f, HEADER - HEADER_OVERLAP);
        bg.SetSiblingIndex(0);
        Image header = PopupStyle.NewImage(tile, "Header", PopupStyle.Kit("Mastery/popup_header"));
        RectTransform hr = (RectTransform)header.transform;
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = Vector2.one;
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(0f, HEADER);
        hr.anchoredPosition = Vector2.zero;
        header.transform.SetSiblingIndex(1);
        if (title == null) return;
        RectTransform tr = title.rectTransform;
        PopupStyle.PlaceTop(tr, 0f, HEADER / 2f, ((RectTransform)tile).sizeDelta.x - 40f, 70f);
        PopupStyle.Outline(title, 38f, 24f);
        Batch7Reskin.Normal(title);
        title.alignment = TextAlignmentOptions.Center;
        title.enableWordWrapping = false;
    }

    private static void TileRow(Transform row, float height, float spacing, bool controlChildHeight)
    {
        RectTransform rt = (RectTransform)row;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.spacing = spacing;
        hlg.childAlignment = TextAnchor.UpperCenter;
        if (!controlChildHeight) return;
        foreach (Transform tile in row) ((RectTransform)tile).sizeDelta = new Vector2(490f, height);
    }

    private static void TileLabel(TMP_Text label)
    {
        PopupStyle.PlaceTopLeft(label.transform, -10f, 150f, 220f, 40f);
        PopupStyle.Plain(label, PopupStyle.BROWN, 30f, 20f);
        Batch7Reskin.Normal(label);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
    }

    private static void TileValue(TMP_Text value)
    {
        PopupStyle.PlaceTopLeft(value.transform, -10f, 196f, 220f, 60f);
        PopupStyle.Outline(value, 52f, 34f);
        Batch7Reskin.Normal(value);
        value.alignment = TextAlignmentOptions.MidlineLeft;
        value.enableWordWrapping = false;
    }

    private static void TileButton(Transform button, float width, float fontSize = 42f)
    {
        PopupStyle.Button(button, PopupStyle.ButtonColor.Green, width, TILE_BUTTON_H, fontSize);
        BottomCenter(button, 0f, TILE_BUTTON_BOTTOM, width, TILE_BUTTON_H);
        Batch7Reskin.Button(button.GetComponent<Button>());
    }

    // Neo mép trái-trên của thẻ, pivot trái giữa: left = khoảng cách mép trái, topToCenter = mép trên tới tâm.
    private static void TopLeftCenter(Transform t, float left, float topToCenter, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(left, -topToCenter);
        rt.localScale = Vector3.one;
    }

    private static void BottomCenter(Transform t, float x, float bottomToCenter, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, bottomToCenter);
        rt.localScale = Vector3.one;
    }
    #endregion
}
