#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM B "Boss modes" vao Google Sheet DungeonRush - GDD.
# Moi tinh nang = 1 tab: B1 Boss Gate, B2 Boss Rush, B3 Boss Rush League.
# Anh: DecodedData/image_urls_bc.json (upload boi tools/upload_images_bc.py).
# Chay: python tools/write_group_b.py   (print ASCII vi console cp1252)
import json, os
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
IMG_A = os.path.join(ROOT, "DecodedData", "image_urls_a.json")  # tai dung anh nhom A (boss_rush_illustration)
if os.path.exists(IMG_A):
    for k, v in json.load(open(IMG_A, encoding="utf-8")).items():
        IMGS.setdefault("A_" + k, v)

BEHAVIOR = {1: "DirectChase (đuổi thẳng)", 2: "RepositionAfterAttack (dịch chuyển sau đòn)"}
BOSS_ASSET = {"Dark Lich": "lych_01", "Crimson Lich": "lych_02", "Elder Lich": "lych_03",
              "Black Dragon": "dragon_purple", "Green Dragon": "dragon_green", "Red Dragon": "dragon_red",
              "Ogre King": "ogre_01", "Ogre Chieftain": "ogre_02",
              "Green Hag": "witch_01", "Purple Hag": "witch_02"}


def load(name):
    return json.load(open(os.path.join(TABLES, name + ".json"), encoding="utf-8"))


def img(key, fallback="", h=70):
    m = IMGS.get(key)
    if not m:
        return fallback
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def yn(v):
    return "Có" if v in (1, True, "1") else "—"


def boss_rows(defs):
    rows = []
    for b in defs:
        name = b["BossName"]
        rows.append([img("boss_" + name.replace(" ", "_")), name, BEHAVIOR.get(b["EnemyBehaviorType"], b["EnemyBehaviorType"]),
                     yn(b["CanMove"]), b["ColliderRadiusMultiplier"], b["MassMultiplier"],
                     b["RepositionDelay"], b["RepositionSearchRadius"],
                     (b.get("Weapon") or {}).get("guid", "")[:8] + "…", BOSS_ASSET.get(name, "") + ".png"])
    return rows


BOSS_HDR = ["Ảnh", "Boss", "Hành vi (EnemyBehaviorType)", "Tự di chuyển (CanMove)", "Collider ×",
            "Khối lượng × (Mass)", "Reposition delay (s)", "Bán kính tìm ô mới", "Weapon GUID", "Asset ảnh"]


# ------------------------------------------------------------ B1. Boss Gate
def tab_bossgate():
    defs = load("BossGateConfig")[0]["BossDefinitions"]
    g = [["B1. BOSS GATE — BossGateConfig (10 boss definition)"],
         [SRC_NOTE],
         ["Cổng boss chặn đường trong dungeon chính: tới cửa boss, người chơi phải hạ boss để đi tiếp. "
          "Remote Config đang TẮT tính năng này trên bản live: boss_gate_enabled = false (v17.0.0)."],
         BOSS_HDR]
    g += boss_rows(defs)
    g += [[],
          ["Nhận xét"],
          ["• 10 boss chia 4 họ theo animator: 3 Lich (Dark/Crimson/Elder), 3 Dragon (Black/Green/Red), 2 Ogre (King/Chieftain), 2 Hag (Green/Purple)."],
          ["• Cả 10 boss dùng CHUNG một bộ tham số hành vi: đứng yên (CanMove = 0), collider ×1.5, khối lượng ×5, sau mỗi đòn dịch chuyển (RepositionAfterAttack) với delay 5 s, bán kính tìm ô 3 — khác nhau duy nhất ở skin + bộ trang bị."],
          ["• Bộ 10 boss này trùng GUID 100% với BossRushConfig.BossDefinitions → hai chế độ dùng chung roster boss, chỉ khác luật chơi."],
          ["• Config không có chỉ số máu/sát thương boss — stat do server trả theo tier/wave."],
          ["• boss_gate_enabled = false trên live: tính năng đã ship trong client nhưng đang bị tắt bằng remote toggle."]]
    return "B1. Boss Gate", g, 4


# ------------------------------------------------------------ B2. Boss Rush
def tab_bossrush():
    c = load("BossRushConfig")[0]
    gs, cam = c["ArenaGridSize"], c["CameraFollowOffset"]
    g = [["B2. BOSS RUSH — BossRushConfig"],
         [SRC_NOTE],
         ["Chế độ đánh boss tính điểm theo mùa: vào trận 30 giây, gây sát thương nhiều nhất có thể để lấy điểm xếp hạng. "
          "Matchmaking, bảng thưởng và chỉ số boss do server điều khiển (BossRushPool/Join/Claim…DTO)."],
         [img("A_bossrush", "boss_rush_illustration.png", 120)],
         ["Tham số", "Giá trị", "Ý nghĩa"],
         ["ArenaGridSize", "%d × %d" % (gs["x"], gs["y"]), "Lưới đấu trường riêng 12 × 12 (map thường 9 × 12)"],
         ["CameraFollowOffset", "(%g, %g, %g)" % (cam["x"], cam["y"], cam["z"]), "Camera bám người chơi, hạ thấp 5 đơn vị"],
         ["ArenaBoundsThickness", c["ArenaBoundsThickness"], "Độ dày tường bao đấu trường"],
         ["PlayerColliderRadiusMultiplier", c["PlayerColliderRadiusMultiplier"], "Collider người chơi giữ nguyên ×1"],
         ["FightDuration", str(c["FightDuration"]) + " s", "Mỗi lượt đánh boss kéo dài 30 giây"],
         ["DamageDivisor", c["DamageDivisor"], "Điểm = sát thương gây ra ÷ 100"],
         ["MaxFreeEntries", c["MaxFreeEntries"], "Số lượt miễn phí"],
         ["MaxAdEntries", c["MaxAdEntries"], "Số lượt thêm khi xem quảng cáo"],
         ["TierConfigs", "1 dòng: Tier 0, AttackMultiplier 0, RewardMultiplier 0", "Placeholder — hệ số thật do server trả"],
         [],
         ["Roster boss (BossDefinitions — trùng GUID với B1. Boss Gate)"],
         BOSS_HDR]
    g += boss_rows(c["BossDefinitions"])
    g += [[],
          ["Nhận xét"],
          ["• Vòng lặp chơi: tối đa 3 lượt miễn phí + 3 lượt xem quảng cáo; mỗi lượt 30 s; điểm = damage/100 → chế độ đua DPS thuần túy."],
          ["• Đấu trường 12 × 12 rộng hơn map thường (9 × 12) và không vật cản — chỗ cho boss collider ×1.5 và cơ chế dịch chuyển."],
          ["• TierConfigs trong APK chỉ có 1 dòng toàn số 0: hệ số tấn công/thưởng theo tier là placeholder, giá trị thật do server quyết định (thấy rõ qua bộ DTO BossRushPool/StartFight/FightResult/Claim)."],
          ["• Boss không tự di chuyển nhưng RepositionAfterAttack mỗi 5 s → người chơi phải liên tục áp sát lại, tạo nhịp cho chế độ."]]
    return "B2. Boss Rush", g, 5


# ----------------------------------------------------- B3. Boss Rush League
def tab_league():
    c = load("BossRushLeagueConfig")[0]
    g = [["B3. BOSS RUSH LEAGUE — BossRushLeagueConfig"],
         [SRC_NOTE],
         ["Giải đấu xếp hạng của Boss Rush: người chơi cùng bracket đua điểm, cuối mùa nhận thưởng theo thứ hạng. "
          "Config local chỉ là khung fallback — bảng thưởng thật server trả qua BossRushRewardTableDTO."],
         ["Tham số", "Giá trị"],
         ["PvPConfig (tham chiếu)", "GUID " + c["PvPConfig"]["guid"][:12] + "… → dùng chung hệ league/trophy với PvP (xem nhóm F)"],
         ["Số tier league trong APK", len(c["Tiers"])],
         [],
         ["Bảng thưởng theo tier — class BossRushRewardTier { MinPosition; MaxPosition; Rewards }"],
         ["Tier", "Hạng từ", "Hạng đến", "Phần thưởng (RewardType)", "Số lượng"]]
    for t in c["Tiers"]:
        for rt in t["RewardTiers"]:
            for rw in rt["Rewards"]:
                g.append([t["Tier"], rt["MinPosition"], rt["MaxPosition"],
                          "Bone (id 0)" if rw["Type"] == 0 else rw["Type"], rw["Amount"]])
    g += [[],
          ["Nhận xét"],
          ["• Cả 3 tier trong APK giống hệt nhau: hạng 1–100 đều nhận 1.000 Bone → rõ ràng là dữ liệu placeholder/fallback, không phải bảng thưởng thật."],
          ["• RewardType id 0 = Bone (enum RewardType: Bone, Gem, DragonBossDungeonKey, ZombieHordeDungeonKey, Lootbox, Exp, …)."],
          ["• League tái dùng PvPConfig (tham chiếu trực tiếp asset) → thang league/trophy của Boss Rush chung khung với đấu trường PvP."],
          ["• Kết luận thiết kế: toàn bộ economy của Boss Rush nằm phía server; client chỉ giữ luật chơi (30 s, damage/100) và roster boss."]]
    return "B3. Boss Rush League", g, 9


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
        # dong co anh (cot A bat dau bang =IMAGE) -> row height
        for i, row in enumerate(grid):
            if row and isinstance(row[0], str) and row[0].startswith("=IMAGE"):
                h = 130 if ",4,120," in row[0] else 78
                reqs.append({"updateDimensionProperties": {"range": {
                    "sheetId": sid, "dimension": "ROWS", "startIndex": i, "endIndex": i + 1},
                    "properties": {"pixelSize": h}, "fields": "pixelSize"}})
    svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()
    for t in tabs:
        print("wrote", len(t[1]), "rows")


def main():
    # index 7 = ngay sau A6 (0: Mind-map, 1..6: A1-A6)
    write_tabs([tab_bossgate(), tab_bossrush(), tab_league()], 7)
    print("images:", ("ON %d keys" % len(IMGS)) if IMGS else "OFF")


if __name__ == "__main__":
    main()
