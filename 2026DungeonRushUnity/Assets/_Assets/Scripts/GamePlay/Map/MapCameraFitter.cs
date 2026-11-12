using UnityEngine;

// Căn camera theo tỉ lệ màn hình: map (lưới + 1 vòng tường) nằm GIỮA vùng trống giữa thanh trên (TopBarUI)
// và cụm dưới (VerticalBottomLayout) của lobby, zoom (orthographicSize) cho vừa cả ngang lẫn dọc.
// Lý do: camera scene đặt cứng (ortho 16.8) nhưng UI bám mép trên/dưới theo CanvasScaler → màn dài
// (20:9, 22:10) thanh trên đè hàng trên của map, màn ngắn (16:9) map lệch xuống.
// Tự đo lại khi đổi kích thước màn hình hoặc khi vùng UI đổi (layout/SafeArea xong muộn 1-2 frame).
[RequireComponent(typeof(Camera))]
public class MapCameraFitter : MonoBehaviour
{
    private const float WALL_THICKNESS = 1f;   // vòng tường dày 1 ô quanh lưới (MainMap bám ô)
    private const float PADDING = 0.25f;       // chừa thêm mỗi phía quanh tường (world unit)
    private const float SIDE_MARGIN_CANVAS = 120f; // chừa mỗi bên (đơn vị canvas) cho cột nút offer phải / vé trái

    private Camera cam;
    private float defaultSize;
    private Vector3 defaultPosition;

    private MapController map;
    private float lastTop, lastBottom;
    private float sideMarginPx;   // lề ngang mỗi bên theo pixel (đo cùng lúc với vùng trống)
    private int lastWidth, lastHeight;

    private static MapCameraFitter current;

    // Gọi sau MapController.SetMap: bám camera theo map này cho tới khi Release.
    public static void Fit(MapController map)
    {
        MapCameraFitter fitter = Get();
        if (fitter == null || map == null || !map.IsReady) return;
        fitter.map = map;
        fitter.lastWidth = 0;   // ép đo lại ngay
        fitter.Apply();
    }

    // Mode tự lo camera (Boss Rush bám hero) → trả camera về cỡ/vị trí gốc của scene.
    public static void Release()
    {
        MapCameraFitter fitter = Get();
        if (fitter == null) return;
        fitter.map = null;
        fitter.cam.orthographicSize = fitter.defaultSize;
        fitter.transform.position = fitter.defaultPosition;
    }

    private static MapCameraFitter Get()
    {
        if (current != null) return current;
        Camera main = Camera.main;
        if (main == null) return null;
        current = main.GetComponent<MapCameraFitter>();
        if (current == null) current = main.gameObject.AddComponent<MapCameraFitter>();
        return current;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        defaultSize = cam.orthographicSize;
        defaultPosition = transform.position;
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void LateUpdate()
    {
        if (map == null || !map.IsReady) return;
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            Apply();
            return;
        }
        float top, bottom;
        if (TryGetFreeArea(out top, out bottom) && (Mathf.Abs(top - lastTop) > 1f || Mathf.Abs(bottom - lastBottom) > 1f))
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float top, bottom;
        if (!TryGetFreeArea(out top, out bottom))
        {
            // Chưa có UI lobby → coi cả màn là vùng trống.
            top = Screen.height;
            bottom = 0f;
            sideMarginPx = 0f;
        }
        lastTop = top;
        lastBottom = bottom;

        // Khung map world: tâm ô góc ± nửa ô + tường + chừa.
        float extra = 0.5f + WALL_THICKNESS + PADDING;
        Vector3 a = map.CellToWorld(0, 0);
        Vector3 b = map.CellToWorld(map.Rows - 1, map.Cols - 1);
        Vector3 min = Vector3.Min(a, b) - new Vector3(extra, extra, 0f);
        Vector3 max = Vector3.Max(a, b) + new Vector3(extra, extra, 0f);
        Vector3 size = max - min;
        Vector3 center = (min + max) * 0.5f;

        // Số pixel / 1 world unit lớn nhất mà map vẫn vừa vùng trống.
        float availWidth = Mathf.Max(1f, Screen.width - 2f * sideMarginPx);
        float availHeight = Mathf.Max(1f, top - bottom);
        float pixelsPerUnit = Mathf.Min(availWidth / size.x, availHeight / size.y);
        if (pixelsPerUnit <= 0f) return;

        cam.orthographicSize = Screen.height / (2f * pixelsPerUnit);

        // Tâm map trùng tâm vùng trống (lệch so với tâm màn hình).
        float offsetPx = (top + bottom) * 0.5f - Screen.height * 0.5f;
        Vector3 pos = transform.position;
        pos.x = center.x;
        pos.y = center.y - offsetPx / pixelsPerUnit;
        transform.position = pos;
    }

    // Vùng trống theo pixel màn hình (y tính từ đáy): top = mép dưới TopBarUI, bottom = mép trên cao nhất
    // của các con đang bật trong VerticalBottomLayout. Đo trong toạ độ canvas gốc → không phụ thuộc camera.
    private bool TryGetFreeArea(out float top, out float bottom)
    {
        top = Screen.height;
        bottom = 0f;
        UIMainLobby lobby = GameController.Instance != null ? GameController.Instance.uiLobby : null;
        if (lobby == null || lobby.rectTopBar == null || lobby.rectBottomLayout == null) return false;
        if (!lobby.rectTopBar.gameObject.activeInHierarchy || !lobby.rectBottomLayout.gameObject.activeInHierarchy) return false;

        Canvas canvas = lobby.rectTopBar.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        RectTransform root = (RectTransform)canvas.rootCanvas.transform;
        Rect rootRect = root.rect;
        if (rootRect.height <= 0f) return false;

        float topLocal = MinMaxY(root, lobby.rectTopBar, true);
        float bottomLocal = float.MinValue;
        foreach (RectTransform child in lobby.rectBottomLayout)
        {
            if (!child.gameObject.activeSelf) continue;
            bottomLocal = Mathf.Max(bottomLocal, MinMaxY(root, child, false));
        }
        if (bottomLocal == float.MinValue) bottomLocal = MinMaxY(root, lobby.rectBottomLayout, false);

        sideMarginPx = SIDE_MARGIN_CANVAS / rootRect.width * Screen.width;
        top = (topLocal - rootRect.yMin) / rootRect.height * Screen.height;
        bottom = (bottomLocal - rootRect.yMin) / rootRect.height * Screen.height;
        return top > bottom;
    }

    private static readonly Vector3[] corners = new Vector3[4];

    // y nhỏ nhất (isMin) hoặc lớn nhất của 4 góc rect, quy về toạ độ local của canvas gốc.
    private static float MinMaxY(RectTransform root, RectTransform rect, bool isMin)
    {
        rect.GetWorldCorners(corners);
        float result = isMin ? float.MaxValue : float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            float y = root.InverseTransformPoint(corners[i]).y;
            result = isMin ? Mathf.Min(result, y) : Mathf.Max(result, y);
        }
        return result;
    }
}
