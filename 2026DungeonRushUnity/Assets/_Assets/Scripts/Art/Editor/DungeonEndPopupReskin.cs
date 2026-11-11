using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Reskin UIDungeonEndPopup theo concept tools/concept/dungeon_end/concept.png (concept.html cùng thư mục).
// Bố cục cũ đã khớp concept (khung 817×631, header 138) → chỉ đổi chữ "Reward" nâu không viền, cỡ số thưởng,
// thêm chữ thua riêng (ô thưởng ẩn khi thua) và gán 2 nền nút thắng/thua cho UIDungeonEndPopup.
// Sửa thẳng prefab có sẵn (giữ GUID), chạy lại nhiều lần vẫn ra cùng kết quả.
public static class DungeonEndPopupReskin
{
    public const string PREFAB = "Assets/_Assets/Resources/Prefabs/UI/UIDungeonEndPopup.prefab";

    private const float REWARD_LABEL_SIZE = 40f;
    private const float REWARD_SIZE = 60f;
    private const float DEFEAT_SIZE = 44f;
    private const float DEFEAT_Y = 50f;            // giữa header và nút
    private const float DEFEAT_WIDTH = 720f;
    private const float DEFEAT_HEIGHT = 150f;

    private static readonly Color BROWN = new Color32(122, 92, 60, 255);

    [MenuItem("Tools/DungeonRush/Reskin/Dungeon End Popup")]
    public static string Apply()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PREFAB);
        try
        {
            UIDungeonEndPopup popup = root.GetComponent<UIDungeonEndPopup>();
            Transform background = popup.txtInfo.transform.parent;

            SetPlain(popup.objRewardLabel.GetComponent<TMP_Text>(), REWARD_LABEL_SIZE, 28f);

            popup.txtInfo.enableAutoSizing = false;
            popup.txtInfo.fontSize = REWARD_SIZE;
            popup.txtInfo.text = "<sprite=0><space=10>150";
            popup.objRewardBox = background.Find("RewardBox").gameObject;

            TMP_Text defeat = GetOrCreateText(background, "DefeatText", popup.objRewardLabel.GetComponent<TMP_Text>());
            RectTransform rt = defeat.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(DEFEAT_WIDTH, DEFEAT_HEIGHT);
            rt.anchoredPosition = new Vector2(0f, DEFEAT_Y);
            SetPlain(defeat, DEFEAT_SIZE, 30f);
            defeat.text = "Better luck next time!";
            defeat.gameObject.SetActive(false);
            popup.txtDefeat = defeat;

            popup.imgButton = (Image)popup.btOk.targetGraphic;
            popup.sprButtonWin = ReskinPrefabUtil.UIKit("Lobby/btn_green");
            popup.sprButtonLose = ReskinPrefabUtil.UIKit("Common/btn_violet");
            ReskinPrefabUtil.Img(popup.imgButton.transform, popup.sprButtonWin);

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[DungeonEndPopupReskin] done");
        return "ok";
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, TMP_Text template)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.GetComponent<TMP_Text>();
        TMP_Text text = Object.Instantiate(template, parent);
        text.name = name;
        foreach (LocalizeText localize in text.GetComponents<LocalizeText>()) Object.DestroyImmediate(localize);
        text.raycastTarget = false;
        return text;
    }

    // Chữ nâu không viền (material mặc định của font Asap), tự co khi bản dịch dài.
    private static void SetPlain(TMP_Text text, float size, float minSize)
    {
        text.fontSharedMaterial = text.font.material;
        text.color = BROWN;
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = minSize;
        text.alignment = TextAlignmentOptions.Center;
    }
}
