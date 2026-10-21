using DG.Tweening;
using UnityEngine;

// FX vẽ dưới chân cho skill boss Boss Rush — port BossRushGroundFX gốc v59: sprite tự vẽ bằng code (128px) tròn / vành / nón,
// sortingOrder −10 (dưới nhân vật), tween DOTween. Skill dùng khi định nghĩa boss không có prefab FX riêng
// (gốc: Lich có prefab Slam, rồng có prefab FireBreath — project chưa có nên luôn vẽ bằng code).
public class BossRushGroundFX : MonoBehaviour
{
    private const int TEXTURE_SIZE = 128;      // yfh
    private const int SORTING_ORDER = -10;     // yfi
    private const float RING_THICKNESS = 0.12f;
    private const float CONE_FALLOFF = 0.35f;

    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite coneSprite;
    private static float coneSpriteAngle;

    private SpriteRenderer spriteRenderer;

    // fku: vòng báo trước (đỏ) cỡ đường kính 2R thu dần về 2 × innerRadius (≥ 0.2) trong duration, ease InQuad.
    public static BossRushGroundFX Telegraph(Vector2 position, float radius, float innerRadius, float duration)
    {
        BossRushGroundFX fx = Create(position, GetCircleSprite(), new Color(1f, 0.15f, 0.1f, 0.5f));
        fx.transform.localScale = Vector3.one * (radius * 2f);
        fx.transform.DOScale(Mathf.Max(innerRadius * 2f, 0.2f), duration).SetEase(Ease.InQuad);
        return fx;
    }

    // fkv: vành loang từ bán kính from tới to trong duration (OutQuad), mờ dần (InQuad) rồi tự huỷ.
    public static void Ring(Vector2 position, float from, float to, float duration, Color color)
    {
        BossRushGroundFX fx = Create(position, GetRingSprite(), color);
        fx.transform.localScale = Vector3.one * (from * 2f);
        fx.transform.DOScale(to * 2f, duration).SetEase(Ease.OutQuad);
        fx.spriteRenderer.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(fx.DestroySelf);
    }

    // fkw: chớp tròn bán kính radius bung ra rồi mờ trong duration.
    public static void Flash(Vector2 position, float radius, float duration, Color color)
    {
        BossRushGroundFX fx = Create(position, GetCircleSprite(), color);
        fx.transform.localScale = Vector3.one * (radius * 0.5f);
        fx.transform.DOScale(radius * 2f, duration).SetEase(Ease.OutQuad);
        fx.spriteRenderer.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(fx.DestroySelf);
    }

    // fkx: nón lửa (đỉnh ở position, hướng +Y khi góc 90°) dài radius, mở coneAngle độ.
    public static BossRushGroundFX Cone(Vector2 position, float radius, float coneAngle, Color color)
    {
        BossRushGroundFX fx = Create(position, GetConeSprite(coneAngle), color);
        fx.transform.localScale = Vector3.one * radius;
        return fx;
    }

    // fky: quay nón theo góc thế giới (độ, 0 = +X).
    public void SetAngle(float angle)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    // fla: mờ nhanh rồi huỷ.
    public void Dismiss()
    {
        transform.DOKill();
        spriteRenderer.DOKill();
        spriteRenderer.DOFade(0f, 0.15f).OnComplete(DestroySelf);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
        }
    }

    // flb
    private static BossRushGroundFX Create(Vector2 position, Sprite sprite, Color color)
    {
        var go = new GameObject("BossRushGroundFX");
        go.transform.position = new Vector3(position.x, position.y, 0f);
        BossRushGroundFX fx = go.AddComponent<BossRushGroundFX>();
        fx.spriteRenderer = go.AddComponent<SpriteRenderer>();
        fx.spriteRenderer.sprite = sprite;
        fx.spriteRenderer.color = color;
        fx.spriteRenderer.sortingOrder = SORTING_ORDER;
        return fx;
    }

    // ===== Sprite vẽ bằng code (1 đơn vị thế giới = cả texture) =====

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
        {
            circleSprite = BuildSprite((x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                return d > 1f ? 0f : Mathf.Clamp01((1f - d) * 6f) * (0.55f + 0.45f * d);
            }, new Vector2(0.5f, 0.5f));
        }
        return circleSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (ringSprite == null)
        {
            ringSprite = BuildSprite((x, y) =>
            {
                float d = Mathf.Sqrt(x * x + y * y);
                float edge = Mathf.Abs(d - (1f - RING_THICKNESS)) / RING_THICKNESS;
                return d > 1f ? 0f : Mathf.Clamp01(1f - edge);
            }, new Vector2(0.5f, 0.5f));
        }
        return ringSprite;
    }

    // Nón: đỉnh ở giữa cạnh dưới texture, mở đều 2 bên trục +Y; sprite cao 1 đơn vị = tầm phun.
    private static Sprite GetConeSprite(float coneAngle)
    {
        if (coneSprite == null || !Mathf.Approximately(coneSpriteAngle, coneAngle))
        {
            float half = coneAngle * 0.5f;
            coneSpriteAngle = coneAngle;
            coneSprite = BuildSprite((x, y) =>
            {
                // x, y trong [-1, 1] → toạ độ tính từ đỉnh (giữa cạnh dưới), cao 1.
                float px = x * 0.5f;
                float py = (y + 1f) * 0.5f;
                float d = Mathf.Sqrt(px * px + py * py);
                if (d > 1f || py <= 0f) return 0f;
                float angle = Mathf.Abs(Mathf.Atan2(px, py) * Mathf.Rad2Deg);
                if (angle > half) return 0f;
                float side = Mathf.Clamp01((half - angle) / (half * CONE_FALLOFF));
                float tip = Mathf.Clamp01((1f - d) * 5f);
                return side * tip;
            }, new Vector2(0.5f, 0f));
        }
        return coneSprite;
    }

    private static Sprite BuildSprite(System.Func<float, float, float> alpha, Vector2 pivot)
    {
        var texture = new Texture2D(TEXTURE_SIZE, TEXTURE_SIZE, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        var pixels = new Color32[TEXTURE_SIZE * TEXTURE_SIZE];
        for (int row = 0; row < TEXTURE_SIZE; row++)
        {
            for (int col = 0; col < TEXTURE_SIZE; col++)
            {
                float x = (col + 0.5f) / TEXTURE_SIZE * 2f - 1f;
                float y = (row + 0.5f) / TEXTURE_SIZE * 2f - 1f;
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y)) * 255f);
                pixels[row * TEXTURE_SIZE + col] = new Color32(255, 255, 255, a);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, TEXTURE_SIZE, TEXTURE_SIZE), pivot, TEXTURE_SIZE);
    }
}
