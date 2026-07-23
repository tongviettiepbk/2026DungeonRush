#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM C "Trang bi (Equipment)" vao Google Sheet DungeonRush - GDD.
# Moi tinh nang = 1 tab: C1 Vu khi ... C9 Forge.
# Anh: DecodedData/image_urls_bc.json (upload boi tools/upload_images_bc.py).
# Chay: python tools/write_group_c.py   (print ASCII vi console cp1252)
import json, os, csv, re
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")
IMG_JSON = os.path.join(ROOT, "DecodedData", "image_urls_bc.json")
SRC_NOTE = ("Nguồn: AssetRipper → Assets/MonoBehaviour/*.asset (Unity ScriptableObject YAML) "
            "· Firebase Remote Config v17.0.0 (lấy từ app-cache)")

IMGS = json.load(open(IMG_JSON, encoding="utf-8")) if os.path.exists(IMG_JSON) else {}

RARITY_ORDER = ["Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic",
                "Artifact", "Ancient", "Immortal", "Divine"]
SUBSTAT = {0: "AttackSpeed", 1: "BlockChance", 2: "CriticalChance", 3: "CriticalDamage",
           4: "Damage", 5: "DoubleChance", 6: "Health", 7: "HealthRegen", 8: "Lifesteal",
           9: "MeleeDamage", 10: "RangedDamage", 11: "CompanionCooldown", 12: "CompanionDamage"}


def load(name):
    return json.load(open(os.path.join(TABLES, name + ".json"), encoding="utf-8"))


def img(key, fallback="", h=56):
    m = IMGS.get(key)
    if not m:
        return fallback
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def rar(r):
    return "%d — %s" % (r["Rarity"], r["Rarity__enum"])


def sort_items(rows, player_pat):
    """Player items truoc (theo tier roi index), enemy items sau (theo ten)."""
    def key(r):
        m = re.match(player_pat, r["_name"])
        if m:
            return (0, int(m.group(1)), int(m.group(2)), r["_name"])
        return (1, 0, 0, r["_name"])
    return sorted(rows, key=key)


def is_enemy(name, player_pat):
    return not re.match(player_pat, name)


# ------------------------------------------------------------- C1. Vu khi
def tab_weapon():
    rows = load("WeaponData")
    pat = r"Weapon_[mr]_(\d+)_(\d+)$"

    def key(r):
        m = re.match(r"Weapon_([mr])_(\d+)_(\d+)$", r["_name"])
        if m:
            return (0 if m.group(1) == "m" else 1, int(m.group(2)), int(m.group(3)), r["_name"])
        return (2, 0, 0, r["_name"])

    g = [["C1. VŨ KHÍ — WeaponData (72 bản ghi: 59 vũ khí người chơi + 13 vũ khí quái/boss)"],
         [SRC_NOTE],
         ["Vũ khí quyết định kiểu tấn công (cận chiến / bắn xa). Config KHÔNG chứa sát thương — "
          "damage sinh runtime theo rarity + level (server-driven); client chỉ giữ tầm đánh, đạn và hiển thị."],
         ["Ảnh", "Asset", "ItemId", "Tên item", "Rarity", "Loại", "Tốc đánh", "Tầm đánh",
          "Tốc độ đạn", "Có projectile", "Localization key", "Ghi chú"]]
    for r in sorted(rows, key=key):
        enemy = not re.match(pat, r["_name"])
        note = "Vũ khí quái/boss (không rơi cho người chơi)" if enemy else ""
        g.append([img(r["_name"]), r["_name"], r["ItemId"], r["ItemName"], rar(r),
                  r["WeaponType__enum"], r["AttackSpeed"], r["AttackDistance"],
                  r["ProjectileSpeed"] or "—",
                  "Có" if (r.get("ProjectilePrefab") or {}).get("fileID") else "—",
                  r.get("LocalizationKey", ""), note])
    g += [[],
          ["Nhận xét"],
          ["• 59 vũ khí người chơi = 30 melee (3 × 10 rarity) + 29 ranged — THIẾU Weapon_r_2_3: tier Uncommon chỉ có 2 vũ khí bắn xa."],
          ["• Mọi vũ khí đều AttackSpeed = 1; tầm đánh chỉ có 2 giá trị: melee 1.5 ô, ranged 3.5 ô → cân bằng nằm hoàn toàn ở damage sinh theo rarity, không ở stat vũ khí."],
          ["• Tốc độ đạn tăng theo rarity (7 → 20): vũ khí xịn bắn đạn nhanh hơn — buff cảm giác chứ không đổi DPS danh nghĩa."],
          ["• Vũ khí boss (Lich/Dragon/Hag) có AttackDistance = 100 → đánh trúng toàn màn; Ogre = 3 (cận chiến tay dài); Cultist dùng đúng 2 giá trị chuẩn của người chơi."],
          ["• 10 bậc rarity client: Common → Uncommon → Rare → Epic → Legendary → Mythic → Artifact → Ancient → Immortal → Divine."]]
    return "C1. Vũ khí", g, 4


# ---------------------------------------------------- C2/C3/C5/C4/C6 catalog
def catalog_tab(code, title_vn, table, player_prefix, intro, notes, extra_note_fn=None):
    rows = load(table)
    pat = player_prefix + r"_(\d+)_(\d+)$"
    g = [["%s — %s (%d bản ghi)" % (code, title_vn, len(rows))],
         [SRC_NOTE],
         [intro],
         ["Ảnh", "Asset", "ItemId", "Tên item", "Rarity", "Localization key", "Ghi chú"]]
    for r in sort_items(rows, pat):
        enemy = is_enemy(r["_name"], pat)
        note = "Đồ quái/boss" if enemy else ""
        if extra_note_fn:
            note = extra_note_fn(r) or note
        g.append([img(r["_name"]), r["_name"], r["ItemId"], r["ItemName"], rar(r),
                  r.get("LocalizationKey", ""), note])
    g += [[], ["Nhận xét"]] + [["• " + n] for n in notes]
    return code + ". " + title_vn, g, 4


def tab_helmet():
    return catalog_tab(
        "C2", "Mũ", "HelmetData", "Helmet",
        "Mũ (helmet) của người chơi và quái. Config chỉ là catalog hiển thị (icon + sprite đội trên đầu); "
        "chỉ số HP/Damage của mũ sinh runtime theo rarity + level.",
        ["40 mũ người chơi = 4 mẫu × 10 rarity; 14 mũ quái/boss (Cultist ×2, Dragon ×4, Lich ×3, Ogre ×2, Hag ×2, Zombie).",
         "Không có bất kỳ stat nào trong config — khác CapeData/WingData (có base stat). Mũ thuần túy là 'vỏ' hiển thị + rarity.",
         "Đồ quái đều gắn Rarity Common → rarity chỉ có ý nghĩa với đồ người chơi."])


def tab_gloves():
    def extra(r):
        if r["_name"].startswith("CultistGloveData"):
            return "Dùng chung ItemId 999 'Zombie Gloves'"
        return None
    return catalog_tab(
        "C3", "Găng tay", "GlovesData", "Gloves",
        "Găng tay (gloves) của người chơi và quái — catalog hiển thị như Mũ, stat sinh runtime.",
        ["40 găng người chơi = 4 mẫu × 10 rarity; 14 găng quái/boss.",
         "LỖI DATA NHỎ: CultistGloveData (cả melee lẫn ranged) trỏ ItemId 999 'Zombie Gloves' — copy-paste từ bộ zombie.",
         "52 ItemId duy nhất / 54 bản ghi vì 2 bản ghi Cultist dùng chung id 999."],
        extra)


def tab_ring():
    return catalog_tab(
        "C4", "Nhẫn", "RingData", "Ring",
        "Nhẫn — 3 mẫu × 10 rarity. Config chỉ có ItemId/tên/rarity/icon; toàn bộ stat của nhẫn sinh runtime.",
        ["Đúng 30 bản ghi, không có biến thể quái — nhẫn là slot thuần người chơi.",
         "Cùng cấu trúc với Dây chuyền (C5): hai slot 'trang sức' chỉ là catalog, sức mạnh do hệ thống roll stat quyết định."])


def tab_necklace():
    return catalog_tab(
        "C5", "Dây chuyền", "NecklaceData", "Neckle",
        "Dây chuyền — 3 mẫu × 10 rarity, catalog thuần như Nhẫn (asset đặt tên 'Neckle', thiếu chữ).",
        ["30 bản ghi, không biến thể quái.",
         "Tên asset gõ sai chính tả 'Neckle_' thay vì 'Necklace_' — giữ nguyên để tra cứu."])


def tab_backpack():
    return catalog_tab(
        "C6", "Ba lô", "BackpackData", "Backpack",
        "Ba lô (đồ đeo lưng) — 2 mẫu × 10 rarity. Catalog hiển thị, có sprite đeo trên lưng nhân vật riêng.",
        ["20 bản ghi, ít mẫu nhất trong các slot trang bị (2/rarity so với 3–4 của slot khác).",
         "Ở rarity cao, ba lô là nhạc cụ/khiên (Celestial Lyre, Radiant Shield) — đổi hẳn concept theo tier."])


# ------------------------------------------------------------ C7. Ao choang
def tab_cape():
    rows = sorted(load("CapeData"), key=lambda r: int(r["CapeId"]))
    cfg = load("CapeConfig")[0]
    g = [["C7. ÁO CHOÀNG (Cape/Cloak) — CapeData (12) + CapeConfig"],
         [SRC_NOTE],
         ["Slot trang bị có hệ nâng cấp riêng: áo choàng có base stat + scaler theo level, "
          "triệu hồi bằng CloakCurrency và lên cấp bằng XP (salvage áo thừa)."],
         ["Ảnh", "CapeId", "Tên", "Rarity", "HP gốc", "Damage gốc", "HP scaler/level",
          "Damage scaler/level", "Số substat"]]
    for r in rows:
        g.append([img(r["_name"]), r["CapeId"], r["CapeName"], rar(r),
                  r["HealthBase"], r["DamageBase"], r["HealthScaler"], r["DamageScaler"],
                  r["SubStatCount"]])
    g += [[],
          ["CapeConfig (tham số chung)"],
          ["MaxLevel", cfg["MaxLevel"], "Cấp tối đa của áo choàng"],
          ["SummonCost", cfg["SummonCost"], "Giá 1 lần triệu hồi (CloakCurrency)"],
          ["SubStatCount", cfg["SubStatCount"], "Số substat tối đa"],
          [],
          ["Bảng XP theo rarity — class CapeRarityXPConfig"],
          ["Rarity", "Salvage XP", "LevelUp BaseXP", "LevelUp Scaler", "LevelUp Expo",
           "LevelUp BaseXP2", "LevelUp Scaler2"]]
    for rc in cfg["RarityConfigs"]:
        g.append([RARITY_ORDER[rc["Rarity"]], rc["SalvageBaseXP"], rc["LevelUpBaseXP"],
                  rc["LevelUpScaler"], rc["LevelUpExpo"], rc["LevelUpBaseXP2"], rc["LevelUpScaler2"]])
    g += [[],
          ["Nhận xét"],
          ["• Áo choàng chỉ có 6 bậc rarity (Common → Mythic, 2 mẫu/bậc) — không lên tới Divine như các slot khác."],
          ["• Base stat tăng 5 → 30 (×6) từ Common tới Mythic; scaler 0.02/level giữ nguyên mọi bậc → khoảng cách rarity là hằng số nhân."],
          ["• SubStatCount: Common–Epic 1 substat, Legendary/Mythic 2 → đúng bằng CapeConfig.SubStatCount = 2 tối đa."],
          ["• XP salvage tăng ~×2.35 mỗi bậc (4 → 9 → 22 → 52 → 122 → 287): áo rarity cao là 'thức ăn' XP giá trị hơn hẳn."],
          ["• Công thức XP lên cấp 2 pha (BaseXP/Scaler/Expo và BaseXP2/Scaler2) — hàm tính cụ thể bị strip (HybridCLR), tham số ở trên là dữ liệu thô."]]
    return "C7. Áo choàng", g, 4


# ----------------------------------------------------------------- C8. Canh
def tab_wing():
    rows = sorted(load("WingData"), key=lambda r: int(r["WingId"]))
    g = [["C8. CÁNH (Wing) — WingData (10 bản ghi)"],
         [SRC_NOTE],
         ["Slot trang bị chế tạo bằng quặng từ Đào mỏ (nhóm E): craft, reroll substat và lên cấp đều tốn quặng. "
          "Mỗi rarity đúng 1 mẫu cánh."],
         ["Ảnh", "WingId", "Tên", "Rarity", "HP gốc", "Dmg gốc", "HP scaler/level", "Dmg scaler/level",
          "Tier scaler (HP & Dmg)", "SubStats cố định", "Quặng craft", "Giá craft",
          "Quặng reroll", "Giá reroll", "Quặng lên cấp", "Giá lên cấp", "Cấp tối đa"]]
    for r in rows:
        subs = "; ".join("%s +%g" % (SUBSTAT.get(s["Type"], s["Type"]), s["Value"])
                         for s in r.get("SubStats") or [])
        rr = r.get("RerollCosts")
        rr_s = ", ".join(str(v) for v in rr) if isinstance(rr, list) else "(blob lỗi decode: %s)" % rr
        g.append([img(r["_name"], h=64), r["WingId"], r["WingName"], rar(r),
                  r["HealthBase"], r["DamageBase"], r["HealthScaler"], r["DamageScaler"],
                  r["HealthTierScaler"], subs, r["CraftOreType__enum"], r["CraftOreCost"],
                  r["RerollOreType__enum"], rr_s, r["LevelUpOreType__enum"], r["LevelUpCost"],
                  r["MaxLevel"]])
    g += [[],
          ["Nhận xét"],
          ["• Cả 10 cánh có CÙNG base stat (HP 30 / Dmg 9, scaler 0.015/level): sức mạnh thật đến từ TierScaler 3.1622777 = 10^0.5 — mỗi bậc rarity nhân ×3.16, hai bậc = ×10, đúng thang log của Army Power (tab A5)."],
          ["• Số substat tăng theo rarity: 1 (Common) → 5 (Divine); substat là bộ CỐ ĐỊNH theo mẫu cánh, không roll ngẫu nhiên loại — reroll chỉ roll lại giá trị."],
          ["• Chuỗi quặng craft leo theo mỏ: Stone → Coal → Iron → Ruby → Emerald → Gold → Diamond (giá 30/100/1000) — gắn tiến trình cánh vào độ sâu Đào mỏ (nhóm E)."],
          ["• Giá reroll tăng dần theo số lần: 3, 6, 9, 12 quặng. Riêng 4 cánh đầu trường RerollCosts giải mã lỗi (blob) — cần xem lại extractor nếu muốn số chính xác."],
          ["• Tên cánh rarity cao trùng theme boss/nhóm khác: Green Dragon (Artifact), Celestial (Immortal), Eternal (Divine)."]]
    return "C8. Cánh", g, 4


# ---------------------------------------------------------------- C9. Forge
def tab_forge():
    rows = list(csv.DictReader(open(os.path.join(TABLES, "forge_rarity_probabilities.csv"),
                                    encoding="utf-8")))
    cols = ["P_Common", "P_Uncommon", "P_Rare", "P_Epic", "P_Legendary", "P_Mythic",
            "P_Divine", "P_Celestial", "P_Immortal", "P_Eternal"]
    g = [["C9. FORGE (Lò rèn — tỉ lệ ra rarity) — remote key forge_rarity_probabilities (101 dòng)"],
         [SRC_NOTE + " · Giá trị APK và Remote Config live TRÙNG NHAU 100%"],
         ["Rèn/mở đồ ở cấp forge càng cao thì tỉ lệ ra rarity cao càng lớn. Mỗi dòng là phân phối xác suất "
          "(tổng luôn = 100%) cho một cấp; dòng 0–100 (suy luận: theo cấp forge/level người chơi)."],
         [img("forge", "auto-forge.png", 70)],
         ["Cấp (dòng)"] + [c[2:] for c in cols] + ["Tổng"]]
    for r in rows:
        vals = [float(r[c]) for c in cols]
        g.append([int(r["row"])] + [v if v else "" for v in vals] + [round(sum(vals), 4)])
    g += [[],
          ["Nhận xét"],
          ["• Đường cong mở rarity: Uncommon xuất hiện từ dòng 1, Rare từ dòng 2… các bậc cao mở dần theo cấp; dòng 100 là phân phối cuối."],
          ["• TÊN CỘT DÙNG BỘ RARITY CŨ: Divine/Celestial/Immortal/Eternal ở 4 bậc cuối, trong khi enum client là Artifact/Ancient/Immortal/Divine — remote config chưa đổi tên theo client (map theo VỊ TRÍ: P_Divine ↔ Artifact, P_Celestial ↔ Ancient, P_Immortal ↔ Immortal, P_Eternal ↔ Divine)."],
          ["• Giá trị trong APK trùng 100% với Remote Config live v17.0.0 → bảng forge hiện không bị chỉnh nóng."],
          ["• Mỗi dòng tổng đúng 100 (cột Tổng để kiểm chứng) — bảng do tool sinh, không chỉnh tay."]]
    return "C9. Forge", g, 5


def service():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    return build("sheets", "v4", credentials=creds, cache_discovery=False)


def write_tabs(tabs, index_start):
    svc = service().spreadsheets()
    meta = svc.get(spreadsheetId=TARGET).execute()
    existing = {s["properties"]["title"]: s["properties"]["sheetId"] for s in meta["sheets"]}

    adds = [{"addSheet": {"properties": {"title": t[0], "index": index_start + i}}}
            for i, t in enumerate(tabs) if t[0] not in existing]
    if adds:
        res = svc.batchUpdate(spreadsheetId=TARGET, body={"requests": adds}).execute()
        for r in res["replies"]:
            p = r["addSheet"]["properties"]
            existing[p["title"]] = p["sheetId"]
            print("added tab id", p["sheetId"])

    for title, grid, hdr in tabs:
        sid = existing[title]
        svc.values().clear(spreadsheetId=TARGET, range="'%s'" % title).execute()
        svc.values().update(spreadsheetId=TARGET, range="'%s'!A1" % title,
                            valueInputOption="USER_ENTERED", body={"values": grid}).execute()
        ncol = max(len(r) for r in grid)
        reqs = [
            {"updateSheetProperties": {"properties": {
                "sheetId": sid, "gridProperties": {"frozenRowCount": hdr}},
                "fields": "gridProperties.frozenRowCount"}},
            {"repeatCell": {"range": {"sheetId": sid, "startRowIndex": 0, "endRowIndex": 1},
                            "cell": {"userEnteredFormat": {
                                "backgroundColor": {"red": 0.11, "green": 0.13, "blue": 0.2},
                                "textFormat": {"bold": True, "fontSize": 12,
                                               "foregroundColor": {"red": 1, "green": 1, "blue": 1}}}},
                            "fields": "userEnteredFormat(textFormat,backgroundColor)"}},
            {"repeatCell": {"range": {"sheetId": sid, "startRowIndex": hdr - 1, "endRowIndex": hdr},
                            "cell": {"userEnteredFormat": {
                                "backgroundColor": {"red": 0.85, "green": 0.88, "blue": 0.93},
                                "textFormat": {"bold": True},
                                "wrapStrategy": "WRAP", "verticalAlignment": "MIDDLE"}},
                            "fields": "userEnteredFormat(textFormat,backgroundColor,wrapStrategy,verticalAlignment)"}},
            {"autoResizeDimensions": {"dimensions": {
                "sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": ncol}}},
        ]
        img_rows = [i for i, row in enumerate(grid)
                    if row and isinstance(row[0], str) and row[0].startswith("=IMAGE")]
        for i in img_rows:
            h = int(grid[i][0].split(",")[2]) + 14  # chieu cao anh trong cong thuc + le
            reqs.append({"updateDimensionProperties": {"range": {
                "sheetId": sid, "dimension": "ROWS", "startIndex": i, "endIndex": i + 1},
                "properties": {"pixelSize": h}, "fields": "pixelSize"}})
        svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()
        print("wrote tab", len(grid), "rows,", len(img_rows), "image rows")


def main():
    tabs = [tab_weapon(), tab_helmet(), tab_gloves(), tab_ring(), tab_necklace(),
            tab_backpack(), tab_cape(), tab_wing(), tab_forge()]
    # index 10 = sau Mind-map(0) + A1-A6(1..6) + B1-B3(7..9)
    write_tabs(tabs, 10)
    print("images:", ("ON %d keys" % len(IMGS)) if IMGS else "OFF")


if __name__ == "__main__":
    main()
