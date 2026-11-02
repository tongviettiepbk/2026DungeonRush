import { getApp } from "firebase-admin/app";
import { getDatabase, getDatabaseWithUrl, Database } from "firebase-admin/database";
import { FieldValue, getFirestore, Firestore } from "firebase-admin/firestore";
import { onCall } from "firebase-functions/v2/https";
import { BossRushCompanionModel, BossRushItemModel } from "./models";
import { sanitizeCompanions, sanitizeEnchantmentTiers, sanitizeItems } from "./bossRush";
import { requireAdmin } from "./admin";
import { CLAN_PLAYERS, ClanPlayerDoc, nowSeconds, requireUid } from "./clan";

// Chat Thế giới + Clan — endpoint giống server gốc (sendchatmessage, sendclanchatmessage, getchatplayerprofile,
// updatechatplayerprofile, reportchatuser). Tin nhắn ghi vào Realtime Database đúng đường dẫn gốc chat/ww và
// chat/clans/{clanId} (client nghe ChildAdded, gốc orderByChild("t").limitToLast(40)); client chỉ được đọc (database.rules.json).
// Phần server gốc không có trong APK (giới hạn tin, luật cấm) là [ĐỀ XUẤT] — xem GDD/CHAT_DESIGN.md.

const CHAT_USERS = "chatUsers";
const CHAT_REPORTS = "chatReports";
const PATH_WORLD = "chat/ww";
const PATH_CLANS = "chat/clans/";

const MESSAGE_MAX = 200;                       // gốc ChatController.ffj cắt 200 ký tự
const NAME_MAX = 15;                           // NicknamePopup gốc 3-15 ký tự
const RATE_WINDOW_SECONDS = 60;                // [ĐỀ XUẤT] tối đa 10 tin / 60s
const RATE_MAX_MESSAGES = 10;
const KEEP_SECONDS = 3 * 24 * 3600;            // [ĐỀ XUẤT] xoá tin cũ hơn 3 ngày (client chỉ đọc 40 tin cuối)
const PRUNE_BATCH = 50;
const REPORT_WINDOW_SECONDS = 24 * 3600;       // [ĐỀ XUẤT] 3 người khác nhau báo cáo trong 24h → cấm
const REPORTS_TO_BAN = 3;
// [ĐỀ XUẤT] mức cấm tăng dần: 1h → 24h → 7 ngày → vĩnh viễn. Vĩnh viễn = mốc gốc 32503680000 (năm 3000, ChatController.uzx).
const BAN_SECONDS = [3600, 24 * 3600, 7 * 24 * 3600];
export const PERMANENT_BAN_UNTIL = 32503680000;
const REPORT_REASONS = ["Harassment", "Spam", "Inappropriate Language", "Offensive Player Name", "Cheating", "Hate Speech", "Other"];

// Danh sách từ tục + luật thay ký tự giống bộ lọc client gốc (class rp): \b(từ1|từ2|...)\b, không phân biệt hoa thường, thay "***".
const PROFANITY = [
  "fuck", "fucker", "fuckers", "fucking", "fucked", "fucks", "motherfucker", "motherfucking", "shit", "shits", "shitty", "shitting",
  "bullshit", "ass", "asshole", "assholes", "asses", "bitch", "bitches", "bitchy", "bitching", "damn", "damned", "damnit", "goddamn",
  "goddamnit", "hell", "dick", "dicks", "dickhead", "dickheads", "cock", "cocks", "cocksucker", "cocksuckers", "pussy", "pussies", "cunt",
  "cunts", "bastard", "bastards", "slut", "sluts", "slutty", "whore", "whores", "piss", "pissed", "pissing", "crap", "crappy", "tits",
  "titty", "titties", "boob", "boobs", "anus", "dildo", "dildos", "fag", "fags", "faggot", "faggots", "nigger", "niggers", "nigga",
  "niggas", "retard", "retards", "retarded", "wanker", "wankers", "wank", "twat", "twats", "porn", "porno", "pornography", "penis",
  "vagina", "jerk", "jerks", "jerkoff", "douche", "douchebag", "douchebags", "spic", "spics", "chink", "chinks", "kike", "kikes",
  "wetback", "wetbacks", "beaner", "beaners", "nazi", "nazis", "rape", "raped", "raping", "rapist", "molest", "molester", "pedophile",
  "pedo", "pedos", "stfu", "gtfo", "lmfao", "wtf", "milf",
];
const LEET: Record<string, string> = { i: "[i1!|]", o: "[o0]", t: "[t7]", a: "[a@4]", e: "[e3]", l: "[l1|]", s: "[s$5]" };
const PROFANITY_REGEX = new RegExp(
  "\\b(" + PROFANITY.map((w) => [...w].map((c) => LEET[c] ?? c.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")).join("")).join("|") + ")\\b",
  "gi",
);

export function filterProfanity(text: string): string {
  return text.replace(PROFANITY_REGEX, "***");
}

// Document chatUsers/{uid}: hồ sơ hiện khi người khác bấm avatar + trạng thái cấm + mốc gửi tin gần đây.
interface ChatUserDoc {
  uid: string;
  playerName: string;
  avatarId: number;
  countryCode: string;
  power: number;
  items: BossRushItemModel[];
  companions: BossRushCompanionModel[];
  enchantmentTiers: number[];
  showCloak: boolean;
  bannedUntil: number;        // unix giây, 0 = không cấm
  banTier: number;            // số lần đã bị cấm
  lastBanAt: number;          // báo cáo trước mốc này không tính cho lần cấm kế
  sentTimes: number[];        // unix giây các tin trong RATE_WINDOW_SECONDS gần nhất
  updatedAt: number;
}

// Snapshot đồ gửi kèm mỗi tin (gốc ChatSendRequestDTO: items/companions/wing/cape/enchantmentTiers) — cùng khuôn Boss Rush.
interface ChatSendRequest {
  message?: string;
  playerName?: string;
  avatarId?: number;
  power?: number;
  items?: BossRushItemModel[];
  companions?: BossRushCompanionModel[];
  enchantmentTiers?: number[];
  showCloak?: boolean;
}

interface ChatMessage {
  u: string;
  a: number;
  n: string;
  m: string;
  t: number;
}

function fs(): Firestore {
  return getFirestore();
}

// RTDB: lấy URL từ FIREBASE_CONFIG (server thật có khi đã bật Realtime Database); thiếu thì dùng instance mặc định
// <project>-default-rtdb. Emulator luôn dùng instance mặc định (FIREBASE_CONFIG ở emulator trỏ namespace khác, không có rules).
function rtdb(): Database {
  const app = getApp();
  if (app.options.databaseURL && !process.env.FIREBASE_DATABASE_EMULATOR_HOST) return getDatabase(app);
  const project = app.options.projectId ?? process.env.GCLOUD_PROJECT ?? "";
  return getDatabaseWithUrl(`https://${project}-default-rtdb.firebaseio.com`, app);
}

function newChatUser(uid: string): ChatUserDoc {
  return {
    uid, playerName: "", avatarId: 0, countryCode: "", power: 0, items: [], companions: [], enchantmentTiers: [], showCloak: true,
    bannedUntil: 0, banTier: 0, lastBanAt: 0, sentTimes: [], updatedAt: 0,
  };
}

function readName(v: unknown): string {
  return String(v ?? "").trim().substring(0, NAME_MAX);
}

function readAvatar(v: unknown): number {
  const n = Math.floor(Number(v));
  return isFinite(n) && n >= 0 ? n : 0;
}

function applySnapshot(user: ChatUserDoc, data: ChatSendRequest): void {
  const name = readName(data.playerName);
  if (name) user.playerName = name;
  if (data.avatarId !== undefined) user.avatarId = readAvatar(data.avatarId);
  if (typeof data.power === "number" && isFinite(data.power) && data.power >= 0) user.power = data.power;
  if (data.items !== undefined) user.items = sanitizeItems(data.items);
  if (data.companions !== undefined) user.companions = sanitizeCompanions(data.companions);
  if (data.enchantmentTiers !== undefined) user.enchantmentTiers = sanitizeEnchantmentTiers(data.enchantmentTiers);
  if (typeof data.showCloak === "boolean") user.showCloak = data.showCloak;
}

function sendResult(success: boolean, message: string, timestamp: number, user: ChatUserDoc) {
  return { success, message, timestamp, bannedUntil: user.bannedUntil, banTier: user.banTier };
}

// Gửi 1 tin vào kênh path: kiểm cấm + giới hạn tin, lọc từ tục, cập nhật hồ sơ, ghi RTDB, dọn tin cũ.
async function send(uid: string, data: ChatSendRequest, path: string) {
  const now = nowSeconds();
  const ref = fs().collection(CHAT_USERS).doc(uid);
  const text = filterProfanity(String(data.message ?? "").trim().substring(0, MESSAGE_MAX));

  const outcome = await fs().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const user = snap.exists ? { ...newChatUser(uid), ...(snap.data() as ChatUserDoc) } : newChatUser(uid);
    applySnapshot(user, data);
    if (user.bannedUntil > now) return { user, status: "banned" };
    if (!text) return { user, status: "empty" };
    if (!user.playerName) return { user, status: "noname" };

    user.sentTimes = (user.sentTimes ?? []).filter((t) => t > now - RATE_WINDOW_SECONDS);
    if (user.sentTimes.length >= RATE_MAX_MESSAGES) {
      tx.set(ref, user);
      return { user, status: "rate" };
    }
    user.sentTimes.push(now);
    user.updatedAt = now;
    tx.set(ref, user);
    return { user, status: "ok" };
  });

  const user = outcome.user;
  if (outcome.status === "banned") return sendResult(false, "Banned", 0, user);
  if (outcome.status === "empty") return sendResult(false, "Empty message", 0, user);
  if (outcome.status === "noname") return sendResult(false, "Not registered", 0, user);
  if (outcome.status === "rate") return sendResult(false, "Too many messages", 0, user);

  const msg: ChatMessage = { u: uid, a: user.avatarId, n: user.playerName, m: text, t: now };
  await rtdb().ref(path).push(msg);
  await prune(path, now);
  return sendResult(true, "", now, user);
}

async function prune(path: string, now: number): Promise<void> {
  const old = await rtdb().ref(path).orderByChild("t").endAt(now - KEEP_SECONDS).limitToFirst(PRUNE_BATCH).get();
  if (!old.exists()) return;
  const updates: Record<string, null> = {};
  old.forEach((child) => {
    updates[child.key as string] = null;
  });
  await rtdb().ref(path).update(updates);
}

// ===== Endpoint =====

export const sendchatmessage = onCall(async (request) => {
  const uid = requireUid(request);
  return send(uid, (request.data ?? {}) as ChatSendRequest, PATH_WORLD);
});

// Kênh clan lấy clanId từ server (clanPlayers/{uid}), không tin clanId client gửi.
export const sendclanchatmessage = onCall(async (request) => {
  const uid = requireUid(request);
  const snap = await fs().collection(CLAN_PLAYERS).doc(uid).get();
  const clanId = snap.exists ? (snap.data() as ClanPlayerDoc).clanId : "";
  if (!clanId) return { success: false, message: "Not in clan", timestamp: 0, bannedUntil: 0, banTier: 0 };
  return send(uid, (request.data ?? {}) as ChatSendRequest, PATH_CLANS + clanId);
});

// Hồ sơ người chơi khi bấm avatar trong chat — cùng khuôn BossRushPlayerModel để UIPlayerPopup vẽ thẳng.
export const getchatplayerprofile = onCall(async (request) => {
  requireUid(request);
  const userId = String((request.data as { userId?: string })?.userId ?? "");
  if (!userId) return { success: false, message: "Missing userId" };
  const snap = await fs().collection(CHAT_USERS).doc(userId).get();
  if (!snap.exists) return { success: false, message: "Not found" };
  const u = snap.data() as ChatUserDoc;
  return {
    success: true, message: "",
    player: {
      UserId: u.uid, PlayerName: u.playerName, AvatarId: u.avatarId ?? 0, Position: 0, Power: u.power ?? 0, TotalDamagePoints: 0,
      Items: u.items ?? [], Companions: u.companions ?? [], EnchantmentTiers: u.enchantmentTiers ?? [], ShowCloak: u.showCloak ?? true,
    },
  };
});

// Gốc gọi khi đặt/đổi tên (NicknamePopup), đổi avatar (AvatarSelectPopup), đổi quốc gia (CountrySelectPopup).
export const updatechatplayerprofile = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { playerName?: string; avatarId?: number; countryCode?: string };
  const ref = fs().collection(CHAT_USERS).doc(uid);
  await fs().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const user = snap.exists ? { ...newChatUser(uid), ...(snap.data() as ChatUserDoc) } : newChatUser(uid);
    const name = readName(data.playerName);
    if (name) user.playerName = name;
    if (data.avatarId !== undefined) user.avatarId = readAvatar(data.avatarId);
    if (typeof data.countryCode === "string") user.countryCode = data.countryCode.substring(0, 8);
    user.updatedAt = nowSeconds();
    tx.set(ref, user);
  });
  return { success: true, message: "" };
});

// Báo cáo (ReportPopup gốc: 7 lý do). Đủ REPORTS_TO_BAN người khác nhau trong 24h kể từ lần cấm trước → cấm mức kế tiếp.
export const reportchatuser = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as {
    reportedUid?: string; type?: string; messageId?: string; text?: string; t?: number; channel?: string; reasons?: string[];
  };
  const reportedUid = String(data.reportedUid ?? "");
  const reasons = (Array.isArray(data.reasons) ? data.reasons : []).map(String).filter((r) => REPORT_REASONS.includes(r));
  if (!reportedUid || reportedUid === uid) return { success: false, message: "Invalid target" };
  if (reasons.length === 0) return { success: false, message: "No reason" };

  const now = nowSeconds();
  await fs().collection(CHAT_REPORTS).add({
    reporterUid: uid, reportedUid, reasons, type: String(data.type ?? ""), messageId: String(data.messageId ?? ""),
    text: String(data.text ?? "").substring(0, MESSAGE_MAX), t: Number(data.t) || 0, channel: String(data.channel ?? ""),
    createdAt: now,
  });

  const ref = fs().collection(CHAT_USERS).doc(reportedUid);
  await fs().runTransaction(async (tx) => {
    const snap = await tx.get(ref);
    const user = snap.exists ? { ...newChatUser(reportedUid), ...(snap.data() as ChatUserDoc) } : newChatUser(reportedUid);
    if (user.bannedUntil > now) return;
    const since = Math.max(now - REPORT_WINDOW_SECONDS, user.lastBanAt ?? 0);
    const reports = await tx.get(fs().collection(CHAT_REPORTS).where("reportedUid", "==", reportedUid).where("createdAt", ">", since));
    const reporters = new Set(reports.docs.map((d) => d.get("reporterUid") as string));
    if (reporters.size < REPORTS_TO_BAN) return;
    user.bannedUntil = user.banTier < BAN_SECONDS.length ? now + BAN_SECONDS[user.banTier] : PERMANENT_BAN_UNTIL;
    user.banTier += 1;
    user.lastBanAt = now;
    user.updatedAt = now;
    tx.set(ref, user);
  });
  return { success: true, message: "" };
});

// Admin (emulator, hoặc uid trong config/admins): gỡ cấm + bỏ đếm báo cáo cũ của 1 người.
export const clearchatban = onCall(async (request) => {
  await requireAdmin(request.auth?.uid);
  const userId = String((request.data as { userId?: string })?.userId ?? "");
  if (!userId) return { success: false };
  await fs().collection(CHAT_USERS).doc(userId).set(
    { bannedUntil: 0, banTier: 0, lastBanAt: nowSeconds(), sentTimes: FieldValue.delete() }, { merge: true },
  );
  return { success: true };
});
