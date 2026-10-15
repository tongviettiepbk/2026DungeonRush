using DG.Tweening;
using UnityEngine;

// Hiệu ứng pattern chạy trên nền ô item rarity Ancient / Immortal / Divine (RarityPatternEffect gốc, reverse il2cpp).
// Pattern nằm trong node Mask (UnityEngine.UI.Mask) nên chỉ lộ trong khung ô.
//  - MoveDown        : trượt anchoredPosition.y xuống MoveDistance       (Ancient)
//  - Spiral          : xoay localRotation.z -RotationDegrees              (Immortal)
//  - MoveDiagonalDown: trượt chéo xuống-trái MoveDistance trên cả x và y (Divine)
// Lặp vô hạn kiểu Restart; tắt object thì trả về vị trí/góc ban đầu.
[RequireComponent(typeof(RectTransform))]
public class RarityPatternEffect : MonoBehaviour
{
    public enum PatternAnimation
    {
        MoveDown = 0,
        Spiral = 1,
        MoveDiagonalDown = 2,
    }

    public PatternAnimation animationType;
    public float duration = 5f;
    public float moveDistance;
    public float rotationDegrees;
    public Ease ease = Ease.Linear;

    private RectTransform rect;
    private Vector2 startPosition;
    private Quaternion startRotation;
    private bool hasStart;
    private Tween tween;

    private void Awake()
    {
        rect = (RectTransform)transform;
        CacheStart();
    }

    private void OnEnable()
    {
        if (rect == null)
            rect = (RectTransform)transform;
        CacheStart();
        ResetToStart();
        Play();
    }

    private void OnDisable()
    {
        tween?.Kill();
        tween = null;
        ResetToStart();
    }

    private void CacheStart()
    {
        if (hasStart)
            return;
        startPosition = rect.anchoredPosition;
        startRotation = rect.localRotation;
        hasStart = true;
    }

    private void ResetToStart()
    {
        if (!hasStart)
            return;
        rect.anchoredPosition = startPosition;
        rect.localRotation = startRotation;
    }

    private void Play()
    {
        tween?.Kill();

        switch (animationType)
        {
            case PatternAnimation.MoveDown:
                tween = rect.DOAnchorPosY(startPosition.y - moveDistance, duration).SetEase(ease);
                break;
            case PatternAnimation.MoveDiagonalDown:
                tween = rect.DOAnchorPos(startPosition - new Vector2(moveDistance, moveDistance), duration).SetEase(ease);
                break;
            case PatternAnimation.Spiral:
                tween = rect.DOLocalRotate(new Vector3(0f, 0f, -rotationDegrees), duration, RotateMode.FastBeyond360).SetEase(ease);
                break;
            default:
                return;
        }

        // Gốc có SetLink(gameObject); DOTween bản này chưa có → OnDisable tự Kill.
        tween.SetLoops(-1, LoopType.Restart);
    }
}
