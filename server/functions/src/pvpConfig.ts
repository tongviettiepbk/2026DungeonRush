import { Firestore } from "firebase-admin/firestore";

// Hằng số PvP — REVERSE từ game gốc v41 (PvPConfig.asset + PvPConfig.jkp/jkx/jky/jlb + PvPController).
// Chi tiết: DecodedData/PVP_MODEL.md. Client (StaticPvPData.cs) giữ bản sao y hệt.

export const UNLOCK_PLAYER_LEVEL = 15;          // PvPController.yev
export const START_TROPHY = 1000;               // save gốc PvPTrophy mặc định
export const MINIMUM_TROPHY = 0;                // PvPConfig.MinimumTrophy
export const DAILY_FREE_TICKETS = 5;            // PvPConfig.TicketConfig.DailyFreeTickets
export const MAX_AD_TICKETS_PER_DAY = 4;        // PvPConfig.TicketConfig.MaxRewardedAdTicketsPerDay
export const BATTLE_DURATION_SECONDS = 30;      // GameController.hia: wtz = 30
// Server-only (APK không lộ) — giả định:
export const ROSTER_SIZE = 5;                   // số đối thủ mỗi lần tìm
export const ROSTER_TTL_SECONDS = 600;          // PvPController.yew = 600
export const BATTLE_TOKEN_TTL_SECONDS = 180;    // 30s trận + load + mạng
export const LEADERBOARD_TOP = 100;
export const LEADERBOARD_NEARBY = 5;            // ± quanh hạng mình

export interface LeagueDefinition {
  LeagueIndex: number;
  LeagueName: string;
  MinTrophy: number;
  MaxTrophy: number;
  KWin: number;
  KLoss: number;
  WeeklyDecay: number;
}

// PvPConfig.Leagues (tables/PvPConfig.json).
export const LEAGUES: LeagueDefinition[] = [
  { LeagueIndex: 1, LeagueName: "Iron League", MinTrophy: 0, MaxTrophy: 1199, KWin: 40, KLoss: -10, WeeklyDecay: 0 },
  { LeagueIndex: 2, LeagueName: "Bronze League", MinTrophy: 1200, MaxTrophy: 1399, KWin: 40, KLoss: -10, WeeklyDecay: 0 },
  { LeagueIndex: 3, LeagueName: "Silver League", MinTrophy: 1400, MaxTrophy: 1599, KWin: 35, KLoss: -15, WeeklyDecay: 0 },
  { LeagueIndex: 4, LeagueName: "Gold League", MinTrophy: 1600, MaxTrophy: 1799, KWin: 30, KLoss: -15, WeeklyDecay: 0 },
  { LeagueIndex: 5, LeagueName: "Platinum League", MinTrophy: 1800, MaxTrophy: 1999, KWin: 30, KLoss: -20, WeeklyDecay: 0 },
  { LeagueIndex: 6, LeagueName: "Emerald League", MinTrophy: 2000, MaxTrophy: 2199, KWin: 30, KLoss: -20, WeeklyDecay: 0 },
  { LeagueIndex: 7, LeagueName: "Diamond League", MinTrophy: 2200, MaxTrophy: 2399, KWin: 25, KLoss: -25, WeeklyDecay: 0 },
  { LeagueIndex: 8, LeagueName: "Master", MinTrophy: 2400, MaxTrophy: 2599, KWin: 25, KLoss: -25, WeeklyDecay: 0 },
  { LeagueIndex: 9, LeagueName: "Grandmaster", MinTrophy: 2600, MaxTrophy: 2799, KWin: 20, KLoss: -20, WeeklyDecay: 0 },
  { LeagueIndex: 10, LeagueName: "Legend", MinTrophy: 2800, MaxTrophy: 2147483647, KWin: 20, KLoss: -20, WeeklyDecay: 0 },
];

// jkp: league chứa trophy (kẹp ≥ 0); không khớp → league đầu.
export function getLeague(trophy: number): LeagueDefinition {
  const t = Math.max(0, trophy);
  return LEAGUES.find((l) => t >= l.MinTrophy && t <= l.MaxTrophy) ?? LEAGUES[0];
}

export function getLeagueIndex(trophy: number): number {
  return getLeague(trophy).LeagueIndex;
}

// jlb: điểm kỳ vọng Elo của mình trước đối thủ.
export function expectedScore(my: number, opp: number): number {
  return 1 / (1 + Math.pow(10, (opp - my) / 400));
}

// Mathf.RoundToInt / Math.Round mặc định của .NET = làm tròn nửa về số CHẴN (banker's).
export function roundHalfEven(v: number): number {
  const f = Math.floor(v);
  const diff = v - f;
  if (diff > 0.5) return f + 1;
  if (diff < 0.5) return f;
  return f % 2 === 0 ? f : f + 1;
}

// jkx: trophy cộng khi thắng (≥ 1).
export function winDelta(my: number, opp: number): number {
  const k = getLeague(my).KWin;
  return Math.max(1, roundHalfEven(k * (1 - expectedScore(my, opp))));
}

// jky: trophy trừ khi thua (≤ −1).
export function lossDelta(my: number, opp: number): number {
  const k = Math.abs(getLeague(my).KLoss);
  return Math.min(-1, roundHalfEven(-k * expectedScore(my, opp)));
}

// jkz / jla.
export function projectedWin(my: number, opp: number): number {
  return Math.max(MINIMUM_TROPHY, my + winDelta(my, opp));
}

export function projectedLoss(my: number, opp: number): number {
  return Math.max(MINIMUM_TROPHY, my + lossDelta(my, opp));
}

// ===== Bảng thưởng (server gốc giấu số) =====
// Đọc Firestore `config/pvp` để đổi KHÔNG cần deploy:
// { "rewardTable": [ { "leagueIndex": 1, "winRewards": [{"Type":"Bone","Amount":100}], "loseRewards": [...] }, ... ] }
// Type là TÊN enum RewardType (gốc PvPRewardEntryDTO.Type là string).

export interface PvPRewardEntryDTO {
  Type: string;
  Amount: number;
}

export interface PvPLeagueRewardsDTO {
  leagueIndex: number;
  winRewards: PvPRewardEntryDTO[];
  loseRewards: PvPRewardEntryDTO[];
}

// PLACEHOLDER — chưa có số thật từ game gốc (cần ảnh popup "PvP Rewards" từng league).
function defaultRewardTable(): PvPLeagueRewardsDTO[] {
  return LEAGUES.map((l) => ({
    leagueIndex: l.LeagueIndex,
    winRewards: [{ Type: "Bone", Amount: 100 * l.LeagueIndex }, { Type: "Gem", Amount: 2 * l.LeagueIndex }],
    loseRewards: [{ Type: "Bone", Amount: 30 * l.LeagueIndex }],
  }));
}

const CACHE_MS = 60 * 1000;
let cache: { at: number; value: PvPLeagueRewardsDTO[] } | null = null;

export async function loadRewardTable(db: Firestore): Promise<PvPLeagueRewardsDTO[]> {
  const now = Date.now();
  if (cache && now - cache.at < CACHE_MS) return cache.value;

  const snap = await db.collection("config").doc("pvp").get();
  const table = snap.exists ? (snap.data()?.rewardTable as PvPLeagueRewardsDTO[] | undefined) : undefined;
  const value = Array.isArray(table) && table.length > 0 ? table : defaultRewardTable();
  cache = { at: now, value };
  return value;
}

export function getLeagueRewards(table: PvPLeagueRewardsDTO[], leagueIndex: number, won: boolean): PvPRewardEntryDTO[] {
  const row = table.find((r) => r.leagueIndex === leagueIndex);
  if (!row) return [];
  return (won ? row.winRewards : row.loseRewards) ?? [];
}
