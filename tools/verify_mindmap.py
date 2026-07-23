#!/usr/bin/env python3
import json, sys
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK  = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
c = json.load(open(CRED))["installed"]; t = json.load(open(TOK))
creds = Credentials(token=t.get("access_token"), refresh_token=t.get("refresh_token"),
    token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
    scopes=t.get("scope").split())
svc = build("sheets", "v4", credentials=creds, cache_discovery=False).spreadsheets()
meta = svc.get(spreadsheetId=TARGET).execute()
first = meta["sheets"][0]["properties"]
vals = svc.values().get(spreadsheetId=TARGET, range=f"'{first['title']}'!A1:F5").execute().get("values", [])
last = svc.values().get(spreadsheetId=TARGET, range=f"'{first['title']}'!A34:F34").execute().get("values", [])
sys.stdout.buffer.write(("first tab title: " + first["title"] + "\n").encode("utf-8"))
sys.stdout.buffer.write(("frozen rows: " + str(first.get("gridProperties", {}).get("frozenRowCount")) + "\n").encode("utf-8"))
for r in vals:
    sys.stdout.buffer.write((" | ".join(r) + "\n").encode("utf-8"))
sys.stdout.buffer.write(("...\nrow34: " + " | ".join(last[0] if last else []) + "\n").encode("utf-8"))
