import { Firestore } from "firebase-admin/firestore";

// Hằng số Clan + Clan War. [CHẮC] = reverse client gốc v41 (DecodedData/CLAN_MODEL.md); [SUY] = server gốc giấu → tự đặt,
// ghi đè được bằng Firestore document `config/clanWar` (cùng shape với ClanWarConfigDTO + các field tier/bot dưới đây).

// ===== Clan [CHẮC] =====
export const CLAN_MAX_MEMBERS = 50;          // ik.vhs
export const CLAN_NAME_MIN = 3;              // ik.vht
export const CLAN_MAX_CAPTAINS = 3;          // ik.vhu
export const CLAN_NAME_MAX = 15;             // ik.vhv
export const CLAN_ANNOUNCEMENT_MAX = 300;    // EditAnnouncementPopup characterLimit
export const CLAN_DESCRIPTION_MAX = 200;     // ClanSettingsPopup characterLimit
export const CLAN_SEARCH_LIMIT = 20;         // ik.vhy
export const CLAN_DAILY_JOIN_REQUESTS = 5;   // ik.via
export const CLAN_JOIN_COOLDOWN_SECONDS = 24 * 3600; // Errors.Clan.JoinCooldown "24 hours"
export const CLAN_NAME_REGEX = /^[A-Za-z0-9]+$/;
export const BANNER_BG_TYPES = 8;
export const BANNER_BG_COLORS = 10;
export const BANNER_IMAGE_TYPES = 20;
export const BANNER_IMAGE_COLORS = 8;

// ===== Tier clan [SUY]: theo tierPoints (= tổng War Score các tuần) =====
export const TIERS: { tier: string; minPoints: number }[] = [
  { tier: "S", minPoints: 60 },
  { tier: "A", minPoints: 35 },
  { tier: "B", minPoints: 18 },
  { tier: "C", minPoints: 7 },
  { tier: "D", minPoints: 0 },
];

export function tierOf(points: number): string {
  for (const t of TIERS) if (points >= t.minPoints) return t.tier;
  return "D";
}

// ===== Clan War =====
// Lịch [CHẮC]: Day1 = Thứ Ba ... Day6 = Chủ Nhật (PvP), Day7 = Thứ Hai (cooldown).
export const LOOT_RARITIES = ["common", "uncommon", "rare", "epic", "legendary", "mythic", "artifact", "ancient", "immortal", "divine"];
export const SUMMON_RARITIES = ["common", "uncommon", "rare", "epic", "legendary", "mythic"];
export const ORES = ["stone", "coal", "iron", "ruby", "emerald", "gold", "diamond"];
export const ODD_DAY_SOURCES = ["lootEquipment", "summonCompanion", "levelUp"];   // ClanWarController.vql
export const EVEN_DAY_SOURCES = ["mining", "dungeonKey", "summonCape"];         // ClanWarController.vqm
export const MAX_ACTIONS_PER_BATCH = 200;                                       // ClanWarController.vpk
export const BATTLE_TOKEN_TTL_SECONDS = 180;

export interface ClanWarRewardsDTO {
  lootBox: number; bones: number; pickaxe: number; experience: number;
  cloakCurrency: number; goldenPickaxe: number; drill: number; vial: number;
}

export interface ClanWarMilestoneRowDTO {
  threshold: number; lootBox: number; bones: number; pickaxe: number; experience: number; cloakCurrency: number;
}

export interface ClanWarAwardsDTO {
  lootEquipment: Record<string, number>;
  summonCompanion: Record<string, number>;
  levelUpMultiplier: number;
  mining: Record<string, number>;
  dungeonKey: number;
  summonCape: Record<string, number>;
  pvpWin: number;
}

export interface ClanWarPvpConfigDTO {
  startTickets: number; winPoints: number; maxRoundResets: number; minMembersForReset: number;
}

export interface ClanWarConfigDTO {
  sourcesByDay: Record<string, string[]>;
  awards: ClanWarAwardsDTO;
  individualMilestones: ClanWarMilestoneRowDTO[];
  clanRewards: Record<string, Record<string, ClanWarRewardsDTO>>;
  warScoreByDay: Record<string, number>;
  pvp: ClanWarPvpConfigDTO;
}

function rw(p: Partial<ClanWarRewardsDTO>): ClanWarRewardsDTO {
  return { lootBox: 0, bones: 0, pickaxe: 0, experience: 0, cloakCurrency: 0, goldenPickaxe: 0, drill: 0, vial: 0, ...p };
}

function ms(threshold: number, lootBox: number, bones: number, pickaxe: number, experience: number, cloakCurrency: number): ClanWarMilestoneRowDTO {
  return { threshold, lootBox, bones, pickaxe, experience, cloakCurrency };
}

// Mặc định [SUY] — số gốc nằm ở server Lava Labs, client không có. dungeonKey 500 = fallback client gốc (gbo).
export const DEFAULT_CONFIG: ClanWarConfigDTO = {
  sourcesByDay: {
    "1": ODD_DAY_SOURCES, "2": EVEN_DAY_SOURCES, "3": ODD_DAY_SOURCES, "4": EVEN_DAY_SOURCES, "5": ODD_DAY_SOURCES,
  },
  awards: {
    lootEquipment: {
      common: 1, uncommon: 2, rare: 5, epic: 10, legendary: 25, mythic: 50, artifact: 100, ancient: 200, immortal: 400, divine: 800,
    },
    summonCompanion: { common: 10, uncommon: 20, rare: 50, epic: 100, legendary: 250, mythic: 500 },
    levelUpMultiplier: 100,                                  // [CHẮC] "New Level x 100 pts"
    mining: { stone: 1, coal: 2, iron: 4, ruby: 8, emerald: 15, gold: 30, diamond: 60 },
    dungeonKey: 500,                                         // [CHẮC] fallback client
    summonCape: { common: 10, uncommon: 20, rare: 50, epic: 100, legendary: 250, mythic: 500 },
    pvpWin: 1000,
  },
  individualMilestones: [
    ms(1000, 20, 20, 0, 50, 0),
    ms(3000, 40, 40, 0, 100, 5),
    ms(6000, 60, 60, 0, 200, 10),
    ms(10000, 80, 80, 0, 300, 15),
    ms(15000, 100, 100, 0, 500, 20),
    ms(25000, 150, 150, 0, 800, 30),
    ms(40000, 200, 200, 0, 1200, 40),
    ms(60000, 300, 300, 0, 2000, 60),
  ],
  clanRewards: {
    S: { win: rw({ lootBox: 600, bones: 600, cloakCurrency: 150, vial: 150 }), lose: rw({ lootBox: 300, bones: 300, cloakCurrency: 75, vial: 75 }) },
    A: { win: rw({ lootBox: 450, bones: 450, cloakCurrency: 110, vial: 110 }), lose: rw({ lootBox: 225, bones: 225, cloakCurrency: 55, vial: 55 }) },
    B: { win: rw({ lootBox: 320, bones: 320, cloakCurrency: 80, vial: 80 }), lose: rw({ lootBox: 160, bones: 160, cloakCurrency: 40, vial: 40 }) },
    C: { win: rw({ lootBox: 220, bones: 220, cloakCurrency: 55, vial: 55 }), lose: rw({ lootBox: 110, bones: 110, cloakCurrency: 25, vial: 25 }) },
    D: { win: rw({ lootBox: 150, bones: 150, cloakCurrency: 35, vial: 35 }), lose: rw({ lootBox: 75, bones: 75, cloakCurrency: 15, vial: 15 }) },
  },
  warScoreByDay: { "1": 1, "2": 1, "3": 1, "4": 1, "5": 1, "6": 2 },
  pvp: { startTickets: 5, winPoints: 1000, maxRoundResets: 2, minMembersForReset: 30 },   // minMembersForReset 30 [CHẮC]
};

export async function loadConfig(db: Firestore): Promise<ClanWarConfigDTO> {
  try {
    const snap = await db.collection("config").doc("clanWar").get();
    const o = snap.data() as Partial<ClanWarConfigDTO> | undefined;
    if (!o) return DEFAULT_CONFIG;
    return {
      sourcesByDay: o.sourcesByDay ?? DEFAULT_CONFIG.sourcesByDay,
      awards: { ...DEFAULT_CONFIG.awards, ...(o.awards ?? {}) },
      individualMilestones: o.individualMilestones ?? DEFAULT_CONFIG.individualMilestones,
      clanRewards: o.clanRewards ?? DEFAULT_CONFIG.clanRewards,
      warScoreByDay: o.warScoreByDay ?? DEFAULT_CONFIG.warScoreByDay,
      pvp: { ...DEFAULT_CONFIG.pvp, ...(o.pvp ?? {}) },
    };
  } catch {
    return DEFAULT_CONFIG;
  }
}

// ===== Lịch tuần (fze/fzf gốc) =====
const DAY_MS = 86400000;

// Day 1..7 của tuần war (1 = Thứ Ba).
export function warDayOf(now: Date): number {
  return ((now.getUTCDay() + 5) % 7) + 1;
}

export function weekStartOf(now: Date): Date {
  const d = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  return new Date(d.getTime() - ((now.getUTCDay() + 5) % 7) * DAY_MS);
}

export function weekIdOf(now: Date): string {
  return weekStartOf(now).toISOString().substring(0, 10);
}

export function dayEndsAtSeconds(now: Date): number {
  const d = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  return Math.floor((d + DAY_MS) / 1000);
}

export function nextWeekStartsAtSeconds(now: Date): number {
  return Math.floor((weekStartOf(now).getTime() + 7 * DAY_MS) / 1000);
}

// Nguồn điểm hợp lệ trong ngày (fzg gốc).
export function sourcesForDay(config: ClanWarConfigDTO, day: number): string[] {
  if (day < 1 || day > 5) return [];
  const list = config.sourcesByDay?.[String(day)];
  if (list && list.length > 0) return list;
  return day % 2 === 1 ? ODD_DAY_SOURCES : EVEN_DAY_SOURCES;
}

export function emptyRewards(): ClanWarRewardsDTO {
  return rw({});
}

export function addRewards(a: ClanWarRewardsDTO, b: Partial<ClanWarRewardsDTO>): ClanWarRewardsDTO {
  return {
    lootBox: a.lootBox + (b.lootBox ?? 0), bones: a.bones + (b.bones ?? 0), pickaxe: a.pickaxe + (b.pickaxe ?? 0),
    experience: a.experience + (b.experience ?? 0), cloakCurrency: a.cloakCurrency + (b.cloakCurrency ?? 0),
    goldenPickaxe: a.goldenPickaxe + (b.goldenPickaxe ?? 0), drill: a.drill + (b.drill ?? 0), vial: a.vial + (b.vial ?? 0),
  };
}

export function milestoneRewards(row: ClanWarMilestoneRowDTO): ClanWarRewardsDTO {
  return rw({ lootBox: row.lootBox, bones: row.bones, pickaxe: row.pickaxe, experience: row.experience, cloakCurrency: row.cloakCurrency });
}
