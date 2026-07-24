#!/usr/bin/env python3
# Doi chieu 72 asset SINH RA voi asset DECODE GOC (AssetRipper) - recheck ky.
import os, re, sys, glob

SRC = r"E:\00Work\00Project\2026DungOnRush\AssetRipper\ExportedProject\Assets\MonoBehaviour"
GEN = r"E:\00Work\00Project\2026DungOnRush\2026DungeonRushUnity\Assets\_Assets\Resources\Scriptable Objects\Weapons"

def parse(path):
    t = open(path, encoding="utf-8").read()
    def g(k, default=None):
        m = re.search(r"^\s*%s:\s*(.+)$" % re.escape(k), t, re.M)
        return m.group(1).strip() if m else default
    def ref_set(k):
        # {fileID: 0} => khong co ; nguoc lai => co
        v = g(k, "{fileID: 0}")
        return "0" if re.search(r"fileID:\s*0\b", v) and "guid" not in v else "1"
    return {
        "name": g("m_Name"),
        "ItemId": g("ItemId"),
        "ItemName": g("ItemName"),
        "Rarity": g("Rarity"),
        "WeaponType": g("WeaponType"),
        "AttackSpeed": g("AttackSpeed"),
        "AttackDistance": g("AttackDistance"),
        "ProjectileSpeed": g("ProjectileSpeed"),
        "AreaDamageRadius": g("AreaDamageRadius"),
        "ProjectilePrefab": ref_set("ProjectilePrefab"),
        "LocalizationKey": g("LocalizationKey"),
    }

def parse_gen(path):
    t = open(path, encoding="utf-8").read()
    def g(k):
        m = re.search(r"^\s*%s:\s*(.+)$" % re.escape(k), t, re.M)
        return m.group(1).strip() if m else None
    return {
        "name": g("m_Name"),
        "ItemId": g("itemId"),
        "ItemName": g("displayName"),
        "Rarity": g("rarity"),
        "WeaponType": g("weaponType"),
        "AttackSpeed": g("attackSpeed"),
        "AttackDistance": g("attackDistance"),
        "ProjectileSpeed": g("projectileSpeed"),
        "hasProjectile": g("hasProjectile"),
        "isMonsterWeapon": g("isMonsterWeapon"),
    }

# ---- Danh sach asset weapon goc (59 Weapon_* + 13 boss) ----
src_files = sorted(glob.glob(os.path.join(SRC, "Weapon_*.asset")))
boss = ["CultistWeaponData_Melee","CultistWeaponData_Ranged","DragonWeaponData",
        "GreenDragonWeaponData","Lych2WeaponData","Lych3WeaponData","LychWeaponData",
        "Ogre1WeaponData","Ogre2WeaponData","PurpleDragonWeaponData","RedDragonWeaponData",
        "Witch2WeaponData","WitchWeaponData"]
src_files += [os.path.join(SRC, b + ".asset") for b in boss]

src = {}
for f in src_files:
    d = parse(f)
    src[d["name"]] = d

gen = {}
for f in glob.glob(os.path.join(GEN, "*.asset")):
    d = parse_gen(f)
    gen[d["name"]] = d

out = []
out.append("SRC goc count=%d | GEN count=%d" % (len(src), len(gen)))

# 1) Set khac biet ten
only_src = set(src) - set(gen)
only_gen = set(gen) - set(src)
if only_src: out.append("!! Chi co o GOC (thieu ben GEN): %s" % sorted(only_src))
if only_gen: out.append("!! Chi co o GEN (thua): %s" % sorted(only_gen))

# 2) AreaDamageRadius != 0 o data goc (field toi CHUA co)
adr = [(n, src[n]["AreaDamageRadius"]) for n in src if src[n]["AreaDamageRadius"] not in ("0", None)]
out.append("\n== AreaDamageRadius != 0 (GOC) == count=%d" % len(adr))
for n, v in sorted(adr): out.append("  %-26s AreaDamageRadius=%s" % (n, v))

# 3) So sanh gia tri tung field
def norm(x):
    if x is None: return ""
    try:
        f = float(x)
        return str(int(f)) if f.is_integer() else str(f)
    except: return str(x)

mism = []
for n in sorted(set(src) & set(gen)):
    s, g = src[n], gen[n]
    # projectile: goc ProjectilePrefab set (1) <-> gen hasProjectile (1)
    checks = [
        ("ItemId", s["ItemId"], g["ItemId"]),
        ("ItemName", s["ItemName"], g["ItemName"]),
        ("Rarity", s["Rarity"], g["Rarity"]),
        ("WeaponType", s["WeaponType"], g["WeaponType"]),
        ("AttackSpeed", s["AttackSpeed"], g["AttackSpeed"]),
        ("AttackDistance", s["AttackDistance"], g["AttackDistance"]),
        ("ProjectileSpeed", s["ProjectileSpeed"], g["ProjectileSpeed"]),
        ("hasProjectile(vs ProjectilePrefab)", s["ProjectilePrefab"], g["hasProjectile"]),
    ]
    for field, a, b in checks:
        if norm(a) != norm(b):
            mism.append((n, field, a, b))

out.append("\n== SAI LECH gia tri (GOC vs GEN) == count=%d" % len(mism))
for n, field, a, b in mism:
    out.append("  %-26s %-38s GOC=%s  GEN=%s" % (n, field, a, b))

sys.stdout.buffer.write(("\n".join(out) + "\n").encode("utf-8"))
