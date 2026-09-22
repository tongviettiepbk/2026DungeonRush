#!/usr/bin/env python3
# Sinh 10 MasteryUpgradeData asset (G1) tu data GOC (DecodedData/tables/MasteryUpgradeData.json),
# giu nguyen moi so. Chi giu stat + icon; bo TMP_SpriteAsset. Theo pattern gen_companion_assets.py.
#
# Chay tren mac. Icon lay tu cac PNG da crop san trong DecodedData/_img_tmp_gh/mastery_NN.png
# (mapping enum -> mastery_NN suy doan theo hinh, giong tools/upload_images_gh.py).
import os, json, uuid, shutil, re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
JSON = os.path.join(ROOT, "DecodedData", "tables", "MasteryUpgradeData.json")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
OUT = os.path.join(PROJ, "Resources", "Scriptable Objects", "Mastery")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "MasteryIcons")
IMG_DIR = os.path.join(ROOT, "DecodedData", "_img_tmp_gh")
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

# = guid trong Mastery/MasteryUpgradeData.cs.meta
SCRIPT_GUID = "bb8ede901b5248dab77d845e4d8b6d54"

# enum UpgradeType__enum -> ten file PNG da crop trong _img_tmp_gh (bo .png).
# MiningMaxPickaxe: PNG rieng "pickaxe" nam trong AssetRipper Texture2D, khong co trong _img_tmp_gh
# -> de fileID:0 (icon Mining se bo sung khi lam UI).
ICON_PNG = {
    "AutoLootHammerCount": "mastery_01",
    "GemOfferChance":      "mastery_03",
    "AdBoostDuration":     "mastery_04",
    "AdBoostWorth":        "mastery_05",
    "MaxOfflineTime":      "mastery_06",
    "OfflineEarningWorth": "mastery_07",
    "PlayerMovementSpeed": "mastery_08",
    "CompanionSummonCount": "mastery_09",
    "ForgeMaxItemLevel":   "mastery_10",
}

os.makedirs(OUT, exist_ok=True)
os.makedirs(ICON_DIR, exist_ok=True)

template = open(TEMPLATE_META, encoding="utf-8").read()
old_guid = re.search(r"^guid: ([0-9a-f]+)", template, re.M).group(1)
old_spriteid = re.search(r"spriteID: ([0-9a-f]+)", template).group(1)


def fnum(v):
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return repr(v) if isinstance(v, float) else str(v)


def yaml_str(s):
    if s is None or s == "":
        return "''"
    s = str(s)
    if any(c in s for c in [":", "#", "<", ">", "%", "{", "}", "[", "]", ",", "'", '"']) or s != s.strip():
        return "'" + s.replace("'", "''") + "'"
    return s


def make_icon(enum_name):
    png_name = ICON_PNG.get(enum_name)
    if not png_name:
        return "{fileID: 0}"
    png = os.path.join(IMG_DIR, png_name + ".png")
    if not os.path.exists(png):
        return "{fileID: 0}"
    dst_name = "mastery_" + enum_name
    shutil.copyfile(png, os.path.join(ICON_DIR, dst_name + ".png"))
    guid = uuid.uuid4().hex
    sid = guid[:24] + "00000000"
    meta = template.replace(old_guid, guid).replace(old_spriteid, sid)
    open(os.path.join(ICON_DIR, dst_name + ".png.meta"), "w", encoding="utf-8", newline="\n").write(meta)
    return "{fileID: 21300000, guid: %s, type: 3}" % guid


def write_asset_meta(apath):
    g = uuid.uuid4().hex
    open(apath + ".meta", "w", encoding="utf-8", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % g)


def suffix_val(s):
    # Bo chuoi hien thi dang <sprite=...> (khong dung cho logic/hien thi text thuong).
    if s and s.strip().startswith("<sprite"):
        return None
    return s


data = json.load(open(JSON, encoding="utf-8"))
count = 0
for r in data:
    name = r["_name"]
    enum_name = r["UpgradeType__enum"]
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000",
             "MonoBehaviour:", "  m_ObjectHideFlags: 0",
             "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
             "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1",
             "  m_EditorHideFlags: 0",
             "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % SCRIPT_GUID,
             "  m_Name: " + name, "  m_EditorClassIdentifier:",
             "  assetName: " + name,
             "  upgradeType: %d" % int(r["UpgradeType"]),
             "  upgradeName: " + yaml_str(r["UpgradeName"]),
             "  description: " + yaml_str(r["Description"]),
             "  icon: " + make_icon(enum_name),
             "  unlockGemCost: %d" % int(r["UnlockGemCost"]),
             "  addedLater: %d" % (1 if r["AddedLater"] else 0),
             "  defaultValue: %s" % fnum(float(r["DefaultValue"])),
             "  isValueAdditive: %d" % (1 if r["IsValueAdditive"] else 0),
             "  applyDefaultBeforeFeatureUnlock: %d" % (1 if r["ApplyDefaultBeforeFeatureUnlock"] else 0),
             "  applyDefaultBeforeCardUnlock: %d" % (1 if r["ApplyDefaultBeforeCardUnlock"] else 0),
             "  valuePrefix: " + yaml_str(r.get("ValuePrefix")),
             "  valueSuffix: " + yaml_str(suffix_val(r.get("ValueSuffix"))),
             "  levels:"]
    for lv in r["Levels"]:
        lines.append("  - gemCost: %d" % int(lv["GemCost"]))
        lines.append("    value: %s" % fnum(float(lv["Value"])))
    apath = os.path.join(OUT, name + ".asset")
    open(apath, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    write_asset_meta(apath)
    count += 1

print("wrote", count, "mastery assets to", OUT)
