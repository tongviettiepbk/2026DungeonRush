import { randomBytes } from "crypto";
import { getFirestore, Firestore } from "firebase-admin/firestore";
import { CallableRequest, HttpsError, onCall } from "firebase-functions/v2/https";
import { sanitizeCompanions, sanitizeEnchantmentTiers, sanitizeItems } from "./bossRush";
import { botLoadout, loadBank } from "./bots";
import { BossRushCompanionModel, BossRushItemModel, BotSeed, RewardEntry } from "./models";
import {
  BATTLE_TOKEN_TTL_SECONDS, DAILY_TICKETS, GROUP_CAPACITY, LEAGUE_NAMES, PvPRemoteConfig, ROSTER_TTL_SECONDS, START_LEAGUE,
  assignStars, clampLeague, demoteCount, loadPvPConfig, pickOpponents, promoteCount, seasonRewardTable, seasonRewards,
} from "./pvpConfig";
import { PvPGroupDoc, PvPGroupEntry, PvPRankEntry, createGroup, finalizeGroup, groupCount, pvpBotParams, rankGroup } from "./pvpGroup";
import { getDayKey, getEventEnd, getEventKey, getEventStart, getNextEventStart, isEventActive } from "./schedule";

// PvP theo mùa + nhóm league (GDD/PVP_DESIGN.md): openpvp, findpvpopponents, startpvpbattle, reportpvpbattle,
// claimpvpseason. Trận đánh chạy ở client với SNAPSHOT đồ của đối thủ; server giữ league, cúp, vé, nhóm, roster, token.
// Nhóm được chốt lười: thành viên đầu tiên mở PvP sau khi hết mùa sẽ chốt cả nhóm, không cần tác vụ hẹn giờ.
// Lỗi nghiệp vụ trả {success:false, message}; chỉ lỗi xác thực mới ném HttpsError.

const PLAYERS = "pvpPlayers";
export const GROUPS = "pvpGroups";
const CONTENT_VERSION = "1.0.0";
const MAX_GROUP_SEARCH = 1000;

export interface PvPTicketsDTO {
  remaining: number;
  dailyTickets: number;
  dayKey: string;
}

// Wing/cape nằm chung Items (slot 7/6) như Boss Rush.
export interface PvPPlayerModel {
  UserId: string;
  PlayerName: string;
  CountryCode: string;
  AvatarId: number;
  Position: number;
  Trophy: number;
  Power: number;
  SnapshotContentVersion: string;
  ProjectedWinTrophy: number;
  ProjectedLossTrophy: number;
  StarsOnWin?: number;
  Items: BossRushItemModel[];
  Companions: BossRushCompanionModel[];
  ShowCloak: boolean;
  AttacksUsed: number;
  EnchantmentTiers: number[];
  IsBot: boolean;
}

interface Roster {
  token: string;
  expiresAt: number;        // unix giây
  opponentIds: string[];
  stars: { [id: string]: number };
}

interface ActiveBattle {
  token: string;
  opponentUserId: string;
  starsOnWin: number;
  seasonKey: string;
  groupId: string;
  startLeagueIndex: number;
  startedAt: number;        // unix giây
  expiresAt: number;        // unix giây
}

// Kết quả mùa chờ bấm Claim ở popup Season Complete.
interface PendingSeason {
  seasonKey: string;
  leagueIndex: number;
  newLeagueIndex: number;
  finalRank: number;
  memberCount: number;
  trophy: number;
  rewards: RewardEntry[];
}

// Document pvpPlayers/{uid}
export interface PvPPlayerDoc {
  uid: string;
  playerName: string;
  countryCode: string;
  avatarId: number;
  power: number;
  items: BossRushItemModel[];
  companions: BossRushCompanionModel[];
  enchantmentTiers: number[];
  showCloak: boolean;
  snapshotHash: string;
  contentVersion: string;
  league: number;
  seasonKey: string;
  groupId: string;
  trophy: number;           // cúp trong mùa hiện tại
  pendingSeason: PendingSeason | null;
  attacksUsed: number;
  wins: number;
  losses: number;
  tickets: PvPTicketsDTO;
  roster: Roster | null;
  activeBattle: ActiveBattle | null;
  isBot: boolean;
  updatedAt: number;        // ms
}

interface SnapshotRequest {
  playerName?: string;
  countryCode?: string;
  avatarId?: number;
  power?: number;
  snapshotHash?: string;
  contentVersion?: string;
  items?: BossRushItemModel[];
  companions?: BossRushCompanionModel[];
  enchantmentTiers?: number[];
  showCloak?: boolean;
}

function db(): Firestore {
  return getFirestore();
}

function requireUid(request: CallableRequest<unknown>): string {
  const uid = request.auth?.uid;
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  return uid;
}

function nowSeconds(): number {
  return Math.floor(Date.now() / 1000);
}

function toSeconds(d: Date): number {
  return Math.floor(d.getTime() / 1000);
}

function newToken(): string {
  return randomBytes(16).toString("hex");
}

// ===== Vé =====

function freshTickets(dayKey: string): PvPTicketsDTO {
  return { remaining: DAILY_TICKETS, dailyTickets: DAILY_TICKETS, dayKey };
}

function resetTicketsIfNewDay(player: PvPPlayerDoc, now: Date): void {
  const dayKey = getDayKey(now);
  if (player.tickets?.dayKey !== dayKey || typeof player.tickets.remaining !== "number") {
    player.tickets = freshTickets(dayKey);
  }
}

function newPlayerDoc(uid: string): PvPPlayerDoc {
  return {
    uid,
    playerName: "",
    countryCode: "",
    avatarId: 0,
    power: 0,
    items: [],
    companions: [],
    enchantmentTiers: [],
    showCloak: true,
    snapshotHash: "",
    contentVersion: CONTENT_VERSION,
    league: START_LEAGUE,
    seasonKey: "",
    groupId: "",
    trophy: 0,
    pendingSeason: null,
    attacksUsed: 0,
    wins: 0,
    losses: 0,
    tickets: freshTickets(getDayKey(new Date())),
    roster: null,
    activeBattle: null,
    isBot: false,
    updatedAt: Date.now(),
  };
}

// Document hệ Elo cũ chưa có league/mùa: về Iron, cúp sẽ reset lúc vào nhóm.
function readPlayer(uid: string, data: FirebaseFirestore.DocumentData | undefined): PvPPlayerDoc {
  if (!data) return newPlayerDoc(uid);
  const player = data as PvPPlayerDoc;
  player.league = clampLeague(player.league ?? START_LEAGUE);
  player.seasonKey = player.seasonKey ?? "";
  player.groupId = player.groupId ?? "";
  player.pendingSeason = player.pendingSeason ?? null;
  return player;
}

function applySnapshot(player: PvPPlayerDoc, data: SnapshotRequest): void {
  if (typeof data.playerName === "string" && data.playerName.length > 0) {
    player.playerName = data.playerName.substring(0, 24);
  }
  if (typeof data.countryCode === "string") player.countryCode = data.countryCode.substring(0, 4).toUpperCase();
  if (typeof data.avatarId === "number" && isFinite(data.avatarId)) player.avatarId = Math.max(0, Math.floor(data.avatarId));
  if (typeof data.power === "number" && isFinite(data.power) && data.power >= 0) player.power = data.power;
  if (typeof data.snapshotHash === "string") player.snapshotHash = data.snapshotHash.substring(0, 64);
  if (typeof data.contentVersion === "string") player.contentVersion = data.contentVersion.substring(0, 16);
  if (data.items !== undefined) player.items = sanitizeItems(data.items);
  if (data.companions !== undefined) player.companions = sanitizeCompanions(data.companions);
  if (data.enchantmentTiers !== undefined) player.enchantmentTiers = sanitizeEnchantmentTiers(data.enchantmentTiers);
  if (typeof data.showCloak === "boolean") player.showCloak = data.showCloak;
}

function realModel(p: PvPPlayerDoc, trophy: number, viewerTrophy: number, stars: number): PvPPlayerModel {
  return {
    UserId: p.uid,
    PlayerName: p.playerName,
    CountryCode: p.countryCode,
    AvatarId: p.avatarId,
    Position: 0,
    Trophy: trophy,
    Power: p.power,
    SnapshotContentVersion: p.contentVersion,
    ProjectedWinTrophy: viewerTrophy + stars,
    ProjectedLossTrophy: viewerTrophy,
    StarsOnWin: stars,
    Items: p.items ?? [],
    Companions: p.companions ?? [],
    ShowCloak: p.showCloak ?? true,
    AttacksUsed: p.attacksUsed ?? 0,
    EnchantmentTiers: p.enchantmentTiers ?? [],
    IsBot: false,
  };
}

// Chưa có ngân hàng bộ đồ thì bot mượn đồ người xem.
function botModel(bot: BotSeed, e: PvPRankEntry, group: PvPGroupDoc, now: number, viewer: PvPPlayerDoc, stars: number): PvPPlayerModel {
  const step = botLoadout(bot, pvpBotParams(group), now, loadBank());
  return {
    UserId: bot.id,
    PlayerName: e.name,
    CountryCode: "",
    AvatarId: e.avatarId,
    Position: 0,
    Trophy: e.trophy,
    Power: e.power,
    SnapshotContentVersion: CONTENT_VERSION,
    ProjectedWinTrophy: viewer.trophy + stars,
    ProjectedLossTrophy: viewer.trophy,
    StarsOnWin: stars,
    Items: step ? step.items : viewer.items ?? [],
    Companions: step ? step.companions : viewer.companions ?? [],
    ShowCloak: step ? step.showCloak : true,
    AttacksUsed: e.fights,
    EnchantmentTiers: step ? step.enchantmentTiers : viewer.enchantmentTiers ?? [],
    IsBot: true,
  };
}

function groupEntryOf(player: PvPPlayerDoc, trophy: number, fights: number, joinedAt: number): PvPGroupEntry {
  return {
    UserId: player.uid, PlayerName: player.playerName, AvatarId: player.avatarId, Power: player.power, Trophy: trophy,
    Fights: fights, JoinedAt: joinedAt,
  };
}

function seasonOf(now: Date) {
  return {
    key: getEventKey(now),
    isActive: isEventActive(now),
    startsAt: toSeconds(getEventStart(now)),
    endsAt: toSeconds(getEventEnd(now)),
    nextStartsAt: toSeconds(getNextEventStart(now)),
  };
}

function pendingDTO(pending: PendingSeason | null) {
  return pending ? { ...pending, leagueName: LEAGUE_NAMES[pending.leagueIndex - 1] } : null;
}

// ===== openPvP =====

// Kết quả mùa cũ của một thành viên (không có thưởng + giữ league thì không cần popup).
function settleSeason(player: PvPPlayerDoc, group: PvPGroupDoc, results: PvPGroupDoc["results"]): void {
  const result = results[player.uid];
  if (!result) return;
  const rewards = result.fights > 0 ? seasonRewards(group.league, result.rank) : [];
  if (rewards.length > 0 || result.newLeague !== group.league) {
    player.pendingSeason = {
      seasonKey: group.seasonKey,
      leagueIndex: group.league,
      newLeagueIndex: result.newLeague,
      finalRank: result.rank,
      memberCount: result.count,
      trophy: result.trophy,
      rewards,
    };
  }
  player.league = result.newLeague;
}

export const openpvp = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as SnapshotRequest;
  const now = new Date();
  const config: PvPRemoteConfig = await loadPvPConfig(db());
  const ref = db().collection(PLAYERS).doc(uid);
  const eventKey = getEventKey(now);
  const bank = loadBank();

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const player = readPlayer(uid, snap.data());
    resetTicketsIfNewDay(player, now);
    applySnapshot(player, data);

    // Firestore bắt đọc hết rồi mới ghi nên gom các lệnh ghi nhóm lại, chạy sau cùng.
    const writes: (() => void)[] = [];

    // 1. Hết mùa → chốt nhóm cũ (nếu chưa ai chốt), lấy kết quả của mình.
    if (player.groupId && player.seasonKey !== eventKey) {
      const oldRef = db().collection(GROUPS).doc(player.groupId);
      const oldSnap = await tx.get(oldRef);
      if (oldSnap.exists) {
        const old = oldSnap.data() as PvPGroupDoc;
        const results = old.isFinalized ? old.results : finalizeGroup(old, config, bank);
        if (!old.isFinalized) writes.push(() => tx.update(oldRef, { isFinalized: true, results }));
        settleSeason(player, old, results);
      }
      player.groupId = "";
      player.seasonKey = "";
      player.trophy = 0;
      player.roster = null;
      player.activeBattle = null;
    }

    // 2. Đang trong mùa → lấy nhóm hiện tại, chưa có thì vào nhóm còn chỗ đầu tiên của league.
    let group: PvPGroupDoc | null = null;
    if (eventKey) {
      if (player.groupId) {
        const current = await tx.get(db().collection(GROUPS).doc(player.groupId));
        if (current.exists) group = current.data() as PvPGroupDoc;
      }
      if (!group) {
        player.trophy = 0;
        player.roster = null;
        player.activeBattle = null;
        const entry = groupEntryOf(player, 0, 0, now.getTime());
        for (let n = 1; n <= MAX_GROUP_SEARCH && !group; n++) {
          const groupId = eventKey + "_L" + player.league + "_" + n;
          const groupRef = db().collection(GROUPS).doc(groupId);
          const candidate = await tx.get(groupRef);
          if (!candidate.exists) {
            const created = createGroup(groupId, eventKey, player.league, [entry], config.groupMinSize, now.getTime(), bank);
            writes.push(() => tx.set(groupRef, created));
            group = created;
          } else {
            const existing = candidate.data() as PvPGroupDoc;
            if (groupCount(existing) < GROUP_CAPACITY) {
              existing.players[uid] = entry;
              writes.push(() => tx.update(groupRef, { ["players." + uid]: entry }));
              group = existing;
            }
          }
        }
        if (!group) return { success: false, message: "Failed to load PvP." };
        player.groupId = group.groupId;
        player.seasonKey = eventKey;
      } else {
        // Cập nhật tên + avatar + sức mạnh trong nhóm khi đổi.
        const old = group.players[uid];
        const entry = groupEntryOf(player, player.trophy, old?.Fights ?? 0, old?.JoinedAt ?? now.getTime());
        if (!old || old.PlayerName !== entry.PlayerName || old.AvatarId !== entry.AvatarId || old.Power !== entry.Power) {
          group.players[uid] = entry;
          const groupRef = db().collection(GROUPS).doc(group.groupId);
          writes.push(() => tx.update(groupRef, { ["players." + uid]: entry }));
        }
      }
    }

    player.updatedAt = now.getTime();
    writes.forEach((write) => write());
    tx.set(ref, player);

    const ranked = group ? rankGroup(group, now.getTime(), bank) : [];
    const me = ranked.find((e) => e.id === uid);
    return {
      success: true,
      message: "",
      userId: uid,
      hasName: player.playerName.length > 0,
      playerName: player.playerName,
      trophy: player.trophy,
      leagueIndex: player.league,
      contentVersion: CONTENT_VERSION,
      tickets: player.tickets,
      rewardTable: seasonRewardTable(),
      season: seasonOf(now),
      playerRank: me?.position ?? 0,
      leaderboard: ranked.map((e) => ({
        UserId: e.id, PlayerName: e.name, AvatarId: e.avatarId, Power: e.power, Trophy: e.trophy, Rank: e.position,
        IsCurrentPlayer: e.id === uid,
      })),
      promotion: {
        promoteCount: promoteCount(config, player.league, ranked.length),
        demoteCount: demoteCount(config, player.league, ranked.length),
      },
      pendingSeasonResult: pendingDTO(player.pendingSeason),
    };
  });
});

// ===== claimPvPSeason =====

export const claimpvpseason = onCall(async (request) => {
  const uid = requireUid(request);
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to load PvP." };
    const player = readPlayer(uid, snap.data());
    const pending = player.pendingSeason;
    if (!pending) return { success: false, message: "No season rewards to claim." };
    tx.update(ref, { pendingSeason: null, updatedAt: Date.now() });
    return { success: true, message: "", rewards: pending.rewards };
  });
});

// ===== findPvPOpponents =====

async function loadCurrentGroup(player: PvPPlayerDoc, now: Date): Promise<PvPGroupDoc | null> {
  const eventKey = getEventKey(now);
  if (!eventKey || player.seasonKey !== eventKey || !player.groupId) return null;
  const snap = await db().collection(GROUPS).doc(player.groupId).get();
  return snap.exists ? (snap.data() as PvPGroupDoc) : null;
}

export const findpvpopponents = onCall(async (request) => {
  const uid = requireUid(request);
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);
  const snap = await ref.get();
  if (!snap.exists) return { success: false, message: "Failed to load PvP." };

  const player = readPlayer(uid, snap.data());
  resetTicketsIfNewDay(player, now);
  const group = await loadCurrentGroup(player, now);
  if (!group) return { success: false, message: "Season ended.", tickets: player.tickets };

  const bank = loadBank();
  const ranked = rankGroup(group, now.getTime(), bank).filter((e) => e.id !== uid);
  const picked = pickOpponents(ranked, player.trophy, Math.random);

  // Đọc snapshot người thật trước khi phát cúp: người đã xoá tài khoản thì bỏ, không để hở một mức cúp.
  const realIds = picked.filter((e) => !e.isBot).map((e) => e.id);
  const docs = new Map<string, PvPPlayerDoc>();
  if (realIds.length > 0) {
    const snaps = await db().getAll(...realIds.map((id) => db().collection(PLAYERS).doc(id)));
    snaps.forEach((s) => { if (s.exists) docs.set(s.id, s.data() as PvPPlayerDoc); });
  }
  const usable = picked.filter((e) => e.isBot || docs.has(e.id));
  const botById = new Map((group.bots ?? []).map((b) => [b.id, b]));
  const assigned = assignStars(usable);

  const stars: { [id: string]: number } = {};
  assigned.forEach((a) => { stars[a.opponent.id] = a.stars; });
  const roster: Roster = {
    token: newToken(),
    expiresAt: nowSeconds() + ROSTER_TTL_SECONDS,
    opponentIds: assigned.map((a) => a.opponent.id),
    stars,
  };
  await ref.update({ roster, tickets: player.tickets });

  return {
    success: true,
    message: "",
    rosterToken: roster.token,
    rosterExpiresAt: roster.expiresAt,
    opponents: assigned.map((a) => (a.opponent.isBot
      ? botModel(botById.get(a.opponent.id) as BotSeed, a.opponent, group, now.getTime(), player, a.stars)
      : realModel(docs.get(a.opponent.id) as PvPPlayerDoc, a.opponent.trophy, player.trophy, a.stars))),
    tickets: player.tickets,
  };
});

// ===== startPvPBattle =====

export const startpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { rosterToken?: string; opponentUserId?: string };
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);
  const eventKey = getEventKey(now);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to load PvP." };
    const player = readPlayer(uid, snap.data());
    resetTicketsIfNewDay(player, now);
    if (!eventKey || player.seasonKey !== eventKey || !player.groupId) {
      return { success: false, message: "Season ended.", tickets: player.tickets };
    }

    const roster = player.roster;
    if (!roster || roster.token !== data.rosterToken || roster.expiresAt < nowSeconds()) {
      tx.set(ref, player);
      return { success: false, message: "Opponent roster expired.", tickets: player.tickets };
    }
    const opponentId = String(data.opponentUserId ?? "");
    if (!roster.opponentIds.includes(opponentId)) {
      return { success: false, message: "Opponent not found.", tickets: player.tickets };
    }

    const groupSnap = await tx.get(db().collection(GROUPS).doc(player.groupId));
    if (!groupSnap.exists) return { success: false, message: "Season ended.", tickets: player.tickets };
    const group = groupSnap.data() as PvPGroupDoc;
    const entry = rankGroup(group, now.getTime(), loadBank()).find((e) => e.id === opponentId);
    if (!entry) return { success: false, message: "Opponent not found.", tickets: player.tickets };

    const stars = roster.stars[opponentId] ?? 0;
    let opponent: PvPPlayerModel;
    if (entry.isBot) {
      opponent = botModel((group.bots ?? []).find((b) => b.id === opponentId) as BotSeed, entry, group, now.getTime(), player, stars);
    } else {
      const oppSnap = await tx.get(db().collection(PLAYERS).doc(opponentId));
      if (!oppSnap.exists) return { success: false, message: "Opponent not found.", tickets: player.tickets };
      opponent = realModel(oppSnap.data() as PvPPlayerDoc, entry.trophy, player.trophy, stars);
    }

    if (player.tickets.remaining <= 0) {
      tx.set(ref, player);
      return { success: false, message: "No PvP tickets remaining.", tickets: player.tickets };
    }
    player.tickets.remaining--;

    const startedAt = nowSeconds();
    const battle: ActiveBattle = {
      token: newToken(),
      opponentUserId: opponentId,
      starsOnWin: stars,
      seasonKey: player.seasonKey,
      groupId: player.groupId,
      startLeagueIndex: player.league,
      startedAt,
      expiresAt: startedAt + BATTLE_TOKEN_TTL_SECONDS,
    };
    player.activeBattle = battle;
    // Đã đánh thì gỡ khỏi roster (không đánh lại cùng người trong 1 roster).
    roster.opponentIds = roster.opponentIds.filter((id) => id !== opponentId);
    player.updatedAt = now.getTime();
    tx.set(ref, player);

    return {
      success: true,
      message: "",
      battleToken: battle.token,
      battleTokenExpiresAt: battle.expiresAt,
      startedAt,
      startLeagueIndex: battle.startLeagueIndex,
      starsOnWin: stars,
      opponent,
      tickets: player.tickets,
    };
  });
});

// ===== reportPvPBattle =====

// Thắng nhận đúng số cúp đã hiện, thua không mất cúp, người bị đánh không đổi cúp.
export const reportpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { battleToken?: string; won?: boolean };
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to report battle." };
    const player = readPlayer(uid, snap.data());
    const battle = player.activeBattle;
    if (!battle || !data.battleToken || battle.token !== data.battleToken) {
      return { success: false, message: "Missing battle token.", tickets: player.tickets };
    }

    const groupRef = db().collection(GROUPS).doc(battle.groupId);
    const groupSnap = await tx.get(groupRef);
    const group = groupSnap.exists ? (groupSnap.data() as PvPGroupDoc) : null;

    // Nộp quá hạn → xử thua (forfeit). Hết mùa giữa trận thì trận không còn tính cúp.
    const forfeit = battle.expiresAt < nowSeconds();
    const won = !forfeit && data.won === true;
    const inSeason = getEventKey(now) === battle.seasonKey && player.seasonKey === battle.seasonKey && !!group && !group.isFinalized;

    const oldTrophy = player.trophy;
    if (inSeason) {
      if (won) player.trophy = oldTrophy + battle.starsOnWin;
      const entry = (group as PvPGroupDoc).players[uid];
      tx.update(groupRef, {
        ["players." + uid]: groupEntryOf(player, player.trophy, (entry?.Fights ?? 0) + 1, entry?.JoinedAt ?? now.getTime()),
      });
    }
    player.attacksUsed = (player.attacksUsed ?? 0) + 1;
    if (won) player.wins = (player.wins ?? 0) + 1;
    else player.losses = (player.losses ?? 0) + 1;
    player.activeBattle = null;
    player.updatedAt = now.getTime();
    tx.set(ref, player);

    return {
      success: true,
      message: "",
      won,
      forfeit,
      seasonEnded: !inSeason,
      oldTrophy,
      newTrophy: player.trophy,
      trophyDelta: player.trophy - oldTrophy,
      leagueIndex: player.league,
      tickets: player.tickets,
    };
  });
});

// ===== Admin =====

async function requireAdmin(uid: string | undefined): Promise<string> {
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  if (process.env.FUNCTIONS_EMULATOR === "true") return uid;
  const snap = await db().collection("config").doc("admins").get();
  const uids: string[] = (snap.data()?.uids as string[]) ?? [];
  if (!uids.includes(uid)) throw new HttpsError("permission-denied", "Admin only");
  return uid;
}

async function deleteCollection(name: string): Promise<number> {
  const snap = await db().collection(name).get();
  for (let i = 0; i < snap.docs.length; i += 400) {
    const batch = db().batch();
    snap.docs.slice(i, i + 400).forEach((d) => batch.delete(d.ref));
    await batch.commit();
  }
  return snap.docs.length;
}

export const removeallpvpplayers = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  const removed = await deleteCollection(PLAYERS);
  await deleteCollection(GROUPS);
  return { success: true, removed };
});
