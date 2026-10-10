using System.Collections.Generic;

// DTO PvP — tên field GIỐNG DTO game gốc (PvP*DTO / PvPPlayerModel / PvPTicketsDTO, il2cpp v41) và khớp JSON
// server (server/functions/src/pvp.ts). Khác gốc như Boss Rush: món đồ là assetName + slot + rarity, wing/cape
// nằm chung Items (dùng lại BossRushItemModel/BossRushCompanionModel). Chi tiết: DecodedData/PVP_MODEL.md.

public class PvPTicketsDTO
{
    public int freeRemaining;
    public int adRemaining;
    public int adClaimedToday;
    public int maxAdPerDay;
    public int dailyFreeTickets;
    public string dayKey;
}

// PvPRewardEntryDTO gốc: Type là TÊN enum RewardType (client gốc Enum.TryParse).
public class PvPRewardEntryDTO
{
    public string Type;
    public int Amount;
}

public class PvPLeagueRewardsDTO
{
    public int leagueIndex;
    public List<PvPRewardEntryDTO> winRewards = new List<PvPRewardEntryDTO>();
    public List<PvPRewardEntryDTO> loseRewards = new List<PvPRewardEntryDTO>();
}

public class PvPPlayerModel
{
    public string UserId;
    public string PlayerName;
    public string CountryCode;
    public int AvatarId;
    public int Position;
    public int Trophy;
    public double Power;
    public string SnapshotContentVersion;
    public int ProjectedWinTrophy;
    public int ProjectedLossTrophy;
    public List<BossRushItemModel> Items = new List<BossRushItemModel>();
    public List<BossRushCompanionModel> Companions = new List<BossRushCompanionModel>();
    public bool ShowCloak = true;
    public int AttacksUsed;
    public List<int> EnchantmentTiers = new List<int>();
    public bool IsBot;
}

public class PvPLeaderboardEntryModel
{
    public string UserId;
    public string PlayerName;
    public string CountryCode;
    public int AvatarId;
    public int Trophy;
    public int Rank;
    public double Power;
    public bool IsCurrentPlayer;
    public List<BossRushItemModel> Items = new List<BossRushItemModel>();
    public List<BossRushCompanionModel> Companions = new List<BossRushCompanionModel>();
    public bool ShowCloak = true;
    public List<int> EnchantmentTiers = new List<int>();
}

// ===== Request =====

public class PvPOpenRequestDTO
{
    public string server;
    public string playerName;
    public string countryCode;
    public int avatarId;
    public double power;
    public string snapshotHash;
    public string contentVersion;
    public List<BossRushItemModel> items;
    public List<BossRushCompanionModel> companions;
    public bool showCloak;
    public List<int> enchantmentTiers;
}

public class PvPFindOpponentsRequestDTO
{
    public string server;
}

public class PvPStartBattleRequestDTO
{
    public string server;
    public string rosterToken;
    public string opponentUserId;
}

public class PvPBattleResultRequestDTO
{
    public string battleToken;
    public bool won;
}

public class PvPLeaderboardRequestDTO
{
    public string scope;
}

// ===== Response =====

public class PvPOpenResponseDTO
{
    public bool success;
    public string message;
    public string userId;
    public bool hasName;
    public string playerName;
    public int trophy;
    public int leagueIndex;
    public string contentVersion;
    public PvPTicketsDTO tickets;
    public List<PvPLeagueRewardsDTO> rewardTable;
}

public class PvPFindOpponentsResponseDTO
{
    public bool success;
    public string message;
    public string rosterToken;
    public long rosterExpiresAt;
    public List<PvPPlayerModel> opponents;
    public PvPTicketsDTO tickets;
}

public class PvPStartBattleResponseDTO
{
    public bool success;
    public string message;
    public string battleToken;
    public long battleTokenExpiresAt;
    public long startedAt;
    public int startLeagueIndex;
    public PvPPlayerModel opponent;
    public PvPTicketsDTO tickets;
}

public class PvPBattleResultResponseDTO
{
    public bool success;
    public string message;
    public bool won;
    public bool forfeit;
    public int oldTrophy;
    public int newTrophy;
    public int trophyDelta;
    public int oldLeagueIndex;
    public int newLeagueIndex;
    public int startLeagueIndex;
    public int opponentOldTrophy;
    public int opponentNewTrophy;
    public PvPPlayerModel opponent;
    public List<PvPRewardEntryDTO> rewards;
    public PvPTicketsDTO tickets;
}

public class PvPAdTicketResponseDTO
{
    public bool success;
    public string message;
    public PvPTicketsDTO tickets;
}

public class PvPLeaderboardResponseDTO
{
    public bool success;
    public string message;
    public string scope;
    public long generatedAt;
    public bool stale;
    public int playerRank;
    public int playerTrophy;
    public List<PvPLeaderboardEntryModel> topPlayers;
    public List<PvPLeaderboardEntryModel> nearbyPlayers;
}

// ===== Model phía client (save) =====

// PvPCachedRosterModel gốc — roster còn hạn thì mở lại không gọi server.
public class PvPCachedRosterModel
{
    public string RosterToken;
    public long ExpiresAt;       // unix giây
    public List<PvPPlayerModel> Opponents = new List<PvPPlayerModel>();
}

// PendingPvPReport gốc — báo kết quả lỗi mạng → lưu lại gửi tiếp.
public class PendingPvPReport
{
    public string BattleToken;
    public bool Won;
    public long AttemptedAt;
    public int AttemptCount;
    public int StartLeagueIndex;
}

// PendingPvPRewardSettlement gốc — kết quả đã nhận từ server nhưng CHƯA cộng thưởng (chờ bấm Claim).
public class PendingPvPRewardSettlement
{
    public bool Won;
    public int OldTrophy;
    public int NewTrophy;
    public int TrophyDelta;
    public int OldLeagueIndex;
    public int NewLeagueIndex;
    public List<RewardEntry> Rewards = new List<RewardEntry>();
}
