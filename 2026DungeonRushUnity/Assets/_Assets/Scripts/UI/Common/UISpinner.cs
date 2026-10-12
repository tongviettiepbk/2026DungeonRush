using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Vòng xoay "đang chờ" (LoadingImage của các popup gốc): quay -360° mỗi giây, lặp vô hạn; tắt thì ẩn hình.
public static class UISpinner
{
    public static void Set(Image image, bool isOn)
    {
        RectTransform rect = image.rectTransform;
        rect.DOKill();
        image.gameObject.SetActive(isOn);
        if (isOn)
        {
            rect.localRotation = Quaternion.identity;
            rect.DORotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);
        }
    }
}
