using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// Kênh chat (ChatTabPage.ChatChannel gốc).
public enum ChatChannel
{
    World = 0,
    Clan = 1,
}

// 1 tin nhắn trong Realtime Database (ChatMessageModel gốc, khoá JSON rút gọn u/a/n/m/t/tp).
[Serializable]
public class ChatMessageModel
{
    public const string TYPE_ITEM_DROP = "itemDrop";

    [JsonProperty("u")] public string UserId;
    [JsonProperty("a")] public int AvatarId;
    [JsonProperty("n")] public string Username;
    [JsonProperty("m")] public string Message;
    [JsonProperty("t")] public int Timestamp;
    [JsonProperty("tp", NullValueHandling = NullValueHandling.Ignore)] public string Type;

    [JsonIgnore] public string MessageId;
    [JsonIgnore] public ChatChannel Channel;
    // Tin mình vừa gửi, server chưa xác nhận → hiện mờ (ChatMessageItem gốc alpha 0.5).
    [JsonIgnore] public bool IsLocal;

    [JsonIgnore] public bool IsItemDrop => Type == TYPE_ITEM_DROP;
}

// Gửi tin: chữ + snapshot đồ để người khác bấm avatar xem (ChatSendRequestDTO gốc gửi items/companions/wing/cape/enchantment).
// Project dùng chung khuôn snapshot Boss Rush (UIPlayerPopup vẽ thẳng BossRushPlayerModel).
public class ChatSendRequestDTO : BossRushJoinRequestDTO
{
    public string message;
}

public class ChatSendResponseDTO
{
    public bool success;
    public string message;
    public int timestamp;
    public long bannedUntil;
    public int banTier;
}

public class ChatProfileResponseDTO
{
    public bool success;
    public string message;
    public BossRushPlayerModel player;
}

public class ChatUpdateProfileRequestDTO
{
    public string playerName;
    public int avatarId;
    public string countryCode;
}

public class ChatReportRequestDTO
{
    public string reportedUid;
    public string type;
    public string messageId;
    public string text;
    public int t;
    public string channel;
    public List<string> reasons;
}

public class ChatBaseResponseDTO
{
    public bool success;
    public string message;
}

// Tin bị báo cáo (class hd gốc: ChatMessageItem truyền sang PlayerPopup → ReportPopup).
public class ChatReportContext
{
    public string UserId;
    public string MessageId;
    public string Text;
    public int Timestamp;
    public ChatChannel Channel;

    public static ChatReportContext From(ChatMessageModel message)
    {
        return new ChatReportContext
        {
            UserId = message.UserId,
            MessageId = message.MessageId,
            Text = message.Message,
            Timestamp = message.Timestamp,
            Channel = message.Channel,
        };
    }
}
