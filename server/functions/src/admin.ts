import { getFirestore } from "firebase-admin/firestore";
import { HttpsError, onCall } from "firebase-functions/v2/https";
import { getEventKey, getNextEventKey, isEventActive } from "./schedule";
import { finalizeEndedPools, prepareSeason } from "./season";

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

// Bot giờ có sẵn trong sảnh ngay lúc tạo (bots.ts) nên hai hàm này không còn việc gì; giữ tên để client cũ gọi không lỗi.
export const seedbossrushdummyplayers = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  return { success: true, added: 0 };
});

export const removebossrushdummyplayers = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  return { success: true, removed: 0 };
});

// Chốt các sảnh đã hết mùa ngay (emulator không chạy tác vụ hẹn giờ). rebuild = true: dựng lại sảnh của mùa đang mở
// (Thứ Hai: mùa kế) — để test luồng giữ chỗ đầu mùa.
export const finalizebossrushnow = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  const now = new Date();
  const db = getFirestore();
  if ((request.data as { rebuild?: boolean })?.rebuild === true) {
    const eventKey = isEventActive(now) ? getEventKey(now) : getNextEventKey(now);
    return { success: true, rebuilt: await prepareSeason(db, eventKey, now, true), eventKey };
  }
  return { success: true, finalized: await finalizeEndedPools(db, now) };
});
