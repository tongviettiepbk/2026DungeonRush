#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM E "Dao mo (Mining)" vao Google Sheet DungeonRush - GDD (1 tinh nang = 1 tab).
# Anh: DecodedData/image_urls_e.json (upload boi tools/upload_images_e.py).
# Chay: python tools/write_group_e.py   (print ASCII vi console cp1252)
import json, os
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")
IMG_JSON = os.path.join(ROOT, "DecodedData", "image_urls_e.json")
SRC_NOTE = ("Nguồn: AssetRipper → Assets/MonoBehaviour/MiningConfig.asset (Unity ScriptableObject YAML) "
            "· enum MineOreType.cs · localization strings_en")

IMGS = json.load(open(IMG_JSON, encoding="utf-8")) if os.path.exists(IMG_JSON) else {}

# OreType enum -> (ten EN, ten VN), thu tu cot theo do hiem
ORES = [
    (6, "Dirt", "Đất"),
    (7, "Stone", "Đá"),
    (0, "Coal", "Than"),
    (4, "Iron", "Sắt"),
    (5, "Ruby", "Ruby"),
    (2, "Emerald", "Ngọc lục bảo"),
    (3, "Gold", "Vàng"),
    (1, "Diamond", "Kim cương"),
]


def img(key, h=48):
    m = IMGS.get(key)
    if not m:
        return ""
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def tab_mining():
    cfg = json.load(open(os.path.join(TABLES, "MiningConfig.json"), encoding="utf-8"))[0]
    levels = cfg["RarityLevels"]
    wt = {lv["MinMineLevel"]: {o["OreType"]: o["Weight"] for o in lv["OreWeights"]} for lv in levels}
    lv_min, lv_max = min(wt), max(wt)
    unlock = {}
    for t, en, vn in ORES:
        unlock[t] = next((lv for lv in sorted(wt) if wt[lv].get(t, 0) > 0), None)
    ore_cfg = {o["OreType"]: o for o in cfg["OreConfigs"]}

    g = [["E1. ĐÀO MỎ (MINING) — MiningConfig (minigame đào sâu, 8 loại khối/quặng)"],
         [SRC_NOTE],
         ["Minigame đào mỏ: lưới rộng 9 cột, hiển thị 9 hàng, nhân vật đứng ở hàng 8 và đào xuống vô hạn. "
          "Mỗi nhát cuốc phá 1 khối (mọi khối đều 1 nhát); phải đào khối phía trên trước (lỗi 'Too deep!') "
          "và cần đường đi tới khối (có pathfinding, lỗi 'No path available'). Khối sinh ngẫu nhiên theo "
          "trọng số phụ thuộc level mỏ; ~40% ô là khoảng rỗng (EmptinessThreshold 0.4). Quặng đào được "
          "cộng vào kho (8 bộ đếm trong MiningData lưu client) — việc quy đổi quặng ra reward do server."],
         [],
         ["1. THÔNG SỐ CHUNG"],
         ["Tham số", "Giá trị", "Ghi chú"],
         ["GridWidth × VisibleRows", "9 × 9", "Lưới 9 cột, camera thấy 9 hàng (cuộn tốc độ 3)"],
         ["BaselineRow", 8, "Hàng nhân vật đứng"],
         ["CellSize / BlockScale", "1 / 1.09", "Khối vẽ to hơn ô 9% (che khe hở)"],
         ["EmptinessThreshold", 0.4, "Tỉ lệ ô rỗng khi sinh hàng mới (suy đoán từ tên + Range 0–1)"],
         ["HitsRequired (mọi khối)", 1, "Không khối nào cần >1 nhát — độ khó không tăng theo độ sâu"],
         ["CharacterMoveSpeed / PowerUpMoveSpeed", "4 / 8", "Tốc độ di chuyển thường / khi ăn power-up"],
         [],
         ["2. CÔNG CỤ ĐÀO"],
         ["Ảnh", "Công cụ", "Tác dụng", "Giá gem", "Nhận qua ad", "Ad tối đa/ngày", "Ghi chú"],
         [img("tool_Pickaxe"), "Cuốc (Pickaxe)", "Phá 1 khối", "—",
          "%d cuốc/lượt" % cfg["PickaxeAdRewardCount"], cfg["DailyPickaxeAdLimit"],
          "Hồi 1 cuốc mỗi %d phút, kho tối đa %d (bắt đầu %d) → đầy lại sau 2h30. "
          "Mastery 'Max Pickaxes' (nhóm G) tăng kho." % (
              cfg["PickaxeRegenIntervalMinutes"], cfg["MaxPickaxeCount"], cfg["StartingPickaxeCount"])],
         [img("tool_Drill"), "Khoan (Drill)", "Khoan xuyên %d khối theo cột (DrillDepth)" % cfg["DrillDepth"],
          cfg["DrillGemCost"], "%d/lượt" % cfg["DrillAdRewardCount"], cfg["DailyDrillAdLimit"],
          "≈ %.2f gem/khối" % (cfg["DrillGemCost"] / cfg["DrillDepth"])],
         [img("tool_GoldenPickaxe"), "Cuốc vàng (Golden Pickaxe)",
          "Phá %d hàng liên tiếp (delay %.2fs/hàng)" % (cfg["GoldenPickaxeRows"], cfg["GoldenPickaxeRowDelay"]),
          cfg["GoldenPickaxeGemCost"], "%d/lượt" % cfg["GoldenPickaxeAdRewardCount"],
          cfg["DailyGoldenPickaxeAdLimit"],
          "%d hàng × 9 cột ≈ 45 khối → ≈ %.1f gem/khối (rẻ nhất)" % (
              cfg["GoldenPickaxeRows"], cfg["GoldenPickaxeGemCost"] / (cfg["GoldenPickaxeRows"] * 9))],
         [],
         ["3. ĐỘ SÂU THEO LEVEL MỎ (DepthTiers)"],
         ["Level mỏ", "Depth / level", "Ghi chú"]]
    prev = 1
    for t in cfg["DepthTiers"]:
        hi = t["MaxMineLevel"]
        label = ("%d – %d" % (prev, hi)) if hi else ("%d+" % prev)
        g.append([label, t["DepthPerLevel"],
                  "" if hi else "MaxMineLevel 0 = không giới hạn"])
        prev = hi + 1 if hi else prev
    g += [["(Diễn giải: mỗi level mỏ cần đào thêm chừng đó độ sâu — càng lên cao càng lâu; "
           "UI hiện 'Depth: {0}' và 'Next in: {0}'. Tổng tới level 60 ≈ 1 000 + 1 000 + 1 500 + 4 000 = 7 500.)"],
          [],
          ["4. TÁM LOẠI KHỐI / QUẶNG (OreConfigs + OreIcons + MineOreType enum)"],
          ["Khối trong mỏ", "Icon tài nguyên", "Tên", "OreType", "Thu thập được?",
           "Mở từ level mỏ", "Tỉ lệ ở lv%d" % lv_min, "Tỉ lệ ở lv%d" % lv_max]]
    for t, en, vn in ORES:
        collect = "Có" if ore_cfg[t]["IsCollectible"] else "Không (đất nền)"
        g.append([img("block_" + en), img("icon_" + en) if IMGS.get("icon_" + en) else "—",
                  "%s (%s)" % (vn, en), "%d — %sMine" % (t, en) if t <= 5 else "%d — %s" % (t, en),
                  collect, unlock[t] if unlock[t] is not None else "—",
                  "%.2f%%" % wt[lv_min].get(t, 0), "%.2f%%" % wt[lv_max].get(t, 0)])
    g += [[],
          ["5. TỈ LỆ SINH KHỐI THEO LEVEL MỎ (RarityLevels — %d mốc, mỗi dòng tổng 100%%)" % len(levels)],
          ["Level mỏ"] + ["%s (%s)" % (vn, en) for _, en, vn in ORES]]
    for lv in sorted(wt):
        g.append([lv] + [wt[lv].get(t, 0) for t, _, _ in ORES])
    g += [[],
          ["6. GÓI OFFER MINING (IAP — MiningOfferTiers)"],
          ["StoreId", "Tên (localization)", "Drill", "Cuốc vàng", "Cuốc", "Gem", "Tăng kho cuốc tối đa"]]
    for o in cfg["MiningOfferTiers"]:
        name = {"UI.Mining.OfferTitle": "Mining Offer",
                "UI.Mining.OfferTitleLarge": "Large Mining Offer"}.get(o["TitleKey"], o["TitleKey"])
        g.append([o["StoreId"], name, o["Drill"], o["GoldenPickaxe"], o["Pickaxe"], o["Gem"],
                  o["MaxPickaxeIncrease"]])
    g += [[],
          ["Nhận xét"],
          ["• Nhịp mở nội dung: cứ ~10 level mỏ mở 1 quặng mới — Sắt lv2, Ruby lv12, Ngọc lục bảo lv22, "
           "Vàng lv32, Kim cương lv43; bảng tỉ lệ dừng ở lv60 (từ đó trở đi giữ nguyên)."],
          ["• Đất + Đá chiếm ~99% ở lv1, giảm dần còn ~51.6% ở lv60; Than tăng 0.99% → 12.9%. "
           "Từ lv21 trở đi tỉ lệ Đá = Than (game coi Đá là 'quặng' rẻ nhất, cũng thu thập được — chỉ Đất là bỏ đi)."],
          ["• Tỉ lệ hội tụ đẹp ở lv60: Ruby = Ngọc lục bảo = Vàng = Kim cương = 6.45%, Sắt 9.68% — "
           "quặng mới không thay thế quặng cũ mà san bằng dần."],
          ["• Kinh tế cuốc: miễn phí 12 cuốc/giờ (regen 5 phút), kho 30; ads cho tối đa 60 cuốc + 6 drill "
           "+ 6 cuốc vàng mỗi ngày (quy ra gem: 6×50 + 6×100 = 900 gem/ngày nếu chăm xem ads)."],
          ["• Cuốc vàng hiệu quả nhất (~2.2 gem/khối) vs drill 6.25 gem/khối — nhưng drill đào SÂU (xuyên cột) "
           "giúp tăng depth/level mỏ nhanh, cuốc vàng đào RỘNG (vét quặng 5 hàng)."],
          ["• 2 gói IAP: gói lớn ≈ 6× giá trị gói nhỏ (100/100/330 công cụ + 3 200 gem vs 17/17/55 + 600); "
           "cả 2 tăng vĩnh viễn kho cuốc (+10 / +20) — buff vĩnh viễn gắn trong gói tiêu hao."],
          ["• Mining nối vào các hệ khác: Mastery 'Max Pickaxes' (nhóm G) tăng kho cuốc; nút vào Mining "
           "nằm ở màn Wings (UI.Wings.MiningButton); mining cũng là 1 DealType trong hệ offer chung."],
          ["• Config KHÔNG có: giá trị quy đổi 8 loại quặng (server), độ bền/nâng cấp công cụ (không tồn tại), "
           "phần thưởng theo level mỏ (server). Client chỉ đếm số quặng đào được."],
          ["• Ghi chú kỹ thuật: mọi khối HitsRequired = 1 và có DamagedBlocks trong save — khung 'khối nhiều máu' "
           "có sẵn nhưng chưa dùng; cuốc/drill/cuốc vàng dùng chung 1 Animator."]]
    return "E1. Đào mỏ (Mining)", g, 2


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
            {"autoResizeDimensions": {"dimensions": {
                "sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": ncol}}},
        ]
        # to dam dong tieu de section ("N. ..." / "Nhận xét") + dong header bang ngay sau no
        section_rows = [i for i, row in enumerate(grid)
                        if len(row) == 1 and isinstance(row[0], str) and (
                            (row[0][:1].isdigit() and row[0][1:2] == ".") or row[0] == "Nhận xét")]
        for i in section_rows:
            reqs.append({"repeatCell": {
                "range": {"sheetId": sid, "startRowIndex": i, "endRowIndex": i + 1},
                "cell": {"userEnteredFormat": {
                    "backgroundColor": {"red": 0.93, "green": 0.94, "blue": 0.98},
                    "textFormat": {"bold": True, "fontSize": 11}}},
                "fields": "userEnteredFormat(textFormat,backgroundColor)"}})
            if i + 1 < len(grid) and len(grid[i + 1]) > 1:
                reqs.append({"repeatCell": {
                    "range": {"sheetId": sid, "startRowIndex": i + 1, "endRowIndex": i + 2},
                    "cell": {"userEnteredFormat": {
                        "backgroundColor": {"red": 0.85, "green": 0.88, "blue": 0.93},
                        "textFormat": {"bold": True},
                        "wrapStrategy": "WRAP", "verticalAlignment": "MIDDLE"}},
                    "fields": "userEnteredFormat(textFormat,backgroundColor,wrapStrategy,verticalAlignment)"}})
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
    tabs = [tab_mining()]
    # index 20 = sau Mind-map(0) + A1-A6(1..6) + B1-B3(7..9) + C1-C9(10..18) + D1(19)
    write_tabs(tabs, 20)
    print("images:", ("ON %d keys" % len(IMGS)) if IMGS else "OFF")


if __name__ == "__main__":
    main()
