using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng prefab UI PvP (Resources/Prefabs/UI/UIPvP*.prefab) từ 8 prefab rip layout-only ở Prefabs/UI/PvP/:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (như UIBossRush*), gắn script, nối field, gán TMP sprite
// asset cho chữ "<sprite=0>". Chạy lại được (ghi đè). Menu: Tools/DungeonRush/Build PvP UI Prefabs.
public static class PvPPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/PvP/";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";

    [MenuItem("Tools/DungeonRush/Build PvP UI Prefabs")]
    public static string BuildAll()
    {
        List<string> log = new List<string>();
        log.Add(BuildPvPPopup());
        log.Add(BuildFindOpponentPopup());
        log.Add(BuildEndPopup());
        log.Add(BuildRewardsPopup());
        log.Add(BuildLeaderboardPopup());
        AssetDatabase.SaveAssets();
        string result = string.Join("\n", log.ToArray());
        Debug.Log("[PvPPrefabBuilder]\n" + result);
        return result;
    }

    // ===== Sảnh =====

    private static string BuildPvPPopup()
    {
        GameObject root = NewRoot("UIPvPPopup", out Transform popup, RIP + "PvPPopup.prefab");
        UIPvPPopup ui = root.AddComponent<UIPvPPopup>();
        ui.isPopup = true;
        ui.txtTitle = Text(popup, "ContentTransform/Background/TitleText");
        ui.txtResetTime = Text(popup, "ContentTransform/Background/ResetText");
        ui.btClose = Btn(popup, "ContentTransform/Background/PvpCloseButton");
        ui.btLeaderboard = Btn(popup, "ContentTransform/Background/LeaderboardButton");
        ui.objLoaded = Find(popup, "ContentTransform/Background/Loaded").gameObject;
        ui.txtRankName = Text(popup, "ContentTransform/Background/Loaded/RankNameText");
        ui.btReward = Btn(popup, "ContentTransform/Background/Loaded/RankParent");
        ui.txtTrophy = Text(popup, "ContentTransform/Background/Loaded/RankParent/TrophyText");
        ui.objTrophySliderRoot = Find(popup, "ContentTransform/Background/Loaded/WhiteSlider").gameObject;
        ui.sliderTrophy = ui.objTrophySliderRoot.GetComponent<Slider>();
        ui.txtTrophySlider = Text(popup, "ContentTransform/Background/Loaded/WhiteSlider/TrophyText");
        ui.btBattle = Btn(popup, "ContentTransform/Background/Loaded/BattleButton");
        ui.txtBattle = Text(popup, "ContentTransform/Background/Loaded/BattleButton/Content/Text");
        ui.objLoading = Find(popup, "ContentTransform/Background/LoadingImage").gameObject;
        ui.btAdTicket = Btn(popup, "ContentTransform/Background/PvpAdButton");
        ui.txtAdTicket = Text(popup, "ContentTransform/Background/PvpAdButton/Content/Text");

        SetSprite(ui.txtTrophy, "IsTrophy");
        SetSprite(ui.txtBattle, "IsTicket");
        SetSprite(ui.txtAdTicket, "IsAd");
        ui.txtTrophySlider.raycastTarget = false;
        // Icon cuối thanh: sprite gốc "trophy 1" (batch rip báo thiếu) = trophy.png.
        Image sliderIcon = Find(popup, "ContentTransform/Background/Loaded/WhiteSlider/Image").GetComponent<Image>();
        sliderIcon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CUR + "trophy.png");
        sliderIcon.preserveAspect = true;
        return Save(root, "UIPvPPopup");
    }

    // ===== Chọn đối thủ =====

    private static string BuildFindOpponentPopup()
    {
        GameObject root = NewRoot("UIPvPFindOpponentPopup", out Transform popup, RIP + "PvPFindOpponentPopup.prefab");
        UIPvPFindOpponentPopup ui = root.AddComponent<UIPvPFindOpponentPopup>();
        ui.isPopup = true;
        ui.txtTitle = Text(popup, "ContentTransform/Background/TitleText");
        ui.txtError = Text(popup, "ContentTransform/Background/ErrorText");
        ui.btClose = Btn(popup, "ContentTransform/Background/PvpFindOpponentCloseButton");
        ui.objLoading = Find(popup, "ContentTransform/Background/LoadingImage").gameObject;
        ui.objLoaded = Find(popup, "ContentTransform/Loaded").gameObject;
        ui.listContent = Find(popup, "ContentTransform/Loaded/VerticalLayout");

        GameObject element = Embed(RIP + "PvPPlayerElementUIPrfab.prefab", ui.listContent, "PlayerElementTemplate");
        ElementPvPPlayerUI e = element.AddComponent<ElementPvPPlayerUI>();
        e.txtName = Text(element.transform, "Background/NicknameText");
        e.txtPower = Text(element.transform, "Background/PowerText");
        e.btBattle = Btn(element.transform, "Background/BattleButton");
        e.txtBattle = Text(element.transform, "Background/BattleButton/NicknameText_1");
        e.imgBackground = Find(element.transform, "Background").GetComponent<Image>();
        // Giá trị gốc còn lưu trong prefab rip (PvPPlayerElementUI.Default/HighlightedBackgroundColor).
        e.defaultColor = new Color(0.09411765f, 0.09411765f, 0.09411765f, 1f);
        e.highlightedColor = new Color(0.80784315f, 0.80784315f, 0.80784315f, 1f);
        SetSprite(e.txtPower, "IsPower");
        SetSprite(e.txtBattle, "IsTrophy");
        element.SetActive(false);
        ui.elementPrefab = e;
        return Save(root, "UIPvPFindOpponentPopup");
    }

    // ===== Kết quả trận =====

    private static string BuildEndPopup()
    {
        GameObject root = NewRoot("UIPvPEndPopup", out Transform popup, RIP + "PvPEndPopup.prefab");
        UIPvPEndPopup ui = root.AddComponent<UIPvPEndPopup>();
        ui.isPopup = true;
        ui.txtTitle = Text(popup, "ContentTransform/Background/TitleText");
        ui.txtError = Text(popup, "ContentTransform/Background/TitleText_1");
        ui.btClaim = Btn(popup, "ContentTransform/Background/PvpEndPopupClaimButton");
        ui.txtClaim = Text(popup, "ContentTransform/Background/PvpEndPopupClaimButton/Content/Text");
        ui.btRetry = Btn(popup, "ContentTransform/Background/RetryButton");
        ui.btClose = Btn(popup, "ContentTransform/Background/PvpEndCloseButton");
        ui.objResult = Find(popup, "ContentTransform/Loaded").gameObject;
        ui.objResult.SetActive(true);
        ui.txtTrophy = Text(popup, "ContentTransform/Loaded/TrophyText");
        ui.objLoading = Find(popup, "ContentTransform/LoadingImage").gameObject;
        ui.rewardContent = Find(popup, "ContentTransform/Loaded/Rewards/RewardsParent_1");
        ui.rewardTextPrefab = NewRewardText(ui.rewardContent, ui.txtTrophy);
        ui.rewardIcons = BuildRewardIcons();
        SetSprite(ui.txtTrophy, "IsTrophy");
        return Save(root, "UIPvPEndPopup");
    }

    // ===== Bảng thưởng =====

    private static string BuildRewardsPopup()
    {
        GameObject root = NewRoot("UIPvPRewardsPopup", out Transform popup, RIP + "PvPRewardPopup.prefab");
        UIPvPRewardsPopup ui = root.AddComponent<UIPvPRewardsPopup>();
        ui.isPopup = true;
        ui.txtTitle = Text(popup, "ContentTransform/NewItem/TitleText");
        ui.btClose = Btn(popup, "ContentTransform/NewItem/CloseButton");
        ui.txtLeague = Text(popup, "ContentTransform/NewItem/MidArea/LevelText");
        ui.btNext = Btn(popup, "ContentTransform/NewItem/MidArea/RightButton");
        ui.btPrevious = Btn(popup, "ContentTransform/NewItem/MidArea/LeftButton");
        ui.winContent = Find(popup, "ContentTransform/NewItem/WinRewards/RewardsParent_1");
        ui.loseContent = Find(popup, "ContentTransform/NewItem/LoseRewards/RewardsParent_1");
        ui.rewardTextPrefab = NewRewardText(ui.winContent, ui.txtLeague);
        ui.rewardIcons = BuildRewardIcons();
        // Nút con mũi tên trong rip cũng có Button → tắt để không nuốt click của nút cha.
        DisableInnerButtons(ui.btNext);
        DisableInnerButtons(ui.btPrevious);
        return Save(root, "UIPvPRewardsPopup");
    }

    // ===== Bảng xếp hạng =====

    private static string BuildLeaderboardPopup()
    {
        GameObject root = NewRoot("UIPvPLeaderboardPopup", out Transform popup, RIP + "PvPLeaderboardPopup.prefab");
        UIPvPLeaderboardPopup ui = root.AddComponent<UIPvPLeaderboardPopup>();
        ui.isPopup = true;
        ui.txtTitle = Text(popup, "ContentTransform/Background/TitleText");
        ui.btClose = Btn(popup, "ContentTransform/Background/CloseButton");
        ui.objLoaded = Find(popup, "ContentTransform/Background/Loaded").gameObject;
        ui.objLoading = Find(popup, "ContentTransform/Background/LoadingImage").gameObject;
        ui.scrollRect = Find(popup, "ContentTransform/Background/Loaded/Scroll View").GetComponent<ScrollRect>();
        ui.listContent = Find(popup, "ContentTransform/Background/Loaded/Scroll View/Viewport/VerticalLayout");

        // Tab world/country (TabSelector gốc sinh nút lúc chạy) — tạo sẵn 2 nút trong ItemContainer.
        Transform tabs = Find(popup, "ContentTransform/Background/LeaderboardTabSelectorUI/ItemContainer");
        ui.tabSelector = Find(popup, "ContentTransform/Background/LeaderboardTabSelectorUI/Selector") as RectTransform;
        string[] labels = { "World", "Country" };
        ui.tabButtons = new Button[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            ui.tabButtons[i] = NewTabButton(tabs, labels[i], ui.txtTitle);
        }
        HorizontalLayoutGroup layout = tabs.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        GameObject element = Embed(RIP + "PvPLeaderboardElement.prefab", ui.listContent, "LeaderboardElementTemplate");
        ElementPvPLeaderboardUI e = element.AddComponent<ElementPvPLeaderboardUI>();
        e.txtRank = Text(element.transform, "Background/RankText");
        e.txtName = Text(element.transform, "Background/NicknameText");
        e.txtTrophy = Text(element.transform, "Background/PowerText");
        e.imgBackground = Find(element.transform, "Background").GetComponent<Image>();
        // Giá trị gốc (PvPLeaderboardElementUI.Default/HighlightBackgroundColor).
        e.defaultColor = new Color(0.09411765f, 0.09411765f, 0.09411765f, 1f);
        e.highlightColor = new Color(1f, 0.85f, 0.4f, 1f);
        SetSprite(e.txtTrophy, "IsTrophy");
        element.SetActive(false);
        ui.elementPrefab = e;

        GameObject separator = Embed(RIP + "PvPLeaderboardSeperator.prefab", ui.listContent, "SeparatorTemplate");
        separator.SetActive(false);
        ui.separatorPrefab = separator;
        return Save(root, "UIPvPLeaderboardPopup");
    }

    // ===== Helper =====

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn) → bản rip đã unpack.
    private static GameObject NewRoot(string name, out Transform popup, string ripPath)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);

        popup = Embed(ripPath, contentAll.transform, null).transform;
        return root;
    }

    private static GameObject Embed(string prefabPath, Transform parent, string rename)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        if (!string.IsNullOrEmpty(rename)) go.name = rename;
        return go;
    }

    private static string Save(GameObject root, string name)
    {
        string path = OUT + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
        Object.DestroyImmediate(root);
        return name + (ok ? " saved" : " FAILED");
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[PvPPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();

    private static Button Btn(Transform root, string path) => Find(root, path).GetComponent<Button>();

    private static void SetSprite(TMP_Text text, string assetName)
    {
        if (text == null) return;
        text.spriteAsset = LoadIcon(assetName);
    }

    private static TMP_SpriteAsset LoadIcon(string assetName)
    {
        TMP_SpriteAsset asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + assetName + ".asset");
        if (asset == null) asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/_Assets/_ResourceGame/MasteryIcons/" + assetName + ".asset");
        return asset;
    }

    // RewardType → icon: Bone, Gem, DragonKey(đỏ), ZombieKey(xanh), Lootbox(box), Exp, …, Cloak(crown), Cultist key, Vial.
    private static RewardIconSet BuildRewardIcons()
    {
        RewardIconSet set = new RewardIconSet();
        set.spriteAssets[(int)RewardType.Bone] = LoadIcon("IsBone");
        set.spriteAssets[(int)RewardType.Gem] = LoadIcon("IsGem");
        set.spriteAssets[(int)RewardType.DragonBossDungeonKey] = LoadIcon("IsRedKey");
        set.spriteAssets[(int)RewardType.ZombieHordeDungeonKey] = LoadIcon("IsGreenKey");
        set.spriteAssets[(int)RewardType.Lootbox] = LoadIcon("IsBox");
        set.spriteAssets[(int)RewardType.Exp] = LoadIcon("IsExp");
        set.spriteAssets[(int)RewardType.CloakCurrency] = LoadIcon("IsCrown");
        set.spriteAssets[(int)RewardType.Vial] = LoadIcon("IsVial");
        return set;
    }

    // RewardTextPrefab gốc: 1 TMP "<sprite=0>{số}" (template ẩn trong lưới thưởng), font theo chữ có sẵn.
    private static TMP_Text NewRewardText(Transform parent, TMP_Text styleFrom)
    {
        GameObject go = new GameObject("RewardTextTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (styleFrom != null)
        {
            text.font = styleFrom.font;
            text.fontSharedMaterial = styleFrom.fontSharedMaterial;
            text.color = Color.white;
        }
        // Ô lưới gốc chỉ cao ~20 → cỡ chữ cố định, cho tràn ô (auto-size sẽ teo chữ).
        text.enableAutoSizing = false;
        text.fontSize = 36f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = "<sprite=0>100";
        go.SetActive(false);
        return text;
    }

    private static Button NewTabButton(Transform parent, string label, TMP_Text styleFrom)
    {
        GameObject go = new GameObject("Tab" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        go.GetComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.layer = go.layer;
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.SetParent(go.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        if (styleFrom != null)
        {
            text.font = styleFrom.font;
            text.fontSharedMaterial = styleFrom.fontSharedMaterial;
            text.color = styleFrom.color;
        }
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 48f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;
        return go.GetComponent<Button>();
    }

    private static void DisableInnerButtons(Button button)
    {
        if (button == null) return;
        foreach (Button inner in button.GetComponentsInChildren<Button>(true))
        {
            if (inner != button) inner.enabled = false;
        }
    }
}
