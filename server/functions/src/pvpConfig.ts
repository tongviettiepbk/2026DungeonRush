import { Firestore } from "firebase-admin/firestore";
import { RewardEntry } from "./models";

// Hằng số + phép tính thuần của PvP mùa + nhóm league (GDD/PVP_DESIGN.md). Client (StaticPvPData.cs) giữ bản sao
// tên league + vé. Tỉ lệ lên/xuống và số bot đổi được ở Firestore `config/pvp` không cần deploy lại.

export const UNLOCK_PLAYER_LEVEL = 15;
export const START_LEAGUE = 1;
export const MAX_LEAGUE = 10;
export const DAILY_TICKETS = 5;                 // không có vé xem quảng cáo
export const BATTLE_DURATION_SECONDS = 30;
export const GROUP_CAPACITY = 100;
export const ROSTER_SIZE = 5;
export const ROSTER_TTL_SECONDS = 600;
export const BATTLE_TOKEN_TTL_SECONDS = 180;    // 30s trận + load + mạng
export const CHALLENGE_STARS = [5, 4, 3, 2, 1]; // cúp nếu thắng, theo thứ tự cúp đối thủ giảm dần

export const LEAGUE_NAMES = [
  "Iron League", "Bronze League", "Silver League", "Gold League", "Platinum League",
  "Emerald League", "Diamond League", "Master", "Grandmaster", "Legend",
];

export function clampLeague(league: number): number {
  return Math.min(Math.max(league, START_LEAGUE), MAX_LEAGUE);
}

// ===== Thưởng mùa (GD mục 10) =====

const BONE = 0;
const GEM = 1;
const LOOTBOX = 4;
const CLOAK = 9;

const GEM_START = 50;
const GEM_STEP = 1.2;
const GEM_STEP_COUNT = 16;
const LOOTBOX_PER_GEM = 4;
const BONE_PER_GEM = 6;
const CLOAK_PER_GEM = 3;

// Mỗi số = số trước × 1.2 rồi làm tròn (không phải 50 × 1.2^n: bậc 4 là 103 chứ không phải 104).
const GEM_SERIES: number[] = (() => {
  const list = [GEM_START];
  for (let i = 0; i < GEM_STEP_COUNT; i++) list.push(Math.round(list[i] * GEM_STEP));
  return list;
})();

const RANK_BANDS: { minRank: number; maxRank: number; step: number }[] = [
  { minRank: 1, maxRank: 1, step: 7 },
  { minRank: 2, maxRank: 2, step: 6 },
  { minRank: 3, maxRank: 3, step: 5 },
  { minRank: 4, maxRank: 10, step: 3 },
  { minRank: 11, maxRank: 25, step: 2 },
  { minRank: 26, maxRank: 50, step: 1 },
  { minRank: 51, maxRank: GROUP_CAPACITY, step: 0 },
];

function bandRewards(league: number, step: number): RewardEntry[] {
  const gem = GEM_SERIES[clampLeague(league) - 1 + step];
  return [
    { Type: GEM, Amount: gem },
    { Type: LOOTBOX, Amount: gem * LOOTBOX_PER_GEM },
    { Type: BONE, Amount: gem * BONE_PER_GEM },
    { Type: CLOAK, Amount: gem * CLOAK_PER_GEM },
  ];
}

export function seasonRewards(league: number, rank: number): RewardEntry[] {
  const band = RANK_BANDS.find((b) => rank >= b.minRank && rank <= b.maxRank);
  return band ? bandRewards(league, band.step) : [];
}

export interface PvPRewardBandDTO {
  MinRank: number;
  MaxRank: number;
  Rewards: RewardEntry[];
}

export interface PvPLeagueRewardsDTO {
  LeagueIndex: number;
  Bands: PvPRewardBandDTO[];
}

export function seasonRewardTable(): PvPLeagueRewardsDTO[] {
  return LEAGUE_NAMES.map((_, i) => ({
    LeagueIndex: i + 1,
    Bands: RANK_BANDS.map((b) => ({ MinRank: b.minRank, MaxRank: b.maxRank, Rewards: bandRewards(i + 1, b.step) })),
  }));
}

// ===== Lên / xuống league (GD mục 9) =====

export interface PvPRemoteConfig {
  promotePercent: number[];   // 10 số, Iron → Legend
  demotePercent: number[];    // 10 số, Iron → Legend
  groupMinSize: number;       // nhóm mới bù bot cho đủ số này
}

export const DEFAULT_PVP_CONFIG: PvPRemoteConfig = {
  promotePercent: [40, 35, 30, 30, 25, 20, 15, 10, 10, 0],
  demotePercent: [0, 5, 10, 10, 15, 15, 20, 20, 25, 25],
  groupMinSize: 30,
};

const CACHE_MS = 60 * 1000;
let cache: { at: number; value: PvPRemoteConfig } | null = null;

export async function loadPvPConfig(db: Firestore): Promise<PvPRemoteConfig> {
  const now = Date.now();
  if (cache && now - cache.at < CACHE_MS) return cache.value;

  const snap = await db.collection("config").doc("pvp").get();
  const data = (snap.exists ? snap.data() : {}) as Partial<PvPRemoteConfig>;
  const value: PvPRemoteConfig = {
    promotePercent: Array.isArray(data.promotePercent) ? data.promotePercent : DEFAULT_PVP_CONFIG.promotePercent,
    demotePercent: Array.isArray(data.demotePercent) ? data.demotePercent : DEFAULT_PVP_CONFIG.demotePercent,
    groupMinSize: typeof data.groupMinSize === "number" ? data.groupMinSize : DEFAULT_PVP_CONFIG.groupMinSize,
  };
  cache = { at: now, value };
  return value;
}

// Số người được lên (làm tròn lên) / bị xuống (làm tròn xuống) của nhóm `count` người, kể cả bot: Platinum 27 người ở
// game thật có 7 người lên, 4 người xuống. League biên không lên/xuống tiếp.
export function promoteCount(config: PvPRemoteConfig, league: number, count: number): number {
  const current = clampLeague(league);
  if (current >= MAX_LEAGUE) return 0;
  return Math.min(count, Math.ceil(count * (Number(config.promotePercent[current - 1]) || 0) / 100));
}

export function demoteCount(config: PvPRemoteConfig, league: number, count: number): number {
  const current = clampLeague(league);
  if (current <= START_LEAGUE) return 0;
  return Math.min(count, Math.floor(count * (Number(config.demotePercent[current - 1]) || 0) / 100));
}

// Phải có cúp mới được lên. Mỗi mùa chỉ lên hoặc xuống 1 league.
export function computeNewLeague(config: PvPRemoteConfig, league: number, rank: number, count: number, trophy: number): number {
  const current = clampLeague(league);
  if (trophy > 0 && rank <= promoteCount(config, current, count)) return current + 1;
  if (rank > count - demoteCount(config, current, count)) return current - 1;
  return current;
}

// ===== Chọn đối thủ (GD mục 6, 7) =====

export interface OpponentCandidate {
  id: string;
  trophy: number;
  power: number;
}

function shuffle<T>(list: T[], rng: () => number): T[] {
  for (let i = list.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [list[i], list[j]] = [list[j], list[i]];
  }
  return list;
}

// Tối đa ROSTER_SIZE người gần cúp mình nhất; cả 2 phía đều có người thì danh sách phải có cả trên lẫn dưới.
export function pickOpponents<T extends OpponentCandidate>(candidates: T[], myTrophy: number, rng: () => number): T[] {
  const distance = (c: OpponentCandidate) => Math.abs(c.trophy - myTrophy);
  const near = shuffle([...candidates], rng).sort((a, b) => distance(a) - distance(b));
  const picked = near.slice(0, ROSTER_SIZE);

  if (picked.length === ROSTER_SIZE) {
    const rest = near.slice(ROSTER_SIZE);
    const ensureSide = (isSide: (c: T) => boolean) => {
      if (picked.some(isSide)) return;
      const extra = rest.find(isSide);
      if (extra) picked[picked.length - 1] = extra;
    };
    ensureSide((c) => c.trophy > myTrophy);
    ensureSide((c) => c.trophy < myTrophy);
  }
  return picked;
}

// Xếp cúp giảm dần (cùng cúp thì mạnh hơn đứng trên) rồi phát +5, +4... từ trên xuống.
export function assignStars<T extends OpponentCandidate>(opponents: T[]): { opponent: T; stars: number }[] {
  return [...opponents]
    .sort((a, b) => (b.trophy - a.trophy) || (b.power - a.power))
    .map((opponent, i) => ({ opponent, stars: CHALLENGE_STARS[Math.min(i, CHALLENGE_STARS.length - 1)] }));
}
