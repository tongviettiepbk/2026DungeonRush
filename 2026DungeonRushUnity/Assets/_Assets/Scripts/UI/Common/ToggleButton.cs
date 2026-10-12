using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Nút gạt bật/tắt (ToggleButton gốc): núm trượt sang phải khi bật, sang trái khi tắt và đổi màu; chữ ON/OFF nằm
// phía đối diện núm. Bấm thì đảo trạng thái rồi báo onChanged.
public class ToggleButton : MonoBehaviour
{
    private const float MOVE_DURATION = 0.15f;

    public Image ChildButtonImage;
    public RectTransform ParentRectTransform;
    public RectTransform ChildRectTransform;
    public Button Button;
    public TextMeshProUGUI StatusText;
    public float Padding;   // khoảng cách tâm núm tới mép khung

    public Action<bool> onChanged;

    private bool isOn;
    private bool isInitialized;
    private float textX;    // vị trí chữ lúc đang bật (bố cục prefab)

    private void Awake()
    {
        Initialize();
    }

    // Set có thể được gọi trước Awake (nút nằm trong trang đang tắt) → nhớ bố cục gốc ngay lần đầu.
    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
        textX = StatusText.rectTransform.anchoredPosition.x;
        Button.onClick.AddListener(OnButtonClicked);
    }

    // kwp gốc.
    public void Set(bool isOn, bool isAnimate = false)
    {
        Initialize();
        this.isOn = isOn;
        float knobX = ParentRectTransform.rect.width * 0.5f - Padding;
        if (isOn == false) knobX = -knobX;

        ChildRectTransform.DOKill();
        if (isAnimate)
        {
            ChildRectTransform.DOAnchorPosX(knobX, MOVE_DURATION).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        else
        {
            ChildRectTransform.anchoredPosition = new Vector2(knobX, ChildRectTransform.anchoredPosition.y);
        }

        ChildButtonImage.color = isOn ? UITabWing.DEFAULT_BUTTON_COLOR : UITabWing.DISABLED_BUTTON_COLOR;
        StatusText.text = isOn ? "ON" : "OFF";
        RectTransform text = StatusText.rectTransform;
        text.anchoredPosition = new Vector2(isOn ? textX : -textX, text.anchoredPosition.y);
    }

    private void OnButtonClicked()
    {
        Set(!isOn, true);
        onChanged?.Invoke(isOn);
    }
}
