import { BankChain, BotParams, botPower, botProgress } from "./bots";
import { DAILY_AD_TICKETS, DAILY_FREE_TICKETS, POOL_CAPACITY, getBossHp } from "./config";
import { BossRushPlayerRow, PoolDoc } from "./models";

// Phép tính THUẦN trên một sảnh (không đụng Firestore): mốc thời gian mùa, bảng xếp hạng gộp người thật + bot,
// máu boss. Xem server/BOSS_RUSH_DESIGN.md mục 4, 6, 7.

const DAY_MS = 24 * 60 * 60 * 1000;
const MAX_BOSS_LOOP = 1000;

// Mùa của eventKey "yyyy-MM-dd" (Thứ 3): 00:00 Thứ 3 → 23:59:59 Chủ nhật, UTC.
export function seasonBounds(eventKey: string): { start: number; end: number } {
  const start = Date.parse(eventKey + "T00:00:00Z");
  return { start, end: start + 6 * DAY_MS - 1000 };
}

export function previousEventKey(eventKey: string): string {
  return new Date(seasonBounds(eventKey).start - 7 * DAY_MS).toISOString().substring(0, 10);
}

export function botParams(pool: PoolDoc): BotParams {
  const { start, end } = seasonBounds(pool.eventKey);
  return {
    seasonStart: start,
    seasonEnd: end,
    botStartAt: pool.botStartAt,
    anchorOwn: pool.anchorOwn,
    anchorTeamRatio: pool.anchorTeamRatio,
    maxFightsPerDay: DAILY_FREE_TICKETS + DAILY_AD_TICKETS,
  };
}

export function poolCount(pool: PoolDoc): number {
  return Object.keys(pool.players ?? {}).length + (pool.bots?.length ?? 0);
}

export function isPoolOpen(pool: PoolDoc): boolean {
  return poolCount(pool) < POOL_CAPACITY;
}

export interface RankEntry {
  id: string;
  name: string;
  avatarId: number;
  power: number;
  score: number;
  fights: number;
  joinedAt: number;
  isBot: boolean;
  position: number;
}

// Bảng xếp hạng tại thời điểm now: điểm giảm dần, hoà thì ai vào sảnh trước đứng trên.
export function rankPool(pool: PoolDoc, now: number, bank: Map<string, BankChain>): RankEntry[] {
  const params = botParams(pool);
  const list: RankEntry[] = Object.values(pool.players ?? {}).map((p) => ({
    id: p.UserId, name: p.PlayerName, avatarId: p.AvatarId ?? 0, power: p.Power, score: p.TotalDamagePoints,
    fights: p.Fights ?? 0, joinedAt: p.JoinedAt, isBot: false, position: 0,
  }));
  (pool.bots ?? []).forEach((bot, i) => {
    const progress = botProgress(bot.seed, params, now);
    list.push({
      id: bot.id, name: bot.name, avatarId: bot.avatarId, power: botPower(bot, params, now, bank), score: progress.score,
      fights: progress.fights, joinedAt: pool.createdAt + 1 + i, isBot: true, position: 0,
    });
  });
  list.sort((a, b) => (b.score - a.score) || (a.joinedAt - b.joinedAt));
  list.forEach((e, i) => (e.position = i + 1));
  return list;
}

export function toRows(ranked: RankEntry[]): BossRushPlayerRow[] {
  return ranked.map((e) => ({
    UserId: e.id, PlayerName: e.name, AvatarId: e.avatarId, Position: e.position, Power: e.power, TotalDamagePoints: e.score,
  }));
}

export interface BossState {
  bossNumber: number;
  bossHP: number;
  maxBossHP: number;
  bossesKilled: number;
}

// Máu boss chung = bảng HP trừ dần tổng damage cả sảnh đã gây (boss chết thì phần dư dồn sang boss kế).
export function bossStateFor(tier: number, totalDamage: number): BossState {
  let remaining = Math.max(0, totalDamage);
  let bossNumber = 1;
  let maxBossHP = getBossHp(bossNumber, tier);
  for (let i = 0; i < MAX_BOSS_LOOP && remaining >= maxBossHP; i++) {
    remaining -= maxBossHP;
    bossNumber++;
    maxBossHP = getBossHp(bossNumber, tier);
  }
  return { bossNumber, bossHP: maxBossHP - remaining, maxBossHP, bossesKilled: bossNumber - 1 };
}

export function poolBossState(pool: PoolDoc, now: number): BossState {
  const params = botParams(pool);
  let total = pool.realTeamDamage ?? 0;
  for (const bot of pool.bots ?? []) total += botProgress(bot.seed, params, now).team;
  return bossStateFor(pool.tier, total);
}

// Chia đều n người vào số sảnh ít nhất sao cho mỗi sảnh không quá reserveMax người. VD 150/80 → [75, 75].
export function distribute(n: number, reserveMax: number): number[] {
  if (n <= 0) return [];
  const pools = Math.ceil(n / reserveMax);
  const base = Math.floor(n / pools);
  const extra = n % pools;
  return Array.from({ length: pools }, (_, i) => base + (i < extra ? 1 : 0));
}

export function median(values: number[]): number {
  const list = values.filter((v) => v > 0).sort((a, b) => a - b);
  if (list.length === 0) return 0;
  const mid = Math.floor(list.length / 2);
  return list.length % 2 === 1 ? list[mid] : (list[mid - 1] + list[mid]) / 2;
}
