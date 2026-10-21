using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Port PressButtonUI gốc (reverse il2cpp v59, v41 cùng field trừ StayPressedWhenDisabled):
//  • Nhấn xuống nút đang bấm được: con ĐẦU TIÊN của nút (thường là "Content", nút đóng là "Image") nhảy tới
//    anchoredPosition.y = PressYValue và kêu AudioConfig.ButtonClickSound — OnPointerDown/fhl.
//  • Thả ra: con đó về y ban đầu (lưu ở Awake) — OnPointerUp/fhk.
//  • Nút bị khoá (không interactable) thì luôn về trạng thái nổi — Update.
// Gốc chỉ gắn trên một phần nút (nút hành động/đóng); ô danh sách, checkbox, mũi tên, tab, nền tối thì không.
// Danh sách nút + PressYValue gốc: UI/Common/Editor/press_buttons.json (menu Tools/DungeonRush/Apply Press Buttons).
// Bỏ phần giữ nút (IsHoldEnabled/ButtonHoldDown): prefab gốc đều tắt.
[RequireComponent(typeof(Button))]
public class PressButtonUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float PressYValue = -20f;   // y của con đầu tiên khi đang nhấn (gốc phần lớn -20, nút đóng -8)

    private Button button;
    private RectTransform content;     // yct gốc
    private float startY;              // ycr gốc
    private bool isPressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (transform.childCount > 0)
        {
            content = transform.GetChild(0) as RectTransform;
        }
        if (content != null)
        {
            startY = content.anchoredPosition.y;
        }
    }

    private void Update()
    {
        if (isPressed && !button.IsInteractable())
        {
            Release();
        }
    }

    // Gốc không có: nút bị tắt giữa lúc đang nhấn thì lần hiện sau vẫn lún → trả về nổi.
    private void OnDisable()
    {
        if (isPressed)
        {
            Release();
        }
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
        if (content != null)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, PressYValue);
        }
    }

    private void Release()
    {
        isPressed = false;
        if (content != null)
        {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, startY);
        }
    }
}
