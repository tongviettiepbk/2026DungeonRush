import * as fs from "fs";
import * as path from "path";
import { BossRushCompanionModel, BossRushItemModel, BotSeed } from "./models";

// BOT Boss Rush (server/BOSS_RUSH_DESIGN.md mục 11). Bot KHÔNG "chạy": không tiến trình, không tác vụ định kỳ,
// không ghi database sau khi tạo. Mỗi bot chỉ là một hạt giống (BotSeed); điểm, damage lên boss, sức mạnh tại một
// thời điểm đều là HÀM THUẦN của (seed, mốc của sảnh, giờ hiện tại) → ai đọc lúc nào cũng ra cùng một số, điểm chỉ
// tăng không giảm. Đổi công thức thì tăng BOT_ALGO_VERSION và giữ nguyên nhánh cũ tới hết mùa.

export const BOT_ALGO_VERSION = 1;

const HOUR_MS = 60 * 60 * 1000;
const DAY_MS = 24 * HOUR_MS;
const SEASON_DAYS = 6;                 // Thứ 3 → Chủ nhật
// Trần sức đánh của bot "cày": người thật đánh hết vé + quảng cáo với damage trung bình luôn hơn mọi bot.
const TOP_EFFORT_CAP = 0.8;
const JITTER_MIN = 0.85;
const JITTER_RANGE = 0.3;

const SALT_TRAIT = 11;
const SALT_DAY = 23;
const SALT_LOGIN = 37;
const SALT_IDENTITY = 51;

// ===== Số ngẫu nhiên có hạt giống =====

function mix(a: number, b: number): number {
  let h = (a ^ Math.imul((b + 0x9e3779b9) | 0, 0x85ebca6b)) >>> 0;
  h ^= h >>> 16;
  h = Math.imul(h, 0x7feb352d) >>> 0;
  h ^= h >>> 15;
  h = Math.imul(h, 0x846ca68b) >>> 0;
  h ^= h >>> 16;
  return h >>> 0;
}

export function hashSeed(...parts: number[]): number {
  let h = 0x1234abcd;
  for (const p of parts) h = mix(h, p | 0);
  return h >>> 0;
}

export function hashString(s: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 0x01000193) >>> 0;
  }
  return h >>> 0;
}

// mulberry32
export function makeRng(seed: number): () => number {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function gauss(rng: () => number): number {
  const u = Math.max(rng(), 1e-9);
  const v = rng();
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}

function clamp(v: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, v));
}

// ===== Tính cách =====

// profile: 0 không chơi (~15%), 1 thỉnh thoảng (~35%), 2 đều đặn (~35%), 3 cày (~15%).
export interface BotTraits {
  profile: number;
  strength: number;      // hệ số damage mỗi trận so với mốc của sảnh
  startHour: number;     // giờ UTC bắt đầu khung chơi
  windowHours: number;   // độ dài khung chơi
  loginHour: number;     // giờ "đăng nhập" mỗi ngày (đổi bộ đồ)
}

export function botTraits(seed: number, maxFightsPerDay: number): BotTraits {
  const rng = makeRng(hashSeed(seed, SALT_TRAIT));
  const u = rng();
  const profile = u < 0.15 ? 0 : u < 0.5 ? 1 : u < 0.85 ? 2 : 3;
  const profileMax = Math.min([1, 2, 4, maxFightsPerDay][profile], maxFightsPerDay);
  // Điểm KHÔNG đi theo power: sức đánh lấy ngẫu nhiên riêng, bị chặn trần theo mức chăm.
  const strength = Math.min(clamp(Math.exp(0.5 * gauss(rng)), 0.35, 2.5), TOP_EFFORT_CAP * maxFightsPerDay / profileMax);
  const startHour = rng() * 24;
  return { profile, strength, startHour, windowHours: 2 + rng() * 6, loginHour: (startHour + 23.5) % 24 };
}

function fightsOnDay(rng: () => number, profile: number, maxFightsPerDay: number): number {
  const u = rng();
  let n = 0;
  if (profile === 1) n = u < 0.3 ? 0 : u < 0.7 ? 1 : 2;
  else if (profile === 2) n = rng() < 0.1 ? 0 : u < 0.2 ? 2 : u < 0.7 ? 3 : 4;
  else if (profile === 3) n = u < 0.2 ? 4 : u < 0.5 ? 5 : 6;
  return Math.min(n, maxFightsPerDay);
}

// ===== Điểm + damage lên boss =====

export interface BotParams {
  seasonStart: number;      // ms
  seasonEnd: number;        // ms
  botStartAt: number;       // ms — 0 = bot chưa hoạt động
  anchorOwn: number;
  anchorTeamRatio: number;
  maxFightsPerDay: number;
}

export interface BotProgress {
  score: number;   // tổng damage riêng → điểm xếp hạng
  team: number;    // tổng damage cả đội → trừ máu boss chung
  fights: number;
}

export function botProgress(seed: number, p: BotParams, now: number): BotProgress {
  const out: BotProgress = { score: 0, team: 0, fights: 0 };
  if (p.anchorOwn <= 0 || p.botStartAt <= 0) return out;

  const until = Math.min(now, p.seasonEnd);
  const traits = botTraits(seed, p.maxFightsPerDay);
  if (traits.profile === 0) return out;

  for (let day = 0; day < SEASON_DAYS; day++) {
    const dayStart = p.seasonStart + day * DAY_MS;
    if (dayStart > until) break;
    const rng = makeRng(hashSeed(seed, SALT_DAY, day));
    const count = fightsOnDay(rng, traits.profile, p.maxFightsPerDay);
    for (let k = 0; k < count; k++) {
      // Luôn rút đủ 3 số cho mỗi trận để kết quả không phụ thuộc trận nào được tính.
      const at = dayStart + ((traits.startHour + rng() * traits.windowHours) % 24) * HOUR_MS;
      const own = Math.floor(p.anchorOwn * traits.strength * (JITTER_MIN + JITTER_RANGE * rng()));
      const team = Math.floor(own * p.anchorTeamRatio * (JITTER_MIN + JITTER_RANGE * rng()));
      if (at < p.botStartAt || at > until) continue;
      out.score += own;
      out.team += team;
      out.fights++;
    }
  }
  return out;
}

// ===== Sức mạnh + bộ đồ =====

// Số lần bot đã "đăng nhập" (đổi bộ đồ) tính tới now — tối đa 1 lần/ngày như người thật gửi snapshot 1 lần/ngày.
export function botLoginDays(seed: number, p: BotParams, now: number): number[] {
  const until = Math.min(now, p.seasonEnd);
  const traits = botTraits(seed, p.maxFightsPerDay);
  const days: number[] = [];
  for (let day = 0; day < SEASON_DAYS; day++) {
    const at = p.seasonStart + day * DAY_MS + traits.loginHour * HOUR_MS;
    if (at > until) break;
    const rng = makeRng(hashSeed(seed, SALT_LOGIN, day));
    if (rng() < (traits.profile === 0 ? 0.35 : 0.85)) days.push(day);
  }
  return days;
}

// Power khi chưa có ngân hàng bộ đồ: tăng theo BƯỚC NHẢY (đa số ngày đứng yên, thỉnh thoảng nhảy mạnh).
function fallbackPower(bot: BotSeed, loginDays: number[]): number {
  let power = bot.basePower;
  for (const day of loginDays) {
    const rng = makeRng(hashSeed(bot.seed, SALT_LOGIN, day, 7));
    const u = rng();
    power *= u < 0.5 ? 1 : u < 0.85 ? 1.03 + 0.12 * rng() : 1.3 + 0.9 * rng();
  }
  return Math.floor(power);
}

export interface BankStep {
  power: number;
  items: BossRushItemModel[];
  companions: BossRushCompanionModel[];
  enchantmentTiers: number[];
  showCloak: boolean;
}

// 1 nhân vật mô phỏng: bộ đồ từng ngày trong mùa (sinh bằng công cụ Unity, power tính bằng công thức thật).
export interface BankChain {
  id: string;
  steps: BankStep[];
}

let bankCache: Map<string, BankChain> | null = null;

// Ngân hàng đóng gói kèm code (functions/data/botBank.json) → không tốn lượt đọc database. Chưa có file = rỗng.
export function loadBank(): Map<string, BankChain> {
  if (bankCache) return bankCache;
  bankCache = new Map();
  try {
    const file = path.join(__dirname, "..", "data", "botBank.json");
    const chains = JSON.parse(fs.readFileSync(file, "utf8")) as BankChain[];
    for (const c of chains) {
      if (c && c.id && Array.isArray(c.steps) && c.steps.length > 0) bankCache.set(c.id, c);
    }
  } catch {
    // chưa có ngân hàng
  }
  return bankCache;
}

export function setBankForTest(chains: BankChain[]): void {
  bankCache = new Map(chains.map((c) => [c.id, c]));
}

function bankStep(bot: BotSeed, loginDays: number[], bank: Map<string, BankChain>): BankStep | null {
  const chain = bot.chain ? bank.get(bot.chain) : undefined;
  return chain ? chain.steps[Math.min(loginDays.length, chain.steps.length - 1)] : null;
}

export function botPower(bot: BotSeed, p: BotParams, now: number, bank: Map<string, BankChain>): number {
  const loginDays = botLoginDays(bot.seed, p, now);
  const step = bankStep(bot, loginDays, bank);
  return step ? step.power : fallbackPower(bot, loginDays);
}

// Bộ đồ hiện tại của bot. Chưa có ngân hàng → trả null, nơi gọi dùng bộ đồ tạm.
export function botLoadout(bot: BotSeed, p: BotParams, now: number, bank: Map<string, BankChain>): BankStep | null {
  return bankStep(bot, botLoginDays(bot.seed, p, now), bank);
}

// ===== Danh tính =====

const FIRST = [
  "Milo", "Zeke", "Kira", "Nova", "Luna", "Axel", "Finn", "Koda", "Mika", "Taro", "Leo", "Max", "Sam", "Niko", "Yuki",
  "Hana", "Ravi", "Omar", "Ivan", "Lars", "Enzo", "Hugo", "Noah", "Liam", "Theo", "Aria", "Maya", "Zara", "Ines", "Rosa",
  "Dante", "Felix", "Oscar", "Bruno", "Pablo", "Diego", "Mateo", "Andre", "Tomas", "Erik", "Sven", "Olaf", "Bjorn", "Anton",
  "Pavel", "Milan", "Luka", "Marko", "Stefan", "Emre", "Kemal", "Deniz", "Arda", "Minh", "Tuan", "Linh", "Bao", "Khoa",
  "Hieu", "Duc", "Long", "Nam", "Phong", "Quan", "Son", "Trung", "Vinh", "Kenji", "Haru", "Sora", "Ren", "Daichi", "Jun",
  "Jae", "Hyun", "Wei", "Chen", "Feng", "Arjun", "Rohan", "Kiran", "Dev", "Amir", "Zaid", "Yusuf", "Karim", "Sami", "Tariq",
  "Artur", "Jax", "Nico", "Tobi", "Remy", "Iris", "Elsa", "Vera", "Otto", "Kai", "Zoe", "Ben", "Tim", "Joe", "Dan", "Alex",
];
const NOUN = [
  "Wolf", "Bear", "Tiger", "Hawk", "Fox", "Owl", "Crow", "Lynx", "Puma", "Shark", "Dragon", "Knight", "Ninja", "Wizard",
  "Hunter", "Slayer", "Reaper", "Sniper", "Rogue", "Mage", "Titan", "Phantom", "Bandit", "Pirate", "Samurai", "Viking",
  "Goblin", "Golem", "Potato", "Noodle", "Pickle", "Waffle", "Cookie", "Mochi", "Taco", "Panda", "Koala", "Otter", "Penguin",
  "Banana", "Shadow", "Raven", "Blaze", "Frost", "Viper", "Ghost", "Storm", "Ash", "Echo", "Onyx", "Drake", "Jinx", "Rex",
];
const ADJ = [
  "Dark", "Mad", "Lazy", "Angry", "Sleepy", "Lucky", "Tiny", "Big", "Salty", "Sneaky", "Epic", "Mighty", "Silent", "Crazy",
  "Fuzzy", "Happy", "Grumpy", "Iron", "Red", "Blue", "Black", "Old", "Lil", "Mr", "Sir", "King", "Lord", "Wild", "Swift",
];
const TAIL = ["GG", "TV", "Jr", "Pro", "X", "_x", "YT", "HD"];
const UID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

function pick<T>(rng: () => number, list: T[]): T {
  return list[Math.floor(rng() * list.length)];
}

function digits(rng: () => number, count: number): string {
  let s = "";
  for (let i = 0; i < count; i++) s += Math.floor(rng() * 10);
  return s;
}

function rollName(rng: () => number): string {
  // ~25% giữ tên mặc định của game: "Player" + 6 số (GameUtils.GetNewUserName ở client).
  if (rng() < 0.25) return "Player" + digits(rng, 6);
  const u = rng();
  let name: string;
  if (u < 0.24) name = pick(rng, FIRST);
  else if (u < 0.5) name = pick(rng, FIRST) + digits(rng, 1 + Math.floor(rng() * 4));
  else if (u < 0.7) name = pick(rng, ADJ) + pick(rng, NOUN);
  else if (u < 0.82) name = pick(rng, NOUN) + digits(rng, 1 + Math.floor(rng() * 3));
  else if (u < 0.92) name = pick(rng, FIRST) + (rng() < 0.5 ? "_" : "") + pick(rng, NOUN);
  else if (u < 0.96) name = "The" + pick(rng, NOUN);
  else name = pick(rng, FIRST) + pick(rng, TAIL);
  return rng() < 0.2 ? name.toLowerCase() : name;
}

function makeName(rng: () => number, used: Set<string>): string {
  for (let i = 0; i < 20; i++) {
    const name = rollName(rng);
    if (!used.has(name.toLowerCase())) {
      used.add(name.toLowerCase());
      return name;
    }
  }
  const name = pick(rng, FIRST) + digits(rng, 5);
  used.add(name.toLowerCase());
  return name;
}

function makeUid(rng: () => number): string {
  let s = "";
  for (let i = 0; i < 28; i++) s += UID_CHARS[Math.floor(rng() * UID_CHARS.length)];
  return s;
}

// Chuỗi bộ đồ có power đầu mùa gần mục tiêu nhất (lấy ngẫu nhiên trong 8 chuỗi gần nhất, không trùng trong sảnh).
function pickChain(bank: Map<string, BankChain>, target: number, rng: () => number, used: Set<string>): BankChain | null {
  if (bank.size === 0 || target <= 0) return null;
  const near = [...bank.values()]
    .filter((c) => !used.has(c.id) && c.steps[0].power > 0)
    .map((c) => ({ c, d: Math.abs(Math.log(c.steps[0].power / target)) }))
    .sort((a, b) => a.d - b.d)
    .slice(0, 8);
  if (near.length === 0) return null;
  const chosen = pick(rng, near).c;
  used.add(chosen.id);
  return chosen;
}

// Tạo bot cho một sảnh. usedNames: tên (chữ thường) của người thật trong sảnh — bot không được trùng.
export function generateBots(
  poolSeed: number, count: number, usedNames: Set<string>, anchorPower: number, bank: Map<string, BankChain>,
): BotSeed[] {
  const bots: BotSeed[] = [];
  const usedChains = new Set<string>();
  for (let i = 0; i < count; i++) {
    const seed = hashSeed(poolSeed, i + 1);
    const rng = makeRng(hashSeed(seed, SALT_IDENTITY));
    const id = makeUid(rng);
    const name = makeName(rng, usedNames);
    // Sức mạnh trải rộng quanh người thật trong sảnh (sảnh thật ở game gốc: 40M..629M cùng một league).
    const target = Math.max(1, anchorPower) * clamp(Math.exp(0.9 * gauss(rng)), 0.25, 8);
    const chain = pickChain(bank, target, rng, usedChains);
    bots.push({
      id, name, avatarId: 0, seed,
      chain: chain ? chain.id : "",
      basePower: chain ? chain.steps[0].power : Math.floor(target),
    });
  }
  return bots;
}
