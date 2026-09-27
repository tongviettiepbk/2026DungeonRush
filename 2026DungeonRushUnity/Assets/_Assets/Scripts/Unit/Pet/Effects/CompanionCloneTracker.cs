using UnityEngine;

// Theo dõi bản sao hero (gốc CompanionCloneTracker): làm mờ hình (CloneAlpha/CloneBrightness),
// đếm CloneLifetime theo thời gian trận rồi gỡ khỏi trận (Deactive — không phát UnitDie) và huỷ.
// Bản sao chết sớm cũng bị huỷ sau khi hết thời gian.
public class CompanionCloneTracker : MonoBehaviour
{
    private BaseUnit clone;
    private float remain;

    public static void Attach(BaseUnit clone, float lifetime, float alpha, float brightness)
    {
        CompanionCloneTracker tracker = clone.gameObject.AddComponent<CompanionCloneTracker>();
        tracker.clone = clone;
        tracker.remain = lifetime;
        tracker.Tint(alpha, brightness);
    }

    private void Tint(float alpha, float brightness)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Color c = renderers[i].color;
            renderers[i].color = new Color(
                Mathf.Clamp01(c.r * brightness), Mathf.Clamp01(c.g * brightness), Mathf.Clamp01(c.b * brightness), c.a * alpha);
        }
    }

    private void Update()
    {
        if (clone != null && clone.isPause)
        {
            return;
        }

        remain -= Time.deltaTime * GameController.Instance.gameSpeed;
        if (remain > 0f)
        {
            return;
        }

        if (clone != null && clone.gameObject.activeSelf)
        {
            clone.Deactive();
        }
        Destroy(gameObject);
    }
}
