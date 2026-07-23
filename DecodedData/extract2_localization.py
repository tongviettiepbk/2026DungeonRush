#!/usr/bin/env python3
# Extract localization (Unity Localization StringTables from Addressables) + forge table
import zipfile, io, os, re, json, csv, glob
import UnityPy

XAPK = r"E:\00Work\00Project\2026DungOnRush\Dungeon+Rush_41_APKPure.xapk"
SRC  = r"E:\00Work\00Project\2026DungOnRush\AssetRipper\ExportedProject\Assets\Scripts\Assembly-CSharp"
OUT  = r"E:\00Work\00Project\2026DungOnRush\DecodedData"
LOC  = os.path.join(OUT, "localization")
os.makedirs(LOC, exist_ok=True)

z = zipfile.ZipFile(XAPK)
apk = zipfile.ZipFile(io.BytesIO(z.read("com.lavalabs.dungeonrush.apk")))
def load(name): return UnityPy.load(io.BytesIO(apk.read(name)))

# ---- 1. shared: collection -> {id: key} ----
shared = {}  # collection short name -> {id:key}
env = load("assets/aa/Android/localization-assets-shared_assets_all.bundle")
for o in env.objects:
    if o.type.name != "MonoBehaviour": continue
    d = o.read_typetree()
    coll = d.get("m_TableCollectionName")  # e.g. "Items"
    if coll is None: continue
    m = {}
    for e in d.get("m_Entries", []):
        m[e["m_Id"]] = e["m_Key"]
    shared[coll] = m

# ---- 2. per-locale string tables ----
locale_bundles = [n for n in apk.namelist()
                  if n.startswith("assets/aa/Android/localization-string-tables-")]
def locale_of(name):
    m = re.search(r'localization-string-tables-(.+?)_assets_all', name)
    return m.group(1)

all_data = {}  # locale -> {fullkey: text}
for b in locale_bundles:
    loc = locale_of(b)
    env = load(b)
    kv = {}
    for o in env.objects:
        if o.type.name != "MonoBehaviour": continue
        d = o.read_typetree()
        name = d.get("m_Name","")            # e.g. "Items_en"
        coll = re.sub(r'_[^_]+$', '', name)    # -> "Items"
        id2key = shared.get(coll, {})
        for e in d.get("m_TableData", []):
            txt = e.get("m_Localized","")
            if not txt: continue
            key = id2key.get(e["m_Id"])
            if key: kv[key] = txt
    all_data[loc] = kv
    print(f"  {loc}: {len(kv)} strings")

# canonical english locale key
en = None
for k in all_data:
    if k.startswith("english"): en = k
en_map = all_data.get(en, {})

# ---- 3. write outputs ----
# per-locale flat json
for loc, kv in all_data.items():
    safe = re.sub(r'[^a-z]', '', loc)[:2] or loc
    with open(os.path.join(LOC, f"strings_{safe}.json"), "w", encoding="utf-8") as fh:
        json.dump(kv, fh, ensure_ascii=False, indent=1, sort_keys=True)

# combined CSV: key, en, + each language (for Items.* keys, most useful)
langs = sorted(all_data.keys())
def short(l): return re.sub(r'[^a-z]', '', l)[:2]
with open(os.path.join(LOC, "strings_all_languages.csv"), "w", encoding="utf-8-sig", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(["key"] + [short(l) for l in langs])
    allkeys = set()
    for kv in all_data.values(): allkeys.update(kv)
    for k in sorted(allkeys):
        w.writerow([k] + [all_data[l].get(k, "") for l in langs])

# ---- 4. forge_rarity_probabilities from RemoteConfigController ----
src = open(os.path.join(SRC, "RemoteConfigController.cs"), encoding="utf-8").read()
m = re.search(r'private const string ubs = "([^"]*)"', src)
rarities = ["Common","Uncommon","Rare","Epic","Legendary","Mythic","Divine","Celestial","Immortal","Eternal"]
with open(os.path.join(OUT, "tables", "forge_rarity_probabilities.csv"), "w", encoding="utf-8", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(["row"] + [f"P_{r}" for r in rarities])
    for i, row in enumerate(m.group(1).split("|")):
        w.writerow([i] + row.split(","))
# also the remote-config key list
keys = re.findall(r'private const string \w+ = "([a-z_]+)"', src)
with open(os.path.join(OUT, "remote_config_keys.json"), "w", encoding="utf-8") as fh:
    json.dump(sorted(set(keys)), fh, indent=1)

# ---- 5. enrich item tables with English name/description ----
def enrich_names():
    for jf in glob.glob(os.path.join(OUT, "tables", "*.json")):
        recs = json.load(open(jf, encoding="utf-8"))
        changed = False
        for r in recs:
            lk = r.get("LocalizationKey")
            if lk and lk in en_map and "Name_en" not in r:
                r["Name_en"] = en_map[lk]; changed = True
            dk = r.get("DescriptionKey") or (lk.replace(".Name.", ".Desc.") if isinstance(lk,str) else None)
            if dk and dk in en_map and "Desc_en" not in r:
                r["Desc_en"] = en_map[dk]; changed = True
        if changed:
            json.dump(recs, open(jf, "w", encoding="utf-8"), ensure_ascii=False, indent=2, default=str)
    print("  enriched item tables with English names/descriptions")
enrich_names()

print("Total keys (en):", len(en_map))
print("Output:", LOC)
