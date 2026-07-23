#!/usr/bin/env python3
# Verify tab E1: khong co #REF!/#ERROR, spot-check vai gia tri.
# Chay: python tools/verify_group_e.py
import json
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"
TAB = "E1. Đào mỏ (Mining)"


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
    titles = [s["properties"]["title"] for s in meta["sheets"]]
    print("tab count:", len(titles))
    print("tab index:", titles.index(TAB) if TAB in titles else "MISSING!")
    res = svc.values().batchGet(spreadsheetId=TARGET, ranges=["'%s'" % TAB],
                                valueRenderOption="FORMATTED_VALUE").execute()
    rows = res["valueRanges"][0].get("values", [])
    print("rows:", len(rows))
    bad = [(i + 1, j + 1, c) for i, r in enumerate(rows) for j, c in enumerate(r)
           if isinstance(c, str) and (c.startswith("#REF") or c.startswith("#ERROR") or c.startswith("Loading"))]
    print("bad cells:", bad if bad else "NONE")
    imgs = sum(1 for r in rows for c in r if c == "")  # IMAGE cells render as "" in FORMATTED_VALUE
    # spot checks
    def find(txt):
        for i, r in enumerate(rows):
            if r and str(r[0]).startswith(txt):
                return i, r
        return None, None
    i, r = find("5.")
    print("section5 at row", i + 1, "|", r[0][:60].encode("ascii", "replace").decode())
    hdr = rows[i + 1]
    lv1 = rows[i + 2]
    lv60 = rows[i + 2 + 59]
    print("weight header cols:", len(hdr))
    print("lv1 row:", lv1)
    print("lv60 row:", lv60)
    s60 = sum(float(str(x).replace(",", "").replace("%", "")) for x in lv60[1:])
    print("lv60 sum =", round(s60, 2))
    i, r = find("6.")
    print("offers:", rows[i + 2][:2], rows[i + 3][:2])


if __name__ == "__main__":
    main()
