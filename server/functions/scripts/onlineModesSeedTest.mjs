// Test các mode online (PvP + Clan + Clan War) trên data seed 1000 người chơi / 10 clan (chạy: npm run test:seed).
// Gọi THẲNG handler của server (lib/*.js qua .run) trên Firestore emulator, không qua HTTP/Auth → đóng vai được bất kỳ uid nào
// (kể cả leader clan seed) và GIẢ được đồng hồ để chạy trọn 1 tuần Clan War (Thứ Ba → Thứ Hai → ghép tuần kế).
// Tầng HTTP + xác thực đã có pvpEmulatorTest.mjs / clanEmulatorTest.mjs kiểm.
// Test tự dựng lại data seed lúc đầu và lúc cuối; tài khoản thật trong emulator không bị đụng (uid test bắt đầu bằng "test_").
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mock } from "node:test";
import {
  CLAN_PREFIX, CLAN_SPECS, PLAYER_PREFIX, PROJECT, clear, db, deleteByIdPrefix, requireEmulator, seedPlayersAndClans, seedWars,
} from "./seedTestData.mjs";

const require = createRequire(import.meta.url);
const handlers = { ...require("../lib/pvp.js"), ...require("../lib/clan.js"), ...require("../lib/clanWar.js") };
const { matchClanWars } = require("../lib/clanWar.js");
const cw = require("../lib/clanWarConfig.js");
const pvp = require("../lib/pvpConfig.js");
const schedule = require("../lib/schedule.js");
const { FieldValue } = require("firebase-admin/firestore");

const TEST_PREFIX = "test_";
const HOUR = 3600 * 1000;
const DAY = 24 * HOUR;
const REAL_NOW = Date.now();
const REAL_WEEK_START = cw.weekStartOf(new Date(REAL_NOW)).getTime();
const REAL_WEEK_ID = cw.weekIdOf(new Date(REAL_NOW));

// ===== Khung test =====

const results = [];

function section(title) {
  console.log("\n" + title);
}

async function test(id, name, fn) {
  try {
    await fn();
    results.push({ id, name, ok: true });
    console.log(`  PASS  ${id}  ${name}`);
  } catch (e) {
    results.push({ id, name, ok: false });
    console.log(`  FAIL  ${id}  ${name}\n          ${String(e.message).split("\n").slice(0, 6).join("\n          ")}`);
  }
}

const call = (uid, name, data = {}) => handlers[name].run({ auth: { uid }, data });
const read = async (collection, id) => (await db.collection(collection).doc(id).get()).data();
const patch = (collection, id, data) => db.collection(collection).doc(id).update(data);
const sum = (list) => list.reduce((s, v) => s + v, 0);
const close = (a, b) => Math.abs(a - b) <= Math.max(Math.abs(a), Math.abs(b)) * 1e-9;

// Đồng hồ giả cho cả tiến trình (server đọc giờ bằng new Date()/Date.now()). 0 = về giờ thật.
function setClock(ms) {
  mock.timers.reset();
  if (ms) mock.timers.enable({ apis: ["Date"], now: ms });
}

async function quiet(fn) {
  const log = console.log;
  console.log = () => {};
  try {
    return await fn();
  } finally {
    console.log = log;
  }
}

// Xoá mọi thứ test tạo ra: người chơi test_, clan do người test lập, war của các tuần giả (sau tuần thật) + index.
async function cleanupTestData() {
  const clans = await db.collection("clans").get();
  for (const d of clans.docs) {
    const c = d.data();
    if (c.clanId.startsWith(CLAN_PREFIX) || !Object.keys(c.members).some((u) => u.startsWith(TEST_PREFIX))) continue;
    await db.collection("clanNames").doc(c.nameLower).delete();
    await d.ref.delete();
  }
  const wars = await db.collection("clanWars").where("weekId", ">", REAL_WEEK_ID).get();
  for (const d of wars.docs) {
    for (const id of d.data().clanIds) await db.collection("clanWarIndex").doc(d.data().weekId + "_" + id).delete();
    await db.recursiveDelete(d.ref);
  }
  await deleteByIdPrefix("pvpPlayers", TEST_PREFIX);
  await deleteByIdPrefix("clanPlayers", TEST_PREFIX);
}

async function reseed() {
  await quiet(async () => {
    await cleanupTestData();
    await clear();
    await seedPlayersAndClans();
  });
}

const clanOf = (i) => CLAN_PREFIX + String(i).padStart(2, "0");
const byRole = (clan, role) => Object.values(clan.members).filter((m) => m.role === role).sort((a, b) => b.power - a.power);
const join = (name, power) => ({ server: PROJECT, playerName: name, power, avatarId: 3 });

await requireEmulator();

// Phần Clan War giả lập "tuần sau" và gọi ghép cặp thật → sẽ chốt sớm war của clan thật. Có clan thật thì dừng.
const realClans = (await db.collection("clans").get()).docs.map((d) => d.data())
  .filter((c) => !c.clanId.startsWith(CLAN_PREFIX) && !Object.keys(c.members).some((u) => u.startsWith(TEST_PREFIX)));
if (realClans.length > 0) {
  console.error(`Emulator đang có clan thật (${realClans.map((c) => c.clanName).join(", ")}). Test này sẽ làm hỏng war của clan đó → dừng.`);
  console.error("Rời/giải tán clan đó (hoặc bật lại emulator) rồi chạy lại.");
  process.exit(1);
}

try {
  await reseed();

  // =====================================================================================================================
  section("A. Data seed — toàn vẹn");
  // =====================================================================================================================

  const seedPlayers = (await db.collection("pvpPlayers").get()).docs.map((d) => d.data()).filter((p) => p.uid.startsWith(PLAYER_PREFIX));
  const seedClans = (await db.collection("clans").get()).docs.map((d) => d.data()).filter((c) => c.clanId.startsWith(CLAN_PREFIX));
  const seedClanPlayers = (await db.collection("clanPlayers").get()).docs.map((d) => d.data()).filter((p) => p.uid.startsWith(PLAYER_PREFIX));
  const playerById = new Map(seedPlayers.map((p) => [p.uid, p]));

  await test("A1", "1000 người chơi: tên không trùng, có power, đồ không trùng slot, có pet ra trận, vé đủ 5", () => {
    assert.equal(seedPlayers.length, 1000);
    assert.equal(new Set(seedPlayers.map((p) => p.playerName.toLowerCase())).size, 1000);
    for (const p of seedPlayers) {
      assert.ok(p.playerName.length > 0 && p.playerName.length <= 24, p.uid + " tên");
      assert.ok(p.power > 0, p.uid + " power");
      assert.ok(p.items.length >= 1, p.uid + " không có đồ");
      assert.equal(new Set(p.items.map((it) => it.Slot)).size, p.items.length, p.uid + " trùng slot đồ");
      assert.ok(p.companions.some((c) => c.Equipped), p.uid + " không có pet ra trận");
      assert.equal(p.wins + p.losses, p.attacksUsed, p.uid + " thắng + thua ≠ số trận");
      assert.ok(p.trophy >= 0, p.uid + " trophy");
      assert.equal(p.tickets.remaining, pvp.DAILY_TICKETS);
      assert.equal(p.roster, null);
      assert.equal(p.activeBattle, null);
    }
  });

  await test("A2", "Cả 10 league đều có người; người mạnh nhìn chung ở league cao hơn", () => {
    for (let league = 1; league <= pvp.LEAGUE_NAMES.length; league++) {
      assert.ok(seedPlayers.some((p) => p.league === league), "league trống: " + pvp.LEAGUE_NAMES[league - 1]);
    }
    const sorted = [...seedPlayers].sort((a, b) => a.power - b.power);
    const avg = (list) => sum(list.map((p) => p.league)) / list.length;
    assert.ok(avg(sorted.slice(900)) > avg(sorted.slice(0, 100)) + 2);
  });

  await test("A3", "10 clan đúng bảng: số người, tổng power, 1 leader, số captain, tier, cách vào, đơn chờ, tên đã giữ chỗ", async () => {
    assert.equal(seedClans.length, CLAN_SPECS.length);
    for (let i = 0; i < CLAN_SPECS.length; i++) {
      const [name, memberCount, joinSetting, tierPoints, requestCount] = CLAN_SPECS[i];
      const c = seedClans.find((x) => x.clanId === clanOf(i + 1));
      assert.ok(c, "thiếu clan " + name);
      const members = Object.values(c.members);
      assert.equal(c.clanName, name);
      assert.equal(members.length, memberCount, name + " số thành viên");
      assert.equal(c.memberCount, memberCount);
      assert.ok(close(c.totalPower, sum(members.map((m) => m.power))), name + " totalPower");
      assert.equal(byRole(c, "leader").length, 1, name + " leader");
      assert.equal(byRole(c, "captain").length, Math.min(cw.CLAN_MAX_CAPTAINS, Math.floor(memberCount / 4)), name + " captain");
      assert.equal(c.tierPoints, tierPoints);
      assert.equal(c.clanTier, cw.tierOf(tierPoints));
      assert.equal(c.joinSetting, joinSetting);
      assert.equal(Object.keys(c.requests).length, requestCount, name + " đơn chờ");
      assert.equal(c.server, PROJECT);
      assert.equal(c.isBot, false);
      assert.ok(c.bannerBackgroundTypeId < cw.BANNER_BG_TYPES && c.bannerImageTypeId < cw.BANNER_IMAGE_TYPES);
      assert.equal((await read("clanNames", c.nameLower))?.clanId, c.clanId, name + " clanNames");
    }
  });

  await test("A4", "clanPlayers khớp clan: không ai ở 2 clan, role đúng, power/tên khớp hồ sơ PvP, người xin vào chưa là thành viên", () => {
    const memberIds = seedClans.flatMap((c) => Object.keys(c.members));
    assert.equal(new Set(memberIds).size, memberIds.length, "có người ở 2 clan");
    const inClan = seedClanPlayers.filter((p) => p.clanId);
    assert.equal(inClan.length, memberIds.length);
    for (const p of inClan) {
      const m = seedClans.find((c) => c.clanId === p.clanId).members[p.uid];
      assert.ok(m, p.uid + " không có trong members");
      assert.equal(m.role, p.role);
      assert.equal(m.power, playerById.get(p.uid).power);
      assert.equal(m.playerName, playerById.get(p.uid).playerName);
    }
    const requesters = seedClanPlayers.filter((p) => !p.clanId);
    assert.equal(requesters.length, sum(CLAN_SPECS.map((s) => s[4])));
    for (const p of requesters) {
      assert.equal(p.requestedClanIds.length, 1);
      assert.ok(seedClans.find((c) => c.clanId === p.requestedClanIds[0]).requests[p.uid], p.uid + " thiếu đơn ở clan");
      assert.ok(!memberIds.includes(p.uid));
    }
  });

  // =====================================================================================================================
  section("B. PvP Arena");
  // =====================================================================================================================

  const A = "test_pvp_a";
  const B = "test_pvp_b";
  const source = playerById.get(PLAYER_PREFIX + "0500");
  const snapshotOf = (p, name, country) => ({
    server: PROJECT, playerName: name, countryCode: country, avatarId: p.avatarId, power: p.power, contentVersion: "1.0.0",
    items: p.items, companions: p.companions, enchantmentTiers: p.enchantmentTiers, showCloak: p.showCloak,
  });
  // Đồng hồ giả: Thứ Tư tuần này (mùa đang mở) và Thứ Ba tuần sau (mùa mới, nhóm cũ phải chốt).
  const SEASON_START = schedule.getEventStart(new Date(REAL_NOW)).getTime();
  const inSeason = SEASON_START + DAY + 12 * HOUR;
  const nextSeason = SEASON_START + 7 * DAY + HOUR;
  const seasonKey = new Date(SEASON_START).toISOString().substring(0, 10);
  const open = (uid, name = "PvpTester") => call(uid, "openpvp", snapshotOf(source, name, "US"));
  const group = async (uid) => read("pvpGroups", (await read("pvpPlayers", uid)).groupId);
  const fight = async (uid, won, pick = (list) => list[0]) => {
    const f = await call(uid, "findpvpopponents", { server: PROJECT });
    const o = pick(f.opponents);
    const s = await call(uid, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: o.UserId });
    assert.equal(s.success, true, s.message);
    const r = await call(uid, "reportpvpbattle", { battleToken: s.battleToken, won });
    return { f, o, s, r };
  };

  setClock(inSeason);

  await test("B1", "Người mới mở PvP: Iron, 0 cúp, 5 vé, thưởng 10 league, vào đúng nhóm của mùa", async () => {
    const r = await open(A, "PvpTesterA");
    assert.equal(r.success, true);
    assert.equal(r.trophy, 0);
    assert.equal(r.leagueIndex, 1);
    assert.deepEqual(r.tickets, { remaining: 5, dailyTickets: 5, dayKey: r.tickets.dayKey });
    assert.equal(r.rewardTable.length, 10);
    assert.deepEqual(r.rewardTable[0].Bands[0].Rewards.map((x) => x.Amount), [179, 716, 1074, 537]);
    assert.equal(r.season.key, seasonKey);
    assert.equal(r.season.isActive, true);
    const saved = await read("pvpPlayers", A);
    assert.equal(saved.groupId, seasonKey + "_L1_1");
    assert.equal(saved.seasonKey, seasonKey);
    assert.ok(saved.items.length > 0, "snapshot đồ phải lưu lại");
    await open(B, "PvpTesterB");
  });

  await test("B2", "Bảng xếp hạng: cúp giảm dần, cùng cúp thì sức mạnh giảm dần, có đúng dòng của mình và vạch lên", async () => {
    const r = await open(A, "PvpTesterA");
    assert.ok(r.leaderboard.length >= 80 && r.leaderboard.length <= 100);
    r.leaderboard.forEach((e, i) => {
      assert.equal(e.Rank, i + 1);
      if (i === 0) return;
      const p = r.leaderboard[i - 1];
      assert.ok(p.Trophy > e.Trophy || (p.Trophy === e.Trophy && p.Power >= e.Power), `hạng ${e.Rank} sai thứ tự`);
    });
    const me = r.leaderboard.filter((e) => e.IsCurrentPlayer);
    assert.equal(me.length, 1);
    assert.equal(me[0].UserId, A);
    assert.equal(r.playerRank, me[0].Rank);
    assert.equal(r.promotion.promoteCount, Math.ceil(r.leaderboard.length * 0.4));
    assert.equal(r.promotion.demoteCount, 0);
  });

  await test("B3", "Chọn đối thủ: tối đa 5 người trong nhóm, không có mình, theo cúp gần mình, phát +5 đến +1 theo cúp giảm dần", async () => {
    await patch("pvpPlayers", B, { trophy: 12 });
    await patch("pvpGroups", (await read("pvpPlayers", B)).groupId, { [`players.${B}.Trophy`]: 12 });
    const f = await call(B, "findpvpopponents", { server: PROJECT });
    assert.equal(f.success, true);
    assert.equal(f.opponents.length, 5);
    assert.ok(!f.opponents.some((o) => o.UserId === B));
    const g = await group(B);
    for (const o of f.opponents) assert.ok(g.players[o.UserId] || g.bots.some((b) => b.id === o.UserId), o.PlayerName + " ngoài nhóm");
    assert.deepEqual(f.opponents.map((o) => o.StarsOnWin), [5, 4, 3, 2, 1]);
    f.opponents.forEach((o, i) => {
      assert.equal(o.ProjectedWinTrophy, 12 + o.StarsOnWin);
      assert.equal(o.ProjectedLossTrophy, 12);
      assert.ok(o.Items.length > 0, o.PlayerName + " thiếu đồ để đánh");
      if (i > 0) assert.ok(f.opponents[i - 1].Trophy >= o.Trophy);
    });
    assert.ok(f.opponents.some((o) => o.Trophy > 12) && f.opponents.some((o) => o.Trophy < 12), "phải có cả trên lẫn dưới cúp mình");
  });

  await test("B4", "Thắng: nhận đúng số cúp đã hiện, nhóm cập nhật, người bị đánh không đổi cúp", async () => {
    const before = await read("pvpPlayers", A);
    const { o, s, r } = await fight(A, true, (list) => list.find((x) => x.UserId.startsWith(PLAYER_PREFIX)) ?? list[0]);
    assert.equal(s.starsOnWin, o.StarsOnWin);
    assert.equal(r.trophyDelta, o.StarsOnWin);
    assert.equal(r.newTrophy, before.trophy + o.StarsOnWin);
    assert.equal(r.seasonEnded, false);
    assert.equal((await read("pvpPlayers", A)).trophy, r.newTrophy);
    const entry = (await group(A)).players[A];
    assert.equal(entry.Trophy, r.newTrophy);
    assert.equal(entry.Fights, 1);
    if (o.UserId.startsWith(PLAYER_PREFIX)) assert.equal((await read("pvpPlayers", o.UserId)).trophy, o.Trophy);
  });

  await test("B5", "Thua: không mất cúp, vẫn tính 1 trận, vẫn tốn vé; nộp token cũ bị từ chối", async () => {
    const before = await read("pvpPlayers", A);
    const { s, r } = await fight(A, false);
    assert.equal(r.won, false);
    assert.equal(r.trophyDelta, 0);
    assert.equal(r.newTrophy, before.trophy);
    assert.equal(r.tickets.remaining, before.tickets.remaining - 1);
    assert.equal((await group(A)).players[A].Fights, 2);
    assert.equal((await call(A, "reportpvpbattle", { battleToken: s.battleToken, won: true })).message, "Missing battle token.");
  });

  await test("B6", "Nộp quá hạn: xử thua, không được cúp", async () => {
    const f = await call(A, "findpvpopponents", {});
    const s = await call(A, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
    await patch("pvpPlayers", A, { "activeBattle.expiresAt": 1 });
    const before = (await read("pvpPlayers", A)).trophy;
    const r = await call(A, "reportpvpbattle", { battleToken: s.battleToken, won: true });
    assert.deepEqual([r.won, r.forfeit, r.trophyDelta, r.newTrophy], [false, true, 0, before]);
  });

  await test("B7", "Roster: token sai, người ngoài danh sách, đánh lại cùng người, roster hết hạn đều bị từ chối", async () => {
    const f = await call(B, "findpvpopponents", {});
    const o = f.opponents[0];
    assert.equal((await call(B, "startpvpbattle", { rosterToken: "bad", opponentUserId: o.UserId })).message, "Opponent roster expired.");
    assert.equal((await call(B, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: "nobody" })).message, "Opponent not found.");
    const s = await call(B, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: o.UserId });
    assert.equal(s.success, true);
    await call(B, "reportpvpbattle", { battleToken: s.battleToken, won: false });
    assert.equal((await call(B, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: o.UserId })).message, "Opponent not found.");
    await patch("pvpPlayers", B, { "roster.expiresAt": 1 });
    assert.equal((await call(B, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[1].UserId })).message, "Opponent roster expired.");
  });

  await test("B8", "Hết 5 vé không đánh được nữa; sang ngày UTC mới hồi đủ 5 vé; không còn endpoint vé quảng cáo", async () => {
    await patch("pvpPlayers", A, { "tickets.remaining": 0 });
    const f = await call(A, "findpvpopponents", {});
    const s = await call(A, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
    assert.equal(s.message, "No PvP tickets remaining.");
    setClock(inSeason + DAY);
    assert.equal((await open(A, "PvpTesterA")).tickets.remaining, 5);
    setClock(inSeason);
    assert.equal(handlers.grantpvpadticket, undefined);
    assert.equal(handlers.getpvpleaderboard, undefined);
  });

  await test("B9", "Đổi tên/sức mạnh: nhóm cập nhật, cúp và số trận giữ nguyên", async () => {
    const before = (await group(B)).players[B];
    await call(B, "openpvp", { ...snapshotOf(source, "PvpRenamed", "US"), power: 123456 });
    const after = (await group(B)).players[B];
    assert.deepEqual([after.PlayerName, after.Power, after.Trophy, after.Fights], ["PvpRenamed", 123456, before.Trophy, before.Fights]);
  });

  await test("B10", "Nhóm đầy 100 người: người mới sang nhóm tiếp theo", async () => {
    const id = seasonKey + "_L1_1";
    const g = await read("pvpGroups", id);
    const players = { ...g.players };
    const entry = players[A];
    for (let i = 0; Object.keys(players).length + g.bots.length < 100; i++) players["fill_" + i] = { ...entry, UserId: "fill_" + i };
    await db.collection("pvpGroups").doc(id).update({ players });
    const NEW = TEST_PREFIX + "pvp_full";
    await open(NEW, "PvpTesterFull");
    assert.notEqual((await read("pvpPlayers", NEW)).groupId, id);
    await db.collection("pvpGroups").doc(id).set(g);
  });

  await test("B11", "Nhóm mới của league trống: bù bot đủ 30 người, không ai trùng tên, bot đánh được", async () => {
    const C = TEST_PREFIX + "pvp_gold";
    await open(C, "PvpTesterGold");
    const old = await read("pvpPlayers", C);
    await db.collection("pvpGroups").doc(old.groupId).update({ [`players.${C}`]: FieldValue.delete() });
    await patch("pvpPlayers", C, { league: 10, seasonKey: "", groupId: "" });
    await db.collection("pvpGroups").doc(seasonKey + "_L10_1").delete();
    const r = await open(C, "PvpTesterGold");
    assert.equal(r.leaderboard.length, 30);
    assert.equal(new Set(r.leaderboard.map((e) => e.PlayerName.toLowerCase())).size, 30);
    assert.equal(r.promotion.promoteCount, 0);
    assert.equal(r.promotion.demoteCount, 7);
    const { o, r: fought } = await fight(C, true);
    assert.equal(o.IsBot, true);
    assert.ok(o.Items.length > 0);
    assert.equal(fought.trophyDelta, o.StarsOnWin);
  });

  await test("B12", "Hết mùa: Thứ Hai không đánh được, có kết quả + thưởng theo league cũ, Claim một lần rồi hết", async () => {
    const before = await read("pvpPlayers", A);
    const beforeGroup = await group(A);
    setClock(SEASON_START + 6 * DAY + HOUR);
    const mon = await open(A, "PvpTesterA");
    assert.equal(mon.season.isActive, false);
    assert.equal(mon.season.key, "");
    assert.equal(mon.leaderboard.length, 0);
    assert.equal(mon.trophy, 0);
    const p = mon.pendingSeasonResult;
    assert.ok(p, "phải có kết quả mùa");
    assert.equal(p.seasonKey, seasonKey);
    assert.equal(p.leagueIndex, 1);
    assert.equal(p.trophy, before.trophy);
    assert.ok(p.finalRank >= 1 && p.finalRank <= p.memberCount);
    assert.deepEqual(p.rewards, pvp.seasonRewards(1, p.finalRank));
    assert.equal((await read("pvpGroups", beforeGroup.groupId)).isFinalized, true);
    assert.equal((await call(A, "findpvpopponents", {})).message, "Season ended.");

    const c = await call(A, "claimpvpseason", {});
    assert.deepEqual(c.rewards, p.rewards);
    assert.equal((await call(A, "claimpvpseason", {})).success, false);
    assert.equal((await open(A, "PvpTesterA")).pendingSeasonResult, null);
  });

  await test("B13", "Mùa mới: vào nhóm mới theo league mới, 0 cúp; trận bắt đầu mùa cũ nộp sang mùa mới không tính cúp", async () => {
    setClock(inSeason);
    const f = await call(B, "findpvpopponents", {});
    const s = await call(B, "startpvpbattle", { rosterToken: f.rosterToken, opponentUserId: f.opponents[0].UserId });
    assert.equal(s.success, true, s.message);
    const trophy = (await read("pvpPlayers", B)).trophy;

    setClock(nextSeason);
    await patch("pvpPlayers", B, { "activeBattle.expiresAt": Math.floor(nextSeason / 1000) + 100 });
    const late = await call(B, "reportpvpbattle", { battleToken: s.battleToken, won: true });
    assert.deepEqual([late.seasonEnded, late.trophyDelta, late.newTrophy], [true, 0, trophy]);

    const r = await open(B, "PvpRenamed");
    const nextKey = new Date(SEASON_START + 7 * DAY).toISOString().substring(0, 10);
    assert.equal(r.season.key, nextKey);
    assert.equal(r.trophy, 0);
    assert.ok(r.pendingSeasonResult, "kết quả mùa cũ");
    assert.equal(r.leagueIndex, r.pendingSeasonResult.newLeagueIndex);
    assert.ok((await read("pvpPlayers", B)).groupId.startsWith(nextKey + "_L" + r.leagueIndex + "_"));
    setClock(inSeason);
  });

  await test("B14", "Dữ liệu hệ Elo cũ (1000 trophy, chưa có league): mở PvP về Iron, 0 cúp, vé kiểu mới", async () => {
    const OLD = TEST_PREFIX + "pvp_old";
    await db.collection("pvpPlayers").doc(OLD).set({
      uid: OLD, playerName: "OldElo", trophy: 2500, items: [], companions: [], enchantmentTiers: [], isBot: false,
      tickets: { freeRemaining: 3, adRemaining: 1, adClaimedToday: 1, maxAdPerDay: 4, dailyFreeTickets: 5, dayKey: "2020-01-01" },
    });
    const r = await open(OLD, "OldElo");
    assert.deepEqual([r.success, r.leagueIndex, r.trophy, r.tickets.remaining], [true, 1, 0, 5]);
  });

  setClock(null);

  // =====================================================================================================================
  section("C. Clan");
  // =====================================================================================================================

  const OUTSIDER = "test_clan_a";
  const REQUESTER = "test_clan_b";
  const clan = (i) => read("clans", clanOf(i));

  await test("C1", "Tìm clan không gõ tên: đủ 10 clan, mạnh nhất lên đầu, không kèm danh sách thành viên", async () => {
    const r = await call(OUTSIDER, "searchclans", { server: PROJECT, clanName: "", hideApprovalOnly: false });
    assert.equal(r.clans.length, 10);
    r.clans.forEach((c, i) => {
      assert.equal(c.members.length, 0);
      if (i > 0) assert.ok(r.clans[i - 1].totalPower >= c.totalPower);
    });
  });

  await test("C2", "Tìm clan: theo đầu tên (không phân biệt hoa thường), lọc số thành viên, ẩn clan cần duyệt, server khác", async () => {
    const search = async (data) => (await call(OUTSIDER, "searchclans", { server: PROJECT, ...data })).clans.map((c) => c.clanName).sort();
    assert.deepEqual(await search({ clanName: "IRON" }), ["IronWolves"]);
    assert.deepEqual(await search({ clanName: "zzz" }), []);
    assert.deepEqual(await search({ minMemberCount: 45 }), ["DragonSlayers", "IronWolves", "VietLegends"]);
    assert.deepEqual(await search({ maxMemberCount: 12 }), ["LazyPandas", "Newbies"]);
    assert.deepEqual(await search({ minMemberCount: 30, maxMemberCount: 40 }), ["BoneCollectors", "ShadowGuild", "StormRiders"]);
    assert.deepEqual(await search({ hideApprovalOnly: true }), ["BoneCollectors", "DragonSlayers", "LazyPandas", "LootGoblins", "StormRiders", "VietLegends"]);
    assert.equal((await call(OUTSIDER, "searchclans", { server: "other-server" })).clans.length, 0);
  });

  await test("C3", "Xem chi tiết clan 50 người khi chưa vào: đủ 50 thành viên kèm role, myRole rỗng", async () => {
    const r = await call(OUTSIDER, "getclandetails", { clanId: clanOf(1) });
    assert.equal(r.clan.members.length, 50);
    assert.equal(r.clan.myRole, "");
    assert.equal(r.clan.members.filter((m) => m.role === "leader").length, 1);
    assert.equal(r.clan.members.filter((m) => m.role === "captain").length, 3);
    assert.equal((await call(OUTSIDER, "getclandetails", { clanId: "" })).code, "ERR_NOT_IN_CLAN");
    assert.equal((await call(OUTSIDER, "getclandetails", { clanId: "c_missing" })).code, "ERR_NOT_FOUND");
  });

  await test("C4", "Vào clan: clan đầy / clan cần duyệt bị từ chối; clan open vào được, tổng power tăng; đã có clan thì không vào clan khác", async () => {
    assert.equal((await call(OUTSIDER, "joinopenclan", { ...join("ClanTesterA", 5e9), clanId: clanOf(1) })).code, "ERR_CLAN_FULL");
    assert.equal((await call(OUTSIDER, "joinopenclan", { ...join("ClanTesterA", 5e9), clanId: clanOf(4) })).code, "ERR_CONFLICT_STATE_CHANGED");
    const before = await clan(3);
    const r = await call(OUTSIDER, "joinopenclan", { ...join("ClanTesterA", 5e9), clanId: clanOf(3) });
    assert.equal(r.success, true);
    assert.equal(r.clan.memberCount, 46);
    assert.equal(r.clan.myRole, "member");
    assert.ok(close(r.clan.totalPower, before.totalPower + 5e9));
    assert.equal((await call(OUTSIDER, "joinopenclan", { ...join("ClanTesterA", 5e9), clanId: clanOf(5) })).code, "ERR_ALREADY_IN_CLAN");
    assert.equal((await call(OUTSIDER, "createclan", { ...join("ClanTesterA", 5e9), clanName: "Another" })).code, "ERR_ALREADY_IN_CLAN");
    assert.equal((await call(OUTSIDER, "createclanjoinrequest", { ...join("ClanTesterA", 5e9), clanId: clanOf(4) })).code, "ERR_ALREADY_IN_CLAN");
  });

  await test("C5", "Mở lại tab clan gửi power mới → power của mình và tổng power clan cập nhật", async () => {
    const before = await clan(3);
    const r = await call(OUTSIDER, "getclandetails", { clanId: "", power: 8e9, playerName: "ClanTesterA2" });
    assert.ok(close(r.clan.totalPower, before.totalPower + 3e9));
    const me = r.clan.members.find((m) => m.userId === OUTSIDER);
    assert.deepEqual([me.power, me.playerName], [8e9, "ClanTesterA2"]);
  });

  await test("C6", "Xin vào clan: gửi được, gửi trùng / clan đầy bị từ chối, tối đa 5 đơn mỗi ngày, danh sách tìm clan đánh dấu clan đã xin", async () => {
    const request = (i) => call(REQUESTER, "createclanjoinrequest", { ...join("ClanTesterB", 7e9), clanId: clanOf(i) });
    assert.equal((await request(4)).pending, true);
    assert.equal((await request(4)).code, "ERR_REQUEST_ALREADY_PENDING");
    assert.equal((await request(2)).code, "ERR_CLAN_FULL");
    for (const i of [7, 10, 5, 6]) assert.equal((await request(i)).pending, true, "đơn vào clan " + i);
    assert.equal((await request(8)).code, "ERR_REQUEST_COOLDOWN");
    const s = await call(REQUESTER, "searchclans", { server: PROJECT });
    assert.deepEqual([...s.requestedClanIds].sort(), [4, 5, 6, 7, 10].map(clanOf).sort());
  });

  await test("C7", "Duyệt đơn (ShadowGuild): member không xem được; leader thấy đơn theo thứ tự gửi, duyệt 1 → vào clan; captain từ chối 1", async () => {
    const c = await clan(4);
    const leader = byRole(c, "leader")[0].userId;
    const captain = byRole(c, "captain")[0].userId;
    const member = byRole(c, "member")[0].userId;
    assert.equal((await call(member, "getclanjoinrequests", { clanId: c.clanId })).code, "ERR_FORBIDDEN");
    assert.equal((await call(member, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: REQUESTER })).code, "ERR_FORBIDDEN");
    assert.equal((await call(OUTSIDER, "getclanjoinrequests", { clanId: c.clanId })).code, "ERR_NOT_IN_CLAN");
    const list = (await call(leader, "getclanjoinrequests", { clanId: c.clanId })).requests;
    assert.equal(list.length, 7);
    for (let i = 1; i < list.length; i++) assert.ok(list[i - 1].requestedAt <= list[i].requestedAt);
    const [first, second] = list.filter((r) => r.userId.startsWith(PLAYER_PREFIX));

    assert.equal((await call(leader, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: first.userId })).success, true);
    const after = await clan(4);
    assert.equal(after.memberCount, 41);
    assert.equal(after.members[first.userId].role, "member");
    assert.equal(after.members[first.userId].power, first.power);
    assert.deepEqual(await read("clanPlayers", first.userId).then((p) => [p.clanId, p.role, p.requestedClanIds]), [c.clanId, "member", []]);

    assert.equal((await call(captain, "denyclanjoinrequest", { clanId: c.clanId, targetUserId: second.userId })).success, true);
    assert.equal((await call(captain, "denyclanjoinrequest", { clanId: c.clanId, targetUserId: second.userId })).code, "ERR_REQUEST_NOT_PENDING");
    assert.equal((await call(leader, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: "nobody" })).code, "ERR_REQUEST_NOT_PENDING");
    assert.equal((await call(leader, "getclanjoinrequests", { clanId: c.clanId })).requests.length, 5);
    assert.deepEqual((await read("clanPlayers", second.userId)).requestedClanIds, []);
  });

  await test("C8", "Clan đầy 50 (IronWolves): duyệt đơn bị từ chối và đơn vẫn còn; kick 1 người rồi duyệt lại được", async () => {
    const c = await clan(2);
    const leader = byRole(c, "leader")[0].userId;
    const target = Object.keys(c.requests)[0];
    assert.equal((await call(leader, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: target })).code, "ERR_CLAN_FULL");
    assert.ok((await clan(2)).requests[target]);
    assert.equal((await call(leader, "kickclanmember", { clanId: c.clanId, targetUserId: byRole(c, "member").at(-1).userId })).success, true);
    assert.equal((await call(leader, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: target })).success, true);
    assert.equal((await clan(2)).memberCount, 50);
  });

  await test("C9", "Được 1 clan duyệt → đơn ở các clan khác tự xoá", async () => {
    const c = await clan(7);
    assert.equal((await call(byRole(c, "captain")[0].userId, "acceptclanjoinrequest", { clanId: c.clanId, targetUserId: REQUESTER })).success, true);
    assert.equal((await clan(7)).members[REQUESTER].role, "member");
    for (const i of [4, 5, 6, 10]) assert.equal((await clan(i)).requests[REQUESTER], undefined, "còn đơn ở clan " + i);
    assert.deepEqual((await read("clanPlayers", REQUESTER)).requestedClanIds, []);
    assert.equal((await call(REQUESTER, "getclandetails", { clanId: "" })).clan.myRole, "member");
  });

  await test("C10", "Phong/giáng Captain: tối đa 3 captain; giáng 1 rồi phong người khác được; chỉ leader có quyền, không đổi được leader", async () => {
    const c = await clan(1);
    const leader = byRole(c, "leader")[0].userId;
    const captains = byRole(c, "captain").map((m) => m.userId);
    const member = byRole(c, "member")[0].userId;
    const act = (uid, target) => call(uid, "promoteordemoteclanmember", { clanId: c.clanId, targetUserId: target });
    assert.equal((await act(leader, member)).code, "ERR_CAPTAIN_LIMIT");
    assert.equal((await act(captains[0], member)).code, "ERR_FORBIDDEN");
    assert.equal((await act(leader, leader)).code, "ERR_FORBIDDEN");
    assert.equal((await act(leader, captains[0])).newRole, "member");
    assert.equal((await act(leader, member)).newRole, "captain");
    assert.equal((await read("clanPlayers", member)).role, "captain");
    assert.equal((await read("clanPlayers", captains[0])).role, "member");
    assert.equal(byRole(await clan(1), "captain").length, 3);
  });

  await test("C11", "Kick: captain chỉ kick được member; member không kick được ai; leader kick được captain; bị kick phải chờ 24h mới vào clan khác", async () => {
    const c = await clan(5);
    const leader = byRole(c, "leader")[0].userId;
    const [cap1, cap2] = byRole(c, "captain").map((m) => m.userId);
    const [mem1, mem2] = byRole(c, "member").map((m) => m.userId);
    const kick = (uid, target) => call(uid, "kickclanmember", { clanId: c.clanId, targetUserId: target });
    assert.equal((await kick(cap1, cap2)).code, "ERR_FORBIDDEN");
    assert.equal((await kick(cap1, leader)).code, "ERR_FORBIDDEN");
    assert.equal((await kick(mem1, mem2)).code, "ERR_FORBIDDEN");
    assert.equal((await kick(OUTSIDER, mem2)).code, "ERR_NOT_IN_CLAN");
    assert.equal((await kick(cap1, mem1)).success, true);
    assert.equal((await kick(cap1, mem1)).code, "ERR_CONFLICT_STATE_CHANGED");
    assert.equal((await kick(leader, cap2)).success, true);
    const after = await clan(5);
    assert.equal(after.memberCount, 33);
    assert.ok(close(after.totalPower, c.totalPower - c.members[mem1].power - c.members[cap2].power));
    assert.equal((await call(mem1, "getclandetails", { clanId: "" })).code, "ERR_NOT_IN_CLAN");

    const info = join("Kicked", 1e9);
    assert.equal((await call(mem1, "joinopenclan", { ...info, clanId: clanOf(6) })).code, "ERR_JOIN_COOLDOWN");
    assert.equal((await call(mem1, "createclanjoinrequest", { ...info, clanId: clanOf(7) })).code, "ERR_JOIN_COOLDOWN");
    assert.equal((await call(mem1, "createclan", { ...info, clanName: "KickedClan" })).code, "ERR_JOIN_COOLDOWN");
    setClock(REAL_NOW + 25 * HOUR);
    const r = await call(mem1, "joinopenclan", { ...info, clanId: clanOf(6) });
    setClock(0);
    assert.equal(r.success, true);
  });

  await test("C12", "Cài đặt clan chỉ leader sửa; thông báo captain sửa được, member không; chặn chữ quá dài và từ tục", async () => {
    const c = await clan(6);
    const leader = byRole(c, "leader")[0].userId;
    const captain = byRole(c, "captain")[0].userId;
    const member = byRole(c, "member")[0].userId;
    const settings = { clanId: c.clanId, description: "New description", joinSetting: "approval", bannerBackgroundTypeId: 1, bannerBackgroundColorId: 2, bannerImageTypeId: 3, bannerImageColorId: 4 };
    assert.equal((await call(member, "updateclansettings", settings)).code, "ERR_FORBIDDEN");
    assert.equal((await call(captain, "updateclansettings", settings)).code, "ERR_FORBIDDEN");
    assert.equal((await call(leader, "updateclansettings", { ...settings, description: "x".repeat(cw.CLAN_DESCRIPTION_MAX + 1) })).code, "ERR_VALIDATION");
    const r = await call(leader, "updateclansettings", settings);
    assert.deepEqual([r.clan.description, r.clan.joinSetting, r.clan.bannerBackgroundTypeId, r.clan.bannerBackgroundColorId, r.clan.bannerImageTypeId, r.clan.bannerImageColorId],
      ["New description", "approval", 1, 2, 3, 4]);
    assert.equal((await call("test_clan_c", "joinopenclan", { ...join("ClanTesterC", 1e9), clanId: c.clanId })).code, "ERR_CONFLICT_STATE_CHANGED");

    const announce = (uid, text) => call(uid, "updateclanannouncement", { clanId: c.clanId, announcement: text });
    assert.equal((await announce(member, "hi")).code, "ERR_FORBIDDEN");
    assert.equal((await announce(captain, "War on Sunday")).announcement, "War on Sunday");
    assert.equal((await announce(leader, "x".repeat(cw.CLAN_ANNOUNCEMENT_MAX + 1))).code, "ERR_VALIDATION");
    assert.equal((await announce(leader, "what the fuck")).code, "ERR_VALIDATION");
    assert.equal((await clan(6)).announcement, "War on Sunday");
  });

  await test("C13", "Leader rời clan (LootGoblins): captain mạnh nhất lên leader, clan còn 19 người", async () => {
    const c = await clan(8);
    const leader = byRole(c, "leader")[0].userId;
    const heir = byRole(c, "captain")[0].userId;
    assert.equal((await call(leader, "leaveclan")).disbanded, false);
    const after = await clan(8);
    assert.equal(after.memberCount, 19);
    assert.equal(after.members[leader], undefined);
    assert.equal(byRole(after, "leader").length, 1);
    assert.equal(byRole(after, "leader")[0].userId, heir);
    assert.equal((await read("clanPlayers", heir)).role, "leader");
    assert.deepEqual(await read("clanPlayers", leader).then((p) => [p.clanId, p.role, p.leftAt > 0]), ["", "", true]);
    assert.equal((await call(leader, "leaveclan")).code, "ERR_NOT_IN_CLAN");
  });

  await test("C14", "Cả 5 người rời (Newbies) → clan giải tán, tên được dùng lại; tạo clan: tên trùng (khác hoa thường) / sai định dạng bị từ chối", async () => {
    const c = await clan(10);
    const order = [...byRole(c, "leader"), ...byRole(c, "captain"), ...byRole(c, "member")].map((m) => m.userId);
    for (let i = 0; i < order.length; i++) {
      assert.equal((await call(order[i], "leaveclan")).disbanded, i === order.length - 1, "lượt rời " + (i + 1));
    }
    assert.equal(await clan(10), undefined);
    assert.equal(await read("clanNames", "newbies"), undefined);
    assert.ok(!(await call(OUTSIDER, "searchclans", { server: PROJECT })).clans.some((x) => x.clanName === "Newbies"));

    const founder = "test_clan_founder";
    const create = (name) => call(founder, "createclan", { ...join("Founder", 2e9), clanName: name, joinSetting: "open", bannerImageTypeId: 7 });
    for (const bad of ["ab", "bad name", "Has-Dash", "x".repeat(cw.CLAN_NAME_MAX + 1)]) assert.equal((await create(bad)).code, "ERR_VALIDATION", bad);
    assert.equal((await create("dragonslayers")).code, "ERR_NAME_TAKEN");
    const r = await create("Newbies");
    assert.equal(r.success, true);
    assert.deepEqual([r.clan.myRole, r.clan.memberCount, r.clan.clanTier, r.clan.bannerImageTypeId, r.clan.totalPower], ["leader", 1, "D", 7, 2e9]);
  });

  // =====================================================================================================================
  section("D. Clan War — giả lập trọn 1 tuần (đồng hồ giả = tuần sau tuần thật)");
  // =====================================================================================================================

  await reseed();
  const ME = "test_war_me";
  const IDLE = "test_war_idle";
  const SOLO = "test_war_solo";
  const MY_CLAN = clanOf(5);       // StormRiders (35 người, tier B)
  const ENEMY_CLAN = clanOf(6);    // BoneCollectors (30 người)
  // Ngày n của tuần giả, 03:00 UTC (1 = Thứ Ba … 6 = Chủ Nhật, 7 = Thứ Hai, 8 = Thứ Ba tuần kế).
  const fakeDay = (n) => REAL_WEEK_START + 7 * DAY + (n - 1) * DAY + 3 * HOUR;
  const membersOf = async (warId) => (await db.collection("clanWars").doc(warId).collection("members").get()).docs.map((d) => d.data());
  const dayTotal = (list, clanId, day) => sum(list.filter((m) => m.clanId === clanId).map((m) => m.daily?.[day] ?? 0));
  const state = (uid) => call(uid, "getclanwarstate", { includeConfig: true, includeDailyResults: true });
  let warId = "";
  let soloClanId = "";
  let config = cw.DEFAULT_CONFIG;
  let ranking = [];

  await test("D1", "Thứ Ba ghép cặp: 11 clan → 6 trận, cặp theo sức mạnh, clan lẻ (mới lập 1 người) gặp clan bot", async () => {
    setClock(fakeDay(1));
    await call(ME, "joinopenclan", { ...join("WarTester", 5e11), clanId: MY_CLAN });
    await call(IDLE, "joinopenclan", { ...join("IdleTester", 1e9), clanId: MY_CLAN });
    await call(SOLO, "openpvp", snapshotOf(source, "SoloTester", "US"));
    // Power thấp hơn mọi clan seed → xếp cuối khi ghép cặp → là clan lẻ gặp bot.
    soloClanId = (await call(SOLO, "createclan", { ...join("SoloTester", 1000), clanName: "SoloTesters", joinSetting: "open" })).clan.clanId;

    const matched = await matchClanWars(new Date());
    assert.equal(matched.wars, 6);
    const wars = (await db.collection("clanWars").where("weekId", "==", matched.weekId).get()).docs.map((d) => d.data());
    assert.equal(wars.length, 6);
    assert.ok(wars.every((w) => w.type === "normal"));
    for (const [a, b] of [[1, 2], [3, 4], [5, 6], [7, 8], [9, 10]]) {
      assert.ok(wars.some((w) => w.clanIds.includes(clanOf(a)) && w.clanIds.includes(clanOf(b))), `thiếu cặp ${a}-${b}`);
    }
    const botWars = wars.filter((w) => w.clanIds.some((id) => w.sides[id].isBot));
    assert.equal(botWars.length, 1);
    assert.ok(botWars[0].clanIds.includes(soloClanId));
    assert.equal(botWars[0].botRoster.length, 5);
    assert.ok(botWars[0].botRoster.every((b) => b.Items.length > 0), "bot không có đồ");

    const mine = wars.find((w) => w.clanIds.includes(MY_CLAN));
    warId = mine.warId;
    assert.equal(mine.sides[MY_CLAN].memberCountAtStart, 37);
    assert.equal(mine.sides[MY_CLAN].rewardTier, "B");
    assert.equal((await matchClanWars(new Date())).wars, 0, "ghép lần 2 trong tuần phải không tạo thêm");
    await quiet(seedWars);
  });

  await test("D2", "Ngày 1: trạng thái trận, thanh điểm = tổng điểm thành viên, nguồn điểm ngày lẻ; người không clan thấy inClan=false", async () => {
    const st = await state(ME);
    config = st.config;
    const w = st.war;
    const list = await membersOf(warId);
    assert.deepEqual([w.state, w.activeDay, w.type, w.myClan.clanId, w.enemyClan.clanId, w.enemyClan.isBot, w.myClan.rewardTier],
      ["day", 1, "normal", MY_CLAN, ENEMY_CLAN, false, "B"]);
    assert.deepEqual([w.myMemberCount, w.enemyMemberCount, w.pvp, w.dailyResults.length, w.myDailyPoints], [37, 30, null, 0, 0]);
    assert.ok(w.liveBar.myDaily > 0 && w.liveBar.enemyDaily > 0);
    assert.equal(w.liveBar.myDaily, dayTotal(list, MY_CLAN, 1));
    assert.equal(w.liveBar.enemyDaily, dayTotal(list, ENEMY_CLAN, 1));
    assert.equal(w.dayEndsAt, Math.floor((fakeDay(2) - 3 * HOUR) / 1000));
    assert.deepEqual(config.sourcesByDay["1"], ["lootEquipment", "summonCompanion", "levelUp"]);
    assert.deepEqual([st.inClan, (await state("test_nobody")).inClan], [true, false]);
  });

  await test("D3", "Ngày 1 đóng góp: loot/summon/lên cấp được điểm đúng bảng; nguồn sai ngày, giờ hôm qua, nguồn lạ, gửi trùng đều bị loại", async () => {
    const ts = Math.floor(Date.now() / 1000);
    const actions = [
      { actionId: "a1", source: "lootEquipment", rarity: "epic", count: 2, eventTimestamp: ts },          // 2 × 2
      { actionId: "a2", source: "levelUp", newLevel: 12, count: 1, eventTimestamp: ts },                  // 12 × 100
      { actionId: "a3", source: "summonCompanion", rarity: "legendary", count: 1, eventTimestamp: ts },   // 250
      { actionId: "a4", source: "dungeonKey", count: 1, eventTimestamp: ts },                             // nguồn ngày chẵn
      { actionId: "a5", source: "lootEquipment", rarity: "epic", count: 1, eventTimestamp: ts - DAY / 1000 },
      { actionId: "a6", source: "pvpWin", count: 1, eventTimestamp: ts },
    ];
    const before = (await state(ME)).war.liveBar.myDaily;
    assert.equal((await call(ME, "recordclanwarcontributions", { warId: "wrong", batchId: "b0", actions })).code, "ERR_NO_WAR");
    const r = await call(ME, "recordclanwarcontributions", { warId, batchId: "b1", actions });
    assert.deepEqual([r.success, r.accepted, r.dropped, r.points], [true, 3, 3, 1454]);
    assert.equal((await call(ME, "recordclanwarcontributions", { warId, batchId: "b1", actions })).duplicate, true);
    const again = await call(ME, "recordclanwarcontributions", { warId, batchId: "b2", actions: actions.slice(0, 1) });
    assert.deepEqual([again.accepted, again.dropped, again.points], [0, 1, 0]);
    const w = (await state(ME)).war;
    assert.deepEqual([w.myDailyPoints, w.myWeeklyPoints, w.liveBar.myDaily], [1454, 1454, before + 1454]);
  });

  await test("D4", "Mốc cá nhân: đạt 1000 nhận được 1 lần; chưa đạt / mốc lạ bị từ chối; thưởng clan, gói gộp, PvP chưa mở giữa tuần", async () => {
    const claim = (threshold) => call(ME, "claimclanwarmilestone", { warId, threshold });
    const row = config.individualMilestones.find((x) => x.threshold === 1000);
    const r = await claim(1000);
    assert.equal(r.success, true);
    assert.deepEqual([r.rewards.lootBox, r.rewards.bones, r.rewards.pickaxe, r.rewards.experience, r.rewards.cloakCurrency], [row.lootBox, row.bones, row.pickaxe, row.experience, row.cloakCurrency]);
    assert.equal((await claim(1000)).code, "ERR_ALREADY_CLAIMED");
    assert.equal((await claim(2500)).code, "ERR_MILESTONE_NOT_REACHED");
    assert.equal((await claim(1234)).code, "ERR_VALIDATION");
    assert.equal((await call(ME, "claimclanwarclanreward", { warId })).code, "ERR_WAR_STATE");
    assert.equal((await call(ME, "claimclanwarpersonalbundle", { warId })).code, "ERR_WAR_STATE");
    assert.equal((await call(ME, "startclanwarpvpbattle", { warId, targetUserId: "x" })).code, "ERR_WAR_STATE");
    assert.deepEqual((await state(ME)).war.milestone.claimedMilestones, [1000]);
  });

  await test("D5", "Ngày 2: ngày 1 được chốt (bên điểm cao thắng, MVP = người điểm cao nhất bên thắng); nguồn điểm đổi sang ngày chẵn", async () => {
    setClock(fakeDay(2));
    await quiet(seedWars);
    const w = (await state(ME)).war;
    const list = await membersOf(warId);
    assert.deepEqual([w.state, w.activeDay, w.dailyResults.length], ["day", 2, 1]);
    const d1 = w.dailyResults[0];
    assert.deepEqual([d1.day, d1.myDailyTotal, d1.enemyDailyTotal], [1, dayTotal(list, MY_CLAN, 1), dayTotal(list, ENEMY_CLAN, 1)]);
    assert.equal(d1.myClanWon, d1.myDailyTotal > d1.enemyDailyTotal);
    const winner = d1.myClanWon ? MY_CLAN : ENEMY_CLAN;
    assert.equal(d1.winnerClanId, winner);
    const top = list.filter((m) => m.clanId === winner).sort((a, b) => (b.daily?.[1] ?? 0) - (a.daily?.[1] ?? 0))[0];
    assert.equal(d1.winnerMvp.userId, top.uid);
    assert.deepEqual([w.myWarScore, w.enemyWarScore], d1.myClanWon ? [1, 0] : [0, 1]);

    const ts = Math.floor(Date.now() / 1000);
    const r = await call(ME, "recordclanwarcontributions", {
      warId, batchId: "b3", actions: [
        { actionId: "c1", source: "dungeonKey", count: 2, eventTimestamp: ts },                    // 500 × 2
        { actionId: "c2", source: "mining", resource: "gold", count: 3, eventTimestamp: ts },      // 30 × 3
        { actionId: "c3", source: "summonCape", rarity: "rare", count: 1, eventTimestamp: ts },    // 200
        { actionId: "c4", source: "lootEquipment", rarity: "epic", count: 1, eventTimestamp: ts }, // nguồn ngày lẻ
      ],
    });
    assert.deepEqual([r.accepted, r.dropped, r.points], [3, 1, 1290]);
    assert.equal((await state(ME)).war.myWeeklyPoints, 2744);
  });

  await test("D6", "Chạy lại `seed war` giữa tuần: điểm người thật đã đóng góp không mất, tổng clan = tổng thành viên", async () => {
    await quiet(seedWars);
    const war = await read("clanWars", warId);
    const list = await membersOf(warId);
    const me = list.find((m) => m.uid === ME);
    assert.deepEqual([me.daily["1"], me.daily["2"], me.weekly], [1454, 1290, 2744]);
    for (const day of ["1", "2"]) {
      assert.equal(war.sides[MY_CLAN].daily[day], dayTotal(list, MY_CLAN, day), "ngày " + day);
      assert.equal(war.sides[ENEMY_CLAN].daily[day], dayTotal(list, ENEMY_CLAN, day), "địch ngày " + day);
    }
  });

  await test("D7", "Chủ Nhật (PvP): mục tiêu = 30 thành viên clan địch kèm đồ; thắng +1000 và hạ mục tiêu; không đánh lại người đã hạ; thua 0 điểm; hết 5 vé thì dừng", async () => {
    setClock(fakeDay(6));
    await quiet(seedWars);
    let w = (await state(ME)).war;
    assert.deepEqual([w.state, w.activeDay, w.dailyResults.length], ["day6", 6, 5]);
    assert.deepEqual([w.pvp.targetCount, w.pvp.round, w.pvp.myTickets, w.pvp.ticketsMax, w.pvp.resetEnabled, w.pvp.canAttack], [30, 1, 5, 5, true, true]);
    const enemy = await read("clans", ENEMY_CLAN);
    assert.deepEqual(w.pvp.targets.map((t) => t.userId).sort(), Object.keys(enemy.members).sort());
    const seededDown = w.pvp.targets.filter((t) => t.defeated);
    assert.equal(w.pvp.defeatedCount, seededDown.length);
    assert.ok(seededDown.length > 0 && seededDown.length <= 15, "thành viên seed phải hạ 1..15 mục tiêu, thực tế " + seededDown.length);
    const dailyBefore = w.liveBar.myDaily;
    const alive = w.pvp.targets.filter((t) => !t.defeated).map((t) => t.userId);
    const start = (target) => call(ME, "startclanwarpvpbattle", { warId, targetUserId: target });
    const report = (token, won) => call(ME, "reportclanwarpvpbattle", { warId, battleToken: token, won });

    assert.equal((await start("nobody")).code, "ERR_TARGET_NOT_IN_ROUND");
    assert.equal((await start(seededDown[0].userId)).code, "ERR_ALREADY_DEFEATED");
    const s1 = await start(alive[0]);
    assert.deepEqual([s1.success, s1.tickets, s1.opponent.UserId], [true, 4, alive[0]]);
    assert.deepEqual(s1.opponent.Items, playerById.get(alive[0]).items);
    assert.equal((await report("bad", true)).code, "ERR_VALIDATION");
    const r1 = await report(s1.battleToken, true);
    assert.deepEqual([r1.won, r1.pointsAwarded, r1.defeatedCount, r1.roundReset, r1.tickets], [true, 1000, seededDown.length + 1, false, 4]);
    assert.equal((await report(s1.battleToken, true)).code, "ERR_VALIDATION");
    assert.equal((await start(alive[0])).code, "ERR_ALREADY_DEFEATED");

    const s2 = await start(alive[1]);
    const r2 = await report(s2.battleToken, false);
    assert.deepEqual([r2.won, r2.pointsAwarded, r2.defeatedCount, r2.tickets], [false, 0, seededDown.length + 1, 3]);
    for (const target of alive.slice(1, 4)) {
      assert.equal((await report((await start(target)).battleToken, true)).pointsAwarded, 1000);
    }
    assert.equal((await start(alive[4])).code, "ERR_NO_TICKETS");
    assert.equal((await call(ME, "recordclanwarcontributions", { warId, batchId: "b6", actions: [] })).code, "ERR_WAR_STATE");

    w = (await state(ME)).war;
    assert.deepEqual([w.myDailyPoints, w.myWeeklyPoints, w.liveBar.myDaily, w.pvp.myTickets, w.pvp.canAttack, w.pvp.defeatedCount],
      [4000, 6744, dailyBefore + 4000, 0, false, seededDown.length + 4]);
  });

  await test("D8", "Chủ Nhật: hạ hết cả đội địch (2 clan đều ≥ 30 người) → sang vòng 2, mục tiêu hồi lại, cả clan được phát lại 5 vé", async () => {
    const w = (await state(ME)).war;
    const last = w.pvp.targets.find((t) => !t.defeated).userId;
    // Dựng tình huống: clan mình đã hạ 29/30, mình còn 1 vé.
    await patch("clanWars", warId, { [`pvp.${MY_CLAN}.defeated`]: w.pvp.targets.map((t) => t.userId).filter((id) => id !== last) });
    await db.collection("clanWars").doc(warId).collection("members").doc(ME).update({ pvpTickets: 1 });
    const s = await call(ME, "startclanwarpvpbattle", { warId, targetUserId: last });
    const r = await call(ME, "reportclanwarpvpbattle", { warId, battleToken: s.battleToken, won: true });
    assert.deepEqual([r.pointsAwarded, r.roundReset, r.round, r.defeatedCount, r.tickets], [1000, true, 2, 0, 5]);
    const after = (await state(ME)).war.pvp;
    assert.deepEqual([after.round, after.resetCount, after.maxResets, after.defeatedCount, after.myTickets], [2, 1, 2, 0, 5]);
    assert.ok(after.targets.every((t) => !t.defeated));
    assert.equal((await state(IDLE)).war.pvp.myTickets, 5);
  });

  await test("D9", "Chủ Nhật, clan gặp bot: 5 mục tiêu bot có đồ; hạ hết 5 nhưng không reset vòng (clan < 30 người)", async () => {
    const w = (await state(SOLO)).war;
    assert.deepEqual([w.state, w.enemyClan.isBot, w.pvp.targetCount, w.enemyMemberCount, w.pvp.resetEnabled], ["day6", true, 5, 5, false]);
    for (let i = 0; i < 5; i++) {
      const s = await call(SOLO, "startclanwarpvpbattle", { warId: w.warId, targetUserId: w.pvp.targets[i].userId });
      assert.deepEqual([s.success, s.opponent.IsBot, s.opponent.Items.length > 0], [true, true, true], "trận " + (i + 1));
      const r = await call(SOLO, "reportclanwarpvpbattle", { warId: w.warId, battleToken: s.battleToken, won: true });
      assert.deepEqual([r.pointsAwarded, r.defeatedCount, r.roundReset], [1000, i + 1, false]);
    }
    assert.equal((await call(SOLO, "startclanwarpvpbattle", { warId: w.warId, targetUserId: w.pvp.targets[0].userId })).code, "ERR_ALREADY_DEFEATED");
    const after = (await state(SOLO)).war;
    assert.deepEqual([after.myDailyPoints, after.liveBar.myDaily, after.pvp.myTickets], [5000, 5000, 0]);
  });

  let outcome = "";
  await test("D10", "Thứ Hai (nghỉ): chốt đủ 6 ngày, War Score đúng trọng số (ngày 6 ×2), clan thắng đúng, điểm tier cộng đúng 1 lần", async () => {
    const tierBefore = [(await read("clans", MY_CLAN)).tierPoints, (await read("clans", ENEMY_CLAN)).tierPoints];
    setClock(fakeDay(7));
    const w = (await state(ME)).war;
    const list = await membersOf(warId);
    assert.deepEqual([w.state, w.activeDay, w.pvp, w.dailyResults.length], ["cooldown", 7, null, 6]);
    let mine = 0;
    let theirs = 0;
    for (const r of w.dailyResults) {
      assert.deepEqual([r.myDailyTotal, r.enemyDailyTotal], [dayTotal(list, MY_CLAN, r.day), dayTotal(list, ENEMY_CLAN, r.day)], "ngày " + r.day);
      assert.equal(r.warScore, config.warScoreByDay[r.day]);
      assert.equal(r.myClanWon, r.myDailyTotal > r.enemyDailyTotal);
      if (r.myDailyTotal > r.enemyDailyTotal) mine += r.warScore;
      if (r.myDailyTotal < r.enemyDailyTotal) theirs += r.warScore;
    }
    assert.equal(w.dailyResults[5].warScore, 2);
    assert.deepEqual([w.myWarScore, w.enemyWarScore], [mine, theirs]);
    const won = mine !== theirs ? mine > theirs : w.liveBar.myWeekly >= w.liveBar.enemyWeekly;
    assert.deepEqual([w.won, w.winnerClanId, w.claims.outcome], [won, won ? MY_CLAN : ENEMY_CLAN, won ? "win" : "lose"]);
    outcome = w.claims.outcome;
    assert.equal(w.myWeeklyPoints, 7744);
    assert.equal(w.cooldownEndsAt, Math.floor((fakeDay(8) - 3 * HOUR) / 1000));

    await state(ME);
    await state(IDLE);
    assert.deepEqual([(await read("clans", MY_CLAN)).tierPoints, (await read("clans", ENEMY_CLAN)).tierPoints], [tierBefore[0] + mine, tierBefore[1] + theirs]);
    assert.equal((await read("clanWars", warId)).finalized, true);
    assert.equal((await state(ME)).war.myClan.rewardTier, "B", "tier thưởng phải giữ theo đầu tuần");
  });

  await test("D11", "Thứ Hai nhận thưởng: thưởng clan đúng tier + thắng/thua, 1 lần; người 0 điểm không được; gói gộp trả các mốc chưa nhận", async () => {
    const r = await call(ME, "claimclanwarclanreward", { warId });
    assert.equal(r.success, true, JSON.stringify(r));
    assert.equal(r.outcome, outcome);
    assert.deepEqual(r.rewards, config.clanRewards.B[outcome]);
    assert.equal((await call(ME, "claimclanwarclanreward", { warId })).code, "ERR_ALREADY_CLAIMED");
    assert.equal((await call(IDLE, "claimclanwarclanreward", { warId })).code, "ERR_NOT_ELIGIBLE");
    assert.equal((await call(IDLE, "claimclanwarpersonalbundle", { warId })).code, "ERR_NOTHING_TO_CLAIM");

    const rows = config.individualMilestones.filter((x) => x.threshold <= 7744 && x.threshold !== 1000);
    const b = await call(ME, "claimclanwarpersonalbundle", { warId });
    assert.deepEqual(b.thresholds, rows.map((x) => x.threshold));
    assert.deepEqual([b.rewards.lootBox, b.rewards.bones, b.rewards.pickaxe, b.rewards.experience, b.rewards.cloakCurrency],
      [sum(rows.map((x) => x.lootBox)), sum(rows.map((x) => x.bones)), sum(rows.map((x) => x.pickaxe)), sum(rows.map((x) => x.experience)), sum(rows.map((x) => x.cloakCurrency))]);
    assert.equal((await call(ME, "claimclanwarpersonalbundle", { warId })).code, "ERR_ALREADY_CLAIMED");
    assert.equal((await call(ME, "claimclanwarmilestone", { warId, threshold: 2500 })).code, "ERR_ALREADY_CLAIMED");
    assert.equal((await call(ME, "startclanwarpvpbattle", { warId, targetUserId: "x" })).code, "ERR_WAR_STATE");
    const claims = (await state(ME)).war.claims;
    assert.deepEqual([claims.clanRewardClaimed, claims.personalBundleClaimed, claims.clanRewardEligible], [true, true, true]);
  });

  await test("D12", "Bảng đóng góp (ngày/tuần, clan mình/clan địch/clan bot) và 2 bảng xếp hạng clan", async () => {
    const w = (await state(ME)).war;
    const board = (uid, data) => call(uid, "getclanwarcontributionleaderboard", data);
    const weekly = await board(ME, { clanId: MY_CLAN, mode: "weekly" });
    weekly.entries.forEach((e, i) => {
      assert.equal(e.rank, i + 1);
      if (i > 0) assert.ok(weekly.entries[i - 1].points >= e.points);
    });
    assert.equal(sum(weekly.entries.map((e) => e.points)), w.liveBar.myWeekly);
    assert.equal(weekly.entries.find((e) => e.userId === ME).points, 7744);
    const day3 = await board(ME, { clanId: MY_CLAN, mode: "daily", day: 3 });
    assert.equal(sum(day3.entries.map((e) => e.points)), w.dailyResults[2].myDailyTotal);
    const enemy = await board(ME, { clanId: ENEMY_CLAN, mode: "weekly" });
    assert.equal(sum(enemy.entries.map((e) => e.points)), w.liveBar.enemyWeekly);
    const solo = (await state(SOLO)).war;
    const bot = await board(SOLO, { clanId: solo.enemyClan.clanId, mode: "weekly" });
    assert.equal(bot.entries.length, 5);
    assert.ok(Math.abs(sum(bot.entries.map((e) => e.points)) - solo.liveBar.enemyWeekly) <= 5);

    const clans = await call(ME, "getclanwarclanleaderboard");
    assert.equal(clans.rows.length, 11);
    clans.rows.forEach((r, i) => {
      assert.equal(r.rank, i + 1);
      if (i > 0) assert.ok(clans.rows[i - 1].tierPoints >= r.tierPoints);
      assert.equal(r.tier, cw.tierOf(r.tierPoints));
    });
    assert.deepEqual([clans.leader.clanId, clans.myClanId], [clanOf(1), MY_CLAN]);

    ranking = (await call(ME, "getclanwarleadershipranking")).rows;
    assert.equal(ranking.length, 11);
    for (let i = 1; i < ranking.length; i++) assert.ok(ranking[i - 1].tierPoints >= ranking[i].tierPoints);
    assert.equal(ranking.find((r) => r.clanId === MY_CLAN).tierPoints, w.liveBar.myWeekly);
  });

  await test("D13", "Thứ Ba tuần kế: mọi trận tuần cũ được chốt + cộng tier; Champion gặp clan điểm cao nhất tuần trước (trận leadership); tuần mới bắt đầu lại từ ngày 1", async () => {
    const oldWeekId = cw.weekIdOf(new Date());
    setClock(fakeDay(8));
    const matched = await matchClanWars(new Date());
    assert.equal(matched.wars, 6);

    const oldWars = (await db.collection("clanWars").where("weekId", "==", oldWeekId).get()).docs.map((d) => d.data());
    assert.ok(oldWars.every((w) => w.finalized && w.winnerClanId), "còn trận tuần cũ chưa chốt");
    for (let i = 0; i < CLAN_SPECS.length; i++) {
      const id = clanOf(i + 1);
      const war = oldWars.find((w) => w.clanIds.includes(id));
      const score = sum(Object.values(war.dailyResults).filter((r) => r.winnerClanId === id).map((r) => r.warScore));
      const c = await read("clans", id);
      assert.equal(c.tierPoints, CLAN_SPECS[i][3] + score, CLAN_SPECS[i][0] + " điểm tier");
      assert.equal(c.clanTier, cw.tierOf(c.tierPoints));
    }

    const newWars = (await db.collection("clanWars").where("weekId", "==", matched.weekId).get()).docs.map((d) => d.data());
    const leadership = newWars.filter((w) => w.type === "leadership");
    assert.equal(leadership.length, 1);
    const challenger = ranking.find((r) => r.clanId !== clanOf(1)).clanId;
    assert.deepEqual([...leadership[0].clanIds].sort(), [clanOf(1), challenger].sort());

    const w = (await state(ME)).war;
    assert.notEqual(w.warId, warId);
    assert.deepEqual([w.state, w.activeDay, w.dailyResults.length, w.myWeeklyPoints, w.myWarScore, w.milestone.claimedMilestones.length], ["day", 1, 0, 0, 0, 0]);
  });
} finally {
  // Trả emulator về data seed sạch của tuần thật.
  setClock(0);
  await quiet(async () => {
    await cleanupTestData();
    await clear();
    await seedPlayersAndClans();
    await seedWars();
  });
}

const failed = results.filter((r) => !r.ok);
console.log(`\n${results.length - failed.length}/${results.length} PASS` + (failed.length ? ` — FAIL: ${failed.map((r) => r.id).join(", ")}` : ""));
console.log("Đã dựng lại data seed sạch.");
process.exit(failed.length ? 1 : 0);
