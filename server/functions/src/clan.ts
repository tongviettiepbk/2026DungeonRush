import { randomBytes } from "crypto";
import { getFirestore, Firestore, Transaction } from "firebase-admin/firestore";
import { CallableRequest, HttpsError, onCall } from "firebase-functions/v2/https";
import {
  BANNER_BG_COLORS, BANNER_BG_TYPES, BANNER_IMAGE_COLORS, BANNER_IMAGE_TYPES, CLAN_ANNOUNCEMENT_MAX, CLAN_DAILY_JOIN_REQUESTS,
  CLAN_DESCRIPTION_MAX, CLAN_JOIN_COOLDOWN_SECONDS, CLAN_MAX_CAPTAINS, CLAN_MAX_MEMBERS, CLAN_NAME_MAX, CLAN_NAME_MIN,
  CLAN_NAME_REGEX, CLAN_SEARCH_LIMIT, tierOf,
} from "./clanWarConfig";
import { getDayKey } from "./schedule";

// Clan — 13 endpoint giống server gốc (tên viết thường: createclan, updateclansettings, ...).
// Response gốc ClanBaseResponseDTO {success, code, message, contentVersion}; lỗi nghiệp vụ trả code ERR_* (client map → Errors.Clan.*).

export const CLANS = "clans";
export const CLAN_PLAYERS = "clanPlayers";
const CLAN_NAMES = "clanNames";
const CONTENT_VERSION = "1.0.0";

export type Role = "leader" | "captain" | "member";
const ROLE_RANK: Record<string, number> = { member: 1, captain: 2, leader: 3 };

export interface ClanMember {
  userId: string;
  playerName: string;
  power: number;
  role: Role;
  avatarId: number;
  joinedAt: number;           // unix giây
}

export interface ClanRequest {
  userId: string;
  playerName: string;
  power: number;
  avatarId: number;
  requestedAt: number;        // unix giây
}

export interface ClanBanner {
  bannerBackgroundTypeId: number;
  bannerBackgroundColorId: number;
  bannerImageTypeId: number;
  bannerImageColorId: number;
}

// Document clans/{clanId}
export interface ClanDoc extends ClanBanner {
  clanId: string;
  clanName: string;
  nameLower: string;
  tierPoints: number;
  clanTier: string;
  joinSetting: "open" | "approval";
  server: string;
  description: string;
  announcement: string;
  members: Record<string, ClanMember>;
  requests: Record<string, ClanRequest>;
  memberCount: number;
  totalPower: number;
  createdAt: number;          // unix giây
  isBot: boolean;
}

// Document clanPlayers/{uid}
export interface ClanPlayerDoc {
  uid: string;
  clanId: string;
  role: Role | "";
  leftAt: number;             // unix giây lần rời/bị kick gần nhất
  requestDayKey: string;
  requestCount: number;
  requestedClanIds: string[];
}

function db(): Firestore {
  return getFirestore();
}

export function requireUid(request: CallableRequest<unknown>): string {
  const uid = request.auth?.uid;
  if (!uid) throw new HttpsError("unauthenticated", "Login required");
  return uid;
}

export function nowSeconds(): number {
  return Math.floor(Date.now() / 1000);
}

function ok<T extends object>(extra: T): T & { success: true; code: string; message: string; contentVersion: string } {
  return { success: true, code: "", message: "", contentVersion: CONTENT_VERSION, ...extra };
}

function fail(code: string, message: string) {
  return { success: false, code, message, contentVersion: CONTENT_VERSION };
}

const PROFANITY = ["fuck", "shit", "bitch", "cunt", "nigger", "nigga", "dick", "pussy", "asshole", "whore", "slut", "rape", "nazi", "hitler"];
export function hasProfanity(text: string): boolean {
  const t = text.toLowerCase();
  return PROFANITY.some((w) => t.includes(w));
}

function clampInt(v: unknown, max: number): number {
  const n = Math.floor(Number(v));
  if (!isFinite(n) || n < 0) return 0;
  return Math.min(n, max - 1);
}

function readBanner(data: Partial<ClanBanner>): ClanBanner {
  return {
    bannerBackgroundTypeId: clampInt(data.bannerBackgroundTypeId, BANNER_BG_TYPES),
    bannerBackgroundColorId: clampInt(data.bannerBackgroundColorId, BANNER_BG_COLORS),
    bannerImageTypeId: clampInt(data.bannerImageTypeId, BANNER_IMAGE_TYPES),
    bannerImageColorId: clampInt(data.bannerImageColorId, BANNER_IMAGE_COLORS),
  };
}

function readJoinSetting(v: unknown): "open" | "approval" {
  const s = String(v ?? "").trim().toLowerCase();
  return s === "approval" || s === "approvalonly" ? "approval" : "open";
}

function readName(v: unknown): string {
  return String(v ?? "").trim().substring(0, 24);
}

function readPower(v: unknown): number {
  const n = Number(v);
  return isFinite(n) && n >= 0 ? n : 0;
}

function newClanPlayer(uid: string): ClanPlayerDoc {
  return { uid, clanId: "", role: "", leftAt: 0, requestDayKey: "", requestCount: 0, requestedClanIds: [] };
}

export function recompute(clan: ClanDoc): void {
  const list = Object.values(clan.members);
  clan.memberCount = list.length;
  clan.totalPower = list.reduce((s, m) => s + (m.power || 0), 0);
  clan.clanTier = tierOf(clan.tierPoints || 0);
}

// ClanWireDTO nhìn từ người xem (myRole rỗng nếu không phải thành viên).
export function toWire(clan: ClanDoc, viewerUid: string, withMembers: boolean) {
  const me = clan.members[viewerUid];
  return {
    clanId: clan.clanId,
    clanName: clan.clanName,
    clanTier: clan.clanTier || tierOf(clan.tierPoints || 0),
    joinSetting: clan.joinSetting,
    server: clan.server,
    description: clan.description,
    announcement: clan.announcement,
    myRole: me ? me.role : "",
    bannerBackgroundTypeId: clan.bannerBackgroundTypeId,
    bannerBackgroundColorId: clan.bannerBackgroundColorId,
    bannerImageTypeId: clan.bannerImageTypeId,
    bannerImageColorId: clan.bannerImageColorId,
    memberCount: clan.memberCount,
    totalPower: clan.totalPower,
    members: withMembers ? Object.values(clan.members) : [],
  };
}

async function getPlayer(tx: Transaction, uid: string): Promise<ClanPlayerDoc> {
  const snap = await tx.get(db().collection(CLAN_PLAYERS).doc(uid));
  return snap.exists ? { ...newClanPlayer(uid), ...(snap.data() as ClanPlayerDoc) } : newClanPlayer(uid);
}

async function getClan(tx: Transaction, clanId: string): Promise<ClanDoc | null> {
  if (!clanId) return null;
  const snap = await tx.get(db().collection(CLANS).doc(clanId));
  return snap.exists ? (snap.data() as ClanDoc) : null;
}

function setPlayer(tx: Transaction, p: ClanPlayerDoc): void {
  tx.set(db().collection(CLAN_PLAYERS).doc(p.uid), p);
}

function setClan(tx: Transaction, clan: ClanDoc): void {
  recompute(clan);
  tx.set(db().collection(CLANS).doc(clan.clanId), clan);
}

function joinCooldownActive(p: ClanPlayerDoc): boolean {
  return p.leftAt > 0 && nowSeconds() - p.leftAt < CLAN_JOIN_COOLDOWN_SECONDS;
}

interface JoinInfo { server?: string; playerName?: string; power?: number; avatarId?: number }

function memberFrom(uid: string, data: JoinInfo, role: Role): ClanMember {
  return {
    userId: uid,
    playerName: readName(data.playerName) || "Player",
    power: readPower(data.power),
    role,
    avatarId: Math.max(0, Math.floor(Number(data.avatarId) || 0)),
    joinedAt: nowSeconds(),
  };
}

// Xoá request đang chờ của uid ở mọi clan khác (khi đã vào 1 clan).
async function clearOtherRequests(tx: Transaction, player: ClanPlayerDoc, exceptClanId: string, loaded: Map<string, ClanDoc>): Promise<void> {
  for (const id of player.requestedClanIds) {
    if (id === exceptClanId) continue;
    const c = loaded.get(id);
    if (c && c.requests?.[player.uid]) {
      delete c.requests[player.uid];
      setClan(tx, c);
    }
  }
  player.requestedClanIds = [];
}

async function loadRequestedClans(tx: Transaction, player: ClanPlayerDoc): Promise<Map<string, ClanDoc>> {
  const map = new Map<string, ClanDoc>();
  for (const id of player.requestedClanIds ?? []) {
    const c = await getClan(tx, id);
    if (c) map.set(id, c);
  }
  return map;
}

// ===== createClan =====

export const createclan = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as JoinInfo & Partial<ClanBanner> & { clanName?: string; joinSetting?: string };
  const name = String(data.clanName ?? "").trim();
  if (name.length < CLAN_NAME_MIN || name.length > CLAN_NAME_MAX || !CLAN_NAME_REGEX.test(name)) {
    return fail("ERR_VALIDATION", "Invalid clan name");
  }
  if (hasProfanity(name)) return fail("ERR_VALIDATION", "Profanity");
  const nameLower = name.toLowerCase();
  const clanId = "c_" + randomBytes(8).toString("hex");

  return db().runTransaction(async (tx) => {
    const player = await getPlayer(tx, uid);
    const nameSnap = await tx.get(db().collection(CLAN_NAMES).doc(nameLower));
    const current = await getClan(tx, player.clanId);
    const requested = await loadRequestedClans(tx, player);
    if (current && current.members[uid]) return fail("ERR_ALREADY_IN_CLAN", "Already in a clan");
    if (joinCooldownActive(player)) return fail("ERR_JOIN_COOLDOWN", "Join cooldown");
    if (nameSnap.exists) return fail("ERR_NAME_TAKEN", "Name taken");

    const clan: ClanDoc = {
      clanId, clanName: name, nameLower, tierPoints: 0, clanTier: "D",
      joinSetting: readJoinSetting(data.joinSetting), server: String(data.server ?? ""),
      description: "", announcement: "", members: { [uid]: memberFrom(uid, data, "leader") }, requests: {},
      memberCount: 1, totalPower: 0, createdAt: nowSeconds(), isBot: false, ...readBanner(data),
    };
    await clearOtherRequests(tx, player, clanId, requested);
    player.clanId = clanId;
    player.role = "leader";
    setPlayer(tx, player);
    setClan(tx, clan);
    tx.set(db().collection(CLAN_NAMES).doc(nameLower), { clanId });
    return ok({ clan: toWire(clan, uid, true) });
  });
});

// ===== updateClanSettings (chỉ Leader) =====

export const updateclansettings = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as Partial<ClanBanner> & { clanId?: string; description?: string; joinSetting?: string };
  const description = String(data.description ?? "").trim();
  if (description.length > CLAN_DESCRIPTION_MAX) return fail("ERR_VALIDATION", "Description too long");
  if (hasProfanity(description)) return fail("ERR_VALIDATION", "Profanity");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (me.role !== "leader") return fail("ERR_FORBIDDEN", "Leader only");
    clan.description = description;
    clan.joinSetting = readJoinSetting(data.joinSetting);
    Object.assign(clan, readBanner(data));
    setClan(tx, clan);
    return ok({ clan: toWire(clan, uid, true) });
  });
});

// ===== updateClanAnnouncement (Captain/Leader) =====

export const updateclanannouncement = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; announcement?: string };
  const text = String(data.announcement ?? "").trim();
  if (text.length > CLAN_ANNOUNCEMENT_MAX) return fail("ERR_VALIDATION", "Announcement too long");
  if (hasProfanity(text)) return fail("ERR_VALIDATION", "Profanity");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (me.role === "member") return fail("ERR_FORBIDDEN", "Captain or leader only");
    clan.announcement = text;
    setClan(tx, clan);
    return ok({ announcement: text });
  });
});

// ===== searchClans =====

export const searchclans = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as {
    server?: string; clanName?: string; minMemberCount?: number | null; maxMemberCount?: number | null; hideApprovalOnly?: boolean;
  };
  const server = String(data.server ?? "");
  const name = String(data.clanName ?? "").trim().toLowerCase();
  let query = db().collection(CLANS).where("server", "==", server);
  query = name.length > 0
    ? query.where("nameLower", ">=", name).where("nameLower", "<", name + "").orderBy("nameLower").limit(100)
    : query.orderBy("totalPower", "desc").limit(100);
  const [snap, playerSnap] = await Promise.all([query.get(), db().collection(CLAN_PLAYERS).doc(uid).get()]);

  const min = typeof data.minMemberCount === "number" ? data.minMemberCount : null;
  const max = typeof data.maxMemberCount === "number" ? data.maxMemberCount : null;
  const clans = snap.docs.map((d) => d.data() as ClanDoc)
    .filter((c) => !c.isBot)
    .filter((c) => (min === null || c.memberCount >= min) && (max === null || c.memberCount <= max))
    .filter((c) => !(data.hideApprovalOnly === true && c.joinSetting === "approval"))
    .slice(0, CLAN_SEARCH_LIMIT);
  const player = playerSnap.exists ? (playerSnap.data() as ClanPlayerDoc) : newClanPlayer(uid);
  return ok({
    clans: clans.map((c) => toWire(c, uid, false)),
    requestedClanIds: (player.requestedClanIds ?? []).filter((id) => clans.some((c) => c.clanId === id && c.requests?.[uid])),
  });
});

// ===== getClanDetails (clanId rỗng = clan của mình; cập nhật power/tên của mình) =====

export const getclandetails = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; power?: number; playerName?: string };

  return db().runTransaction(async (tx) => {
    const player = await getPlayer(tx, uid);
    const clanId = String(data.clanId ?? "") || player.clanId;
    const clan = await getClan(tx, clanId);
    if (!clan) {
      if (!data.clanId && player.clanId) {
        player.clanId = "";
        player.role = "";
        setPlayer(tx, player);
      }
      return clanId ? fail("ERR_NOT_FOUND", "Clan not found") : fail("ERR_NOT_IN_CLAN", "Not in clan");
    }
    const me = clan.members[uid];
    if (me) {
      let changed = false;
      const power = readPower(data.power);
      if (power > 0 && power !== me.power) { me.power = power; changed = true; }
      const name = readName(data.playerName);
      if (name && name !== me.playerName) { me.playerName = name; changed = true; }
      if (changed) setClan(tx, clan);
      if (player.clanId !== clan.clanId || player.role !== me.role) {
        player.clanId = clan.clanId;
        player.role = me.role;
        setPlayer(tx, player);
      }
    } else if (!data.clanId && player.clanId === clanId) {
      // Đã bị kick khi offline.
      player.clanId = "";
      player.role = "";
      setPlayer(tx, player);
      return fail("ERR_NOT_IN_CLAN", "Not in clan");
    }
    return ok({ clan: toWire(clan, uid, true) });
  });
});

// ===== joinOpenClan =====

export const joinopenclan = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as JoinInfo & { clanId?: string };

  return db().runTransaction(async (tx) => {
    const player = await getPlayer(tx, uid);
    const current = await getClan(tx, player.clanId);
    const clan = await getClan(tx, String(data.clanId ?? ""));
    const requested = await loadRequestedClans(tx, player);
    if (current && current.members[uid]) return fail("ERR_ALREADY_IN_CLAN", "Already in a clan");
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    if (joinCooldownActive(player)) return fail("ERR_JOIN_COOLDOWN", "Join cooldown");
    if (clan.joinSetting !== "open") return fail("ERR_CONFLICT_STATE_CHANGED", "Clan requires approval");
    if (clan.memberCount >= CLAN_MAX_MEMBERS) return fail("ERR_CLAN_FULL", "Clan is full");

    clan.members[uid] = memberFrom(uid, data, "member");
    delete clan.requests[uid];
    requested.delete(clan.clanId);
    await clearOtherRequests(tx, player, clan.clanId, requested);
    player.clanId = clan.clanId;
    player.role = "member";
    setPlayer(tx, player);
    setClan(tx, clan);
    return ok({ clan: toWire(clan, uid, true) });
  });
});

// ===== createClanJoinRequest =====

export const createclanjoinrequest = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as JoinInfo & { clanId?: string };
  const dayKey = getDayKey(new Date());

  return db().runTransaction(async (tx) => {
    const player = await getPlayer(tx, uid);
    const current = await getClan(tx, player.clanId);
    const clan = await getClan(tx, String(data.clanId ?? ""));
    if (current && current.members[uid]) return fail("ERR_ALREADY_IN_CLAN", "Already in a clan");
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    if (joinCooldownActive(player)) return fail("ERR_JOIN_COOLDOWN", "Join cooldown");
    if (clan.requests[uid]) return fail("ERR_REQUEST_ALREADY_PENDING", "Request already pending");
    if (clan.memberCount >= CLAN_MAX_MEMBERS) return fail("ERR_CLAN_FULL", "Clan is full");
    if (player.requestDayKey !== dayKey) {
      player.requestDayKey = dayKey;
      player.requestCount = 0;
    }
    if (player.requestCount >= CLAN_DAILY_JOIN_REQUESTS) return fail("ERR_REQUEST_COOLDOWN", "Daily request limit");

    const m = memberFrom(uid, data, "member");
    clan.requests[uid] = { userId: uid, playerName: m.playerName, power: m.power, avatarId: m.avatarId, requestedAt: nowSeconds() };
    player.requestCount++;
    if (!player.requestedClanIds.includes(clan.clanId)) player.requestedClanIds.push(clan.clanId);
    setPlayer(tx, player);
    setClan(tx, clan);
    return ok({ pending: true, clanId: clan.clanId });
  });
});

// ===== getClanJoinRequests (Captain/Leader) =====

export const getclanjoinrequests = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string };
  const snap = await db().collection(CLANS).doc(String(data.clanId ?? "")).get();
  if (!snap.exists) return fail("ERR_NOT_FOUND", "Clan not found");
  const clan = snap.data() as ClanDoc;
  const me = clan.members[uid];
  if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
  if (me.role === "member") return fail("ERR_FORBIDDEN", "Captain or leader only");
  const requests = Object.values(clan.requests ?? {}).sort((a, b) => a.requestedAt - b.requestedAt);
  return ok({ requests });
});

// ===== acceptClanJoinRequest / denyClanJoinRequest =====

export const acceptclanjoinrequest = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; targetUserId?: string };
  const target = String(data.targetUserId ?? "");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    const targetPlayer = await getPlayer(tx, target);
    const targetCurrent = await getClan(tx, targetPlayer.clanId);
    const requested = await loadRequestedClans(tx, targetPlayer);
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (me.role === "member") return fail("ERR_FORBIDDEN", "Captain or leader only");
    const req = clan.requests[target];
    if (!req) return fail("ERR_REQUEST_NOT_PENDING", "Request not pending");
    if (targetCurrent && targetCurrent.members[target]) {
      delete clan.requests[target];
      setClan(tx, clan);
      return fail("ERR_ALREADY_IN_CLAN", "Player already in a clan");
    }
    if (clan.memberCount >= CLAN_MAX_MEMBERS) return fail("ERR_CLAN_FULL", "Clan is full");

    clan.members[target] = {
      userId: target, playerName: req.playerName, power: req.power, role: "member", avatarId: req.avatarId, joinedAt: nowSeconds(),
    };
    delete clan.requests[target];
    requested.delete(clan.clanId);
    await clearOtherRequests(tx, targetPlayer, clan.clanId, requested);
    targetPlayer.clanId = clan.clanId;
    targetPlayer.role = "member";
    setPlayer(tx, targetPlayer);
    setClan(tx, clan);
    return ok({});
  });
});

export const denyclanjoinrequest = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; targetUserId?: string };
  const target = String(data.targetUserId ?? "");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    const targetPlayer = await getPlayer(tx, target);
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (me.role === "member") return fail("ERR_FORBIDDEN", "Captain or leader only");
    if (!clan.requests[target]) return fail("ERR_REQUEST_NOT_PENDING", "Request not pending");
    delete clan.requests[target];
    targetPlayer.requestedClanIds = targetPlayer.requestedClanIds.filter((id) => id !== clan.clanId);
    setPlayer(tx, targetPlayer);
    setClan(tx, clan);
    return ok({});
  });
});

// ===== promoteOrDemoteClanMember (Leader): Member ↔ Captain =====

export const promoteordemoteclanmember = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; targetUserId?: string };
  const target = String(data.targetUserId ?? "");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    const targetPlayer = await getPlayer(tx, target);
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    const t = clan.members[target];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (!t) return fail("ERR_CONFLICT_STATE_CHANGED", "Target left the clan");
    if (me.role !== "leader" || t.role === "leader") return fail("ERR_FORBIDDEN", "Leader only");
    if (t.role === "member") {
      const captains = Object.values(clan.members).filter((m) => m.role === "captain").length;
      if (captains >= CLAN_MAX_CAPTAINS) return fail("ERR_CAPTAIN_LIMIT", "Captain limit");
      t.role = "captain";
    } else {
      t.role = "member";
    }
    targetPlayer.clanId = clan.clanId;
    targetPlayer.role = t.role;
    setPlayer(tx, targetPlayer);
    setClan(tx, clan);
    return ok({ targetUserId: target, newRole: t.role });
  });
});

// ===== kickClanMember (frk: Captain → Member; Leader → Member/Captain) =====

export function canActOn(actor: Role, target: Role): boolean {
  if (actor === "captain") return target === "member";
  if (actor === "leader") return target === "member" || target === "captain";
  return false;
}

export const kickclanmember = onCall(async (request) => {
  const uid = requireUid(request);
  const data = (request.data ?? {}) as { clanId?: string; targetUserId?: string };
  const target = String(data.targetUserId ?? "");

  return db().runTransaction(async (tx) => {
    const clan = await getClan(tx, String(data.clanId ?? ""));
    const targetPlayer = await getPlayer(tx, target);
    if (!clan) return fail("ERR_NOT_FOUND", "Clan not found");
    const me = clan.members[uid];
    const t = clan.members[target];
    if (!me) return fail("ERR_NOT_IN_CLAN", "Not in clan");
    if (!t) return fail("ERR_CONFLICT_STATE_CHANGED", "Target left the clan");
    if (!canActOn(me.role, t.role)) return fail("ERR_FORBIDDEN", "Forbidden");
    delete clan.members[target];
    if (targetPlayer.clanId === clan.clanId) {
      targetPlayer.clanId = "";
      targetPlayer.role = "";
      targetPlayer.leftAt = nowSeconds();
      setPlayer(tx, targetPlayer);
    }
    setClan(tx, clan);
    return ok({});
  });
});

// ===== leaveClan — Leader rời: chuyển cho Captain (power cao nhất) rồi tới Member; người cuối → giải tán =====

export const leaveclan = onCall(async (request) => {
  const uid = requireUid(request);

  return db().runTransaction(async (tx) => {
    const player = await getPlayer(tx, uid);
    const clan = await getClan(tx, player.clanId);
    if (!clan || !clan.members[uid]) {
      player.clanId = "";
      player.role = "";
      setPlayer(tx, player);
      return fail("ERR_NOT_IN_CLAN", "Not in clan");
    }
    const wasLeader = clan.members[uid].role === "leader";
    delete clan.members[uid];
    const rest = Object.values(clan.members);
    rest.sort((a, b) => (ROLE_RANK[b.role] - ROLE_RANK[a.role]) || (b.power - a.power) || (a.joinedAt - b.joinedAt));
    // Đọc doc người thừa kế TRƯỚC mọi lệnh ghi (luật transaction Firestore).
    const heirPlayer = wasLeader && rest.length > 0 ? await getPlayer(tx, rest[0].userId) : null;

    player.clanId = "";
    player.role = "";
    player.leftAt = nowSeconds();
    setPlayer(tx, player);

    if (rest.length === 0) {
      tx.delete(db().collection(CLANS).doc(clan.clanId));
      tx.delete(db().collection(CLAN_NAMES).doc(clan.nameLower));
      return ok({ disbanded: true });
    }
    if (heirPlayer) {
      rest[0].role = "leader";
      heirPlayer.clanId = clan.clanId;
      heirPlayer.role = "leader";
      setPlayer(tx, heirPlayer);
    }
    setClan(tx, clan);
    return ok({ disbanded: false });
  });
});
