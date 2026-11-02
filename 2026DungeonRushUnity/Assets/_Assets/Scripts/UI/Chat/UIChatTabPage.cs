using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Trang chat (ChatTabPage gốc): 2 kênh Thế giới | Clan (TabSelector), 40 tin cuối, ô nhập + nút gửi.
// - Chưa có clan mà chọn Clan → báo UI.Chat.NoClanWarning, giữ kênh cũ (fja/fjb gốc).
// - Đang chờ hồi gửi tin → ô nhập ghi UI.Chat.CooldownPlaceholder (số giây), nút gửi tắt; gửi quá nhanh → Errors.Chat.TooManyMessages.
// - Chưa tải xong kênh → vòng xoay; tải lỗi → Errors.Chat.LoadFailed. Bàn phím mobile bật → đẩy khung chat lên (fjz gốc).
// - Nút Discord gốc mở cộng đồng của game gốc → project ẩn.
public class UIChatTabPage : BaseUI
{
    private const float LOADING_SPIN_SPEED = 360f;

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _contentParent;
    [SerializeField] private ChatMessageItem _messagePrefab;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _sendButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private Image _loadingImage;
    [SerializeField] private Button _discordButton;
    [SerializeField] private TabSelector _channelSelector;
    [SerializeField] private TMP_Text _titleText;

    private readonly List<ChatMessageItem> items = new List<ChatMessageItem>();
    private ChatChannel channel = ChatChannel.World;
    private string placeholderText;
    private TMP_Text placeholder;
    private float panelBaseY;
    private bool isCoolingDown;

    private static ChatController Controller => ChatController.Instance;

    protected override void Awake()
    {
        base.Awake();
        isPopup = true;
        _closeButton.onClick.AddListener(Close);
        _sendButton.onClick.AddListener(OnSendClicked);
        _inputField.characterLimit = ChatController.MESSAGE_MAX_LENGTH;
        _inputField.onSubmit.AddListener(_ => OnSendClicked());
        if (_discordButton != null) _discordButton.gameObject.SetActive(false);
        placeholder = _inputField.placeholder as TMP_Text;
        placeholderText = Loc.L("UI.Chat.Placeholder");
        panelBaseY = _panelRoot.anchoredPosition.y;
        _channelSelector.OnTabSelected += OnChannelSelected;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ChatController.MessagesUpdated += OnMessagesUpdated;
        ChatController.MessageSendFailed += OnSendFailed;
        ChatController.ChatLoadFailed += OnLoadFailed;
        ChatController.BannedFromChat += OnBanned;

        _channelSelector.Setup(new[] { Loc.L("Common.World"), Loc.L("UI.Chat.ClanTab") }, (int)channel);
        _inputField.text = string.Empty;
        isCoolingDown = false;
        Controller.StartListening(ChatChannel.World);
        Controller.StartListening(ChatChannel.Clan);
        ShowChannel(channel);
    }

    private void OnDisable()
    {
        ChatController.MessagesUpdated -= OnMessagesUpdated;
        ChatController.MessageSendFailed -= OnSendFailed;
        ChatController.ChatLoadFailed -= OnLoadFailed;
        ChatController.BannedFromChat -= OnBanned;
        // Kênh Thế giới vẫn nghe cho dòng xem trước ở lobby.
        if (ChatController.Instance != null) Controller.StopListening(ChatChannel.Clan);
        if (_panelRoot != null) SetPanelOffset(0f);
    }

    private void Update()
    {
        if (_loadingImage != null && _loadingImage.gameObject.activeSelf)
        {
            _loadingImage.transform.Rotate(0f, 0f, -LOADING_SPIN_SPEED * Time.unscaledDeltaTime);
        }
        UpdateCooldown();
        SetPanelOffset(GetKeyboardHeight());
    }

    // ===== Kênh =====

    private void OnChannelSelected(int index)
    {
        ChatChannel next = (ChatChannel)index;
        if (next == ChatChannel.Clan && !GameData.userData.clan.HasClan)
        {
            UIManager.Instance.ShowToastMessage(Loc.L("UI.Chat.NoClanWarning"), isLocalize: false);
            _channelSelector.Select((int)channel, false);
            return;
        }
        ShowChannel(next);
    }

    // fjc gốc.
    private void ShowChannel(ChatChannel next)
    {
        if (next == ChatChannel.Clan && !GameData.userData.clan.HasClan)
        {
            next = ChatChannel.World;
            _channelSelector.Select((int)next, false);
        }
        channel = next;
        if (_titleText != null) _titleText.text = Loc.L(channel == ChatChannel.Clan ? "UI.Chat.ClanTab" : "Common.World");
        Redraw(true);
    }

    private void OnMessagesUpdated(ChatChannel updated)
    {
        if (updated == channel) Redraw(IsAtBottom());
    }

    // fjv gốc: dựng lại danh sách, đang ở cuối thì cuộn theo tin mới.
    private void Redraw(bool scrollToBottom)
    {
        bool loaded = Controller.IsLoaded(channel);
        if (_loadingImage != null) _loadingImage.gameObject.SetActive(!loaded);

        List<ChatMessageModel> messages = Controller.GetMessages(channel);
        for (int i = 0; i < messages.Count; i++)
        {
            if (i >= items.Count) items.Add(Instantiate(_messagePrefab, _contentParent));
            items[i].gameObject.SetActive(true);
            items[i].Bind(messages[i], OnClickAvatar);
        }
        for (int i = messages.Count; i < items.Count; i++) items[i].gameObject.SetActive(false);

        if (scrollToBottom)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_contentParent);
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private bool IsAtBottom()
    {
        return _scrollRect.verticalNormalizedPosition <= 0.01f || _contentParent.rect.height <= ((RectTransform)_scrollRect.transform).rect.height;
    }

    // ===== Gửi =====

    // fjj gốc.
    private void OnSendClicked()
    {
        string text = _inputField.text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        if (Controller.IsBanned())
        {
            UIManager.Instance.ShowToastMessage(Controller.GetBanMessage(), isLocalize: false);
            return;
        }
        if (!Controller.CanSend(channel))
        {
            // fjn gốc.
            UIManager.Instance.ShowToastMessage(Loc.L("Errors.Chat.TooManyMessages", Mathf.CeilToInt(Controller.GetCooldownRemaining(channel))), isLocalize: false);
            return;
        }
        Controller.Send(channel, text);
        _inputField.text = string.Empty;
        Redraw(true);
    }

    // fjo gốc: đang hồi → placeholder đếm giây, khoá ô nhập + nút gửi.
    private void UpdateCooldown()
    {
        float remaining = Controller.GetCooldownRemaining(channel);
        bool cooling = remaining > 0f;
        if (cooling)
        {
            if (placeholder != null) placeholder.text = Loc.L("UI.Chat.CooldownPlaceholder", Mathf.CeilToInt(remaining));
        }
        if (cooling == isCoolingDown)
        {
            return;
        }
        isCoolingDown = cooling;
        _sendButton.interactable = !cooling;
        _inputField.interactable = !cooling;
        if (!cooling && placeholder != null) placeholder.text = placeholderText;
    }

    // Server từ chối (gửi quá nhanh / lỗi mạng) — gốc báo "Too many messages".
    private void OnSendFailed(ChatChannel failed)
    {
        int seconds = Mathf.Max(1, Mathf.CeilToInt(Controller.GetCooldownRemaining(failed)));
        UIManager.Instance.ShowToastMessage(Loc.L("Errors.Chat.TooManyMessages", seconds), isLocalize: false);
    }

    private void OnLoadFailed(ChatChannel failed)
    {
        if (failed != channel) return;
        if (_loadingImage != null) _loadingImage.gameObject.SetActive(false);
        UIManager.Instance.ShowToastMessage(Loc.L("Errors.Chat.LoadFailed"), isLocalize: false);
    }

    private void OnBanned()
    {
        UIManager.Instance.ShowToastMessage(Controller.GetBanMessage(), isLocalize: false);
    }

    // ===== Hồ sơ người gửi (ChatMessageItem.fix → PlayerPopup gốc) =====

    private void OnClickAvatar(ChatMessageModel message)
    {
        UIPlayerPopup popup = UIManager.Instance.LoadUI(UIKey.PlayerPopup) as UIPlayerPopup;
        if (popup == null) return;

        string userId = message.UserId;
        ChatReportContext context = message.UserId == Controller.MyUserId ? null : ChatReportContext.From(message);
        popup.ShowLoading(userId);
        Controller.GetProfile(userId, (ok, profile) =>
        {
            if (popup == null || !popup.IsShowing(userId)) return;
            if (!ok)
            {
                UIManager.Instance.ShowToastMessage("Couldn't load player info. Please try again.", isLocalize: false);
                popup.Close();
                return;
            }
            popup.Show(profile);
            popup.ShowChatActions(context);
        });
    }

    // ===== Bàn phím mobile (fjz/fka gốc) =====

    private void SetPanelOffset(float keyboardHeight)
    {
        Vector2 pos = _panelRoot.anchoredPosition;
        pos.y = panelBaseY + keyboardHeight;
        _panelRoot.anchoredPosition = pos;
    }

    // Chiều cao bàn phím theo đơn vị canvas (0 khi ẩn / Editor).
    private float GetKeyboardHeight()
    {
        if (!TouchScreenKeyboard.visible || !_inputField.isFocused)
        {
            return 0f;
        }
        float pixels = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
        using (AndroidJavaObject view = window.Call<AndroidJavaObject>("getDecorView"))
        using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
        {
            view.Call("getWindowVisibleDisplayFrame", rect);
            pixels = Screen.height - rect.Call<int>("height");
        }
#else
        pixels = TouchScreenKeyboard.area.height;
#endif
        Canvas canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        return Mathf.Max(0f, pixels / Mathf.Max(0.01f, scale));
    }
}
