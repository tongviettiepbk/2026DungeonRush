#!/usr/bin/env python3
# Crop icon item tu atlas (theo sprite rect) + anh boss, upload len Google Drive
# (public), luu URL vao DecodedData/image_urls_bc.json cho tab nhom B & C.
# Chay: python tools/upload_images_bc.py   (print ASCII vi console cp1252)
import json, os, re
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEX = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
SPRITE_DIR = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Sprite")
TABLES = os.path.join(ROOT, "DecodedData", "tables")
OUT = os.path.join(ROOT, "DecodedData", "image_urls_bc.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_bc")

# guid texture (trong sprite .asset) -> file atlas png
ATLAS = {
    "aefe761f7d1edad42a7c824b9f41f189": "sactx-0-2048x2048-ASTC 6x6-_AtlasItemsIcons1-5-c3160f15.png",
    "0062f43cc1e77a044bdb938d217beef8": "sactx-0-2048x2048-ASTC 6x6-_AtlasItemsIcons6-10-15f93d1b.png",
    "dda985d63c6f64d4aa9203a800f37474": "sactx-0-2048x1024-ASTC 6x6-_AtlasItem_Common_Uncommon-5a59a33c.png",
    "bae7ce38a5a47d24ab8668d8343af817": "sactx-0-1024x2048-ASTC 6x6-_AtlasItem_Rare_Epic-66cefc1c.png",
    "e76ff7c11114c2d4a96664c780fd42d0": "sactx-0-2048x2048-ASTC 6x6-_AtlasItem_Legendary_Mythic-c174cad3.png",
    "f634fbcb5864aa64e9aa928ff3c137df": "sactx-0-2048x2048-ASTC 6x6-_Atlas_Immortal_Divine-54d1ad1b.png",
}

pat_rect = re.compile(r"m_Rect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: ([\d.-]+)\s*\n\s*y: ([\d.-]+)"
                      r"\s*\n\s*width: ([\d.-]+)\s*\n\s*height: ([\d.-]+)")
pat_tex = re.compile(r"texture: \{fileID: \d+, guid: ([0-9a-f]+)")
_atlas_cache = {}


def crop_sprite(name):
    """Crop sprite `name` tu atlas theo rect trong Assets/Sprite/<name>.asset. None neu khong co."""
    p = os.path.join(SPRITE_DIR, name + ".asset")
    if not os.path.exists(p):
        return None
    txt = open(p, encoding="utf-8", errors="ignore").read()
    m, t = pat_rect.search(txt), pat_tex.search(txt)
    if not m or not t or t.group(1) not in ATLAS:
        return None
    x, y, w, h = (int(float(v)) for v in m.groups())
    g = t.group(1)
    if g not in _atlas_cache:
        _atlas_cache[g] = Image.open(os.path.join(TEX, ATLAS[g])).convert("RGBA")
    a = _atlas_cache[g]
    return a.crop((x, a.height - y - h, x + w, a.height - y))


def sprite_for(table_name):
    """Ten asset item -> ten sprite icon."""
    m = re.match(r"Weapon_([mr])_(\d+)_(\d+)$", table_name)
    if m:
        kind = "melee_weapon" if m.group(1) == "m" else "range_weapon"
        return "tier_%02d_%s_%02d_icon_transparent" % (int(m.group(2)), kind, int(m.group(3)))
    m = re.match(r"Helmet_(\d+)_(\d+)$", table_name)
    if m:
        return "tier_%02d_hat_%02d_icon_transparent" % (int(m.group(1)), int(m.group(2)))
    m = re.match(r"Gloves_(\d+)_(\d+)$", table_name)
    if m:
        return "tier_%02d_glove_%02d_icon_transparent" % (int(m.group(1)), int(m.group(2)))
    m = re.match(r"Ring_(\d+)_(\d+)$", table_name)
    if m:
        return "tier_%02d_ring_%02d" % (int(m.group(1)), int(m.group(2)))
    m = re.match(r"Neckle_(\d+)_(\d+)$", table_name)
    if m:
        return "tier_%02d_necklace_%02d" % (int(m.group(1)), int(m.group(2)))
    m = re.match(r"Backpack_(\d+)_(\d+)$", table_name)
    if m:
        return "tier_%02d_back_item_%02d_icon_transparent" % (int(m.group(1)), int(m.group(2)))
    return None


# item enemy/boss -> file png rieng trong Texture2D
ENEMY_PNG = {
    "CultistWeaponData_Melee": "cultist_dungeon_melee_weapon.png",
    "CultistWeaponData_Ranged": "cultist_dungeon_range_weapon.png",
    "LychWeaponData": "lych_01_weapon.png",
    "Lych2WeaponData": "lych_02_weapon.png",
    "Lych3WeaponData": "lych_03_weapon.png",
    "WitchWeaponData": "witch_01_weapon.png",
    "Witch2WeaponData": "witch_02_weapon.png",
    "LychGloveData": "lych_01_glove.png",
    "Lych2GloveData": "lych_02_glove.png",
    "Lych3GloveData": "lych_03_glove.png",
}

BOSS_PNG = {  # BossName -> file (doi chieu mau sac bang mat)
    "Dark Lich": "lych_01.png", "Crimson Lich": "lych_02.png", "Elder Lich": "lych_03.png",
    "Black Dragon": "dragon_purple.png", "Green Dragon": "dragon_green.png", "Red Dragon": "dragon_red.png",
    "Ogre King": "ogre_01.png", "Ogre Chieftain": "ogre_02.png",
    "Green Hag": "witch_01.png", "Purple Hag": "witch_02.png",
}


def collect_jobs():
    """key -> PIL Image"""
    jobs = {}

    def add(key, im):
        if im is not None and key not in jobs:
            jobs[key] = im

    for t in ["WeaponData", "HelmetData", "GlovesData", "RingData",
              "NecklaceData", "BackpackData"]:
        for r in json.load(open(os.path.join(TABLES, t + ".json"), encoding="utf-8")):
            n = r["_name"]
            sp = sprite_for(n)
            if sp:
                im = crop_sprite(sp)
                if im is None and sp.endswith("_icon_transparent"):  # fallback sprite thuong
                    im = crop_sprite(sp[:-len("_icon_transparent")])
                add(n, im)
            elif n in ENEMY_PNG:
                add(n, Image.open(os.path.join(TEX, ENEMY_PNG[n])).convert("RGBA"))
    for r in json.load(open(os.path.join(TABLES, "CapeData.json"), encoding="utf-8")):
        cid = int(r["CapeId"])
        add(r["_name"], crop_sprite("tier_%02d_cloak_%02d_icon" % ((cid + 1) // 2, (cid - 1) % 2 + 1)))
    for r in json.load(open(os.path.join(TABLES, "WingData.json"), encoding="utf-8")):
        f = os.path.join(TEX, "tier_%02d_wing.png" % (int(r["WingId"]) + 1))
        add(r["_name"], Image.open(f).convert("RGBA") if os.path.exists(f) else None)
    for boss, fn in BOSS_PNG.items():
        add("boss_" + boss.replace(" ", "_"), Image.open(os.path.join(TEX, fn)).convert("RGBA"))
    add("forge", Image.open(os.path.join(TEX, "auto-forge.png")).convert("RGBA"))
    return jobs


def drive():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    return build("drive", "v3", credentials=creds, cache_discovery=False)


def main():
    os.makedirs(TMP, exist_ok=True)
    urls = json.load(open(OUT)) if os.path.exists(OUT) else {}
    jobs = collect_jobs()
    print("total images:", len(jobs), "| already uploaded:", len([k for k in jobs if k in urls]))
    svc = drive()
    done = 0
    for key, im in sorted(jobs.items()):
        if key in urls:
            continue
        if im.width > 300:
            im = im.resize((300, round(im.height * 300 / im.width)), Image.LANCZOS)
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)
        f = svc.files().create(
            body={"name": "dungeonrush_BC_" + key + ".png"},
            media_body=MediaFileUpload(dst, mimetype="image/png"),
            fields="id").execute()
        fid = f["id"]
        svc.permissions().create(fileId=fid, body={"role": "reader", "type": "anyone"}).execute()
        urls[key] = {"id": fid, "w": im.width, "h": im.height}
        done += 1
        if done % 20 == 0:
            json.dump(urls, open(OUT, "w"), indent=1)
            print("uploaded", done, "...")
    json.dump(urls, open(OUT, "w"), indent=1)
    missing = [k for k in jobs if k not in urls]
    print("DONE. uploaded now:", done, "| total in json:", len(urls), "| missing:", missing)


if __name__ == "__main__":
    main()
