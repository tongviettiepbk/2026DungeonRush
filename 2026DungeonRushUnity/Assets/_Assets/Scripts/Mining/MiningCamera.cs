using System;
using DG.Tweening;
using UnityEngine;

// Camera orthographic riêng của mỏ (MiningCamera gốc), render vào RenderTexture của popup.
// orthographicSize = VisibleRows·CellSize/2; tâm = giữa GridWidth cột, hàng (VisibleRows−1)/2 + depth.
public class MiningCamera : MonoBehaviour
{
    public Camera miningCam;

    private MiningConfig config;
    private int depth;
    private Tween moveTween;
    private Vector3 origin;

    private const float MIN_TWEEN_DURATION = 0.1f;

    // ixt: origin = vị trí BlockParent lúc vào mỏ.
    public void Init(MiningConfig cfg, int currentDepth, Transform blockParent)
    {
        config = cfg;
        depth = currentDepth;
        origin = blockParent.position;
        if (miningCam != null)
        {
            miningCam.orthographicSize = config.VisibleRows * config.CellSize * 0.5f;
        }
        SnapToDepth(currentDepth);
    }

    // ixu
    private Vector3 GetPosition(int atDepth)
    {
        float cs = config.CellSize;
        float x = origin.x + (config.GridWidth - 1) * cs * 0.5f;
        float y = origin.y - cs * ((config.VisibleRows - 1) * 0.5f + atDepth);
        return new Vector3(x, y, -10f);
    }

    // ixv
    public void SnapToDepth(int atDepth)
    {
        depth = atDepth;
        transform.position = GetPosition(atDepth);
    }

    // ixw: trượt xuống độ sâu mới với tốc độ `speed` hàng/giây (≤0 → CameraScrollSpeed), tối đa 1s, tối thiểu 0.1s.
    public void ScrollToDepth(int atDepth, Action onComplete = null, float speed = 0f)
    {
        if (moveTween != null)
        {
            moveTween.Kill();
        }

        depth = atDepth;
        Vector3 target = GetPosition(atDepth);
        if (speed <= 0f)
        {
            speed = config.CameraScrollSpeed;
        }

        float duration = Mathf.Abs(transform.position.y - target.y) / speed;
        duration = duration < MIN_TWEEN_DURATION ? MIN_TWEEN_DURATION : Mathf.Min(duration, 1f);
        moveTween = transform.DOMove(target, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            moveTween = null;
            onComplete?.Invoke();
        });
    }

    // ixx
    public int Depth => depth;

    // ixy
    public void Stop()
    {
        if (moveTween != null)
        {
            moveTween.Kill();
            moveTween = null;
        }
    }

    private void OnDestroy()
    {
        Stop();
    }
}
