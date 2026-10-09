import { randomBytes } from "crypto";
import { getFirestore, Firestore, Transaction, DocumentReference } from "firebase-admin/firestore";
import { CallableRequest, HttpsError, onCall } from "firebase-functions/v2/https";
import { onSchedule } from "firebase-functions/v2/scheduler";
import {
  DAILY_AD_TICKETS, DAILY_FREE_TICKETS, FIGHT_TOKEN_TTL_SECONDS, POOL_SIZE, START_TIER, getBossHp,
} from "./config";
import {
  BossRushCompanionModel, BossRushItemModel, BossRushPlayerModel, PlayerDoc, PlayerSnapshotRequest, PoolDoc, TicketsDTO,
} from "./models";
import { computeNewTier, computeRewards, getRewardTable, loadRemoteConfig } from "./rewardConfig";
import { getDayKey, getEventKey, getNextEventKey, isEventActive } from "./schedule";

// 6 endpoint giống server gốc (Cloud Functions v2, tên hàm viết thường như URL gốc
// https://{fn}-umgnfrxyuq-uc.a.run.app/): joinbossrush, getbossrushpool, startbossrushfight,
// reportbossrushdamage, claimbossrushrewards, updatebossrushplayer. Lỗi nghiệp vụ trả {success:false, message}
// như DTO gốc; chỉ lỗi xác thực mới ném HttpsError.

const PLAYERS = "bossRushPlayers";
const POOLS = "bossRushPools";
const MAX_BOSS_LOOP = 1000;

function db(): Firestore {
  return getFirestore();
}

function requireUid(request: CallableRequest<unknown>): string {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required");
  }
  return uid;
}

// ===== Vé =====

function freshTickets(dayKey: string): TicketsDTO {
  return {
    freeRemaining: DAILY_FREE_TICKETS,
    adRemaining: DAILY_AD_TICKETS,
    adClaimedToday: 0,
    dailyFreeTickets: DAILY_FREE_TICKETS,
    dayKey,
  };
}

// Sang ngày UTC mới → hồi vé (SET lại, không cộng dồn).
function resetTicketsIfNewDay(player: PlayerDoc, now: Date): boolean {
  const dayKey = getDayKey(now);
  if (player.tickets?.dayKey === dayKey) {
    return false;
  }
  player.tickets = freshTickets(dayKey);
  return true;
}

function newPlayerDoc(uid: string, now: Date): PlayerDoc {
  return {
    uid,
    playerName: "",
    power: 0,
    items: [],
    companions: [],
    enchantmentTiers: [],
    tier: START_TIER,
    currentPoolId: "",
    lastJoinEventKey: "",
    tickets: freshTickets(getDayKey(now)),
    activeFight: null,
    updatedAt: now.getTime(),
  };
}

async function readPlayer(tx: Transaction, ref: DocumentReference, uid: string, now: Date): Promise<PlayerDoc> {
  const snap = await tx.get(ref);
  return snap.exists ? (snap.data() as PlayerDoc) : newPlayerDoc(uid, now);
}

// ===== Snapshot đồ =====

function sanitizeItems(items: BossRushItemModel[] | undefined): BossRushItemModel[] {
  if (!Array.isArray(items)) return [];
  return items.slice(0, 16).map((it) => ({
    Slot: Number(it.Slot) || 0,
    ItemId: String(it.ItemId ?? ""),
    Rarity: Number(it.Rarity) || 0,
    ItemLevel: Number(it.ItemLevel) || 0,
    SubStats: Array.isArray(it.SubStats)
      ? it.SubStats.slice(0, 8).map((s) => ({ Type: Number(s.Type) || 0, Value: Number(s.Value) || 0 }))
      : [],
  }));
}

function sanitizeCompanions(list: BossRushCompanionModel[] | undefined): BossRushCompanionModel[] {
  if (!Array.isArray(list)) return [];
  return list.slice(0, 64).map((c) => ({
    CompanionId: String(c.CompanionId ?? ""),
    CompanionLevel: Number(c.CompanionLevel) || 1,
    Equipped: c.Equipped === true,
  }));
}

// Tier relic (Enchantment) đang đeo theo slot: số nguyên 0..11 (gốc qm.xkk = 11), tối đa 16 slot.
function sanitizeEnchantmentTiers(list: number[] | undefined): number[] {
  if (!Array.isArray(list)) return [];
  return list.slice(0, 16).map((t) => Math.min(11, Math.max(0, Math.floor(Number(t) || 0))));
}

function applySnapshot(player: PlayerDoc, data: PlayerSnapshotRequest): void {
  if (typeof data.playerName === "string" && data.playerName.length > 0) {
    player.playerName = data.playerName.substring(0, 24);
  }
  if (typeof data.power === "number" && isFinite(data.power) && data.power >= 0) {
    player.power = data.power;
  }
  if (data.items !== undefined) player.items = sanitizeItems(data.items);
  if (data.companions !== undefined) player.companions = sanitizeCompanions(data.companions);
  if (data.enchantmentTiers !== undefined) player.enchantmentTiers = sanitizeEnchantmentTiers(data.enchantmentTiers);
}

function toPoolPlayer(player: PlayerDoc, prev: BossRushPlayerModel | undefined, now: Date): BossRushPlayerModel {
  return {
    UserId: player.uid,
    PlayerName: player.playerName,
    Position: 0,
    Power: player.power,
    TotalDamagePoints: prev?.TotalDamagePoints ?? 0,
    Items: player.items,
    Companions: player.companions,
    EnchantmentTiers: player.enchantmentTiers ?? [],
    IsBot: false,
    JoinedAt: prev?.JoinedAt ?? now.getTime(),
  };
}

// ===== Xếp hạng =====

// Danh sách người chơi theo TotalDamagePoints giảm dần (hoà → vào trước đứng trên), Position 1-based.
function sortedPlayers(pool: PoolDoc): BossRushPlayerModel[] {
  const list = Object.values(pool.players ?? {}).map((p) => ({ ...p }));
  list.sort((a, b) => (b.TotalDamagePoints - a.TotalDamagePoints) || (a.JoinedAt - b.JoinedAt));
  list.forEach((p, i) => (p.Position = i + 1));
  return list;
}

function rankOf(pool: PoolDoc, uid: string): number {
  const p = sortedPlayers(pool).find((x) => x.UserId === uid);
  return p ? p.Position : 0;
}

// Pool thuộc đợt đã kết thúc (đang Thứ 2 hoặc đã sang đợt mới) → chờ nhận thưởng.
function isPoolEnded(pool: PoolDoc, now: Date): boolean {
  return pool.eventKey !== getEventKey(now);
}

// Pool cũ chưa nhận thưởng của người chơi (gốc: hasUnclaimed/unclaimedPoolId).
async function findUnclaimedPoolId(tx: Transaction, player: PlayerDoc, now: Date): Promise<string> {
  if (!player.currentPoolId) return "";
  const snap = await tx.get(db().collection(POOLS).doc(player.currentPoolId));
  if (!snap.exists) return "";
  const pool = snap.data() as PoolDoc;
  if (isPoolEnded(pool, now) && !pool.claimed?.[player.uid] && pool.players?.[player.uid]) {
    return pool.poolId;
  }
  return "";
}

function poolState(pool: PoolDoc) {
  return {
    poolId: pool.poolId,
    tier: pool.tier,
    eventKey: pool.eventKey,
    players: sortedPlayers(pool),
    bossNumber: pool.bossNumber,
    bossHP: pool.bossHP,
    maxBossHP: pool.maxBossHP,
    isFinalized: pool.isFinalized,
  };
}

// ===== joinBossRush =====

export const joinbossrush = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as PlayerSnapshotRequest;
  const now = new Date();
  const eventKey = getEventKey(now);
  const config = await loadRemoteConfig(db());

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    resetTicketsIfNewDay(player, now);
    applySnapshot(player, data);
    const unclaimedPoolId = await findUnclaimedPoolId(tx, player, now);

    const base = {
      success: true,
      nextEventKey: getNextEventKey(now),
      tickets: player.tickets,
      hasUnclaimed: unclaimedPoolId !== "",
      unclaimedPoolId,
    };

    // Ngoài giờ (Thứ 2) hoặc còn thưởng đợt trước chưa nhận → chưa cho vào nhóm mới.
    if (!isEventActive(now) || unclaimedPoolId !== "") {
      player.updatedAt = now.getTime();
      tx.set(playerRef, player);
      return { ...base, inactive: !isEventActive(now), alreadyJoined: false, tier: player.tier };
    }

    // Đã vào nhóm của đợt này → cập nhật snapshot, trả nhóm cũ.
    if (player.lastJoinEventKey === eventKey && player.currentPoolId) {
      const poolRef = db().collection(POOLS).doc(player.currentPoolId);
      const poolSnap = await tx.get(poolRef);
      if (poolSnap.exists) {
        const pool = poolSnap.data() as PoolDoc;
        pool.players[uid] = toPoolPlayer(player, pool.players[uid], now);
        player.updatedAt = now.getTime();
        tx.set(poolRef, pool);
        tx.set(playerRef, player);
        return {
          ...base, ...poolState(pool), alreadyJoined: true, inactive: false,
          rewardTable: getRewardTable(config, pool.tier),
        };
      }
    }

    // Ghép nhóm: nhóm còn chỗ cùng đợt + cùng tier; không có → tạo nhóm mới (boss #1).
    const openQuery = db().collection(POOLS)
      .where("eventKey", "==", eventKey)
      .where("tier", "==", player.tier)
      .where("isOpen", "==", true)
      .limit(1);
    const openSnap = await tx.get(openQuery);

    let pool: PoolDoc;
    let poolRef: DocumentReference;
    if (!openSnap.empty) {
      poolRef = openSnap.docs[0].ref;
      pool = openSnap.docs[0].data() as PoolDoc;
    } else {
      poolRef = db().collection(POOLS).doc();
      const hp = getBossHp(1, player.tier);
      pool = {
        poolId: poolRef.id,
        eventKey,
        tier: player.tier,
        isOpen: true,
        playerCount: 0,
        players: {},
        bossNumber: 1,
        bossHP: hp,
        maxBossHP: hp,
        bossesKilled: 0,
        isFinalized: false,
        claimed: {},
        createdAt: now.getTime(),
      };
    }

    pool.players[uid] = toPoolPlayer(player, undefined, now);
    pool.playerCount = Object.keys(pool.players).length;
    pool.isOpen = pool.playerCount < POOL_SIZE;

    player.currentPoolId = pool.poolId;
    player.lastJoinEventKey = eventKey;
    player.updatedAt = now.getTime();

    tx.set(poolRef, pool);
    tx.set(playerRef, player);
    return {
      ...base, ...poolState(pool), alreadyJoined: false, inactive: false,
      rewardTable: getRewardTable(config, pool.tier),
    };
  });
});

// ===== getBossRushPool =====

export const getbossrushpool = onCall(async (request) => {
  const uid = requireUid(request);
  const poolId = String((request.data as { poolId?: string })?.poolId ?? "");
  const now = new Date();
  const config = await loadRemoteConfig(db());

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    const ticketsChanged = resetTicketsIfNewDay(player, now);
    const unclaimedPoolId = await findUnclaimedPoolId(tx, player, now);

    const poolSnap = poolId ? await tx.get(db().collection(POOLS).doc(poolId)) : null;
    if (ticketsChanged) tx.set(playerRef, player);

    if (!poolSnap || !poolSnap.exists || !(poolSnap.data() as PoolDoc).players?.[uid]) {
      return {
        success: false, removed: true, inactive: !isEventActive(now), message: "Pool not found",
        nextEventKey: getNextEventKey(now), tickets: player.tickets, unclaimedPoolId,
      };
    }

    const pool = poolSnap.data() as PoolDoc;
    const ended = isPoolEnded(pool, now);
    return {
      success: true,
      removed: false,
      inactive: !isEventActive(now),
      concluding: ended,
      autoLoss: false,
      nextEventKey: getNextEventKey(now),
      message: "",
      ...poolState(pool),
      isFinalized: pool.isFinalized || ended,
      tickets: player.tickets,
      rewardTable: getRewardTable(config, pool.tier),
      unclaimedPoolId,
    };
  });
});

// ===== startBossRushFight =====

export const startbossrushfight = onCall(async (request) => {
  const uid = requireUid(request);
  const poolId = String((request.data as { poolId?: string })?.poolId ?? "");
  const now = new Date();

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    resetTicketsIfNewDay(player, now);

    const poolRef = db().collection(POOLS).doc(poolId || "_");
    const poolSnap = await tx.get(poolRef);
    const fail = (message: string) => {
      tx.set(playerRef, player);
      return { success: false, message, tickets: player.tickets };
    };

    if (!poolSnap.exists || !(poolSnap.data() as PoolDoc).players?.[uid]) return fail("Pool not found");
    const pool = poolSnap.data() as PoolDoc;
    if (!isEventActive(now) || isPoolEnded(pool, now)) return fail("Event ended");

    // Tiêu vé free trước, hết free mới dùng vé ads (client đã cho xem ads trước khi gọi).
    if (player.tickets.freeRemaining > 0) {
      player.tickets.freeRemaining--;
    } else if (player.tickets.adRemaining > 0) {
      player.tickets.adRemaining--;
      player.tickets.adClaimedToday++;
    } else {
      return fail("No fights remaining.");
    }

    const expiresAt = now.getTime() + FIGHT_TOKEN_TTL_SECONDS * 1000;
    player.activeFight = {
      token: randomBytes(16).toString("hex"),
      poolId,
      bossNumber: pool.bossNumber,
      expiresAt,
    };
    player.updatedAt = now.getTime();
    tx.set(playerRef, player);

    return {
      success: true,
      message: "",
      fightToken: player.activeFight.token,
      bossNumber: pool.bossNumber,
      bossHP: pool.bossHP,
      maxBossHP: pool.maxBossHP,
      fightExpiresAt: Math.floor(expiresAt / 1000),
      tickets: player.tickets,
    };
  });
});

// ===== reportBossRushDamage =====

function toDamage(v: unknown): number {
  const n = Math.floor(Number(v));
  return isFinite(n) && n > 0 ? Math.min(n, Number.MAX_SAFE_INTEGER) : 0;
}

export const reportbossrushdamage = onCall(async (request) => {
  const uid = requireUid(request);
  const body = (request.data ?? {}) as { fightToken?: string; damageDealt?: number; totalDamageDealt?: number };
  const fightToken = String(body.fightToken ?? "");
  const damageDealt = toDamage(body.damageDealt);
  const totalDamageDealt = Math.max(toDamage(body.totalDamageDealt), damageDealt);
  const now = new Date();

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    resetTicketsIfNewDay(player, now);
    const fight = player.activeFight;

    if (!fight || !fightToken || fight.token !== fightToken) {
      tx.set(playerRef, player);
      return { success: false, autoLoss: false, message: "Invalid fight token", tickets: player.tickets };
    }

    const poolRef = db().collection(POOLS).doc(fight.poolId);
    const poolSnap = await tx.get(poolRef);
    player.activeFight = null;
    player.updatedAt = now.getTime();

    // Quá hạn nộp / pool mất / đợt đã kết thúc → không tính (autoLoss như gốc).
    const pool = poolSnap.exists ? (poolSnap.data() as PoolDoc) : null;
    if (!pool || !pool.players?.[uid] || now.getTime() > fight.expiresAt || isPoolEnded(pool, now)) {
      tx.set(playerRef, player);
      return { success: true, autoLoss: true, damagePoints: 0, tickets: player.tickets };
    }

    // Điểm xếp hạng = damage của riêng hero; máu boss chung bị trừ bằng damage CẢ ĐỘI (khớp trận local).
    const me = pool.players[uid];
    me.TotalDamagePoints += damageDealt;

    let remaining = totalDamageDealt;
    let bossKilled = false;
    for (let i = 0; i < MAX_BOSS_LOOP && remaining >= pool.bossHP; i++) {
      remaining -= pool.bossHP;
      pool.bossesKilled++;
      pool.bossNumber++;
      pool.maxBossHP = getBossHp(pool.bossNumber, pool.tier);
      pool.bossHP = pool.maxBossHP;
      bossKilled = true;
    }
    pool.bossHP = Math.max(0, pool.bossHP - remaining);

    tx.set(poolRef, pool);
    tx.set(playerRef, player);

    const players = sortedPlayers(pool);
    return {
      success: true,
      autoLoss: false,
      damagePoints: damageDealt,
      totalDamagePoints: me.TotalDamagePoints,
      newPosition: players.find((p) => p.UserId === uid)?.Position ?? 0,
      bossKilled,
      newBossNumber: pool.bossNumber,
      newBossHP: pool.bossHP,
      newMaxBossHP: pool.maxBossHP,
      players,
      rewards: [],
      tickets: player.tickets,
    };
  });
});

// ===== claimBossRushRewards =====

export const claimbossrushrewards = onCall(async (request) => {
  const uid = requireUid(request);
  const poolId = String((request.data as { poolId?: string })?.poolId ?? "");
  const now = new Date();
  const config = await loadRemoteConfig(db());

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    resetTicketsIfNewDay(player, now);
    const poolRef = db().collection(POOLS).doc(poolId || "_");
    const poolSnap = await tx.get(poolRef);

    if (!poolSnap.exists || !(poolSnap.data() as PoolDoc).players?.[uid]) {
      return { success: false, expired: true, tickets: player.tickets };
    }
    const pool = poolSnap.data() as PoolDoc;
    if (!isPoolEnded(pool, now)) {
      return { success: false, expired: false, tickets: player.tickets };
    }

    const rank = rankOf(pool, uid);
    const tier = pool.tier;
    const alreadyClaimed = pool.claimed?.[uid] === true;
    const rewards = alreadyClaimed ? [] : computeRewards(getRewardTable(config, tier), rank, pool.bossesKilled);
    const newTier = alreadyClaimed ? player.tier : computeNewTier(config, tier, rank, pool.playerCount);

    if (!alreadyClaimed) {
      pool.claimed = { ...(pool.claimed ?? {}), [uid]: true };
      pool.isFinalized = true;
      player.tier = newTier;
      if (player.currentPoolId === poolId) player.currentPoolId = "";
      player.updatedAt = now.getTime();
      tx.set(poolRef, pool);
      tx.set(playerRef, player);
    }

    return {
      success: true,
      expired: false,
      rank,
      promoted: newTier > tier,
      demoted: newTier < tier,
      bossesKilled: pool.bossesKilled,
      tier,
      newTier,
      rewards,
      tickets: player.tickets,
    };
  });
});

// ===== updateBossRushPlayer =====

export const updatebossrushplayer = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as PlayerSnapshotRequest & { poolId?: string };
  const now = new Date();

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    const poolId = String(data.poolId ?? player.currentPoolId ?? "");
    const poolRef = poolId ? db().collection(POOLS).doc(poolId) : null;
    const poolSnap = poolRef ? await tx.get(poolRef) : null;

    applySnapshot(player, data);
    player.updatedAt = now.getTime();
    tx.set(playerRef, player);

    if (poolRef && poolSnap?.exists) {
      const pool = poolSnap.data() as PoolDoc;
      if (pool.players?.[uid] && !isPoolEnded(pool, now)) {
        pool.players[uid] = toPoolPlayer(player, pool.players[uid], now);
        tx.set(poolRef, pool);
      }
    }
    return { success: true };
  });
});

// ===== Chốt đợt (gốc finalizeBossRushEndedEvents) — 00:10 UTC Thứ 2 =====

export async function finalizeEndedPools(now: Date): Promise<number> {
  const currentKey = getEventKey(now);
  const snap = await db().collection(POOLS).where("isFinalized", "==", false).limit(500).get();
  const batch = db().batch();
  let count = 0;
  for (const doc of snap.docs) {
    const pool = doc.data() as PoolDoc;
    if (pool.eventKey !== currentKey) {
      batch.update(doc.ref, { isFinalized: true, isOpen: false });
      count++;
    }
  }
  if (count > 0) await batch.commit();
  return count;
}

export const finalizebossrushendedevents = onSchedule(
  { schedule: "10 0 * * 1", timeZone: "Etc/UTC" },
  async () => {
    await finalizeEndedPools(new Date());
  },
);
