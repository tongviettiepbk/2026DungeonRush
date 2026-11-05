"""Chạy các query NN_*.sql (ghép _header.sql trước) qua BigQuery REST bằng token gcloud, in bảng kết quả.
Dùng: python run_all.py            # tất cả, 30 ngày gần nhất
      python run_all.py 03 07      # chỉ vài query
      python run_all.py 16 --days 365   # đọc xa hơn: query về đơn đầu của người nạp (15-18) cần đủ lịch sử
Gọi REST chứ không `bq`: trên Windows `bq.cmd` cắt mất xuống dòng của câu SQL nhiều dòng.
"""
import glob
import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = "dungeon-duck-quack-slash"
API = "https://bigquery.googleapis.com/bigquery/v2/projects/%s/queries" % PROJECT


def token():
    return subprocess.run("gcloud auth print-access-token", shell=True, capture_output=True, text=True).stdout.strip()


def call(url, body, tok):
    req = urllib.request.Request(url, data=json.dumps(body).encode() if body is not None else None,
                                 headers={"Authorization": "Bearer " + tok, "Content-Type": "application/json"})
    with urllib.request.urlopen(req) as r:
        return json.load(r)


def run(sql, tok):
    res = call(API, {"query": sql, "useLegacySql": False, "location": "US", "timeoutMs": 60000, "maxResults": 500}, tok)
    job = res["jobReference"]["jobId"]

    while not res.get("jobComplete"):
        time.sleep(2)
        res = call("%s/%s?location=US&maxResults=500" % (API, job), None, tok)

    fields = [f["name"] for f in res.get("schema", {}).get("fields", [])]
    rows = [["" if c["v"] is None else str(c["v"]) for c in r["f"]] for r in res.get("rows", [])]
    return fields, rows


def table(fields, rows):
    widths = [max([len(f)] + [len(r[i]) for r in rows]) for i, f in enumerate(fields)]
    line = lambda cells: "| " + " | ".join(c.ljust(w) for c, w in zip(cells, widths)) + " |"
    return "\n".join([line(fields)] + [line(r) for r in rows])


def main():
    args = sys.argv[1:]
    days = "30"
    if "--days" in args:
        i = args.index("--days")
        days = args[i + 1]
        del args[i:i + 2]
    header = open(os.path.join(HERE, "_header.sql"), encoding="utf-8").read().replace("INTERVAL 30 DAY", "INTERVAL %s DAY" % days)
    only = args
    tok = token()

    for path in sorted(glob.glob(os.path.join(HERE, "[0-9][0-9]_*.sql"))):
        name = os.path.basename(path)
        if only and name[:2] not in only:
            continue

        print("=" * 24, name, flush=True)
        try:
            print(table(*run(header + "\n" + open(path, encoding="utf-8").read(), tok)), flush=True)
        except urllib.error.HTTPError as e:
            print("ERROR", e.code, e.read().decode()[:1500], flush=True)


if __name__ == "__main__":
    main()
