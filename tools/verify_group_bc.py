#!/usr/bin/env python3
# Verify 12 tab nhom B + C: ton tai, so dong, cell anh loi (#REF!/#ERROR/Loading).
# Chay: python tools/verify_group_bc.py   (print ASCII)
import json
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

TABS = ["B1. Boss Gate", "B2. Boss Rush", "B3. Boss Rush League",
        "C1. Vũ khí", "C2. Mũ", "C3. Găng tay", "C4. Nhẫn",
        "C5. Dây chuyền", "C6. Ba lô", "C7. Áo choàng",
        "C8. Cánh", "C9. Forge"]


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
    order = [s["properties"]["title"] for s in meta["sheets"]]
    print("tab order:", " | ".join(t.encode("ascii", "replace").decode() for t in order))
    missing = [t for t in TABS if t not in order]
    if missing:
        print("MISSING TABS:", missing)
        return
    res = svc.values().batchGet(spreadsheetId=TARGET,
                                ranges=["'%s'" % t for t in TABS],
                                valueRenderOption="FORMATTED_VALUE").execute()
    total_err = 0
    for t, vr in zip(TABS, res["valueRanges"]):
        rows = vr.get("values", [])
        errs = []
        n_img = 0
        for i, row in enumerate(rows):
            for j, cell in enumerate(row):
                if isinstance(cell, str):
                    if cell.startswith(("#REF", "#ERROR", "#N/A", "Loading")):
                        errs.append((i + 1, j + 1, cell[:30]))
        print("%-24s rows=%3d  err_cells=%d %s" % (
            t.encode("ascii", "replace").decode(), len(rows), len(errs), errs[:5]))
        total_err += len(errs)
    print("TOTAL error cells:", total_err)


if __name__ == "__main__":
    main()
