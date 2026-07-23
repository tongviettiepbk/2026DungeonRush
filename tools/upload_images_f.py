#!/usr/bin/env python3
# Upload anh nhom F (PvP & xa hoi) len Google Drive (public),
# luu URL vao DecodedData/image_urls_f.json.
# Nguon anh: PNG roi trong Assets/Texture2D (khong can crop atlas).
# - flag_XX: clan_flag_XX (base) + clan_flag_XX_color tint xanh duong (mau #8 catalog)
# - emblem_<ten>: clan_icon_<ten> nguyen ban (grayscale, game tint runtime)
# - banner_example: flag_01 + overlay do + emblem dragon trang (minh hoa ghep banner)
# Chay: python tools/upload_images_f.py crop   -> chi xuat _img_tmp_f de soi mat
#       python tools/upload_images_f.py        -> xuat + upload
import json, os, sys
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEX = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
OUT = os.path.join(ROOT, "DecodedData", "image_urls_f.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_f")

EMBLEMS = ["axe", "book", "bow", "castle", "crown", "dragon", "helmet", "horse",
           "jester_hat", "knight", "lion", "mace", "scroll", "shield", "swords",
           "symbol", "torch", "trophy", "viking_hat", "wizard_hat"]

BLUE = (45, 91, 181)   # backgroundColors[8] cua catalog
RED = (204, 27, 53)    # backgroundColors[2]


def load(name):
    return Image.open(os.path.join(TEX, name + ".png")).convert("RGBA")


def tint(im, rgb):
    r, g, b = rgb
    px = im.load()
    for yy in range(im.height):
        for xx in range(im.width):
            pr, pg, pb, pa = px[xx, yy]
            px[xx, yy] = (pr * r // 255, pg * g // 255, pb * b // 255, pa)
    return im


def flag(i, color):
    base = load("clan_flag_%02d" % i)
    over = tint(load("clan_flag_%02d_color" % i), color)
    base.alpha_composite(over)
    return base


def main():
    os.makedirs(TMP, exist_ok=True)
    imgs = {}
    for i in range(1, 9):
        imgs["flag_%02d" % i] = flag(i, BLUE)
    for e in EMBLEMS:
        imgs["emblem_" + e] = load("clan_icon_" + e)
    ex = flag(1, RED)
    dg = tint(load("clan_icon_dragon"), (255, 255, 255))
    dg.thumbnail((150, 150))
    ex.alpha_composite(dg, ((ex.width - dg.width) // 2, (ex.height - dg.height) // 2 - 10))
    imgs["banner_example"] = ex
    imgs["pvp_illustration"] = load("pvp_illustration")
    imgs["role_leader"] = load("clan_leader_icon")
    imgs["role_captain"] = load("clan_captain_icon")

    for k, im in imgs.items():
        im.save(os.path.join(TMP, k + ".png"), "PNG", optimize=True)
    if len(sys.argv) > 1 and sys.argv[1] == "crop":
        print("CROP ONLY. files in", TMP)
        return

    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    svc = build("drive", "v3", credentials=creds, cache_discovery=False)

    urls = json.load(open(OUT)) if os.path.exists(OUT) else {}
    done = 0
    for key, im in sorted(imgs.items()):
        if key in urls:
            continue
        if im.width > 300:
            im = im.resize((300, round(im.height * 300 / im.width)), Image.LANCZOS)
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)
        f = svc.files().create(
            body={"name": "dungeonrush_F_" + key + ".png"},
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
