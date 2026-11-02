using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1 dòng tin trong trang chat (ChatMessageItem gốc, prefab ChatMessageItemUI): avatar, tên, nội dung, giờ HH:mm theo giờ máy.
// Tin mình chưa được server xác nhận hiện mờ 0.5. Bấm avatar → hồ sơ người gửi (kèm nút Chặn/Báo cáo).
public class ChatMessageItem : MonoBehaviour
{
    [SerializeField] private Button _avatarButton;
    [SerializeField] private Image _avatarImage;
    [SerializeField] private TextMeshProUGUI _usernameText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private TextMeshProUGUI _timeText;
    [SerializeField] private CanvasGroup _canvasGroup;

    private static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const float PENDING_ALPHA = 0.5f;

    private ChatMessageModel message;
    private Action<ChatMessageModel> onClickAvatar;

    public ChatMessageModel Message => message;

    // fiv gốc.
    public void Bind(ChatMessageModel message, Action<ChatMessageModel> onClickAvatar)
    {
        this.message = message;
        this.onClickAvatar = onClickAvatar;

        if (_usernameText != null) _usernameText.text = message.Username;
        if (_messageText != null) _messageText.text = message.Message;
        if (_timeText != null)
        {
            _timeText.text = message.Timestamp > 0 ? EPOCH.AddSeconds(message.Timestamp).ToLocalTime().ToString("HH:mm") : string.Empty;
        }
        if (_avatarImage != null) _avatarImage.sprite = AvatarConfig.Get(message.AvatarId);
        if (_canvasGroup != null) _canvasGroup.alpha = message.IsLocal ? PENDING_ALPHA : 1f;

        if (_avatarButton != null)
        {
            _avatarButton.onClick.RemoveAllListeners();
            if (!message.IsItemDrop) _avatarButton.onClick.AddListener(OnAvatarClicked);
        }
    }

    // fix gốc.
    private void OnAvatarClicked()
    {
        if (message == null || string.IsNullOrEmpty(message.UserId) || message.IsLocal)
        {
            return;
        }
        onClickAvatar?.Invoke(message);
    }
}
