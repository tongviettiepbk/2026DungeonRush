import { DocumentReference, FieldValue, Firestore, WriteBatch } from "firebase-admin/firestore";
import { BOT_ALGO_VERSION, generateBots, hashString, loadBank, makeRng } from "./bots";
import {
  distribute, isPoolOpen, median, poolBossState, poolCount, previousEventKey, rankPool, seasonBounds,
} from "./bossRushPool";
import { MAX_TIER, POOL_RESERVE_MAX, POOL_VISIBLE_TARGET, START_TIER } from "./config";
import { PlayerDoc, PoolDoc, PoolEntry, PoolResult } from "./models";
import { BossRushRemoteConfig, computeNewTier, loadRemoteConfig } from "./rewardConfig";
import { getEventKey } from "./schedule";

// VÒNG ĐỜI MÙA (server/BOSS_RUSH_DESIGN.md mục 4.2): chốt các sảnh đã hết mùa (hạng cuối + league mới), rồi dựng
// sảnh mùa mới — giữ chỗ cho người còn hoạt động, thiếu 60 người thì bù bot. Chạy bằng tác vụ hẹn giờ Thứ Hai;
// dự phòng: lượt Join đầu tiên của mùa (có khoá ở document bossRushSeasons/{eventKey}).

export const PLAYERS = "bossRushPlayers";
export const POOLS = "bossRushPools";
const SEASONS = "bossRushSeasons";
const STATS = "bossRushStats";

const LOCK_MS = 2 * 60 * 1000;
const BATCH_LIMIT = 400;
const MAX_ACTIVE_PER_TIER = 5000;
const DEFAULT_TEAM_RATIO = 8;

export function isPoolEnded(pool: PoolDoc, now: Date): boolean {
  return now.getTime() > seasonBounds(pool.eventKey).end;
}

// ===== Số liệu damage trung bình theo league (mốc cho bot khi sảnh chưa có ai từng đánh) =====

export interface TierStat {
  own: number;
  team: number;
  n: number;
}

export function readTierStat(data: FirebaseFirestore.DocumentData | undefined, tier: number): TierStat | null {
  const s = data?.["t" + tier] as TierStat | undefined;
  return s && s.n > 0 && s.own > 0 ? s : null;
}

export function statsRef(db: Firestore, eventKey: string): DocumentReference {
  return db.collection(STATS).doc(eventKey);
}

export async function recordFightStats(db: Firestore, eventKey: string, tier: number, own: number, team: number): Promise<void> {
  if (own <= 0) return;
  await statsRef(db, eventKey).set({
    ["t" + tier]: { own: FieldValue.increment(own), team: FieldValue.increment(team), n: FieldValue.increment(1) },
  }, { merge: true });
}

// ===== Tạo sảnh =====

export function entryOf(player: PlayerDoc, joinedAt: number, joined: boolean): PoolEntry {
  return {
    UserId: player.uid,
    PlayerName: player.playerName,
    AvatarId: player.avatarId ?? 0,
    Power: player.power,
    TotalDamagePoints: 0,
    Fights: 0,
    JoinedAt: joinedAt,
    Joined: joined,
  };
}

// Sảnh mới: members là người thật có mặt lúc tạo; số bot = 60 − số người thật (tối thiểu 0).
// Mốc của bot lấy theo thứ tự: damage trung bình của chính members → số liệu chung của league → chưa có (bot chờ
// tới trận thật đầu tiên của sảnh mới bắt đầu hoạt động).
export function createPool(
  poolId: string, eventKey: string, tier: number, members: PlayerDoc[], joined: boolean, now: number, stat: TierStat | null,
): PoolDoc {
  const own = median(members.map((m) => m.avgOwn ?? 0)) || (stat ? stat.own / stat.n : 0);
  const ratios = members.filter((m) => (m.avgOwn ?? 0) > 0).map((m) => (m.avgTeam ?? 0) / (m.avgOwn as number));
  const teamRatio = median(ratios) || (stat ? stat.team / stat.own : DEFAULT_TEAM_RATIO);
  const anchorPower = median(members.map((m) => m.power));

  const players: { [uid: string]: PoolEntry } = {};
  const usedNames = new Set<string>();
  members.forEach((m, i) => {
    players[m.uid] = entryOf(m, now - members.length + i, joined);
    usedNames.add((m.playerName ?? "").toLowerCase());
  });

  const botCount = Math.max(0, POOL_VISIBLE_TARGET - members.length);
  const pool: PoolDoc = {
    poolId,
    eventKey,
    tier,
    isOpen: true,
    realCount: members.length,
    players,
    bots: generateBots(hashString(poolId), botCount, usedNames, anchorPower, loadBank()),
    botAlgo: BOT_ALGO_VERSION,
    anchorOwn: own,
    anchorTeamRatio: Math.min(Math.max(teamRatio, 1), 12),
    anchorPower,
    botStartAt: own > 0 ? seasonBounds(eventKey).start : 0,
    realTeamDamage: 0,
    isFinalized: false,
    claimed: {},
    createdAt: now,
  };
  pool.isOpen = isPoolOpen(pool);
  return pool;
}

// ===== Chốt một sảnh =====

// Hạng cuối tính tại thời điểm KẾT THÚC MÙA (bot cũng dừng ở mốc đó). Ghi kết quả vào sảnh + league mới vào người
// chơi. Chỉ đổi league cho người chưa sang mùa khác (tránh chốt muộn đè lên league mới hơn).
export async function finalizePool(db: Firestore, poolRef: DocumentReference, config: BossRushRemoteConfig): Promise<void> {
  await db.runTransaction(async (tx) => {
    const snap = await tx.get(poolRef);
    if (!snap.exists) return;
    const pool = snap.data() as PoolDoc;
    if (pool.isFinalized || !pool.players) return;

    const end = seasonBounds(pool.eventKey).end;
    const ranked = rankPool(pool, end, loadBank());
    const count = poolCount(pool);
    const uids = Object.keys(pool.players);
    const playerRefs = uids.map((uid) => db.collection(PLAYERS).doc(uid));
    const playerSnaps = playerRefs.length > 0 ? await tx.getAll(...playerRefs) : [];

    const results: { [uid: string]: PoolResult } = {};
    for (const e of ranked) {
      if (e.isBot) continue;
      results[e.id] = {
        rank: e.position,
        fights: e.fights,
        newTier: computeNewTier(config, pool.tier, e.position, count, e.fights, e.score),
      };
    }

    tx.update(poolRef, {
      isFinalized: true, isOpen: false, results, finalBossesKilled: poolBossState(pool, end).bossesKilled,
    });
    playerSnaps.forEach((ps, i) => {
      const player = ps.data() as PlayerDoc | undefined;
      const result = results[uids[i]];
      if (!player || !result) return;
      const stillHere = player.lastJoinEventKey === pool.eventKey || player.reservedEventKey === pool.eventKey;
      if (stillHere && player.tier !== result.newTier) tx.update(playerRefs[i], { tier: result.newTier });
    });
  });
}

export async function finalizeEndedPools(db: Firestore, now: Date): Promise<number> {
  const config = await loadRemoteConfig(db);
  const snap = await db.collection(POOLS).where("isFinalized", "==", false).limit(500).get();
  let count = 0;
  for (const doc of snap.docs) {
    if (isPoolEnded(doc.data() as PoolDoc, now)) {
      await finalizePool(db, doc.ref, config);
      count++;
    }
  }
  return count;
}

// ===== Dựng sảnh mùa mới =====

function shuffle<T>(list: T[], seed: number): T[] {
  const rng = makeRng(seed);
  for (let i = list.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [list[i], list[j]] = [list[j], list[i]];
  }
  return list;
}

// "Còn hoạt động" = mùa trước đã đánh ít nhất 1 trận. Mỗi league: trộn ngẫu nhiên, chia đều, mỗi sảnh giữ chỗ tối
// đa POOL_RESERVE_MAX người. Id sảnh và cách trộn cố định theo eventKey → chạy lại vẫn ra cùng kết quả.
export async function buildSeason(db: Firestore, eventKey: string, now: Date): Promise<number> {
  const prevKey = previousEventKey(eventKey);
  const statsData = (await statsRef(db, prevKey).get()).data();
  let batch: WriteBatch = db.batch();
  let ops = 0;
  let pools = 0;
  const flush = async (force: boolean) => {
    if (ops > 0 && (force || ops >= BATCH_LIMIT)) {
      await batch.commit();
      batch = db.batch();
      ops = 0;
    }
  };

  for (let tier = START_TIER; tier <= MAX_TIER; tier++) {
    const snap = await db.collection(PLAYERS)
      .where("lastFoughtEventKey", "==", prevKey).where("tier", "==", tier).limit(MAX_ACTIVE_PER_TIER).get();
    if (snap.empty) continue;

    const actives = snap.docs.map((d) => d.data() as PlayerDoc).sort((a, b) => a.uid.localeCompare(b.uid));
    shuffle(actives, hashString(eventKey + ":" + tier));
    const stat = readTierStat(statsData, tier);
    let offset = 0;
    const sizes = distribute(actives.length, POOL_RESERVE_MAX);
    for (let i = 0; i < sizes.length; i++) {
      const members = actives.slice(offset, offset + sizes[i]);
      offset += sizes[i];
      const poolId = eventKey + "_t" + tier + "_" + (i + 1);
      batch.set(db.collection(POOLS).doc(poolId), createPool(poolId, eventKey, tier, members, false, now.getTime(), stat));
      ops++;
      pools++;
      for (const m of members) {
        batch.update(db.collection(PLAYERS).doc(m.uid), { reservedPoolId: poolId, reservedEventKey: eventKey });
        ops++;
        await flush(false);
      }
    }
  }
  await flush(true);
  return pools;
}

// ===== Khoá + chạy =====

const readySeasons = new Set<string>();

// Bảo đảm mùa eventKey đã được dựng. Trả false nếu một request khác đang dựng dở (client báo "đang chuẩn bị").
export async function prepareSeason(db: Firestore, eventKey: string, now: Date, force = false): Promise<boolean> {
  if (!eventKey) return true;
  if (!force && readySeasons.has(eventKey)) return true;

  const ref = db.collection(SEASONS).doc(eventKey);
  const state = await db.runTransaction(async (tx) => {
    const data = (await tx.get(ref)).data();
    if (!force && data?.state === "ready") return "ready";
    if (data?.state === "building" && now.getTime() - (data.startedAt as number) < LOCK_MS) return "busy";
    tx.set(ref, { state: "building", startedAt: now.getTime() });
    return "mine";
  });
  if (state === "busy") return false;
  if (state === "mine") {
    const finalized = await finalizeEndedPools(db, now);
    const pools = await buildSeason(db, eventKey, now);
    await ref.set({ state: "ready", startedAt: now.getTime(), readyAt: Date.now(), finalized, pools });
  }
  readySeasons.add(eventKey);
  return true;
}

export function ensureCurrentSeason(db: Firestore, now: Date): Promise<boolean> {
  return prepareSeason(db, getEventKey(now), now);
}
