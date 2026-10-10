using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lớp phủ tải/lỗi dùng chung của Clan (ClanLoadingView gốc): ShowLoading = chặn bấm + icon xoay (SpinSeconds/vòng),
// ShowError = chữ lỗi + nút Retry, Hide = tắt hết.
public class ClanLoadingView : MonoBehaviour
{
    public GameObject ContentRoot;
    public GameObject Blocker;
    public Image LoadingImage;
    public GameObject ErrorRoot;
    public TMP_Text ErrorText;
    public Button RetryButton;
    public TMP_Text RetryButtonText;
    public float SpinSeconds = 1f;

    private Tween spinTween;
    private Action onRetry;

    private void Awake()
    {
        if (RetryButton != null) RetryButton.onClick.AddListener(OnRetryClicked);
        if (RetryButtonText != null) RetryButtonText.text = "Retry";
    }

    private void OnDestroy()
    {
        spinTween?.Kill();
    }

    // fuc gốc.
    public void ShowLoading()
    {
        gameObject.SetActive(true);
        SetContent(false);
        if (Blocker != null) Blocker.SetActive(true);
        if (ErrorRoot != null) ErrorRoot.SetActive(false);
        SetSpin(true);
    }

    // fud gốc.
    public void Hide()
    {
        SetSpin(false);
        if (Blocker != null) Blocker.SetActive(false);
        if (ErrorRoot != null) ErrorRoot.SetActive(false);
        SetContent(true);
        gameObject.SetActive(false);
    }

    // fue gốc.
    public void ShowError(string message, Action retry)
    {
        onRetry = retry;
        gameObject.SetActive(true);
        SetContent(false);
        SetSpin(false);
        if (Blocker != null) Blocker.SetActive(true);
        if (ErrorRoot != null) ErrorRoot.SetActive(true);
        if (ErrorText != null) ErrorText.text = message;
        if (RetryButton != null) RetryButton.gameObject.SetActive(retry != null);
    }

    private void SetContent(bool visible)
    {
        if (ContentRoot != null) ContentRoot.SetActive(visible);
    }

    private void SetSpin(bool on)
    {
        if (LoadingImage == null) return;
        LoadingImage.gameObject.SetActive(on);
        spinTween?.Kill();
        spinTween = null;
        if (on)
        {
            LoadingImage.rectTransform.localEulerAngles = Vector3.zero;
            spinTween = LoadingImage.rectTransform
                .DOLocalRotate(new Vector3(0f, 0f, -360f), Mathf.Max(0.1f, SpinSeconds), RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);
        }
    }

    private void OnRetryClicked()
    {
        Action retry = onRetry;
        onRetry = null;
        retry?.Invoke();
    }
}
