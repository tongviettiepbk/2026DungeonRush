#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM I "He thong nen" vao Google Sheet DungeonRush - GDD. Moi tinh nang = 1 tab.
#   I1 Remote Config (Firebase) — remote_config_live.json + RemoteConfigController.cs
#   I2 Localization — DecodedData/localization/* + LanguageSelectPopup.prefab
#   I3 Audio — tables/AudioConfig.json + AudioController.cs + thu muc AudioClip
# Anh: DecodedData/image_urls_i.json (co quoc gia, upload boi upload_images_i.py).
# Chay: python tools/write_group_i.py   (print ASCII vi console cp1252)
import json, os, re
from collections import Counter, OrderedDict
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DATA = os.path.join(ROOT, "DecodedData")
LOC = os.path.join(DATA, "localization")
CLIPS = os.path.join(ROOT, "AssetRipper", "ExportedProject", "Assets", "AudioClip")

IMGS = json.load(open(os.path.join(DATA, "image_urls_i.json"), encoding="utf-8"))


def img(key, h=34):
    m = IMGS.get(key)
    if not m:
        return ""
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w400" % m["id"]
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def safe(s):
    # tranh bay Sheets: cell text bat dau bang '+' hoac '=' voi USER_ENTERED bi coi la cong thuc
    if isinstance(s, str) and s[:1] in ("+", "="):
        return "'" + s
    return s


# ---------------------------------------------------------------- I1 Remote Config
def tab_remote():
    rc = json.load(open(os.path.join(DATA, "remote_config_live.json"), encoding="utf-8"))
    code_keys = set(json.load(open(os.path.join(DATA, "remote_config_keys.json"), encoding="utf-8")))
    exp = rc["experience_required_per_level"].split(",")
    forge_rows = rc["forge_rarity_probabilities"].count("|") + 1

    # khoa -> (gia tri hien thi, y nghia, tab chi tiet)
    meta = OrderedDict([
        ("config_version", (rc["config_version"], "Phiên bản bộ config đang phát hành", "—")),
        ("ab_test_group", (rc["ab_test_group"], "Nhóm A/B test của thiết bị này — 'control' = nhóm đối chứng, "
                                                "chứng tỏ hạ tầng A/B test đang chạy", "—")),
        ("tutorials_enabled", (rc["tutorials_enabled"], "Công tắc bật/tắt toàn bộ tutorial từ xa", "—")),
        ("boss_gate_enabled", (rc["boss_gate_enabled"], "Công tắc Boss Gate — đang TẮT trên bản live "
                                                        "(tính năng bị khoá từ xa dù có sẵn trong app)", "B1")),
        ("chest_store_enabled", (rc["chest_store_enabled"], "Công tắc cửa hàng rương", "H1, H3")),
        ("army_power_base", (rc["army_power_base"], "Công thức Army Power: hệ số gốc", "A5")),
        ("army_power_exponential_scaler", (rc["army_power_exponential_scaler"],
                                           "Hệ số mũ (3.162 ≈ căn 10 → x10 mỗi 2 bậc)", "A5")),
        ("army_power_level_scaler", (rc["army_power_level_scaler"], "Hệ số theo level", "A5")),
        ("army_power_segments", (rc["army_power_segments"], "Mốc phân đoạn 'ngưỡng,scaler' phân tách '|'", "A5")),
        ("experience_level_base", (rc["experience_level_base"], "Đường cong EXP: hệ số gốc", "A6")),
        ("experience_level_mult", (rc["experience_level_mult"], "Đường cong EXP: hệ số nhân", "A6")),
        ("experience_level_scaler", (rc["experience_level_scaler"], "Đường cong EXP: hệ số scale", "A6")),
        ("experience_required_per_level", ("100 mốc: %s, %s, … %s" % (exp[0], exp[1], "{:,}".format(int(exp[-1]))),
                                           "Bảng EXP cần cho từng level 1→100 (ghi đè công thức)", "A6")),
        ("forge_rarity_probabilities", ("%d dòng × 10 rarity (mỗi dòng tổng 100%%)" % forge_rows,
                                        "Bảng tỉ lệ lên bậc rarity khi Forge", "C9")),
        ("weapon_offer_config", (rc["weapon_offer_config"], "Chuỗi 'level,ItemId|…' — 5 mốc offer vũ khí", "G3")),
        ("starting_loot_box_count", (rc["starting_loot_box_count"], "Số Loot Box người chơi mới có sẵn", "H3")),
    ])

    g = [["I1. REMOTE CONFIG (Firebase) — Bảng điều khiển từ xa của game (remote_config_live)"],
         ["Nguồn: cache Firebase Remote Config LIVE pull từ thiết bị root (LDPlayer) — file "
          "frc_*_firebase_activate.json trong /data/data/com.lavalabs.dungeonrush/files · đối chiếu "
          "RemoteConfigController.cs (Assembly-CSharp) · parse_remote_config.py"],
         ["Remote Config là 'bảng điều khiển LiveOps': server Firebase đẩy tham số xuống app lúc khởi động, "
          "cho phép hãng chỉnh cân bằng, bật/tắt tính năng, chạy A/B test mà KHÔNG cần phát hành bản mới. "
          "Đây là bản LIVE thật (config_version %s) lấy trực tiếp từ cache app đang chạy — gồm %d khoá, "
          "trong đó %d khoá khai báo hằng trong RemoteConfigController.cs." % (
              rc["config_version"], len(rc), len(code_keys))],
         [],
         ["1. TOÀN BỘ %d KHOÁ LIVE (config_version %s)" % (len(rc), rc["config_version"])],
         ["Khoá", "Giá trị LIVE", "Ý nghĩa", "Tab chi tiết", "Khai báo trong code?"]]
    for k, (val, mean, tab) in meta.items():
        g.append([k, safe(str(val)), mean, tab, "Có" if k in code_keys else "KHÔNG (chỉ thấy trên live)"])
    live_only = sorted(set(rc) - code_keys)
    g += [[],
          ["2. CƠ CHẾ HOẠT ĐỘNG"],
          ["Bước", "Chi tiết"],
          ["Fetch", "App khởi động → SDK Firebase fetch config theo project, có điều kiện A/B (ab_test_group)"],
          ["Cache", "Bản activate lưu ở files/frc_<app-id>_firebase_activate.json — chính là nguồn bảng này"],
          ["Fallback", "RemoteConfigController nhúng sẵn bảng forge mặc định (const ubs) TRÙNG bản live → "
                       "chưa fetch được vẫn chơi đúng số liệu"],
          ["Ghi đè", "Giá trị live thắng default trong app; server đổi là trận sau áp dụng ngay"],
          [],
          ["Nhận xét"],
          ["• Đây là 'trái tim LiveOps' của game: 5 hệ lớn (Army Power A5, EXP A6, Forge C9, Weapon Offer G3, "
           "Loot/Chest H3) đều lấy số từ đây → hãng cân bằng kinh tế game theo thời gian thực, không cần update."],
          ["• Có kill-switch từ xa: boss_gate_enabled=false — tính năng Boss Gate ĐÃ CÓ trong app nhưng bị "
           "tắt trên live (đang thử nghiệm hoặc gỡ tạm); chest_store_enabled/tutorials_enabled cùng dạng."],
          ["• ab_test_group='control' xác nhận hạ tầng A/B test: cùng bản app, mỗi nhóm người chơi có thể "
           "nhận bảng số khác nhau — số liệu ở đây là của nhóm đối chứng."],
          ["• %d khoá thấy trên live nhưng không khai báo hằng trong RemoteConfigController (%s) — được đọc "
           "ở controller khác, cho thấy config còn phủ rộng hơn file này." % (len(live_only), ", ".join(live_only))],
          ["• Độ tin: giá trị là THẬT 100%% (cache live của app), nhưng chỉ là snapshot tại thời điểm pull "
           "(2026-07, config_version %s) — hãng có thể đã đổi sau đó." % rc["config_version"]]]
    return "I1. Remote Config", g, 1


# ---------------------------------------------------------------- I2 Localization
LANGS = [  # (code hien thi, ten trong game, cot CSV/file json, key co)
    ("en", "English", "en", "flag_en"), ("tr", "Türkçe", "tu", "flag_tr"),
    ("ar", "عربي", "ar", "flag_ar"), ("fr", "Français", "fr", "flag_fr"),
    ("de", "Deutsch", "ge", "flag_de"), ("it", "Italiano", "ta", "flag_it"),
    ("ja", "日本語", "ja", "flag_ja"), ("ko", "한국인", "ko", "flag_ko"),
    ("es", "Español", "sp", "flag_es"), ("pt-BR", "Português", "po", "flag_pt")]


def tab_localization():
    data = {short: json.load(open(os.path.join(LOC, "strings_%s.json" % short), encoding="utf-8"))
            for _, _, short, _ in LANGS}
    en = data["en"]
    cats = Counter(k.split(".")[0] for k in en)
    missing = sorted(k for k in en if k not in data["fr"])

    g = [["I2. LOCALIZATION — Đa ngôn ngữ (Unity Localization + Addressables, 10 ngôn ngữ · 1,031 chuỗi)"],
         ["Nguồn: bundle localization-* trong APK (extract2_localization.py, UnityPy) · "
          "LanguageSelectPopup.prefab (danh sách ngôn ngữ + cờ) · LocalizationController.cs, LocalizedLabel.cs"],
         ["Game dùng Unity Localization package: chuỗi chia 6 bảng (StringTable collection) đóng trong bundle "
          "Addressables, mỗi ngôn ngữ 1 bundle riêng. UI gắn key qua LocalizedLabel; đổi ngôn ngữ trong "
          "LanguageSelectPopup (10 lựa chọn, có cờ). English là ngôn ngữ gốc đủ 1,031 chuỗi; 9 ngôn ngữ còn "
          "lại 1,006 chuỗi — thiếu đúng 25 chuỗi Clan mới chưa kịp dịch."],
         [],
         ["1. 10 NGÔN NGỮ HỖ TRỢ (thứ tự trong LanguageSelectPopup)"],
         ["Cờ", "Code", "Tên trong game", "Số chuỗi", "Ghi chú"]]
    for code, name, short, flag in LANGS:
        note = "Ngôn ngữ gốc — đủ 100%" if code == "en" else "Thiếu 25 chuỗi Clan mới (98%)"
        g.append([img(flag), code, name, len(data[short]), note])
    g += [["", "", "", "", "Cờ theo file ISO trong Texture2D (GB/TR/SA/FR/DE/IT/JP/KR/ES/BR) — mapping "
                          "cờ↔ngôn ngữ là SUY ĐOÁN vì guid FlagSprite không tra được (thiếu .meta)"],
          [],
          ["2. 6 BẢNG CHUỖI (StringTable collection)"],
          ["Bảng", "Số key", "Nội dung"]]
    cat_desc = {"Items": "Tên + mô tả toàn bộ trang bị/companion — nhóm lớn nhất",
                "Popup": "Text các popup (shop, forge, settings, clan…)",
                "Clan": "Toàn bộ UI Clan / Clan War / chat",
                "UI": "Nhãn UI chung (nút, card, HUD)",
                "Errors": "Thông báo lỗi (mạng, server, validate tên…)",
                "Common": "Chuỗi dùng chung (Level, OK, Cancel…)"}
    for cat, n in cats.most_common():
        g.append([cat, n, cat_desc.get(cat, "")])
    g.append(["TỔNG", sum(cats.values()), "10 ngôn ngữ × ~1,031 = ~10,310 chuỗi dịch trong APK"])

    # bang doi chieu mau: mỗi bảng 1 key ngắn có đủ 10 ngôn ngữ
    g += [[], ["3. VÍ DỤ ĐỐI CHIẾU 10 NGÔN NGỮ (mỗi bảng 1 chuỗi)"],
          ["Key"] + [c for c, _, _, _ in LANGS]]
    samples = []
    for cat in ["Common", "UI", "Items", "Popup", "Errors", "Clan"]:
        for k in sorted(en):
            if k.startswith(cat + ".") and len(en[k]) <= 22 and all(k in data[s] for _, _, s, _ in LANGS):
                samples.append(k)
                break
    for k in samples:
        g.append([k] + [safe(data[s][k]) for _, _, s, _ in LANGS])

    g += [[], ["4. 25 CHUỖI CHỈ CÓ TIẾNG ANH (chưa dịch — toàn bộ thuộc Clan)"],
          ["Key", "Text EN"]]
    for k in missing[:12]:
        g.append([k, en[k]])
    g.append(["… (%d key còn lại cùng dạng: Common.Clan.*, Popup.Clan.*, UI.Clan.*)" % (len(missing) - 12), ""])
    g += [[],
          ["Nhận xét"],
          ["• Chọn 10 ngôn ngữ = bản đồ thị trường mục tiêu: EN + EU lớn (FR/DE/IT/ES/PT-BR) + Đông Á nạp "
           "mạnh (JA/KO) + TR/AR (thị trường mobile tăng trưởng). KHÔNG có tiếng Việt/Trung/Nga/Indo."],
          ["• 25 chuỗi Clan chưa dịch (chỉ EN) → tính năng Clan/Clan War được thêm ở bản cập nhật gần nhất, "
           "kịp code nhưng chưa kịp vòng dịch thuật — khớp nhận định F3 rằng Clan là hệ mới nhất."],
          ["• Items chiếm 38%% số chuỗi (388/1031): mọi trang bị đều có tên + mô tả riêng được dịch — đầu tư "
           "'flavor text' cho gacha trang bị, các bảng C1–C8 lấy Name_en/Desc_en từ chính nguồn này."],
          ["• Kiến trúc chuẩn công nghiệp: string tách khỏi code (đổi/thêm ngôn ngữ chỉ cần rebuild bundle "
           "Addressables), key phân cấp 'Bảng.Màn hình.Phần tử' dễ tra — GDD nên bắt chước cấu trúc key này."],
          ["• Tiếng Ả Rập có đủ 1,006 chuỗi (RTL) — hiếm game casual chịu làm RTL, xác nhận MENA là thị "
           "trường chủ đích chứ không phải dịch máy cho có."]]
    return "I2. Localization", g, 1


# ---------------------------------------------------------------- I3 Audio
def tab_audio():
    cfg = json.load(open(os.path.join(DATA, "tables", "AudioConfig.json"), encoding="utf-8"))[0]
    clips = sorted(os.listdir(CLIPS))
    n = len(clips)

    def grp(pat):
        return [c for c in clips if re.match(pat, c)]

    groups = [
        ("bullet_impact_*", grp(r"bullet_impact"), "Đạn trúng theo BỀ MẶT: dirt/grass/ice/flesh — 4 biến thể "
                                                   "mỗi loại để không lặp tai"),
        ("bow_crossbow_*", grp(r"bow_crossbow"), "Tiếng bắn cung/nỏ (4 biến thể) — WeaponData.ShootAudios"),
        ("etfx_shoot/explosion_*", grp(r"etfx_"), "Phép: bắn energy/fireball/lightning/magic + nổ plasma/poof/soul"),
        ("magic_flame_of_light_*", grp(r"magic_flame"), "Phép lửa ánh sáng (3 biến thể)"),
        ("footstep_*", grp(r"footstep"), "Bước chân đất (3) + ván gỗ (1)"),
        ("pick_axe_*", grp(r"pick_axe"), "Cuốc đập đá — hệ Đào mỏ E1 (3 biến thể)"),
        ("punch_grit_*", grp(r"punch_grit"), "Đấm tay không (3 biến thể) = UnarmedHitAudios trong AudioConfig"),
        ("Ice (1/2)", grp(r"Ice \("), "Hiệu ứng băng"),
        ("loop dài", [c for c in clips if "loop" in c], "Vòng lặp môi trường/kỹ năng: điện, energy, nấu potion"),
        ("skill có tên", [c for c in clips if c[0].isdigit()], "Kỹ năng companion/vũ khí: HealBurst, "
                                                              "Spearthrower (+projectile), Crossbow impact"),
        ("UI & sự kiện", [c for c in clips if c.startswith(("ui_", "levelup", "whoosh", "rock_door", "wood_",
                                                            "Special"))], "Click nút, level-up, mở cửa, vỡ gỗ, power-up"),
        ("Nhạc nền", [c for c in clips if "Shire" in c], "'Shire Evening Echoes (Edit)' — bản nhạc DUY NHẤT"),
    ]

    def v(x):
        return int(x) if x == int(x) else x

    # (truong, volume, clip suy doan, do tin, ghi chu)
    events = [
        ("ButtonClickSound", cfg["ButtonClickVolume"], "ui_menu_button_click_16.ogg", "Cao (clip UI duy nhất)",
         "Mọi nút bấm UI"),
        ("ItemEquipSound", cfg["ItemEquipVolume"], "— (fileID 0, KHÔNG gán clip)", "Chắc chắn",
         "Mặc đồ im lặng — sự kiện bị tắt tiếng có chủ đích"),
        ("ItemSellSound", cfg["ItemSellVolume"], "cùng clip với LootCollect (trùng guid e64abf71)", "Chắc chắn (so guid)",
         "Bán đồ = tiếng nhặt loot — tái dùng 1 clip"),
        ("BoxPunchSound", cfg["BoxPunchVolume"], "wood_tree_branch_break_03.ogg (?)", "Thấp",
         "Đấm rương loot"),
        ("BoxOpenSound", cfg["BoxOpenVolume"], "whoosh_low_deep_soft_01.ogg (?)", "Thấp", "Mở rương"),
        ("LevelUpSound", cfg["LevelUpVolume"], "levelup.ogg", "Cao (tên trùng)",
         "Volume 2 = DUY NHẤT được khuếch đại — nhấn khoảnh khắc thưởng"),
        ("DoorOpenSound", cfg["DoorOpenVolume"], "rock_door_slide_block_move_drag_03.ogg", "Cao (clip cửa duy nhất)",
         "Mở cửa phòng dungeon — nén 0.151 vì lặp rất thường xuyên"),
        ("LootCollectSound", cfg["LootCollectVolume"], "cùng clip với ItemSellSound (dùng chung)", "Chắc chắn (so guid)",
         "Nhặt loot"),
        ("UnarmedHitAudios ×3", cfg["UnarmedHitAudioVolume"], "punch_grit_wet_impact_01/02/03.ogg",
         "Cao (đúng 3 file / 3 guid)", "Đánh tay không — nén 0.239, random 3 biến thể"),
        ("Music", cfg["MusicVolume"], "Shire Evening Echoes (Edit).ogg", "Cao (nhạc duy nhất)",
         "Nhạc nền toàn game, volume 0.5"),
    ]

    g = [["I3. AUDIO — Hệ thống âm thanh (AudioConfig + AudioController + %d AudioClip)" % n],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/AudioConfig.asset · AudioController.cs · thư mục "
          "Assets/AudioClip (%d file .ogg) · WeaponData (audio theo vũ khí)" % n],
         ["Kiến trúc 2 tầng: (1) AudioConfig — ScriptableObject gom SFX SỰ KIỆN CHUNG (click, level-up, mở "
          "rương, nhặt loot…) kèm volume từng sự kiện; (2) audio THEO VŨ KHÍ nằm ngay trong WeaponData "
          "(HitAudios / ShootAudios / ProjectileHitAudios + volume riêng). AudioController chạy 2 AudioSource "
          "tách kênh (Gameplay + Music) và có AudioReplayInterval chống spam 1 tiếng lặp quá dày. "
          "LƯU Ý: cột 'Clip' là SUY ĐOÁN theo tên/số lượng file vì guid không tra được (thiếu .meta); "
          "riêng các dòng ghi 'so guid' là chắc chắn."],
         [],
         ["1. AUDIOCONFIG — SFX SỰ KIỆN CHUNG (volume gốc từng sự kiện)"],
         ["Sự kiện", "Volume", "Clip (suy đoán)", "Độ tin", "Ghi chú"]]
    for name, vol, clip, conf, note in events:
        g.append([name, v(vol), clip, conf, note])
    g += [[],
          ["2. KHO %d CLIP .OGG — NHÓM THEO CHỨC NĂNG" % n],
          ["Nhóm", "Số clip", "File", "Dùng cho"]]
    seen = set()
    for name, files, use in groups:
        files = [f for f in files if f not in seen]
        seen.update(files)
        g.append([name, len(files), ", ".join(f[:-4] for f in files)[:180], use])
    rest = [c for c in clips if c not in seen]
    if rest:
        g.append(["khác", len(rest), ", ".join(rest), ""])
    g += [[],
          ["3. AUDIO THEO VŨ KHÍ (trong WeaponData, không nằm ở AudioConfig)"],
          ["Trường", "Ý nghĩa"],
          ["ShootAudios + ShootAudioVolume", "Tiếng BẮN của vũ khí ranged (vd 4 biến thể bow_crossbow_*)"],
          ["HitAudios + HitAudioVolume", "Tiếng CHÉM/ĐẬP trúng của vũ khí melee"],
          ["ProjectileHitAudios + ProjectileHitAudioVolume",
           "Tiếng ĐẠN TRÚNG đích — khớp họ bullet_impact_* theo bề mặt (flesh/dirt/grass/ice)"],
          [],
          ["Nhận xét"],
          ["• Footprint audio cực gọn: %d clip .ogg + đúng 1 bản nhạc nền — chuẩn tối ưu dung lượng game "
           "mobile casual (APK nhẹ để quảng cáo UA rẻ); cảm giác đa dạng tạo bằng 3–4 BIẾN THỂ random mỗi "
           "tiếng thay vì nhiều tiếng khác nhau." % n],
          ["• Mixing làm bằng số trong config chứ không bằng file: tiếng lặp dày bị nén sẵn (DoorOpen 0.151, "
           "UnarmedHit 0.239) còn LevelUp x2 — sự kiện thưởng là tiếng TO NHẤT game, đúng công thức "
           "dopamine của idle game."],
          ["• Tái dùng có chủ đích: ItemSell = LootCollect (trùng guid), ItemEquip tắt hẳn — 'ngân sách' "
           "tiếng chỉ dồn cho vòng lặp thưởng (loot, level-up, mở rương)."],
          ["• Tách 2 AudioSource Gameplay/Music + AudioReplayInterval chống spam: khi cả bầy quái trúng đòn "
           "cùng lúc, tiếng không dồn thành 'vỡ loa' — chi tiết nhỏ nhưng chuyên nghiệp."],
          ["• Tên file lộ nguồn asset store (etfx_* = Epic Toon FX, họ bullet_impact/footstep từ thư viện "
           "SFX thương mại) — chiến lược mua-ghép asset thay vì thu âm riêng, khớp quy mô studio nhỏ."]]
    return "I3. Audio", g, 1


# ---------------------------------------------------------------- ghi sheet
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
        img_rows = []
        for i, row in enumerate(grid):
            hs = [int(cell.split(",")[2]) for cell in row
                  if isinstance(cell, str) and cell.startswith("=IMAGE")]
            if hs:
                img_rows.append(i)
                reqs.append({"updateDimensionProperties": {"range": {
                    "sheetId": sid, "dimension": "ROWS", "startIndex": i, "endIndex": i + 1},
                    "properties": {"pixelSize": max(hs) + 12}, "fields": "pixelSize"}})
        svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()
        print("wrote tab:", title.encode("ascii", "replace").decode(), "|", len(grid), "rows,",
              len(img_rows), "image rows")


def main():
    tabs = [tab_remote(), tab_localization(), tab_audio()]
    # index 31 = ...H4(30) + 1
    write_tabs(tabs, 31)
    print("images I:", len(IMGS))


if __name__ == "__main__":
    main()
