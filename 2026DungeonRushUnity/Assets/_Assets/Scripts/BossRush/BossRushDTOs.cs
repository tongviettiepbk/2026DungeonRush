using System.Collections.Generic;

// DTO Boss Rush — tên field GIỐNG DTO game gốc (BossRush*DTO / BossRushPlayerModel / RewardEntry, il2cpp v41)
// và khớp JSON server (server/functions/src/models.ts). Khác gốc: món đồ là assetName + slot + rarity
// (DungeonRush) thay vì ItemId int; wing/cape nằm chung Items.

// RewardType gốc (TypeDefIndex 961).
public enum RewardType
{
    Bone = 0,
    Gem = 1,
    DragonBossDungeonKey = 2,
    ZombieHordeDungeonKey = 3,
    Lootbox = 4,
    Exp = 5,
    MiningGoldenPickaxe = 6,
    MiningPickaxe = 7,
    MiningDrill = 8,
    CloakCurrency = 9,
    CultistDungeonKey = 10,
    Vial = 11,
}

public class RewardEntry
{
    public RewardType Type;
    public int Amount;
}

public class SubStatEntry
{
    public SubStatType Type;
    public float Value;
}

public class BossRushItemModel
{
    public int Slot;            // GearSlotType
    public string ItemId;       // equipId (assetName)
    public int Rarity;
    public int ItemLevel;
    public List<SubStatEntry> SubStats = new List<SubStatEntry>();
}

public class BossRushCompanionModel
{
    public string CompanionId;
    public int CompanionLevel;
    public bool Equipped;
}

// Dòng trong bảng xếp hạng sảnh KHÔNG kèm bộ đồ (Items/Companions/EnchantmentTiers rỗng): server chỉ gửi bộ đồ cho
// 7 người hỗ trợ lúc vào trận (BossRushStartFightResponseDTO.allies). Client không biết ai là bot.
public class BossRushPlayerModel
{
    public string UserId;
    public string PlayerName;
    public int Position;
    public double Power;
    public long TotalDamagePoints;
    public List<BossRushItemModel> Items = new List<BossRushItemModel>();
    public List<BossRushCompanionModel> Companions = new List<BossRushCompanionModel>();
    public List<int> EnchantmentTiers = new List<int>();   // tier relic đang đeo, index = GearSlotType (0 = trống)
    public bool ShowCloak = true;                          // User.ShowCloak gốc: tắt → ghost ẩn hình áo choàng
    public int AvatarId;
}

public class BossRushTicketsDTO
{
    public int freeRemaining;
    public int adRemaining;
    public int adClaimedToday;
    public int dailyFreeTickets;
    public string dayKey;
}

public class BossRushPlacementBracketDTO
{
    public int minRank;
    public int maxRank;
    public List<RewardEntry> rewards = new List<RewardEntry>();
}

public class BossRushRewardTableDTO
{
    public List<List<RewardEntry>> guaranteedRewardsByBossesKilled = new List<List<RewardEntry>>();
    public List<BossRushPlacementBracketDTO> placementBrackets = new List<BossRushPlacementBracketDTO>();
}

// ===== Request =====

public class BossRushJoinRequestDTO
{
    public string server;
    public string playerName;
    public double power;
    public List<BossRushItemModel> items;
    public List<BossRushCompanionModel> companions;
    public List<int> enchantmentTiers;
    public bool showCloak;
}

public class BossRushUpdateRequestDTO : BossRushJoinRequestDTO
{
    public string poolId;
}

public class BossRushPoolRequestDTO
{
    public string poolId;
}

public class BossRushStartFightRequestDTO
{
    public string poolId;
}

public class BossRushFightResultRequestDTO
{
    public string fightToken;
    public long damageDealt;
    public long totalDamageDealt;
}

public class BossRushClaimRequestDTO
{
    public string poolId;
}

// ===== Response =====

public class BossRushJoinResponseDTO
{
    public bool success;
    public bool alreadyJoined;
    public bool inactive;
    public bool preparing;      // server đang chốt mùa cũ / dựng sảnh mùa mới — thử lại sau ít giây
    public string poolId;
    public int tier;
    public string eventKey;
    public string nextEventKey;
    public List<BossRushPlayerModel> players;
    public int bossNumber;
    public double bossHP;
    public double maxBossHP;
    public bool isFinalized;
    public bool autoLoss;
    public BossRushTicketsDTO tickets;
    public BossRushRewardTableDTO rewardTable;
    public bool hasUnclaimed;
    public string unclaimedPoolId;
}

public class BossRushPoolResponseDTO
{
    public bool success;
    public bool removed;
    public bool inactive;
    public bool concluding;
    public bool autoLoss;
    public string poolId;
    public int tier;
    public string eventKey;
    public string nextEventKey;
    public string message;
    public List<BossRushPlayerModel> players;
    public int bossNumber;
    public double bossHP;
    public double maxBossHP;
    public bool isFinalized;
    public BossRushTicketsDTO tickets;
    public BossRushRewardTableDTO rewardTable;
    public string unclaimedPoolId;
}

public class BossRushStartFightResponseDTO
{
    public bool success;
    public string message;
    public string fightToken;
    public int bossNumber;
    public double bossHP;
    public double maxBossHP;
    public long fightExpiresAt;
    public List<BossRushPlayerModel> allies;   // 7 người hỗ trợ server chọn ngẫu nhiên từ sảnh, kèm bộ đồ
    public BossRushTicketsDTO tickets;
}

public class BossRushFightResultResponseDTO
{
    public bool success;
    public bool autoLoss;
    public long damagePoints;
    public long totalDamagePoints;
    public int newPosition;
    public bool bossKilled;
    public int newBossNumber;
    public double newBossHP;
    public double newMaxBossHP;
    public List<BossRushPlayerModel> players;
    public List<RewardEntry> rewards;
    public BossRushTicketsDTO tickets;
}

public class BossRushClaimResponseDTO
{
    public bool success;
    public bool expired;
    public int rank;
    public bool promoted;
    public bool demoted;
    public int bossesKilled;
    public int tier;
    public int newTier;
    public List<RewardEntry> rewards;
    public BossRushTicketsDTO tickets;
}

// ===== Model phía client =====

// BossRushPoolModel gốc — trạng thái nhóm đang hiển thị.
public class BossRushPoolModel
{
    public string PoolId;
    public int Tier;
    public string EventKey;
    public List<BossRushPlayerModel> Players = new List<BossRushPlayerModel>();
    public bool IsFinalized;
    public int CurrentBossNumber;
    public double CurrentBossHP;
    public double MaxBossHP;
}

// BossRushBossModel gốc — boss của trận đang đánh.
public class BossRushBossModel
{
    public int BossNumber;
    public double CurrentHP;
    public double MaxHP;
    public float AttackPower;
}

// BossRushPendingReport gốc — báo damage lỗi mạng → lưu lại gửi tiếp.
public class BossRushPendingReport
{
    public string FightToken;
    public long DamageDealt;
    public long TotalDamageDealt;
    public long AttemptedAtUnix;
    public int AttemptCount;
}
