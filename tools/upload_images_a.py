#!/usr/bin/env python3
# Resize + upload anh minh hoa nhom A len Google Drive (public), luu URL vao
# DecodedData/image_urls_a.json de tab Sheets dung =IMAGE(...).
# Chay: python tools/upload_images_a.py
import json, os
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK  = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEX  = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
OUT  = os.path.join(ROOT, "DecodedData", "image_urls_a.json")
TMP  = os.path.join(ROOT, "DecodedData", "_img_tmp")

# key -> (ten file png trong Texture2D, chieu rong toi da khi upload)
IMAGES = {
    "cultist":      ("cultist_dungeon_illustration.png", 800),
    "dragon":       ("dragon_dungeon_illustration.png", 800),
    "zombie":       ("zombie_dungeon_illustration.png", 800),
    "pvp":          ("pvp_illustration.png", 800),
    "bossrush":     ("boss_rush_illustration.png", 800),
    "mainmap":      ("sactx-0-512x512-ASTC 6x6-_AtlasMainMap-32742ba5.png", 512),
    "enemy_cultist": ("cultist_dungeon_enemy_01.png", 265),
    "enemy_dragon":  ("dragon_green.png", 328),
    "enemy_zombie":  ("zombie_head.png", 189),
}


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
    svc = drive()
    urls = json.load(open(OUT)) if os.path.exists(OUT) else {}

    for key, (fname, maxw) in IMAGES.items():
        if key in urls:
            print("skip (da co):", key)
            continue
        src = os.path.join(TEX, fname)
        im = Image.open(src).convert("RGBA")
        if im.width > maxw:
            im = im.resize((maxw, round(im.height * maxw / im.width)), Image.LANCZOS)
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)

        f = svc.files().create(
            body={"name": "dungeonrush_A_" + key + ".png"},
            media_body=MediaFileUpload(dst, mimetype="image/png"),
            fields="id").execute()
        fid = f["id"]
        svc.permissions().create(fileId=fid, body={"role": "reader", "type": "anyone"}).execute()
        urls[key] = {"id": fid,
                     "url": "https://lh3.googleusercontent.com/d/" + fid,
                     "w": im.width, "h": im.height}
        print("uploaded:", key, fid, im.size)

    json.dump(urls, open(OUT, "w"), indent=2)
    print("DONE ->", OUT)


if __name__ == "__main__":
    main()
