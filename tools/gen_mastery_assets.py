#!/usr/bin/env python3
# Sinh 10 MasteryUpgradeData asset (G1) tu data GOC (DecodedData/tables/MasteryUpgradeData.json),
# giu nguyen moi so. Theo pattern gen_companion_assets.py.
#   - Ten/mo ta: localization EN goc (Items.Mastery.Name/Desc.<enum>) — JSON ghi nham cua MaxPickaxe.
#   - ValueSuffix "<sprite=0> " giu nguyen + sinh TMP_SpriteAsset (AutoLoot = hop loot IsBox).
#   - Chay lai KHONG doi guid: meta da co thi dung lai guid cu.
#
# Chay tren mac. Icon: PNG crop trong DecodedData/_img_tmp_gh + Texture2D cua AssetRipper
# (mapping enum -> PNG doi chieu anh game that 2026-09-29).
import os, json, uuid, shutil, re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
JSON = os.path.join(ROOT, "DecodedData", "tables", "MasteryUpgradeData.json")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
OUT = os.path.join(PROJ, "Resources", "Scriptable Objects", "Mastery")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "MasteryIcons")
IMG_DIR = os.path.join(ROOT, "DecodedData", "_img_tmp_gh")
RIP_TEX = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
RIP_SHEETS = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Resources", "spritesheets")
LOCALIZATION = os.path.join(ROOT, "DecodedData", "localization", "strings_en.json")
# TMP_SpriteAsset mau cua TMP Essentials (lay header + material nhung).
TMP_SPRITE_TEMPLATE = os.path.join(PROJ, "..", "TextMesh Pro", "Resources", "Sprite Assets", "EmojiOne.asset")
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

# = guid trong Mastery/MasteryUpgradeData.cs.meta
SCRIPT_GUID = "bb8ede901b5248dab77d845e4d8b6d54"

# enum UpgradeType__enum -> duong dan PNG nguon.
# Doi chieu anh game that: AutoLoot = hop + (mastery_10), MaxPickaxe = cuoc + (mastery_13).
# ForgeMaxItemLevel = kiem + (mastery_14) — suy theo hinh, chua co anh game that.
ICON_PNG = {
    "AutoLootHammerCount": os.path.join(IMG_DIR, "mastery_10.png"),
    "GemOfferChance":      os.path.join(IMG_DIR, "mastery_03.png"),
    "AdBoostDuration":     os.path.join(IMG_DIR, "mastery_04.png"),
    "AdBoostWorth":        os.path.join(IMG_DIR, "mastery_05.png"),
    "MaxOfflineTime":      os.path.join(IMG_DIR, "mastery_06.png"),
    "OfflineEarningWorth": os.path.join(IMG_DIR, "mastery_07.png"),
    "PlayerMovementSpeed": os.path.join(IMG_DIR, "mastery_08.png"),
    "CompanionSummonCount": os.path.join(IMG_DIR, "mastery_09.png"),
    "ForgeMaxItemLevel":   os.path.join(RIP_TEX, "mastery_14.png"),
    "MiningMaxPickaxe":    os.path.join(RIP_TEX, "mastery_13.png"),
}

# guid ValueSpriteAsset trong JSON goc -> ten TMP_SpriteAsset trong spritesheets cua AssetRipper.
SPRITE_ASSETS = {
    "84698602950e4d84ab3679ea0cd2fab8": "IsBox",
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


def existing_guid(meta_path):
    # Meta da co -> dung lai guid (chay lai tool khong lam gay reference).
    if os.path.exists(meta_path):
        return re.search(r"^guid: ([0-9a-f]+)", open(meta_path, encoding="utf-8").read(), re.M).group(1)
    return uuid.uuid4().hex


def copy_texture(png, dst_name):
    shutil.copyfile(png, os.path.join(ICON_DIR, dst_name + ".png"))
    meta_path = os.path.join(ICON_DIR, dst_name + ".png.meta")
    guid = existing_guid(meta_path)
    sid = guid[:24] + "00000000"
    meta = template.replace(old_guid, guid).replace(old_spriteid, sid)
    open(meta_path, "w", encoding="utf-8", newline="\n").write(meta)
    return guid


def make_icon(enum_name):
    png = ICON_PNG.get(enum_name)
    if not png or not os.path.exists(png):
        return "{fileID: 0}"
    guid = copy_texture(png, "mastery_" + enum_name)
    return "{fileID: 21300000, guid: %s, type: 3}" % guid


def make_sprite_asset(ref):
    # TMP_SpriteAsset: header + material lay tu EmojiOne (dung version TMP cua project),
    # bang glyph/character lay nguyen tu asset rip; texture = spriteSheet cua asset rip.
    name = SPRITE_ASSETS.get((ref or {}).get("guid"))
    if not name:
        return "{fileID: 0}"
    rip = open(os.path.join(RIP_SHEETS, name + ".asset"), encoding="utf-8").read()
    char_name = re.search(r"m_SpriteCharacterTable:.*?m_Name: (\S+)", rip, re.S).group(1)
    tex_guid = copy_texture(os.path.join(RIP_TEX, char_name + ".png"), char_name)

    tpl = open(TMP_SPRITE_TEMPLATE, encoding="utf-8").read()
    tpl = re.sub(r"(_MainTex:\n\s+m_Texture: )\{[^}]*\}", r"\g<1>{fileID: 2800000, guid: %s, type: 3}" % tex_guid, tpl)
    head = tpl[:tpl.index("  m_Name: EmojiOne")]
    face = tpl[tpl.index("  m_EditorClassIdentifier:"):tpl.index("  spriteSheet:")]
    face = re.sub(r"hashCode: -?\d+", "hashCode: " + re.search(r"\n  hashCode: (-?\d+)", rip).group(1), face, count=1)
    tables = rip[rip.index("  m_SpriteCharacterTable:"):]
    body = (head + "  m_Name: " + name + "\n" + face
            + "  spriteSheet: {fileID: 2800000, guid: %s, type: 3}\n" % tex_guid + tables)

    apath = os.path.join(ICON_DIR, name + ".asset")
    open(apath, "w", encoding="utf-8", newline="\n").write(body)
    write_asset_meta(apath)
    return "{fileID: 11400000, guid: %s, type: 2}" % existing_guid(apath + ".meta")


def write_asset_meta(apath):
    g = existing_guid(apath + ".meta")
    open(apath + ".meta", "w", encoding="utf-8", newline="\n").write(
        "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % g)


data = json.load(open(JSON, encoding="utf-8"))
loc = json.load(open(LOCALIZATION, encoding="utf-8"))
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
             "  upgradeName: " + yaml_str(loc.get("Items.Mastery.Name." + enum_name, r["UpgradeName"])),
             "  description: " + yaml_str(loc.get("Items.Mastery.Desc." + enum_name, r["Description"])),
             "  icon: " + make_icon(enum_name),
             "  unlockGemCost: %d" % int(r["UnlockGemCost"]),
             "  addedLater: %d" % (1 if r["AddedLater"] else 0),
             "  defaultValue: %s" % fnum(float(r["DefaultValue"])),
             "  isValueAdditive: %d" % (1 if r["IsValueAdditive"] else 0),
             "  applyDefaultBeforeFeatureUnlock: %d" % (1 if r["ApplyDefaultBeforeFeatureUnlock"] else 0),
             "  applyDefaultBeforeCardUnlock: %d" % (1 if r["ApplyDefaultBeforeCardUnlock"] else 0),
             "  valuePrefix: " + yaml_str(r.get("ValuePrefix")),
             "  valueSuffix: " + yaml_str(r.get("ValueSuffix")),
             "  valueSpriteAsset: " + make_sprite_asset(r.get("ValueSpriteAsset")),
             "  levels:"]
    for lv in r["Levels"]:
        lines.append("  - gemCost: %d" % int(lv["GemCost"]))
        lines.append("    value: %s" % fnum(float(lv["Value"])))
    apath = os.path.join(OUT, name + ".asset")
    open(apath, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    write_asset_meta(apath)
    count += 1

print("wrote", count, "mastery assets to", OUT)
