#!/usr/bin/env python3
# Doi chieu 18 asset sinh ra vs data GOC trong CompanionData.json (== source MonoBehaviour).
# Bao ca sai lech moi field stat.
import os, json, re

ROOT = r"E:\00Work\00Project\2026DungOnRush"
JSON = os.path.join(ROOT, "DecodedData", "tables", "CompanionData.json")
GEN = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets",
                   "Resources", "Scriptable Objects", "Companions")

import importlib.util
spec = importlib.util.spec_from_file_location("gen", os.path.join(ROOT, "tools", "gen_companion_assets.py"))
# reuse FIELDS list without running side effects: parse manually
FIELDS = []
for line in open(os.path.join(ROOT, "tools", "gen_companion_assets.py"), encoding="utf-8"):
    m = re.match(r'\s*\("([^"]+)",\s*"([^"]+)",\s*"([sfib])"\),', line)
    if m:
        FIELDS.append((m.group(1), m.group(2), m.group(3)))


def parse_asset(path):
    d = {}
    for line in open(path, encoding="utf-8"):
        m = re.match(r"  ([a-zA-Z]+): (.*)$", line.rstrip("\n"))
        if m:
            d[m.group(1)] = m.group(2)
    return d


def norm_num(s):
    try:
        f = float(s)
        return f
    except Exception:
        return s


data = json.load(open(JSON, encoding="utf-8"))
mismatch = 0
checked = 0
for r in data:
    name = r["_name"]
    ap = os.path.join(GEN, name + ".asset")
    if not os.path.exists(ap):
        print("MISSING asset:", name); mismatch += 1; continue
    a = parse_asset(ap)
    for ykey, jkey, kind in FIELDS:
        want = r.get(jkey)
        got = a.get(ykey)
        checked += 1
        if kind == "s":
            gs = got
            if gs is not None and len(gs) >= 2 and gs[0] == "'" and gs[-1] == "'":
                gs = gs[1:-1].replace("''", "'")
            if (want or "") != (gs or ""):
                print("MISMATCH str", name, ykey, "| want=", repr(want), "got=", repr(gs)); mismatch += 1
        elif kind == "b":
            if int(bool(want)) != int(got):
                print("MISMATCH bool", name, ykey, want, got); mismatch += 1
        else:
            if norm_num(want) != norm_num(got):
                print("MISMATCH num", name, ykey, "| want=", want, "got=", got); mismatch += 1

print("checked fields:", checked, "| mismatches:", mismatch, "| assets:", len(data))
