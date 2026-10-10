import { randomBytes } from "crypto";
import { getFirestore, Firestore, Query } from "firebase-admin/firestore";
import { CallableRequest, HttpsError, onCall } from "firebase-functions/v2/https";
import { sanitizeCompanions, sanitizeEnchantmentTiers, sanitizeItems } from "./bossRush";
import { BossRushCompanionModel, BossRushItemModel } from "./models";
import {
  BATTLE_TOKEN_TTL_SECONDS, DAILY_FREE_TICKETS, LEADERBOARD_NEARBY, LEADERBOARD_TOP, MAX_AD_TICKETS_PER_DAY,
  MINIMUM_TROPHY, PvPLeagueRewardsDTO, ROSTER_SIZE, ROSTER_TTL_SECONDS, START_TROPHY, getLeagueIndex,
  getLeagueRewards, loadRewardTable, lossDelta, projectedLoss, projectedWin, winDelta,
} from "./pvpConfig";
import { getDayKey } from "./schedule";

// PvP Arena — 7 endpoint giống server gốc (tên viết thường như URL https://{fn}-umgnfrxyuq-uc.a.run.app/):
// initpvpprofile, openpvp, findpvpopponents, startpvpbattle, reportpvpbattle, grantpvpadticket, getpvpleaderboard.
// Trận đánh chạy ở client với SNAPSHOT đồ của đối thủ (PvP bất đồng bộ); server giữ trophy (Elo), vé, roster, token.
// Lỗi nghiệp vụ trả {success:false, message} như DTO gốc; chỉ lỗi xác thực mới ném HttpsError.

const PLAYERS = "pvpPlayers";
const CONTENT_VERSION = "1.0.0";

export interface PvPTicketsDTO {
  freeRemaining: number;
  adRemaining: number;
  adClaimedToday: number;
  maxAdPerDay: number;
  dailyFreeTickets: number;
  dayKey: string;
}

// PvPPlayerModel gốc. Khác gốc: wing/cape nằm chung Items (slot 7/6) như Boss Rush.
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
}

interface ActiveBattle {
  token: string;
  opponentUserId: string;
  myTrophyAtStart: number;
  opponentTrophyAtStart: number;
  startLeagueIndex: number;
  startedAt: number;        // unix giây
  expiresAt: number;        // unix giây
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
  trophy: number;
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

function newToken(): string {
  return randomBytes(16).toString("hex");
}

// ===== Vé (PvPTicketsDTO) =====

function freshTickets(dayKey: string): PvPTicketsDTO {
  return {
    freeRemaining: DAILY_FREE_TICKETS,
    adRemaining: 0,
    adClaimedToday: 0,
    maxAdPerDay: MAX_AD_TICKETS_PER_DAY,
    dailyFreeTickets: DAILY_FREE_TICKETS,
    dayKey,
  };
}

// Sang ngày UTC mới → hồi vé free, xoá lượt ads (vé ads chưa dùng cũng hết hạn).
function resetTicketsIfNewDay(player: PvPPlayerDoc, now: Date): void {
  const dayKey = getDayKey(now);
  if (player.tickets?.dayKey !== dayKey) {
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
    trophy: START_TROPHY,
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

// PvPPlayerModel nhìn từ góc người xem (Projected* = trophy NGƯỜI XEM sau thắng/thua đối thủ này — jkz/jla).
function toModel(p: PvPPlayerDoc, viewerTrophy: number): PvPPlayerModel {
  return {
    UserId: p.uid,
    PlayerName: p.playerName,
    CountryCode: p.countryCode,
    AvatarId: p.avatarId,
    Position: 0,
    Trophy: p.trophy,
    Power: p.power,
    SnapshotContentVersion: p.contentVersion,
    ProjectedWinTrophy: projectedWin(viewerTrophy, p.trophy),
    ProjectedLossTrophy: projectedLoss(viewerTrophy, p.trophy),
    Items: p.items ?? [],
    Companions: p.companions ?? [],
    ShowCloak: p.showCloak ?? true,
    AttacksUsed: p.attacksUsed ?? 0,
    EnchantmentTiers: p.enchantmentTiers ?? [],
    IsBot: p.isBot === true,
  };
}

// ===== initPvPProfile =====

export const initpvpprofile = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as SnapshotRequest;
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const player = snap.exists ? (snap.data() as PvPPlayerDoc) : newPlayerDoc(uid);
    applySnapshot(player, { playerName: data.playerName, countryCode: data.countryCode });
    player.updatedAt = Date.now();
    tx.set(ref, player);
    return {
      success: true, userId: uid, playerName: player.playerName, hasName: player.playerName.length > 0,
      created: !snap.exists, message: "",
    };
  });
});

// ===== openPvP =====

export const openpvp = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as SnapshotRequest;
  const now = new Date();
  const rewardTable = await loadRewardTable(db());
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const player = snap.exists ? (snap.data() as PvPPlayerDoc) : newPlayerDoc(uid);
    resetTicketsIfNewDay(player, now);
    applySnapshot(player, data);
    player.updatedAt = now.getTime();
    tx.set(ref, player);

    return {
      success: true,
      message: "",
      userId: uid,
      hasName: player.playerName.length > 0,
      playerName: player.playerName,
      trophy: player.trophy,
      leagueIndex: getLeagueIndex(player.trophy),
      contentVersion: CONTENT_VERSION,
      tickets: player.tickets,
      rewardTable: rewardTable as PvPLeagueRewardsDTO[],
    };
  });
});

// ===== findPvPOpponents =====

// Ghép đối thủ (server gốc giấu luật): lấy ~3×ROSTER_SIZE người gần trophy nhất (cả trên lẫn dưới),
// bỏ chính mình, chọn ngẫu nhiên ROSTER_SIZE trong 2×ROSTER_SIZE người gần nhất.
async function pickOpponents(uid: string, trophy: number): Promise<PvPPlayerDoc[]> {
  const col = db().collection(PLAYERS);
  const fetch = ROSTER_SIZE * 3;
  const [above, below] = await Promise.all([
    col.where("trophy", ">=", trophy).orderBy("trophy", "asc").limit(fetch).get(),
    col.where("trophy", "<", trophy).orderBy("trophy", "desc").limit(fetch).get(),
  ]);

  const candidates = [...above.docs, ...below.docs]
    .map((d) => d.data() as PvPPlayerDoc)
    .filter((p) => p.uid !== uid);
  candidates.sort((a, b) => Math.abs(a.trophy - trophy) - Math.abs(b.trophy - trophy));

  const nearest = candidates.slice(0, ROSTER_SIZE * 2);
  for (let i = nearest.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [nearest[i], nearest[j]] = [nearest[j], nearest[i]];
  }
  return nearest.slice(0, ROSTER_SIZE);
}

export const findpvpopponents = onCall(async (request) => {
  const uid = requireUid(request);
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);
  const snap = await ref.get();
  if (!snap.exists) return { success: false, message: "Failed to load PvP." };

  const player = snap.data() as PvPPlayerDoc;
  resetTicketsIfNewDay(player, now);
  const opponents = await pickOpponents(uid, player.trophy);

  const roster: Roster = {
    token: newToken(),
    expiresAt: nowSeconds() + ROSTER_TTL_SECONDS,
    opponentIds: opponents.map((o) => o.uid),
  };
  await ref.update({ roster, tickets: player.tickets });

  return {
    success: true,
    message: "",
    rosterToken: roster.token,
    rosterExpiresAt: roster.expiresAt,
    opponents: opponents.map((o) => toModel(o, player.trophy)),
    tickets: player.tickets,
  };
});

// ===== startPvPBattle =====

export const startpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { rosterToken?: string; opponentUserId?: string };
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to load PvP." };
    const player = snap.data() as PvPPlayerDoc;
    resetTicketsIfNewDay(player, now);

    const roster = player.roster;
    if (!roster || roster.token !== data.rosterToken || roster.expiresAt < nowSeconds()) {
      tx.set(ref, player);
      return { success: false, message: "Opponent roster expired.", tickets: player.tickets };
    }
    const opponentId = String(data.opponentUserId ?? "");
    if (!roster.opponentIds.includes(opponentId)) {
      return { success: false, message: "Opponent not found.", tickets: player.tickets };
    }
    const oppSnap = await tx.get(db().collection(PLAYERS).doc(opponentId));
    if (!oppSnap.exists) {
      return { success: false, message: "Opponent not found.", tickets: player.tickets };
    }

    // Tiêu vé: free trước, hết free mới dùng vé ads.
    const t = player.tickets;
    if (t.freeRemaining > 0) {
      t.freeRemaining--;
    } else if (t.adRemaining > 0) {
      t.adRemaining--;
    } else {
      tx.set(ref, player);
      return { success: false, message: "No PvP tickets remaining.", tickets: t };
    }

    const opponent = oppSnap.data() as PvPPlayerDoc;
    const startedAt = nowSeconds();
    const battle: ActiveBattle = {
      token: newToken(),
      opponentUserId: opponentId,
      myTrophyAtStart: player.trophy,
      opponentTrophyAtStart: opponent.trophy,
      startLeagueIndex: getLeagueIndex(player.trophy),
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
      opponent: toModel(opponent, player.trophy),
      tickets: t,
    };
  });
});

// ===== reportPvPBattle =====

export const reportpvpbattle = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { battleToken?: string; won?: boolean };
  const rewardTable = await loadRewardTable(db());
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to report battle." };
    const player = snap.data() as PvPPlayerDoc;
    const battle = player.activeBattle;
    if (!battle || !data.battleToken || battle.token !== data.battleToken) {
      return { success: false, message: "Missing battle token.", tickets: player.tickets };
    }

    const oppRef = db().collection(PLAYERS).doc(battle.opponentUserId);
    const oppSnap = await tx.get(oppRef);
    const opponent = oppSnap.exists ? (oppSnap.data() as PvPPlayerDoc) : null;

    // Nộp quá hạn → xử thua (forfeit).
    const forfeit = battle.expiresAt < nowSeconds();
    const won = !forfeit && data.won === true;

    const oldTrophy = player.trophy;
    const oppStart = battle.opponentTrophyAtStart;
    const delta = won ? winDelta(oldTrophy, oppStart) : lossDelta(oldTrophy, oppStart);
    player.trophy = Math.max(MINIMUM_TROPHY, oldTrophy + delta);
    player.attacksUsed = (player.attacksUsed ?? 0) + 1;
    if (won) player.wins = (player.wins ?? 0) + 1;
    else player.losses = (player.losses ?? 0) + 1;
    player.activeBattle = null;
    player.updatedAt = Date.now();

    // Bên phòng thủ đổi trophy ngược chiều (K theo league của họ).
    let opponentOldTrophy = oppStart;
    let opponentNewTrophy = oppStart;
    if (opponent) {
      opponentOldTrophy = opponent.trophy;
      const oppDelta = won ? lossDelta(opponent.trophy, oldTrophy) : winDelta(opponent.trophy, oldTrophy);
      opponentNewTrophy = Math.max(MINIMUM_TROPHY, opponent.trophy + oppDelta);
      tx.update(oppRef, { trophy: opponentNewTrophy, updatedAt: Date.now() });
    }
    tx.set(ref, player);

    return {
      success: true,
      message: "",
      won,
      forfeit,
      oldTrophy,
      newTrophy: player.trophy,
      trophyDelta: player.trophy - oldTrophy,
      oldLeagueIndex: getLeagueIndex(oldTrophy),
      newLeagueIndex: getLeagueIndex(player.trophy),
      startLeagueIndex: battle.startLeagueIndex,
      opponentOldTrophy,
      opponentNewTrophy,
      opponent: opponent ? toModel({ ...opponent, trophy: opponentNewTrophy }, player.trophy) : null,
      rewards: getLeagueRewards(rewardTable, battle.startLeagueIndex, won),
      tickets: player.tickets,
    };
  });
});

// ===== grantPvPAdTicket =====

export const grantpvpadticket = onCall(async (request) => {
  const uid = requireUid(request);
  const now = new Date();
  const ref = db().collection(PLAYERS).doc(uid);

  return db().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    if (!snap.exists) return { success: false, message: "Failed to grant ticket." };
    const player = snap.data() as PvPPlayerDoc;
    resetTicketsIfNewDay(player, now);
    const t = player.tickets;
    if (t.adClaimedToday >= t.maxAdPerDay) {
      tx.set(ref, player);
      return { success: false, message: "No more PvP ad tickets today.", tickets: t };
    }
    t.adClaimedToday++;
    t.adRemaining++;
    tx.set(ref, player);
    return { success: true, message: "", tickets: t };
  });
});

// ===== getPvPLeaderboard (scope "world" | "country") =====

interface LeaderboardEntry {
  UserId: string;
  PlayerName: string;
  CountryCode: string;
  AvatarId: number;
  Trophy: number;
  Rank: number;
  Power: number;
  IsCurrentPlayer: boolean;
  Items: BossRushItemModel[];
  Companions: BossRushCompanionModel[];
  ShowCloak: boolean;
  EnchantmentTiers: number[];
}

function toEntry(p: PvPPlayerDoc, rank: number, uid: string): LeaderboardEntry {
  return {
    UserId: p.uid, PlayerName: p.playerName, CountryCode: p.countryCode, AvatarId: p.avatarId, Trophy: p.trophy,
    Rank: rank, Power: p.power, IsCurrentPlayer: p.uid === uid, Items: p.items ?? [], Companions: p.companions ?? [],
    ShowCloak: p.showCloak ?? true, EnchantmentTiers: p.enchantmentTiers ?? [],
  };
}

export const getpvpleaderboard = onCall(async (request) => {
  const uid = requireUid(request);
  const scope = (request.data as { scope?: string })?.scope === "country" ? "country" : "world";
  const meSnap = await db().collection(PLAYERS).doc(uid).get();
  if (!meSnap.exists) return { success: false, message: "Failed to load leaderboard." };
  const me = meSnap.data() as PvPPlayerDoc;

  let base: Query = db().collection(PLAYERS);
  if (scope === "country") base = base.where("countryCode", "==", me.countryCode ?? "");

  const [topSnap, aboveCount] = await Promise.all([
    base.orderBy("trophy", "desc").limit(LEADERBOARD_TOP).get(),
    base.where("trophy", ">", me.trophy).count().get(),
  ]);
  const playerRank = aboveCount.data().count + 1;

  const topPlayers = topSnap.docs.map((d, i) => toEntry(d.data() as PvPPlayerDoc, i + 1, uid));
  const inTop = topPlayers.some((e) => e.IsCurrentPlayer);

  // Ngoài top → thêm vài người quanh mình (separator giữa 2 danh sách ở client).
  let nearbyPlayers: LeaderboardEntry[] = [];
  if (!inTop) {
    const [aboveSnap, belowSnap] = await Promise.all([
      base.where("trophy", ">", me.trophy).orderBy("trophy", "asc").limit(LEADERBOARD_NEARBY).get(),
      base.where("trophy", "<=", me.trophy).orderBy("trophy", "desc").limit(LEADERBOARD_NEARBY + 1).get(),
    ]);
    const above = aboveSnap.docs.map((d) => d.data() as PvPPlayerDoc).reverse();
    const below = belowSnap.docs.map((d) => d.data() as PvPPlayerDoc).filter((p) => p.uid !== uid)
      .slice(0, LEADERBOARD_NEARBY);
    nearbyPlayers = [
      ...above.map((p, i) => toEntry(p, playerRank - above.length + i, uid)),
      toEntry(me, playerRank, uid),
      ...below.map((p, i) => toEntry(p, playerRank + 1 + i, uid)),
    ];
  }

  return {
    success: true,
    message: "",
    scope,
    generatedAt: nowSeconds(),
    stale: false,
    playerRank,
    playerTrophy: me.trophy,
    topPlayers,
    nearbyPlayers,
  };
});

// ===== Admin (gốc: seedPvPDummyPlayers / removePvPDummyPlayers / removeAllPvPPlayers) =====

async function requireAdmin(uid: string | undefined): Promise<string> {
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  if (process.env.FUNCTIONS_EMULATOR === "true") return uid;
  const snap = await db().collection("config").doc("admins").get();
  const uids: string[] = (snap.data()?.uids as string[]) ?? [];
  if (!uids.includes(uid)) throw new HttpsError("permission-denied", "Admin only");
  return uid;
}

// Tạo bot quanh trophy người gọi (±150), mặc đồ người gọi, power dao động ±30%.
export const seedpvpdummyplayers = onCall(async (request) => {
  const uid = await requireAdmin(request.auth?.uid);
  const count = Math.max(1, Math.min(50, Number((request.data as { count?: number })?.count) || 10));
  const meSnap = await db().collection(PLAYERS).doc(uid).get();
  if (!meSnap.exists) return { success: false, message: "Open PvP first" };
  const me = meSnap.data() as PvPPlayerDoc;

  const batch = db().batch();
  for (let i = 0; i < count; i++) {
    const id = "bot_" + randomBytes(5).toString("hex");
    const bot: PvPPlayerDoc = {
      ...newPlayerDoc(id),
      playerName: "Player" + Math.floor(1000 + Math.random() * 9000),
      countryCode: me.countryCode,
      avatarId: Math.floor(Math.random() * 8),
      power: Math.round(me.power * (0.7 + Math.random() * 0.6)),
      items: me.items,
      companions: me.companions,
      enchantmentTiers: me.enchantmentTiers ?? [],
      showCloak: me.showCloak ?? true,
      trophy: Math.max(MINIMUM_TROPHY, me.trophy + Math.round((Math.random() * 2 - 1) * 150)),
      isBot: true,
    };
    batch.set(db().collection(PLAYERS).doc(id), bot);
  }
  await batch.commit();
  return { success: true, added: count };
});

async function deleteWhere(onlyBots: boolean): Promise<number> {
  let q: Query = db().collection(PLAYERS);
  if (onlyBots) q = q.where("isBot", "==", true);
  const snap = await q.get();
  let removed = 0;
  for (let i = 0; i < snap.docs.length; i += 400) {
    const batch = db().batch();
    snap.docs.slice(i, i + 400).forEach((d) => batch.delete(d.ref));
    await batch.commit();
    removed += Math.min(400, snap.docs.length - i);
  }
  return removed;
}

export const removepvpdummyplayers = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  return { success: true, removed: await deleteWhere(true) };
});

export const removeallpvpplayers = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  return { success: true, removed: await deleteWhere(false) };
});
