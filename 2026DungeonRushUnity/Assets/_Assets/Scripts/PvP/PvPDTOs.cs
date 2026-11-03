using System.Collections.Generic;

// DTO PvP mùa + nhóm league, khớp JSON server (server/functions/src/pvp.ts). Khác gốc như Boss Rush: món đồ là
// assetName + slot + rarity, wing/cape nằm chung Items (dùng lại BossRushItemModel/BossRushCompanionModel).
// Chi tiết: GDD/PVP_DESIGN.md.

public class PvPTicketsDTO
{
    public int remaining;
    public int dailyTickets;
    public string dayKey;
}

// 1 khung hạng của bảng thưởng mùa.
public class PvPRewardBandDTO
{
    public int MinRank;
    public int MaxRank;
    public List<RewardEntry> Rewards = new List<RewardEntry>();
}

public class PvPLeagueRewardsDTO
{
    public int LeagueIndex;
    public List<PvPRewardBandDTO> Bands = new List<PvPRewardBandDTO>();
}

public class PvPSeasonDTO
{
    public string key;
    public bool isActive;
    public long startsAt;
    public long endsAt;
    public long nextStartsAt;
}

public class PvPPromotionDTO
{
    public int promoteCount;
    public int demoteCount;
}

// Kết quả mùa chờ nhận ở popup Season Complete.
public class PvPSeasonResultDTO
{
    public string seasonKey;
    public int leagueIndex;
    public int newLeagueIndex;
    public int finalRank;
    public int memberCount;
    public int trophy;
    public List<RewardEntry> rewards = new List<RewardEntry>();
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
    public int StarsOnWin;
    public List<BossRushItemModel> Items = new List<BossRushItemModel>();
    public List<BossRushCompanionModel> Companions = new List<BossRushCompanionModel>();
    public bool ShowCloak = true;
    public int AttacksUsed;
    public List<int> EnchantmentTiers = new List<int>();
    public bool IsBot;
}

// 1 dòng bảng xếp hạng nhóm.
public class PvPLeaderboardEntryModel
{
    public string UserId;
    public string PlayerName;
    public int AvatarId;
    public int Trophy;
    public int Rank;
    public double Power;
    public bool IsCurrentPlayer;
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
    public PvPSeasonDTO season;
    public int playerRank;
    public List<PvPLeaderboardEntryModel> leaderboard;
    public PvPPromotionDTO promotion;
    public PvPSeasonResultDTO pendingSeasonResult;
}

public class PvPClaimSeasonResponseDTO
{
    public bool success;
    public string message;
    public List<RewardEntry> rewards;
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
    public int starsOnWin;
    public PvPPlayerModel opponent;
    public PvPTicketsDTO tickets;
}

public class PvPBattleResultResponseDTO
{
    public bool success;
    public string message;
    public bool won;
    public bool forfeit;
    public bool seasonEnded;
    public int oldTrophy;
    public int newTrophy;
    public int trophyDelta;
    public int leagueIndex;
    public PvPTicketsDTO tickets;
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

// Kết quả trận đã nhận từ server, giữ để hiện ở popup cuối trận.
public class PendingPvPRewardSettlement
{
    public bool Won;
    public int OldTrophy;
    public int NewTrophy;
    public int TrophyDelta;
}
