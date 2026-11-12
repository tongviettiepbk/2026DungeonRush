using UnityEngine;

// Gắn vào khung popup (neo giữa): thu nhỏ đều cho vừa vùng cha khi màn thấp (iPad 4:3, máy 16:9...),
// không phóng to quá kích thước gốc. Canvas match theo chiều rộng nên chiều cao canvas đổi theo tỉ lệ màn.
[RequireComponent(typeof(RectTransform))]
public class UIFitInParent : MonoBehaviour
{
    public float margin = 40f; // khoảng hở mỗi phía, đơn vị canvas

    private RectTransform rect;
    private Vector2 lastParentSize;

    private void OnEnable()
    {
        Fit();
    }

    // Đổi kích thước cửa sổ / xoay màn: vùng cha đổi thì tính lại.
    private void LateUpdate()
    {
        RectTransform parent = transform.parent as RectTransform;
        if (parent != null && parent.rect.size != lastParentSize)
            Fit();
    }

    private void Fit()
    {
        if (rect == null)
            rect = (RectTransform)transform;
        RectTransform parent = transform.parent as RectTransform;
        if (parent == null)
            return;

        lastParentSize = parent.rect.size;
        Vector2 size = rect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return;

        float scale = Mathf.Min(1f,
            (lastParentSize.x - margin * 2f) / size.x,
            (lastParentSize.y - margin * 2f) / size.y);
        rect.localScale = new Vector3(scale, scale, 1f);
    }
}
