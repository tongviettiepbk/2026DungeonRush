#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Upload anh nhom I (He thong nen) len Google Drive (public),
# luu URL vao DecodedData/image_urls_i.json.
# Nguon: co quoc gia ISO alpha-2 PNG roi trong Texture2D (128x128) — dung cho tab
# I2 Localization. MAPPING ngon ngu -> co la SUY DOAN (guid FlagSprite trong
# LanguageSelectPopup.prefab khong tra duoc vi thieu .meta): en->GB, ar->SA.
# Chay: python tools/upload_images_i.py
import json, os
from PIL import Image
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
from googleapiclient.http import MediaFileUpload

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEX = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "Texture2D")
OUT = os.path.join(ROOT, "DecodedData", "image_urls_i.json")
TMP = os.path.join(ROOT, "DecodedData", "_img_tmp_i")

FLAGS = {"flag_en": "GB", "flag_tr": "TR", "flag_ar": "SA", "flag_fr": "FR",
         "flag_de": "DE", "flag_it": "IT", "flag_ja": "JP", "flag_ko": "KR",
         "flag_es": "ES", "flag_pt": "BR"}


def main():
    os.makedirs(TMP, exist_ok=True)
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    svc = build("drive", "v3", credentials=creds, cache_discovery=False)

    urls = json.load(open(OUT)) if os.path.exists(OUT) else {}
    done = 0
    for key, iso in sorted(FLAGS.items()):
        if key in urls:
            continue
        im = Image.open(os.path.join(TEX, iso + ".png")).convert("RGBA")
        dst = os.path.join(TMP, key + ".png")
        im.save(dst, "PNG", optimize=True)
        f = svc.files().create(
            body={"name": "dungeonrush_I_" + key + ".png"},
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
