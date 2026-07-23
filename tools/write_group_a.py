#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM A "Chien dau trong dungeon" vao Google Sheet DungeonRush - GDD.
# Moi tinh nang = 1 tab: A1..A6.
# Anh minh hoa: doc DecodedData/image_urls_a.json (neu co) -> chen =IMAGE(url),
# neu chua co thi ghi ten file png trong AssetRipper export.
# Chay: python tools/write_group_a.py     (print phai ASCII vi console cp1252)
import json, os, csv
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")
IMG_JSON = os.path.join(ROOT, "DecodedData", "image_urls_a.json")
SRC_NOTE = ("Nguồn: AssetRipper → Assets/MonoBehaviour/*.asset (Unity ScriptableObject YAML) "
            "· Firebase Remote Config v17.0.0 (lấy từ app-cache)")

IMGS = json.load(open(IMG_JSON, encoding="utf-8")) if os.path.exists(IMG_JSON) else {}


def load(name):
    return json.load(open(os.path.join(TABLES, name + ".json"), encoding="utf-8"))


def hexcol(c):
    if not c:
        return ""
    return "#%02X%02X%02X" % tuple(min(255, round(c.get(k, 0) * 255)) for k in "rgb")


def img(key, fallback, h=90):
    """Ô ảnh: công thức IMAGE nếu đã upload, nếu chưa thì ghi tên file nguồn."""
    m = IMGS.get(key)
    if not m:
        return fallback
    w = round(m["w"] * h / m["h"])
    # IMAGE() chặn link lh3.googleusercontent (#REF!); endpoint thumbnail thì chấp nhận.
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def yn(v):
    return "Có" if v in (1, True, "1") else "—"


def has(ref):
    return "Có" if ref and ref.get("fileID") else "—"


# ---------------------------------------------------------------- A1. Map
def tab_map():
    rows = load("MapConfig")
    use = {
        "DefaultLevel": ("Màn chơi thường (dungeon chính)", "mainmap", "sactx-…AtlasMainMap.png"),
        "BossRush": ("Chế độ Boss Rush", "bossrush", "boss_rush_illustration.png"),
        "DragonBossDungeon": ("Dungeon boss Rồng", "dragon", "dragon_dungeon_illustration.png"),
        "ZombieHordeDungeon": ("Dungeon Zombie", "zombie", "zombie_dungeon_illustration.png"),
        "PvP": ("Đấu trường PvP", "pvp", "pvp_illustration.png"),
        "ChatBattle": ("Đấu tay đôi mở từ khung chat", "pvp", "pvp_illustration.png"),
        "CultistDungeon": ("Dungeon Cultist", "cultist", "cultist_dungeon_illustration.png"),
    }
    g = [["A1. MAP / MÔI TRƯỜNG — MapConfig (7 bản ghi)"],
         [SRC_NOTE],
         ["Định nghĩa lưới chơi, camera và vật cản cho từng loại màn. Mỗi EnvironmentType trỏ tới một prefab môi trường riêng."],
         ["Ảnh", "Map asset", "EnvironmentType", "ID", "Dùng cho", "Grid W", "Grid H", "Tổng ô",
          "Có cửa (HasDoor)", "Sinh vật cản", "Camera offset (x, y, z)", "Camera ortho offset",
          "Số obstacle prefab", "Obstacle prefab GUID", "File ảnh nguồn"]]
    for r in sorted(rows, key=lambda x: x["EnvironmentType"]):
        e = r["EnvironmentType__enum"]
        label, key, fname = use.get(e, ("", "", ""))
        o = r["CameraPositionOffset"]
        obs = r.get("ObstaclePrefabs") or []
        g.append([img(key, fname), r["_name"], e, r["EnvironmentType"], label,
                  r["GridWidth"], r["GridHeight"], r["GridWidth"] * r["GridHeight"],
                  yn(r["HasDoor"]), yn(r["SpawnObstacles"]),
                  "(%g, %g, %g)" % (o["x"], o["y"], o["z"]), r["CameraOrthoSizeOffset"],
                  len(obs), ", ".join(p.get("guid", "") for p in obs), fname])
    g += [[],
          ["Nhận xét"],
          ["• Cả 7 map đều dùng chung lưới 9 × 12 = 108 ô và đều có cửa (HasDoor = 1) → kích thước sàn đấu là hằng số toàn game."],
          ["• Chỉ 3 map sinh vật cản: MainMap, CultistMap, ZombieMap. Các chế độ đối kháng (PvP, ChatBattle, BossRush) và DragonMap để sàn trống."],
          ["• Khác biệt chính giữa các map nằm ở camera: DragonMap kéo xa nhất (offset y = −1.5, ortho +2.5) để lọt boss rồng cỡ lớn."],
          ["• MainMap / PvpMap / ChatPvpMap dùng chung một EnvironmentPrefab (guid 29bd51b2…) → cùng bộ art, chỉ đổi camera và vật cản."]]
    return "A1. Map & Môi trường", g, 4


# --------------------------------------------------------------- A2. Tier
def tab_tier():
    rows = load("TierData")
    g = [["A2. TIER / ĐỘ SÂU MÀN — TierData (8 bản ghi)"],
         [SRC_NOTE],
         ["Bậc độ khó của dungeon. Mỗi tier chỉ định nghĩa tên và bảng màu UI; chỉ số quái theo tier do server trả về."],
         ["Tier", "Tên (EN)", "Tên hiển thị", "Localization key", "Màu tên (hex)", "Màu nền (hex)",
          "Màu viền (hex)", "Số obstacle prefab"]]
    for r in rows:
        g.append([r["_name"], r.get("Name_en", ""), r.get("Name", ""), r.get("LocalizationKey", ""),
                  hexcol(r.get("NameColor")), hexcol(r.get("BackgroundColor")),
                  hexcol(r.get("LineColor")), len(r.get("ObstaclePrefabs") or [])])
    g += [[],
          ["Nhận xét"],
          ["• 8 tier: Normal → Hard → Extreme → Overkill → Impossible → Hell I → Hell II → Hell III."],
          ["• Tier 6/7/8 (Hell I–II–III) dùng y hệt một màu đỏ #FF2D47 → UI không phân biệt được 3 bậc địa ngục bằng màu, chỉ bằng chữ số La Mã."],
          ["• Tier 1 là tier duy nhất có nền tối (#1F2020); từ Tier 2 trở đi nền sáng (#E8F3F5) và chỉ đổi màu chữ."],
          ["• Cả 8 tier đều trỏ tới cùng một obstacle prefab (guid 79e77d1e…) → độ khó không đổi layout vật cản."]]
    return "A2. Tier (độ khó)", g, 4


# ------------------------------------------------------------ A3. Dungeon
def tab_dungeon():
    rows = load("DungeonData")
    meta = {
        "CultistDungeon": ("cultist", "cultist_dungeon_illustration.png",
                           "Nghi lễ giáo phái; quái có cả cận chiến lẫn bắn xa"),
        "DragonHorde": ("dragon", "dragon_dungeon_illustration.png",
                        "Boss rồng đứng yên, không di chuyển"),
        "ZombieInvasion": ("zombie", "zombie_dungeon_illustration.png",
                           "Bầy zombie số lượng lớn, thưởng Bone"),
    }
    g = [["A3. DUNGEON (định nghĩa) — DungeonData (3 bản ghi)"],
         [SRC_NOTE],
         ["Ba dungeon sự kiện, mỗi cái có loại chìa/thưởng riêng để mở cửa."],
         ["Ảnh", "Dungeon asset", "DungeonType", "ID", "Tên hiển thị", "Tên EN", "Localization key",
          "Loại thưởng (RewardType)", "ID reward", "Màu chìa (hex)", "Ghi chú", "File ảnh nguồn"]]
    for r in sorted(rows, key=lambda x: x["DungeonType"]):
        key, fname, note = meta.get(r["_name"], ("", "", ""))
        g.append([img(key, fname), r["_name"], r["DungeonType__enum"], r["DungeonType"],
                  r.get("Name", ""), r.get("Name_en", ""), r.get("LocalizationKey", ""),
                  r["RewardType__enum"], r["RewardType"], hexcol(r.get("KeyColor")), note, fname])
    g += [[],
          ["Nhận xét"],
          ["• CẢNH BÁO DỮ LIỆU: DragonHorde có RewardType = ZombieHordeDungeonKey (id 3), không phải chìa rồng. Nhiều khả năng là lỗi copy-paste trong config gốc."],
          ["• ZombieInvasion trả thưởng RewardType = Bone (id 0) chứ không phải một loại chìa riêng."],
          ["• Chỉ CultistDungeon dùng chìa riêng: CultistDungeonKey (id 10)."],
          ["• Tên nội bộ khác tên hiển thị: DungeonData.Name = “Cultist Dungeon” nhưng bản EN thực tế là “Cultist Ritual”."]]
    return "A3. Dungeon", g, 4


# ------------------------------------------------------- A4. Dungeon theme
def tab_theme():
    rows = load("DungeonThemeData")
    meta = {"Cultist": ("enemy_cultist", "cultist_dungeon_enemy_01.png"),
            "DragonBoss": ("enemy_dragon", "dragon_green.png"),
            "ZombieHorde": ("enemy_zombie", "zombie_head.png")}
    g = [["A4. DUNGEON THEME — DungeonThemeData (3 bản ghi)"],
         [SRC_NOTE],
         ["Bộ trang bị và animator gán cho quái của từng dungeon. Quyết định quái trông như thế nào và có bắn xa hay không."],
         ["Ảnh quái", "Theme asset", "DungeonType", "ID", "Collider radius ×", "Quái di chuyển",
          "Melee weapon", "Helmet", "Gloves", "Ranged weapon", "Ranged helmet", "Ranged gloves",
          "Animator GUID", "File ảnh nguồn"]]
    for r in sorted(rows, key=lambda x: x["DungeonType"]):
        key, fname = meta.get(r["DungeonType__enum"], ("", ""))
        g.append([img(key, fname, 70), r["_name"], r["DungeonType__enum"], r["DungeonType"],
                  r.get("ColliderRadiusMultiplier"), yn(r.get("EnemyCanMove")),
                  has(r.get("EnemyWeapon")), has(r.get("EnemyHelmet")), has(r.get("EnemyGloves")),
                  has(r.get("EnemyRangedWeapon")), has(r.get("EnemyRangedHelmet")),
                  has(r.get("EnemyRangedGloves")),
                  (r.get("EnemyAnimator") or {}).get("guid", ""), fname])
    g += [[],
          ["Nhận xét"],
          ["• Chỉ Cultist có bộ trang bị bắn xa đầy đủ → đây là dungeon duy nhất có quái ranged."],
          ["• DragonBoss: EnemyCanMove = 0 (boss đứng yên) và collider to hơn 10% (×1.1) → dễ trúng đòn hơn nhưng không đuổi theo người chơi."],
          ["• ZombieHorde không gán melee weapon → zombie đánh tay không, phù hợp kiểu bầy số lượng lớn."],
          ["• Theme chỉ gán trang bị/animator, KHÔNG chứa chỉ số máu hay sát thương: stat quái theo wave do server trả về."]]
    return "A4. Dungeon Theme", g, 4


# ------------------------------------------------------- A5. Army Power
def tab_army():
    rc = json.load(open(os.path.join(ROOT, "DecodedData", "remote_config_live.json"), encoding="utf-8"))
    g = [["A5. ARMY POWER (công thức sức mạnh đội quân) — Firebase Remote Config"],
         ["Nguồn: /data/data/com.lavalabs.dungeonrush/files/frc_*_firebase_activate.json (config_version "
          + rc.get("config_version", "?") + ")"],
         ["Army Power là chỉ số tổng sức mạnh dùng để so người chơi với độ khó màn và với đối thủ PvP."],
         ["Tham số remote", "Giá trị", "Ý nghĩa"],
         ["army_power_base", rc["army_power_base"], "Sức mạnh gốc ở mốc đầu tiên"],
         ["army_power_exponential_scaler", rc["army_power_exponential_scaler"],
          "Hệ số nhân lũy thừa = 10^0.5 (căn bậc hai của 10) → cứ hai bước thì sức mạnh ×10"],
         ["army_power_level_scaler", rc["army_power_level_scaler"], "Số level cho một bước thăng tiến mặc định"],
         ["army_power_segments", rc["army_power_segments"], "Chuỗi “Threshold,LevelScaler” phân cách bởi dấu |"],
         [],
         ["Bảng segment — class ArmyPowerSegment { int Threshold; float LevelScaler; }"],
         ["#", "Threshold", "LevelScaler", "Dải áp dụng"]]
    segs = [s.split(",") for s in rc["army_power_segments"].split("|")]
    prev = 0
    for i, (th, ls) in enumerate(segs, 1):
        g.append([i, int(th), float(ls),
                  "%d – %s" % (prev + 1, "không giới hạn" if int(th) > 100000 else th)])
        prev = int(th)
    g += [[],
          ["Nhận xét"],
          ["• LevelScaler tăng dần 20 → 30 → 40: càng lên cao càng cần NHIỀU level hơn để nhận cùng một bước ×3.162 sức mạnh → đường cong chậm dần có chủ ý."],
          ["• exponential_scaler = 3.16227766 = 10^0.5 chính xác → thiết kế theo thang log cơ số 10, không phải số ngẫu nhiên."],
          ["• Ba khoảng: 1–80 (LevelScaler 20), 81–200 (30), 201+ (40). Mốc 80 và 200 trùng vùng end-game."],
          ["• LƯU Ý: thân hàm tính Army Power trong Assembly-CSharp đã bị strip (HybridCLR) nên công thức chính xác không đọc được từ APK — phần diễn giải ở trên là suy luận từ tên tham số và giá trị."]]
    return "A5. Army Power", g, 4


# ------------------------------------------------------- A6. Experience
def tab_exp():
    rc = json.load(open(os.path.join(ROOT, "DecodedData", "remote_config_live.json"), encoding="utf-8"))
    rows = list(csv.DictReader(open(os.path.join(TABLES, "experience_required_per_level.csv"), encoding="utf-8")))
    g = [["A6. KINH NGHIỆM / LEVEL — Firebase Remote Config (100 mốc)"],
         ["Nguồn: remote key experience_required_per_level (config_version " + rc.get("config_version", "?") + ")"],
         ["Bảng XP cần để lên từng level. Level ngoài bảng sinh bằng công thức (base / mult / scaler)."],
         ["Tham số remote", "Giá trị", "Ý nghĩa"],
         ["experience_level_base", rc["experience_level_base"], "XP gốc dùng cho level ngoài bảng"],
         ["experience_level_mult", rc["experience_level_mult"], "Hệ số nhân theo level"],
         ["experience_level_scaler", rc["experience_level_scaler"], "Hệ số điều chỉnh chung (đang = 1, tức tắt)"],
         [],
         ["Level", "XP cần lên level này", "XP cộng dồn", "Chênh lệch vs level trước", "Tỷ lệ vs level trước"]]
    total = 0
    prev = None
    for r in rows:
        lv, xp = int(r["level"]), int(r["xp_required"])
        total += xp
        g.append([lv, xp, total, "" if prev is None else xp - prev,
                  "" if prev is None else round(xp / prev, 3)])
        prev = xp
    g += [[],
          ["Nhận xét"],
          ["• Tổng XP để đạt level 100 = %s." % format(total, ",").replace(",", ".")],
          ["• Đường cong chia ba pha rõ rệt: lv 1–7 tăng gấp bội (×3, ×2, ×2.5) cho onboarding nhanh; lv 8–20 tăng đều +1.800 XP/level; từ lv 21 trở đi gần như tuyến tính với bước tăng dần."],
          ["• Từ level ~20 hệ số mỗi level chỉ còn ~1,02–1,05 → không có tường chắn XP; tempo do trang bị và Army Power quyết định chứ không phải XP."],
          ["• Level 100 cần 1.377.500 XP, gấp ~6.900 lần level 1 (200 XP)."]]
    return "A6. Kinh nghiệm & Level", g, 9


def service():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    return build("sheets", "v4", credentials=creds, cache_discovery=False)


def main():
    tabs = [tab_map(), tab_tier(), tab_dungeon(), tab_theme(), tab_army(), tab_exp()]
    svc = service().spreadsheets()
    meta = svc.get(spreadsheetId=TARGET).execute()
    existing = {s["properties"]["title"]: s["properties"]["sheetId"] for s in meta["sheets"]}

    adds = [{"addSheet": {"properties": {"title": t[0], "index": i + 1}}}
            for i, t in enumerate(tabs) if t[0] not in existing]
    if adds:
        res = svc.batchUpdate(spreadsheetId=TARGET, body={"requests": adds}).execute()
        for r in res["replies"]:
            p = r["addSheet"]["properties"]
            existing[p["title"]] = p["sheetId"]
            print("added tab id", p["sheetId"])

    reqs = []
    for title, grid, hdr in tabs:
        sid = existing[title]
        svc.values().clear(spreadsheetId=TARGET, range="'%s'" % title).execute()
        svc.values().update(spreadsheetId=TARGET, range="'%s'!A1" % title,
                            valueInputOption="USER_ENTERED", body={"values": grid}).execute()
        ncol = max(len(r) for r in grid)
        reqs += [
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
        if grid[hdr - 1][0].startswith("Ảnh"):
            nrow = sum(1 for r in grid[hdr:] if len(r) > 3)
            h = 95 if title.startswith(("A1", "A3")) else 75
            reqs += [
                {"updateDimensionProperties": {"range": {
                    "sheetId": sid, "dimension": "COLUMNS", "startIndex": 0, "endIndex": 1},
                    "properties": {"pixelSize": 300 if h > 80 else 90}, "fields": "pixelSize"}},
                {"updateDimensionProperties": {"range": {
                    "sheetId": sid, "dimension": "ROWS", "startIndex": hdr, "endIndex": hdr + nrow},
                    "properties": {"pixelSize": h}, "fields": "pixelSize"}},
            ]
    svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()

    # A2: to nen o mau dung dung mau tier
    tier_title, tier_grid, _ = tabs[1]
    sid = existing[tier_title]
    cells = []
    for i, row in enumerate(tier_grid[4:]):
        if len(row) < 7:
            break
        for col in (4, 5, 6):
            hx = row[col]
            if not hx.startswith("#"):
                continue
            r, gg, b = (int(hx[k:k + 2], 16) / 255 for k in (1, 3, 5))
            fg = {"red": 1, "green": 1, "blue": 1} if (r + gg + b) < 1.2 else {"red": 0, "green": 0, "blue": 0}
            cells.append({"repeatCell": {
                "range": {"sheetId": sid, "startRowIndex": 4 + i, "endRowIndex": 5 + i,
                          "startColumnIndex": col, "endColumnIndex": col + 1},
                "cell": {"userEnteredFormat": {"backgroundColor": {"red": r, "green": gg, "blue": b},
                                               "textFormat": {"foregroundColor": fg}}},
                "fields": "userEnteredFormat(backgroundColor,textFormat)"}})
    svc.batchUpdate(spreadsheetId=TARGET, body={"requests": cells}).execute()

    for t in tabs:
        print("wrote tab with", len(t[1]), "rows")
    print("images:", ("ON %d" % len(IMGS)) if IMGS else "OFF (chua upload len Drive)")


if __name__ == "__main__":
    main()
