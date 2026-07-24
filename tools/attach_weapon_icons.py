#!/usr/bin/env python3
# Copy 61 PNG icon vu khi vao project, sinh .meta Sprite (guid rieng),
# roi gan icon: {fileID: 21300000, guid, type: 3} vao tung WeaponData .asset.
import os, re, glob, shutil, uuid

ROOT = r"E:\00Work\00Project\2026DungOnRush"
SRC_ASSET = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "MonoBehaviour")
IMG = os.path.join(ROOT, "DecodedData", "_img_tmp_bc")
PROJ = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets")
ICON_DIR = os.path.join(PROJ, "_ResourceGame", "WeaponIcons")
WEAPON_DIR = os.path.join(PROJ, "Resources", "Scriptable Objects", "Weapons")
TEMPLATE_META = os.path.join(PROJ, "_ResourceGame", "Avatar", "angel.png.meta")

boss = ["CultistWeaponData_Melee","CultistWeaponData_Ranged","DragonWeaponData","GreenDragonWeaponData",
        "Lych2WeaponData","Lych3WeaponData","LychWeaponData","Ogre1WeaponData","Ogre2WeaponData",
        "PurpleDragonWeaponData","RedDragonWeaponData","Witch2WeaponData","WitchWeaponData"]
names = [os.path.splitext(os.path.basename(f))[0] for f in glob.glob(os.path.join(SRC_ASSET, "Weapon_*.asset"))] + boss

template = open(TEMPLATE_META, encoding="utf-8").read()
old_guid = re.search(r"^guid: ([0-9a-f]+)", template, re.M).group(1)
old_spriteid = re.search(r"spriteID: ([0-9a-f]+)", template).group(1)

os.makedirs(ICON_DIR, exist_ok=True)

copied, attached, skipped_boss = 0, 0, 0
for n in names:
    # asset goc co icon?
    src_t = open(os.path.join(SRC_ASSET, n + ".asset"), encoding="utf-8").read()
    has_icon = "guid" in re.search(r"^\s*Icon:\s*(.+)$", src_t, re.M).group(1)
    png = os.path.join(IMG, n + ".png")
    if not has_icon or not os.path.exists(png):
        skipped_boss += 1
        continue

    # 1) copy png
    shutil.copyfile(png, os.path.join(ICON_DIR, n + ".png"))
    copied += 1

    # 2) sinh .meta guid rieng
    guid = uuid.uuid4().hex
    sprite_id = guid[:24] + "00000000"
    meta = template.replace(old_guid, guid).replace(old_spriteid, sprite_id)
    with open(os.path.join(ICON_DIR, n + ".png.meta"), "w", encoding="utf-8", newline="\n") as f:
        f.write(meta)

    # 3) gan icon vao weapon .asset (thay dong icon: {fileID: 0})
    wpath = os.path.join(WEAPON_DIR, n + ".asset")
    wt = open(wpath, encoding="utf-8").read()
    new_line = "  icon: {fileID: 21300000, guid: %s, type: 3}" % guid
    wt2 = re.sub(r"^\s*icon: \{fileID: 0\}\s*$", new_line, wt, count=1, flags=re.M)
    assert wt2 != wt, "khong thay dong icon o " + n
    with open(wpath, "w", encoding="utf-8", newline="\n") as f:
        f.write(wt2)
    attached += 1

print("PNG copied      :", copied)
print("Icon attached   :", attached)
print("Boss/null (skip):", skipped_boss)
