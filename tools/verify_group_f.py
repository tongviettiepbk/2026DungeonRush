#!/usr/bin/env python3
# Verify 3 tab nhom F: doc FORMATTED_VALUE, soi #REF!/#ERROR/Loading, dem dong.
# Chay: python tools/verify_group_f.py
import json
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
TABS = ["F1. PvP Đấu trường", "F2. Clan Banner", "F3. Clan · Clan War · Chat"]


def service():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    return build("sheets", "v4", credentials=creds, cache_discovery=False)


def main():
    svc = service().spreadsheets()
    meta = svc.get(spreadsheetId=TARGET).execute()
    order = [(s["properties"]["index"], s["properties"]["title"]) for s in meta["sheets"]]
    print("tab order:")
    for i, t in sorted(order):
        print("  %2d %s" % (i, t.encode("ascii", "replace").decode()))
    res = svc.values().batchGet(
        spreadsheetId=TARGET, ranges=["'%s'" % t for t in TABS],
        valueRenderOption="FORMATTED_VALUE").execute()
    bad = 0
    for tab, vr in zip(TABS, res["valueRanges"]):
        rows = vr.get("values", [])
        nimg = 0
        for ri, row in enumerate(rows):
            for ci, cell in enumerate(row):
                s = str(cell)
                if s.startswith(("#REF", "#ERROR", "#N/A", "Loading")):
                    bad += 1
                    print("BAD CELL %s r%d c%d: %s" % (
                        tab.encode("ascii", "replace").decode(), ri + 1, ci + 1, s[:60]))
                if s == "":
                    continue
        # formatted value cua =IMAGE la chuoi rong -> dem qua formula
    res2 = svc.values().batchGet(
        spreadsheetId=TARGET, ranges=["'%s'" % t for t in TABS],
        valueRenderOption="FORMULA").execute()
    for tab, vr in zip(TABS, res2["valueRanges"]):
        rows = vr.get("values", [])
        nimg = sum(1 for row in rows for cell in row
                   if isinstance(cell, str) and cell.startswith("=IMAGE"))
        print("%s: %d rows, %d IMAGE formulas" % (
            tab.encode("ascii", "replace").decode(), len(rows), nimg))
    print("RESULT:", "FAIL %d bad cells" % bad if bad else "OK - no error cells")


if __name__ == "__main__":
    main()
