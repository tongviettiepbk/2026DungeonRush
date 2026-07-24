#!/usr/bin/env python3
# Recheck C2..C6: doi chieu asset SINH RA vs asset GOC (AssetRipper) tung field + icon.
import os, re, glob

ROOT = r"E:\00Work\00Project\2026DungOnRush"
SRC = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "MonoBehaviour")
RES = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets", "Resources", "Scriptable Objects", "Gears")

TYPES = [
    (1, "Helmets",   "7f715997e5b840bda8230056bea95e2c"),
    (2, "Gloves",    "e9efb72f1936240fdb227142af2f8fe8"),
    (3, "Rings",     "7af5f1a3859c3fc63c6e880c4bad3ca5"),
    (4, "Necklaces", "3e49dd65944305d182771504d45baafb"),
    (5, "Backpacks", "9ba934ab2e1a8b5285b90ba362f47049"),
]

def field(t, k):
    m = re.search(r"^\s*%s:\s*(.+)$" % re.escape(k), t, re.M)
    return m.group(1).strip() if m else None

total_mism = 0
total_icon_missing = 0  # goc CO icon nhung gen null (thieu png)
for slot, folder, sguid in TYPES:
    src_files = [f for f in glob.glob(os.path.join(SRC, "*.asset"))
                 if ("guid: " + sguid) in open(f, encoding="utf-8").read()]
    src = {}
    for f in src_files:
        t = open(f, encoding="utf-8").read()
        src[field(t, "m_Name")] = t
    gen = {}
    for f in glob.glob(os.path.join(RES, folder, "*.asset")):
        t = open(f, encoding="utf-8").read()
        gen[field(t, "m_Name")] = t

    only_s = set(src) - set(gen)
    only_g = set(gen) - set(src)
    mism = []
    icon_missing = []
    for n in sorted(set(src) & set(gen)):
        s, g = src[n], gen[n]
        checks = [
            ("itemId", field(s, "ItemId"), field(g, "itemId")),
            ("displayName", field(s, "ItemName"), field(g, "displayName")),
            ("rarity", field(s, "Rarity"), field(g, "rarity")),
            ("localizationKey", field(s, "LocalizationKey"), field(g, "localizationKey")),
            ("slot", str(slot), field(g, "slot")),
        ]
        for fld, a, b in checks:
            if (a or "") != (b or ""):
                mism.append((n, fld, a, b))
        # icon: goc co Icon guid nhung gen null?
        src_has = "guid" in (field(s, "Icon") or "")
        gen_has = "guid" in (field(g, "icon") or "")
        if src_has and not gen_has:
            icon_missing.append(n)

    print("== %s ==  src=%d gen=%d  mismatch=%d  icon_thieu_png=%d" %
          (folder, len(src), len(gen), len(mism), len(icon_missing)))
    if only_s: print("   !! chi co GOC:", sorted(only_s))
    if only_g: print("   !! chi co GEN:", sorted(only_g))
    for n, fld, a, b in mism[:20]:
        print("   MISMATCH %-18s %-16s GOC=%s GEN=%s" % (n, fld, a, b))
    if icon_missing:
        print("   ICON thieu png (goc co, gen null):", sorted(icon_missing))
    total_mism += len(mism)
    total_icon_missing += len(icon_missing)

print("\nTONG mismatch =", total_mism, "| tong icon thieu png =", total_icon_missing)
