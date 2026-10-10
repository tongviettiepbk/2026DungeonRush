// Test tích hợp Clan + Clan War trên Firebase Emulator (chạy: npm run test:emulator:clan).
// Clan: tạo/tìm/vào/xin vào/duyệt/role/kick/rời/giải tán. War: ghép cặp (admin) → state → đóng góp theo nguồn của ngày
// → leaderboard → mốc cá nhân. Ngày 6/7 (PvP/cooldown) phụ thuộc lịch thật → chỉ kiểm khi chạy đúng ngày.
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

const join = (name, power) => ({ server: PROJECT, playerName: name, power, avatarId: 0 });
const suffix = Math.floor(Math.random() * 1e6).toString();

const leader = await signUp();
const p2 = await signUp();
const p3 = await signUp();
const leader2 = await signUp();

// --- createClan: validate tên ---
assert.equal((await call(leader, "createclan", { ...join("L", 100), clanName: "ab" })).code, "ERR_VALIDATION");
assert.equal((await call(leader, "createclan", { ...join("L", 100), clanName: "bad name!" })).code, "ERR_VALIDATION");
const c1 = await call(leader, "createclan", {
  ...join("Leader", 1000), clanName: "Alpha" + suffix, joinSetting: "open",
  bannerBackgroundTypeId: 2, bannerBackgroundColorId: 3, bannerImageTypeId: 5, bannerImageColorId: 1,
});
assert.equal(c1.success, true, JSON.stringify(c1));
const clanId = c1.clan.clanId;
assert.equal(c1.clan.myRole, "leader");
assert.equal(c1.clan.memberCount, 1);
assert.equal(c1.clan.clanTier, "D");
assert.equal(c1.clan.bannerImageTypeId, 5);
assert.equal((await call(leader2, "createclan", { ...join("X", 1), clanName: "alpha" + suffix })).code, "ERR_NAME_TAKEN");
assert.equal((await call(leader, "createclan", { ...join("X", 1), clanName: "Other" + suffix })).code, "ERR_ALREADY_IN_CLAN");

// --- search ---
const s1 = await call(p2, "searchclans", { server: PROJECT, clanName: "alpha" + suffix, hideApprovalOnly: false });
assert.equal(s1.success, true);
assert.equal(s1.clans.length, 1);
assert.equal(s1.clans[0].members.length, 0);

// --- joinOpenClan ---
const j2 = await call(p2, "joinopenclan", { ...join("Two", 500), clanId });
assert.equal(j2.success, true);
assert.equal(j2.clan.memberCount, 2);
assert.equal(j2.clan.myRole, "member");

// --- settings: chỉ leader; approval → p3 phải xin ---
assert.equal((await call(p2, "updateclansettings", { clanId, description: "x", joinSetting: "open" })).code, "ERR_FORBIDDEN");
const st = await call(leader, "updateclansettings", { clanId, description: "Hello", joinSetting: "approval", bannerBackgroundTypeId: 1, bannerBackgroundColorId: 1, bannerImageTypeId: 1, bannerImageColorId: 1 });
assert.equal(st.clan.joinSetting, "approval");
assert.equal((await call(p3, "joinopenclan", { ...join("Three", 300), clanId })).code, "ERR_CONFLICT_STATE_CHANGED");
const r3 = await call(p3, "createclanjoinrequest", { ...join("Three", 300), clanId });
assert.equal(r3.pending, true);
assert.equal((await call(p3, "createclanjoinrequest", { ...join("Three", 300), clanId })).code, "ERR_REQUEST_ALREADY_PENDING");
const sr = await call(p3, "searchclans", { server: PROJECT, clanName: "alpha" + suffix });
assert.deepEqual(sr.requestedClanIds, [clanId]);
assert.equal((await call(p2, "getclanjoinrequests", { clanId })).code, "ERR_FORBIDDEN");

// --- announcement: member không được ---
assert.equal((await call(p2, "updateclanannouncement", { clanId, announcement: "hi" })).code, "ERR_FORBIDDEN");
assert.equal((await call(leader, "updateclanannouncement", { clanId, announcement: "Welcome" })).announcement, "Welcome");

// --- promote p2 → captain, captain duyệt request ---
const pr = await call(leader, "promoteordemoteclanmember", { clanId, targetUserId: p2.uid });
assert.equal(pr.newRole, "captain");
const reqs = await call(p2, "getclanjoinrequests", { clanId });
assert.equal(reqs.requests.length, 1);
assert.equal(reqs.requests[0].userId, p3.uid);
assert.equal((await call(p2, "acceptclanjoinrequest", { clanId, targetUserId: p3.uid })).success, true);
const d1 = await call(p3, "getclandetails", { clanId: "", power: 350, playerName: "Three" });
assert.equal(d1.clan.memberCount, 3);
assert.equal(d1.clan.myRole, "member");
assert.equal(d1.clan.announcement, "Welcome");
assert.equal(d1.clan.totalPower, 1000 + 500 + 350);

// --- captain không kick được captain/leader, kick được member ---
assert.equal((await call(p2, "kickclanmember", { clanId, targetUserId: leader.uid })).code, "ERR_FORBIDDEN");
assert.equal((await call(p2, "kickclanmember", { clanId, targetUserId: p3.uid })).success, true);
assert.equal((await call(p3, "getclandetails", { clanId: "" })).code, "ERR_NOT_IN_CLAN");
// Vừa bị kick → chờ 24h.
assert.equal((await call(p3, "createclanjoinrequest", { ...join("Three", 300), clanId })).code, "ERR_JOIN_COOLDOWN");

// --- clan thứ 2 để ghép war ---
const c2 = await call(leader2, "createclan", { ...join("Beta", 800), clanName: "Beta" + suffix, joinSetting: "open" });
assert.equal(c2.success, true);

// ===== Clan War =====
const m = await call(leader, "matchclanwarsnow", {});
assert.equal(m.success, true);
const ws = await call(leader, "getclanwarstate", { includeConfig: true, includeDailyResults: true });
assert.equal(ws.success, true);
assert.equal(ws.inClan, true);
assert.ok(ws.war, "war phải có sau khi ghép");
assert.ok(ws.config.awards.levelUpMultiplier === 100);
const war = ws.war;
console.log("war", war.warId, "type", war.type, "state", war.state, "day", war.activeDay, "enemy", war.enemyClan.clanName, "bot", war.enemyClan.isBot);
assert.equal(war.myClan.clanId, clanId);

const noClan = await call(p3, "getclanwarstate", {});
assert.equal(noClan.inClan, false);

if (war.state === "day") {
  const day = String(war.activeDay);
  const sources = ws.config.sourcesByDay[day];
  const now = Math.floor(Date.now() / 1000);
  const actions = [];
  if (sources.includes("lootEquipment")) actions.push({ actionId: "a1" + suffix, source: "lootEquipment", rarity: "epic", count: 2, eventTimestamp: now });
  if (sources.includes("levelUp")) actions.push({ actionId: "a2" + suffix, source: "levelUp", newLevel: 12, count: 1, eventTimestamp: now });
  if (sources.includes("dungeonKey")) actions.push({ actionId: "a3" + suffix, source: "dungeonKey", count: 1, eventTimestamp: now });
  if (sources.includes("mining")) actions.push({ actionId: "a4" + suffix, source: "mining", resource: "gold", count: 3, eventTimestamp: now });
  actions.push({ actionId: "bad" + suffix, source: "pvpWin", count: 1, eventTimestamp: now });   // nguồn sai → dropped
  const rec = await call(leader, "recordclanwarcontributions", { warId: war.warId, batchId: "b1" + suffix, actions });
  assert.equal(rec.success, true, JSON.stringify(rec));
  assert.equal(rec.dropped, 1);
  assert.ok(rec.points > 0);
  const dup = await call(leader, "recordclanwarcontributions", { warId: war.warId, batchId: "b1" + suffix, actions });
  assert.equal(dup.duplicate, true);
  const ws2 = await call(leader, "getclanwarstate", {});
  assert.equal(ws2.war.myDailyPoints, rec.points);
  assert.equal(ws2.war.myWeeklyPoints, rec.points);
  assert.equal(ws2.war.liveBar.myDaily, rec.points);
  assert.equal(ws2.war.myMvp.userId, leader.uid);
  const lb = await call(leader, "getclanwarcontributionleaderboard", { clanId, mode: "daily", day: war.activeDay });
  assert.equal(lb.entries[0].userId, leader.uid);
  assert.equal(lb.entries[0].points, rec.points);
  const lbe = await call(leader, "getclanwarcontributionleaderboard", { clanId: war.enemyClan.clanId, mode: "weekly" });
  assert.equal(lbe.success, true);

  // Mốc cá nhân 1000.
  if (rec.points >= 1000) {
    const cm = await call(leader, "claimclanwarmilestone", { warId: war.warId, threshold: 1000 });
    assert.equal(cm.success, true, JSON.stringify(cm));
    assert.ok(cm.rewards.lootBox > 0);
    assert.equal((await call(leader, "claimclanwarmilestone", { warId: war.warId, threshold: 1000 })).code, "ERR_ALREADY_CLAIMED");
  }
  assert.equal((await call(leader, "claimclanwarmilestone", { warId: war.warId, threshold: 60000 })).code, "ERR_MILESTONE_NOT_REACHED");
  assert.equal((await call(leader, "claimclanwarclanreward", { warId: war.warId })).code, "ERR_WAR_STATE");
} else if (war.state === "day6") {
  assert.ok(war.pvp && war.pvp.targets.length > 0);
  const t = war.pvp.targets[0];
  const sb = await call(leader, "startclanwarpvpbattle", { warId: war.warId, targetUserId: t.userId, requestId: "r1" });
  assert.equal(sb.success, true, JSON.stringify(sb));
  const rb = await call(leader, "reportclanwarpvpbattle", { warId: war.warId, battleToken: sb.battleToken, won: true });
  assert.equal(rb.success, true);
  assert.ok(rb.pointsAwarded > 0);
} else {
  console.log("cooldown — kiểm thưởng clan");
  const cr = await call(leader, "claimclanwarclanreward", { warId: war.warId });
  console.log("clan reward", JSON.stringify(cr));
}

const cl = await call(leader, "getclanwarclanleaderboard", {});
assert.equal(cl.success, true);
assert.ok(cl.rows.some((r) => r.clanId === clanId));
const lr = await call(leader, "getclanwarleadershipranking", {});
assert.equal(lr.success, true);

// --- rời clan: leader rời → captain p2 thành leader; p2 rời → giải tán ---
assert.equal((await call(leader, "leaveclan", {})).disbanded, false);
const d2 = await call(p2, "getclandetails", { clanId: "" });
assert.equal(d2.clan.myRole, "leader");
assert.equal((await call(p2, "leaveclan", {})).disbanded, true);
assert.equal((await call(p2, "getclandetails", { clanId })).code, "ERR_NOT_FOUND");
await call(leader2, "leaveclan", {});

console.log("CLAN EMULATOR TEST PASS");
