using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Gốc PressButtonUI (reverse il2cpp v59) lún con đầu tiên xuống PressYValue; đã đổi theo yêu cầu (2026-10-28):
//  • Nhấn xuống nút đang bấm được: CẢ nút thu nhỏ về PressScale × scale ban đầu và kêu AudioConfig.ButtonClickSound.
//  • Thả ra: nút phóng to lại scale ban đầu (lưu ở Awake).
//  • Nút bị khoá (không interactable) thì luôn về scale ban đầu — Update.
// Gốc chỉ gắn trên một phần nút (nút hành động/đóng); ô danh sách, checkbox, mũi tên, tab, nền tối thì không.
// Danh sách nút gốc: UI/Common/Editor/press_buttons.json (menu Tools/DungeonRush/Apply Press Buttons).
// Bỏ phần giữ nút (IsHoldEnabled/ButtonHoldDown): prefab gốc đều tắt.
[RequireComponent(typeof(Button))]
public class PressButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float PressScale = 0.9f;        // tỉ lệ scale khi đang nhấn
    public float PressDuration = 0.08f;    // thời gian thu nhỏ
    public float ReleaseDuration = 0.1f;   // thời gian phóng to lại

    private Button button;
    private Vector3 startScale;
    private bool isPressed;
    private Tween tween;

    private void Awake()
    {
        button = GetComponent<Button>();
        startScale = transform.localScale;
    }

    private void Update()
    {
        if (isPressed && !button.IsInteractable())
        {
            Release();
        }
    }

    // Nút bị tắt giữa lúc đang nhấn thì lần hiện sau vẫn nhỏ → trả về scale ban đầu ngay.
    private void OnDisable()
    {
        isPressed = false;
        tween?.Kill();
        transform.localScale = startScale;
    }

    private void OnDestroy()
    {
        tween?.Kill();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.IsInteractable())
        {
            return;
        }

        Press();
        AudioManager.Instance.PlayButtonClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    private void Press()
    {
        isPressed = true;
        tween?.Kill();
        tween = transform.DOScale(startScale * PressScale, PressDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void Release()
    {
        isPressed = false;
        tween?.Kill();
        tween = transform.DOScale(startScale, ReleaseDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }
}
