import * as assert from "assert";
import { BankChain, BotParams, botPower, botProgress, generateBots, hashSeed, loadBank, setBankForTest } from "../bots";
import { bossStateFor, distribute, poolBossState, rankPool, seasonBounds, toRows } from "../bossRushPool";
import { PlayerDoc, RewardEntry } from "../models";
import { DEFAULT_CONFIG, computeNewTier, computeRewards, getRewardTable } from "../rewardConfig";
import { createPool } from "../season";

// Kiểm các phép tính thuần của server/BOSS_RUSH_DESIGN.md: thưởng, lên/xuống league, chia sảnh, máu boss, bot.

const HOUR = 60 * 60 * 1000;
const amount = (list: RewardEntry[], type: number) => list.find((r) => r.Type === type)?.Amount ?? 0;

// ----- Thưởng: khớp lần nhận thưởng THẬT trong game gốc (Iron, hạng 71) -----
const iron = getRewardTable(DEFAULT_CONFIG, 1);
const real = computeRewards(iron, 71, 3);
assert.deepStrictEqual(
  [amount(real, 9), amount(real, 4), amount(real, 0), amount(real, 2), amount(real, 3), amount(real, 1)],
  [185, 540, 540, 2, 2, 5], "Iron hạng 71: 185 Cloak, 540 Lootbox, 540 Bone, 2 + 2 key, 5 Gem");
assert.strictEqual(amount(computeRewards(iron, 500, 1), 9), 140, "giết 1 boss → Guaranteed 140 Cloak, ngoài hạng 100 không có Placement");
assert.strictEqual(amount(computeRewards(iron, 500, 1), 1), 0);
assert.strictEqual(amount(computeRewards(iron, 1, 0), 1), 100);
const legend = computeRewards(getRewardTable(DEFAULT_CONFIG, 10), 1, 0);
assert.strictEqual(amount(legend, 1), 350, "Legend ×3.5");
assert.strictEqual(amount(legend, 2), 2, "key dungeon không nhân hệ số");

// ----- Lên / xuống league -----
const tier = (t: number, rank: number, count: number, fights: number, score: number) =>
  computeNewTier(DEFAULT_CONFIG, t, rank, count, fights, score);
assert.strictEqual(tier(1, 15, 60, 5, 100), 2, "Iron top 15 lên");
assert.strictEqual(tier(1, 16, 60, 5, 100), 1);
assert.strictEqual(tier(1, 1, 60, 0, 0), 1, "0 điểm không bao giờ lên");
assert.strictEqual(tier(1, 60, 60, 0, 0), 1, "Iron không xuống");
assert.strictEqual(tier(2, 60, 60, 0, 0), 1, "Bronze không đánh trận nào → xuống");
assert.strictEqual(tier(2, 60, 60, 1, 10), 2, "Bronze có đánh thì không xuống dù đứng cuối");
assert.strictEqual(tier(4, 52, 60, 3, 10), 3, "Gold: 15% cuối của 60 người = 9 người cuối");
assert.strictEqual(tier(4, 51, 60, 3, 10), 4);
assert.strictEqual(tier(4, 11, 60, 3, 10), 4, "Gold chỉ top 10 lên");
assert.strictEqual(tier(7, 5, 100, 9, 10), 8);
assert.strictEqual(tier(10, 1, 100, 9, 10), 10, "Legend không lên nữa");
assert.strictEqual(tier(10, 71, 100, 9, 10), 9, "Legend: 30% cuối xuống");
assert.strictEqual(tier(10, 70, 100, 9, 10), 10);

// ----- Chia sảnh đầu mùa -----
assert.deepStrictEqual(distribute(0, 80), []);
assert.deepStrictEqual(distribute(25, 80), [25]);
assert.deepStrictEqual(distribute(70, 80), [70]);
assert.deepStrictEqual(distribute(150, 80), [75, 75]);
assert.deepStrictEqual(distribute(200, 80), [67, 67, 66]);

// ----- Máu boss -----
assert.deepStrictEqual(bossStateFor(1, 0), { bossNumber: 1, bossHP: 500000000, maxBossHP: 500000000, bossesKilled: 0 });
assert.deepStrictEqual(bossStateFor(1, 500000000), { bossNumber: 2, bossHP: 750000000, maxBossHP: 750000000, bossesKilled: 1 });
assert.strictEqual(bossStateFor(1, 815390000).bossHP, 434610000, "sảnh thật: Boss #2 còn 434.61M / 750M");

// ----- Bot -----
const { start, end } = seasonBounds("2026-10-06");
const params: BotParams = { seasonStart: start, seasonEnd: end, botStartAt: start, anchorOwn: 1000000, anchorTeamRatio: 8, maxFightsPerDay: 6 };
const SEEDS = 5000;
let idle = 0;
let best = 0;
for (let i = 0; i < SEEDS; i++) {
  const seed = hashSeed(777, i);
  let last = 0;
  for (let t = start; t <= end + 24 * HOUR; t += 6 * HOUR) {
    const p = botProgress(seed, params, t);
    assert.ok(p.score >= last, "điểm bot chỉ tăng");
    assert.ok(p.team >= p.score, "damage cả đội ≥ damage riêng");
    last = p.score;
  }
  const final = botProgress(seed, params, end);
  assert.deepStrictEqual(botProgress(seed, params, end), final, "cùng seed + cùng giờ → cùng kết quả");
  assert.deepStrictEqual(botProgress(seed, params, end + 30 * 24 * HOUR), final, "hết mùa thì điểm đứng yên");
  assert.ok(botProgress(seed, { ...params, botStartAt: start + 72 * HOUR }, end).score <= final.score);
  if (final.score === 0) idle++;
  best = Math.max(best, final.score);
}
assert.ok(idle / SEEDS > 0.1 && idle / SEEDS < 0.2, "khoảng 15% bot không đánh trận nào: " + idle / SEEDS);
assert.ok(best < 36 * params.anchorOwn, "người thật đánh hết 36 trận với damage trung bình luôn hơn mọi bot: " + best);
assert.strictEqual(botProgress(1, { ...params, anchorOwn: 0 }, end).score, 0, "chưa có mốc → bot chưa hoạt động");
assert.strictEqual(botProgress(1, { ...params, botStartAt: 0 }, end).score, 0);

const used = new Set(["alice"]);
const bots = generateBots(12345, 59, used, 50000000, loadBank());
assert.strictEqual(bots.length, 59);
assert.strictEqual(new Set(bots.map((b) => b.name.toLowerCase())).size, 59, "tên bot không trùng nhau");
assert.strictEqual(new Set(bots.map((b) => b.id)).size, 59);
assert.ok(bots.every((b) => b.id.length === 28 && b.name.toLowerCase() !== "alice" && b.basePower > 0));
const defaults = bots.filter((b) => /^Player\d{6}$/.test(b.name)).length;
assert.ok(defaults >= 5 && defaults <= 28, "một phần bot giữ tên mặc định: " + defaults);
assert.deepStrictEqual(generateBots(12345, 59, new Set(["alice"]), 50000000, loadBank()), bots, "cùng seed sảnh → cùng bot");
for (const b of bots) {
  let last = 0;
  for (let t = start; t <= end; t += 12 * HOUR) {
    const power = botPower(b, params, t, loadBank());
    assert.ok(power >= last, "power bot không giảm");
    last = power;
  }
}

// ----- Sảnh: người thật + bot cho đủ 60, bảng xếp hạng gộp -----
const member = (uid: string, power: number): PlayerDoc => ({
  uid, playerName: uid, power, items: [], companions: [], enchantmentTiers: [], tier: 1, currentPoolId: "", lastJoinEventKey: "",
  tickets: { freeRemaining: 3, adRemaining: 3, adClaimedToday: 0, dailyFreeTickets: 3, dayKey: "" }, activeFight: null,
  updatedAt: 0, avgOwn: 2000000, avgTeam: 16000000,
});
const pool = createPool("pool-a", "2026-10-06", 1, [member("u1", 40000000), member("u2", 60000000)], false, start, null);
assert.strictEqual(pool.bots.length, 58, "2 người thật + 58 bot = 60");
assert.strictEqual(pool.anchorOwn, 2000000);
assert.strictEqual(pool.anchorTeamRatio, 8);
assert.strictEqual(pool.botStartAt, start, "có mốc từ đầu → bot tính từ đầu mùa");
assert.strictEqual(createPool("pool-b", "2026-10-06", 1, [{ ...member("u3", 1), avgOwn: 0 }], true, start, null).botStartAt, 0);
assert.strictEqual(createPool("pool-c", "2026-10-06", 1, Array.from({ length: 70 }, (_, i) => member("m" + i, 5)), false, start, null).bots.length, 0);
pool.players.u1.TotalDamagePoints = 1e15;
const ranked = rankPool(pool, end, loadBank());
assert.strictEqual(ranked.length, 60);
assert.deepStrictEqual(ranked.map((e) => e.position), Array.from({ length: 60 }, (_, i) => i + 1));
assert.strictEqual(ranked[0].id, "u1");
assert.deepStrictEqual(Object.keys(toRows(ranked)[0]).sort(), ["AvatarId", "PlayerName", "Position", "Power", "TotalDamagePoints", "UserId"],
  "dòng gửi về client không có cờ bot, không kèm bộ đồ");
assert.ok(poolBossState(pool, end).bossesKilled >= poolBossState(pool, start + 24 * HOUR).bossesKilled);

// ----- Ngân hàng bộ đồ thật (functions/data/botBank.json — sinh bằng công cụ Unity "Generate Bot Bank") -----
const realBank = [...loadBank().values()];
if (realBank.length > 0) {
  for (const c of realBank) {
    assert.strictEqual(c.steps.length, 7, c.id + ": 7 mốc (đầu mùa + 6 ngày)");
    for (let i = 0; i < c.steps.length; i++) {
      assert.ok(c.steps[i].power > 0 && c.steps[i].items.length > 0 && c.steps[i].companions.length > 0, c.id);
      assert.ok(i === 0 || c.steps[i].power >= c.steps[i - 1].power, c.id + ": power không giảm");
    }
  }
  const sample = generateBots(4242, 59, new Set(), 5000000000, loadBank());
  assert.strictEqual(new Set(sample.map((b) => b.chain)).size, 59, "59 bot của một sảnh mặc 59 bộ đồ khác nhau");
}

// ----- Ngân hàng giả để kiểm cách chọn chuỗi -----
const chain = (id: string, power: number): BankChain => ({
  id, steps: [0, 1, 2].map((i) => ({ power: power * (i + 1), items: [], companions: [], enchantmentTiers: [], showCloak: true })),
});
setBankForTest([chain("a", 40000000), chain("b", 50000000), chain("c", 70000000)]);
const banked = generateBots(999, 3, new Set(), 50000000, loadBank());
assert.strictEqual(new Set(banked.map((b) => b.chain)).size, 3, "mỗi bot một chuỗi bộ đồ, không trùng trong sảnh");
for (const b of banked) {
  const steps = loadBank().get(b.chain)?.steps ?? [];
  assert.strictEqual(b.basePower, steps[0].power);
  assert.ok(steps.some((s) => s.power === botPower(b, params, end, loadBank())), "power bot = power của một bộ đồ trong chuỗi");
}

console.log("bossRush design OK");
