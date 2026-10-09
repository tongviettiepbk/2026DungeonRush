import { getFirestore } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";
import { POOL_SIZE } from "./config";
import { BossRushPlayerModel, PlayerDoc, PoolDoc } from "./models";
import { finalizeEndedPools } from "./bossRush";

// Hàm admin/test (gốc có seedBossRushDummyPlayers / removeBossRushDummyPlayers / finalizeBossRushEndedEvents).
// Quyền: chạy trên emulator, hoặc uid nằm trong document `config/admins` { uids: ["..."] }.

async function requireAdmin(uid: string | undefined): Promise<string> {
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  if (process.env.FUNCTIONS_EMULATOR === "true") return uid;
  const snap = await getFirestore().collection("config").doc("admins").get();
  const uids: string[] = (snap.data()?.uids as string[]) ?? [];
  if (!uids.includes(uid)) throw new HttpsError("permission-denied", "Admin only");
  return uid;
}

// Lấp chỗ trống của nhóm hiện tại bằng bot (copy đồ của người gọi, tên "Player####").
export const seedbossrushdummyplayers = onCall(async (request) => {
  const uid = await requireAdmin(request.auth?.uid);
  const count = Math.max(1, Math.min(POOL_SIZE, Number((request.data as { count?: number })?.count) || POOL_SIZE));
  const db = getFirestore();

  return db.runTransaction(async (tx) => {
    const playerSnap = await tx.get(db.collection("bossRushPlayers").doc(uid));
    const player = playerSnap.data() as PlayerDoc | undefined;
    if (!player?.currentPoolId) return { success: false, message: "Join Boss Rush first" };

    const poolRef = db.collection("bossRushPools").doc(player.currentPoolId);
    const poolSnap = await tx.get(poolRef);
    if (!poolSnap.exists) return { success: false, message: "Pool not found" };
    const pool = poolSnap.data() as PoolDoc;

    let added = 0;
    const now = Date.now();
    while (added < count && Object.keys(pool.players).length < POOL_SIZE) {
      const id = "bot_" + Math.random().toString(36).substring(2, 10);
      const bot: BossRushPlayerModel = {
        UserId: id,
        PlayerName: "Player" + Math.floor(1000 + Math.random() * 9000),
        Position: 0,
        Power: player.power,
        TotalDamagePoints: 0,
        Items: player.items,
        Companions: player.companions,
        EnchantmentTiers: player.enchantmentTiers ?? [],
        ShowCloak: player.showCloak ?? true,
        IsBot: true,
        JoinedAt: now + added,
      };
      pool.players[id] = bot;
      added++;
    }
    pool.playerCount = Object.keys(pool.players).length;
    pool.isOpen = pool.playerCount < POOL_SIZE;
    tx.set(poolRef, pool);
    return { success: true, added };
  });
});

export const removebossrushdummyplayers = onCall(async (request) => {
  const uid = await requireAdmin(request.auth?.uid);
  const db = getFirestore();
  const player = (await db.collection("bossRushPlayers").doc(uid).get()).data() as PlayerDoc | undefined;
  if (!player?.currentPoolId) return { success: false, message: "Join Boss Rush first" };

  const poolRef = db.collection("bossRushPools").doc(player.currentPoolId);
  return db.runTransaction(async (tx) => {
    const pool = (await tx.get(poolRef)).data() as PoolDoc;
    let removed = 0;
    for (const [id, p] of Object.entries(pool.players)) {
      if (p.IsBot) {
        delete pool.players[id];
        removed++;
      }
    }
    pool.playerCount = Object.keys(pool.players).length;
    pool.isOpen = pool.playerCount < POOL_SIZE;
    tx.set(poolRef, pool);
    return { success: true, removed };
  });
});

export const finalizebossrushnow = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  return { success: true, finalized: await finalizeEndedPools(new Date()) };
});
