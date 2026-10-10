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
  AvatarId: number;
}

// 1 dòng bảng xếp hạng sảnh gửi về client — KHÔNG kèm bộ đồ (bộ đồ chỉ gửi cho 7 người hỗ trợ lúc vào trận
// và khi bấm xem hồ sơ). Không có cờ bot: client không phân biệt được bot với người thật.
export type BossRushPlayerRow = Pick<BossRushPlayerModel, "UserId" | "PlayerName" | "AvatarId" | "Position" | "Power" | "TotalDamagePoints">;

// Người THẬT trong sảnh (lưu trong PoolDoc.players).
export interface PoolEntry {
  UserId: string;
  PlayerName: string;
  AvatarId: number;
  Power: number;
  TotalDamagePoints: number;
  Fights: number;      // số trận đã báo kết quả trong mùa
  JoinedAt: number;    // ms — phá hoà khi xếp hạng (vào trước đứng trên)
  Joined: boolean;     // false = được giữ chỗ đầu mùa nhưng chưa bấm Join
}

// Bot trong sảnh: chỉ lưu "hạt giống". Điểm, sức mạnh, lịch chơi đều TÍNH RA từ seed + giờ hiện tại (bots.ts).
export interface BotSeed {
  id: string;          // giống uid thật
  name: string;
  avatarId: number;
  seed: number;
  chain: string;       // id chuỗi bộ đồ trong ngân hàng ("" = chưa có ngân hàng → dùng bộ đồ tạm)
  basePower: number;   // power đầu mùa khi không có ngân hàng
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
  avatarId?: number;
  // Chỗ giữ sẵn đầu mùa (season.ts) + dấu "còn hoạt động" + damage trung bình mỗi trận (mốc cho bot).
  reservedPoolId?: string;
  reservedEventKey?: string;
  lastFoughtEventKey?: string;
  avgOwn?: number;
  avgTeam?: number;
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
  isOpen: boolean;          // còn chỗ (người thật + bot < POOL_CAPACITY) — dùng để ghép sảnh
  realCount: number;
  players: { [uid: string]: PoolEntry };   // CHỈ người thật
  bots: BotSeed[];
  botAlgo: number;          // phiên bản công thức bot — khoá theo sảnh để điểm bot không bao giờ đổi ngược
  // Mốc của bot, khoá lúc tạo sảnh (hoặc ở trận thật đầu tiên nếu lúc tạo chưa có số liệu → anchorOwn = 0).
  anchorOwn: number;        // damage riêng mỗi trận của một người chơi "trung bình"
  anchorTeamRatio: number;  // damage cả đội / damage riêng
  anchorPower: number;
  botStartAt: number;       // ms — bot chỉ tính các trận từ mốc này (0 = chưa hoạt động)
  realTeamDamage: number;   // tổng damage cả đội của mọi trận người thật → máu boss = bảng HP − (số này + phần bot)
  isFinalized: boolean;
  claimed: { [uid: string]: boolean };
  results?: { [uid: string]: PoolResult };   // chốt mùa (season.ts)
  finalBossesKilled?: number;
  createdAt: number;
}

export interface PoolResult {
  rank: number;
  fights: number;
  newTier: number;
}

// Snapshot đồ client gửi lên (joinBossRush / updateBossRushPlayer).
export interface PlayerSnapshotRequest {
  playerName?: string;
  power?: number;
  items?: BossRushItemModel[];
  companions?: BossRushCompanionModel[];
  enchantmentTiers?: number[];
  showCloak?: boolean;
  avatarId?: number;
}
