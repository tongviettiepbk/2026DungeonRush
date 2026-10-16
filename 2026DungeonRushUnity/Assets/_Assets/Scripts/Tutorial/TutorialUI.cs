using DG.Tweening;
using UnityEngine;

// Tay chỉ tutorial gắn sẵn trong prefab màn (TutorialUI gốc, reverse il2cpp v41 kxf..kxj).
// Show: phóng _child từ 0 lên scale (OutBack) rồi nhún lên/xuống vô hạn; Hide: thu về 0.
// Gốc dùng SetLink(gameObject); DOTween trong project chưa có SetLink nên tự giữ tween và kill ở OnDestroy.
// Giá trị mặc định = giá trị serialized phổ biến trong prefab gốc (8 node gốc dùng moveDistance 15).
public class TutorialUI : MonoBehaviour
{
    [SerializeField] private Transform _child;
    [SerializeField] private float _moveDistance = 30f;
    [SerializeField] private float _moveDuration = 0.5f;
    [SerializeField] private float _scaleDuration = 0.25f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;

    private Vector3 originalPosition;
    private Tween scaleTween;   // phóng to khi Show / thu nhỏ khi Hide
    private Tween loopTween;    // nhún lên xuống
    private bool isShowing;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (_child == null)
        {
            return;
        }

        originalPosition = _child.localPosition;
        _child.localScale = Vector3.zero;
    }

    public void Show(float scale = 1f)
    {
        if (TutorialService.IsEnabled == false || isShowing)
        {
            return;
        }

        isShowing = true;
        scaleTween?.Kill();
        loopTween?.Kill();

        _child.localPosition = originalPosition;
        _child.localScale = Vector3.zero;

        scaleTween = DOTween.Sequence()
            .Append(_child.DOScale(Vector3.one * scale, _scaleDuration).SetEase(_scaleEase))
            .OnComplete(StartLoop);
    }

    public void Hide()
    {
        if (isShowing == false)
        {
            return;
        }

        isShowing = false;
        scaleTween?.Kill();
        loopTween?.Kill();
        loopTween = null;

        scaleTween = _child.DOScale(Vector3.zero, _scaleDuration).SetEase(_scaleEase);
    }

    // Tiện cho nơi gọi: điều kiện đúng thì Show, sai thì Hide (mẫu `if (...) kxh() else kxi()` lặp khắp code gốc).
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void StartLoop()
    {
        loopTween = _child.DOLocalMoveY(originalPosition.y + _moveDistance, _moveDuration)
            .SetEase(_moveEase)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        loopTween?.Kill();
    }
}
