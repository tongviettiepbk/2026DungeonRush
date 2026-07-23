#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Upload anh nhom G (Tien trinh/Meta) + H (Kinh te/Shop) len Google Drive (public),
# luu URL vao DecodedData/image_urls_gh.json.
# Nguon:
#  - mastery_NN: crop tu atlas _AtlasAllUI_1 (Unity y tu day: top = H - y - h)
#  - chest/pickaxe/reward...: PNG roi trong Texture2D
# Mapping mastery_NN -> upgrade la SUY DOAN theo hinh (khong co .meta de tra guid).
# Chay: python tools/upload_images_gh.py crop   -> chi xuat _img_tmp_gh de soi mat
#       python tools/upload_images_gh.py        -> xuat + upload
import json, os, re, sys, glob
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
ASSETS = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets")
TEX = os.path.join(ASSETS, "Texture2D")
SPR = os.path.join(ASSETS, "Sprite")
ATLAS = os.path.join(TEX, "sactx-0-2048x1024-ASTC 4x4-_AtlasAllUI_1-ad210386.png")
OUT = os.path.join(ROOT, "DecodedData", "image_urls_gh.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_gh")

# key -> mastery sprite tren atlas (mapping suy doan theo hinh)
MASTERY = {
    "m_autoloot": "mastery_01",       # 2 mui ten len = toc do auto-loot
    "m_gemoffer": "mastery_03",       # ruong + % = ti le ra Gem trong Offer Chest
    "m_boostdur": "mastery_04",       # tia set + dong ho cat = keo dai EXP Boost
    "m_boostworth": "mastery_05",     # tia set = he so EXP Boost
    "m_offlinetime": "mastery_06",    # dong ho = thoi gian offline toi da
    "m_offlineworth": "mastery_07",   # dong ho + vien = toc do thu offline
    "m_movespeed": "mastery_08",      # giay co canh = toc do di chuyen
    "m_companion": "mastery_09",      # dau chan thu = so companion moi lan trieu
    "m_maxitem": "mastery_10",        # o item + dau cong = max item level (Forge)
}
# PNG roi trong Texture2D
STANDALONE = {
    "m_pickaxe": "pickaxe",           # max pickaxe (Mining)
    "chest_standard": "standart_chest",
    "chest_epic": "epic_chest",
    "chest_legendary": "legendary_chest",
    "chest_mythic": "mythic_chest",
    "chest_epic_open": "epic_chest_opened",
    "chest_legend_open": "legendary_chest_opened",
    "chest_mythic_open": "mythic_chest_opened",
    "chest_offer_01": "chest_offer_01",
    "chest_offer_02": "chest_offer_02",
    "gold_pickaxe": "gold_pickaxe",
    "drill": "drill",
    "reward": "reward",
}


def crop_atlas():
    im = Image.open(ATLAS).convert("RGBA")
    H = im.height
    out = {}
    for key, name in MASTERY.items():
        t = open(os.path.join(SPR, name + ".asset"), encoding="utf-8", errors="ignore").read()
        m = re.search(r"m_Rect:\s*\n\s*serializedVersion:\s*\d+\s*\n\s*x:\s*([\d.]+)\s*\n"
                      r"\s*y:\s*([\d.]+)\s*\n\s*width:\s*([\d.]+)\s*\n\s*height:\s*([\d.]+)", t)
        x, y, w, h = map(float, m.groups())
        out[key] = im.crop((round(x), round(H - y - h), round(x + w), round(H - y)))
    return out


def main():
    os.makedirs(TMP, exist_ok=True)
    imgs = crop_atlas()
    for key, name in STANDALONE.items():
        p = os.path.join(TEX, name + ".png")
        if os.path.exists(p):
            imgs[key] = Image.open(p).convert("RGBA")
        else:
            print("MISSING", name)
    for k, im in imgs.items():
        im.save(os.path.join(TMP, k + ".png"), "PNG", optimize=True)
    if len(sys.argv) > 1 and sys.argv[1] == "crop":
        print("CROP ONLY. files in", TMP, "| count", len(imgs))
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
            body={"name": "dungeonrush_GH_" + key + ".png"},
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
