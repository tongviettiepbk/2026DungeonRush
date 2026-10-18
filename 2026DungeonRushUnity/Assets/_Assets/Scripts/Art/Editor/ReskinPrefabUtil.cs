using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Hàm dùng chung khi reskin prefab popup theo mockup (canvas 1080x2160, toạ độ mockup = pixel, gốc TRÊN-trái).
// Gọi qua UnityMCP execute_code:
//   var root = ReskinPrefabUtil.Open(path);   ... Place / Img / Header / Close / Fonts ...   ReskinPrefabUtil.Save(root, path);
public static class ReskinPrefabUtil
{
    private static RectTransform canvasRoot;
    private static Vector2 oldSize, oldAmin, oldAmax, oldPivot;
    private static Vector3 oldScale;

    // Prefab popup có Canvas gốc kích thước 0 / scale 0 khi mở bằng LoadPrefabContents → tạm đặt 1080x2160 để quy đổi toạ độ.
    public static GameObject Open(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        canvasRoot = (RectTransform)root.transform;
        oldSize = canvasRoot.sizeDelta; oldAmin = canvasRoot.anchorMin; oldAmax = canvasRoot.anchorMax;
        oldPivot = canvasRoot.pivot; oldScale = canvasRoot.localScale;
        canvasRoot.anchorMin = canvasRoot.anchorMax = canvasRoot.pivot = new Vector2(0.5f, 0.5f);
        canvasRoot.sizeDelta = new Vector2(1080, 2160);
        canvasRoot.localScale = Vector3.one;
        return root;
    }

    public static void Save(GameObject root, string path)
    {
        var rt = (RectTransform)root.transform;
        rt.anchorMin = oldAmin; rt.anchorMax = oldAmax; rt.pivot = oldPivot; rt.sizeDelta = oldSize; rt.localScale = oldScale;
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        canvasRoot = null;
    }

    // Đặt tâm (x, y) + cỡ w x h theo pixel mockup; neo giữa cha.
    public static void Place(Transform t, float x, float y, float w, float h)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        var parent = (RectTransform)rt.parent;
        Vector2 lp = parent.InverseTransformPoint(canvasRoot.TransformPoint(new Vector3(x - 540, 1080 - y, 0)));
        rt.anchoredPosition = lp - parent.rect.center;
    }

    public static Sprite UIKit(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Art/1_UIKit/" + name + ".png");
    }

    // Gán sprite: có viền 9-slice → Sliced, màu trắng, pixelsPerUnitMultiplier 1 (prefab rip hay để ≠1).
    public static Image Img(Transform t, Sprite s)
    {
        var im = t.GetComponent<Image>();
        if (im == null) im = t.gameObject.AddComponent<Image>();
        im.sprite = s;
        im.type = s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        im.color = Color.white;
        im.pixelsPerUnitMultiplier = 1;
        im.preserveAspect = false;
        return im;
    }

    // Thanh tiêu đề tím (kit Mastery) cao 138 trên đỉnh khung popup, nằm dưới mọi node con khác.
    public static Transform Header(Transform popup)
    {
        Transform header = popup.Find("Header");
        if (header == null)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(Image));
            go.layer = popup.gameObject.layer;
            header = go.transform;
            header.SetParent(popup, false);
        }
        header.SetSiblingIndex(0);
        var hr = (RectTransform)header;
        hr.anchorMin = new Vector2(0, 1); hr.anchorMax = new Vector2(1, 1); hr.pivot = new Vector2(0.5f, 1);
        hr.sizeDelta = new Vector2(0, 138); hr.anchoredPosition = Vector2.zero;
        Img(header, UIKit("Mastery/popup_header")).raycastTarget = false;
        return header;
    }

    // Nút X đỏ (ảnh có sẵn dấu X) góc trên phải như UILevelPopup; ẩn icon X cũ.
    public static void Close(Transform btn, string oldIconChild = "Image")
    {
        var rt = (RectTransform)btn;
        Img(btn, UIKit("Mastery/btn_close"));
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(120, 120);
        rt.anchoredPosition = new Vector2(-11, -9);
        Transform icon = btn.Find(oldIconChild);
        if (icon != null) icon.gameObject.SetActive(false);
    }

    // Mọi chữ: Asap-SemiBold + viền đen, màu trắng.
    public static void Fonts(GameObject root)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Assets/Fonts/Asap/Asap-SemiBold.asset");
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Assets/Fonts/Asap/Asap-SemiBold-Outline-Black.mat");
        foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
        {
            t.font = font;
            t.fontSharedMaterial = mat;
            t.color = Color.white;
        }
    }
}
