#!/usr/bin/env python3
# -*- coding: utf-8 -*-
import json, os, sys
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
TABS = ["I1. Remote Config", "I2. Localization", "I3. Audio"]
c = json.load(open(CRED))["installed"]; t = json.load(open(TOK))
creds = Credentials(token=t.get("access_token"), refresh_token=t.get("refresh_token"),
    token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
    scopes=t.get("scope", "").split())
svc = build("sheets", "v4", credentials=creds, cache_discovery=False)
ranges = ["'%s'" % x for x in TABS]
res = svc.spreadsheets().values().batchGet(
    spreadsheetId=TARGET, ranges=ranges, valueRenderOption="FORMATTED_VALUE").execute()
bad = 0
for vr in res["valueRanges"]:
    title = vr["range"].split("!")[0].strip("'")
    rows = vr.get("values", [])
    errs = []
    for ri, row in enumerate(rows):
        for ci, cell in enumerate(row):
            if isinstance(cell, str) and (cell.startswith("#REF") or cell.startswith("#ERROR")
                                          or cell == "#N/A" or cell.startswith("#VALUE")):
                errs.append("r%d c%d=%s" % (ri + 1, ci + 1, cell))
    bad += len(errs)
    msg = "OK" if not errs else ("BAD %d: %s" % (len(errs), errs[:5]))
    sys.stdout.buffer.write(("%-22s %d rows  %s\n" % (title, len(rows), msg)).encode("utf-8"))
print("TOTAL ERRORS:", bad)
