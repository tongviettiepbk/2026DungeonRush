using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Dựng Resources/Prefabs/UI/UILevelPopup từ prefab rip layout-only Resources/Prefabs/UI/LevelPopup:
// bọc Canvas/CanvasScaler 1080×2160 + nền mờ DarkBackground (bấm nền = CloseOnClickDark gốc), gắn UILevelPopup,
// nối field theo đúng node của LevelPopup gốc (map từ PathID trong xapk). Chạy lại là GHI ĐÈ prefab.
public static class LevelPopupPrefabBuilder
{
    private const string RIP = "Assets/_Assets/Resources/Prefabs/UI/LevelPopup.prefab";
    private const string OUT = "Assets/_Assets/Resources/Prefabs/UI/UILevelPopup.prefab";
    private const string CUR = "Assets/_Assets/_ResourceGame/Currency/";
    private const string SOUND = "Assets/_Assets/_Sound/UI/";
    private const string CONTENT = "ContentTransform/Background/";
    private const string TABLE = CONTENT + "RarityTable/VerticalLayout/";

    // Thứ tự Rarity 0..9 + ảnh nháy nền của từng hàng (RarityRowBackgrounds gốc).
    private static readonly string[] ROWS = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic", "Artifact", "Ancient", "Immortal", "Divine" };
    private static readonly string[] ROW_FLASH = { "Image_1", "Image", "Image_1", "Image_2", "Image_3", "Image_3", "Image_3", "Image_3", "Image_3", "Image_3" };

    [MenuItem("Tools/DungeonRush/Build Level Popup")]
    public static string BuildLevelPopup()
    {
        GameObject root = NewRoot("UILevelPopup", out Transform popup, out Button dark);
        UILevelPopup ui = root.AddComponent<UILevelPopup>();
        ui.isPopup = true;
        ui.DarkButton = dark;
        ui.LevelText = Text(popup, CONTENT + "TitleText");
        ui.CompleteButton = Find(popup, CONTENT + "CompleteButton").GetComponent<Button>();
        ui.CompleteButtonImage = ui.CompleteButton.GetComponent<Image>();
        ui.CompleteButtonText = Text(popup, CONTENT + "CompleteButton/Content/ButtonText");
        ui.CurrentLevelText = Text(popup, TABLE + "Header/CurrentLevelText");
        ui.NextLevelText = Text(popup, TABLE + "Header/NextLevelText");
        ui.CurrentLevelRarityProbabilityTexts = new List<TMP_Text>();
        ui.NextLevelRarityProbabilityTexts = new List<TMP_Text>();
        ui.RarityRowElements = new List<RectTransform>();
        ui.RarityRowBackgrounds = new List<Image>();
        for (int i = 0; i < ROWS.Length; i++)
        {
            string row = TABLE + ROWS[i] + "/";
            ui.CurrentLevelRarityProbabilityTexts.Add(Text(popup, row + "CommonCurrentRate"));
            ui.NextLevelRarityProbabilityTexts.Add(Text(popup, row + "CommonNextRate"));
            ui.RarityRowElements.Add(Find(popup, TABLE + ROWS[i]).GetComponent<RectTransform>());
            ui.RarityRowBackgrounds.Add(Find(popup, row + ROW_FLASH[i]).GetComponent<Image>());
        }
        ui.ExperienceSlider = Find(popup, CONTENT + "ExperienceSlider").GetComponent<Slider>();
        ui.PercentageText = Text(popup, CONTENT + "ExperienceSlider/PercentageText");
        ui.CloseButton = Find(popup, CONTENT + "CloseButton").GetComponent<Button>();
        ui.GemCountText = Text(popup, CONTENT + "Rewards/RewardText_2");
        ui.GemCountText.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(CUR + "IsGem.asset");
        ui.LevelUpSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SOUND + "levelup.ogg");

        // Bản rip mất RarityPatternEffect ở hoa văn 3 hàng cuối → gắn lại theo thông số gốc (5s, Linear).
        SetPatternFx(Find(popup, TABLE + "Ancient/Image/Pattern"), RarityPatternEffect.PatternAnimation.MoveDown, 128.5f, 0f);
        SetPatternFx(Find(popup, TABLE + "Immortal/Image_1/Pattern"), RarityPatternEffect.PatternAnimation.Spiral, 0f, 360f);
        SetPatternFx(Find(popup, TABLE + "Divine/Image_1/Pattern"), RarityPatternEffect.PatternAnimation.MoveDiagonalDown, 128f, 0f);

        // Tay chỉ bấm Complete (FTUE step 2).
        ui.LevelUpTutorialUI = TutorialUIBuilderUtil.Setup(Find(popup, CONTENT + "CompleteButton/Content/TutorialUI"));

        PrefabUtility.SaveAsPrefabAsset(root, OUT, out bool ok);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        string result = "UILevelPopup" + (ok ? " saved" : " FAILED");
        Debug.Log("[LevelPopupPrefabBuilder] " + result);
        return result;
    }

    // Root Canvas (Overlay, 1080×2160) → DarkBackground (nền mờ toàn màn, có Button) + bản rip đã unpack nằm trên.
    // Bản rip là anh em (không phải con) của nền để bấm vào khung popup không lan lên Button nền.
    private static GameObject NewRoot(string name, out Transform popup, out Button dark)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.matchWidthOrHeight = 0f;

        GameObject darkBackground = new GameObject("DarkBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        darkBackground.layer = root.layer;
        RectTransform rt = darkBackground.GetComponent<RectTransform>();
        rt.SetParent(root.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        darkBackground.GetComponent<Image>().color = new Color(0.149f, 0.149f, 0.149f, 0.588f);
        dark = darkBackground.GetComponent<Button>();
        dark.transition = Selectable.Transition.None;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RIP);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        popup = go.transform;
        return root;
    }

    private static Transform Find(Transform root, string path)
    {
        Transform t = root.Find(path);
        if (t == null) throw new System.Exception("[LevelPopupPrefabBuilder] Không thấy node: " + root.name + "/" + path);
        return t;
    }

    private static TMP_Text Text(Transform root, string path) => Find(root, path).GetComponent<TMP_Text>();

    private static void SetPatternFx(Transform t, RarityPatternEffect.PatternAnimation type, float moveDistance, float rotationDegrees)
    {
        RarityPatternEffect fx = t.GetComponent<RarityPatternEffect>();
        if (fx == null)
            fx = t.gameObject.AddComponent<RarityPatternEffect>();
        fx.animationType = type;
        fx.duration = 5f;
        fx.moveDistance = moveDistance;
        fx.rotationDegrees = rotationDegrees;
        fx.ease = DG.Tweening.Ease.Linear;
        Image image = t.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;
    }
}
