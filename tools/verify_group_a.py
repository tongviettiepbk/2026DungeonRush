#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Doc lai 6 tab nhom A, bao so dong + kiem tra o =IMAGE co loi khong.
# Chay: python tools/verify_group_a.py
import json, sys
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
TABS = ["A1. Map & Môi trường", "A2. Tier (độ khó)", "A3. Dungeon",
        "A4. Dungeon Theme", "A5. Army Power", "A6. Kinh nghiệm & Level"]


def out(s):
    sys.stdout.buffer.write((s + "\n").encode("utf-8"))


def main():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(token=t.get("access_token"), refresh_token=t.get("refresh_token"),
                        token_uri=c["token_uri"], client_id=c["client_id"],
                        client_secret=c["client_secret"], scopes=t.get("scope", "").split())
    svc = build("sheets", "v4", credentials=creds, cache_discovery=False).spreadsheets()
    res = svc.values().batchGet(spreadsheetId=TARGET,
                                ranges=["'%s'" % t for t in TABS],
                                valueRenderOption="FORMATTED_VALUE").execute()
    for tab, vr in zip(TABS, res["valueRanges"]):
        vals = vr.get("values", [])
        errs = [(i + 1, j + 1, cell) for i, row in enumerate(vals) for j, cell in enumerate(row)
                if isinstance(cell, str) and cell.startswith(("#ERROR", "#N/A", "#REF", "#VALUE"))]
        out("%-26s rows=%-4d cols=%-3d loi=%s" % (
            tab, len(vals), max((len(r) for r in vals), default=0),
            "KHONG" if not errs else errs[:5]))
        out("   header: " + " | ".join(str(x) for x in vals[3][:8]))


if __name__ == "__main__":
    main()
