import { BankChain, BotParams, botAvatarId, botPower, botProgress, generateBots, hashSeed, hashString, makeRng } from "./bots";
import { seasonBounds } from "./bossRushPool";
import { BotSeed } from "./models";
import { DAILY_TICKETS, PvPRemoteConfig, computeNewLeague } from "./pvpConfig";

// Phép tính THUẦN trên một nhóm league (không đụng Firestore): tạo nhóm, xếp hạng người thật + bot, chốt mùa.
// Xem GDD/PVP_DESIGN.md mục 4, 8, 9. Bot không lưu tiến trình: cúp là hàm thuần của (seed, giờ) như bot Boss Rush.

const SALT_PVP_FIGHT = 83;
const BOT_WIN_RATE = 0.55;
const BOT_STAR_LEVELS = 5;

export interface PvPGroupEntry {
  UserId: string;
  PlayerName: string;
  AvatarId: number;
  Power: number;
  Trophy: number;
  Fights: number;
  JoinedAt: number;
}

export interface PvPGroupResult {
  rank: number;
  count: number;
  trophy: number;
  fights: number;
  newLeague: number;
}

// Document pvpGroups/{seasonKey_L{league}_{n}}
export interface PvPGroupDoc {
  groupId: string;
  seasonKey: string;
  league: number;
  players: { [uid: string]: PvPGroupEntry };
  bots: BotSeed[];
  createdAt: number;
  isFinalized: boolean;
  results: { [uid: string]: PvPGroupResult };
}

export interface PvPRankEntry {
  id: string;
  name: string;
  avatarId: number;
  power: number;
  trophy: number;
  fights: number;
  isBot: boolean;
  position: number;
}

export function groupCount(group: PvPGroupDoc): number {
  return Object.keys(group.players ?? {}).length + (group.bots?.length ?? 0);
}

export function pvpBotParams(group: PvPGroupDoc): BotParams {
  const { start, end } = seasonBounds(group.seasonKey);
  return {
    seasonStart: start, seasonEnd: end, botStartAt: start, anchorOwn: 1, anchorTeamRatio: 1, maxFightsPerDay: DAILY_TICKETS,
  };
}

// Mỗi trận của bot thắng/thua + số cúp suy ra từ (seed, số thứ tự trận) nên cúp chỉ tăng, ai đọc cũng ra cùng một số.
export function botTrophy(seed: number, fights: number): number {
  let trophy = 0;
  for (let i = 0; i < fights; i++) {
    const rng = makeRng(hashSeed(seed, SALT_PVP_FIGHT, i));
    if (rng() < BOT_WIN_RATE) trophy += 1 + Math.floor(rng() * BOT_STAR_LEVELS);
  }
  return trophy;
}

export function createGroup(
  groupId: string, seasonKey: string, league: number, members: PvPGroupEntry[], minSize: number, now: number,
  bank: Map<string, BankChain>,
): PvPGroupDoc {
  const players: { [uid: string]: PvPGroupEntry } = {};
  const usedNames = new Set<string>();
  const powers: number[] = [];
  for (const m of members) {
    players[m.UserId] = m;
    usedNames.add((m.PlayerName ?? "").toLowerCase());
    powers.push(m.Power);
  }
  powers.sort((a, b) => a - b);
  const anchorPower = powers.length > 0 ? powers[Math.floor(powers.length / 2)] : 0;
  const botCount = Math.max(0, minSize - members.length);
  return {
    groupId, seasonKey, league, players,
    bots: generateBots(hashString(groupId), botCount, usedNames, anchorPower, bank),
    createdAt: now,
    isFinalized: false,
    results: {},
  };
}

// Cúp giảm dần, cùng cúp thì sức mạnh giảm dần.
export function rankGroup(group: PvPGroupDoc, now: number, bank: Map<string, BankChain>): PvPRankEntry[] {
  const params = pvpBotParams(group);
  const list: PvPRankEntry[] = Object.values(group.players ?? {}).map((p) => ({
    id: p.UserId, name: p.PlayerName, avatarId: p.AvatarId ?? 0, power: p.Power, trophy: p.Trophy, fights: p.Fights ?? 0,
    isBot: false, position: 0,
  }));
  for (const bot of group.bots ?? []) {
    const fights = botProgress(bot.seed, params, now).fights;
    list.push({
      id: bot.id, name: bot.name, avatarId: botAvatarId(bot.seed), power: botPower(bot, params, now, bank),
      trophy: botTrophy(bot.seed, fights), fights, isBot: true, position: 0,
    });
  }
  list.sort((a, b) => (b.trophy - a.trophy) || (b.power - a.power) || a.id.localeCompare(b.id));
  list.forEach((e, i) => (e.position = i + 1));
  return list;
}

// Hạng cuối + league mới của người thật, tính tại thời điểm KẾT THÚC mùa.
export function finalizeGroup(group: PvPGroupDoc, config: PvPRemoteConfig, bank: Map<string, BankChain>): { [uid: string]: PvPGroupResult } {
  const ranked = rankGroup(group, seasonBounds(group.seasonKey).end, bank);
  const results: { [uid: string]: PvPGroupResult } = {};
  for (const e of ranked) {
    if (e.isBot) continue;
    results[e.id] = {
      rank: e.position,
      count: ranked.length,
      trophy: e.trophy,
      fights: e.fights,
      newLeague: computeNewLeague(config, group.league, e.position, ranked.length, e.trophy),
    };
  }
  return results;
}
