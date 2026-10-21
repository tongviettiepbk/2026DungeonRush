using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Phần âm thanh của PressButtonUI gốc (OnPointerDown): nhấn xuống nút đang bấm được → AudioConfig.ButtonClickSound.
// Gốc chỉ gắn PressButtonUI trên một phần nút (nút hành động/đóng); ô danh sách, checkbox, mũi tên, nền tối thì im.
// Danh sách nút gắn theo gốc: Audio/Editor/button_click_sfx.json (menu Tools/DungeonRush/Apply Button Click Sfx).
[RequireComponent(typeof(Button))]
public class ButtonClickSfx : MonoBehaviour, IPointerDownHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable())
        {
            AudioManager.Instance.PlayButtonClick();
        }
    }
}
