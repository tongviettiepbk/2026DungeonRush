#!/usr/bin/env python3
# Sinh GearItemData .asset cho C2..C6 tu data GOC (AssetRipper), doc theo script-guid
# tung loai de khong sot boss. Gan luon icon (copy PNG tu _img_tmp) neu goc co Icon.
import os, re, glob, shutil, uuid

ROOT = r"E:\00Work\00Project\2026DungOnRush"
SRC = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "MonoBehaviour")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
RES = os.path.join(PROJ, "Resources", "Scriptable Objects", "Gears")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "GearIcons")
IMG_DIRS = [os.path.join(ROOT, "DecodedData", d) for d in
            ("_img_tmp", "_img_tmp_bc", "_img_tmp_d", "_img_tmp_e", "_img_tmp_f", "_img_tmp_gh", "_img_tmp_i")]
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

GEAR_SCRIPT_GUID = "db263ffe4d7dc7a40852fe2457c254f0"  # GearItemData.cs (Unity tu sinh)

# slot enum int (khop GearSlotType), thu muc con, script-guid GOC de quet dung loai
TYPES = [
    # (slotInt, folder, script_guid_goc)
    (1, "Helmets",   "7f715997e5b840bda8230056bea95e2c"),  # HelmetData
    (2, "Gloves",    "e9efb72f1936240fdb227142af2f8fe8"),  # GlovesData
    (3, "Rings",     "7af5f1a3859c3fc63c6e880c4bad3ca5"),  # RingData
    (4, "Necklaces", "3e49dd65944305d182771504d45baafb"),  # NeckleData
    (5, "Backpacks", "9ba934ab2e1a8b5285b90ba362f47049"),  # BackpackData
]

template = open(TEMPLATE_META, encoding="utf-8").read()
old_guid = re.search(r"^guid: ([0-9a-f]+)", template, re.M).group(1)
old_spriteid = re.search(r"spriteID: ([0-9a-f]+)", template).group(1)

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
  slot: {slot}
  assetName: {asset}
  itemId: {itemId}
  displayName: {name}
  rarity: {rarity}
  localizationKey: {lkey}
  icon: {icon}
"""

def field(text, key):
    m = re.search(r"^\s*%s:\s*(.+)$" % re.escape(key), text, re.M)
    return m.group(1).strip() if m else None

def find_png(name):
    for d in IMG_DIRS:
        p = os.path.join(d, name + ".png")
        if os.path.exists(p):
            return p
    return None

os.makedirs(ICON_DIR, exist_ok=True)
report = {}
for slot, folder, sguid in TYPES:
    out = os.path.join(RES, folder)
    os.makedirs(out, exist_ok=True)
    files = [f for f in glob.glob(os.path.join(SRC, "*.asset"))
             if ("guid: " + sguid) in open(f, encoding="utf-8").read()]
    n_total = n_icon = 0
    for f in sorted(files):
        t = open(f, encoding="utf-8").read()
        name = field(t, "m_Name")
        itemId = field(t, "ItemId")
        iname = field(t, "ItemName")
        rarity = field(t, "Rarity")
        lkey = field(t, "LocalizationKey")
        icon_line = field(t, "Icon")
        has_icon = icon_line and "guid" in icon_line

        icon_yaml = "{fileID: 0}"
        if has_icon:
            png = find_png(name)
            if png:
                shutil.copyfile(png, os.path.join(ICON_DIR, name + ".png"))
                guid = uuid.uuid4().hex
                sid = guid[:24] + "00000000"
                meta = template.replace(old_guid, guid).replace(old_spriteid, sid)
                open(os.path.join(ICON_DIR, name + ".png.meta"), "w", encoding="utf-8", newline="\n").write(meta)
                icon_yaml = "{fileID: 21300000, guid: %s, type: 3}" % guid
                n_icon += 1

        body = ASSET_TMPL.format(sguid=GEAR_SCRIPT_GUID, asset=name, slot=slot,
                                 itemId=itemId, name=iname, rarity=rarity, lkey=lkey, icon=icon_yaml)
        apath = os.path.join(out, name + ".asset")
        open(apath, "w", encoding="utf-8", newline="\n").write(body)
        mguid = uuid.uuid4().hex
        open(apath + ".meta", "w", encoding="utf-8", newline="\n").write(
            "fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n"
            "  mainObjectFileID: 11400000\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n" % mguid)
        n_total += 1
    report[folder] = (n_total, n_icon)

for folder, (nt, ni) in report.items():
    print("%-12s assets=%3d  icons=%3d" % (folder, nt, ni))
print("TOTAL assets =", sum(v[0] for v in report.values()))
