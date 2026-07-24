#!/usr/bin/env python3
# Sinh 72 WeaponData .asset + .meta cho DungeonRush (C1 Vu khi) tu data da decode.
import os, sys, uuid

ROOT = r"E:\00Work\00Project\2026DungOnRush\2026DungeonRushUnity\Assets\_Assets"
WEAPON_SCRIPT_GUID = "7d3f1c9e5a4b42c88e0f6b2a9c1d4e5f"  # WeaponData.cs.meta
OUT = os.path.join(ROOT, "Resources", "Scriptable Objects", "Weapons")

# (asset, itemId, name, rarity, wtype[0=Melee,1=Range], dist, projSpeed, hasProj, isMonster)
MEL, RNG = 0, 1
rows = []

# --- Player melee: dist 1.5, no projectile ---
melee = [
    ("Weapon_m_1_1", 2, "Pitchfork", 0), ("Weapon_m_1_2", 1, "Wooden Sword", 0),
    ("Weapon_m_1_3", 1277, "Sickle", 0), ("Weapon_m_2_1", 30, "Crude Dagger", 1),
    ("Weapon_m_2_2", 29, "Wood Axe", 1), ("Weapon_m_2_3", 1287, "Battle Axe", 1),
    ("Weapon_m_3_1", 1295, "Iron Sword", 2), ("Weapon_m_3_2", 32, "Great Axe", 2),
    ("Weapon_m_3_3", 1288, "Mace", 2), ("Weapon_m_4_1", 57, "Soldier Sword", 3),
    ("Weapon_m_4_2", 58, "Soldier Daggers", 3), ("Weapon_m_4_3", 1290, "Squire Sword", 3),
    ("Weapon_m_5_1", 61, "Twin Dagger", 4), ("Weapon_m_5_2", 62, "Royal Sword", 4),
    ("Weapon_m_5_3", 1293, "Knight Sword", 4), ("Weapon_m_6_1", 1332, "Mystic Sword", 5),
    ("Weapon_m_6_2", 1333, "Arcane Dagger", 5), ("Weapon_m_6_3", 1334, "Glimmer Axe", 5),
    ("Weapon_m_7_1", 1335, "Sacred Sword", 6), ("Weapon_m_7_2", 1336, "Wild Axe", 6),
    ("Weapon_m_7_3", 1337, "Druidic Sword", 6), ("Weapon_m_8_1", 1360, "Ice Dagger", 7),
    ("Weapon_m_8_2", 1361, "Frost Greataxe", 7), ("Weapon_m_8_3", 1362, "Blizzard Greatsword", 7),
    ("Weapon_m_9_1", 1428, "Dread Dagger", 8), ("Weapon_m_9_2", 1429, "Infernal Greatsword", 8),
    ("Weapon_m_9_3", 1430, "Void Scythe", 8), ("Weapon_m_10_1", 1431, "Divine Mace", 9),
    ("Weapon_m_10_2", 1432, "Holy Greatsword", 9), ("Weapon_m_10_3", 1433, "Solar Greataxe", 9),
]
for a, iid, nm, rar in melee:
    rows.append((a, iid, nm, rar, MEL, 1.5, 0, False, False))

# --- Player range: dist 3.5, projectile, projSpeed varies ---
rng = [
    ("Weapon_r_1_1", 35, "Sling", 0, 20), ("Weapon_r_1_2", 63, "Throwing Stick", 0, 20),
    ("Weapon_r_1_3", 1299, "Primitive Bow", 0, 20), ("Weapon_r_2_1", 65, "Javelin", 1, 10),
    ("Weapon_r_2_2", 66, "Bow", 1, 10), ("Weapon_r_3_1", 68, "Viking Spear", 2, 10),
    ("Weapon_r_3_2", 67, "Viking Bow", 2, 10), ("Weapon_r_3_3", 1298, "Longbow", 2, 10),
    ("Weapon_r_4_1", 70, "Squire Javelin", 3, 15), ("Weapon_r_4_2", 71, "Soldier Bow", 3, 15),
    ("Weapon_r_4_3", 1301, "Squire Crossbow", 3, 15), ("Weapon_r_5_1", 73, "Knight Javelin", 4, 15),
    ("Weapon_r_5_2", 72, "Knight Crossbow", 4, 15), ("Weapon_r_5_3", 1296, "Flame Staff", 4, 7),
    ("Weapon_r_6_1", 1338, "Mystic Kunai", 5, 8), ("Weapon_r_6_2", 1339, "Arcane Bow", 5, 8),
    ("Weapon_r_6_3", 1340, "Wizard Staff", 5, 7), ("Weapon_r_7_1", 1341, "Sacred Spear", 6, 8),
    ("Weapon_r_7_2", 1342, "Druidic Crossbow", 6, 8), ("Weapon_r_7_3", 1343, "Wild Staff", 6, 7),
    ("Weapon_r_8_1", 1363, "Ice Javelin", 7, 8), ("Weapon_r_8_2", 1364, "Frost Bow", 7, 8),
    ("Weapon_r_8_3", 1365, "Ice Staff", 7, 7), ("Weapon_r_9_1", 1434, "Undying Bow", 8, 8),
    ("Weapon_r_9_2", 1435, "Void Javelin", 8, 8), ("Weapon_r_9_3", 1436, "Tyrant Staff", 8, 7),
    ("Weapon_r_10_1", 1437, "Celestial Lightning", 9, 8), ("Weapon_r_10_2", 1438, "Eternal Crossbow", 9, 8),
    ("Weapon_r_10_3", 1439, "Radiant Staff", 9, 7),
]
for a, iid, nm, rar, ps in rng:
    rows.append((a, iid, nm, rar, RNG, 3.5, ps, True, False))

# --- Monster/boss weapons (isMonster=True) ---
# (asset, itemId, name, wtype, dist, projSpeed, hasProj)
mon = [
    ("CultistWeaponData_Melee", 1010, "CultistWeapon", MEL, 1.5, 0, False),
    ("CultistWeaponData_Ranged", 1010, "CultistWeapon", RNG, 3.5, 15, True),
    ("DragonWeaponData", 1003, "DragonWeapon", RNG, 100, 12, True),
    ("GreenDragonWeaponData", 1368, "DragonWeapon", RNG, 100, 12, True),
    ("Lych2WeaponData", 1380, "DragonWeapon", RNG, 100, 12, True),
    ("Lych3WeaponData", 1383, "DragonWeapon", RNG, 100, 12, True),
    ("LychWeaponData", 1377, "DragonWeapon", RNG, 100, 12, True),
    ("Ogre1WeaponData", 1386, "DragonWeapon", MEL, 3, 12, False),
    ("Ogre2WeaponData", 1389, "DragonWeapon", MEL, 3, 12, False),
    ("PurpleDragonWeaponData", 1371, "DragonWeapon", RNG, 100, 12, True),
    ("RedDragonWeaponData", 1374, "DragonWeapon", RNG, 100, 12, True),
    ("Witch2WeaponData", 1395, "DragonWeapon", RNG, 100, 12, True),
    ("WitchWeaponData", 1392, "DragonWeapon", RNG, 100, 12, True),
]
for a, iid, nm, wt, dist, ps, hp in mon:
    rows.append((a, iid, nm, 0, wt, dist, ps, hp, True))


def num(x):
    # In so kieu Unity: bo .0 thua neu la so nguyen.
    if isinstance(x, float) and x.is_integer():
        return str(int(x))
    return str(x)


ASSET_TMPL = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sguid}, type: 3}}
  m_Name: {asset}
  m_EditorClassIdentifier:
  assetName: {asset}
  itemId: {itemId}
  displayName: {name}
  rarity: {rarity}
  icon: {{fileID: 0}}
  weaponType: {wtype}
  attackSpeed: 1
  attackDistance: {dist}
  projectileSpeed: {proj}
  hasProjectile: {hasproj}
  isMonsterWeapon: {ismon}
"""

META_TMPL = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

os.makedirs(OUT, exist_ok=True)
count = 0
for (asset, itemId, name, rarity, wtype, dist, projSpeed, hasProj, isMon) in rows:
    body = ASSET_TMPL.format(
        sguid=WEAPON_SCRIPT_GUID, asset=asset, itemId=itemId, name=name,
        rarity=rarity, wtype=wtype, dist=num(dist), proj=num(projSpeed),
        hasproj=1 if hasProj else 0, ismon=1 if isMon else 0)
    apath = os.path.join(OUT, asset + ".asset")
    with open(apath, "w", encoding="utf-8", newline="\n") as f:
        f.write(body)
    guid = uuid.uuid4().hex
    with open(apath + ".meta", "w", encoding="utf-8", newline="\n") as f:
        f.write(META_TMPL.format(guid=guid))
    count += 1

sys.stdout.buffer.write(("Generated %d weapon assets into %s\n" % (count, OUT)).encode("utf-8"))
