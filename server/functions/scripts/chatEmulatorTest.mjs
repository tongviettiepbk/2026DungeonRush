// Test tích hợp Chat trên Firebase Emulator (chạy: npm run test:emulator:chat).
// Gửi tin Thế giới → đọc lại RTDB chat/ww qua REST (đúng cách client nghe: orderBy "t" + limitToLast + auth=idToken),
// lọc từ tục, chặn chưa đặt tên, giới hạn 10 tin/phút, kênh Clan, hồ sơ, báo cáo 3 người → cấm 1h.
import assert from "node:assert/strict";

const PROJECT = process.env.GCLOUD_PROJECT || "demo-dungeonrush";
const AUTH = "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=emulator-key";
const FN = `http://127.0.0.1:5001/${PROJECT}/us-central1`;
const DB = `http://127.0.0.1:9000`;
const NS = `${PROJECT}-default-rtdb`;

async function signUp() {
  const r = await fetch(AUTH, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ returnSecureToken: true }) });
  const j = await r.json();
  return { token: j.idToken, uid: j.localId };
}

async function call(user, name, data = {}) {
  const r = await fetch(`${FN}/${name}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${user.token}` },
    body: JSON.stringify({ data }),
  });
  const j = await r.json();
  if (j.error) throw new Error(name + ": " + JSON.stringify(j.error));
  return j.result;
}

async function readChannel(user, path, last = 40) {
  const q = `orderBy=${encodeURIComponent('"t"')}&limitToLast=${last}&ns=${NS}&auth=${user.token}`;
  const r = await fetch(`${DB}/${path}.json?${q}`);
  assert.equal(r.status, 200, await r.clone().text());
  return Object.values((await r.json()) ?? {});
}

const snap = (name) => ({ playerName: name, avatarId: 3, power: 1234, items: [{ Slot: 8, ItemId: "w1", Rarity: 2, ItemLevel: 5, SubStats: [] }], companions: [], enchantmentTiers: [] });
const tag = Math.floor(Math.random() * 1e6).toString();

const a = await signUp();
const b = await signUp();
const c = await signUp();
const d = await signUp();

// --- chưa có tên → từ chối ---
const noName = await call(a, "sendchatmessage", { message: "hi" });
assert.equal(noName.success, false);
assert.equal(noName.message, "Not registered");

// --- gửi Thế giới + lọc từ tục + cắt 200 ký tự ---
const s1 = await call(a, "sendchatmessage", { ...snap("Alice"), message: `hello ${tag} you sh1t` });
assert.equal(s1.success, true, JSON.stringify(s1));
assert.ok(s1.timestamp > 0);
const long = await call(a, "sendchatmessage", { ...snap("Alice"), message: tag + "x".repeat(300) });
assert.equal(long.success, true);
let world = await readChannel(b, "chat/ww");
const mine = world.filter((m) => m.u === a.uid && m.m.includes(tag));
assert.equal(mine.length, 2);
assert.equal(mine[0].n, "Alice");
assert.equal(mine[0].a, 3);
assert.equal(mine[0].m, `hello ${tag} you ***`);
assert.equal(mine[1].m.length, 200);

// --- chưa đăng nhập không đọc được ---
const anon = await fetch(`${DB}/chat/ww.json?ns=${NS}`);
assert.equal(anon.status, 401);

// --- giới hạn 10 tin / 60s ---
for (let i = 0; i < 8; i++) assert.equal((await call(a, "sendchatmessage", { message: "spam" + i })).success, true);
const limited = await call(a, "sendchatmessage", { message: "spam!" });
assert.equal(limited.success, false);
assert.equal(limited.message, "Too many messages");

// --- hồ sơ ---
const prof = await call(b, "getchatplayerprofile", { userId: a.uid });
assert.equal(prof.success, true);
assert.equal(prof.player.PlayerName, "Alice");
assert.equal(prof.player.Power, 1234);
assert.equal(prof.player.Items.length, 1);
await call(a, "updatechatplayerprofile", { playerName: "Alicia", avatarId: 7, countryCode: "VN" });
assert.equal((await call(b, "getchatplayerprofile", { userId: a.uid })).player.AvatarId, 7);

// --- kênh Clan: chưa vào clan bị từ chối; vào clan rồi ghi đúng chat/clans/{clanId} ---
assert.equal((await call(b, "sendclanchatmessage", { ...snap("Bob"), message: "yo" })).message, "Not in clan");
const created = await call(b, "createclan", { server: PROJECT, playerName: "Bob", power: 10, avatarId: 0, clanName: "Chat" + tag, joinSetting: "open" });
assert.equal(created.success, true, JSON.stringify(created));
const clanId = created.clan.clanId;
assert.equal((await call(b, "sendclanchatmessage", { ...snap("Bob"), message: "clan " + tag })).success, true);
const clanMsgs = await readChannel(c, "chat/clans/" + clanId);
assert.equal(clanMsgs.length, 1);
assert.equal(clanMsgs[0].m, "clan " + tag);
world = await readChannel(c, "chat/ww");
assert.equal(world.some((m) => m.m === "clan " + tag), false);

// --- báo cáo: tự báo cáo / không lý do bị từ chối; 3 người khác nhau → cấm 1h, banTier 1 ---
const target = await signUp();
await call(target, "sendchatmessage", { ...snap("Target"), message: "bad" });
assert.equal((await call(target, "reportchatuser", { reportedUid: target.uid, reasons: ["Spam"] })).success, false);
assert.equal((await call(b, "reportchatuser", { reportedUid: target.uid, reasons: [] })).success, false);
const rep = { reportedUid: target.uid, type: "world", messageId: "m1", text: "bad", t: 1, channel: "world", reasons: ["Spam", "Other"] };
await call(b, "reportchatuser", rep);
await call(b, "reportchatuser", rep);          // cùng 1 người báo 2 lần chỉ tính 1
await call(c, "reportchatuser", rep);
assert.equal((await call(target, "sendchatmessage", { message: "still ok" })).success, true);
await call(d, "reportchatuser", rep);
const banned = await call(target, "sendchatmessage", { message: "after ban" });
assert.equal(banned.success, false);
assert.equal(banned.banTier, 1);
const now = Math.floor(Date.now() / 1000);
assert.ok(banned.bannedUntil > now + 3500 && banned.bannedUntil <= now + 3600, JSON.stringify(banned));

// --- admin gỡ cấm (emulator) → gửi lại được; báo cáo cũ không tính lại ---
await call(a, "clearchatban", { userId: target.uid });
assert.equal((await call(target, "sendchatmessage", { message: "free" })).success, true);
await call(b, "reportchatuser", rep);
assert.equal((await call(target, "sendchatmessage", { message: "free 2" })).success, true);

console.log("chatEmulatorTest PASS");
