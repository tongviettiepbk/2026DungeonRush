using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng Resources/Prefabs/UI/UIBattlePassPopup từ prefab rip layout-only Prefabs/UI/BattlePass/*:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ ContentAll (chạm nền = đóng, CloseOnClickDark gốc), gắn script, nối field;
// 3 ô mẫu (Free / Premium / mốc level) đặt sẵn trong popup để nhân bản lúc chạy.
// Chạy lại là GHI ĐÈ prefab — đã chỉnh bố cục bằng tay thì đừng dựng lại.
public static class BattlePassPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Prefabs/UI/BattlePass/";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/UIBattlePassPopup.prefab";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private const string MASTERY_ICONS = "Assets/_Assets/_ResourceGame/MasteryIcons/";
    private const string BG = "ContentTransform/Background/";
    private const string CONTENT = BG + "Scroll View/Viewport/Content/";

    [MenuItem("Tools/DungeonRush/Build Battle Pass UI/Battle Pass Popup")]
    public static string BuildBattlePassPopup()
    {
        GameObject root = NewRoot("UIBattlePassPopup", out Transform popup, out Button btBackground);
        UIBattlePassPopup ui = root.AddComponent<UIBattlePassPopup>();
        ui.isPopup = true;
        ui.ScrollRect = Find(popup, BG + "Scroll View").GetComponent<ScrollRect>();
        ui.LevelsTransform = (RectTransform)Find(popup, CONTENT + "Levels");
        ui.FreeRewardsTransform = (RectTransform)Find(popup, CONTENT + "FreeRewards");
        ui.PassRewardsTransform = (RectTransform)Find(popup, CONTENT + "PassRewards");
        ui.FillBarParentRectTransform = (RectTransform)Find(popup, CONTENT + "FillParent/FillParent");
        ui.FillBarRectTransform = (RectTransform)Find(popup, CONTENT + "FillParent/FillParent/FillBar");
        ui.BuyBattlePassButton = Find(popup, BG + "BuyPremiumPassButton").gameObject;
        ui.btBuy = ui.BuyBattlePassButton.GetComponent<Button>();
        ui.BattlePassPriceText = Text(popup, BG + "BuyPremiumPassButton/Content/Text_1");
        ui.btClose = Find(popup, BG + "BattlePassPopupCloseButton").GetComponent<Button>();
        ui.btBackground = btBackground;

        ui.FreeRewardPrefab = BuildCell(InstantiateRip("BattlePassFreeRewardUI", ui.FreeRewardsTransform), "ContentTransform/Reward1Text", "ContentTransform/Reward2Text", "ContentTransform/CheckMarkImage", null, "ContentTransform/NotificationUI");
        ui.PremiumRewardPrefab = BuildCell(InstantiateRip("BattlePassPremiumRewardUI", ui.PassRewardsTransform), "ContentTransform/RewardText", "ContentTransform/RewardText2", "ContentTransform/CheckImage", "ContentTransform/LockImage", "ContentTransform/NotificationUI_1");

        Transform levelElement = InstantiateRip("BattlePassLevelElementUI", ui.LevelsTransform);
        BattlePassLevelView levelView = levelElement.gameObject.AddComponent<BattlePassLevelView>();
        levelView.LevelText = Text(levelElement, "LevelText");
        levelView.BackgroundImage = levelElement.GetComponent<Image>();
        ui.LevelElementPrefab = levelView;

        // Icon "<sprite=0>" theo BattlePassRewardType (Cloak / key Cultist chưa có TMP sprite asset → không icon).
        ui.rewardSpriteAssets = new[]
        {
            Asset(CUR + "IsBone.asset"), Asset(CUR + "IsGem.asset"), Asset(CUR + "IsRedKey.asset"), Asset(CUR + "IsGreenKey.asset"),
            Asset(MASTERY_ICONS + "IsBox.asset"), Asset(CUR + "IsExp.asset"), Asset(CUR + "IsGoldPickaxe.asset"),
            Asset(CUR + "IsPickaxe.asset"), Asset(CUR + "IsDrill.asset"), null, null, Asset(CUR + "IsVial.asset"),
        };

        PrefabUtility.SaveAsPrefabAsset(root, OUT, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = "UIBattlePassPopup" + (ok ? " saved" : " FAILED");
        Debug.Log("[BattlePassPrefabBuilder] " + result);
        return result;
    }

    private static BattlePassRewardCellView BuildCell(Transform cell, string text1, string text2, string check, string lockPath, string notification)
    {
        BattlePassRewardCellView view = cell.gameObject.AddComponent<BattlePassRewardCellView>();
        view.RewardText1 = Text(cell, text1);
        view.RewardText2 = Text(cell, text2);
        view.CheckMarkImage = Find(cell, check).gameObject;
        view.LockImage = lockPath != null ? Find(cell, lockPath).gameObject : null;
        view.BackgroundImage = cell.GetComponent<Image>();
        view.Button = cell.GetComponent<Button>();
        if (view.Button == null) view.Button = cell.gameObject.AddComponent<Button>();
        view.NotificationUI = Find(cell, notification).gameObject;
        return view;
    }

    private static TMP_SpriteAsset Asset(string path) => AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);

    private static Transform InstantiateRip(string name, Transform parent)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RIP + name + ".prefab"), parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        RemoveMissingScripts(go);
        go.name = name;
        return go.transform;
    }

    private static void RemoveMissingScripts(GameObject go)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
    }

    // Root Canvas (Overlay, 1080×2160) → ContentAll (nền mờ toàn màn, có Button để đóng) → bản rip đã unpack.
    private static GameObject NewRoot(string name, out Transform popup, out Button btBackground)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject contentAll = new GameObject("ContentAll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        contentAll.layer = root.layer;
        RectTransform rt = contentAll.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        contentAll.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        btBackground = contentAll.GetComponent<Button>();
        btBackground.transition = Selectable.Transition.None;

        popup = InstantiateRip("BattlePassPopup", contentAll.transform);
        return root;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[BattlePassPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();
}
