import * as assert from "assert";
import {
  DEFAULT_PVP_CONFIG, assignStars, computeNewLeague, demoteCount, pickOpponents, promoteCount, seasonRewards, seasonRewardTable,
} from "../pvpConfig";
import { botTrophy, createGroup, finalizeGroup, rankGroup } from "../pvpGroup";

const config = DEFAULT_PVP_CONFIG;
const amounts = (league: number, rank: number) => seasonRewards(league, rank).map((r) => r.Amount);

// Thưởng mùa: đối chiếu ảnh game thật (Gem, Lootbox, Bone, Cloak).
assert.deepStrictEqual(amounts(1, 1), [179, 716, 1074, 537]);
assert.deepStrictEqual(amounts(1, 100), [50, 200, 300, 150]);
assert.deepStrictEqual(amounts(2, 4), [103, 412, 618, 309]);    // bậc làm tròn dần, không phải 104
assert.deepStrictEqual(amounts(4, 22), [124, 496, 744, 372]);   // ảnh Season Complete: Gold hạng 22
assert.deepStrictEqual(amounts(5, 51), [103, 412, 618, 309]);
assert.deepStrictEqual(amounts(10, 1), [924, 3696, 5544, 2772]);
assert.deepStrictEqual(amounts(10, 50), [310, 1240, 1860, 930]);
assert.deepStrictEqual(amounts(1, 101), []);
assert.strictEqual(seasonRewardTable().length, 10);
assert.strictEqual(seasonRewardTable()[0].Bands.length, 7);

// Vạch lên/xuống: Iron 100 người lên 40 người, không xuống (ảnh game thật). Legend không lên.
assert.strictEqual(promoteCount(config, 1, 100), 40);
assert.strictEqual(demoteCount(config, 1, 100), 0);
assert.strictEqual(promoteCount(config, 5, 27), 7);
assert.strictEqual(demoteCount(config, 5, 27), 4);
assert.strictEqual(promoteCount(config, 10, 100), 0);

// League mùa sau.
assert.strictEqual(computeNewLeague(config, 1, 40, 100, 3), 2);
assert.strictEqual(computeNewLeague(config, 1, 41, 100, 3), 1);
assert.strictEqual(computeNewLeague(config, 1, 100, 100, 0), 1);     // Iron không xuống
assert.strictEqual(computeNewLeague(config, 1, 1, 100, 0), 1);       // 0 cúp không được lên
assert.strictEqual(computeNewLeague(config, 5, 23, 30, 2), 5);
assert.strictEqual(computeNewLeague(config, 5, 27, 30, 0), 4);
assert.strictEqual(computeNewLeague(config, 10, 1, 100, 9), 10);     // Legend không lên nữa

// Chọn đối thủ theo cúp gần mình, có cả trên lẫn dưới.
const person = (id: string, trophy: number, power = 1) => ({ id, trophy, power });
const fixed = () => 0;
const crowd = [person("a", 10), person("b", 11), person("c", 12), person("d", 13), person("e", 14), person("f", 15), person("low", 2)];
const picked = pickOpponents(crowd, 10, fixed);
assert.strictEqual(picked.length, 5);
assert.ok(picked.some((p) => p.trophy > 10));
assert.ok(picked.some((p) => p.trophy < 10), "phải có người dưới cúp mình");
assert.strictEqual(pickOpponents([person("x", 5), person("y", 6)], 0, fixed).length, 2);
assert.strictEqual(pickOpponents([], 0, fixed).length, 0);

// Phát cúp: nhiều cúp nhất +5, cùng cúp thì mạnh hơn được nhiều hơn; ít người thì từ +5 xuống.
const stars = assignStars([person("a", 1, 9), person("b", 3, 1), person("c", 1, 50)]);
assert.deepStrictEqual(stars.map((s) => [s.opponent.id, s.stars]), [["b", 5], ["c", 4], ["a", 3]]);
assert.deepStrictEqual(
  assignStars([person("a", 5), person("b", 4), person("c", 3), person("d", 2), person("e", 1)]).map((s) => s.stars), [5, 4, 3, 2, 1]);

// Cúp của bot chỉ tăng theo số trận.
assert.strictEqual(botTrophy(123, 0), 0);
for (let f = 1; f < 30; f++) assert.ok(botTrophy(123, f) >= botTrophy(123, f - 1));

// Nhóm: bù bot đủ số tối thiểu, xếp theo cúp rồi sức mạnh, chốt theo mùa.
const bank = new Map();
const entry = (id: string, trophy: number, power: number, fights = 1) => ({
  UserId: id, PlayerName: id, AvatarId: 0, Power: power, Trophy: trophy, Fights: fights, JoinedAt: 0,
});
const group = createGroup("2026-11-03_L1_1", "2026-11-03", 1, [entry("me", 0, 100)], 30, 0, bank);
assert.strictEqual(group.bots.length, 29);
assert.strictEqual(createGroup("g2", "2026-11-03", 1, [], 0, 0, bank).bots.length, 0);

const small = createGroup("g3", "2026-11-03", 1, [entry("x", 7, 1), entry("y", 7, 9), entry("z", 9, 1), entry("idle", 0, 5, 0)], 0, 0, bank);
assert.deepStrictEqual(rankGroup(small, 0, bank).map((e) => e.id), ["z", "y", "x", "idle"]);
const results = finalizeGroup(small, config, bank);
assert.strictEqual(results.z.rank, 1);
assert.strictEqual(results.z.newLeague, 2);          // 40% của 4 người = 2 người lên
assert.strictEqual(results.y.newLeague, 2);
assert.strictEqual(results.x.newLeague, 1);
assert.strictEqual(results.idle.fights, 0);

console.log("pvpConfig OK");
