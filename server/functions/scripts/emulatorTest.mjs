// Test tích hợp Boss Rush trên Firebase Emulator (chạy: firebase emulators:exec "node functions/scripts/emulatorTest.mjs").
// Giả lập client Unity: đăng nhập ẩn danh qua Auth emulator → gọi callable functions qua HTTP.
import assert from "node:assert/strict";

const PROJECT = process.env.GCLOUD_PROJECT || "demo-dungeonrush";
const AUTH = "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/accounts:signUp?key=emulator-key";
const FN = `http://127.0.0.1:5001/${PROJECT}/us-central1`;
const FS = `http://127.0.0.1:8085/v1/projects/${PROJECT}/databases/(default)/documents`;

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

// Sửa thẳng Firestore emulator (giả lập đợt đã kết thúc) — header "Bearer owner" bỏ qua rules.
async function patchPoolEventKey(poolId, eventKey) {
  const url = `${FS}/bossRushPools/${poolId}?updateMask.fieldPaths=eventKey`;
  const r = await fetch(url, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", Authorization: "Bearer owner" },
    body: JSON.stringify({ fields: { eventKey: { stringValue: eventKey } } }),
  });
  assert.equal(r.status, 200, await r.text());
}

const snapshot = (name, power) => ({
  server: PROJECT, playerName: name, power,
  items: [{ Slot: 8, ItemId: "BasicBow", Rarity: 2, ItemLevel: 5, SubStats: [{ Type: 4, Value: 0.12 }] }],
  companions: [{ CompanionId: "Companion_1", CompanionLevel: 3, Equipped: true }],
});

const isMonday = new Date().getUTCDay() === 1;
const a = await signUp();
const b = await signUp();
console.log("users", a.uid, b.uid);

// --- Chưa đăng nhập → bị chặn ---
const anon = await fetch(`${FN}/joinbossrush`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ data: {} }) });
assert.equal(anon.status, 401);

const ja = await call(a, "joinbossrush", snapshot("Alice", 1000));
if (isMonday) {
  assert.equal(ja.inactive, true);
  console.log("Hôm nay Thứ 2 (UTC) → sự kiện nghỉ, dừng test luồng đánh.");
  process.exit(0);
}

assert.equal(ja.success, true);
assert.equal(ja.tier, 1);
assert.equal(ja.bossNumber, 1);
assert.equal(ja.maxBossHP, 500000000);
assert.equal(ja.tickets.freeRemaining, 3);
assert.equal(ja.tickets.adRemaining, 3);
assert.equal(ja.players.length, 1);

// B vào cùng nhóm (cùng tier, còn chỗ).
const jb = await call(b, "joinbossrush", snapshot("Bob", 2000));
assert.equal(jb.poolId, ja.poolId);
assert.equal(jb.players.length, 2);

// Join lại → alreadyJoined, không tạo nhóm mới.
const ja2 = await call(a, "joinbossrush", snapshot("Alice", 1500));
assert.equal(ja2.alreadyJoined, true);
assert.equal(ja2.poolId, ja.poolId);

// Lấp bot (emulator = admin).
const seed = await call(a, "seedbossrushdummyplayers", { count: 8 });
assert.equal(seed.success, true);
const pool = await call(a, "getbossrushpool", { poolId: ja.poolId });
assert.equal(pool.players.length, 8);

// --- Trận 1: A đánh 100M (cả đội 300M) ---
const f1 = await call(a, "startbossrushfight", { poolId: ja.poolId });
assert.equal(f1.success, true);
assert.equal(f1.tickets.freeRemaining, 2);
const r1 = await call(a, "reportbossrushdamage", { fightToken: f1.fightToken, damageDealt: 100000000, totalDamageDealt: 300000000 });
assert.equal(r1.success, true);
assert.equal(r1.autoLoss, false);
assert.equal(r1.bossKilled, false);
assert.equal(r1.newBossHP, 200000000);
assert.equal(r1.totalDamagePoints, 100000000);
assert.equal(r1.newPosition, 1);

// Token dùng lại → bị từ chối.
const replay = await call(a, "reportbossrushdamage", { fightToken: f1.fightToken, damageDealt: 1, totalDamageDealt: 1 });
assert.equal(replay.success, false);

// --- Trận 2: B đánh đội 1.2B → giết boss #1 (200M còn lại) + boss #2 (750M) → boss #3 còn 1B - 250M ---
const f2 = await call(b, "startbossrushfight", { poolId: ja.poolId });
const r2 = await call(b, "reportbossrushdamage", { fightToken: f2.fightToken, damageDealt: 500000000, totalDamageDealt: 1200000000 });
assert.equal(r2.bossKilled, true);
assert.equal(r2.newBossNumber, 3);
assert.equal(r2.newMaxBossHP, 1000000000);
assert.equal(r2.newBossHP, 750000000);
assert.equal(r2.players[0].UserId, b.uid);   // B nhiều damage hơn → hạng 1

// --- Hết vé: dùng 3 free rồi 3 ads, lần thứ 7 bị chặn ---
for (let i = 0; i < 4; i++) {
  const f = await call(a, "startbossrushfight", { poolId: ja.poolId });
  assert.equal(f.success, true, "fight " + i);
  await call(a, "reportbossrushdamage", { fightToken: f.fightToken, damageDealt: 1, totalDamageDealt: 1 });
}
const out = await call(a, "startbossrushfight", { poolId: ja.poolId });
assert.equal(out.success, true);       // vé ads thứ 3
assert.equal(out.tickets.freeRemaining, 0);
assert.equal(out.tickets.adRemaining, 0);
assert.equal(out.tickets.adClaimedToday, 3);
const none = await call(a, "startbossrushfight", { poolId: ja.poolId });
assert.equal(none.success, false);
assert.equal(none.message, "No fights remaining.");

// --- Claim khi đợt còn chạy → chưa cho ---
const early = await call(a, "claimbossrushrewards", { poolId: ja.poolId });
assert.equal(early.success, false);

// --- Giả lập đợt đã kết thúc → join báo hasUnclaimed, claim nhận thưởng mặc định (hạng 1-100 → 1000 Bone) ---
await patchPoolEventKey(ja.poolId, "2000-01-04");
const jEnded = await call(a, "joinbossrush", snapshot("Alice", 1500));
assert.equal(jEnded.hasUnclaimed, true);
assert.equal(jEnded.unclaimedPoolId, ja.poolId);
const claim = await call(a, "claimbossrushrewards", { poolId: ja.poolId });
assert.equal(claim.success, true);
assert.equal(claim.rank, 2);
assert.equal(claim.bossesKilled, 2);
assert.deepEqual(claim.rewards, [{ Type: 0, Amount: 1000 }]);
assert.equal(claim.newTier, 1);
const claim2 = await call(a, "claimbossrushrewards", { poolId: ja.poolId });
assert.deepEqual(claim2.rewards, []);   // không nhận 2 lần

// Sau khi nhận → join đợt mới tạo nhóm mới.
const jNew = await call(a, "joinbossrush", snapshot("Alice", 1500));
assert.equal(jNew.hasUnclaimed, false);
assert.notEqual(jNew.poolId, ja.poolId);
assert.equal(jNew.bossNumber, 1);

console.log("BOSS RUSH EMULATOR TEST: ALL PASSED");
