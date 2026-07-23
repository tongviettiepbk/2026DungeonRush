#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM D "Companion" vao Google Sheet DungeonRush - GDD (1 tinh nang = 1 tab).
# Anh: DecodedData/image_urls_d.json (upload boi tools/upload_images_d.py).
# Chay: python tools/write_group_d.py   (print ASCII vi console cp1252)
import json, os, re
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")
IMG_JSON = os.path.join(ROOT, "DecodedData", "image_urls_d.json")
SRC_NOTE = ("Nguồn: AssetRipper → Assets/MonoBehaviour/*.asset (Unity ScriptableObject YAML) "
            "· Firebase Remote Config v17.0.0 (lấy từ app-cache)")

IMGS = json.load(open(IMG_JSON, encoding="utf-8")) if os.path.exists(IMG_JSON) else {}


def load(name):
    return json.load(open(os.path.join(TABLES, name + ".json"), encoding="utf-8"))


def img(key, h=56):
    m = IMGS.get(key)
    if not m:
        return ""
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def rar(r):
    return "%d — %s" % (r["Rarity"], r["Rarity__enum"])


# Thong so ky nang rieng theo tung pet (chi liet ke field co nghia voi type do)
SPECIAL = {
    "Companion_1_Common_DPS_Single":
        "Đạn lửa đơn mục tiêu, tốc độ đạn 10",
    "Companion_1_Common_Heal_Single":
        "Đạn hồi máu đơn mục tiêu, tốc độ đạn 8",
    "Companion_1_Common_Lightning_Single":
        "Tia sét kéo dài 1s, tick 0.2s (damage tính mỗi giây)",
    "Companion_2_Uncommon_Slow_Single":
        "Cầu băng: làm chậm 50% trong 2s, tốc độ đạn 12",
    "Companion_2_Uncommon_Heal_Chain":
        "Tia hồi máu liên tục 3s, tick 0.1s, delay xuất trận 1s",
    "Companion_2_Uncommon_Bomber_AOE_Bomb":
        "Bom vòng cung: bán kính nổ 2 ô, bay 1.25s, vòm cao 2.5",
    "Companion_3_Rare_Slow_Multi":
        "Cầu băng tách 2 nhánh (AoE), làm chậm 50% trong 2s",
    "Companion_3_Rare_Lightning_Multi":
        "Sét lan sang 2 mục tiêu phụ, kéo dài 2s, tick 0.2s",
    "Companion_3_Rare_Bomber_AOE_Burn":
        "Bom bán kính 2 ô + thiêu đốt 600 dmg/tick trong 2s",
    "Companion_4_Epic_Guardian_Single":
        "Khiên hộ vệ 5s: hồi 18 000 HP + tăng 5% Block Chance",
    "Companion_4_Epic_Slow_AOE":
        "Vùng băng bán kính 2 ô tồn tại 4s, làm chậm 50% trong 2s",
    "Companion_4_Epic_Beam_Single":
        "Tia năng lượng 1.5s, tick 0.25s (damage tính mỗi giây)",
    "Companion_5_Legendary_Meteor":
        "Gọi 6 thiên thạch, mỗi 0.7s một quả, bán kính nổ 2 ô",
    "Companion_5_Legendary_HealDamage":
        "Đạn siphon bay lượn sóng (sine 0.75/5Hz): gây damage VÀ hồi máu",
    "Companion_5_Legendary_Blaster":
        "Súng quạt: 4 loạt trong 1.75s, góc quạt 55°, tầm 6 ô",
    "Companion_6_Divine_Clone":
        "Triệu hồi phân thân người chơi 30% HP (+0.5%/level), sống 6s",
    "Companion_6_Divine_HealNova":
        "Nova lan từ bán kính 0.5 → 5 ô trong 1.5s, tick 0.1s: hồi máu + gây damage",
    "Companion_6_Divine_Immortal":
        "BẤT TỬ 2s (+0.02s/level), delay xuất trận 1s",
}

HEAL_TYPES = {"Healer", "ChainHealer", "Guardian", "Siphon", "HealNova"}


def tab_companion():
    rows = load("CompanionData")
    rows.sort(key=lambda r: (int(re.match(r"Companion_(\d+)_", r["_name"]).group(1)), r["_name"]))

    g = [["D1. COMPANION (Pet đồng hành) — CompanionData (18 bản ghi)"],
         [SRC_NOTE],
         ["Pet đi theo người chơi trong dungeon (tốc độ 2, giữ khoảng cách 2–3 ô), tự dùng kỹ năng theo "
          "cooldown và có thân thể chiến đấu riêng (tự đánh + có HP). 18 pet = 6 tier × 3 con, "
          "mỗi tier đúng 1 rarity. Mọi chỉ số lên theo level: mỗi level +5% giá trị gốc (Scaler = Base/20)."],
         ["Ảnh", "Asset", "Tên", "Loại kỹ năng", "Rarity", "Cooldown (s)",
          "Damage gốc", "Dmg scaler/level", "Heal gốc", "Heal scaler/level",
          "Pet Atk gốc", "Atk scaler/level", "Pet HP gốc", "HP scaler/level",
          "Thông số kỹ năng riêng", "Mô tả trong game (EN)"]]
    for r in rows:
        healer = r["Type__enum"] in HEAL_TYPES
        # Heal 10/5 la gia tri default khong dung o pet khong hoi mau
        heal_b = r["HealBase"] if healer else "—"
        heal_s = r["HealScaler"] if healer else "—"
        g.append([img(r["_name"]), r["_name"], r["CompanionName"], r["Type__enum"], rar(r),
                  r["Cooldown"], r["DamageBase"], r["DamageScaler"], heal_b, heal_s,
                  r["OwnAttackBase"], r["OwnAttackScaler"], r["OwnHealthBase"], r["OwnHealthScaler"],
                  SPECIAL.get(r["_name"], ""), r.get("Desc_en", r.get("Details", ""))])
    g += [[],
          ["Nhận xét"],
          ["• 18 pet = 6 tier × 3 con, mỗi tier 1 rarity: Common → Uncommon → Rare → Epic → Legendary → Mythic. "
           "Companion chỉ có 6 bậc rarity như Áo choàng (C7), KHÔNG lên tới Divine như trang bị thường."],
          ["• LỆCH TÊN: asset tier 6 đặt tên 'Companion_6_Divine_*' nhưng enum Rarity = Mythic (5) — "
           "tên file dùng bộ rarity cũ (giống cột forge C9), enum client là chuẩn."],
          ["• 15 loại kỹ năng khác nhau trên 18 pet (chỉ Bomber và Lightning có 2 biến thể). Tier càng cao "
           "kỹ năng càng 'độc': tier 5–6 là blaster quạt, siphon hút máu, mưa thiên thạch, phân thân, nova, BẤT TỬ."],
          ["• Công thức tăng trưởng đồng nhất TOÀN BẢNG: mọi Scaler = Base/20, tức mỗi level +5% giá trị gốc "
           "(damage, heal, atk và HP của pet đều vậy) — chỉ Immortality dùng scaler riêng +0.02s/level."],
          ["• Damage gốc nhảy vọt theo tier: 90 (Common) → 900 → 1 800 → 9 000 → 270 000 (Legendary/Mythic) — "
           "thang lũy thừa tương tự Army Power (tab A5), pet cũ lỗi thời rất nhanh."],
          ["• Bộ 3 mỗi tier thường đủ vai: 1 damage + 1 hồi máu/hộ vệ + 1 khống chế; riêng tier 5 và 6 "
           "thiên hẳn về damage/utility (Siphon và HealNova vừa hồi máu vừa gây damage)."],
          ["• Celestial Mind (Mythic): cho người chơi BẤT TỬ 2s mỗi 12s cooldown (uptime ~17%) — "
           "cooldown dài nhất bảng; đổi lại là hiệu ứng mạnh nhất game."],
          ["• Trang bị có substat CompanionCooldown / CompanionDamage (xem C8 Cánh) → có hướng build quanh pet."],
          ["• Quirk data: Storm Eye có Level = 2 trong config (mọi pet khác Level = 1)."],
          ["• Config KHÔNG có giá triệu hồi/nâng cấp pet — hệ gacha pet và tiến trình level do server điều khiển."]]
    return "D1. Companion", g, 4


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
    tabs = [tab_companion()]
    # index 19 = sau Mind-map(0) + A1-A6(1..6) + B1-B3(7..9) + C1-C9(10..18)
    write_tabs(tabs, 19)
    print("images:", ("ON %d keys" % len(IMGS)) if IMGS else "OFF")


if __name__ == "__main__":
    main()
