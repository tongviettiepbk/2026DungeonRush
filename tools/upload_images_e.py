#!/usr/bin/env python3
# Crop icon Mining (khoi quang, icon tai nguyen, cong cu) tu atlas
# _AtlasMining-0ad2c8bf (guid 42c020e1b6fd5bb4593c8481ddcd39f7), upload len
# Google Drive (public), luu URL vao DecodedData/image_urls_e.json.
# Chay: python tools/upload_images_e.py crop   -> chi crop ra _img_tmp_e de soi mat
#       python tools/upload_images_e.py        -> crop + upload
import json, os, re, sys
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEX = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
SPRITE_DIR = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Sprite")
OUT = os.path.join(ROOT, "DecodedData", "image_urls_e.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_e")
ATLAS = os.path.join(TEX, "sactx-0-1024x2048-ASTC 6x6-_AtlasMining-0ad2c8bf.png")

# key trong json -> ten sprite tren atlas Mining
SPRITES = {
    "block_Dirt": "dirt",
    "block_Stone": "stone_obstacle_broken",  # doi chieu bang mat: khoi da xam (nut); "mining (1)" la icon cuoc+da
    "block_Coal": "coal_mine",
    "block_Iron": "iron_mine",
    "block_Ruby": "ruby_mine",
    "block_Emerald": "emerald_mine",
    "block_Gold": "gold_mine",
    "block_Diamond": "diamond_mine",
    "icon_Coal": "coal_icon",
    "icon_Iron": "iron_icon",
    "icon_Ruby": "ruby_icon",
    "icon_Emerald": "emerald_icon",
    "icon_Gold": "gold_icon",
    "icon_Diamond": "diamond_icon",
    "icon_Stone": "stone_icon",
    "tool_Pickaxe": "pickaxe",
    "tool_GoldenPickaxe": "gold_pickaxe",
    "tool_Drill": "drill",
}

pat_rect = re.compile(r"m_Rect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: ([\d.-]+)\s*\n\s*y: ([\d.-]+)"
                      r"\s*\n\s*width: ([\d.-]+)\s*\n\s*height: ([\d.-]+)")
_atlas = None


def crop(name):
    global _atlas
    txt = open(os.path.join(SPRITE_DIR, name + ".asset"), encoding="utf-8", errors="ignore").read()
    x, y, w, h = (int(float(v)) for v in pat_rect.search(txt).groups())
    if _atlas is None:
        _atlas = Image.open(ATLAS).convert("RGBA")
    return _atlas.crop((x, _atlas.height - y - h, x + w, _atlas.height - y))


def contact_sheet(imgs):
    cell = 96
    keys = sorted(imgs)
    cols = 6
    rows = (len(keys) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * (cell + 14)), (40, 40, 48, 255))
    for i, k in enumerate(keys):
        im = imgs[k].copy()
        im.thumbnail((cell - 8, cell - 8))
        cx, cy = (i % cols) * cell, (i // cols) * (cell + 14)
        sheet.paste(im, (cx + (cell - im.width) // 2, cy + (cell - im.height) // 2), im)
    sheet.save(os.path.join(TMP, "_contact_sheet.png"))
    print("contact sheet keys in order:")
    for i, k in enumerate(keys):
        print("  %d,%d: %s" % (i // cols, i % cols, k))


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
    imgs = {k: crop(sp) for k, sp in SPRITES.items()}
    for k, im in imgs.items():
        im.save(os.path.join(TMP, k + ".png"), "PNG", optimize=True)
    contact_sheet(imgs)
    if len(sys.argv) > 1 and sys.argv[1] == "crop":
        print("CROP ONLY. files in", TMP)
        return
    urls = json.load(open(OUT)) if os.path.exists(OUT) else {}
    svc = drive()
    done = 0
    for key, im in sorted(imgs.items()):
        if key in urls:
            continue
        if im.width > 300:
            im = im.resize((300, round(im.height * 300 / im.width)), Image.LANCZOS)
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)
        f = svc.files().create(
            body={"name": "dungeonrush_E_" + key + ".png"},
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
