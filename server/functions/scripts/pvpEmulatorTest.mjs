// Test tích hợp PvP trên Firebase Emulator (chạy: npm run test:emulator:pvp).
// Giả lập client Unity: đăng nhập ẩn danh → openpvp → findpvpopponents → startpvpbattle → reportpvpbattle → leaderboard.
import assert from "node:assert/strict";

const PROJECT = process.env.GCLOUD_PROJECT || "demo-dungeonrush";
const AUTH = "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=emulator-key";
const FN = `http://127.0.0.1:5001/${PROJECT}/us-central1`;

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

const snapshot = (name, power, country = "VN") => ({
  server: PROJECT, playerName: name, countryCode: country, avatarId: 1, power, contentVersion: "1.0.0",
  items: [
    { Slot: 8, ItemId: "BasicBow", Rarity: 2, ItemLevel: 5, SubStats: [{ Type: 4, Value: 0.12 }] },
    { Slot: 7, ItemId: "3", Rarity: 4, ItemLevel: 2, SubStats: [] },
  ],
  companions: [{ CompanionId: "Companion_1", CompanionLevel: 3, Equipped: true }],
  enchantmentTiers: [0, 2, 0, 0, 0, 0, 5, 0, 99],
  showCloak: false,
});

// Dọn dữ liệu PvP còn sót của lần chạy trước.
const admin = await signUp();
await call(admin, "removeallpvpplayers");

const a = await signUp();
const b = await signUp();

// Chưa đăng nhập → 401.
const anon = await fetch(`${FN}/openpvp`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ data: {} }) });
assert.equal(anon.status, 401);

// --- openPvP: người mới 1000 trophy, Iron, 5 vé free, 0 vé ads, bảng thưởng 10 league ---
const oa = await call(a, "openpvp", snapshot("Alice", 1000));
assert.equal(oa.success, true);
assert.equal(oa.trophy, 1000);
assert.equal(oa.leagueIndex, 1);
assert.equal(oa.hasName, true);
assert.deepEqual(
  { f: oa.tickets.freeRemaining, a: oa.tickets.adRemaining, c: oa.tickets.adClaimedToday, m: oa.tickets.maxAdPerDay, d: oa.tickets.dailyFreeTickets },
  { f: 5, a: 0, c: 0, m: 4, d: 5 });
assert.equal(oa.rewardTable.length, 10);
assert.equal(typeof oa.rewardTable[0].winRewards[0].Type, "string");
await call(b, "openpvp", snapshot("Bob", 2000));

// --- findPvPOpponents: A thấy B (không thấy chính mình), Projected theo Elo ---
const fa = await call(a, "findpvpopponents", { server: PROJECT });
assert.equal(fa.success, true);
assert.ok(fa.rosterToken);
assert.ok(fa.rosterExpiresAt > Date.now() / 1000);
assert.equal(fa.opponents.length, 1);
const ob = fa.opponents[0];
assert.equal(ob.UserId, b.uid);
assert.equal(ob.Trophy, 1000);
assert.equal(ob.ProjectedWinTrophy, 1020);
assert.equal(ob.ProjectedLossTrophy, 995);
assert.equal(ob.Items.length, 2);
assert.equal(ob.Companions[0].Equipped, true);
assert.equal(ob.ShowCloak, false);
assert.deepEqual(ob.EnchantmentTiers, [0, 2, 0, 0, 0, 0, 5, 0, 11]);

// Roster sai token → hết hạn; đối thủ ngoài roster → không tìm thấy.
let r = await call(a, "startpvpbattle", { rosterToken: "bad", opponentUserId: b.uid });
assert.equal(r.success, false);
assert.equal(r.message, "Opponent roster expired.");
r = await call(a, "startpvpbattle", { rosterToken: fa.rosterToken, opponentUserId: "nobody" });
assert.equal(r.success, false);
assert.equal(r.message, "Opponent not found.");

// --- startPvPBattle: tiêu 1 vé free ---
const sa = await call(a, "startpvpbattle", { rosterToken: fa.rosterToken, opponentUserId: b.uid });
assert.equal(sa.success, true);
assert.ok(sa.battleToken);
assert.equal(sa.startLeagueIndex, 1);
assert.equal(sa.opponent.UserId, b.uid);
assert.equal(sa.tickets.freeRemaining, 4);

// Đã đánh B thì B rời roster.
r = await call(a, "startpvpbattle", { rosterToken: fa.rosterToken, opponentUserId: b.uid });
assert.equal(r.message, "Opponent not found.");

// --- reportPvPBattle: thắng +20, B (phòng thủ) −5, thưởng win league 1 ---
const ra = await call(a, "reportpvpbattle", { battleToken: sa.battleToken, won: true });
assert.equal(ra.success, true);
assert.equal(ra.won, true);
assert.equal(ra.forfeit, false);
assert.equal(ra.oldTrophy, 1000);
assert.equal(ra.newTrophy, 1020);
assert.equal(ra.trophyDelta, 20);
assert.equal(ra.opponentOldTrophy, 1000);
assert.equal(ra.opponentNewTrophy, 995);
assert.deepEqual(ra.rewards, oa.rewardTable[0].winRewards);

// Token dùng rồi → lỗi.
r = await call(a, "reportpvpbattle", { battleToken: sa.battleToken, won: true });
assert.equal(r.success, false);
assert.equal(r.message, "Missing battle token.");

// --- Thua: B đánh A ---
const fb = await call(b, "findpvpopponents", {});
const sb = await call(b, "startpvpbattle", { rosterToken: fb.rosterToken, opponentUserId: a.uid });
assert.equal(sb.success, true);
const rb = await call(b, "reportpvpbattle", { battleToken: sb.battleToken, won: false });
assert.equal(rb.won, false);
assert.equal(rb.oldTrophy, 995);
assert.ok(rb.trophyDelta <= -1);
assert.ok(rb.opponentNewTrophy > 1020);            // A phòng thủ thắng → cộng
assert.deepEqual(rb.rewards, oa.rewardTable[0].loseRewards);

// --- Vé ads: tối đa 4/ngày ---
for (let i = 1; i <= 4; i++) {
  const g = await call(a, "grantpvpadticket");
  assert.equal(g.success, true);
  assert.equal(g.tickets.adClaimedToday, i);
  assert.equal(g.tickets.adRemaining, i);
}
r = await call(a, "grantpvpadticket");
assert.equal(r.success, false);
assert.equal(r.message, "No more PvP ad tickets today.");

// Bot lấp roster + tiêu hết vé free rồi sang vé ads.
const seeded = await call(a, "seedpvpdummyplayers", { count: 12 });
assert.equal(seeded.added, 12);
let tickets = null;
for (let i = 0; i < 8; i++) {
  const f = await call(a, "findpvpopponents", {});
  assert.equal(f.opponents.length, 5);
  assert.ok(f.opponents.every((o) => o.UserId !== a.uid));
  const s = await call(a, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
  assert.equal(s.success, true, JSON.stringify(s));
  tickets = s.tickets;
  await call(a, "reportpvpbattle", { battleToken: s.battleToken, won: i % 2 === 0 });
}
assert.equal(tickets.freeRemaining, 0);
assert.equal(tickets.adRemaining, 0);
const f = await call(a, "findpvpopponents", {});
r = await call(a, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
assert.equal(r.success, false);
assert.equal(r.message, "No PvP tickets remaining.");

// --- Leaderboard world + country ---
const lw = await call(a, "getpvpleaderboard", { scope: "world" });
assert.equal(lw.success, true);
assert.equal(lw.scope, "world");
assert.equal(lw.topPlayers.length, 14);   // A + B + 12 bot
for (let i = 1; i < lw.topPlayers.length; i++) assert.ok(lw.topPlayers[i - 1].Trophy >= lw.topPlayers[i].Trophy);
const meRow = lw.topPlayers.find((e) => e.IsCurrentPlayer);
assert.ok(meRow);
assert.equal(lw.playerTrophy, meRow.Trophy);
assert.equal(lw.nearbyPlayers.length, 0);
const lc = await call(a, "getpvpleaderboard", { scope: "country" });
assert.equal(lc.scope, "country");
assert.ok(lc.topPlayers.every((e) => e.CountryCode === "VN"));

await call(a, "removepvpdummyplayers");
console.log("PvP emulator test PASS");
