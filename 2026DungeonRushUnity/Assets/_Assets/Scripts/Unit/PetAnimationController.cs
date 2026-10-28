using UnityEngine;

// Pet chỉ là 1 sprite "Icon" (không có Animator) → nhún idle bằng code, LUÔN chạy (đứng, đi, ra chiêu).
// Nhịp copy clip UnitIdle của hero (Animations/HeroesNew/UnitIdle.anim): scale Y thân 0.9 → 1.1 (0.517s) → 0.9 (1s),
// tiếp tuyến phẳng, scale X giữ nguyên. Sprite pet pivot ở GIỮA → bù vị trí để mép dưới (chân) đứng yên như hero.
public class PetAnimationController : AnimationController
{
    public const string BODY_NODE_NAME = "Icon";

    public Transform body;
    public float idleDuration = 1f;
    public AnimationCurve idleScaleY = new AnimationCurve(
        new Keyframe(0f, 0.9f, 0f, 0f),
        new Keyframe(0.51666665f, 1.1f, 0f, 0f),
        new Keyframe(1f, 0.9f, 0f, 0f));

    private Vector3 baseScale;
    private Vector3 basePosition;
    private float bottomY;      // mép dưới sprite trong hệ local của body (âm vì pivot giữa)
    private float time;
    private bool isPaused;

    private void Awake()
    {
        if (body == null)
        {
            body = transform.Find(BODY_NODE_NAME);
        }

        if (body == null)
        {
            return;
        }

        baseScale = body.localScale;
        basePosition = body.localPosition;
        SpriteRenderer sr = body.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            bottomY = sr.sprite.bounds.min.y;
        }
    }

    private void Update()
    {
        if (body == null || isPaused)
        {
            return;
        }

        time = (time + Time.deltaTime) % idleDuration;
        float scaleY = idleScaleY.Evaluate(time / idleDuration);

        body.localScale = new Vector3(baseScale.x, baseScale.y * scaleY, baseScale.z);
        // Giữ chân: pos.y + scale.y × bottomY không đổi so với lúc scale gốc.
        body.localPosition = basePosition + new Vector3(0f, bottomY * baseScale.y * (1f - scaleY), 0f);
    }

    public override void Pause()
    {
        isPaused = true;
    }

    public override void Resume()
    {
        isPaused = false;
    }

    public override void Freeze(bool isOn)
    {
        isPaused = isOn;
    }
}
