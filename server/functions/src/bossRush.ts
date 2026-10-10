import { randomBytes } from "crypto";
import { FieldPath, getFirestore, Firestore, Transaction, DocumentReference } from "firebase-admin/firestore";
import { CallableRequest, HttpsError, onCall } from "firebase-functions/v2/https";
import { onSchedule } from "firebase-functions/v2/scheduler";
import { botLoadout, loadBank } from "./bots";
import { RankEntry, botParams, isPoolOpen, poolBossState, poolCount, previousEventKey, rankPool, seasonBounds, toRows } from "./bossRushPool";
import { DAILY_AD_TICKETS, DAILY_FREE_TICKETS, FIGHT_TOKEN_TTL_SECONDS, MAX_GHOSTS, START_TIER } from "./config";
import {
  BossRushCompanionModel, BossRushItemModel, BossRushPlayerModel, BotSeed, PlayerDoc, PlayerSnapshotRequest, PoolDoc, PoolResult,
  TicketsDTO,
} from "./models";
import { computeNewTier, computeRewards, getRewardTable, loadRemoteConfig } from "./rewardConfig";
import { getDayKey, getEventKey, getNextEventKey, isEventActive } from "./schedule";
import {
  PLAYERS, POOLS, createPool, ensureCurrentSeason, entryOf, finalizePool, isPoolEnded, prepareSeason, readTierStat,
  recordFightStats, statsRef,
} from "./season";

// Endpoint Boss Rush — tên hàm viết thường như URL server gốc (https://{fn}-umgnfrxyuq-uc.a.run.app/): joinbossrush,
// getbossrushpool, startbossrushfight, reportbossrushdamage, claimbossrushrewards, updatebossrushplayer; thêm
// getbossrushprofile (bấm avatar xem hồ sơ). Luật chơi: server/BOSS_RUSH_DESIGN.md. Lỗi nghiệp vụ trả
// {success:false, message} như DTO gốc; chỉ lỗi xác thực mới ném HttpsError.

const AVG_WEIGHT = 0.3;

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
    showCloak: true,
    avatarId: 0,
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

export function sanitizeItems(items: BossRushItemModel[] | undefined): BossRushItemModel[] {
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

export function sanitizeCompanions(list: BossRushCompanionModel[] | undefined): BossRushCompanionModel[] {
  if (!Array.isArray(list)) return [];
  return list.slice(0, 64).map((c) => ({
    CompanionId: String(c.CompanionId ?? ""),
    CompanionLevel: Number(c.CompanionLevel) || 1,
    Equipped: c.Equipped === true,
  }));
}

// Tier relic (Enchantment) đang đeo theo slot: số nguyên 0..11 (gốc qm.xkk = 11), tối đa 16 slot.
export function sanitizeEnchantmentTiers(list: number[] | undefined): number[] {
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
  if (typeof data.showCloak === "boolean") player.showCloak = data.showCloak;
  if (typeof data.avatarId === "number" && isFinite(data.avatarId)) player.avatarId = Math.max(0, Math.floor(data.avatarId));
}

// Đưa tên/power/avatar mới nhất của người chơi vào dòng của họ trong sảnh (điểm và số trận giữ nguyên).
function syncEntry(pool: PoolDoc, player: PlayerDoc): void {
  const entry = pool.players[player.uid];
  entry.PlayerName = player.playerName;
  entry.AvatarId = player.avatarId ?? 0;
  entry.Power = player.power;
  entry.Joined = true;
}

// ===== Hồ sơ đầy đủ (bộ đồ) — chỉ gửi cho 7 người hỗ trợ lúc vào trận và khi bấm xem hồ sơ =====

function playerModel(p: PlayerDoc, e: RankEntry | undefined): BossRushPlayerModel {
  return {
    UserId: p.uid, PlayerName: p.playerName, AvatarId: p.avatarId ?? 0, Position: e?.position ?? 0, Power: p.power,
    TotalDamagePoints: e?.score ?? 0, Items: p.items ?? [], Companions: p.companions ?? [],
    EnchantmentTiers: p.enchantmentTiers ?? [], ShowCloak: p.showCloak ?? true,
  };
}

// Bot lấy bộ đồ từ ngân hàng. Ngân hàng chưa có (functions/data/botBank.json) → TẠM mượn bộ đồ của người gọi.
function botModel(bot: BotSeed, pool: PoolDoc, now: number, e: RankEntry | undefined, fallback: PlayerDoc): BossRushPlayerModel {
  const step = botLoadout(bot, botParams(pool), now, loadBank());
  return {
    UserId: bot.id, PlayerName: bot.name, AvatarId: bot.avatarId, Position: e?.position ?? 0, Power: e?.power ?? bot.basePower,
    TotalDamagePoints: e?.score ?? 0,
    Items: step ? step.items : fallback.items ?? [],
    Companions: step ? step.companions : fallback.companions ?? [],
    EnchantmentTiers: step ? step.enchantmentTiers : fallback.enchantmentTiers ?? [],
    ShowCloak: step ? step.showCloak : true,
  };
}

// Sảnh cũ còn thưởng chưa nhận (gốc: hasUnclaimed/unclaimedPoolId). Người không đánh trận nào thì không có thưởng.
async function findUnclaimedPoolId(tx: Transaction, player: PlayerDoc, now: Date): Promise<string> {
  if (!player.currentPoolId) return "";
  const snap = await tx.get(db().collection(POOLS).doc(player.currentPoolId));
  if (!snap.exists) return "";
  const pool = snap.data() as PoolDoc;
  const entry = pool.players?.[player.uid];
  if (isPoolEnded(pool, now) && !pool.claimed?.[player.uid] && entry && (entry.Fights ?? 0) > 0) {
    return pool.poolId;
  }
  return "";
}

function poolState(pool: PoolDoc, now: Date) {
  const boss = poolBossState(pool, now.getTime());
  return {
    poolId: pool.poolId,
    tier: pool.tier,
    eventKey: pool.eventKey,
    players: toRows(rankPool(pool, now.getTime(), loadBank())),
    bossNumber: boss.bossNumber,
    bossHP: boss.bossHP,
    maxBossHP: boss.maxBossHP,
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
  // Dự phòng khi tác vụ Thứ Hai chưa chạy: lượt Join đầu tiên của mùa sẽ chốt mùa cũ + dựng sảnh mùa mới.
  const seasonReady = await ensureCurrentSeason(db(), now);

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

    // Ngoài giờ (Thứ 2) hoặc còn thưởng mùa trước chưa nhận → chưa cho vào sảnh mới.
    if (!isEventActive(now) || unclaimedPoolId !== "") {
      player.updatedAt = now.getTime();
      tx.set(playerRef, player);
      return { ...base, inactive: !isEventActive(now), alreadyJoined: false, tier: player.tier };
    }
    if (!seasonReady) {
      player.updatedAt = now.getTime();
      tx.set(playerRef, player);
      return {
        ...base, success: false, preparing: true, message: "Results are being prepared", inactive: false,
        alreadyJoined: false, tier: player.tier,
      };
    }

    const joined = (pool: PoolDoc, poolRef: DocumentReference, alreadyJoined: boolean) => {
      syncEntry(pool, player);
      pool.realCount = Object.keys(pool.players).length;
      pool.isOpen = isPoolOpen(pool);
      player.currentPoolId = pool.poolId;
      player.lastJoinEventKey = eventKey;
      player.updatedAt = now.getTime();
      tx.set(poolRef, pool);
      tx.set(playerRef, player);
      return {
        ...base, ...poolState(pool, now), alreadyJoined, inactive: false,
        rewardTable: getRewardTable(config, pool.tier),
      };
    };

    // 1. Đã ở trong một sảnh của mùa này, hoặc được giữ chỗ từ đầu mùa → vào đúng sảnh đó.
    const knownPoolId = player.lastJoinEventKey === eventKey && player.currentPoolId ? player.currentPoolId
      : player.reservedEventKey === eventKey && player.reservedPoolId ? player.reservedPoolId : "";
    if (knownPoolId) {
      const poolRef = db().collection(POOLS).doc(knownPoolId);
      const poolSnap = await tx.get(poolRef);
      const pool = poolSnap.data() as PoolDoc | undefined;
      if (pool?.players?.[uid]) {
        return joined(pool, poolRef, player.lastJoinEventKey === eventKey);
      }
    }

    // 2. Sảnh cùng league còn chỗ → ưu tiên sảnh nhiều người thật nhất.
    const openSnap = await tx.get(db().collection(POOLS)
      .where("eventKey", "==", eventKey)
      .where("tier", "==", player.tier)
      .where("isOpen", "==", true)
      .orderBy("realCount", "desc")
      .limit(1));
    if (!openSnap.empty && isPoolOpen(openSnap.docs[0].data() as PoolDoc)) {
      const pool = openSnap.docs[0].data() as PoolDoc;
      pool.players[uid] = entryOf(player, now.getTime(), true);
      return joined(pool, openSnap.docs[0].ref, false);
    }

    // 3. Không còn sảnh nào → sảnh mới: 1 người + bot cho đủ 60.
    const stats = (await tx.get(statsRef(db(), eventKey))).data();
    const prevStats = (await tx.get(statsRef(db(), previousEventKey(eventKey)))).data();
    const stat = readTierStat(stats, player.tier) ?? readTierStat(prevStats, player.tier);
    const poolRef = db().collection(POOLS).doc();
    return joined(createPool(poolRef.id, eventKey, player.tier, [player], true, now.getTime(), stat), poolRef, false);
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
      ...poolState(pool, now),
      isFinalized: pool.isFinalized || ended,
      tickets: player.tickets,
      rewardTable: getRewardTable(config, pool.tier),
      unclaimedPoolId,
    };
  });
});

// ===== startBossRushFight =====

function pickRandom<T>(list: T[], count: number): T[] {
  const rest = [...list];
  const out: T[] = [];
  while (out.length < count && rest.length > 0) {
    out.push(rest.splice(Math.floor(Math.random() * rest.length), 1)[0]);
  }
  return out;
}

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
    if (player.tickets.freeRemaining <= 0 && player.tickets.adRemaining <= 0) return fail("No fights remaining.");

    // 7 người hỗ trợ lấy ngẫu nhiên từ sảnh (ekz(7) gốc) kèm bộ đồ: người thật đọc từ hồ sơ của họ, bot từ ngân hàng.
    const ranked = rankPool(pool, now.getTime(), loadBank());
    const picks = pickRandom(ranked.filter((e) => e.id !== uid), MAX_GHOSTS);
    const realPicks = picks.filter((e) => !e.isBot);
    const realSnaps = realPicks.length > 0
      ? await tx.getAll(...realPicks.map((e) => db().collection(PLAYERS).doc(e.id)))
      : [];
    const realDocs = new Map(realSnaps.filter((s) => s.exists).map((s) => [s.id, s.data() as PlayerDoc]));
    const bots = new Map((pool.bots ?? []).map((b) => [b.id, b]));
    const allies: BossRushPlayerModel[] = [];
    for (const e of picks) {
      const bot = bots.get(e.id);
      const real = realDocs.get(e.id);
      if (bot) allies.push(botModel(bot, pool, now.getTime(), e, player));
      else if (real) allies.push(playerModel(real, e));
    }

    // Tiêu vé free trước, hết free mới dùng vé ads (client đã cho xem ads trước khi gọi).
    if (player.tickets.freeRemaining > 0) {
      player.tickets.freeRemaining--;
    } else {
      player.tickets.adRemaining--;
      player.tickets.adClaimedToday++;
    }

    const boss = poolBossState(pool, now.getTime());
    const expiresAt = now.getTime() + FIGHT_TOKEN_TTL_SECONDS * 1000;
    player.activeFight = {
      token: randomBytes(16).toString("hex"),
      poolId,
      bossNumber: boss.bossNumber,
      expiresAt,
    };
    player.updatedAt = now.getTime();
    tx.set(playerRef, player);

    return {
      success: true,
      message: "",
      fightToken: player.activeFight.token,
      bossNumber: boss.bossNumber,
      bossHP: boss.bossHP,
      maxBossHP: boss.maxBossHP,
      fightExpiresAt: Math.floor(expiresAt / 1000),
      allies,
      tickets: player.tickets,
    };
  });
});

// ===== reportBossRushDamage =====

function toDamage(v: unknown): number {
  const n = Math.floor(Number(v));
  return isFinite(n) && n > 0 ? Math.min(n, Number.MAX_SAFE_INTEGER) : 0;
}

function average(prev: number | undefined, value: number): number {
  return prev && prev > 0 ? prev * (1 - AVG_WEIGHT) + value * AVG_WEIGHT : value;
}

export const reportbossrushdamage = onCall(async (request) => {
  const uid = requireUid(request);
  const body = (request.data ?? {}) as { fightToken?: string; damageDealt?: number; totalDamageDealt?: number };
  const fightToken = String(body.fightToken ?? "");
  const damageDealt = toDamage(body.damageDealt);
  const totalDamageDealt = Math.max(toDamage(body.totalDamageDealt), damageDealt);
  const now = new Date();
  let counted: { eventKey: string; tier: number } | null = null;

  const result = await db().runTransaction(async (tx) => {
    counted = null;
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

    // Quá hạn nộp / sảnh mất / mùa đã kết thúc → không tính (autoLoss như gốc).
    const pool = poolSnap.exists ? (poolSnap.data() as PoolDoc) : null;
    const me = pool?.players?.[uid];
    if (!pool || !me || now.getTime() > fight.expiresAt || isPoolEnded(pool, now)) {
      tx.set(playerRef, player);
      return { success: true, autoLoss: true, damagePoints: 0, tickets: player.tickets };
    }

    // Bảng xếp hạng chỉ cộng damage của CHÍNH người vào đánh; máu boss chung trừ theo damage CẢ ĐỘI (8 người).
    me.TotalDamagePoints += damageDealt;
    me.Fights = (me.Fights ?? 0) + 1;
    pool.realTeamDamage = (pool.realTeamDamage ?? 0) + totalDamageDealt;

    if (damageDealt > 0) {
      // Sảnh chưa có mốc cho bot → lấy trận thật đầu tiên làm mốc, bot bắt đầu hoạt động từ lúc này.
      if (pool.anchorOwn <= 0) {
        pool.anchorOwn = damageDealt;
        pool.anchorTeamRatio = Math.min(Math.max(totalDamageDealt / damageDealt, 1), 12);
        pool.botStartAt = now.getTime();
      }
      player.avgOwn = average(player.avgOwn, damageDealt);
      player.avgTeam = average(player.avgTeam, totalDamageDealt);
    }
    player.lastFoughtEventKey = pool.eventKey;
    counted = { eventKey: pool.eventKey, tier: pool.tier };

    tx.set(poolRef, pool);
    tx.set(playerRef, player);

    const boss = poolBossState(pool, now.getTime());
    const players = toRows(rankPool(pool, now.getTime(), loadBank()));
    return {
      success: true,
      autoLoss: false,
      damagePoints: damageDealt,
      totalDamagePoints: me.TotalDamagePoints,
      newPosition: players.find((p) => p.UserId === uid)?.Position ?? 0,
      bossKilled: boss.bossNumber > fight.bossNumber,
      newBossNumber: boss.bossNumber,
      newBossHP: boss.bossHP,
      newMaxBossHP: boss.maxBossHP,
      players,
      rewards: [],
      tickets: player.tickets,
    };
  });

  // Số liệu damage trung bình của league — mốc cho bot ở các sảnh tạo sau. Lỗi ở đây không ảnh hưởng kết quả trận.
  const stat = counted as { eventKey: string; tier: number } | null;
  if (stat) {
    try {
      await recordFightStats(db(), stat.eventKey, stat.tier, damageDealt, totalDamageDealt);
    } catch {
      // bỏ qua
    }
  }
  return result;
});

// ===== claimBossRushRewards =====

export const claimbossrushrewards = onCall(async (request) => {
  const uid = requireUid(request);
  const poolId = String((request.data as { poolId?: string })?.poolId ?? "");
  const now = new Date();
  const config = await loadRemoteConfig(db());
  const poolRef = db().collection(POOLS).doc(poolId || "_");

  // Chốt sảnh ngay tại đây nếu tác vụ Thứ Hai chưa kịp chạy — nhận thưởng không phụ thuộc lịch.
  const pre = await poolRef.get();
  if (pre.exists && isPoolEnded(pre.data() as PoolDoc, now) && !(pre.data() as PoolDoc).isFinalized) {
    await finalizePool(db(), poolRef, config);
  }

  return db().runTransaction(async (tx) => {
    const playerRef = db().collection(PLAYERS).doc(uid);
    const player = await readPlayer(tx, playerRef, uid, now);
    resetTicketsIfNewDay(player, now);
    const poolSnap = await tx.get(poolRef);

    if (!poolSnap.exists || !(poolSnap.data() as PoolDoc).players?.[uid]) {
      return { success: false, expired: true, tickets: player.tickets };
    }
    const pool = poolSnap.data() as PoolDoc;
    if (!isPoolEnded(pool, now)) {
      return { success: false, expired: false, tickets: player.tickets };
    }

    const end = seasonBounds(pool.eventKey).end;
    const tier = pool.tier;
    let result: PoolResult | undefined = pool.results?.[uid];
    if (!result) {
      const e = rankPool(pool, end, loadBank()).find((x) => x.id === uid) as RankEntry;
      result = { rank: e.position, fights: e.fights, newTier: computeNewTier(config, tier, e.position, poolCount(pool), e.fights, e.score) };
    }
    const bossesKilled = pool.finalBossesKilled ?? poolBossState(pool, end).bossesKilled;
    const alreadyClaimed = pool.claimed?.[uid] === true;
    // Chỉ người đã đánh ít nhất 1 trận trong mùa mới có thưởng.
    const rewards = alreadyClaimed || result.fights <= 0 ? [] : computeRewards(getRewardTable(config, tier), result.rank, bossesKilled);

    if (!alreadyClaimed) {
      if (player.currentPoolId === poolId) player.currentPoolId = "";
      player.updatedAt = now.getTime();
      tx.update(poolRef, new FieldPath("claimed", uid), true);
      tx.set(playerRef, player);
    }

    return {
      success: true,
      expired: false,
      rank: result.rank,
      promoted: result.newTier > tier,
      demoted: result.newTier < tier,
      bossesKilled,
      tier,
      newTier: result.newTier,
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
        syncEntry(pool, player);
        tx.set(poolRef, pool);
      }
    }
    return { success: true };
  });
});

// ===== getBossRushProfile — bấm avatar trong sảnh để xem hồ sơ (bộ đồ, pet, relic) =====

export const getbossrushprofile = onCall(async (request) => {
  const uid = requireUid(request);
  const body = (request.data ?? {}) as { poolId?: string; userId?: string };
  const targetId = String(body.userId ?? "");
  const now = new Date();

  const poolSnap = await db().collection(POOLS).doc(String(body.poolId ?? "") || "_").get();
  const pool = poolSnap.data() as PoolDoc | undefined;
  if (!pool?.players?.[uid]) return { success: false, message: "Pool not found" };

  const entry = rankPool(pool, now.getTime(), loadBank()).find((e) => e.id === targetId);
  if (!entry) return { success: false, message: "Player not found" };

  const readDoc = async (id: string) => (await db().collection(PLAYERS).doc(id).get()).data() as PlayerDoc | undefined;
  const bot = (pool.bots ?? []).find((b) => b.id === targetId);
  const source = await readDoc(bot ? uid : targetId);
  if (!source) return { success: false, message: "Player not found" };
  return { success: true, player: bot ? botModel(bot, pool, now.getTime(), entry, source) : playerModel(source, entry) };
});

// ===== Chốt mùa + dựng sảnh mùa mới (gốc finalizeBossRushEndedEvents) — 00:10 UTC Thứ 2 =====

export const finalizebossrushendedevents = onSchedule(
  { schedule: "10 0 * * 1", timeZone: "Etc/UTC" },
  async () => {
    const now = new Date();
    await prepareSeason(db(), getNextEventKey(now), now);
  },
);
