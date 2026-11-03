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

// --- openPvP: người mới Iron, 0 cúp, 5 vé, bảng thưởng 10 league ---
const oa = await call(a, "openpvp", snapshot("Alice", 1000));
assert.equal(oa.success, true);
assert.equal(oa.trophy, 0);
assert.equal(oa.leagueIndex, 1);
assert.equal(oa.hasName, true);
assert.deepEqual([oa.tickets.remaining, oa.tickets.dailyTickets], [5, 5]);
assert.equal(oa.rewardTable.length, 10);
assert.equal(typeof oa.rewardTable[0].Bands[0].Rewards[0].Type, "number");
assert.equal(oa.pendingSeasonResult, null);
const b0 = await call(b, "openpvp", snapshot("Bob", 2000));

// Thứ Hai nghỉ: hết phần cần có mùa thì dừng ở đây.
if (!oa.season.isActive) {
  assert.equal(oa.leaderboard.length, 0);
  assert.equal((await call(a, "findpvpopponents", {})).message, "Season ended.");
  console.log("PvP emulator test PASS (Thứ Hai nghỉ, chỉ kiểm phần mở popup)");
  process.exit(0);
}

// Nhóm đầu tiên có 30 người (bù bot), 2 người cùng league vào cùng nhóm.
assert.equal(oa.leaderboard.length, 30);
assert.equal(b0.leaderboard.length, 30 + 1);
assert.equal(oa.promotion.promoteCount, 12);
assert.equal(oa.promotion.demoteCount, 0);

// --- findPvPOpponents: tối đa 5 người, không có mình, phát +5 đến +1 ---
const fa = await call(a, "findpvpopponents", { server: PROJECT });
assert.equal(fa.success, true);
assert.ok(fa.rosterToken);
assert.ok(fa.rosterExpiresAt > Date.now() / 1000);
assert.equal(fa.opponents.length, 5);
assert.ok(!fa.opponents.some((o) => o.UserId === a.uid));
assert.deepEqual(fa.opponents.map((o) => o.StarsOnWin), [5, 4, 3, 2, 1]);

// --- startPvPBattle: lỗi roster, rồi tiêu 1 vé ---
let r = await call(a, "startpvpbattle", { rosterToken: "bad", opponentUserId: fa.opponents[0].UserId });
assert.equal(r.message, "Opponent roster expired.");
const target = fa.opponents[0];
const sa = await call(a, "startpvpbattle", { rosterToken: fa.rosterToken, opponentUserId: target.UserId });
assert.equal(sa.success, true);
assert.equal(sa.starsOnWin, target.StarsOnWin);
assert.equal(sa.tickets.remaining, 4);
assert.ok(sa.opponent.Items !== undefined);

// --- reportPvPBattle: thắng nhận đúng số cúp đã hiện ---
const ra = await call(a, "reportpvpbattle", { battleToken: sa.battleToken, won: true });
assert.deepEqual([ra.won, ra.oldTrophy, ra.newTrophy, ra.trophyDelta], [true, 0, target.StarsOnWin, target.StarsOnWin]);
r = await call(a, "reportpvpbattle", { battleToken: sa.battleToken, won: true });
assert.equal(r.message, "Missing battle token.");

// --- Thua không mất cúp ---
const lose = await call(a, "startpvpbattle", { rosterToken: fa.rosterToken, opponentUserId: fa.opponents[1].UserId });
const rl = await call(a, "reportpvpbattle", { battleToken: lose.battleToken, won: false });
assert.deepEqual([rl.won, rl.trophyDelta, rl.newTrophy], [false, 0, target.StarsOnWin]);

// --- Mở lại: bảng xếp hạng có cúp mới ---
const reopen = await call(a, "openpvp", snapshot("Alice", 1000));
assert.equal(reopen.trophy, target.StarsOnWin);
assert.equal(reopen.leaderboard.find((e) => e.IsCurrentPlayer).Trophy, target.StarsOnWin);

// --- Hết vé ---
for (let i = 0; i < 3; i++) {
  const f = await call(a, "findpvpopponents", {});
  const s = await call(a, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
  assert.equal(s.success, true, s.message);
  await call(a, "reportpvpbattle", { battleToken: s.battleToken, won: false });
}
const f = await call(a, "findpvpopponents", {});
r = await call(a, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
assert.equal(r.success, false);
assert.equal(r.message, "No PvP tickets remaining.");

await call(admin, "removeallpvpplayers");
console.log("PvP emulator test PASS");
