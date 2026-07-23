#!/usr/bin/env python3
# Parse LIVE Firebase Remote Config (pulled from emulator app cache) into tables
import json, os, csv

BASE = r"E:\00Work\00Project\2026DungOnRush\DecodedData"
SRC  = os.path.join(BASE, "appcache", "drdump", "frc_activate.json")
TBL  = os.path.join(BASE, "tables")

cfg = json.load(open(SRC, encoding="utf-8"))["configs_key"]

# 1. raw scalar config -> json + csv
scalars = {k: v for k, v in cfg.items()}
json.dump(scalars, open(os.path.join(BASE, "remote_config_live.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=2)

# 2. experience_required_per_level -> table
xp = [int(x) for x in cfg["experience_required_per_level"].split(",") if x != ""]
with open(os.path.join(TBL, "experience_required_per_level.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["level", "xp_required"])
    for i, v in enumerate(xp, 1): w.writerow([i, v])

# 3. army_power_segments -> table (threshold,value pairs)
seg = cfg["army_power_segments"].split("|")
with open(os.path.join(TBL, "army_power_segments.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["max_power", "segment_size"])
    for s in seg:
        a, b = s.split(","); w.writerow([a, b])

# 4. weapon_offer_config -> table (armyLevel, weaponItemId)
wo = cfg["weapon_offer_config"].split("|")
with open(os.path.join(TBL, "weapon_offer_config.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["army_level", "weapon_item_id"])
    for s in wo:
        a, b = s.split(","); w.writerow([a, b])

# 5. forge_rarity_probabilities (live) -> confirm/emit
rar = ["Common","Uncommon","Rare","Epic","Legendary","Mythic","Divine","Celestial","Immortal","Eternal"]
rows = cfg["forge_rarity_probabilities"].split("|")
with open(os.path.join(TBL, "forge_rarity_probabilities_live.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f); w.writerow(["row"] + [f"P_{r}" for r in rar])
    for i, r in enumerate(rows): w.writerow([i] + r.split(","))

print("Remote Config version:", cfg.get("config_version"))
print("XP levels:", len(xp), "| army segments:", len(seg), "| weapon offers:", len(wo), "| forge rows:", len(rows))
print("Scalar keys:", ", ".join(k for k in scalars if "," not in str(scalars[k]) and "|" not in str(scalars[k])))
