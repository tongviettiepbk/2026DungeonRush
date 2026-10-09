// Kiểu dữ liệu Boss Rush — tên field GIỐNG DTO game gốc (BossRush*DTO / BossRushPlayerModel / RewardEntry)
// để client C# deserialize thẳng. Khác gốc duy nhất: món đồ của DungeonRush là assetName (string) + slot + rarity
// thay vì ItemId int; wing/cape nằm chung danh sách Items (slot 7/6) thay vì field riêng.

export interface SubStatEntry {
  Type: number;
  Value: number;
}

export interface BossRushItemModel {
  Slot: number;        // GearSlotType
  ItemId: string;      // equipId (assetName)
  Rarity: number;
  ItemLevel: number;
  SubStats: SubStatEntry[];
}

export interface BossRushCompanionModel {
  CompanionId: string;
  CompanionLevel: number;
  Equipped: boolean;   // đang mang ra trận (spawn cùng ghost); false = chỉ sở hữu (Own Effect)
}

export interface BossRushPlayerModel {
  UserId: string;
  PlayerName: string;
  Position: number;
  Power: number;
  TotalDamagePoints: number;
  Items: BossRushItemModel[];
  Companions: BossRushCompanionModel[];
  EnchantmentTiers: number[];   // tier relic đang đeo, index = GearSlotType (0 = trống)
  ShowCloak: boolean;           // User.ShowCloak gốc
  IsBot: boolean;
  JoinedAt: number;    // ms — phá hoà khi xếp hạng (vào trước đứng trên)
}

export interface RewardEntry {
  Type: number;        // RewardType gốc: Bone 0, Gem 1, ... Vial 11
  Amount: number;
}

export interface PlacementBracket {
  minRank: number;
  maxRank: number;
  rewards: RewardEntry[];
}

export interface RewardTable {
  guaranteedRewardsByBossesKilled: RewardEntry[][];
  placementBrackets: PlacementBracket[];
}

export interface TicketsDTO {
  freeRemaining: number;
  adRemaining: number;
  adClaimedToday: number;
  dailyFreeTickets: number;
  dayKey: string;
}

export interface ActiveFight {
  token: string;
  poolId: string;
  bossNumber: number;
  expiresAt: number;   // ms
}

// Document bossRushPlayers/{uid}
export interface PlayerDoc {
  uid: string;
  playerName: string;
  power: number;
  items: BossRushItemModel[];
  companions: BossRushCompanionModel[];
  enchantmentTiers: number[];
  showCloak?: boolean;   // User.ShowCloak gốc — tắt thì ghost ẩn hình áo choàng (vẫn có chỉ số)
  tier: number;
  currentPoolId: string;
  lastJoinEventKey: string;
  tickets: TicketsDTO;
  activeFight: ActiveFight | null;
  updatedAt: number;
}

// Document bossRushPools/{poolId}
export interface PoolDoc {
  poolId: string;
  eventKey: string;
  tier: number;
  isOpen: boolean;          // còn chỗ (playerCount < POOL_SIZE) — dùng để ghép nhóm
  playerCount: number;
  players: { [uid: string]: BossRushPlayerModel };
  bossNumber: number;
  bossHP: number;
  maxBossHP: number;
  bossesKilled: number;
  isFinalized: boolean;
  claimed: { [uid: string]: boolean };
  createdAt: number;
}

// Snapshot đồ client gửi lên (joinBossRush / updateBossRushPlayer).
export interface PlayerSnapshotRequest {
  playerName?: string;
  power?: number;
  items?: BossRushItemModel[];
  companions?: BossRushCompanionModel[];
  enchantmentTiers?: number[];
  showCloak?: boolean;
}
