import { Firestore } from "firebase-admin/firestore";
import { MAX_TIER, START_TIER } from "./config";
import { RewardEntry, RewardTable } from "./models";

// Cấu hình THƯỞNG + LÊN/XUỐNG TIER — đọc từ Firestore document `config/bossRush` để đổi số liệu
// KHÔNG cần deploy lại (sửa trên Firebase console). Server gốc giấu bảng này; APK chỉ có
// BossRushLeagueConfig placeholder (hạng 1..100 → 1000 × RewardType 0) → dùng làm mặc định.
//
// Schema `config/bossRush` (mọi field tuỳ chọn):
// {
//   "tiers": {
//     "1": { "guaranteedRewardsByBossesKilled": [[{"Type":1,"Amount":10}], ...],   // index = số boss cả nhóm đã giết
//            "placementBrackets": [{"minRank":1,"maxRank":1,"rewards":[{"Type":1,"Amount":100}]}] },
//     "default": { ... }            // dùng cho tier không khai báo riêng
//   },
//   "promotion": { "promoteMaxRank": 0, "demoteMinRank": 0 }   // 0 = tắt. VD 2 / 7: hạng 1-2 lên, hạng 7-8 xuống
// }

export interface PromotionConfig {
  promoteMaxRank: number;
  demoteMinRank: number;
}

export interface BossRushRemoteConfig {
  tiers: { [tier: string]: RewardTable };
  promotion: PromotionConfig;
}

// BossRushLeagueConfig.asset gốc: mọi tier → hạng 1..100 nhận 1000 × Type 0.
const DEFAULT_REWARD_TABLE: RewardTable = {
  guaranteedRewardsByBossesKilled: [],
  placementBrackets: [{ minRank: 1, maxRank: 100, rewards: [{ Type: 0, Amount: 1000 }] }],
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
    promotion: {
      promoteMaxRank: data.promotion?.promoteMaxRank ?? 0,
      demoteMinRank: data.promotion?.demoteMinRank ?? 0,
    },
  };
  cache = { at: now, value };
  return value;
}

export function getRewardTable(config: BossRushRemoteConfig, tier: number): RewardTable {
  const table = config.tiers[String(tier)] ?? config.tiers["default"] ?? DEFAULT_REWARD_TABLE;
  return {
    guaranteedRewardsByBossesKilled: table.guaranteedRewardsByBossesKilled ?? [],
    placementBrackets: table.placementBrackets ?? [],
  };
}

// Thưởng cuối đợt = thưởng chắc chắn theo số boss cả nhóm giết + thưởng theo hạng.
export function computeRewards(table: RewardTable, rank: number, bossesKilled: number): RewardEntry[] {
  const result: RewardEntry[] = [];
  const guaranteed = table.guaranteedRewardsByBossesKilled;
  if (guaranteed.length > 0) {
    const index = Math.min(bossesKilled, guaranteed.length - 1);
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

export function computeNewTier(config: BossRushRemoteConfig, tier: number, rank: number, poolSize: number): number {
  const { promoteMaxRank, demoteMinRank } = config.promotion;
  if (promoteMaxRank > 0 && rank <= promoteMaxRank) {
    return Math.min(tier + 1, MAX_TIER);
  }
  if (demoteMinRank > 0 && rank >= demoteMinRank && rank <= poolSize) {
    return Math.max(tier - 1, START_TIER);
  }
  return tier;
}
