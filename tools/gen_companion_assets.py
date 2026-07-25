#!/usr/bin/env python3
# Sinh 18 CompanionData asset (D1) tu data GOC (DecodedData/tables/CompanionData.json),
# giu nguyen moi so. Chi giu stat + icon; bo prefab/sound/mau VFX.
import os, json, uuid, shutil

ROOT = r"E:\00Work\00Project\2026DungOnRush"
JSON = os.path.join(ROOT, "DecodedData", "tables", "CompanionData.json")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
OUT = os.path.join(PROJ, "Resources", "Scriptable Objects", "Companions")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "CompanionIcons")
IMG_DIR = os.path.join(ROOT, "DecodedData", "_img_tmp_d")
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

SCRIPT_GUID = "3f8b1d6a9c2e4f5b8a0d7c3e6f1b4a29"  # = CompanionData.cs.meta

os.makedirs(OUT, exist_ok=True)
os.makedirs(ICON_DIR, exist_ok=True)

import re
template = open(TEMPLATE_META, encoding="utf-8").read()
old_guid = re.search(r"^guid: ([0-9a-f]+)", template, re.M).group(1)
old_spriteid = re.search(r"spriteID: ([0-9a-f]+)", template).group(1)

# (yamlKey C#, jsonKey, kind) — kind: f=float, i=int, b=bool, s=string
FIELDS = [
    ("companionName", "CompanionName", "s"),
    ("localizationKey", "LocalizationKey", "s"),
    ("descriptionKey", "DescriptionKey", "s"),
    ("type", "Type", "i"),
    ("rarity", "Rarity", "i"),
    ("details", "Details", "s"),
    ("moveSpeed", "MoveSpeed", "f"),
    ("followDistance", "FollowDistance", "f"),
    ("minDistance", "MinDistance", "f"),
    ("initialDelay", "InitialDelay", "f"),
    ("cooldown", "Cooldown", "f"),
    ("effectDuration", "EffectDuration", "f"),
    ("level", "Level", "i"),
    ("damageBase", "DamageBase", "f"),
    ("damageScaler", "DamageScaler", "f"),
    ("healBase", "HealBase", "f"),
    ("healScaler", "HealScaler", "f"),
    ("slowDuration", "SlowDuration", "f"),
    ("slowAmount", "SlowAmount", "f"),
    ("projectileSpeed", "ProjectileSpeed", "f"),
    ("bombRadius", "BombRadius", "f"),
    ("bombProjectileSpeed", "BombProjectileSpeed", "f"),
    ("bombTravelDuration", "BombTravelDuration", "f"),
    ("bombArcHeight", "BombArcHeight", "f"),
    ("bombExplosionDuration", "BombExplosionDuration", "f"),
    ("burnEnabled", "BurnEnabled", "b"),
    ("burnDuration", "BurnDuration", "f"),
    ("burnTickDamage", "BurnTickDamage", "f"),
    ("multiSlowChainCount", "MultiSlowChainCount", "i"),
    ("chainHealSpeed", "ChainHealSpeed", "f"),
    ("chainHealDuration", "ChainHealDuration", "f"),
    ("healTickInterval", "HealTickInterval", "f"),
    ("lightningChainCount", "LightningChainCount", "i"),
    ("lightningSpeed", "LightningSpeed", "f"),
    ("lightningDuration", "LightningDuration", "f"),
    ("lightningTickInterval", "LightningTickInterval", "f"),
    ("beamSpeed", "BeamSpeed", "f"),
    ("beamDuration", "BeamDuration", "f"),
    ("beamTickInterval", "BeamTickInterval", "f"),
    ("guardianActiveDuration", "GuardianActiveDuration", "f"),
    ("guardianHealAmount", "GuardianHealAmount", "f"),
    ("guardianBlockChance", "GuardianBlockChance", "f"),
    ("aoeSlowRadius", "AoeSlowRadius", "f"),
    ("aoeSlowProjectileSpeed", "AoeSlowProjectileSpeed", "f"),
    ("aoeSlowArcHeight", "AoeSlowArcHeight", "f"),
    ("aoeSlowExplosionDuration", "AoeSlowExplosionDuration", "f"),
    ("blasterRange", "BlasterRange", "f"),
    ("blasterFireDuration", "BlasterFireDuration", "f"),
    ("blasterWaveCount", "BlasterWaveCount", "i"),
    ("blasterConeAngle", "BlasterConeAngle", "f"),
    ("meteorBombCount", "MeteorBombCount", "i"),
    ("meteorOffsetRange", "MeteorOffsetRange", "f"),
    ("meteorBombDelay", "MeteorBombDelay", "f"),
    ("meteorDropHeight", "MeteorDropHeight", "f"),
    ("siphonProjectileSpeed", "SiphonProjectileSpeed", "f"),
    ("siphonSineAmplitude", "SiphonSineAmplitude", "f"),
    ("siphonSineFrequency", "SiphonSineFrequency", "f"),
    ("ownAttackBase", "OwnAttackBase", "f"),
    ("ownAttackScaler", "OwnAttackScaler", "f"),
    ("ownHealthBase", "OwnHealthBase", "f"),
    ("ownHealthScaler", "OwnHealthScaler", "f"),
    ("healNovaStartRadius", "HealNovaStartRadius", "f"),
    ("healNovaEndRadius", "HealNovaEndRadius", "f"),
    ("healNovaDuration", "HealNovaDuration", "f"),
    ("healNovaTickInterval", "HealNovaTickInterval", "f"),
    ("healNovaDamageBase", "HealNovaDamageBase", "f"),
    ("healNovaDamageScaler", "HealNovaDamageScaler", "f"),
    ("cloneHealthPercent", "CloneHealthPercent", "f"),
    ("cloneHealthPercentScaler", "CloneHealthPercentScaler", "f"),
    ("cloneLifetime", "CloneLifetime", "f"),
    ("cloneGridSearchCount", "CloneGridSearchCount", "i"),
    ("cloneWalkDuration", "CloneWalkDuration", "f"),
    ("cloneTintAmount", "CloneTintAmount", "f"),
    ("cloneAlpha", "CloneAlpha", "f"),
    ("cloneBrightness", "CloneBrightness", "f"),
    ("immortalityDuration", "ImmortalityDuration", "f"),
    ("immortalityDurationScaler", "ImmortalityDurationScaler", "f"),
]


def fnum(v):
    # In so gon, giu nguyen gia tri; int khong .0
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return repr(v) if isinstance(v, float) else str(v)


def yaml_str(s):
    # Unity YAML: escape neu can. Details co the co dau ':' -> boc trong single quote.
    if s is None:
        return "''"
    s = str(s)
    if s == "":
        return "''"
    if any(c in s for c in [":", "#", "<", ">", "%", "{", "}", "[", "]", ",", "'", '"']) or s != s.strip():
        return "'" + s.replace("'", "''") + "'"
    return s


def make_icon(name):
    png = os.path.join(IMG_DIR, name + ".png")
    if not os.path.exists(png):
        return "{fileID: 0}"
    shutil.copyfile(png, os.path.join(ICON_DIR, name + ".png"))
    guid = uuid.uuid4().hex
    sid = guid[:24] + "00000000"
    meta = template.replace(old_guid, guid).replace(old_spriteid, sid)
    open(os.path.join(ICON_DIR, name + ".png.meta"), "w", encoding="utf-8", newline="\n").write(meta)
    return "{fileID: 21300000, guid: %s, type: 3}" % guid


def write_asset_meta(apath):
    g = uuid.uuid4().hex
    open(apath + ".meta", "w", encoding="utf-8", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % g)


data = json.load(open(JSON, encoding="utf-8"))
count = 0
for r in data:
    name = r["_name"]
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000",
             "MonoBehaviour:", "  m_ObjectHideFlags: 0",
             "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
             "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1",
             "  m_EditorHideFlags: 0",
             "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % SCRIPT_GUID,
             "  m_Name: " + name, "  m_EditorClassIdentifier:",
             "  assetName: " + name]
    for ykey, jkey, kind in FIELDS:
        v = r.get(jkey)
        if kind == "s":
            lines.append("  %s: %s" % (ykey, yaml_str(v)))
        elif kind == "b":
            lines.append("  %s: %d" % (ykey, 1 if v else 0))
        elif kind == "i":
            lines.append("  %s: %d" % (ykey, int(v)))
        else:
            lines.append("  %s: %s" % (ykey, fnum(v)))
    lines.append("  icon: " + make_icon(name))
    apath = os.path.join(OUT, name + ".asset")
    open(apath, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    write_asset_meta(apath)
    count += 1

print("wrote", count, "companion assets to", OUT)
