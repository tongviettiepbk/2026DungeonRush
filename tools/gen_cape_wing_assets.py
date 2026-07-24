#!/usr/bin/env python3
# Sinh CapeData (12) + CapeConfig + WingData (10) tu asset GOC, giu nguyen moi so.
import os, re, glob, shutil, uuid, struct

ROOT = r"E:\00Work\00Project\2026DungOnRush"
SRC = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "MonoBehaviour")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
CAPE_OUT = os.path.join(PROJ, "Resources", "Scriptable Objects", "Gears", "Capes")
WING_OUT = os.path.join(PROJ, "Resources", "Scriptable Objects", "Gears", "Wings")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "GearIcons")
IMG_DIRS = [os.path.join(ROOT, "DecodedData", d) for d in
            ("_img_tmp", "_img_tmp_bc", "_img_tmp_d", "_img_tmp_e", "_img_tmp_f", "_img_tmp_gh", "_img_tmp_i")]
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

CAPE_GUID = "1420b4d46853ed149a2f7aa8b7069cde"
CAPECFG_GUID = "10ef4fa273635064081a8fe8632b217a"
WING_GUID = "20f648cef567f364798835ff305ded65"

template = open(TEMPLATE_META, encoding="utf-8").read()
old_guid = re.search(r"^guid: ([0-9a-f]+)", template, re.M).group(1)
old_spriteid = re.search(r"spriteID: ([0-9a-f]+)", template).group(1)


def f1(t, k):
    m = re.search(r"^\s*%s:\s*(.+)$" % re.escape(k), t, re.M)
    return m.group(1).strip() if m else None

def find_png(name):
    for d in IMG_DIRS:
        p = os.path.join(d, name + ".png")
        if os.path.exists(p):
            return p
    return None

def make_icon(name, has_icon):
    if not has_icon:
        return "{fileID: 0}"
    png = find_png(name)
    if not png:
        return "{fileID: 0}"
    shutil.copyfile(png, os.path.join(ICON_DIR, name + ".png"))
    guid = uuid.uuid4().hex
    sid = guid[:24] + "00000000"
    meta = template.replace(old_guid, guid).replace(old_spriteid, sid)
    open(os.path.join(ICON_DIR, name + ".png.meta"), "w", encoding="utf-8", newline="\n").write(meta)
    return "{fileID: 21300000, guid: %s, type: 3}" % guid

def write_meta(apath):
    g = uuid.uuid4().hex
    open(apath + ".meta", "w", encoding="utf-8", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % g)

def head(name, sguid):
    return ("%%YAML 1.1\n%%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n"
            "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
            "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n"
            "  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: %s, type: 3}\n"
            "  m_Name: %s\n  m_EditorClassIdentifier:\n" % (sguid, name))

os.makedirs(CAPE_OUT, exist_ok=True); os.makedirs(WING_OUT, exist_ok=True); os.makedirs(ICON_DIR, exist_ok=True)

# ---------- CAPE items ----------
cape_icons = 0
for i in range(1, 13):
    t = open(os.path.join(SRC, "%d-Cape.asset" % i), encoding="utf-8").read()
    name = "%d-Cape" % i
    has_icon = "guid" in (f1(t, "Icon") or "")
    icon = make_icon(name, has_icon)
    if "guid" in icon: cape_icons += 1
    body = head(name, CAPE_GUID)
    body += ("  capeId: %s\n  capeName: %s\n  rarity: %s\n  localizationKey: %s\n  icon: %s\n"
             "  healthBase: %s\n  damageBase: %s\n  healthScaler: %s\n  damageScaler: %s\n  subStatCount: %s\n" % (
             f1(t,"CapeId"), f1(t,"CapeName"), f1(t,"Rarity"), f1(t,"LocalizationKey"), icon,
             f1(t,"HealthBase"), f1(t,"DamageBase"), f1(t,"HealthScaler"), f1(t,"DamageScaler"), f1(t,"SubStatCount")))
    p = os.path.join(CAPE_OUT, name + ".asset")
    open(p, "w", encoding="utf-8", newline="\n").write(body); write_meta(p)

# ---------- CapeConfig ----------
ct = open(os.path.join(SRC, "CapeConfig.asset"), encoding="utf-8").read()
body = head("CapeConfig", CAPECFG_GUID)
body += "  maxLevel: %s\n  summonCost: %s\n  subStatCount: %s\n" % (
    f1(ct,"MaxLevel"), f1(ct,"SummonCost"), f1(ct,"SubStatCount"))
# parse RarityConfigs blocks
blocks = re.findall(r"-\s*Rarity:\s*(\d+)\s*\n\s*SalvageBaseXP:\s*(\S+)\s*\n\s*LevelUpBaseXP:\s*(\S+)\s*\n"
                    r"\s*LevelUpScaler:\s*(\S+)\s*\n\s*LevelUpExpo:\s*(\S+)\s*\n\s*LevelUpBaseXP2:\s*(\S+)\s*\n"
                    r"\s*LevelUpScaler2:\s*(\S+)", ct)
body += "  rarityConfigs:\n"
for r, sal, l1, s1, ex, l2, s2 in blocks:
    body += ("  - rarity: %s\n    salvageBaseXP: %s\n    levelUpBaseXP: %s\n    levelUpScaler: %s\n"
             "    levelUpExpo: %s\n    levelUpBaseXP2: %s\n    levelUpScaler2: %s\n" % (r, sal, l1, s1, ex, l2, s2))
p = os.path.join(CAPE_OUT, "CapeConfig.asset")
open(p, "w", encoding="utf-8", newline="\n").write(body); write_meta(p)
print("Cape items=12 (icons=%d) + CapeConfig (rarityConfigs=%d)" % (cape_icons, len(blocks)))

# ---------- WING items ----------
def decode_reroll(hexstr):
    if not hexstr: return []
    b = bytes.fromhex(hexstr)
    return [struct.unpack("<i", b[i:i+4])[0] for i in range(0, len(b), 4)]

wing_icons = 0
for i in range(1, 11):
    t = open(os.path.join(SRC, "Wing_%d.asset" % i), encoding="utf-8").read()
    name = "Wing_%d" % i
    has_icon = "guid" in (f1(t, "Icon") or "")
    icon = make_icon(name, has_icon)
    if "guid" in icon: wing_icons += 1
    # subStats blocks
    subs = re.findall(r"-\s*Type:\s*(\S+)\s*\n\s*Value:\s*(\S+)", t)
    reroll = decode_reroll(f1(t, "RerollCosts"))
    body = head(name, WING_GUID)
    body += ("  wingId: %s\n  wingName: %s\n  rarity: %s\n  localizationKey: %s\n  icon: %s\n"
             "  healthBase: %s\n  damageBase: %s\n  healthScaler: %s\n  damageScaler: %s\n"
             "  healthTierScaler: %s\n  damageTierScaler: %s\n" % (
             f1(t,"WingId"), f1(t,"WingName"), f1(t,"Rarity"), f1(t,"LocalizationKey"), icon,
             f1(t,"HealthBase"), f1(t,"DamageBase"), f1(t,"HealthScaler"), f1(t,"DamageScaler"),
             f1(t,"HealthTierScaler"), f1(t,"DamageTierScaler")))
    body += "  subStats:\n"
    for ty, va in subs:
        body += "  - type: %s\n    value: %s\n" % (ty, va)
    body += "  craftOreType: %s\n  craftOreCost: %s\n  rerollOreType: %s\n" % (
        f1(t,"CraftOreType"), f1(t,"CraftOreCost"), f1(t,"RerollOreType"))
    body += "  rerollCosts:\n"
    for c in reroll:
        body += "  - %d\n" % c
    body += "  levelUpOreType: %s\n  levelUpCost: %s\n  levelUpCostMultiplier: %s\n  maxLevel: %s\n" % (
        f1(t,"LevelUpOreType"), f1(t,"LevelUpCost"), f1(t,"LevelUpCostMultiplier"), f1(t,"MaxLevel"))
    p = os.path.join(WING_OUT, name + ".asset")
    open(p, "w", encoding="utf-8", newline="\n").write(body); write_meta(p)
    print("  %-8s rarity=%s subStats=%d rerollCosts=%s" % (name, f1(t,"Rarity"), len(subs), reroll))
print("Wing items=10 (icons=%d)" % wing_icons)
