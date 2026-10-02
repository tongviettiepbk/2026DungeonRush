import * as assert from "assert";
import { getEventEnd, getEventKey, getEventStart, getNextEventStart, isEventActive } from "../schedule";
import { getBossDamage, getBossHp } from "../config";

// Kiểm lịch tuần gốc: mở Thứ 3 00:00 → CN 23:59:59 UTC, Thứ 2 nghỉ.
const iso = (d: Date) => d.toISOString();
// 2026-10-06 là Thứ 3.
assert.strictEqual(isEventActive(new Date("2026-10-05T12:00:00Z")), false, "Thứ 2 nghỉ");
assert.strictEqual(getEventKey(new Date("2026-10-05T12:00:00Z")), "");
assert.strictEqual(iso(getNextEventStart(new Date("2026-10-05T12:00:00Z"))), "2026-10-06T00:00:00.000Z");
for (const day of ["2026-10-06", "2026-10-08", "2026-10-11"]) {
  const now = new Date(day + "T23:00:00Z");
  assert.strictEqual(getEventKey(now), "2026-10-06", day);
  assert.strictEqual(iso(getEventStart(now)), "2026-10-06T00:00:00.000Z");
  assert.strictEqual(iso(getEventEnd(now)), "2026-10-11T23:59:59.000Z");
  assert.strictEqual(iso(getNextEventStart(now)), "2026-10-13T00:00:00.000Z");
}
// Bảng gốc: T1 boss1 = 500M, T10 boss20 = 221.684T, kẹp ngoài biên.
assert.strictEqual(getBossHp(1, 1), 500000000);
assert.strictEqual(getBossHp(20, 10), 221684000000000);
assert.strictEqual(getBossHp(25, 0), getBossHp(20, 1));
assert.strictEqual(getBossDamage(1), 300);
assert.strictEqual(getBossDamage(99), 21000);
console.log("schedule/config OK");
