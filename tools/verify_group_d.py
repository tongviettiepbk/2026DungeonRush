#!/usr/bin/env python3
# Verify tab D1: so dong, cot anh khong #REF!/#ERROR. Print ASCII.
import json
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

c = json.load(open(CRED))["installed"]
t = json.load(open(TOK))
creds = Credentials(token=t.get("access_token"), refresh_token=t.get("refresh_token"),
                    token_uri=c["token_uri"], client_id=c["client_id"],
                    client_secret=c["client_secret"], scopes=t.get("scope", "").split())
svc = build("sheets", "v4", credentials=creds, cache_discovery=False).spreadsheets()

res = svc.values().get(spreadsheetId=TARGET, range="'D1. Companion'!A1:P40",
                       valueRenderOption="FORMATTED_VALUE").execute()
vals = res.get("values", [])
print("rows:", len(vals))
bad = 0
for i, row in enumerate(vals):
    for j, cell in enumerate(row):
        if isinstance(cell, str) and (cell.startswith("#REF") or cell.startswith("#ERROR")):
            print("BAD cell r%d c%d: %s" % (i + 1, j + 1, cell[:60]))
            bad += 1
print("bad cells:", bad)
names = [row[2] for row in vals[4:22] if len(row) > 2]
print("pets:", len(names), "|", ", ".join(n.encode("ascii", "replace").decode() for n in names))
