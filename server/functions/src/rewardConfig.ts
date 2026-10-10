import { Firestore } from "firebase-admin/firestore";
import { MAX_BOSS_COUNT, MAX_TIER, START_TIER } from "./config";
import { RewardEntry, RewardTable } from "./models";

// THƯỞNG + LÊN/XUỐNG LEAGUE (server/BOSS_RUSH_DESIGN.md mục 9, 10). Mặc định nằm trong file này; ghi đè được bằng
// Firestore document `config/bossRush` để đổi số liệu KHÔNG cần deploy lại (cache 60 giây).
//
// Schema `config/bossRush` (mọi field tuỳ chọn):
// {
//   "tiers": { "1": { "guaranteedRewardsByBossesKilled": [[{"Type":9,"Amount":120}], ...],   // index = số boss đã giết
//                     "placementBrackets": [{"minRank":1,"maxRank":1,"rewards":[{"Type":9,"Amount":100}]}] } },
//        → bảng khai báo riêng cho 1 league dùng NGUYÊN VĂN (không nhân hệ số)
//   "rewardMultipliers": [1, 1.2, ...],                       // 10 số, league 1..10
//   "promotion": [{ "promoteMaxRank": 15, "demoteBottomPercent": 0, "demoteIfNoFight": false }, ...]   // 10 dòng
// }

const BONE = 0;
const GEM = 1;
const DRAGON_KEY = 2;
const ZOMBIE_KEY = 3;
const LOOTBOX = 4;
const CLOAK = 9;

export interface PromotionRule {
  promoteMaxRank: number;        // hạng 1..N được lên (0 = không lên)
  demoteBottomPercent: number;   // % cuối sảnh bị xuống, tính theo số người thực có trong sảnh (0 = không)
  demoteIfNoFight: boolean;      // không đánh trận nào trong mùa → xuống
}

export interface BossRushRemoteConfig {
  tiers: { [tier: string]: RewardTable };
  rewardMultipliers: number[];
  promotion: PromotionRule[];
}

// Guaranteed của Iron theo số boss cả sảnh đã giết. Tỉ lệ Cloak : Lootbox : Bone = 1 : 3 : 3 và 2 + 2 key là số GỐC
// (ảnh game thật: giết 1 boss = 140/420/420, chốt mùa = 180/540/540). Bước +20 Cloak mỗi boss là TẠM — khớp cả hai
// mốc trên nếu mùa đó sảnh giết 3 boss; chờ thêm số liệu từ game gốc.
function ironGuaranteed(): RewardEntry[][] {
  const table: RewardEntry[][] = [];
  for (let killed = 0; killed <= MAX_BOSS_COUNT; killed++) {
    const cloak = 120 + 20 * killed;
    table.push([
      { Type: CLOAK, Amount: cloak },
      { Type: LOOTBOX, Amount: cloak * 3 },
      { Type: BONE, Amount: cloak * 3 },
      { Type: DRAGON_KEY, Amount: 2 },
      { Type: ZOMBIE_KEY, Amount: 2 },
    ]);
  }
  return table;
}

// Placement của Iron — số GỐC (bảng Rewards trong game thật), mỗi hạng nhận Cloak + Gem cùng số lượng.
const IRON_PLACEMENT: [number, number, number][] = [
  [1, 1, 100], [2, 2, 80], [3, 3, 50], [4, 10, 30], [11, 30, 20], [31, 50, 10], [51, 100, 5],
];

const IRON_TABLE: RewardTable = {
  guaranteedRewardsByBossesKilled: ironGuaranteed(),
  placementBrackets: IRON_PLACEMENT.map(([minRank, maxRank, amount]) => ({
    minRank, maxRank, rewards: [{ Type: CLOAK, Amount: amount }, { Type: GEM, Amount: amount }],
  })),
};

// ĐỀ XUẤT: league cao nhân bảng Iron theo hệ số. Key dungeon không nhân (mỗi ngày người chơi chỉ có 2 key).
const DEFAULT_REWARD_MULTIPLIERS = [1, 1.2, 1.4, 1.6, 1.8, 2, 2.3, 2.6, 3, 3.5];
const SCALED_TYPES = new Set([BONE, GEM, LOOTBOX, CLOAK]);

// ĐỀ XUẤT: league thấp dễ lên, league cao khó giữ.
const DEFAULT_PROMOTION: PromotionRule[] = [
  { promoteMaxRank: 15, demoteBottomPercent: 0, demoteIfNoFight: false },   // Iron
  { promoteMaxRank: 15, demoteBottomPercent: 0, demoteIfNoFight: true },    // Bronze
  { promoteMaxRank: 15, demoteBottomPercent: 0, demoteIfNoFight: true },    // Silver
  { promoteMaxRank: 10, demoteBottomPercent: 15, demoteIfNoFight: true },   // Gold
  { promoteMaxRank: 10, demoteBottomPercent: 15, demoteIfNoFight: true },   // Platinum
  { promoteMaxRank: 10, demoteBottomPercent: 15, demoteIfNoFight: true },   // Emerald
  { promoteMaxRank: 5, demoteBottomPercent: 25, demoteIfNoFight: true },    // Diamond
  { promoteMaxRank: 5, demoteBottomPercent: 25, demoteIfNoFight: true },    // Master
  { promoteMaxRank: 5, demoteBottomPercent: 25, demoteIfNoFight: true },    // Grandmaster
  { promoteMaxRank: 0, demoteBottomPercent: 30, demoteIfNoFight: true },    // Legend
];

export const DEFAULT_CONFIG: BossRushRemoteConfig = {
  tiers: {},
  rewardMultipliers: DEFAULT_REWARD_MULTIPLIERS,
  promotion: DEFAULT_PROMOTION,
};

const CACHE_MS = 60 * 1000;
let cache: { at: number; value: BossRushRemoteConfig } | null = null;

export async function loadRemoteConfig(db: Firestore): Promise<BossRushRemoteConfig> {
  const now = Date.now();
  if (cache && now - cache.at < CACHE_MS) {
    return cache.value;
  }

  const snap = await db.collection("config").doc("bossRush").get();
  const data = (snap.exists ? snap.data() : {}) as Partial<BossRushRemoteConfig>;
  const value: BossRushRemoteConfig = {
    tiers: data.tiers ?? {},
    rewardMultipliers: Array.isArray(data.rewardMultipliers) ? data.rewardMultipliers : DEFAULT_REWARD_MULTIPLIERS,
    promotion: Array.isArray(data.promotion) ? data.promotion : DEFAULT_PROMOTION,
  };
  cache = { at: now, value };
  return value;
}

function clampTier(tier: number): number {
  return Math.min(Math.max(tier, START_TIER), MAX_TIER);
}

function scale(list: RewardEntry[], multiplier: number): RewardEntry[] {
  return list.map((r) => ({ Type: r.Type, Amount: SCALED_TYPES.has(r.Type) ? Math.round(r.Amount * multiplier) : r.Amount }));
}

// Bảng thưởng của một league (đã nhân hệ số) — cũng là bảng client hiển thị trong mục Rewards.
export function getRewardTable(config: BossRushRemoteConfig, tier: number): RewardTable {
  const explicit = config.tiers[String(tier)];
  if (explicit) {
    return {
      guaranteedRewardsByBossesKilled: explicit.guaranteedRewardsByBossesKilled ?? [],
      placementBrackets: explicit.placementBrackets ?? [],
    };
  }
  const multiplier = Number(config.rewardMultipliers[clampTier(tier) - 1]) || 1;
  return {
    guaranteedRewardsByBossesKilled: IRON_TABLE.guaranteedRewardsByBossesKilled.map((list) => scale(list, multiplier)),
    placementBrackets: IRON_TABLE.placementBrackets.map((b) => ({ ...b, rewards: scale(b.rewards, multiplier) })),
  };
}

// Thưởng cuối mùa = Guaranteed theo số boss cả sảnh giết + Placement theo hạng trong sảnh.
export function computeRewards(table: RewardTable, rank: number, bossesKilled: number): RewardEntry[] {
  const result: RewardEntry[] = [];
  const guaranteed = table.guaranteedRewardsByBossesKilled;
  if (guaranteed.length > 0) {
    const index = Math.min(Math.max(bossesKilled, 0), guaranteed.length - 1);
    result.push(...(guaranteed[index] ?? []));
  }
  for (const bracket of table.placementBrackets) {
    if (rank >= bracket.minRank && rank <= bracket.maxRank) {
      result.push(...bracket.rewards);
      break;
    }
  }
  return mergeRewards(result);
}

function mergeRewards(list: RewardEntry[]): RewardEntry[] {
  const byType = new Map<number, number>();
  for (const r of list) {
    byType.set(r.Type, (byType.get(r.Type) ?? 0) + r.Amount);
  }
  return [...byType.entries()].map(([Type, Amount]) => ({ Type, Amount }));
}

// League mùa sau. rank = hạng hiển thị trong sảnh (tính cả bot); poolCount = số người thực có trong sảnh.
// Mỗi mùa chỉ lên hoặc xuống 1 league; phải có điểm mới được lên.
export function computeNewTier(
  config: BossRushRemoteConfig, tier: number, rank: number, poolCount: number, fights: number, score: number,
): number {
  const current = clampTier(tier);
  const rule = config.promotion[current - 1] ?? DEFAULT_PROMOTION[current - 1];

  if (current < MAX_TIER && score > 0 && rule.promoteMaxRank > 0 && rank >= 1 && rank <= rule.promoteMaxRank) {
    return current + 1;
  }
  if (current > START_TIER) {
    if (rule.demoteIfNoFight && fights <= 0) return current - 1;
    const bottom = Math.ceil(poolCount * rule.demoteBottomPercent / 100);
    if (bottom > 0 && rank > poolCount - bottom) return current - 1;
  }
  return current;
}
