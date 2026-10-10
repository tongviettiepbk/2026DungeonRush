import { randomBytes } from "crypto";
import { getFirestore, Firestore, Transaction } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";
import { onSchedule } from "firebase-functions/v2/scheduler";
import { CLAN_PLAYERS, CLANS, ClanDoc, ClanPlayerDoc, nowSeconds, requireUid } from "./clan";
import {
  BATTLE_TOKEN_TTL_SECONDS, ClanWarConfigDTO, ClanWarRewardsDTO, LOOT_RARITIES, MAX_ACTIONS_PER_BATCH, ORES, SUMMON_RARITIES,
  addRewards, dayEndsAtSeconds, emptyRewards, loadConfig, milestoneRewards, nextWeekStartsAtSeconds, sourcesForDay, tierOf,
  warDayOf, weekIdOf, weekStartOf,
} from "./clanWarConfig";
import { PvPPlayerDoc, PvPPlayerModel } from "./pvp";

// Clan War — endpoint giống server gốc (tên viết thường): getclanwarstate, recordclanwarcontributions,
// getclanwarcontributionleaderboard, getclanwarclanleaderboard, getclanwarleadershipranking, startclanwarpvpbattle,
// reportclanwarpvpbattle, claimclanwarmilestone, claimclanwarpersonalbundle, claimclanwarclanreward
// + cron ghép cặp đầu tuần (Thứ Ba 00:05 UTC) và admin matchclanwarsnow / finalizeclanwarsnow.
// Gốc bọc payload trong phong bì HMAC (issueClanWarSessionKey) — bản này dùng callable có auth.

const WARS = "clanWars";
const WAR_INDEX = "clanWarIndex";       // {weekId}_{clanId} → {warId}
const META = "clanWarMeta";             // champion
const CONTENT_VERSION = "1.0.0";

interface SideDoc {
  clanId: string;
  clanName: string;
  bannerBackgroundTypeId: number;
  bannerBackgroundColorId: number;
  bannerImageTypeId: number;
  bannerImageColorId: number;
  server: string;
  rewardTier: string;
  isBot: boolean;
  memberCountAtStart: number;
  daily: Record<string, number>;          // "1".."6" → tổng điểm clan
}

interface PvpSideState {
  round: number;
  resetCount: number;
  defeated: string[];                     // uid đối thủ đã bị clan này hạ trong vòng
}

interface DailyResult {
  day: number;
  warScore: number;
  winnerClanId: string;
  totals: Record<string, number>;
  mvp: Record<string, { userId: string; playerName: string; avatarId: number } | null>;
}

interface WarDoc {
  warId: string;
  weekId: string;
  type: "normal" | "leadership";
  clanIds: string[];
  sides: Record<string, SideDoc>;
  botRoster: PvPPlayerModel[];            // thành viên clan bot (snapshot copy)
  pvp: Record<string, PvpSideState>;
  dailyResults: Record<string, DailyResult>;
  finalized: boolean;
  winnerClanId: string;
  createdAt: number;
}

interface ActiveBattle { token: string; targetUserId: string; expiresAt: number }

interface MemberDoc {
  uid: string;
  clanId: string;
  playerName: string;
  avatarId: number;
  daily: Record<string, number>;
  weekly: number;
  batchIds: string[];
  actionIds: string[];
  pvpRound: number;
  pvpTickets: number;
  activeBattle: ActiveBattle | null;
  claimedMilestones: number[];
  bundleClaimed: boolean;
  clanRewardClaimed: boolean;
}

function db(): Firestore {
  return getFirestore();
}

function ok<T extends object>(extra: T) {
  return { success: true, code: "", message: "", ...extra };
}

function fail(code: string, message: string) {
  return { success: false, code, message };
}

function stateOf(day: number): string {
  if (day <= 5) return "day";
  if (day === 6) return "day6";
  return "cooldown";
}

function enemyOf(war: WarDoc, clanId: string): string {
  return war.clanIds[0] === clanId ? war.clanIds[1] : war.clanIds[0];
}

// Hệ số điểm của clan bot theo ngày (0.55..1.25), cố định theo warId+day.
function botFactor(warId: string, day: number): number {
  let h = 0;
  const s = warId + "#" + day;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) >>> 0;
  return 0.55 + (h % 1000) / 1000 * 0.7;
}

// Tổng điểm 1 ngày của 1 phe; clan bot = điểm phe thật × hệ số (ngày đang chạy tăng dần theo giờ).
function sideDaily(war: WarDoc, clanId: string, day: number, now: Date): number {
  const side = war.sides[clanId];
  if (!side.isBot) return side.daily?.[String(day)] ?? 0;
  const real = war.sides[enemyOf(war, clanId)].daily?.[String(day)] ?? 0;
  let total = Math.round(real * botFactor(war.warId, day));
  if (warDayOf(now) === day && weekIdOf(now) === war.weekId) {
    const dayStart = (dayEndsAtSeconds(now) - 86400) * 1000;
    const frac = Math.min(1, Math.max(0.1, (now.getTime() - dayStart) / 86400000));
    total = Math.round(total * frac);
  }
  // Bot cũng phải có điểm khi phe thật chưa đóng góp gì (để vẫn có thắng/thua).
  if (total === 0 && day < warDayOf(now)) total = 1000 + Math.floor(botFactor(war.warId, day + 10) * 2000);
  return total;
}

function newMemberDoc(uid: string, clanId: string, name: string, avatarId: number): MemberDoc {
  return {
    uid, clanId, playerName: name, avatarId, daily: {}, weekly: 0, batchIds: [], actionIds: [],
    pvpRound: 0, pvpTickets: 0, activeBattle: null, claimedMilestones: [], bundleClaimed: false, clanRewardClaimed: false,
  };
}

async function loadMembers(warId: string): Promise<MemberDoc[]> {
  const snap = await db().collection(WARS).doc(warId).collection("members").get();
  return snap.docs.map((d) => d.data() as MemberDoc);
}

function mvpOf(members: MemberDoc[], clanId: string, day: number) {
  let best: MemberDoc | null = null;
  for (const m of members) {
    if (m.clanId !== clanId) continue;
    const p = day > 0 ? (m.daily?.[String(day)] ?? 0) : m.weekly;
    if (p <= 0) continue;
    if (!best || p > (day > 0 ? (best.daily?.[String(day)] ?? 0) : best.weekly)) best = m;
  }
  return best ? { userId: best.uid, playerName: best.playerName, avatarId: best.avatarId } : null;
}

function botMvp(war: WarDoc, clanId: string, day: number) {
  if (war.botRoster.length === 0) return null;
  const b = war.botRoster[Math.floor(botFactor(war.warId, day + 20) * 1000) % war.botRoster.length];
  void clanId;
  return { userId: b.UserId, playerName: b.PlayerName, avatarId: b.AvatarId };
}

// Chốt các ngày đã qua (kết quả ngày + MVP), chốt tuần khi sang cooldown. Trả true nếu war doc đổi.
function concludeDays(war: WarDoc, members: MemberDoc[], config: ClanWarConfigDTO, now: Date): boolean {
  const sameWeek = weekIdOf(now) === war.weekId;
  const currentDay = sameWeek ? warDayOf(now) : 8;
  let changed = false;
  for (let d = 1; d <= 6; d++) {
    if (d >= currentDay || war.dailyResults[String(d)]) continue;
    const [a, b] = war.clanIds;
    const ta = sideDaily(war, a, d, now);
    const tb = sideDaily(war, b, d, now);
    const winner = ta === tb ? "" : (ta > tb ? a : b);
    const mvp: DailyResult["mvp"] = {};
    for (const id of war.clanIds) mvp[id] = war.sides[id].isBot ? botMvp(war, id, d) : mvpOf(members, id, d);
    war.dailyResults[String(d)] = {
      day: d, warScore: config.warScoreByDay[String(d)] ?? 1, winnerClanId: winner, totals: { [a]: ta, [b]: tb }, mvp,
    };
    changed = true;
  }
  if (currentDay >= 7 && !war.finalized) {
    const [a, b] = war.clanIds;
    const sa = warScore(war, a);
    const sb = warScore(war, b);
    if (sa !== sb) {
      war.winnerClanId = sa > sb ? a : b;
    } else {
      const wa = weeklyTotal(war, a, now);
      const wb = weeklyTotal(war, b, now);
      war.winnerClanId = wa >= wb ? a : b;
    }
    changed = true;
  }
  return changed;
}

function warScore(war: WarDoc, clanId: string): number {
  return Object.values(war.dailyResults).reduce((s, r) => s + (r.winnerClanId === clanId ? r.warScore : 0), 0);
}

function weeklyTotal(war: WarDoc, clanId: string, now: Date): number {
  let t = 0;
  for (let d = 1; d <= 6; d++) t += sideDaily(war, clanId, d, now);
  return t;
}

// Sau khi chốt tuần: cộng tierPoints (= War Score) cho clan thật, cập nhật champion nếu là trận leadership. Idempotent theo war.finalized.
async function applyFinalization(war: WarDoc): Promise<void> {
  if (war.finalized || !war.winnerClanId) return;
  await db().runTransaction(async (tx) => {
    const ref = db().collection(WARS).doc(war.warId);
    const snap = await tx.get(ref);
    const cur = snap.data() as WarDoc | undefined;
    if (!cur || cur.finalized) return;
    const clanRefs = war.clanIds.filter((id) => !war.sides[id].isBot).map((id) => db().collection(CLANS).doc(id));
    const clanSnaps = await Promise.all(clanRefs.map((r) => tx.get(r)));
    for (const cs of clanSnaps) {
      if (!cs.exists) continue;
      const clan = cs.data() as ClanDoc;
      clan.tierPoints = (clan.tierPoints || 0) + warScore(war, clan.clanId);
      clan.clanTier = tierOf(clan.tierPoints);
      tx.set(cs.ref, clan);
    }
    if (war.type === "leadership" && !war.sides[war.winnerClanId].isBot) {
      tx.set(db().collection(META).doc("champion"), { clanId: war.winnerClanId, weekId: war.weekId });
    }
    tx.set(ref, { ...war, finalized: true });
  });
  war.finalized = true;
}

// ===== Tra cứu war của người chơi =====

interface Ctx { uid: string; player: ClanPlayerDoc | null; clan: ClanDoc | null; war: WarDoc | null }

async function loadContext(uid: string, now: Date): Promise<Ctx> {
  const ps = await db().collection(CLAN_PLAYERS).doc(uid).get();
  const player = ps.exists ? (ps.data() as ClanPlayerDoc) : null;
  if (!player?.clanId) return { uid, player, clan: null, war: null };
  const cs = await db().collection(CLANS).doc(player.clanId).get();
  const clan = cs.exists ? (cs.data() as ClanDoc) : null;
  if (!clan || !clan.members[uid]) return { uid, player, clan: null, war: null };
  const idx = await db().collection(WAR_INDEX).doc(weekIdOf(now) + "_" + clan.clanId).get();
  if (!idx.exists) return { uid, player, clan, war: null };
  const ws = await db().collection(WARS).doc(String(idx.data()?.warId)).get();
  return { uid, player, clan, war: ws.exists ? (ws.data() as WarDoc) : null };
}

async function getMember(tx: Transaction, warId: string, uid: string, clan: ClanDoc): Promise<{ ref: FirebaseFirestore.DocumentReference; m: MemberDoc }> {
  const ref = db().collection(WARS).doc(warId).collection("members").doc(uid);
  const snap = await tx.get(ref);
  const me = clan.members[uid];
  const m = snap.exists ? (snap.data() as MemberDoc) : newMemberDoc(uid, clan.clanId, me?.playerName ?? "Player", me?.avatarId ?? 0);
  m.clanId = clan.clanId;
  if (me) { m.playerName = me.playerName; m.avatarId = me.avatarId; }
  return { ref, m };
}

// ===== Roster PvP ngày 6 =====

function botModel(id: string, name: string, src: PvPPlayerDoc | null, power: number): PvPPlayerModel {
  return {
    UserId: id, PlayerName: name, CountryCode: "", AvatarId: 0, Position: 0, Trophy: 0, Power: power,
    SnapshotContentVersion: CONTENT_VERSION, ProjectedWinTrophy: 0, ProjectedLossTrophy: 0,
    Items: src?.items ?? [], Companions: src?.companions ?? [], ShowCloak: src?.showCloak ?? true, AttacksUsed: 0,
    EnchantmentTiers: src?.enchantmentTiers ?? [], IsBot: true,
  };
}

function snapshotModel(uid: string, name: string, avatarId: number, power: number, p: PvPPlayerDoc | null): PvPPlayerModel {
  return {
    UserId: uid, PlayerName: name, CountryCode: p?.countryCode ?? "", AvatarId: avatarId, Position: 0, Trophy: p?.trophy ?? 0,
    Power: p?.power || power, SnapshotContentVersion: p?.contentVersion ?? CONTENT_VERSION, ProjectedWinTrophy: 0,
    ProjectedLossTrophy: 0, Items: p?.items ?? [], Companions: p?.companions ?? [], ShowCloak: p?.showCloak ?? true,
    AttacksUsed: 0, EnchantmentTiers: p?.enchantmentTiers ?? [], IsBot: false,
  };
}

// Đối thủ của clanId (thành viên clan địch; clan bot → botRoster).
async function enemyRoster(war: WarDoc, myClanId: string): Promise<PvPPlayerModel[]> {
  const enemyId = enemyOf(war, myClanId);
  if (war.sides[enemyId].isBot) return war.botRoster;
  const cs = await db().collection(CLANS).doc(enemyId).get();
  if (!cs.exists) return [];
  const clan = cs.data() as ClanDoc;
  const members = Object.values(clan.members);
  const snaps = await Promise.all(members.map((m) => db().collection("pvpPlayers").doc(m.userId).get()));
  return members.map((m, i) => snapshotModel(m.userId, m.playerName, m.avatarId, m.power,
    snaps[i].exists ? (snaps[i].data() as PvPPlayerDoc) : null));
}

function pvpState(war: WarDoc, clanId: string): PvpSideState {
  if (!war.pvp) war.pvp = {};
  if (!war.pvp[clanId]) war.pvp[clanId] = { round: 1, resetCount: 0, defeated: [] };
  return war.pvp[clanId];
}

function resetEnabled(war: WarDoc, config: ClanWarConfigDTO, clanId: string): boolean {
  const st = pvpState(war, clanId);
  const min = config.pvp.minMembersForReset;
  return st.resetCount < config.pvp.maxRoundResets && war.clanIds.every((id) => war.sides[id].memberCountAtStart >= min);
}

function syncTickets(m: MemberDoc, war: WarDoc, config: ClanWarConfigDTO): void {
  const st = pvpState(war, m.clanId);
  if (m.pvpRound !== st.round) {
    m.pvpRound = st.round;
    m.pvpTickets = config.pvp.startTickets;
  }
}

// ===== Dựng ClanWarWireDTO =====

function sideWire(s: SideDoc) {
  return {
    clanId: s.clanId, clanName: s.clanName, bannerBackgroundTypeId: s.bannerBackgroundTypeId,
    bannerBackgroundColorId: s.bannerBackgroundColorId, bannerImageTypeId: s.bannerImageTypeId,
    bannerImageColorId: s.bannerImageColorId, server: s.server, rewardTier: s.rewardTier, isBot: s.isBot,
  };
}

async function buildWire(ctx: Ctx, config: ClanWarConfigDTO, now: Date, includeDailyResults: boolean) {
  const war = ctx.war as WarDoc;
  const clan = ctx.clan as ClanDoc;
  const myId = clan.clanId;
  const enemyId = enemyOf(war, myId);
  const members = await loadMembers(war.warId);
  if (concludeDays(war, members, config, now)) {
    await db().collection(WARS).doc(war.warId).set(war);
  }
  if (war.winnerClanId && !war.finalized) await applyFinalization(war);

  const day = weekIdOf(now) === war.weekId ? warDayOf(now) : 7;
  const state = stateOf(day);
  const me = members.find((m) => m.uid === ctx.uid) ?? newMemberDoc(ctx.uid, myId, clan.members[ctx.uid]?.playerName ?? "", 0);
  const myDailyTotal = day <= 6 ? sideDaily(war, myId, day, now) : 0;
  const enemyDailyTotal = day <= 6 ? sideDaily(war, enemyId, day, now) : 0;

  let pvp = null;
  if (day === 6) {
    syncTickets(me, war, config);
    const st = pvpState(war, myId);
    const roster = await enemyRoster(war, myId);
    pvp = {
      round: st.round, resetCount: st.resetCount, maxResets: config.pvp.maxRoundResets,
      resetEnabled: resetEnabled(war, config, myId), resetThreshold: roster.length, targetCount: roster.length,
      defeatedCount: st.defeated.length, myTickets: me.pvpTickets, ticketsMax: config.pvp.startTickets,
      canAttack: me.pvpTickets > 0,
      targets: roster.map((r) => ({
        userId: r.UserId, playerName: r.PlayerName, power: r.Power, avatarId: r.AvatarId, defeated: st.defeated.includes(r.UserId),
      })),
    };
  }

  const tier = war.sides[myId].rewardTier;
  const outcome = war.winnerClanId ? (war.winnerClanId === myId ? "win" : "lose") : "";
  const dailyResults = includeDailyResults
    ? Object.values(war.dailyResults).sort((a, b) => a.day - b.day).map((r) => ({
      day: r.day, warScore: r.warScore, winnerClanId: r.winnerClanId,
      winnerClanName: r.winnerClanId ? war.sides[r.winnerClanId].clanName : "",
      winnerMvp: r.winnerClanId ? r.mvp[r.winnerClanId] : null, myClanWon: r.winnerClanId === myId,
      myDailyTotal: r.totals[myId] ?? 0, enemyDailyTotal: r.totals[enemyId] ?? 0,
    }))
    : [];

  const myWeekly = weeklyTotal(war, myId, now);
  const enemyWeekly = weeklyTotal(war, enemyId, now);
  return {
    warId: war.warId, weekId: war.weekId, type: war.type, state, activeDay: Math.min(day, 7),
    dayEndsAt: dayEndsAtSeconds(now), cooldownEndsAt: nextWeekStartsAtSeconds(now),
    myClan: sideWire(war.sides[myId]), enemyClan: sideWire(war.sides[enemyId]),
    myWarScore: warScore(war, myId), enemyWarScore: warScore(war, enemyId),
    winnerClanId: war.winnerClanId, leaderClanId: myWeekly >= enemyWeekly ? myId : enemyId,
    won: war.winnerClanId ? war.winnerClanId === myId : null,
    liveBar: { myDaily: myDailyTotal, enemyDaily: enemyDailyTotal, myWeekly, enemyWeekly },
    myDailyPoints: day <= 6 ? (me.daily?.[String(day)] ?? 0) : 0, myWeeklyPoints: me.weekly,
    myMvp: day <= 6 ? mvpOf(members, myId, day) : mvpOf(members, myId, 0),
    enemyMvp: war.sides[enemyId].isBot ? botMvp(war, enemyId, Math.min(day, 6)) : (day <= 6 ? mvpOf(members, enemyId, day) : mvpOf(members, enemyId, 0)),
    myMemberCount: clan.memberCount,
    enemyMemberCount: war.sides[enemyId].isBot ? war.botRoster.length : war.sides[enemyId].memberCountAtStart,
    pvp,
    claims: {
      outcome, clanRewardClaimed: me.clanRewardClaimed, clanRewardEligible: me.weekly > 0, personalBundleClaimed: me.bundleClaimed,
      clanReward: outcome ? (config.clanRewards[tier]?.[outcome] ?? emptyRewards()) : null,
    },
    milestone: { weekId: war.weekId, weeklyContribution: me.weekly, claimedMilestones: me.claimedMilestones, bundleClaimed: me.bundleClaimed },
    dailyResults,
  };
}

// ===== getClanWarState =====

export const getclanwarstate = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { includeConfig?: boolean; includeDailyResults?: boolean };
  const now = new Date();
  const config = await loadConfig(db());
  const ctx = await loadContext(uid, now);
  const base = {
    nextWeekStartsAt: nextWeekStartsAtSeconds(now), serverTime: nowSeconds(), contentVersion: CONTENT_VERSION,
    config: data.includeConfig === false ? null : config,
  };
  if (!ctx.clan) return ok({ ...base, inClan: false, war: null });
  if (!ctx.war) return ok({ ...base, inClan: true, war: null });
  const war = await buildWire(ctx, config, now, data.includeDailyResults !== false);
  return ok({ ...base, inClan: true, war });
});

// ===== recordClanWarContributions =====

interface ActionDTO { actionId?: string; source?: string; rarity?: string; resource?: string; newLevel?: number; count?: number; eventTimestamp?: number }

function pointsOf(a: ActionDTO, config: ClanWarConfigDTO): number {
  const aw = config.awards;
  const rarity = String(a.rarity ?? "").toLowerCase();
  const count = Math.max(1, Math.min(1000, Math.floor(Number(a.count) || 1)));
  switch (a.source) {
    case "lootEquipment": return LOOT_RARITIES.includes(rarity) ? (aw.lootEquipment[rarity] ?? 0) * count : 0;
    case "summonCompanion": return SUMMON_RARITIES.includes(rarity) ? (aw.summonCompanion[rarity] ?? 0) * count : 0;
    case "summonCape": return SUMMON_RARITIES.includes(rarity) ? (aw.summonCape[rarity] ?? 0) * count : 0;
    case "levelUp": {
      const lv = Math.floor(Number(a.newLevel) || 0);
      return lv > 0 && lv <= 10000 ? lv * aw.levelUpMultiplier : 0;
    }
    case "mining": {
      const ore = String(a.resource ?? "").toLowerCase();
      return ORES.includes(ore) ? (aw.mining[ore] ?? 0) * count : 0;
    }
    case "dungeonKey": return aw.dungeonKey * count;
    default: return 0;
  }
}

export const recordclanwarcontributions = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string; batchId?: string; actions?: ActionDTO[] };
  const now = new Date();
  const config = await loadConfig(db());
  const ctx = await loadContext(uid, now);
  if (!ctx.clan) return fail("ERR_NOT_IN_CLAN", "Not in clan");
  if (!ctx.war || ctx.war.warId !== data.warId) return fail("ERR_NO_WAR", "No active war");
  const day = warDayOf(now);
  if (day > 5) return fail("ERR_WAR_STATE", "Contributions closed");
  const actions = Array.isArray(data.actions) ? data.actions.slice(0, MAX_ACTIONS_PER_BATCH) : [];
  const allowed = sourcesForDay(config, day);
  const dayStart = dayEndsAtSeconds(now) - 86400;
  const clan = ctx.clan;
  const warRef = db().collection(WARS).doc(ctx.war.warId);

  return db().runTransaction(async (tx) => {
    const ws = await tx.get(warRef);
    const war = ws.data() as WarDoc;
    const { ref, m } = await getMember(tx, war.warId, uid, clan);
    const batchId = String(data.batchId ?? "");
    if (batchId && m.batchIds.includes(batchId)) return ok({ duplicate: true, accepted: 0, dropped: 0, points: 0, serverTime: nowSeconds() });

    let accepted = 0;
    let dropped = 0;
    let points = 0;
    for (const a of actions) {
      const id = String(a.actionId ?? "");
      const ts = Math.floor(Number(a.eventTimestamp) || 0);
      if (!id || m.actionIds.includes(id) || !allowed.includes(String(a.source)) || ts < dayStart || ts > nowSeconds() + 60) {
        dropped++;
        continue;
      }
      const p = pointsOf(a, config);
      if (p <= 0) { dropped++; continue; }
      m.actionIds.push(id);
      points += p;
      accepted++;
    }
    m.actionIds = m.actionIds.slice(-400);
    if (batchId) m.batchIds = [...m.batchIds, batchId].slice(-50);
    if (points > 0) {
      const key = String(day);
      m.daily[key] = (m.daily[key] ?? 0) + points;
      m.weekly += points;
      const side = war.sides[clan.clanId];
      side.daily[key] = (side.daily[key] ?? 0) + points;
      tx.set(warRef, war);
    }
    tx.set(ref, m);
    return ok({ duplicate: false, accepted, dropped, points, serverTime: nowSeconds() });
  });
});

// ===== PvP ngày 6 =====

export const startclanwarpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string; targetUserId?: string; requestId?: string };
  const now = new Date();
  const config = await loadConfig(db());
  const ctx = await loadContext(uid, now);
  if (!ctx.clan) return fail("ERR_NOT_IN_CLAN", "Not in clan");
  if (!ctx.war || ctx.war.warId !== data.warId) return fail("ERR_NO_WAR", "No active war");
  if (warDayOf(now) !== 6) return fail("ERR_WAR_STATE", "Not PvP day");
  const roster = await enemyRoster(ctx.war, ctx.clan.clanId);
  const target = roster.find((r) => r.UserId === data.targetUserId);
  if (!target) return fail("ERR_TARGET_NOT_IN_ROUND", "Target not in round");
  const clan = ctx.clan;
  const warRef = db().collection(WARS).doc(ctx.war.warId);

  return db().runTransaction(async (tx) => {
    const war = (await tx.get(warRef)).data() as WarDoc;
    const { ref, m } = await getMember(tx, war.warId, uid, clan);
    syncTickets(m, war, config);
    if (pvpState(war, clan.clanId).defeated.includes(target.UserId)) return fail("ERR_ALREADY_DEFEATED", "Already defeated");
    if (m.pvpTickets <= 0) return fail("ERR_NO_TICKETS", "No tickets");
    m.pvpTickets--;
    m.activeBattle = { token: randomBytes(16).toString("hex"), targetUserId: target.UserId, expiresAt: nowSeconds() + BATTLE_TOKEN_TTL_SECONDS };
    tx.set(ref, m);
    tx.set(warRef, war);
    return ok({
      battleToken: m.activeBattle.token, battleTokenExpiresAt: m.activeBattle.expiresAt, opponent: target,
      tickets: m.pvpTickets, serverTime: nowSeconds(),
    });
  });
});

export const reportclanwarpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string; battleToken?: string; won?: boolean };
  const now = new Date();
  const config = await loadConfig(db());
  const ctx = await loadContext(uid, now);
  if (!ctx.clan) return fail("ERR_NOT_IN_CLAN", "Not in clan");
  if (!ctx.war || ctx.war.warId !== data.warId) return fail("ERR_NO_WAR", "No active war");
  const roster = await enemyRoster(ctx.war, ctx.clan.clanId);
  const clan = ctx.clan;
  const warRef = db().collection(WARS).doc(ctx.war.warId);

  return db().runTransaction(async (tx) => {
    const war = (await tx.get(warRef)).data() as WarDoc;
    const { ref, m } = await getMember(tx, war.warId, uid, clan);
    const battle = m.activeBattle;
    if (!battle || battle.token !== data.battleToken) return fail("ERR_VALIDATION", "Invalid battle token");
    m.activeBattle = null;
    const st = pvpState(war, clan.clanId);
    let pointsAwarded = 0;
    let alreadyDefeated = false;
    let roundReset = false;
    const won = data.won === true && battle.expiresAt >= nowSeconds() && warDayOf(now) === 6;
    if (won) {
      if (st.defeated.includes(battle.targetUserId)) {
        alreadyDefeated = true;
      } else {
        st.defeated.push(battle.targetUserId);
        pointsAwarded = config.pvp.winPoints || config.awards.pvpWin;
        m.daily["6"] = (m.daily["6"] ?? 0) + pointsAwarded;
        m.weekly += pointsAwarded;
        const side = war.sides[clan.clanId];
        side.daily["6"] = (side.daily["6"] ?? 0) + pointsAwarded;
        if (st.defeated.length >= roster.length && roster.length > 0 && resetEnabled(war, config, clan.clanId)) {
          st.round++;
          st.resetCount++;
          st.defeated = [];
          roundReset = true;
        }
      }
    }
    syncTickets(m, war, config);
    tx.set(ref, m);
    tx.set(warRef, war);
    return ok({
      won, pointsAwarded, alreadyDefeated, defeatedCount: st.defeated.length, roundReset, round: st.round,
      tickets: m.pvpTickets, serverTime: nowSeconds(),
    });
  });
});

// ===== Thưởng =====

async function claimCommon(uid: string, warId: string | undefined,
  fn: (m: MemberDoc, war: WarDoc, config: ClanWarConfigDTO, day: number) => { error?: [string, string]; rewards?: ClanWarRewardsDTO; extra?: object }) {
  const now = new Date();
  const config = await loadConfig(db());
  const ctx = await loadContext(uid, now);
  if (!ctx.clan) return fail("ERR_NOT_IN_CLAN", "Not in clan");
  if (!ctx.war || ctx.war.warId !== warId) return fail("ERR_NO_WAR", "No active war");
  if (warDayOf(now) >= 7) {
    const members = await loadMembers(ctx.war.warId);
    if (concludeDays(ctx.war, members, config, now)) await db().collection(WARS).doc(ctx.war.warId).set(ctx.war);
  }
  const clan = ctx.clan;
  const warRef = db().collection(WARS).doc(ctx.war.warId);
  return db().runTransaction(async (tx) => {
    const war = (await tx.get(warRef)).data() as WarDoc;
    const { ref, m } = await getMember(tx, war.warId, uid, clan);
    const r = fn(m, war, config, warDayOf(now));
    if (r.error) return fail(r.error[0], r.error[1]);
    tx.set(ref, m);
    return ok({ ...(r.extra ?? {}), rewards: r.rewards, serverTime: nowSeconds() });
  });
}

export const claimclanwarmilestone = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string; threshold?: number };
  const threshold = Math.floor(Number(data.threshold) || 0);
  return claimCommon(uid, data.warId, (m, _war, config) => {
    const row = config.individualMilestones.find((x) => x.threshold === threshold);
    if (!row) return { error: ["ERR_VALIDATION", "Unknown milestone"] };
    if (m.claimedMilestones.includes(threshold) || m.bundleClaimed) return { error: ["ERR_ALREADY_CLAIMED", "Already claimed"] };
    if (m.weekly < threshold) return { error: ["ERR_MILESTONE_NOT_REACHED", "Milestone not reached"] };
    m.claimedMilestones.push(threshold);
    return { rewards: milestoneRewards(row), extra: { threshold } };
  });
});

export const claimclanwarpersonalbundle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string };
  return claimCommon(uid, data.warId, (m, _war, config, day) => {
    if (day < 7) return { error: ["ERR_WAR_STATE", "Available during cooldown"] };
    if (m.bundleClaimed) return { error: ["ERR_ALREADY_CLAIMED", "Already claimed"] };
    const rows = config.individualMilestones.filter((x) => m.weekly >= x.threshold && !m.claimedMilestones.includes(x.threshold));
    if (rows.length === 0) return { error: ["ERR_NOTHING_TO_CLAIM", "Nothing to claim"] };
    let total = emptyRewards();
    for (const r of rows) total = addRewards(total, milestoneRewards(r));
    m.claimedMilestones.push(...rows.map((r) => r.threshold));
    m.bundleClaimed = true;
    return { rewards: total, extra: { thresholds: rows.map((r) => r.threshold) } };
  });
});

export const claimclanwarclanreward = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { warId?: string };
  return claimCommon(uid, data.warId, (m, war, config, day) => {
    if (day < 7 || !war.winnerClanId) return { error: ["ERR_WAR_STATE", "Available during cooldown"] };
    if (m.clanRewardClaimed) return { error: ["ERR_ALREADY_CLAIMED", "Already claimed"] };
    if (m.weekly <= 0) return { error: ["ERR_NOT_ELIGIBLE", "Not eligible"] };
    const outcome = war.winnerClanId === m.clanId ? "win" : "lose";
    const rewards = config.clanRewards[war.sides[m.clanId].rewardTier]?.[outcome] ?? emptyRewards();
    m.clanRewardClaimed = true;
    return { rewards, extra: { outcome } };
  });
});

// ===== Leaderboard =====

export const getclanwarcontributionleaderboard = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; mode?: string; day?: number };
  const now = new Date();
  const ctx = await loadContext(uid, now);
  if (!ctx.clan || !ctx.war) return fail("ERR_NO_WAR", "No active war");
  const war = ctx.war;
  const clanId = war.clanIds.includes(String(data.clanId)) ? String(data.clanId) : ctx.clan.clanId;
  const mode = data.mode === "weekly" ? "weekly" : "daily";
  const day = Math.min(6, Math.max(1, Math.floor(Number(data.day) || warDayOf(now))));
  let entries: { userId: string; playerName: string; avatarId: number; points: number }[];
  if (war.sides[clanId].isBot) {
    const total = mode === "weekly" ? weeklyTotal(war, clanId, now) : sideDaily(war, clanId, day, now);
    const weights = war.botRoster.map((b, i) => botFactor(war.warId, i + day * 100));
    const sum = weights.reduce((s, w) => s + w, 0) || 1;
    entries = war.botRoster.map((b, i) => ({ userId: b.UserId, playerName: b.PlayerName, avatarId: b.AvatarId, points: Math.round(total * weights[i] / sum) }));
  } else {
    const members = (await loadMembers(war.warId)).filter((m) => m.clanId === clanId);
    entries = members.map((m) => ({
      userId: m.uid, playerName: m.playerName, avatarId: m.avatarId, points: mode === "weekly" ? m.weekly : (m.daily?.[String(day)] ?? 0),
    }));
  }
  entries.sort((a, b) => b.points - a.points);
  return ok({ mode, day, clanId, entries: entries.map((e, i) => ({ rank: i + 1, ...e })) });
});

function clanRow(c: ClanDoc, rank: number) {
  return {
    rank, clanId: c.clanId, clanName: c.clanName, bannerBackgroundTypeId: c.bannerBackgroundTypeId,
    bannerBackgroundColorId: c.bannerBackgroundColorId, bannerImageTypeId: c.bannerImageTypeId,
    bannerImageColorId: c.bannerImageColorId, tier: c.clanTier || tierOf(c.tierPoints || 0), tierPoints: c.tierPoints || 0,
    totalPower: c.totalPower || 0, server: c.server,
  };
}

export const getclanwarclanleaderboard = onCall(async (request) => {
  const uid = requireUid(request);
  const [top, champ, ps] = await Promise.all([
    db().collection(CLANS).orderBy("tierPoints", "desc").limit(50).get(),
    db().collection(META).doc("champion").get(),
    db().collection(CLAN_PLAYERS).doc(uid).get(),
  ]);
  const clans = top.docs.map((d) => d.data() as ClanDoc).filter((c) => !c.isBot);
  clans.sort((a, b) => (b.tierPoints || 0) - (a.tierPoints || 0) || (b.totalPower || 0) - (a.totalPower || 0));
  let leader = null;
  const champId = champ.data()?.clanId as string | undefined;
  if (champId) {
    const cs = await db().collection(CLANS).doc(champId).get();
    if (cs.exists) leader = clanRow(cs.data() as ClanDoc, 0);
  }
  return ok({ leader, rows: clans.map((c, i) => clanRow(c, i + 1)), myClanId: (ps.data() as ClanPlayerDoc | undefined)?.clanId ?? "" });
});

// Xếp hạng tuần theo tổng điểm đóng góp (clan dẫn đầu được thách đấu Champion tuần sau).
async function leadershipRows(weekId: string, now: Date) {
  const wars = await db().collection(WARS).where("weekId", "==", weekId).get();
  const totals = new Map<string, number>();
  for (const d of wars.docs) {
    const w = d.data() as WarDoc;
    for (const id of w.clanIds) if (!w.sides[id].isBot) totals.set(id, weeklyTotal(w, id, now));
  }
  const ids = [...totals.keys()];
  const snaps = await Promise.all(ids.map((id) => db().collection(CLANS).doc(id).get()));
  const rows = snaps.filter((s) => s.exists).map((s) => s.data() as ClanDoc)
    .map((c) => ({ c, pts: totals.get(c.clanId) ?? 0 }))
    .sort((a, b) => b.pts - a.pts);
  return rows.map((r, i) => ({ ...clanRow(r.c, i + 1), tierPoints: r.pts }));
}

export const getclanwarleadershipranking = onCall(async (request) => {
  const uid = requireUid(request);
  const now = new Date();
  const ps = await db().collection(CLAN_PLAYERS).doc(uid).get();
  const weekId = weekIdOf(now);
  const rows = await leadershipRows(weekId, now);
  return ok({ weekId, rows: rows.slice(0, 50), myClanId: (ps.data() as ClanPlayerDoc | undefined)?.clanId ?? "" });
});

// ===== Ghép cặp đầu tuần =====

const BOT_NAMES = ["Ironclad", "Shadowfang", "Stormborn", "Dragonhold", "Bloodmoon", "Frostguard", "Nightwatch", "Ravenclaw", "Goldcrest", "Thornwall"];

function newSide(c: ClanDoc): SideDoc {
  return {
    clanId: c.clanId, clanName: c.clanName, bannerBackgroundTypeId: c.bannerBackgroundTypeId,
    bannerBackgroundColorId: c.bannerBackgroundColorId, bannerImageTypeId: c.bannerImageTypeId,
    bannerImageColorId: c.bannerImageColorId, server: c.server, rewardTier: c.clanTier || tierOf(c.tierPoints || 0),
    isBot: false, memberCountAtStart: c.memberCount, daily: {},
  };
}

async function botSideFor(c: ClanDoc, seed: string): Promise<{ side: SideDoc; roster: PvPPlayerModel[] }> {
  const id = "bot_" + randomBytes(6).toString("hex");
  const name = BOT_NAMES[Math.floor(botFactor(seed, 1) * 1000) % BOT_NAMES.length] + Math.floor(10 + botFactor(seed, 2) * 89);
  const members = Object.values(c.members);
  const snaps = await Promise.all(members.map((m) => db().collection("pvpPlayers").doc(m.userId).get()));
  const roster: PvPPlayerModel[] = [];
  const count = Math.max(5, Math.min(30, members.length));
  for (let i = 0; i < count; i++) {
    const srcIdx = i % members.length;
    const src = snaps[srcIdx].exists ? (snaps[srcIdx].data() as PvPPlayerDoc) : null;
    const power = Math.round((members[srcIdx].power || 100) * (0.8 + botFactor(seed, 30 + i) * 0.3));
    roster.push(botModel(id + "_" + i, "Player" + Math.floor(1000 + botFactor(seed, 60 + i) * 8999), src, power));
  }
  const side: SideDoc = {
    clanId: id, clanName: name, bannerBackgroundTypeId: Math.floor(botFactor(seed, 3) * 8) % 8,
    bannerBackgroundColorId: Math.floor(botFactor(seed, 4) * 10) % 10, bannerImageTypeId: Math.floor(botFactor(seed, 5) * 20) % 20,
    bannerImageColorId: Math.floor(botFactor(seed, 6) * 8) % 8, server: c.server, rewardTier: c.clanTier || "D",
    isBot: true, memberCountAtStart: roster.length, daily: {},
  };
  return { side, roster };
}

async function createWar(weekId: string, a: ClanDoc, b: ClanDoc | null, type: "normal" | "leadership"): Promise<string> {
  const warId = weekId + "_" + a.clanId + "_" + (b ? b.clanId : "bot");
  const sides: Record<string, SideDoc> = { [a.clanId]: newSide(a) };
  let botRoster: PvPPlayerModel[] = [];
  let bId: string;
  if (b) {
    sides[b.clanId] = newSide(b);
    bId = b.clanId;
  } else {
    const bot = await botSideFor(a, warId);
    sides[bot.side.clanId] = bot.side;
    botRoster = bot.roster;
    bId = bot.side.clanId;
  }
  const war: WarDoc = {
    warId, weekId, type, clanIds: [a.clanId, bId], sides, botRoster, pvp: {}, dailyResults: {}, finalized: false,
    winnerClanId: "", createdAt: nowSeconds(),
  };
  const batch = db().batch();
  batch.set(db().collection(WARS).doc(warId), war);
  batch.set(db().collection(WAR_INDEX).doc(weekId + "_" + a.clanId), { warId });
  if (b) batch.set(db().collection(WAR_INDEX).doc(weekId + "_" + b.clanId), { warId });
  await batch.commit();
  return warId;
}

// Chốt toàn bộ war của tuần trước (tier, champion) — cron gọi trước khi ghép tuần mới.
async function finalizeWeek(weekId: string, now: Date): Promise<number> {
  const config = await loadConfig(db());
  const wars = await db().collection(WARS).where("weekId", "==", weekId).get();
  let n = 0;
  for (const d of wars.docs) {
    const war = d.data() as WarDoc;
    if (war.finalized) continue;
    const members = await loadMembers(war.warId);
    concludeDays(war, members, config, now);
    await d.ref.set(war);
    await applyFinalization(war);
    n++;
  }
  return n;
}

export async function matchClanWars(now: Date): Promise<{ weekId: string; wars: number }> {
  const weekId = weekIdOf(now);
  const prevWeekId = weekIdOf(new Date(weekStartOf(now).getTime() - 86400000));
  await finalizeWeek(prevWeekId, now);

  const snap = await db().collection(CLANS).get();
  const clans = snap.docs.map((d) => d.data() as ClanDoc).filter((c) => !c.isBot && c.memberCount > 0);
  const idx = await Promise.all(clans.map((c) => db().collection(WAR_INDEX).doc(weekId + "_" + c.clanId).get()));
  let pool = clans.filter((_, i) => !idx[i].exists);
  let created = 0;

  // Trận Championship: Champion vs clan đứng đầu xếp hạng tuần trước (không có Champion → top 2).
  const champ = (await db().collection(META).doc("champion").get()).data()?.clanId as string | undefined;
  const ranking = await leadershipRows(prevWeekId, now);
  const inPool = (id: string) => pool.find((c) => c.clanId === id);
  let a: ClanDoc | undefined;
  let b: ClanDoc | undefined;
  if (champ && inPool(champ)) {
    a = inPool(champ);
    const ch = ranking.find((r) => r.clanId !== champ && inPool(r.clanId));
    b = ch ? inPool(ch.clanId) : undefined;
  } else {
    const top = ranking.filter((r) => inPool(r.clanId)).slice(0, 2);
    if (top.length === 2) { a = inPool(top[0].clanId); b = inPool(top[1].clanId); }
  }
  if (a && b) {
    await createWar(weekId, a, b, "leadership");
    pool = pool.filter((c) => c !== a && c !== b);
    created++;
  }

  pool.sort((x, y) => (y.tierPoints || 0) - (x.tierPoints || 0) || (y.totalPower || 0) - (x.totalPower || 0));
  for (let i = 0; i < pool.length; i += 2) {
    await createWar(weekId, pool[i], i + 1 < pool.length ? pool[i + 1] : null, "normal");
    created++;
  }
  return { weekId, wars: created };
}

// Thứ Ba 00:05 UTC.
export const matchclanwarsweekly = onSchedule({ schedule: "5 0 * * 2", timeZone: "UTC" }, async () => {
  await matchClanWars(new Date());
});

// ===== Admin (emulator hoặc uid trong config/admins) =====

async function requireAdmin(uid: string | undefined): Promise<void> {
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  if (process.env.FUNCTIONS_EMULATOR === "true") return;
  const snap = await db().collection("config").doc("admins").get();
  const uids: string[] = (snap.data()?.uids as string[]) ?? [];
  if (!uids.includes(uid)) throw new HttpsError("permission-denied", "Admin only");
}

export const matchclanwarsnow = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  const r = await matchClanWars(new Date());
  return ok(r);
});

// Test: cộng điểm cho clan bot / clan mình ở ngày bất kỳ (để thử thắng/thua ngày).
export const seedclanwarpoints = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  const uid = request.auth!.uid;
  const data = (request.data ?? {}) as { day?: number; points?: number };
  const now = new Date();
  const ctx = await loadContext(uid, now);
  if (!ctx.war || !ctx.clan) return fail("ERR_NO_WAR", "No war");
  const day = String(Math.min(6, Math.max(1, Math.floor(Number(data.day) || warDayOf(now)))));
  const pts = Math.floor(Number(data.points) || 1000);
  const war = ctx.war;
  war.sides[ctx.clan.clanId].daily[day] = (war.sides[ctx.clan.clanId].daily[day] ?? 0) + pts;
  await db().collection(WARS).doc(war.warId).set(war);
  return ok({ day, points: pts });
});
