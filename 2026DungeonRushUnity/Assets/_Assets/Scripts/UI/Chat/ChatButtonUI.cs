using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Nút chat ở đáy lobby (ChatButtonUI gốc, GameplayScene: UnlockPlayerLevel 6, 2 dòng xem trước tin Thế giới mới nhất).
// Bấm: đang bị cấm → báo thời hạn cấm; chưa đặt tên → popup đặt tên trước; xong mở trang chat.
public class ChatButtonUI : MonoBehaviour
{
    public Button ChatButton;
    public ChatButtonMessageItemUI[] MessageItems;

    private bool isVisible;

    private void Awake()
    {
        ChatButton.onClick.AddListener(OnChatButtonClicked);
        ChatController.MessagesUpdated += OnMessagesUpdated;
        ClearPreview();
    }

    private void OnDestroy()
    {
        ChatController.MessagesUpdated -= OnMessagesUpdated;
    }

    // jxm gốc: hiện khi đủ level; hiện thì bắt đầu nghe kênh Thế giới để có tin xem trước.
    public void Refresh()
    {
        bool visible = GameData.userData.player.playerLevel >= ChatController.UNLOCK_PLAYER_LEVEL;
        gameObject.SetActive(visible);
        if (visible == isVisible)
        {
            return;
        }
        isVisible = visible;
        if (visible)
        {
            ChatController.Instance.StartListening(ChatChannel.World);
            OnMessagesUpdated(ChatChannel.World);
        }
    }

    // jxr gốc: 2 tin cuối (đã bỏ người bị chặn), tin mới nhất ở dòng dưới.
    private void OnMessagesUpdated(ChatChannel channel)
    {
        if (channel != ChatChannel.World || MessageItems == null)
        {
            return;
        }
        List<ChatMessageModel> messages = ChatController.Instance.GetMessages(ChatChannel.World);
        int count = Mathf.Min(MessageItems.Length, messages.Count);
        for (int i = 0; i < MessageItems.Length; i++)
        {
            if (i < count) MessageItems[i].Bind(messages[messages.Count - count + i]);
            else MessageItems[i].Clear();
        }
    }

    private void ClearPreview()
    {
        if (MessageItems == null) return;
        for (int i = 0; i < MessageItems.Length; i++) MessageItems[i].Clear();
    }

    private void OnChatButtonClicked()
    {
        ChatController chat = ChatController.Instance;
        if (chat.IsBanned())
        {
            UIManager.Instance.ShowToastMessage(chat.GetBanMessage(), isLocalize: false);
            return;
        }
        if (!GameData.userData.profile.isNameSet)
        {
            UINicknamePopup popup = UIManager.Instance.LoadUI(UIKey.NicknamePopup) as UINicknamePopup;
            if (popup != null) popup.Show(OpenChatIfNamed);
            return;
        }
        OpenChat();
    }

    // jxt gốc: đóng popup đặt tên — có tên rồi mới mở chat.
    private void OpenChatIfNamed()
    {
        if (GameData.userData.profile.isNameSet) OpenChat();
    }

    private static void OpenChat()
    {
        UIManager.Instance.LoadUI(UIKey.ChatTabPage);
    }
}
