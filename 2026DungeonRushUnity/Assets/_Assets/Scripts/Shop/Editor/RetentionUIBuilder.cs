using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng 3 popup Starter Pack / thiếu gem / thưởng 7 ngày từ khung UIMiningOfferPopup, thêm 2 nút vào cột chào hàng lobby,
// bật mục No Ads + Starter Pack ở Store. Chạy lại được: popup dựng lại từ đầu, nút lobby đã có thì bỏ qua.
public static class RetentionUIBuilder
{
    private const string UI_DIR = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string TEMPLATE = UI_DIR + "UIMiningOfferPopup.prefab";
    private const string LOBBY = "Assets/_Assets/Prefabs/UI/UIMainGame2.prefab";
    private const string REWARDED_CHEST = UI_DIR + "UIRewardedChestPopup.prefab";
    private const string STORE = UI_DIR + "StoreTabPage.prefab";

    private const string SPR_BOX = "Assets/_Assets/Art/1_UIKit/Mastery/box_tan.png";
    private const string SPR_CHECK = "Assets/_Assets/Art/1_UIKit/Common/check_mark_icon.png";
    private const string SPR_BONE = "Assets/_Assets/Art/2_Icons/Currency/bone_icon.png";
    private const string SPR_GEM = "Assets/_Assets/Art/2_Icons/Currency/gem.png";
    private const string SPR_SEVEN_DAY = "Assets/_Assets/Art/2_Icons/Chest/epic_chest.png";
    private const string SPR_STARTER = "Assets/_Assets/Art/1_UIKit/Shop/gem_pack_1.png";

    private const float CELL_WIDTH = 165f;
    private const float CELL_HEIGHT = 255f;
    private const float CELL_SPACING = 14f;

    [MenuItem("Tools/DungeonRush/Build Retention UI")]
    public static void BuildAll()
    {
        Sprite hammer = GetLootboxSprite();
        BuildStarterPack(hammer);
        BuildMissingGems();
        BuildSevenDay(hammer);
        BuildStore();
        BuildLobby();
        AssetDatabase.SaveAssets();
        Debug.Log("[Retention UI] done");
    }

    #region Popup
    private static Sprite GetLootboxSprite()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(REWARDED_CHEST).GetComponent<UIRewardedChestPopup>().LootboxRewardSprite;
    }

    private static GameObject Begin(string name, out UIMiningOfferPopup old)
    {
        string path = UI_DIR + name + ".prefab";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(TEMPLATE, path);
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        root.name = name;
        old = root.GetComponent<UIMiningOfferPopup>();
        return root;
    }

    private static void End(GameObject root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root, UI_DIR + name + ".prefab");
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Transform Find(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) return t;
        }
        return null;
    }

    private static void Remove(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Transform t = Find(root, name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
    }

    private static TMP_Text CenterDesc(TMP_Text text)
    {
        RectTransform rt = text.rectTransform;
        rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
        rt.sizeDelta = new Vector2(680f, rt.sizeDelta.y);
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void BuildStarterPack(Sprite hammer)
    {
        GameObject root = Begin("UIStarterPackPopup", out UIMiningOfferPopup old);
        TMP_Text title = old.txtTitle, desc = old.txtMaxPickaxe, gem = old.txtGem, price = old.txtPrice, hammerText = old.txtPickaxe;
        Button buy = old.btBuy, background = old.btBackground;
        Object.DestroyImmediate(old);

        Find(root.transform, "Pickaxe").name = "Hammer";
        Find(root.transform, "Hammer").Find("Icon").GetComponent<Image>().sprite = hammer;
        Remove(root.transform, "Drill", "Golden", "Max+", "LoadingImage");

        UIStarterPackPopup popup = root.AddComponent<UIStarterPackPopup>();
        popup.txtTitle = title;
        popup.txtDesc = CenterDesc(desc);
        popup.txtGem = gem;
        popup.txtHammer = hammerText;
        popup.btBuy = buy;
        popup.txtPrice = price;
        popup.btBackground = background;
        End(root, "UIStarterPackPopup");
    }

    private static void BuildMissingGems()
    {
        GameObject root = Begin("UIMissingGemsPopup", out UIMiningOfferPopup old);
        TMP_Text title = old.txtTitle, desc = old.txtMaxPickaxe, gem = old.txtGem, price = old.txtPrice;
        Button buy = old.btBuy, background = old.btBackground;
        Object.DestroyImmediate(old);

        Remove(root.transform, "Drill", "Golden", "Pickaxe", "Max+", "LoadingImage");

        UIMissingGemsPopup popup = root.AddComponent<UIMissingGemsPopup>();
        popup.txtTitle = title;
        popup.txtDesc = CenterDesc(desc);
        popup.txtGem = gem;
        popup.btBuy = buy;
        popup.txtPrice = price;
        popup.btBackground = background;
        End(root, "UIMissingGemsPopup");
    }

    private static void BuildSevenDay(Sprite hammer)
    {
        GameObject root = Begin("UISevenDayPopup", out UIMiningOfferPopup old);
        TMP_Text title = old.txtTitle, price = old.txtPrice, amountTemplate = old.txtGem;
        Button buy = old.btBuy, background = old.btBackground;
        Object.DestroyImmediate(old);

        amountTemplate.transform.SetParent(root.transform, false);
        Remove(root.transform, "Drill", "Golden", "Pickaxe", "Gem", "Max+", "IncreaseMaxPickaxeText", "LoadingImage");

        Transform content = Find(root.transform, "Content");
        foreach (LayoutGroup group in content.GetComponents<LayoutGroup>()) Object.DestroyImmediate(group);
        foreach (ContentSizeFitter fitter in content.GetComponents<ContentSizeFitter>()) Object.DestroyImmediate(fitter);

        RectTransform contentRect = (RectTransform)content;
        contentRect.anchorMin = contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(720f, 540f);
        contentRect.anchoredPosition = new Vector2(0f, 50f);
        VerticalLayoutGroup vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = CELL_SPACING;
        vertical.childAlignment = TextAnchor.MiddleCenter;
        vertical.childControlWidth = false;
        vertical.childControlHeight = false;
        vertical.childForceExpandWidth = false;
        vertical.childForceExpandHeight = false;

        Transform row1 = NewRow(content, "Row1");
        Transform row2 = NewRow(content, "Row2");
        List<SevenDayCellUI> cells = new List<SevenDayCellUI>();
        for (int i = 0; i < SevenDayService.DAY_COUNT; i++)
        {
            bool isGrand = i == SevenDayService.DAY_COUNT - 1;
            float width = isGrand ? CELL_WIDTH * 2f + CELL_SPACING : CELL_WIDTH;
            cells.Add(NewCell(i < 4 ? row1 : row2, i, width, isGrand, amountTemplate));
        }
        Object.DestroyImmediate(amountTemplate.gameObject);

        UISevenDayPopup popup = root.AddComponent<UISevenDayPopup>();
        popup.txtTitle = title;
        popup.cells = cells;
        popup.btClaim = buy;
        popup.txtClaim = price;
        popup.btBackground = background;
        popup.sprGem = AssetDatabase.LoadAssetAtPath<Sprite>(SPR_GEM);
        popup.sprHammer = hammer;
        popup.sprBone = AssetDatabase.LoadAssetAtPath<Sprite>(SPR_BONE);
        End(root, "UISevenDayPopup");
    }

    private static Transform NewRow(Transform parent, string name)
    {
        GameObject row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        ((RectTransform)row.transform).sizeDelta = new Vector2(720f, CELL_HEIGHT);
        HorizontalLayoutGroup horizontal = row.AddComponent<HorizontalLayoutGroup>();
        horizontal.spacing = CELL_SPACING;
        horizontal.childAlignment = TextAnchor.MiddleCenter;
        horizontal.childControlWidth = false;
        horizontal.childControlHeight = false;
        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = false;
        return row.transform;
    }

    private static SevenDayCellUI NewCell(Transform parent, int index, float width, bool isGrand, TMP_Text amountTemplate)
    {
        GameObject go = new GameObject("Day" + (index + 1), typeof(RectTransform));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(width, CELL_HEIGHT);
        Image background = go.AddComponent<Image>();
        SetBox(background);

        Image today = NewImage(go.transform, "Today", Vector2.zero, Vector2.zero);
        SetBox(today);
        Stretch(today.rectTransform);
        today.color = new Color(1f, 0.85f, 0.25f, 0.65f);

        TMP_Text day = NewText(go.transform, "DayText", amountTemplate, new Vector2(0f, 95f), width, 32f);

        float iconX = isGrand ? 85f : 0f;
        Image icon = NewImage(go.transform, "Icon", new Vector2(-iconX, 8f), new Vector2(110f, 110f));
        icon.preserveAspect = true;
        TMP_Text amount = NewText(go.transform, "Amount", amountTemplate, new Vector2(-iconX, -90f), CELL_WIDTH, 36f);

        SevenDayCellUI cell = go.AddComponent<SevenDayCellUI>();
        cell.txtDay = day;
        cell.imgIcon = icon;
        cell.txtAmount = amount;
        cell.objToday = today.gameObject;

        if (isGrand)
        {
            cell.imgIcon2 = NewImage(go.transform, "Icon2", new Vector2(iconX, 8f), new Vector2(110f, 110f));
            cell.imgIcon2.preserveAspect = true;
            cell.txtAmount2 = NewText(go.transform, "Amount2", amountTemplate, new Vector2(iconX, -90f), CELL_WIDTH, 36f);
        }

        Image claimed = NewImage(go.transform, "Claimed", Vector2.zero, Vector2.zero);
        claimed.sprite = null;
        claimed.color = new Color(0f, 0f, 0f, 0.55f);
        Stretch(claimed.rectTransform);
        Image check = NewImage(claimed.transform, "Check", Vector2.zero, new Vector2(90f, 90f));
        check.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR_CHECK);
        check.preserveAspect = true;
        cell.objClaimed = claimed.gameObject;
        return cell;
    }

    private static void SetBox(Image image)
    {
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPR_BOX);
        image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
    }

    private static Image NewImage(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(Transform parent, string name, TMP_Text template, Vector2 position, float width, float fontSize)
    {
        TMP_Text text = Object.Instantiate(template, parent);
        text.name = name;
        RectTransform rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(width, 50f);
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
    #endregion

    #region Store
    private static void BuildStore()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(STORE);
        StoreTabPage page = root.GetComponent<StoreTabPage>();
        page.StarterPackParent = Find(root.transform, "StarterPack").gameObject;

        Transform lifetime = Find(root.transform, "LifetimeBoost");
        foreach (string section in new[] { "NoAds", "StarterPack" })
        {
            Transform t = Find(root.transform, section);
            CopyImage(lifetime.Find("Bg"), t.Find("Bg"));
            CopyImage(lifetime.Find("LifetimeBoostBuyButton"), t.Find(section + "BuyButton"));
            CopyText(lifetime.Find("TitleText"), t.Find("TitleText"));
        }

        // Đưa lên đầu Store: gốc xếp 2 mục này dưới ribbon Daily Deals vì chúng luôn tắt.
        Transform starter = Find(root.transform, "StarterPack"), noAds = Find(root.transform, "NoAds");
        starter.SetSiblingIndex(0);
        noAds.SetSiblingIndex(1);

        SetKey(starter.Find("TitleText"), "Popup.StarterPack.Title");
        SetKey(noAds.Find("TitleText"), "UI.Store.NoAds.Title");

        // Ô icon của bản rip mất sprite: bỏ ô, dời chữ sang trái.
        Transform descText = noAds.Find("VerticalLayout/TitleText_1");
        descText.Find("Image").gameObject.SetActive(false);
        RectTransform descGroup = (RectTransform)noAds.Find("VerticalLayout");
        descGroup.anchoredPosition = new Vector2(251f, descGroup.anchoredPosition.y);
        SetKey(descText, "UI.Store.NoAds.Desc");

        PrefabUtility.SaveAsPrefabAsset(root, STORE);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void SetKey(Transform text, string key)
    {
        LocalizeText localize = text.GetComponent<LocalizeText>();
        if (localize == null) localize = text.gameObject.AddComponent<LocalizeText>();
        localize.key = key;
    }

    private static void CopyImage(Transform from, Transform to)
    {
        Image a = from.GetComponent<Image>(), b = to.GetComponent<Image>();
        b.sprite = a.sprite;
        b.type = a.type;
        b.color = a.color;
    }

    private static void CopyText(Transform from, Transform to)
    {
        TMP_Text a = from.GetComponent<TMP_Text>(), b = to.GetComponent<TMP_Text>();
        b.font = a.font;
        b.fontSharedMaterial = a.fontSharedMaterial;
        b.color = a.color;
    }
    #endregion

    #region Lobby
    private static void BuildLobby()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(LOBBY);
        UIMainLobby lobby = root.GetComponentInChildren<UIMainLobby>(true);
        Transform layout = Find(root.transform, "OffersVerticalLayout");
        Transform template = layout.Find("MiningBundleOfferButton");

        if (lobby.starterPackButton == null)
        {
            GameObject go = CloneButton(template, "StarterPackOfferButton", SPR_STARTER);
            StarterPackButtonView view = go.AddComponent<StarterPackButtonView>();
            view.Button = go.GetComponent<Button>();
            view.TimerText = go.transform.Find("TimeText").GetComponent<TMP_Text>();
            lobby.starterPackButton = view;
        }

        if (lobby.sevenDayButton == null)
        {
            GameObject go = CloneButton(template, "SevenDayButton", SPR_SEVEN_DAY);
            SevenDayButtonView view = go.AddComponent<SevenDayButtonView>();
            view.Button = go.GetComponent<Button>();
            view.TimerText = go.transform.Find("TimeText").GetComponent<TMP_Text>();
            lobby.sevenDayButton = view;
        }

        PrefabUtility.SaveAsPrefabAsset(root, LOBBY);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static GameObject CloneButton(Transform template, string name, string iconPath)
    {
        GameObject go = Object.Instantiate(template.gameObject, template.parent);
        go.name = name;
        go.SetActive(false);
        go.transform.Find("TimeText").gameObject.SetActive(true);
        go.transform.Find("Icon").GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        return go;
    }
    #endregion
}
