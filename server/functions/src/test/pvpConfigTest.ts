import * as assert from "assert";
import { getLeagueIndex, lossDelta, projectedLoss, projectedWin, roundHalfEven, winDelta } from "../pvpConfig";

// League theo trophy (PvPConfig.jkp): kẹp ≥ 0, biên Min/Max.
assert.strictEqual(getLeagueIndex(-50), 1);
assert.strictEqual(getLeagueIndex(1000), 1);
assert.strictEqual(getLeagueIndex(1199), 1);
assert.strictEqual(getLeagueIndex(1200), 2);
assert.strictEqual(getLeagueIndex(2800), 10);
assert.strictEqual(getLeagueIndex(99999), 10);

// Làm tròn kiểu .NET (nửa về số chẵn).
assert.strictEqual(roundHalfEven(2.5), 2);
assert.strictEqual(roundHalfEven(3.5), 4);
assert.strictEqual(roundHalfEven(-2.5), -2);
assert.strictEqual(roundHalfEven(-3.5), -4);

// Elo jkx/jky: ngang trophy ở Iron (KWin 40, KLoss 10) → +20 / −5.
assert.strictEqual(winDelta(1000, 1000), 20);
assert.strictEqual(lossDelta(1000, 1000), -5);
// Đối thủ mạnh hơn nhiều → thắng gần đủ K, thua tối thiểu −1.
assert.strictEqual(winDelta(1000, 2000), 40);
assert.strictEqual(lossDelta(1000, 2000), -1);
// Đối thủ yếu hơn nhiều → thắng tối thiểu +1, thua gần đủ K.
assert.strictEqual(winDelta(1000, 0), 1);
assert.strictEqual(lossDelta(1000, 0), -10);
// Gold (KWin 30, KLoss 15).
assert.strictEqual(winDelta(1700, 1700), 15);
assert.strictEqual(lossDelta(1700, 1700), -8);   // −7.5 → −8 (nửa về chẵn)
// jkz/jla kẹp ≥ 0.
assert.strictEqual(projectedLoss(3, 3), 0);
assert.strictEqual(projectedWin(1000, 1000), 1020);
console.log("pvpConfig OK");
