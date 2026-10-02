// Lịch sự kiện Boss Rush — port 1:1 class `fn` của game gốc (libil2cpp v41), mọi mốc theo UTC.
//   ely: đang mở ⇔ KHÔNG phải Thứ 2 (Thứ 2 = ngày chốt/nhận thưởng).
//   ema: ngày bắt đầu đợt = Date - ((dow + 5) % 7) ngày  → luôn rơi vào Thứ 3 00:00.
//   emb: kết thúc = bắt đầu + 6 ngày - 1 giây           → Chủ nhật 23:59:59.
//   emc: đợt kế = Thứ 2 → +1 ngày; ngày khác → bắt đầu đợt hiện tại + 7 ngày.
//   elz: eventKey = "yyyy-MM-dd" của ngày bắt đầu (rỗng nếu đang nghỉ).
// DayOfWeek giống .NET: 0 = Chủ nhật ... 6 = Thứ 7.

const DAY_MS = 24 * 60 * 60 * 1000;
const MONDAY = 1;

function utcDate(d: Date): Date {
  return new Date(Date.UTC(d.getUTCFullYear(), d.getUTCMonth(), d.getUTCDate()));
}

function formatDay(d: Date): string {
  return d.toISOString().substring(0, 10);
}

export function isEventActive(now: Date): boolean {
  return now.getUTCDay() !== MONDAY;
}

// Ngày bắt đầu đợt hiện tại (đang mở) hoặc đợt kế (nếu đang Thứ 2).
export function getEventStart(now: Date): Date {
  if (!isEventActive(now)) {
    return getNextEventStart(now);
  }
  const offset = (now.getUTCDay() + 5) % 7;
  return new Date(utcDate(now).getTime() - offset * DAY_MS);
}

export function getEventEnd(now: Date): Date {
  return new Date(getEventStart(now).getTime() + 6 * DAY_MS - 1000);
}

export function getNextEventStart(now: Date): Date {
  if (now.getUTCDay() === MONDAY) {
    return new Date(utcDate(now).getTime() + DAY_MS);
  }
  return new Date(getEventStart(now).getTime() + 7 * DAY_MS);
}

export function getEventKey(now: Date): string {
  return isEventActive(now) ? formatDay(getEventStart(now)) : "";
}

export function getNextEventKey(now: Date): string {
  return formatDay(getNextEventStart(now));
}

// Key ngày reset vé (UTC) — BossRushTicketsDTO.dayKey.
export function getDayKey(now: Date): string {
  return formatDay(now);
}
