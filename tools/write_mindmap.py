#!/usr/bin/env python3
# Ghi tab "Mind-map tính năng" vào Google Sheet DungeonRush bằng Google Sheets API v4.
# Dùng CÙNG OAuth token đã set up ở project CarSurvival (scope spreadsheets).
# Chạy: python tools/write_mindmap.py
import json, csv, os
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK  = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"   # DungeonRush - GDD
TSV = os.path.join(os.path.dirname(__file__), "..", "DecodedData", "mindmap_tinh_nang.tsv")
TAB_TITLE = "Mind-map tính năng"

def service():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "https://www.googleapis.com/auth/spreadsheets").split())
    return build("sheets", "v4", credentials=creds, cache_discovery=False)

def main():
    grid = [row for row in csv.reader(open(TSV, encoding="utf-8"), delimiter="\t")]
    svc = service().spreadsheets()
    meta = svc.get(spreadsheetId=TARGET).execute()
    first = meta["sheets"][0]["properties"]
    sid = first["sheetId"]
    print("Sheet OK | first tab id", sid, "| rows to write:", None)

    # đổi tên tab đầu -> TAB_TITLE, đưa về index 0, freeze + bold header
    reqs = [
        {"updateSheetProperties": {"properties": {"sheetId": sid, "title": TAB_TITLE,
            "index": 0, "gridProperties": {"frozenRowCount": 1}},
            "fields": "title,index,gridProperties.frozenRowCount"}},
        {"repeatCell": {"range": {"sheetId": sid, "startRowIndex": 0, "endRowIndex": 1},
            "cell": {"userEnteredFormat": {"backgroundColor": {"red": 0.2, "green": 0.2, "blue": 0.25},
                "textFormat": {"bold": True, "foregroundColor": {"red": 1, "green": 1, "blue": 1}}}},
            "fields": "userEnteredFormat(textFormat,backgroundColor)"}},
    ]
    svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()

    svc.values().clear(spreadsheetId=TARGET, range=f"'{TAB_TITLE}'").execute()
    svc.values().update(spreadsheetId=TARGET, range=f"'{TAB_TITLE}'!A1",
        valueInputOption="RAW", body={"values": grid}).execute()
    print("DONE: wrote", len(grid), "rows into first tab.")

if __name__ == "__main__":
    main()
