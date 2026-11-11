using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Kit popup dùng chung (Đợt 0 đã chốt — tools/concept/kit/concept.png, CSS ở kit.css).
// Số đo = concept × 2 (1 px CSS = 2 đơn vị canvas 1080×2160). Tool reskin từng đợt gọi các hàm này để sửa prefab tại chỗ.
public static class PopupStyle
{
    public const float WIDTH_SMALL = 680f;
    public const float WIDTH_MEDIUM = 816f;
    public const float WIDTH_LARGE = 968f;
    public const float HEADER_HEIGHT = 138f;
    public const float FRAME_UNDER_HEADER = 12f;      // header chồng lên khung 12
    public const float CONTENT_TOP = 176f;            // mép trên nội dung = header 138 - 12 + viền 14 + lề 36
    public const float SIDE_INSET = 54f;              // viền 14 + lề 40
    public const float BOTTOM_INSET = 58f;            // lề 44 + viền 14
    public const float CLOSE_SIZE = 104f;
    public const float BUTTON_HEIGHT = 150f;

    public static readonly Color BROWN = new Color32(122, 92, 60, 255);      // #7A5C3C chữ mô tả
    public static readonly Color MUTE = new Color32(166, 139, 103, 255);     // #A68B67 chữ phụ
    public static readonly Color ACCENT = new Color32(255, 211, 90, 255);    // #FFD35A số nhấn
    public static readonly Color UP = new Color32(98, 211, 74, 255);         // #62D34A tăng
    public static readonly Color DOWN = new Color32(255, 90, 90, 255);       // #FF5A5A giảm

    public enum ButtonColor { Green, Orange, Pink, Violet, Blue, Red, Gray }

    private const string FONT = "Assets/_Assets/Fonts/Asap/Asap-SemiBold.asset";
    private const string OUTLINE_MATERIAL = "Assets/_Assets/Fonts/Asap/Asap-SemiBold-Outline-Black.mat";

    public static Sprite Kit(string name)
    {
        Sprite sprite = ReskinPrefabUtil.UIKit(name);
        if (sprite == null) Debug.LogError("[PopupStyle] thiếu sprite 1_UIKit/" + name);
        return sprite;
    }

    public static Sprite Art(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Art/" + path + ".png");
        if (sprite == null) Debug.LogError("[PopupStyle] thiếu sprite Art/" + path);
        return sprite;
    }

    public static Sprite ButtonSprite(ButtonColor color)
    {
        switch (color)
        {
            case ButtonColor.Green: return Kit("Lobby/btn_green");
            case ButtonColor.Orange: return Kit("BattlePass/btn_orange");
            case ButtonColor.Pink: return Kit("Common/btn_pink");
            case ButtonColor.Violet: return Kit("Common/btn_violet");
            case ButtonColor.Blue: return Kit("Lobby/btn_blue");
            case ButtonColor.Red: return Kit("Lobby/btn_red");
            default: return Kit("Lobby/btn_gray");
        }
    }

    #region Toạ độ
    // Đặt theo khoảng cách từ mép TRÊN cha tới TÂM phần tử (giống luồng concept HTML).
    public static void PlaceTop(Transform t, float x, float topToCenter, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, -topToCenter);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    // Chữ/khối neo theo MÉP TRÁI (pivot trái) — cho cột chữ canh trái.
    public static void PlaceTopLeft(Transform t, float left, float topToCenter, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(left, -topToCenter);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    public static void Center(Transform t, float x, float y, float width, float height)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    public static void Stretch(Transform t, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }
    #endregion

    #region Khung popup
    // Khung chuẩn: panel căn giữa màn (w×h), ảnh khung kem phủ dưới header, header tím 138 trên cùng.
    // frame/header = node có sẵn (giữ GUID) hoặc null để tạo mới. Trả về header.
    public static RectTransform Shell(RectTransform panel, float width, float height, Transform frame, Transform header)
    {
        Center(panel, 0f, 0f, width, height);
        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null && panelImage.transform != frame)
            panelImage.enabled = false;

        if (frame == null) frame = NewImage(panel, "Frame", null).transform;
        ReskinPrefabUtil.Img(frame, Kit("Mastery/popup_frame"));
        if (frame != panel)
        {
            Stretch(frame, 0f, 0f, 0f, HEADER_HEIGHT - FRAME_UNDER_HEADER);
            frame.SetSiblingIndex(0);
        }

        if (header == null) header = NewImage(panel, "Header", null).transform;
        ReskinPrefabUtil.Img(header, Kit("Mastery/popup_header"));
        RectTransform rt = (RectTransform)header;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, HEADER_HEIGHT);
        rt.anchoredPosition = Vector2.zero;
        if (frame != panel) header.SetSiblingIndex(1);
        return rt;
    }

    // Tiêu đề trắng viền trong header (tâm cách mép trên 69).
    public static void Title(TMP_Text title, float popupWidth)
    {
        PlaceTop(title.transform, 0f, HEADER_HEIGHT / 2f, popupWidth - 2f * (CLOSE_SIZE + 40f), 100f);
        Outline(title, 60f, 36f);
        title.alignment = TextAlignmentOptions.Center;
    }

    // Nút X đỏ nằm TRONG header sát phải, cách đều trên/phải (tâm cách góc 69,69). Ẩn icon X trắng cũ.
    public static void CloseButton(Transform button)
    {
        button.gameObject.SetActive(true);
        ReskinPrefabUtil.Img(button, Kit("Mastery/btn_close"));
        RectTransform rt = (RectTransform)button;
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CLOSE_SIZE, CLOSE_SIZE);
        rt.anchoredPosition = new Vector2(-HEADER_HEIGHT / 2f, -HEADER_HEIGHT / 2f);
        rt.localScale = Vector3.one;
        foreach (Transform child in button)
        {
            Image image = child.GetComponent<Image>();
            if (image != null && image.sprite != null && image.sprite.name.Contains("Close"))
                child.gameObject.SetActive(false);
        }
        button.SetAsLastSibling();
    }
    #endregion

    #region Nút
    // Nút theo màu chức năng; chữ con (TMP đầu tiên) trắng viền, căn giữa nút (lệch lên vì mép đáy dày).
    public static Image Button(Transform button, ButtonColor color, float width, float height = BUTTON_HEIGHT, float fontSize = 52f)
    {
        Image image = ReskinPrefabUtil.Img(button, ButtonSprite(color));
        RectTransform rt = (RectTransform)button;
        rt.sizeDelta = new Vector2(width, height);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Outline(text, fontSize, fontSize * 0.6f);
            text.alignment = TextAlignmentOptions.Center;
            Transform content = button.Find("Content");
            if (content != null) Stretch(content);
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = textRt.anchorMax = textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(width - 40f, height - 30f);
            textRt.anchoredPosition = new Vector2(0f, 6f);
        }
        return image;
    }
    #endregion

    #region Chữ
    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT);
    public static Material OutlineMaterial => AssetDatabase.LoadAssetAtPath<Material>(OUTLINE_MATERIAL);

    // Trắng viền đen (trên nền màu / nút / ô). minSize > 0 → tự co khi chữ dài.
    public static void Outline(TMP_Text text, float size, float minSize = 0f)
    {
        text.font = Font;
        text.fontSharedMaterial = OutlineMaterial;
        text.color = Color.white;
        SetSize(text, size, minSize);
    }

    // Không viền (material mặc định của font), màu tuỳ — chữ nhỏ trên nền kem.
    public static void Plain(TMP_Text text, Color color, float size, float minSize = 0f)
    {
        text.font = Font;
        text.fontSharedMaterial = Font.material;
        text.color = color;
        SetSize(text, size, minSize);
    }

    private static void SetSize(TMP_Text text, float size, float minSize)
    {
        text.enableAutoSizing = minSize > 0f;
        text.fontSize = size;
        text.fontSizeMax = size;
        text.fontSizeMin = minSize > 0f ? minSize : size;
        text.richText = true;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    public static TMP_Text GetOrCreateText(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.GetComponent<TMP_Text>();
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = Font;
        text.raycastTarget = false;
        return text;
    }

    public static Image NewImage(Transform parent, string name, Sprite sprite)
    {
        Transform t = parent.Find(name);
        Image image = t != null ? t.GetComponent<Image>() : null;
        if (image == null)
        {
            GameObject go = t != null ? t.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            image = go.AddComponent<Image>();
        }
        image.raycastTarget = false;
        if (sprite != null) ReskinPrefabUtil.Img(image.transform, sprite);
        return image;
    }

    // Gắn/đổi khoá chữ dịch (chữ mới phải có key đủ 10 file Languages).
    public static void SetKey(TMP_Text text, string key, string sourceText)
    {
        LocalizeText localize = text.GetComponent<LocalizeText>();
        if (localize == null) localize = text.gameObject.AddComponent<LocalizeText>();
        localize.key = key;
        localize.sourceText = sourceText;
        text.text = sourceText;
    }

    public static void RemoveKey(TMP_Text text)
    {
        foreach (LocalizeText localize in text.GetComponents<LocalizeText>()) Object.DestroyImmediate(localize);
    }
    #endregion

    // Ẩn ảnh nền cũ nhưng giữ Image làm vùng bấm (Button cần targetGraphic).
    public static void Invisible(Image image)
    {
        if (image == null) return;
        image.sprite = null;
        image.color = new Color(1f, 1f, 1f, 0f);
    }

    public static void RemoveComponent<T>(Component target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null) Object.DestroyImmediate(component);
    }
}
