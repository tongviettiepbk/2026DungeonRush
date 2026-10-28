using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Áp kit popup mới (khung Mastery) lên popup còn art cũ (khung đen Button-White-Active1, nút màu, ô frame-double đen)
// theo quy tắc chung, GIỮ bố cục: chỉ đổi sprite + font, dời tiêu đề vào thanh header, nút X về góc phải.
// Gọi qua UnityMCP execute_code: PopupKitReskin.Apply(path, "TitleText") → chụp kiểm → chỉnh tay chỗ lệch.
public static class PopupKitReskin
{
    private const string OLD_BUTTON = "Button-White-Active1";
    private const float HEADER_H = 138;
    private static readonly Color SMALL_TEXT = new Color32(0x5A, 0x46, 0x36, 0xFF);

    public static string Apply(string path, string titleName)
    {
        var log = new System.Text.StringBuilder();
        GameObject root = ReskinPrefabUtil.Open(path);

        // Khung chính = Image Button-White-Active1 lớn nhất không phải nút.
        Image panel = null;
        foreach (var im in root.GetComponentsInChildren<Image>(true))
        {
            if (!IsSprite(im, OLD_BUTTON) || im.GetComponent<Button>() != null) continue;
            if (panel == null || Area(im) > Area(panel)) panel = im;
        }
        if (panel == null) { PrefabUtility.UnloadPrefabContents(root); return path + ": không thấy khung"; }

        ReskinPrefabUtil.Img(panel.transform, ReskinPrefabUtil.UIKit("Mastery/popup_frame"));
        Rect pr = CanvasRect((RectTransform)panel.transform);
        log.Append("panel=" + panel.name);

        // Tiêu đề → giữa thanh header.
        Transform title = titleName != null ? FindDeep(root.transform, titleName) : null;
        if (title != null)
        {
            ReskinPrefabUtil.Header(panel.transform);
            var tr = (RectTransform)title;
            Rect r = CanvasRect(tr);
            ReskinPrefabUtil.Place(tr, r.center.x, pr.yMin + HEADER_H / 2, r.width, Mathf.Min(r.height, 100));
            log.Append(" header");
        }

        foreach (var im in root.GetComponentsInChildren<Image>(true))
        {
            if (im == panel || im.sprite == null) continue;
            Color c = im.color;
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            string sp = im.sprite.name;

            if (sp == OLD_BUTTON)
            {
                if (IsCloseButton(im))
                {
                    ReskinPrefabUtil.Img(im.transform, ReskinPrefabUtil.UIKit("Mastery/btn_close"));
                    ReskinPrefabUtil.Place(im.transform, pr.xMax - 11 - 60, pr.yMin + 9 + 60, 120, 120);
                    Transform icon = im.transform.Find("Image");
                    if (icon != null) icon.gameObject.SetActive(false);
                    continue;
                }
                ReskinPrefabUtil.Img(im.transform, ReskinPrefabUtil.UIKit(ButtonKit(c))).color = new Color(1, 1, 1, c.a);
            }
            else if ((sp == "frame-double" || sp == "item-double") && max < 0.3f)
            {
                // Giữ alpha: ô bấm trong suốt phủ cả hàng (CheckButton) vẫn trong suốt.
                ReskinPrefabUtil.Img(im.transform, ReskinPrefabUtil.UIKit("Mastery/box_tan")).color = new Color(1, 1, 1, c.a);
            }
        }

        // Màu chữ cũ có sắc (link xanh, chữ đỏ báo lỗi...) giữ lại; chữ nhỏ (< 30) viền đen khó đọc → không viền, màu nâu đậm.
        var oldColors = new System.Collections.Generic.Dictionary<TMP_Text, Color>();
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true)) oldColors[t] = t.color;
        ReskinPrefabUtil.Fonts(root);
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Color oc = oldColors[t];
            float h, s, v;
            Color.RGBToHSV(oc, out h, out s, out v);
            bool small = t.fontSize < 30 && !t.enableAutoSizing && !OnBar(t.transform);
            if (small) t.fontSharedMaterial = t.font.material;
            if (s > 0.35f && v > 0.4f) t.color = oc;
            else if (small) t.color = SMALL_TEXT;
        }
        // Placeholder ô nhập: giữ mờ.
        foreach (var f in root.GetComponentsInChildren<TMP_InputField>(true))
            if (f.placeholder != null) f.placeholder.color = new Color(1, 1, 1, 0.6f);

        ReskinPrefabUtil.Save(root, path);
        return path + ": " + log;
    }

    // Màu nút cũ → sprite kit: xanh dương = btn_blue, đỏ = btn_red, cam (quảng cáo) = btn_green, đen = ô tan, còn lại xám.
    public static string ButtonKit(Color c)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        if (max < 0.3f) return "Mastery/box_tan";
        if (c.b > c.r + 0.3f) return "Mastery/btn_blue";
        if (c.r > 0.9f && c.g < 0.35f) return "Lobby/btn_red";
        if (c.r > 0.9f && c.g < 0.6f) return "Lobby/btn_green";
        if (c.g > c.r + 0.2f) return "Lobby/btn_green";
        return "Lobby/btn_gray";
    }

    private static bool IsCloseButton(Image im)
    {
        if (im.GetComponent<Button>() == null) return false;
        Transform icon = im.transform.Find("Image");
        var iconImg = icon != null ? icon.GetComponent<Image>() : null;
        return iconImg != null && IsSprite(iconImg, "Icon_WhiteIcon_Close");
    }

    // Chữ nằm trên thanh tiến độ / nút màu → giữ viền trắng-đen.
    private static bool OnBar(Transform t)
    {
        Transform p = t.parent;
        for (int i = 0; i < 2 && p != null; i++, p = p.parent)
        {
            if (p.GetComponent<Button>() != null) return true;
            foreach (Transform c in p)
                if (c.name.StartsWith("Fill")) return true;
        }
        return false;
    }

    private static bool IsSprite(Image im, string name)
    {
        return im.sprite != null && im.sprite.name == name;
    }

    private static float Area(Image im)
    {
        var r = ((RectTransform)im.transform).rect;
        return r.width * r.height;
    }

    // Rect theo toạ độ mockup (gốc TRÊN-trái, canvas 1080x2160).
    public static Rect CanvasRect(RectTransform rt)
    {
        var c = new Vector3[4];
        rt.GetWorldCorners(c);
        return Rect.MinMaxRect(c[0].x + 540, 1080 - c[2].y, c[2].x + 540, 1080 - c[0].y);
    }

    private static Transform FindDeep(Transform t, string name)
    {
        Transform found = null;
        foreach (var c in t.GetComponentsInChildren<Transform>(true))
        {
            if (c.name != name) continue;
            if (c.gameObject.activeInHierarchy) return c;
            if (found == null) found = c;
        }
        return found;
    }
}
