# -*- coding: utf-8 -*-
# Máy tính chỉ số enemy DungeonRush — CÔNG THỨC GỐC (reverse từ libil2cpp.so bản 41).
# Hằng số/preset đọc từ gameresources_values.json. Chi tiết: ENEMY_STATS_MODEL.md
#
# 2 chế độ:
#   CAMPAIGN (main)  : combatLevel = level THÔ, preset = ManualPresets(1..10) rồi ArmyPresets shuffle.
#   DUNGEON (Z/D/C)  : combatLevel = base+(level-1)*3, preset riêng từng dungeon.
import math

# ===== Hằng số thật =====
ARMY_POWER_BASE = 500.0
EXP_SCALER = 3.16227770            # √10 -> scaler^(2x) = 10^x
SEGMENTS = [(80, 20.0), (200, 30.0), (999999, 40.0)]
RATIO = {"melee": 3.0, "ranged": 2.0, "boss": 10.0}
RANGED_DMG_MULT = 0.8
PRESET_SEED = 42
DUNGEON_BASE = {"dragon": 1, "zombie": 5, "cultist": 60}
LEVEL_MULT = 3

# (melee, ranged, lancaster)
MANUAL_PRESETS = [(1,0,1.0),(2,0,1.0),(0,1,1.0),(0,2,1.3),(1,1,1.0),
                  (1,2,1.5),(2,2,1.5),(3,3,1.5),(0,5,2.0),(5,0,1.0)]
ARMY_PRESETS   = [(1,0,1.0),(0,1,1.2),(1,1,1.0),(3,0,1.2),(0,3,1.5),
                  (6,0,1.2),(0,6,1.8),(2,2,1.5),(3,3,1.5)]
ZOMBIE_PRESETS  = [(6,0,1.0),(8,0,1.0),(10,0,1.0),(12,0,1.0)]
CULTIST_PRESETS = [(2,3,1.5),(3,4,1.5),(3,3,1.5),(3,2,1.5),(4,3,1.5)]


# ===== System.Random bản Mono/.NET Framework (để shuffle campaign khớp gốc) =====
class NetRandom:
    MBIG = 2147483647
    def __init__(self, seed):
        self.a = [0]*56
        sub = 2147483647 if seed == -2147483648 else abs(seed)
        mj = 161803398 - sub; self.a[55] = mj; mk = 1
        for i in range(1, 55):
            ii = (21*i) % 55; self.a[ii] = mk; mk = mj - mk
            if mk < 0: mk += self.MBIG
            mj = self.a[ii]
        for _ in range(1, 5):
            for i in range(1, 56):
                self.a[i] -= self.a[1 + (i+30) % 55]
                if self.a[i] < 0: self.a[i] += self.MBIG
        self.inext = 0; self.inextp = 21
    def _sample(self):
        ni = self.inext + 1;  ni = 1 if ni >= 56 else ni
        np = self.inextp + 1; np = 1 if np >= 56 else np
        r = self.a[ni] - self.a[np]
        if r == self.MBIG: r -= 1
        if r < 0: r += self.MBIG
        self.a[ni] = r; self.inext = ni; self.inextp = np
        return r
    def next(self, mn, mx):
        return int((self._sample() * (1.0/self.MBIG)) * (mx - mn)) + mn


def hck(L):
    acc, prev = 0.0, 0
    for th, sc in SEGMENTS:
        if th >= L: return acc + (L - prev)/sc
        acc += (th - prev)/sc; prev = th
    return acc

def total_army_power(combat_level):
    return ARMY_POWER_BASE * (EXP_SCALER ** (2.0 * hck(combat_level)))

def rnd(x):
    return max(1, round(x))

def unit_stats(per_unit, role):
    r = RATIO[role]
    d = math.sqrt(per_unit / r) * (RANGED_DMG_MULT if role == "ranged" else 1.0)
    return rnd(d), rnd(math.sqrt(per_unit * r))


# ===== CAMPAIGN =====
def campaign_preset(level):
    if level <= len(MANUAL_PRESETS):
        return MANUAL_PRESETS[level - 1]
    n = level - len(MANUAL_PRESETS)          # jhe(n)
    c = len(ARMY_PRESETS)
    rng = NetRandom(PRESET_SEED + (n - 1)//c)
    idx = list(range(c))
    for i in range(c - 1, 0, -1):
        j = rng.next(0, i + 1); idx[i], idx[j] = idx[j], idx[i]
    return ARMY_PRESETS[idx[(n - 1) % c]]

def campaign_stage(chapter, stage_in_chapter, stages_per_chapter=10):
    """Trả (level, preset, [(role,dmg,hp)...]) cho màn campaign 'chapter-stage'."""
    level = (chapter - 1) * stages_per_chapter + stage_in_chapter
    m, rg, lanc = campaign_preset(level)
    uc = m + rg
    per = total_army_power(level) / (uc ** lanc)
    units = []
    md, mh = unit_stats(per, "melee")
    rd, rh = unit_stats(per, "ranged")
    units += [("melee", md, mh)] * m + [("ranged", rd, rh)] * rg
    return level, (m, rg, lanc), units


if __name__ == "__main__":
    print("===== CAMPAIGN (main) — chương 1..4 =====")
    print(f"{'màn':>6} {'lvl':>3} {'preset':>7} {'melee d/h':>13} {'ranged d/h':>13}")
    for ch in range(1, 5):
        for st in range(1, 11):
            level, (m, rg, lanc), units = campaign_stage(ch, st)
            md = next((f"{d}/{h}" for r,d,h in units if r=="melee"), "-")
            rd = next((f"{d}/{h}" for r,d,h in units if r=="ranged"), "-")
            pv = (f"{m}M" if m else "") + ("+" if m and rg else "") + (f"{rg}R" if rg else "")
            print(f"{ch}-{st:<4} {level:>3} {pv:>7} {md:>13} {rd:>13}")

    print("\n===== Ví dụ hỏi cụ thể: NORMAL 3-6 =====")
    level, (m, rg, lanc), units = campaign_stage(3, 6)
    print(f"level {level}, preset {m}M+{rg}R (Lanc {lanc}), {len(units)} lính")
    for r, d, h in units[:1]:
        print(f"  mỗi lính {r}: damage {d}, HP {h}")
