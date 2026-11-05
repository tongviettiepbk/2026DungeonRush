using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Chat Thế giới + Clan phía client — gộp ChatController + ClanChatController gốc (2 class gốc gần như y hệt, khác đường dẫn
// và endpoint). Nghe RTDB chat/ww và chat/clans/{clanId} (40 tin cuối, orderBy "t"), gửi qua Cloud Function.
// Luật gốc: tin cắt 200 ký tự + lọc từ tục; 3 tin đầu gửi liền, từ tin thứ 4 mỗi tin cách tin trước 3s (đếm riêng mỗi kênh);
// tin mình gửi hiện mờ ngay rồi thay bằng bản server; người bị chặn ẩn tin; đang bị cấm thì không gửi.
// Gốc tắt: tin "rơi đồ" Mythic/Artifact (ChatController.ffk không ai gọi) → không gửi, chỉ đọc nếu có.
public class ChatController : Singleton<ChatController>
{
    public const string FN_SEND = "sendchatmessage";
    public const string FN_SEND_CLAN = "sendclanchatmessage";
    public const string FN_GET_PROFILE = "getchatplayerprofile";
    public const string FN_UPDATE_PROFILE = "updatechatplayerprofile";
    public const string FN_REPORT = "reportchatuser";

    public const int MESSAGE_MAX_LENGTH = 200;
    public const int UNLOCK_PLAYER_LEVEL = 6;                // ChatButtonUI.UnlockPlayerLevel (GameplayScene gốc)
    public const long PERMANENT_BAN_UNTIL = 32503680000L;    // uzx gốc (năm 3000)
    private const int MAX_MESSAGES = 40;                     // uze gốc
    private const int FREE_MESSAGES = 3;                     // uzb gốc
    private const float SEND_COOLDOWN = 3f;                  // uza gốc
    private const string PATH_WORLD = "chat/ww";
    private const string PATH_CLANS = "chat/clans/";

    public static event Action<ChatChannel> MessagesUpdated;
    public static event Action<ChatChannel> MessageSendFailed;
    public static event Action<ChatChannel> ChatLoadFailed;
    public static event Action BannedFromChat;

    private class ChannelState
    {
        public readonly List<ChatMessageModel> messages = new List<ChatMessageModel>();
        public RealtimeDbStream stream;
        public string path;
        public int sentCount;
        public float lastSendTime = -1000f;
    }

    private readonly Dictionary<ChatChannel, ChannelState> channels = new Dictionary<ChatChannel, ChannelState>
    {
        { ChatChannel.World, new ChannelState() },
        { ChatChannel.Clan, new ChannelState() },
    };
    private readonly HashSet<ChatChannel> wanted = new HashSet<ChatChannel>();
    private bool isPaused;

    private static UserChatData User => GameData.userData.chat;
    public string MyUserId => FirebaseManager.Instance.Uid;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        ClanController.ClanUpdated += OnClanUpdated;
    }

    private void OnDestroy()
    {
        ClanController.ClanUpdated -= OnClanUpdated;
    }

    // ===== Nghe kênh (fem/fez gốc) =====

    // Bắt đầu nghe 1 kênh; kênh Clan đổi clan thì tự chuyển nhánh.
    public void StartListening(ChatChannel channel)
    {
        wanted.Add(channel);
        Attach(channel);
    }

    public void StopListening(ChatChannel channel)
    {
        wanted.Remove(channel);
        Detach(channel);
    }

    public bool IsLoaded(ChatChannel channel)
    {
        RealtimeDbStream stream = channels[channel].stream;
        return stream != null && stream.HasSnapshot;
    }

    // Tin hiện được (đã bỏ người bị chặn), cũ → mới.
    public List<ChatMessageModel> GetMessages(ChatChannel channel)
    {
        List<ChatMessageModel> list = new List<ChatMessageModel>();
        foreach (ChatMessageModel m in channels[channel].messages)
        {
            if (!IsBlocked(m.UserId)) list.Add(m);
        }
        return list;
    }

    private void Attach(ChatChannel channel)
    {
        if (isPaused)
        {
            return;
        }

        ChannelState state = channels[channel];
        string path = GetPath(channel);
        if (state.stream != null && state.path == path)
        {
            state.stream.Start();
            return;
        }

        Detach(channel);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        state.path = path;
        state.stream = new RealtimeDbStream(this, path, MAX_MESSAGES);
        state.stream.Snapshot += data => OnSnapshot(channel, data);
        state.stream.ChildChanged += (key, data) => OnChildChanged(channel, key, data);
        state.stream.Failed += () => ChatLoadFailed?.Invoke(channel);
        state.stream.Start();
    }

    private void Detach(ChatChannel channel)
    {
        ChannelState state = channels[channel];
        if (state.stream != null)
        {
            state.stream.Stop();
            state.stream = null;
        }
        state.path = null;
        state.messages.Clear();
    }

    private static string GetPath(ChatChannel channel)
    {
        if (channel == ChatChannel.World)
        {
            return PATH_WORLD;
        }
        string clanId = GameData.userData.clan.clanId;
        return string.IsNullOrEmpty(clanId) ? null : PATH_CLANS + clanId;
    }

    // Vào/rời clan → kênh Clan nghe nhánh mới.
    private void OnClanUpdated(ClanModel clan)
    {
        if (wanted.Contains(ChatChannel.Clan))
        {
            Attach(ChatChannel.Clan);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        isPaused = pause;
        foreach (ChatChannel channel in wanted)
        {
            if (pause) channels[channel].stream?.Stop();
            else Attach(channel);
        }
    }

    // ===== Nhận tin (ffg/fff gốc) =====

    private void OnSnapshot(ChatChannel channel, JObject data)
    {
        ChannelState state = channels[channel];
        // Giữ tin mình đang gửi dở (chưa có bản server).
        List<ChatMessageModel> pending = state.messages.FindAll(m => m.IsLocal);
        state.messages.Clear();
        if (data != null)
        {
            foreach (JProperty p in data.Properties())
            {
                ChatMessageModel m = Parse(channel, p.Name, p.Value as JObject);
                if (m != null) state.messages.Add(m);
            }
        }
        state.messages.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
        state.messages.AddRange(pending);
        Trim(state);
        MessagesUpdated?.Invoke(channel);
    }

    private void OnChildChanged(ChatChannel channel, string key, JObject data)
    {
        ChannelState state = channels[channel];
        int index = state.messages.FindIndex(m => m.MessageId == key);
        if (data == null)
        {
            if (index >= 0) state.messages.RemoveAt(index);
            MessagesUpdated?.Invoke(channel);
            return;
        }

        ChatMessageModel message = Parse(channel, key, data);
        if (message == null || index >= 0)
        {
            return;
        }

        // Bản server của tin mình vừa gửi → thay tin mờ.
        int local = state.messages.FindIndex(m => m.IsLocal && m.UserId == message.UserId && m.Message == message.Message);
        if (local >= 0) state.messages.RemoveAt(local);

        int insert = state.messages.FindIndex(m => m.IsLocal || m.Timestamp > message.Timestamp);
        if (insert < 0) state.messages.Add(message);
        else state.messages.Insert(insert, message);
        Trim(state);
        MessagesUpdated?.Invoke(channel);
    }

    private static ChatMessageModel Parse(ChatChannel channel, string key, JObject data)
    {
        if (data == null)
        {
            return null;
        }
        try
        {
            ChatMessageModel m = data.ToObject<ChatMessageModel>();
            m.MessageId = key;
            m.Channel = channel;
            return m;
        }
        catch (Exception e)
        {
            DebugCustom.LogWarning("[chat] parse " + key + ": " + e.Message);
            return null;
        }
    }

    private static void Trim(ChannelState state)
    {
        while (state.messages.Count > MAX_MESSAGES)
        {
            state.messages.RemoveAt(0);
        }
    }

    // ===== Gửi tin (ffj gốc) =====

    // ffh gốc: 3 tin đầu tự do, sau đó cách tin trước SEND_COOLDOWN giây.
    public bool CanSend(ChatChannel channel)
    {
        ChannelState state = channels[channel];
        return state.sentCount < FREE_MESSAGES || Time.time - state.lastSendTime >= SEND_COOLDOWN;
    }

    // ffi gốc: số giây còn phải chờ.
    public float GetCooldownRemaining(ChatChannel channel)
    {
        ChannelState state = channels[channel];
        if (state.sentCount < FREE_MESSAGES)
        {
            return 0f;
        }
        return Mathf.Max(0f, state.lastSendTime + SEND_COOLDOWN - Time.time);
    }

    public void Send(ChatChannel channel, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || !CanSend(channel))
        {
            return;
        }
        if (string.IsNullOrEmpty(MyUserId))
        {
            DebugCustom.LogWarning("Cannot send chat message: userId is null or empty. User must register first.");
            return;
        }
        if (channel == ChatChannel.Clan && string.IsNullOrEmpty(GetPath(ChatChannel.Clan)))
        {
            return;
        }

        text = text.Trim();
        if (text.Length > MESSAGE_MAX_LENGTH)
        {
            text = text.Substring(0, MESSAGE_MAX_LENGTH);
        }
        text = ProfanityFilter.Filter(text);

        ChannelState state = channels[channel];
        state.sentCount++;
        state.lastSendTime = Time.time;
        Analytics.Log("chat_send", ("channel", channel));

        UserProfileData profile = GameData.userData.profile;
        ChatMessageModel local = new ChatMessageModel
        {
            UserId = MyUserId,
            AvatarId = profile.avatarId,
            Username = profile.userName,
            Message = text,
            Timestamp = 0,
            Channel = channel,
            IsLocal = true,
        };
        state.messages.Add(local);
        Trim(state);
        MessagesUpdated?.Invoke(channel);

        ChatSendRequestDTO request = new ChatSendRequestDTO { message = text };
        BossRushController.FillSnapshot(request);
        string fn = channel == ChatChannel.World ? FN_SEND : FN_SEND_CLAN;
        FirebaseManager.Instance.Call<ChatSendResponseDTO>(fn, request, (ok, res, raw) =>
        {
            if (ok && res != null && res.success)
            {
                local.Timestamp = res.timestamp;
                return;
            }

            state.messages.Remove(local);
            MessagesUpdated?.Invoke(channel);
            if (ok && res != null && res.bannedUntil > NowSeconds())
            {
                SetBan(res.bannedUntil, res.banTier);
                BannedFromChat?.Invoke();
                return;
            }
            DebugCustom.LogWarning("Chat message failed to send: " + raw);
            MessageSendFailed?.Invoke(channel);
        });
    }

    // ===== Cấm chat (ffl/ffn/ffp gốc) =====

    public bool IsBanned()
    {
        return User.bannedUntil >= 1 && (User.bannedUntil >= PERMANENT_BAN_UNTIL || User.bannedUntil > NowSeconds());
    }

    // Chữ gốc tiếng Anh cứng (không có key localize).
    public string GetBanMessage()
    {
        long until = User.bannedUntil;
        if (until >= PERMANENT_BAN_UNTIL)
        {
            return "You are permanently banned from chat.";
        }
        long remaining = until - NowSeconds();
        if (remaining <= 0)
        {
            return "You are banned from chat.";
        }

        string duration;
        if (remaining >= 7 * 86400) duration = Ceil(remaining, 7 * 86400) + " week(s)";
        else if (remaining >= 86400) duration = Ceil(remaining, 86400) + " day(s)";
        else if (remaining >= 3600) duration = Ceil(remaining, 3600) + " hour(s)";
        else duration = Ceil(remaining, 60) + " minute(s)";
        return "You are banned from chat. Try again in " + duration + ".";
    }

    private static long Ceil(long seconds, long unit)
    {
        return Math.Max(1, (seconds + unit - 1) / unit);
    }

    private static void SetBan(long bannedUntil, int banTier)
    {
        User.bannedUntil = bannedUntil;
        User.banTier = banTier;
        User.isDataChanged = true;
        GameData.Save();
    }

    private static long NowSeconds()
    {
        return new DateTimeOffset(ServerTime.UtcNow).ToUnixTimeSeconds();
    }

    // ===== Chặn (fen/feo/fep gốc) — chỉ lưu ở máy =====

    public bool IsBlocked(string userId)
    {
        return string.IsNullOrEmpty(userId) == false && User.blockedUserIds.Contains(userId);
    }

    public void Block(string userId)
    {
        if (string.IsNullOrEmpty(userId) || IsBlocked(userId))
        {
            return;
        }
        User.blockedUserIds.Add(userId);
        User.isDataChanged = true;
        GameData.Save();
        NotifyAll();
    }

    public void Unblock(string userId)
    {
        if (!User.blockedUserIds.Remove(userId))
        {
            return;
        }
        User.isDataChanged = true;
        GameData.Save();
        NotifyAll();
    }

    private static void NotifyAll()
    {
        MessagesUpdated?.Invoke(ChatChannel.World);
        MessagesUpdated?.Invoke(ChatChannel.Clan);
    }

    // ===== Hồ sơ + báo cáo (ffv/ffu/ffs gốc) =====

    public void GetProfile(string userId, Action<bool, BossRushPlayerModel> callback)
    {
        FirebaseManager.Instance.Call<ChatProfileResponseDTO>(FN_GET_PROFILE, new { userId }, (ok, res, raw) =>
        {
            bool success = ok && res != null && res.success && res.player != null;
            callback?.Invoke(success, success ? res.player : null);
        });
    }

    public void Report(ChatReportContext context, List<string> reasons, Action<bool> callback)
    {
        ChatReportRequestDTO request = new ChatReportRequestDTO
        {
            reportedUid = context.UserId,
            type = context.Channel == ChatChannel.World ? "world" : "clan",
            messageId = context.MessageId,
            text = context.Text,
            t = context.Timestamp,
            channel = context.Channel == ChatChannel.World ? "world" : "clan",
            reasons = reasons,
        };
        FirebaseManager.Instance.Call<ChatBaseResponseDTO>(FN_REPORT, request, (ok, res, raw) =>
        {
            callback?.Invoke(ok && res != null && res.success);
        });
    }

    // Đặt/đổi tên hoặc avatar → cập nhật hồ sơ chat (NicknamePopup / AvatarSelectPopup gốc).
    public void UpdateProfile()
    {
        UserProfileData profile = GameData.userData.profile;
        if (!profile.isNameSet)
        {
            return;
        }
        ChatUpdateProfileRequestDTO request = new ChatUpdateProfileRequestDTO
        {
            playerName = profile.userName,
            avatarId = profile.avatarId,
            countryCode = string.Empty,
        };
        FirebaseManager.Instance.Call<ChatBaseResponseDTO>(FN_UPDATE_PROFILE, request, (ok, res, raw) =>
        {
            if (!ok) DebugCustom.LogWarning("[chat] updateChatPlayerProfile failed: " + raw);
        });
    }
}
