using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Port ForgeButton gốc (nút Loot ở lobby): ấn xuống thì nắp Top lún xuống + đổi màu xám,
// nhả thì nảy về màu đỏ. Số liệu mặc định = giá trị serialized của bản gốc.
//  - Press (ibt): Top tụt (Move+Overshoot) trong Press*0.6 OutQuad → lên lại -Move trong Press*0.4 OutBounce; Shadow tương tự.
//  - Release (ibu): Top/Shadow về chỗ cũ + màu về mặc định trong Press*0.5 OutBounce.
//  - Khi ForgeUIController khoá (đang loot/auto) thì nút đứng ở trạng thái lún, không nhận nhấn.
public class ForgeButtonView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image topImage;
    public Image bottomImage;
    public RectTransform topTransform;
    public RectTransform shadowTransform;
    public float topMoveDownAmount = 12f;
    public float overshootAmount = 10f;
    public float shadowMoveDownAmount = 4f;
    public float shadowOvershootAmount = 2f;
    public float pressDuration = 0.2f;
    public Color defaultTopColor = new Color(0.8f, 0.10588236f, 0.20784315f, 1f);
    public Color pressedTopColor = new Color(0.4039216f, 0.4039216f, 0.4039216f, 1f);
    public Color defaultBottomColor = new Color(0.47450984f, 0.027450982f, 0.098039225f, 1f);
    public Color pressedBottomColor = new Color(0.16862746f, 0.16862746f, 0.16862746f, 1f);

    // Nhả tay trên nút (OnReleased gốc) — kể cả khi kéo ra ngoài rồi thả.
    public Action onReleased;

    private float topStartY;
    private float shadowStartY;
    private Tween tween;
    private bool interactable = true;

    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    private void Awake()
    {
        if (topTransform != null)
            topStartY = topTransform.anchoredPosition.y;
        if (shadowTransform != null)
            shadowStartY = shadowTransform.anchoredPosition.y;
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactable)
            Press();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (interactable)
            onReleased?.Invoke();
    }

    public void Press()
    {
        if (topImage != null) topImage.color = pressedTopColor;
        if (bottomImage != null) bottomImage.color = pressedBottomColor;

        tween?.Kill();
        Sequence seq = DOTween.Sequence();
        if (topTransform != null)
        {
            seq.Append(topTransform.DOAnchorPosY(topStartY - topMoveDownAmount - overshootAmount, pressDuration * 0.6f).SetEase(Ease.OutQuad));
            seq.Append(topTransform.DOAnchorPosY(topStartY - topMoveDownAmount, pressDuration * 0.4f).SetEase(Ease.OutBounce));
        }
        if (shadowTransform != null)
        {
            seq.Insert(0f, shadowTransform.DOAnchorPosY(shadowStartY - shadowMoveDownAmount - shadowOvershootAmount, pressDuration * 0.6f).SetEase(Ease.OutQuad));
            seq.Insert(pressDuration * 0.6f, shadowTransform.DOAnchorPosY(shadowStartY - shadowMoveDownAmount, pressDuration * 0.4f).SetEase(Ease.OutBounce));
        }
        tween = seq;
    }

    public void Release()
    {
        tween?.Kill();
        float duration = pressDuration * 0.5f;
        Sequence seq = DOTween.Sequence();
        if (topTransform != null)
            seq.Append(topTransform.DOAnchorPosY(topStartY, duration).SetEase(Ease.OutBounce));
        if (topImage != null)
            seq.Join(topImage.DOColor(defaultTopColor, duration));
        if (bottomImage != null)
            seq.Join(bottomImage.DOColor(defaultBottomColor, duration));
        if (shadowTransform != null)
            seq.Join(shadowTransform.DOAnchorPosY(shadowStartY, duration).SetEase(Ease.OutBounce));
        tween = seq;
    }
}
