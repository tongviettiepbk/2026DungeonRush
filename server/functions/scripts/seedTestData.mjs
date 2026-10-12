// Dựng data test cho các mode online trên Firebase Emulator: 1000 người chơi PvP + 10 clan + Clan War tuần này.
// Ghi thẳng Firestore bằng Admin SDK (không qua Functions) và CHỈ ghi vào emulator. Data sinh từ seed cố định → chạy lại ra y hệt.
// Emulator không lưu data qua lần tắt/bật → bật lại emulator thì chạy lại script này.
//   node scripts/seedTestData.mjs          xoá data seed cũ rồi dựng lại toàn bộ (người thật đang ở clan seed sẽ bị văng ra)
//   node scripts/seedTestData.mjs war      chỉ ghép war tuần hiện tại + điểm đóng góp (chạy lại khi sang tuần/ngày mới)
//   node scripts/seedTestData.mjs clear    xoá data seed
// Nhận diện data seed: uid người chơi "seed_0001".."seed_1000", clanId "c_seed_01".."c_seed_10".
import { createRequire } from "node:module";
import { pathToFileURL } from "node:url";

process.env.FIRESTORE_EMULATOR_HOST ??= "127.0.0.1:8085";
process.env.GCLOUD_PROJECT ??= "demo-dungeonrush";

const require = createRequire(import.meta.url);
const { initializeApp } = require("firebase-admin/app");
const { FieldPath, getFirestore } = require("firebase-admin/firestore");
const { generateBots, hashString, loadBank, makeRng } = require("../lib/bots.js");
const { matchClanWars } = require("../lib/clanWar.js");
const cw = require("../lib/clanWarConfig.js");
const pvp = require("../lib/pvpConfig.js");
const { getDayKey } = require("../lib/schedule.js");

const PROJECT = process.env.GCLOUD_PROJECT;
initializeApp({ projectId: PROJECT });
const db = getFirestore();

const SEED = 20261012;
const PLAYER_PREFIX = "seed_";
const CLAN_PREFIX = "c_seed_";
const PLAYER_COUNT = 1000;
const DAY_SECONDS = 86400;

// ===== 1000 người chơi (pvpPlayers) =====

// % người chơi mỗi league, Iron → Legend.
const LEAGUE_SHARE = [30, 20, 14, 10, 8, 6, 5, 3.5, 2.5, 1];
const TROPHY_FLOOR = 700;
const TROPHY_CEIL = 3200;
// Người chưa đánh PvP: trophy quanh mốc khởi điểm bất kể power → quanh 1000 có cả đối thủ yếu lẫn mạnh.
const NEWCOMER_RATE = 0.15;
// Client gửi mã vùng của máy (máy dev đang là US) → US chiếm nhiều để tab "country" có người.
const COUNTRIES = ["US", "US", "US", "US", "US", "VN", "VN", "VN", "BR", "ID", "RU", "DE", "FR", "GB", "JP", "KR", "TH", "PH", "TR", "MX"];

function pick(rng, list) {
  return list[Math.floor(rng() * list.length)];
}

function between(rng, min, max) {
  return min + Math.floor(rng() * (max - min + 1));
}

function buildPlayers(rng) {
  // Bộ đồ lấy từ ngân hàng Boss Rush (power tính bằng công thức thật), trải đều từ yếu tới mạnh.
  const steps = [...loadBank().values()].flatMap((c) => c.steps).sort((a, b) => a.power - b.power);
  if (steps.length === 0) throw new Error("Thiếu functions/data/botBank.json");
  // Tên + avatar dùng chung bộ sinh với bot Boss Rush.
  const idents = generateBots(SEED, PLAYER_COUNT, new Set(), 1, new Map());

  const players = idents.map((bot, i) => ({
    uid: PLAYER_PREFIX + String(i + 1).padStart(4, "0"),
    name: bot.name,
    avatarId: bot.avatarId,
    loadout: steps[Math.floor(i * steps.length / PLAYER_COUNT)],
    newcomer: rng() < NEWCOMER_RATE,
    // Hạng sức mạnh có nhiễu → trophy đi theo power nhưng không khớp tuyệt đối.
    rank: i + (rng() - 0.5) * PLAYER_COUNT * 0.3,
  }));

  const ranked = players.filter((p) => !p.newcomer).sort((a, b) => a.rank - b.rank);
  let league = 0;
  let quota = LEAGUE_SHARE[0] / 100 * ranked.length;
  ranked.forEach((p, i) => {
    while (i >= quota && league < pvp.LEAGUES.length - 1) quota += LEAGUE_SHARE[++league] / 100 * ranked.length;
    const l = pvp.LEAGUES[league];
    p.trophy = between(rng, Math.max(l.MinTrophy, TROPHY_FLOOR), Math.min(l.MaxTrophy, TROPHY_CEIL));
    p.attacksUsed = between(rng, 10, 300);
    p.wins = Math.round(p.attacksUsed * (0.35 + 0.03 * league + rng() * 0.1));
  });
  for (const p of players.filter((x) => x.newcomer)) {
    p.trophy = between(rng, pvp.START_TROPHY - 50, pvp.START_TROPHY + 50);
    p.attacksUsed = between(rng, 0, 3);
    p.wins = Math.round(p.attacksUsed * rng());
  }

  const dayKey = getDayKey(new Date());
  return players.map((p) => ({
    uid: p.uid,
    playerName: p.name,
    countryCode: pick(rng, COUNTRIES),
    avatarId: p.avatarId,
    power: p.loadout.power,
    items: p.loadout.items,
    companions: p.loadout.companions,
    enchantmentTiers: p.loadout.enchantmentTiers,
    showCloak: p.loadout.showCloak,
    snapshotHash: "",
    contentVersion: "1.0.0",
    trophy: p.trophy,
    attacksUsed: p.attacksUsed,
    wins: p.wins,
    losses: p.attacksUsed - p.wins,
    tickets: {
      freeRemaining: pvp.DAILY_FREE_TICKETS, adRemaining: 0, adClaimedToday: 0, maxAdPerDay: pvp.MAX_AD_TICKETS_PER_DAY,
      dailyFreeTickets: pvp.DAILY_FREE_TICKETS, dayKey,
    },
    roster: null,
    activeBattle: null,
    isBot: false,
    updatedAt: Date.now(),
  }));
}

// ===== 10 clan (clans + clanPlayers + clanNames) =====

// Tên, số thành viên, cách vào, điểm tier, số đơn xin vào đang chờ. Xếp từ mạnh tới yếu.
// Đủ các ca cần test: đầy 50 (open + approval có đơn chờ → ERR_CLAN_FULL), đúng 30 (ngưỡng reset vòng PvP), dưới 30, clan nhỏ, đủ 5 tier.
const CLAN_SPECS = [
  ["DragonSlayers", 50, "open", 72, 0],
  ["IronWolves", 50, "approval", 41, 5],
  ["VietLegends", 45, "open", 36, 0],
  ["ShadowGuild", 40, "approval", 22, 6],
  ["StormRiders", 35, "open", 18, 0],
  ["BoneCollectors", 30, "open", 9, 0],
  ["PhoenixRise", 28, "approval", 7, 3],
  ["LootGoblins", 20, "open", 3, 0],
  ["LazyPandas", 12, "open", 0, 0],
  ["Newbies", 5, "approval", 0, 2],
];
const DESCRIPTIONS = [
  "Daily players only. Clan War every week.", "Friendly clan, everyone welcome!", "Push for S tier. Be active or be kicked.",
  "Casual and chill. No pressure.", "We loot, we fight, we win.",
];
const ANNOUNCEMENTS = ["", "Clan War starts Tuesday. Save your keys!", "Welcome new members. Use all 5 PvP tickets on Sunday.", "Donate points daily please."];

function buildClans(rng, players) {
  const nowSec = Math.floor(Date.now() / 1000);
  const dayKey = getDayKey(new Date());
  const byPower = [...players].sort((a, b) => b.power - a.power);
  const taken = new Set();
  const clans = [];
  const clanPlayers = [];

  CLAN_SPECS.forEach(([name, memberCount, joinSetting, tierPoints, requestCount], i) => {
    const clanId = CLAN_PREFIX + String(i + 1).padStart(2, "0");
    // Clan mạnh lấy người từ nhóm power cao hơn (các cửa sổ gối nhau).
    const candidates = byPower.slice(i * 70, i * 70 + 200).filter((p) => !taken.has(p.uid));
    for (let k = candidates.length - 1; k > 0; k--) {
      const j = Math.floor(rng() * (k + 1));
      [candidates[k], candidates[j]] = [candidates[j], candidates[k]];
    }
    const chosen = candidates.slice(0, memberCount + requestCount);
    chosen.forEach((p) => taken.add(p.uid));
    const roster = chosen.slice(0, memberCount).sort((a, b) => b.power - a.power);
    const captains = Math.min(cw.CLAN_MAX_CAPTAINS, Math.floor(memberCount / 4));
    const createdAt = nowSec - between(rng, 20, 90) * DAY_SECONDS;

    const members = {};
    roster.forEach((p, k) => {
      const role = k === 0 ? "leader" : k <= captains ? "captain" : "member";
      members[p.uid] = {
        userId: p.uid, playerName: p.playerName, power: p.power, role, avatarId: p.avatarId,
        joinedAt: k === 0 ? createdAt : between(rng, createdAt, nowSec),
      };
      clanPlayers.push({ uid: p.uid, clanId, role, leftAt: 0, requestDayKey: "", requestCount: 0, requestedClanIds: [] });
    });
    const requests = {};
    for (const p of chosen.slice(memberCount)) {
      requests[p.uid] = {
        userId: p.uid, playerName: p.playerName, power: p.power, avatarId: p.avatarId,
        requestedAt: nowSec - between(rng, 60, DAY_SECONDS / 2),
      };
      clanPlayers.push({ uid: p.uid, clanId: "", role: "", leftAt: 0, requestDayKey: dayKey, requestCount: 1, requestedClanIds: [clanId] });
    }

    clans.push({
      clanId, clanName: name, nameLower: name.toLowerCase(), tierPoints, clanTier: cw.tierOf(tierPoints), joinSetting,
      server: PROJECT, description: pick(rng, DESCRIPTIONS), announcement: pick(rng, ANNOUNCEMENTS), members, requests,
      memberCount: roster.length, totalPower: roster.reduce((s, p) => s + p.power, 0), createdAt, isBot: false,
      bannerBackgroundTypeId: between(rng, 0, cw.BANNER_BG_TYPES - 1), bannerBackgroundColorId: between(rng, 0, cw.BANNER_BG_COLORS - 1),
      bannerImageTypeId: between(rng, 0, cw.BANNER_IMAGE_TYPES - 1), bannerImageColorId: between(rng, 0, cw.BANNER_IMAGE_COLORS - 1),
    });
  });
  return { clans, clanPlayers };
}

async function seedPlayersAndClans() {
  const rng = makeRng(SEED);
  const players = buildPlayers(rng);
  const { clans, clanPlayers } = buildClans(rng, players);

  const writer = db.bulkWriter();
  for (const p of players) writer.set(db.collection("pvpPlayers").doc(p.uid), p);
  for (const p of clanPlayers) writer.set(db.collection("clanPlayers").doc(p.uid), p);
  for (const c of clans) {
    writer.set(db.collection("clans").doc(c.clanId), c);
    writer.set(db.collection("clanNames").doc(c.nameLower), { clanId: c.clanId });
  }
  // Clan đứng đầu làm Champion để ô Champion ở bảng xếp hạng Clan War có dữ liệu.
  writer.set(db.collection("clanWarMeta").doc("champion"), { clanId: clans[0].clanId, weekId: "" });
  await writer.close();

  console.log(`Người chơi: ${players.length}`);
  for (const l of pvp.LEAGUES) {
    const inLeague = players.filter((p) => pvp.getLeagueIndex(p.trophy) === l.LeagueIndex);
    console.log(`  ${l.LeagueName.padEnd(16)} ${String(inLeague.length).padStart(4)} người`);
  }
  console.log(`Clan: ${clans.length} (${clanPlayers.filter((p) => p.clanId).length} người có clan)`);
  for (const c of clans) {
    console.log(`  ${c.clanId}  ${c.clanName.padEnd(15)} tier ${c.clanTier}  ${String(c.memberCount).padStart(2)}/50  ${c.joinSetting.padEnd(8)}  đơn chờ ${Object.keys(c.requests).length}`);
  }
}

// ===== Clan War tuần hiện tại =====

// Ghép cặp bằng đúng hàm của server (clan thật đang có cũng được ghép), rồi bơm điểm đóng góp cho thành viên seed
// ở các ngày đã/đang diễn ra. Điểm suy ra từ (warId, uid) → chạy lại không đổi; điểm của người thật giữ nguyên.
async function seedWars() {
  const now = new Date();
  const { weekId, wars: created } = await matchClanWars(now);
  const lastDay = Math.min(6, cw.warDayOf(now));
  const { winPoints, startTickets } = cw.DEFAULT_CONFIG.pvp;
  const snap = await db.collection("clanWars").where("weekId", "==", weekId).get();
  const writer = db.bulkWriter();
  let seeded = 0;

  for (const doc of snap.docs) {
    const war = doc.data();
    const seedClanIds = war.clanIds.filter((id) => id.startsWith(CLAN_PREFIX));
    if (war.finalized || seedClanIds.length === 0) continue;
    const existing = (await doc.ref.collection("members").get()).docs.map((d) => d.data());

    for (const clanId of seedClanIds) {
      const clan = (await db.collection("clans").doc(clanId).get()).data();
      const enemyId = war.clanIds.find((id) => id !== clanId);
      const enemyUids = war.sides[enemyId].isBot
        ? war.botRoster.map((b) => b.UserId)
        : Object.keys((await db.collection("clans").doc(enemyId).get()).data()?.members ?? {});
      // Ngày 6: thành viên seed chỉ hạ tối đa nửa đội địch, phần còn lại để người thật đánh.
      const maxDefeated = Math.floor(enemyUids.length / 2);
      const defeated = [];
      const totals = {};
      for (const m of existing) {
        if (m.clanId !== clanId || m.uid.startsWith(PLAYER_PREFIX)) continue;
        for (const [d, p] of Object.entries(m.daily ?? {})) totals[d] = (totals[d] ?? 0) + p;
      }

      for (const member of Object.values(clan.members)) {
        if (!member.userId.startsWith(PLAYER_PREFIX)) continue;
        const rng = makeRng(hashString(war.warId + member.userId));
        // ~15% thành viên không đóng góp gì.
        const effort = rng() < 0.15 ? 0 : 0.3 + rng() * 1.7;
        const daily = {};
        let weekly = 0;
        let wins = 0;
        for (let d = 1; d <= lastDay; d++) {
          let points;
          if (d < 6) {
            points = Math.round(effort * (500 + rng() * 4500) / 10) * 10;
          } else {
            wins = Math.min(Math.floor(effort * rng() * 3), maxDefeated - defeated.length);
            for (let k = 0; k < wins; k++) defeated.push(enemyUids[defeated.length]);
            points = wins * winPoints;
          }
          if (points <= 0) continue;
          daily[d] = points;
          weekly += points;
          totals[d] = (totals[d] ?? 0) + points;
        }
        writer.set(doc.ref.collection("members").doc(member.userId), {
          uid: member.userId, clanId, playerName: member.playerName, avatarId: member.avatarId, daily, weekly,
          batchIds: [], actionIds: [], pvpRound: lastDay === 6 ? 1 : 0, pvpTickets: lastDay === 6 ? startTickets - wins : 0,
          activeBattle: null, claimedMilestones: [], bundleClaimed: false, clanRewardClaimed: false,
        });
      }

      war.sides[clanId].daily = totals;
      if (lastDay === 6) {
        const old = war.pvp?.[clanId];
        war.pvp = war.pvp ?? {};
        war.pvp[clanId] = {
          round: old?.round ?? 1, resetCount: old?.resetCount ?? 0, defeated: [...new Set([...(old?.defeated ?? []), ...defeated])],
        };
      }
    }
    writer.set(doc.ref, war);
    seeded++;
    const [a, b] = war.clanIds.map((id) => war.sides[id].clanName);
    console.log(`  ${a} vs ${b}`);
  }
  await writer.close();
  console.log(`Clan War tuần ${weekId}: ghép mới ${created} trận, bơm điểm ${seeded} trận (tới ngày ${lastDay}, hôm nay là ngày ${cw.warDayOf(now)})`);
}

// ===== Xoá data seed =====

async function deleteByIdPrefix(collection, prefix) {
  const snap = await db.collection(collection)
    .where(FieldPath.documentId(), ">=", prefix).where(FieldPath.documentId(), "<", prefix + "").get();
  const writer = db.bulkWriter();
  snap.docs.forEach((d) => writer.delete(d.ref));
  await writer.close();
  return snap.size;
}

async function clear() {
  // War có clan seed: xoá cả subcollection members + index của HAI phe để clan thật ở phe kia được ghép lại.
  const wars = await db.collection("clanWars").get();
  let warCount = 0;
  for (const d of wars.docs) {
    const war = d.data();
    if (!war.clanIds.some((id) => id.startsWith(CLAN_PREFIX))) continue;
    for (const id of war.clanIds) await db.collection("clanWarIndex").doc(war.weekId + "_" + id).delete();
    await db.recursiveDelete(d.ref);
    warCount++;
  }
  const names = await db.collection("clanNames").get();
  for (const d of names.docs) if (String(d.data().clanId).startsWith(CLAN_PREFIX)) await d.ref.delete();
  const champion = await db.collection("clanWarMeta").doc("champion").get();
  if (String(champion.data()?.clanId ?? "").startsWith(CLAN_PREFIX)) await champion.ref.delete();

  const clanCount = await deleteByIdPrefix("clans", CLAN_PREFIX);
  const playerCount = await deleteByIdPrefix("pvpPlayers", PLAYER_PREFIX);
  await deleteByIdPrefix("clanPlayers", PLAYER_PREFIX);
  console.log(`Đã xoá data seed cũ: ${playerCount} người chơi, ${clanCount} clan, ${warCount} war`);
}

// ===== Chạy =====

export { CLAN_PREFIX, CLAN_SPECS, PLAYER_PREFIX, PROJECT, clear, db, deleteByIdPrefix, seedPlayersAndClans, seedWars };

export async function requireEmulator() {
  try {
    await fetch(`http://${process.env.FIRESTORE_EMULATOR_HOST}/`);
  } catch {
    console.error(`Không kết nối được Firestore emulator ở ${process.env.FIRESTORE_EMULATOR_HOST}. Chạy trước: cd server && firebase emulators:start`);
    process.exit(1);
  }
}

// Chỉ chạy lệnh khi gọi trực tiếp (bộ test import file này để dùng lại các hàm trên).
if (import.meta.url === pathToFileURL(process.argv[1]).href) {
  await requireEmulator();
  const command = process.argv[2] ?? "all";
  if (command === "clear") {
    await clear();
  } else if (command === "war") {
    await seedWars();
  } else if (command === "all") {
    await clear();
    await seedPlayersAndClans();
    await seedWars();
  } else {
    console.error("Dùng: node scripts/seedTestData.mjs [all|war|clear]");
    process.exit(1);
  }
}
