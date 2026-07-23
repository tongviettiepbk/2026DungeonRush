#!/usr/bin/env python3
import json, os
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build
CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
c = json.load(open(CRED))["installed"]; t = json.load(open(TOK))
creds = Credentials(token=t.get("access_token"), refresh_token=t.get("refresh_token"),
    token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
    scopes=t.get("scope", "").split())
svc = build("sheets", "v4", credentials=creds, cache_discovery=False)
meta = svc.spreadsheets().get(spreadsheetId=TARGET).execute()
for s in meta["sheets"]:
    p = s["properties"]
    import sys
    sys.stdout.buffer.write(("%2d  %s\n" % (p["index"], p["title"])).encode("utf-8"))
