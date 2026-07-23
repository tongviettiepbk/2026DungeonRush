#!/usr/bin/env python3
# Crop 18 icon companion (tier 1-3 tu atlas icons1-5, tier 4-6 la png roi),
# upload len Google Drive (public), luu URL vao DecodedData/image_urls_d.json.
# Map tier_XX_pet_YY -> ten companion doi chieu bang mat (contact sheet).
# Chay: python tools/upload_images_d.py   (print ASCII vi console cp1252)
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
OUT = os.path.join(ROOT, "DecodedData", "image_urls_d.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_d")
ATLAS15 = os.path.join(TEX, "sactx-0-2048x2048-ASTC 6x6-_AtlasItemsIcons1-5-c3160f15.png")

# _name companion -> (tier, pet) — doi chieu icon bang mat 2026-07-22
PET = {
    "Companion_1_Common_Lightning_Single":   (1, 1),  # Spark Mouse (chuot vang)
    "Companion_1_Common_Heal_Single":        (1, 2),  # Magic Wool (cuu xanh)
    "Companion_1_Common_DPS_Single":         (1, 3),  # Ember Fist (khi lua)
    "Companion_2_Uncommon_Slow_Single":      (2, 1),  # Snow Fang (cao tuyet)
    "Companion_2_Uncommon_Heal_Chain":       (2, 2),  # Life Horn (huou xanh)
    "Companion_2_Uncommon_Bomber_AOE_Bomb":  (2, 3),  # Flame Wing (chim lua)
    "Companion_3_Rare_Slow_Multi":           (3, 1),  # Frost Claw (gau trang)
    "Companion_3_Rare_Lightning_Multi":      (3, 2),  # Storm Eye (cao vang sung set)
    "Companion_3_Rare_Bomber_AOE_Burn":      (3, 3),  # Blaze Tail (cao lua)
    "Companion_4_Epic_Guardian_Single":      (4, 1),  # Vital Root (cay la)
    "Companion_4_Epic_Slow_AOE":             (4, 2),  # Glacier Fist (yeti bang)
    "Companion_4_Epic_Beam_Single":          (4, 3),  # Arcane Paw (meo tim 3 mat)
    "Companion_5_Legendary_Meteor":          (5, 1),  # Star Feather (phuong hoang lua)
    "Companion_5_Legendary_HealDamage":      (5, 2),  # Phantom Gaze (meo ma xanh)
    "Companion_5_Legendary_Blaster":         (5, 3),  # Echo Wing (doi tim)
    "Companion_6_Divine_Clone":              (6, 1),  # Halo Hare (tho trang-vang)
    "Companion_6_Divine_HealNova":           (6, 2),  # Radiant Talon (rong vang)
    "Companion_6_Divine_Immortal":           (6, 3),  # Celestial Mind (sinh vat vu tru)
}

pat_rect = re.compile(r"m_Rect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: ([\d.-]+)\s*\n\s*y: ([\d.-]+)"
                      r"\s*\n\s*width: ([\d.-]+)\s*\n\s*height: ([\d.-]+)")
_atlas = None


def icon(tier, pet):
    global _atlas
    if tier <= 3:
        txt = open(os.path.join(SPRITE_DIR, "tier_%02d_pet_%02d_icon.asset" % (tier, pet)),
                   encoding="utf-8", errors="ignore").read()
        x, y, w, h = (int(float(v)) for v in pat_rect.search(txt).groups())
        if _atlas is None:
            _atlas = Image.open(ATLAS15).convert("RGBA")
        return _atlas.crop((x, _atlas.height - y - h, x + w, _atlas.height - y))
    return Image.open(os.path.join(TEX, "tier_%02d_pet_%02d_icon.png" % (tier, pet))).convert("RGBA")


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
    svc = drive()
    done = 0
    for key, (t, p) in sorted(PET.items()):
        if key in urls:
            continue
        im = icon(t, p)
        if im.width > 300:
            im = im.resize((300, round(im.height * 300 / im.width)), Image.LANCZOS)
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)
        f = svc.files().create(
            body={"name": "dungeonrush_D_" + key + ".png"},
            media_body=MediaFileUpload(dst, mimetype="image/png"),
            fields="id").execute()
        fid = f["id"]
        svc.permissions().create(fileId=fid, body={"role": "reader", "type": "anyone"}).execute()
        urls[key] = {"id": fid, "w": im.width, "h": im.height}
        done += 1
    json.dump(urls, open(OUT, "w"), indent=1)
    print("DONE. uploaded now:", done, "| total in json:", len(urls))


if __name__ == "__main__":
    main()
