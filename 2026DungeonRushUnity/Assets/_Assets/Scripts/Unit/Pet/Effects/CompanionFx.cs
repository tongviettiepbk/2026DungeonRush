using UnityEngine;

// Helper hình ảnh cho companion: FX nổ/va chạm (prefab particle đã import, CHƯA gắn BaseFx nên không
// đi qua FxController) và vật liệu mặc định cho LineRenderer.
public static class CompanionFx
{
    // Sinh FX tại 'position', phóng theo 'scale' (bán kính nổ), tự huỷ sau 'lifetime' giây.
    public static void Spawn(GameObject prefab, Vector3 position, float scale, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject fx = Object.Instantiate(prefab, position, Quaternion.identity);
        if (scale > 0f)
        {
            fx.transform.localScale = prefab.transform.localScale * scale;
        }
        Object.Destroy(fx, Mathf.Max(0.1f, lifetime));
    }

    private static Material lineMaterial;

    public static Material LineMaterial
    {
        get
        {
            if (lineMaterial == null)
            {
                // Ưu tiên shader sprite của URP (luôn có trong build vì sprite dùng); built-in là dự phòng.
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
                lineMaterial = new Material(shader);
            }
            return lineMaterial;
        }
    }

    public static LineRenderer CreateLine(Transform parent, string name, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.material = LineMaterial;
        line.useWorldSpace = true;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.sortingOrder = 500;
        line.numCapVertices = 2;
        line.positionCount = 0;
        line.enabled = false;
        return line;
    }
}
