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

// Đọc/sửa thẳng Firestore emulator (giả lập mùa đã kết thúc) — header "Bearer owner" bỏ qua rules.
const OWNER = { "Content-Type": "application/json", Authorization: "Bearer owner" };

async function patchDoc(path, strings) {
  const mask = Object.keys(strings).map((k) => "updateMask.fieldPaths=" + k).join("&");
  const fields = Object.fromEntries(Object.entries(strings).map(([k, v]) => [k, { stringValue: v }]));
  const r = await fetch(`${FS}/${path}?${mask}`, { method: "PATCH", headers: OWNER, body: JSON.stringify({ fields }) });
  assert.equal(r.status, 200, await r.text());
}

async function getDoc(path) {
  const r = await fetch(`${FS}/${path}`, { headers: OWNER });
  return r.status === 200 ? (await r.json()).fields : null;
}

// Wing (slot 7) + Cape (slot 6) đi chung Items; relic theo index GearSlotType; showCloak = User.ShowCloak gốc.
const snapshot = (name, power, showCloak = true) => ({
  server: PROJECT, playerName: name, power,
  items: [
    { Slot: 8, ItemId: "BasicBow", Rarity: 2, ItemLevel: 5, SubStats: [{ Type: 4, Value: 0.12 }] },
    { Slot: 7, ItemId: "3", Rarity: 4, ItemLevel: 2, SubStats: [] },
    { Slot: 6, ItemId: "5", Rarity: 3, ItemLevel: 7, SubStats: [{ Type: 1, Value: 0.05 }] },
  ],
  companions: [{ CompanionId: "Companion_1", CompanionLevel: 3, Equipped: true }],
  enchantmentTiers: [0, 2, 0, 0, 0, 0, 5, 0, 99],
  showCloak,
});

const ROW_KEYS = ["AvatarId", "PlayerName", "Position", "Power", "TotalDamagePoints", "UserId"];
const amount = (list, type) => list.find((r) => r.Type === type)?.Amount ?? 0;
const fight = async (user, poolId, own, team) => {
  const f = await call(user, "startbossrushfight", { poolId });
  assert.equal(f.success, true, JSON.stringify(f));
  return { start: f, result: await call(user, "reportbossrushdamage", { fightToken: f.fightToken, damageDealt: own, totalDamageDealt: team }) };
};

const isMonday = new Date().getUTCDay() === 1;
const [a, b, c, outsider] = [await signUp(), await signUp(), await signUp(), await signUp()];
console.log("users", a.uid, b.uid, c.uid);

// --- Chưa đăng nhập → bị chặn ---
const anon = await fetch(`${FN}/joinbossrush`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ data: {} }) });
assert.equal(anon.status, 401);

const ja = await call(a, "joinbossrush", snapshot("Alice", 50000000));
if (isMonday) {
  assert.equal(ja.inactive, true);
  console.log("Hôm nay Thứ 2 (UTC) → sự kiện nghỉ, dừng test luồng đánh.");
  process.exit(0);
}

// --- Sảnh mới: 1 người thật + 59 bot = 60, client không phân biệt được bot ---
assert.equal(ja.success, true, JSON.stringify(ja));
assert.equal(ja.tier, 1);
assert.equal(ja.bossNumber, 1);
assert.equal(ja.bossHP, 500000000);
assert.equal(ja.maxBossHP, 500000000);
assert.equal(ja.tickets.freeRemaining, 3);
assert.equal(ja.tickets.adRemaining, 3);
assert.equal(ja.players.length, 60);
for (const row of ja.players) assert.deepEqual(Object.keys(row).sort(), ROW_KEYS, "dòng sảnh không kèm bộ đồ, không có cờ bot");
assert.equal(new Set(ja.players.map((p) => p.PlayerName.toLowerCase())).size, 60, "tên không trùng");
assert.equal(new Set(ja.players.map((p) => p.UserId)).size, 60);
assert.ok(ja.players.every((p) => p.UserId.length === 28 && p.Power > 0), "id bot giống uid thật, có power");
assert.ok(ja.players.every((p) => p.TotalDamagePoints === 0), "sảnh chưa có ai đánh → bot chưa hoạt động");
assert.deepEqual(ja.players.map((p) => p.Position), Array.from({ length: 60 }, (_, i) => i + 1));
assert.equal(amount(ja.rewardTable.placementBrackets[0].rewards, 9), 100, "bảng thưởng Iron: hạng 1 = 100 Cloak");
const eventKey = ja.eventKey;
const poolId = ja.poolId;

// --- Người thật vào thêm chỗ trống, không thay chỗ bot ---
const jb = await call(b, "joinbossrush", snapshot("Bob", 80000000));
assert.equal(jb.poolId, poolId);
assert.equal(jb.players.length, 61);
const jc = await call(c, "joinbossrush", snapshot("Carol", 20000000));
assert.equal(jc.poolId, poolId);
assert.equal(jc.players.length, 62);

// Join lại → alreadyJoined, không tạo sảnh mới.
const ja2 = await call(a, "joinbossrush", snapshot("Alice", 55000000, false));
assert.equal(ja2.alreadyJoined, true);
assert.equal(ja2.poolId, poolId);
assert.equal(ja2.players.length, 62);
assert.equal(ja2.players.find((p) => p.UserId === a.uid).Power, 55000000);

// --- Hồ sơ (bấm avatar): bộ đồ chỉ tải khi xem, cho cả người thật lẫn bot ---
const pa = await call(b, "getbossrushprofile", { poolId, userId: a.uid });
assert.equal(pa.success, true);
assert.deepEqual(pa.player.Items.map((it) => it.Slot), [8, 7, 6]);
assert.deepEqual(pa.player.EnchantmentTiers, [0, 2, 0, 0, 0, 0, 5, 0, 11], "relic kẹp 0..11");
assert.equal(pa.player.ShowCloak, false);
const botRow = ja2.players.find((p) => ![a.uid, b.uid, c.uid].includes(p.UserId));
const pbot = await call(b, "getbossrushprofile", { poolId, userId: botRow.UserId });
assert.equal(pbot.success, true);
assert.equal(pbot.player.PlayerName, botRow.PlayerName);
assert.equal(pbot.player.Power, botRow.Power, "power trong hồ sơ = power ở bảng xếp hạng");
assert.ok(pbot.player.Items.length > 0);
assert.equal((await call(outsider, "getbossrushprofile", { poolId, userId: a.uid })).success, false, "người ngoài sảnh không xem được");

// --- Trận 1: A đánh 100M (cả đội 300M), đi cùng 7 người hỗ trợ có bộ đồ ---
const { start: f1, result: r1 } = await fight(a, poolId, 100000000, 300000000);
assert.equal(f1.tickets.freeRemaining, 2);
assert.equal(f1.allies.length, 7);
assert.ok(f1.allies.every((x) => x.UserId !== a.uid && Array.isArray(x.Items) && x.Items.length > 0 && !("IsBot" in x)));
assert.equal(r1.success, true);
assert.equal(r1.autoLoss, false);
assert.equal(r1.bossKilled, false);
assert.ok(r1.newBossHP <= 200000000 && r1.newBossHP > 150000000, "boss #1 còn khoảng 200M: " + r1.newBossHP);
assert.equal(r1.totalDamagePoints, 100000000, "bảng xếp hạng chỉ cộng damage của chính người đánh");
assert.equal(r1.newPosition, 1);
assert.equal(r1.players.length, 62);

// Token dùng lại → bị từ chối.
const replay = await call(a, "reportbossrushdamage", { fightToken: f1.fightToken, damageDealt: 1, totalDamageDealt: 1 });
assert.equal(replay.success, false);

// --- Trận 2: B đánh đội 1.2B → giết boss #1 + boss #2 (750M) ---
const { result: r2 } = await fight(b, poolId, 500000000, 1200000000);
assert.equal(r2.bossKilled, true);
assert.ok(r2.newBossNumber >= 3);
assert.equal(r2.players[0].UserId, b.uid);   // B nhiều damage hơn → hạng 1

// Điểm bot chỉ tăng theo thời gian; máu boss không hồi.
const p1 = await call(a, "getbossrushpool", { poolId });
const p2 = await call(a, "getbossrushpool", { poolId });
for (const row of p2.players) assert.ok(row.TotalDamagePoints >= p1.players.find((x) => x.UserId === row.UserId).TotalDamagePoints);
assert.ok(p2.bossNumber > p1.bossNumber || p2.bossHP <= p1.bossHP);
const stats = await getDoc(`bossRushStats/${eventKey}`);
assert.equal(stats.t1.mapValue.fields.n.integerValue, "2", "số liệu damage trung bình của league được ghi lại");

// --- Hết vé: dùng 3 free rồi 3 ads, lần thứ 7 bị chặn ---
for (let i = 0; i < 5; i++) await fight(a, poolId, 1, 1);
const f7 = await call(a, "startbossrushfight", { poolId });
assert.equal(f7.success, false);
assert.equal(f7.message, "No fights remaining.");

// --- Chưa hết mùa → chưa nhận được thưởng ---
const early = await call(a, "claimbossrushrewards", { poolId });
assert.equal(early.success, false);
assert.equal(early.expired, false);

// --- Giả lập hết mùa: lùi mùa của sảnh + của 3 người về một tuần cũ ---
const OLD = "2020-01-07";
await patchDoc(`bossRushPools/${poolId}`, { eventKey: OLD });
for (const u of [a, b, c]) await patchDoc(`bossRushPlayers/${u.uid}`, { lastJoinEventKey: OLD });
const ended = await call(a, "getbossrushpool", { poolId });
assert.equal(ended.concluding, true);
const jaEnd = await call(a, "joinbossrush", snapshot("Alice", 55000000));
assert.equal(jaEnd.hasUnclaimed, true);
assert.equal(jaEnd.unclaimedPoolId, poolId);

// B hạng 1, sảnh giết 2 boss: Guaranteed 160 Cloak / 480 Lootbox / 480 Bone / 2 + 2 key, Placement 100 Cloak + 100 Gem.
const cb = await call(b, "claimbossrushrewards", { poolId });
assert.equal(cb.success, true, JSON.stringify(cb));
assert.equal(cb.rank, 1);
assert.equal(cb.bossesKilled, 2);
assert.deepEqual([amount(cb.rewards, 9), amount(cb.rewards, 4), amount(cb.rewards, 0), amount(cb.rewards, 2), amount(cb.rewards, 3), amount(cb.rewards, 1)],
  [260, 480, 480, 2, 2, 100]);
assert.equal(cb.promoted, true, "Iron top 15 lên league");
assert.equal(cb.newTier, 2);

const ca = await call(a, "claimbossrushrewards", { poolId });
assert.equal(ca.rank, 2);
assert.deepEqual([amount(ca.rewards, 9), amount(ca.rewards, 1)], [240, 80]);
assert.equal(ca.newTier, 2);
const caAgain = await call(a, "claimbossrushrewards", { poolId });
assert.equal(caAgain.success, true);
assert.equal(caAgain.rewards.length, 0, "không nhận được lần 2");

// C vào sảnh nhưng không đánh trận nào → không có thưởng, không lên league, không bị hỏi nhận thưởng.
const cc = await call(c, "claimbossrushrewards", { poolId });
assert.equal(cc.rewards.length, 0);
assert.equal(cc.promoted, false);
assert.equal(cc.newTier, 1);

// --- Mùa hiện tại: B, A đã lên Bronze (boss 1B) → sảnh Bronze mới; C ở lại Iron ---
const jb2 = await call(b, "joinbossrush", snapshot("Bob", 80000000));
assert.equal(jb2.hasUnclaimed, false);
assert.equal(jb2.tier, 2);
assert.equal(jb2.maxBossHP >= 1000000000, true);
assert.notEqual(jb2.poolId, poolId);
assert.equal(jb2.players.length, 60);
const ja3 = await call(a, "joinbossrush", snapshot("Alice", 55000000));
assert.equal(ja3.poolId, jb2.poolId, "người mới vào sảnh cùng league còn chỗ");
assert.equal(ja3.players.length, 61);
const jc2 = await call(c, "joinbossrush", snapshot("Carol", 20000000));
assert.equal(jc2.hasUnclaimed, false);
assert.equal(jc2.tier, 1);
assert.equal(jc2.players.length, 60);

// --- Giữ chỗ đầu mùa: D, E đánh ở "mùa trước" → dựng lại mùa này phải xếp sẵn 2 người vào một sảnh + 58 bot ---
const [d, e] = [await signUp(), await signUp()];
const prevKey = new Date(Date.parse(eventKey + "T00:00:00Z") - 7 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10);
for (const [u, name] of [[d, "Dave"], [e, "Erin"]]) {
  const j = await call(u, "joinbossrush", snapshot(name, 30000000));
  await fight(u, j.poolId, 2000000, 16000000);
  await patchDoc(`bossRushPlayers/${u.uid}`, { lastFoughtEventKey: prevKey, lastJoinEventKey: prevKey, currentPoolId: "" });
}
const rebuilt = await call(a, "finalizebossrushnow", { rebuild: true });
assert.equal(rebuilt.rebuilt, true);
const reservedId = `${eventKey}_t1_1`;
const jd = await call(d, "joinbossrush", snapshot("Dave", 30000000));
assert.equal(jd.poolId, reservedId, "người còn hoạt động vào thẳng sảnh đã giữ chỗ");
assert.equal(jd.alreadyJoined, false);
assert.equal(jd.players.length, 60, "2 người thật + 58 bot");
assert.ok(jd.players.some((p) => p.UserId === e.uid), "E được giữ chỗ dù chưa bấm Join");
const je = await call(e, "joinbossrush", snapshot("Erin", 30000000));
assert.equal(je.poolId, reservedId);
assert.equal(je.players.length, 60);

console.log("BOSS RUSH EMULATOR TEST: ALL PASSED");
