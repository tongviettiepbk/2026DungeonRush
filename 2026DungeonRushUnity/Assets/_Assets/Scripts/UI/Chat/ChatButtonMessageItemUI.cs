using TMPro;
using UnityEngine;

// 1 dòng xem trước trên nút chat ở lobby (ChatButtonMessageItemUI gốc): "Tên: nội dung".
public class ChatButtonMessageItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    // jxi gốc.
    public void Bind(ChatMessageModel message)
    {
        gameObject.SetActive(true);
        if (_text != null) _text.text = message.Username + ": " + message.Message;
    }

    public void Clear()
    {
        if (_text != null) _text.text = string.Empty;
        gameObject.SetActive(false);
    }
}
