#!/usr/bin/env python3
# Decode Dungeon Rush ScriptableObject .asset tables -> JSON + CSV
import os, re, glob, json, csv, sys
from collections import defaultdict

ASSETS = r"E:\00Work\00Project\2026DungOnRush\AssetRipper\ExportedProject\Assets"
SRC    = os.path.join(ASSETS, "Scripts", "Assembly-CSharp")
OUT    = r"E:\00Work\00Project\2026DungOnRush\DecodedData"
os.makedirs(OUT, exist_ok=True)
os.makedirs(os.path.join(OUT, "tables"), exist_ok=True)

import yaml

# ---------- 1. Parse C# sources: class fields (name,type) + enums ----------
field_re = re.compile(r'^\s*public\s+([\w<>\[\],\.]+)\s+(\w+)\s*;', re.M)
class_re = re.compile(r'\b(?:public\s+)?(?:abstract\s+|sealed\s+)?class\s+(\w+)')
enum_re  = re.compile(r'\benum\s+(\w+)\s*(?::\s*\w+\s*)?\{([^}]*)\}', re.S)

class_fields = {}   # class -> {fieldName: typeName}
enums = {}          # enumName -> {int: label}

for f in glob.glob(os.path.join(SRC, "*.cs")):
    txt = open(f, encoding='utf-8', errors='replace').read()
    # enums
    for m in enum_re.finditer(txt):
        name = m.group(1)
        body = m.group(2)
        cur = 0
        mapping = {}
        for tok in body.split(','):
            tok = tok.strip()
            if not tok or tok.startswith('//'):
                continue
            tok = tok.split('//')[0].strip()
            if not tok:
                continue
            if '=' in tok:
                k, v = tok.split('=', 1)
                k = k.strip(); v = v.strip()
                try:
                    cur = int(v, 0)
                except ValueError:
                    # reference to another member or expression; skip precise value
                    mapping[k] = None
                    continue
                mapping[k] = cur
            else:
                mapping[tok] = cur
            cur += 1
        enums[name] = {v: k for k, v in mapping.items() if v is not None}
    # classes (take first class in file = the main one)
    cm = class_re.search(txt)
    if cm:
        cname = cm.group(1)
        fields = {}
        for fm in field_re.finditer(txt):
            ftype, fname = fm.group(1), fm.group(2)
            fields[fname] = ftype
        class_fields.setdefault(cname, {}).update(fields)

# Merge base-class fields for known inheritance (ItemData base)
# Detect ": Base" to inherit fields
inherit_re = re.compile(r'class\s+(\w+)\s*:\s*([\w<>,\s]+)')
inheritance = {}
for f in glob.glob(os.path.join(SRC, "*.cs")):
    txt = open(f, encoding='utf-8', errors='replace').read()
    for m in inherit_re.finditer(txt):
        child = m.group(1)
        bases = [b.strip() for b in m.group(2).split(',')]
        inheritance[child] = bases
def all_fields(cls, seen=None):
    if seen is None: seen=set()
    if cls in seen: return {}
    seen.add(cls)
    out = dict(class_fields.get(cls, {}))
    for b in inheritance.get(cls, []):
        for k,v in all_fields(b, seen).items():
            out.setdefault(k, v)
    return out

# ---------- 2. Load & parse assets ----------
UNITY_KEYS = {'m_ObjectHideFlags','m_CorrespondingSourceObject','m_PrefabInstance',
    'm_PrefabAsset','m_GameObject','m_Enabled','m_EditorHideFlags','m_Script',
    'm_EditorClassIdentifier'}
guid_re = re.compile(r'm_Script:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)')

def load_asset(path):
    raw = open(path, encoding='utf-8', errors='replace').read()
    g = guid_re.search(raw)
    guid = g.group(1) if g else None
    # strip unity yaml directives / doc tag
    lines = raw.splitlines()
    body = []
    for ln in lines:
        if ln.startswith('%'): continue
        if ln.startswith('--- '): continue
        body.append(ln)
    try:
        doc = yaml.safe_load("\n".join(body))
    except Exception as e:
        return guid, None, None
    if not isinstance(doc, dict) or 'MonoBehaviour' not in doc:
        return guid, None, None
    mb = doc['MonoBehaviour']
    name = mb.get('m_Name')
    data = {k: v for k, v in mb.items() if k not in UNITY_KEYS and k != 'm_Name'}
    return guid, name, data

# gather assets by guid
groups = defaultdict(list)  # guid -> [(name, data, path)]
for root in ('MonoBehaviour', 'Resources'):
    for path in glob.glob(os.path.join(ASSETS, root, '**', '*.asset'), recursive=True):
        guid, name, data = load_asset(path)
        if data is None:
            continue
        groups[guid].append((name, data, path))

# ---------- 3. Resolve class name per guid via field overlap ----------
SKIP_CLASSES = {'TMP_SpriteAsset','SpriteAsset','AtlasAsset','SkeletonDataAsset',
    'UniversalRenderPipelineGlobalSettings','BaseDTO'}  # BaseDTO = misresolved AstarGizmos engine asset
def resolve_class(records):
    # union of keys across group
    keys = set()
    for _, data, _ in records:
        keys.update(data.keys())
    best, best_score = None, -1
    for cls, fields in class_fields.items():
        if not fields: continue
        fk = set(all_fields(cls).keys())
        if not fk: continue
        inter = len(keys & fk)
        if inter == 0: continue
        # score: fraction of asset keys matched, tiebreak by fewer extra class fields
        score = inter - 0.01*len(fk - keys) - 0.001*len(keys - fk)
        if inter/len(keys) < 0.5:  # require majority of asset keys to be real fields
            continue
        if score > best_score:
            best, best_score = cls, score
    return best

# Ring & Necklace are field-identical to abstract ItemData base -> disambiguate by guid
GUID_OVERRIDE = {
    '7af5f1a3859c3fc63c6e880c4bad3ca5': 'RingData',
    '3e49dd65944305d182771504d45baafb': 'NecklaceData',
}
guid_class = {}
for guid, recs in groups.items():
    cls = GUID_OVERRIDE.get(guid) or resolve_class(recs)
    guid_class[guid] = cls

# ---------- 4. Enum resolution helper ----------
def enrich(cls, data):
    """Add *_name for enum-typed int fields."""
    if not cls: return data
    ftypes = all_fields(cls)
    out = {}
    for k, v in data.items():
        out[k] = v
        t = ftypes.get(k)
        if t and t in enums and isinstance(v, int):
            lbl = enums[t].get(v)
            if lbl is not None:
                out[k + '__enum'] = lbl
    return out

_hex_re = re.compile(r'^[0-9a-fA-F]+$')
def decode_blobs(obj, key=None):
    """Unity serializes primitive arrays (int[]/float[]) as a LE hex string.
    Decode them to lists. Skip 32-char asset guids (key == 'guid')."""
    if isinstance(obj, dict):
        return {k: decode_blobs(v, k) for k, v in obj.items()}
    if isinstance(obj, list):
        return [decode_blobs(v) for v in obj]
    if isinstance(obj, str) and key != 'guid' and len(obj) >= 8 and len(obj) % 8 == 0 \
            and _hex_re.match(obj):
        b = bytes.fromhex(obj)
        return [int.from_bytes(b[i:i+4], 'little') for i in range(0, len(b), 4)]
    return obj

def scalarize(v):
    """Return scalar for CSV, else None (skip complex)."""
    if isinstance(v, (int, float, str)) or v is None:
        return v
    if isinstance(v, dict):
        # unity ref {fileID,guid,type} -> guid ref
        if 'guid' in v:
            return f"ref:{v.get('guid')}"
        if set(v.keys()) <= {'fileID'}:
            return None
    return None

# ---------- 5. Emit ----------
summary = []
by_class = defaultdict(list)  # className -> records (with _name)
for guid, recs in groups.items():
    cls = guid_class.get(guid)
    if cls is None or cls in SKIP_CLASSES:
        continue   # engine/UI/font assets (TMP sprite, Spine, URP, fonts, localization...)
    label = cls
    for name, data, path in recs:
        rec = {'_name': name}
        rec.update(enrich(cls, decode_blobs(data)))
        by_class[label].append(rec)

engine_like = {'AudioConfig','AdjustSettings','AppLovinSettings','AstarPath',
    'DOTweenSettings','ScriptableRendererFeature'}

index_rows = []
for label, records in sorted(by_class.items(), key=lambda x: -len(x[1])):
    # JSON (full, nested preserved)
    jpath = os.path.join(OUT, 'tables', f"{label}.json")
    with open(jpath, 'w', encoding='utf-8') as fh:
        json.dump(records, fh, ensure_ascii=False, indent=2, default=str)
    # CSV (scalar/flat fields only, union of columns)
    cols = []
    seen = set()
    for r in records:
        for k, v in r.items():
            sv = scalarize(v)
            if sv is not None or k == '_name':
                if k not in seen:
                    seen.add(k); cols.append(k)
    cpath = os.path.join(OUT, 'tables', f"{label}.csv")
    with open(cpath, 'w', encoding='utf-8', newline='') as fh:
        w = csv.writer(fh)
        w.writerow(cols)
        for r in records:
            row = []
            for k in cols:
                if k in r:
                    sv = scalarize(r[k])
                    row.append(sv if sv is not None else '')
                else:
                    row.append('')
            w.writerow(row)
    index_rows.append((label, len(records), guid_class_repr(label) if False else ''))
    print(f"{len(records):4d}  {label}")

# index file
with open(os.path.join(OUT, '_index.md'), 'w', encoding='utf-8') as fh:
    fh.write("# Dungeon Rush - Decoded Data Tables\n\n")
    fh.write("Source: Unity ScriptableObject `.asset` files (standard YAML serialization).\n\n")
    fh.write("| Table (C# class) | Rows | Files |\n|---|---:|---|\n")
    for label, records in sorted(by_class.items(), key=lambda x: -len(x[1])):
        fh.write(f"| {label} | {len(records)} | tables/{label}.json, tables/{label}.csv |\n")

print("\nEnums parsed:", len(enums), " Classes parsed:", len(class_fields))
print("Output:", OUT)
