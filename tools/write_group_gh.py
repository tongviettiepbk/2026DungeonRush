#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM G "Tien trinh / Meta" + NHOM H "Kinh te / Shop" vao Google Sheet DungeonRush - GDD.
# Moi tinh nang = 1 tab.
#   G1 Mastery (MasteryUpgradeData + MasteryConfig)
#   G2 Battle Pass (BattlePassConfig)
#   G3 Weapon Offer (remote:weapon_offer_config + WeaponData)
#   H1 Ruong (ChestData + ChestConfig)
#   H2 Bundle ruong (ChestBundleData)
#   H3 Loot box / Offer (remote:starting_loot_box_count, chest_offer)
#   H4 IAP / Quang cao (lootio.* product ids + ad SDK)
# Anh: DecodedData/image_urls_gh.json (upload boi upload_images_gh.py) + image_urls_bc.json (weapon icon).
# Chay: python tools/write_group_gh.py   (print ASCII vi console cp1252)
import json, os
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")

IMGS = json.load(open(os.path.join(ROOT, "DecodedData", "image_urls_gh.json"), encoding="utf-8"))
IMGS_BC = json.load(open(os.path.join(ROOT, "DecodedData", "image_urls_bc.json"), encoding="utf-8"))

REWARD_VN = {0: "Xương (Bone)", 1: "Ngọc (Gem)", 2: "Chìa Dragon Boss", 3: "Chìa Zombie Horde",
             4: "Rương Loot Box", 5: "Kinh nghiệm (EXP)", 6: "Cuốc Vàng (Golden Pickaxe)",
             7: "Cuốc (Pickaxe)", 8: "Máy khoan (Drill)", 9: "Tiền Áo choàng (Cloak)",
             10: "Chìa Cultist", 11: "Lọ (Vial)"}
RARITY_VN = {0: "Common", 1: "Uncommon", 2: "Rare", 3: "Epic", 4: "Legendary",
             5: "Mythic", 6: "Ancient", 7: "Immortal", 8: "Divine", 9: "Eternal"}


def load(name):
    return json.load(open(os.path.join(TABLES, name + ".json"), encoding="utf-8"))


def img(key, h=48):
    m = IMGS.get(key) or IMGS_BC.get(key)
    if not m:
        return ""
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def safe(s):
    # tranh bay Sheets: cell text bat dau bang '+' hoac '=' voi USER_ENTERED bi coi la cong thuc
    if isinstance(s, str) and s[:1] in ("+", "="):
        return "'" + s
    return s


def reward(r):
    """{'RewardType','BaseAmount','Multiplier'} -> chuoi VN."""
    amt = r["BaseAmount"] * (r.get("Multiplier") or 1)
    amt = int(amt) if amt == int(amt) else amt
    return "%s %s" % (amt, REWARD_VN.get(r["RewardType"], "?%d" % r["RewardType"]))


# ---------------------------------------------------------------- G1 Mastery
MASTERY_ICON = {"AutoLootHammerCount": "m_autoloot", "GemOfferChance": "m_gemoffer",
                "AdBoostDuration": "m_boostdur", "AdBoostWorth": "m_boostworth",
                "MaxOfflineTime": "m_offlinetime", "OfflineEarningWorth": "m_offlineworth",
                "PlayerMovementSpeed": "m_movespeed", "CompanionSummonCount": "m_companion",
                "ForgeMaxItemLevel": "m_maxitem", "MiningMaxPickaxe": "m_pickaxe"}


def fmt_val(u, v):
    p, s = u.get("ValuePrefix"), u.get("ValueSuffix")
    v = int(v) if isinstance(v, float) and v == int(v) else v
    out = str(v)
    if p == "x":
        out = "x%s" % v
    elif p == "%":
        out = "%s%%" % v
    elif p == "+":
        out = "+%s" % v
    if s and s.strip() and not s.strip().startswith("<sprite"):
        out += s.strip()
    return out


def tab_mastery():
    ups = load("MasteryUpgradeData")
    cfg = load("MasteryConfig")[0]
    ups.sort(key=lambda u: int(u["_name"].split("-")[0]))
    swatches = []
    g = [["G1. MASTERY — Cây nâng cấp vĩnh viễn bằng Ngọc (MasteryUpgradeData + MasteryConfig)"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/MasteryConfig.asset + 10 file *Mastery*Data · "
          "MasteryUpgradeData.cs · icon crop từ atlas _AtlasAllUI_1"],
         ["Mastery là hệ nâng cấp META vĩnh viễn (không mất khi chơi lại): tiêu Ngọc (Gem) để mở & lên "
          "cấp 10 nhánh buff toàn cục. Mỗi nhánh phải TRẢ PHÍ MỞ KHOÁ (UnlockGemCost) một lần rồi mới "
          "lên cấp; mỗi cấp có giá Ngọc riêng (GemCost) tăng dần và một Giá trị (Value) tương ứng. "
          "MasteryConfig.Enabled=%d; 2 nhánh cuối (Max Item Level, Max Pickaxe) là AddedLater=1 (bổ sung "
          "sau bản gốc). Trạng thái mở khoá lưu server, số Ngọc đã tiêu không hoàn." % cfg["Enabled"]],
         [],
         ["1. TỔNG QUAN 10 NHÁNH (sắp theo thứ tự UpgradeType)"],
         ["Icon", "Nhánh (UpgradeName)", "Hệ thống ảnh hưởng", "Phí mở khoá (Gem)", "Mặc định",
          "Số cấp", "Giá trị tối đa", "Tổng Gem để MAX", "Mô tả"]]
    for u in ups:
        levels = u["Levels"]
        total = sum(l["GemCost"] for l in levels) + u["UnlockGemCost"]
        maxv = fmt_val(u, levels[-1]["Value"])
        g.append([img(MASTERY_ICON.get(u["UpgradeType__enum"], ""), 40), u["UpgradeName"],
                  u["UpgradeType__enum"], u["UnlockGemCost"], safe(fmt_val(u, u["DefaultValue"])),
                  len(levels), safe(maxv), total, u["Description"]])
    hdr_over = [
        ("AutoLootHammerCount", "Auto Mode mở nhiều rương loot cùng lúc hơn — tăng tốc cày."),
        ("GemOfferChance", "Tăng % ra Ngọc khi mở Offer Chest — vòng lặp tự cấp Ngọc."),
        ("AdBoostDuration", "Kéo dài thời gian hiệu lực EXP Boost (xem quảng cáo)."),
        ("AdBoostWorth", "Tăng hệ số nhân EXP khi bật Boost."),
        ("MaxOfflineTime", "Tăng số giờ tối đa vẫn tích thưởng khi offline."),
        ("OfflineEarningWorth", "Tăng tốc độ tích tài nguyên khi offline."),
        ("PlayerMovementSpeed", "Tăng tốc chạy nhân vật trong dungeon."),
        ("CompanionSummonCount", "Tăng số companion xuất hiện mỗi lần triệu hồi."),
        ("ForgeMaxItemLevel", "Nâng trần cấp tối đa của trang bị khi Forge."),
        ("MiningMaxPickaxe", "Nâng trần số cuốc (Pickaxe) — hệ Đào mỏ."),
    ]
    g += [[],
          ["2. THANG CẤP CHI TIẾT TỪNG NHÁNH (Gem mỗi cấp → Giá trị đạt được)"]]
    for u in ups:
        levels = u["Levels"]
        g.append(["%s — mở khoá %d Gem · mặc định %s · %s" % (
            u["UpgradeName"], u["UnlockGemCost"], fmt_val(u, u["DefaultValue"]),
            "cộng dồn (additive)" if u["IsValueAdditive"] else "thay thế (set)")])
        g.append(["Cấp"] + [str(i + 1) for i in range(len(levels))])
        g.append(["Gem"] + [l["GemCost"] for l in levels])
        g.append(["Giá trị"] + [safe(fmt_val(u, l["Value"])) for l in levels])
        g.append([])
    g += [["Nhận xét"],
          ["• Mastery là 'gem sink' chính ngoài rương: 10 nhánh, tổng để MAX HẾT ≈ %s Ngọc (chưa kể "
           "phí mở khoá) — mục tiêu tiêu Ngọc dài hạn cho người chơi lâu năm." %
           format(sum(sum(l["GemCost"] for l in u["Levels"]) + u["UnlockGemCost"] for u in ups), ",")],
          ["• Chia 2 nhóm: (a) buff cày/kinh tế — AutoLoot, Offline Time/Rate, EXP Boost, Gem Offer → "
           "tăng tốc vòng lặp idle; (b) buff sức mạnh trực tiếp — Move Speed, Companion Summon, Max Item "
           "Level, Max Pickaxe. Nhánh kinh tế mở giá rẻ (10–30 Gem) để móc nối sớm."],
          ["• Đường cong giá Ngọc mỗi cấp tăng phi tuyến (vd EXP Boost Worth: 10→1100 Gem cho 20 cấp) "
           "trong khi giá trị tăng tuyến tính (x2.0→x4.0) → cấp cuối 'đắt xắt ra miếng', ép người chơi "
           "cân nhắc/rút ví."],
          ["• GemOfferChance là nhánh tự nuôi: nâng nó → mở offer chest ra nhiều Ngọc hơn → có Ngọc nâng "
           "tiếp; mặc định 10%%, MAX 50%%."],
          ["• 2 nhánh AddedLater (Max Item Level, Max Pickaxe) nối trực tiếp hệ Forge (C9) và Mining (E1) "
           "— thêm trần tiến trình cho end-game, chứng tỏ mastery được mở rộng theo bản cập nhật."],
          ["• Icon nhánh khớp chủ đề (đồng hồ=offline, tia sét=boost, dấu chân=companion, giày có cánh="
           "tốc độ...). MAPPING icon↔nhánh là SUY ĐOÁN theo hình vì AssetRipper không export .meta để tra "
           "guid; số liệu Gem/Value thì chính xác 100%% từ config."]]
    return "G1. Mastery", g, 2, swatches


# ---------------------------------------------------------------- G2 Battle Pass
def tab_battlepass():
    c = load("BattlePassConfig")[0]
    rows = c["LevelRewards"]
    g = [["G2. BATTLE PASS — Chuỗi thưởng theo Level (BattlePassConfig)"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/BattlePassConfig.asset · BattlePassLevelRewardConfig.cs, "
          "BattlePassRewardModel.cs · IAP lootio.battlepass · icon reward crop atlas"],
         ["Battle Pass gắn với LEVEL nhân vật (không phải mùa thời gian): bắt đầu từ level %d "
          "(StartLevel), cứ mỗi %d level (RewardInterval) mở 1 mốc thưởng. Mỗi mốc có 2 phần: hàng FREE "
          "(ai cũng nhận) và hàng PREMIUM (phải mua Pass — lootio.battlepass). Mỗi hàng gồm 2 ô thưởng. "
          "Bảng mẫu dưới có %d mốc lặp (pool), PoolSize=%d, FutureRewardCount=%d (số mốc hiện trước cho "
          "người chơi thấy đích)." % (c["StartLevel"], c["RewardInterval"], len(rows),
                                      c["PoolSize"], c["FutureRewardCount"])],
         [img("reward", 70)],
         [],
         ["1. THAM SỐ", "Giá trị", "Ý nghĩa"],
         ["StartLevel", c["StartLevel"], "Level bắt đầu nhận thưởng Pass"],
         ["RewardInterval", c["RewardInterval"], "Cứ mỗi 5 level = 1 mốc thưởng"],
         ["PoolSize", c["PoolSize"], "Tổng số mốc trong 1 vòng pool"],
         ["FutureRewardCount", c["FutureRewardCount"], "Số mốc hiển thị trước (xem đích)"],
         [],
         ["2. BẢNG THƯỞNG MẪU (mỗi mốc: 2 ô FREE + 2 ô PREMIUM)"],
         ["Mốc #", "Mở tại Level", "FREE 1", "FREE 2", "PREMIUM 1", "PREMIUM 2"]]
    for i, r in enumerate(rows):
        g.append([i + 1, r["UnlockLevel"] if r["UnlockLevel"] else "—",
                  reward(r["FreeReward1"]), reward(r["FreeReward2"]),
                  reward(r["PremiumReward1"]), reward(r["PremiumReward2"])])
    g += [[],
          ["Nhận xét"],
          ["• Battle Pass ở đây là 'pass theo tiến trình level' chứ không phải pass theo mùa/thời gian: "
           "càng lên level càng nhận, không sợ hết hạn — hợp với game idle/progression."],
          ["• Hàng FREE thiên về tài nguyên cày (EXP, Loot Box, Bone, cuốc/khoan Mining, Gem nhỏ); hàng "
           "PREMIUM cùng loại nhưng SỐ LƯỢNG LỚN hơn hẳn (vd Loot Box 25 free vs 100 premium; chìa dungeon "
           "2–3 cái) → mua Pass = nhân đôi/nhân ba lượng thưởng cùng chặng."],
          ["• Ô FREE 2 gần như luôn cố định = 25 Loot Box (RewardType 4) — 'xương sống' phần thưởng miễn "
           "phí đều đặn mỗi mốc, kéo người chơi tiếp tục lên level."],
          ["• Thưởng bơm chéo nhiều hệ: Chìa Dragon/Zombie (Boss modes B), Cuốc/Khoan (Mining E), Vial & "
           "Cloak Currency (Cape C7) — Pass là 'kênh phân phối' tài nguyên cho mọi tính năng."],
          ["• Mốc cuối bảng mở tại Level 120 (RewardType 10 = Chìa Cultist + Vial) — nội dung end-game; "
           "các mốc còn lại UnlockLevel=0 nghĩa là lặp theo pool tính từ StartLevel."],
          ["• BaseAmount × Multiplier: hiện Multiplier toàn = 1, nhưng có sẵn field → server/sự kiện có thể "
           "nhân đôi thưởng Pass (vd Pass x2) mà không đổi bảng gốc."]]
    return "G2. Battle Pass", g, 1, []


# ---------------------------------------------------------------- G3 Weapon Offer
def tab_weapon_offer():
    rc = json.load(open(os.path.join(ROOT, "DecodedData", "remote_config_live.json"), encoding="utf-8"))
    weapons = {w["ItemId"]: w for w in load("WeaponData")}
    offers = [tuple(map(int, seg.split(","))) for seg in rc["weapon_offer_config"].split("|")]
    # _name -> key trong image_urls_bc (chinh la _name)
    g = [["G3. WEAPON OFFER — Gói vũ khí mời chào theo Level (remote:weapon_offer_config)"],
         ["Nguồn: Firebase Remote Config LIVE (config_version 17.0.0, app-cache) khoá weapon_offer_config · "
          "khớp ItemId với WeaponData.asset · icon crop từ atlas item theo rarity"],
         ["Khi người chơi ĐẠT mốc level định sẵn, game bật một OFFER mua vũ khí cụ thể (thường kèm IAP/"
          "quảng cáo). Cấu hình là chuỗi 'level,ItemId' phân tách bằng '|'. Đây là bản LIVE lấy từ Remote "
          "Config nên hãng có thể đổi vũ khí/level bất cứ lúc nào KHÔNG cần update app. 5 mốc hiện tại:"],
         [],
         ["Level mở", "Icon", "Vũ khí", "ItemId", "Loại", "Rarity", "Ghi chú"]]
    note = {2: "Mốc đầu — câu người mới bằng vũ khí Epic vượt cấp",
            10: "Nâng cấp giữa chặng đầu", 20: "Mốc Ancient — nhảy vọt sức mạnh",
            50: "Vũ khí Immortal cận end-game", 100: "Đỉnh — Divine, chốt hạ người chơi lâu năm"}
    for lv, iid in offers:
        w = weapons.get(iid, {})
        kind = "Cận chiến (Melee)" if w.get("_name", "").startswith("Weapon_m") else "Tầm xa (Ranged)"
        g.append([lv, img(w.get("_name", ""), 46), w.get("ItemName", "?"), iid, kind,
                  "%s (%s)" % (RARITY_VN.get(w.get("Rarity"), "?"), w.get("Rarity")),
                  note.get(lv, "")])
    g += [[],
          ["Nhận xét"],
          ["• Offer bám mốc level tâm lý (2, 10, 20, 50, 100): mốc 2 đưa ngay vũ khí Epic (Squire Crossbow) "
           "— vượt xa đồ nhặt lúc mới chơi → tạo cảm giác 'hời' để tập thói quen mua."],
          ["• Rarity offer leo theo level đúng đường cong sức mạnh: Epic(lv2) → Mythic(lv10) → Ancient(lv20) "
           "→ Immortal(lv50) → Divine(lv100). Luôn 'trên tầm' đồ tự cày ở mốc đó một bậc."],
          ["• Trộn Melee/Ranged (Crossbow, Dagger, Bow, Scythe, Greatsword) để phủ mọi kiểu build, ai cũng "
           "thấy có món hợp."],
          ["• Vì là Remote Config, đây là 'đòn bẩy doanh thu' tinh chỉnh liên tục: A/B test vũ khí nào bán "
           "chạy ở mốc nào mà không phát hành bản mới."]]
    return "G3. Weapon Offer", g, 1, []


# ---------------------------------------------------------------- H1 Chest
def tab_chest():
    chests = load("ChestData")
    chests.sort(key=lambda c: c["GemCost"])
    icon = {"standard": "chest_standard", "epic": "chest_epic",
            "legendary": "chest_legendary", "mythic": "chest_mythic"}
    g = [["H1. RƯƠNG (Gacha trang bị) — ChestData + ChestConfig"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/Chest_*.asset, ChestConfig.asset · ChestStoreCard.cs, "
          "ChestRewardElementUI.cs · icon PNG rời trong Texture2D"],
         ["Rương là gacha trang bị chính: tiêu Ngọc mở, nhận ItemCount món ngẫu nhiên theo bảng tỉ lệ "
          "rarity (RarityOdds, tính theo Weight). Rương xịn hơn = nhiều Ngọc + tỉ lệ rớt đồ hiếm cao + có "
          "cơ chế PITY (đảm bảo sau N lần). Mythic yêu cầu đã hoàn thành bộ sưu tập Epic mới mở được "
          "(RequireRarityCompletion)."],
         [],
         ["1. TỔNG QUAN 4 LOẠI RƯƠNG"],
         ["Icon", "Rương", "ChestId", "Giá (Gem)", "Số món", "Pity", "Điều kiện mở", "Rarity cấp rương"]]
    for c in chests:
        cond = "Cần hoàn thành bộ %s" % c["RequiredCompletedRarity__enum"] if c["RequireRarityCompletion"] else "Mở tự do"
        g.append([img(icon.get(c["ChestId"], ""), 44), c["ChestName"], c["ChestId"], c["GemCost"],
                  c["ItemCount"], c["PityCount"] if c["PityCount"] else "—", cond, c["Rarity__enum"]])
    g += [[],
          ["2. BẢNG TỈ LỆ RỚT (RarityOdds — % theo Weight, mỗi rương tổng ≈100%)"],
          ["Rương \\ Rarity"] + [RARITY_VN[i] for i in range(6)]]
    for c in chests:
        odds = {o["Rarity"]: o["Weight"] for o in c["RarityOdds"]}
        g.append([c["ChestName"]] + [("%s%%" % odds[i]) if i in odds else "—" for i in range(6)])
    g += [[],
          ["3. THỨ TỰ & CẤU HÌNH (ChestConfig)"],
          ["Trường", "Nội dung"],
          ["Chests[]", "4 rương xếp thứ tự hiển thị store: Standard → Epic → Legendary → Mythic"],
          ["Bundles[]", "4 bundle rương (xem tab H2)"],
          ["LegendaryFeaturedBundle / MythicFeaturedBundle", "2 bundle được 'nổi bật' trong store"],
          [],
          ["Nhận xét"],
          ["• Bậc thang rõ ràng: Standard 40 Gem (2 món, không pity) là 'phễu' rẻ; Epic 80 / Legendary 120 "
           "/ Mythic 200 đều 6 món + pity 20 → càng lên càng đáng mở gói."],
          ["• Pity 20: tối đa 20 lần mở không trúng bậc cao nhất thì lần sau ép trúng — chống 'nhọ' kéo "
           "dài, giữ người chơi không bỏ cuộc; Standard không có pity (rương rác)."],
          ["• Tỉ lệ đỉnh cực thấp: Mythic chest chỉ 0.33%% ra đồ Mythic, 0.75%% Legendary — đồ top hiếm "
           "thật, đẩy nhu cầu mở nhiều/mua Ngọc; nhưng vẫn có sàn nhờ pity."],
          ["• Gate tiến trình: Mythic Chest KHÓA đến khi hoàn thành bộ Epic (RequireRarityCompletion=1) — "
           "buộc người chơi 'tốt nghiệp' cấp đồ thấp trước, kéo dài lộ trình."],
          ["• Đồ chơi tỉ lệ dịch dần: từ Epic→Legendary→Mythic, trọng số Common tụt (75→62.5→45.9) còn "
           "Uncommon/Rare tăng → rương cao 'nâng sàn' chất lượng chứ không chỉ thêm nóc."]]
    return "H1. Rương", g, 1, []


# ---------------------------------------------------------------- H2 Chest Bundle
def tab_bundle():
    bundles = load("ChestBundleData")
    # guid chest -> ten (tu ChestData ChestConfig thu tu; suy doan theo mau chest icon)
    order = {"lootio.chestbundle_1": 1, "lootio.chestbundle_2": 2,
             "lootio.chestbundle_legendary": 3, "lootio.chestbundle_mythic": 4}
    bundles.sort(key=lambda b: order.get(b["BundleId"], 9))
    icon = {"Chest_Bundle_1": "chest_legendary", "Chest_Bundle_2": "chest_mythic",
            "Chest Bundle_Legendary": "chest_legendary", "Chest Bundle_Mythic": "chest_mythic"}
    g = [["H2. BUNDLE RƯƠNG (Gói gacha trả tiền) — ChestBundleData + ChestConfig"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/Chest*Bundle*.asset, ChestConfig.asset · IAP "
          "lootio.chestbundle_* · icon PNG rời"],
         ["Bundle là gói MUA (StoreId lootio.chestbundle_*) gộp nhiều rương + Ngọc kèm theo (GemAmount), "
          "bán ngoài IAP. Store hiển thị 4 bundle; 2 bundle Legendary/Mythic được đặt 'featured' "
          "(LegendaryFeaturedBundle / MythicFeaturedBundle trong ChestConfig)."],
         [],
         ["Icon", "Bundle", "StoreId (IAP)", "Gem kèm", "Nội dung rương (Count × loại)", "Số sprite khoe"]]
    for b in bundles:
        chests = ", ".join("%d rương" % ch["Count"] for ch in b["Chests"])
        g.append([img(icon.get(b["_name"], "chest_legendary"), 44), b["_name"], b["StoreId"],
                  b["GemAmount"], chests, len(b.get("ShowcaseSprites", []))])
    g += [[],
          ["Nhận xét"],
          ["• Hai tầng bundle: gói nhỏ 'lookup' (Chest_Bundle_1: 500 Gem + 2 rương; Chest_Bundle_2: 400 "
           "Gem + 2 rương) và gói lớn 'whale' (Legendary: 2500 Gem + 30 rương; Mythic: 3000 Gem + 30 "
           "rương) — phủ cả người mua nhẹ lẫn người chi mạnh."],
          ["• Bundle lớn nhồi 20+10 rương hai loại → vừa cho Ngọc mua thêm, vừa cho rương mở luôn: 'combo' "
           "tối ưu cảm giác nhiều đồ trong 1 lần mua."],
          ["• 2 bundle cao được 'featured' cố định trong ChestConfig → luôn nổi bật ở store, là mũi nhọn "
           "doanh thu; bundle nhỏ đóng vai mồi giá thấp."],
          ["• Lưu ý: guid rương bên trong Chests[] không tra được tên (thiếu .meta) nên bảng chỉ chắc chắn "
           "SỐ LƯỢNG rương mỗi gói; loại rương suy theo tên bundle (Legendary/Mythic)."]]
    return "H2. Bundle Rương", g, 1, []


# ---------------------------------------------------------------- H3 Loot box / Offer
def tab_lootoffer():
    rc = json.load(open(os.path.join(ROOT, "DecodedData", "remote_config_live.json"), encoding="utf-8"))
    g = [["H3. LOOT BOX & OFFER CHEST — Rương khởi đầu + rương mời chào (Remote Config + chest_offer)"],
         ["Nguồn: Firebase Remote Config LIVE khoá starting_loot_box_count, chest_store_enabled · sprite "
          "chest_offer_01/02, chest_icon · AdvertisementController.cs"],
         ["Đây là hệ rương 'nhẹ' ngoài gacha trang bị H1: Loot Box là rương thường rơi liên tục khi chơi "
          "(mở bằng búa/hammer trong Auto Mode — nối Mastery AutoLoot G1); Offer Chest là rương ưu đãi "
          "bật lên định kỳ, thường mở bằng xem QUẢNG CÁO hoặc Ngọc, có thể rớt Gem (nối Mastery "
          "GemOfferChance G1)."],
         [],
         ["1. THAM SỐ REMOTE CONFIG (LIVE, config_version %s)" % rc["config_version"]],
         ["Khoá", "Giá trị", "Ý nghĩa"],
         ["starting_loot_box_count", rc["starting_loot_box_count"],
          "Số Loot Box người chơi mới có sẵn từ đầu (240) — 'kho mồi' để tập mở rương ngay"],
         ["chest_store_enabled", rc["chest_store_enabled"],
          "Bật cửa hàng rương (H1/H2) — công tắc từ xa, có thể tắt bán theo vùng/sự kiện"],
         [],
         ["2. CÁC LOẠI 'RƯƠNG NHẸ'"],
         ["Icon", "Loại", "Cách mở", "Vai trò"],
         [img("chest_offer_01", 46), "Offer Chest #1", "Xem quảng cáo / Ngọc (offer định kỳ)",
          "Rớt Ngọc + tài nguyên; % Ngọc tăng theo Mastery GemOfferChance"],
         [img("chest_offer_02", 46), "Offer Chest #2", "Xem quảng cáo / Ngọc", "Biến thể offer khác"],
         [img("reward", 46), "Loot Box thường", "Auto Mode (búa) / bấm mở",
          "Rơi liên tục khi cày; số mở cùng lúc theo Mastery AutoLoot"],
         [],
         ["Nhận xét"],
         ["• 240 Loot Box khởi đầu = 'onboarding hào phóng': người mới mở rương thoả thích ngay, hiểu "
          "vòng lặp loot → hình thành thói quen trước khi hết."],
         ["• Offer Chest là nơi 'ads gặp gacha': đổi lượt xem quảng cáo lấy Ngọc/đồ — nguồn thu ads đều "
          "đặn, đồng thời là lối cấp Ngọc miễn phí giữ người chơi F2P."],
         ["• Hai công tắc Remote (starting_loot_box_count, chest_store_enabled) cho phép LiveOps chỉnh độ "
          "hào phóng & bật/tắt bán mà không update app — điển hình game vận hành theo dữ liệu."],
         ["• Cả hệ này bám chặt Mastery (G1): AutoLoot tăng tốc mở Loot Box, GemOfferChance tăng Ngọc từ "
          "Offer Chest — nâng cấp meta khiến hệ rương nhẹ sinh lợi hơn, khuyến khích tiêu Ngọc vào Mastery."]]
    return "H3. Loot Box & Offer", g, 1, []


# ---------------------------------------------------------------- H4 IAP / Ads
def tab_iap_ads():
    g = [["H4. IAP & QUẢNG CÁO — Danh mục mua trong app + mạng quảng cáo (product id + SDK)"],
         ["Nguồn: chuỗi StoreId 'lootio.*' trong Assembly-CSharp · AdvertisementController.cs, "
          "AnalyticsController.cs, AdsPage.cs · SDK mediation (native libs / manifest)"],
         ["Doanh thu game gồm 2 chân: IAP (mua Ngọc & gói ưu đãi) và Quảng cáo (rewarded/interstitial/"
          "banner qua mediation). Bảng dưới là danh mục product id tìm thấy trong code; GIÁ thực do store "
          "(Google Play/App Store) & server quyết định — id offer_NNN gợi mức giá USD tương ứng."],
         [],
         ["1. GÓI NGỌC (Gem packs — lootio.gem1..6)"],
         ["Product Id", "Suy đoán"],
         ["lootio.gem1 … lootio.gem6", "6 bậc gói Ngọc từ nhỏ → lớn (càng lớn càng nhiều Ngọc/USD)"],
         [],
         ["2. GÓI ƯU ĐÃI THEO MỨC GIÁ (offer_NNN — số = giá USD)"],
         ["Product Id", "Mức giá gợi ý (USD)"],
         ["lootio.offer_099", "$0.99"], ["lootio.offer_199", "$1.99"], ["lootio.offer_299", "$2.99"],
         ["lootio.offer_499", "$4.99"], ["lootio.offer_999", "$9.99"], ["lootio.offer_1499", "$14.99"],
         ["lootio.offer_1999", "$19.99"], ["lootio.offer_2999", "$29.99"], ["lootio.offer_4999", "$49.99"],
         ["lootio.offer_9999", "$99.99"],
         [],
         ["3. GÓI ĐẶC BIỆT & TIỆN ÍCH"],
         ["Product Id", "Nội dung suy đoán"],
         ["lootio.starterpack", "Gói người mới (starter) — giá trị cao, mua 1 lần"],
         ["lootio.epicpack / lootio.resourcepack", "Gói đồ Epic / gói tài nguyên"],
         ["lootio.deal_tiny / _small / _medium / _large", "4 bậc deal ưu đãi định kỳ"],
         ["lootio.battlepass", "Mua Battle Pass premium (tab G2)"],
         ["lootio.chestbundle_legendary / _mythic", "Bundle rương (tab H2)"],
         ["lootio.dungeonkeys", "Gói chìa dungeon (Boss modes B)"],
         ["lootio.mining_offer / _large", "Gói ưu đãi Đào mỏ (Mining E1)"],
         ["lootio.lifetimeboost", "Boost vĩnh viễn (mua 1 lần, buff mãi mãi)"],
         ["lootio.noads", "Gỡ quảng cáo (Remove Ads) — vẫn giữ được nút xem ads tự nguyện lấy thưởng"],
         [],
         ["4. QUẢNG CÁO (Advertisement)"],
         ["Hạng mục", "Chi tiết"],
         ["Định dạng", "Banner + Interstitial + Rewarded (thưởng khi xem) — thấy trong AdvertisementController"],
         ["Mediation", "AppLovin MAX làm trung gian, đấu giá các mạng: AdMob (Google), Meta/Facebook, "
          "Unity Ads, Vungle, Mintegral, Pangle (theo phân tích trước)"],
         ["Attribution", "Adjust (đo lường nguồn cài đặt / chiến dịch)"],
         ["Điểm đặt Rewarded", "Double reward (x2 thưởng), tặng Gem, thêm vé PvP, thêm cuốc/khoan Mining, "
          "kéo dài EXP Boost — 'xem 1 ad đổi lợi ích' rải khắp game"],
         [],
         ["Nhận xét"],
         ["• Thang giá IAP dày đặc từ $0.99 → $99.99 (10 bậc offer) + 6 gói Gem → phủ mọi kiểu chi tiêu, "
          "tối ưu hoá theo từng nhóm người chơi (minnow → whale)."],
         ["• Chiến lược 'ads là tài nguyên miễn phí': hầu hết hệ (rương, PvP, Mining, boost) đều có nút "
          "rewarded ad — vừa cho F2P đường tiến, vừa là nguồn thu ads ổn định; noads chỉ gỡ ad ép buộc, "
          "KHÔNG chặn ads tự nguyện."],
         ["• Mediation AppLovin MAX + 6 mạng = tối đa hoá eCPM bằng đấu giá real-time; Adjust để đo ROAS "
          "từng chiến dịch UA — bộ khung LiveOps/marketing chuẩn game hyper-casual/mid-core."],
         ["• LƯU Ý ĐỘ TIN: product id là CHẮC CHẮN (trích từ code), nhưng GIÁ USD và nội dung mỗi gói là "
          "SUY ĐOÁN từ tên id — số liệu chốt phải lấy từ cấu hình store/server, không có trong APK. "
          "Danh sách mạng ads lấy từ ghi chú phân tích native trước đó."]]
    return "H4. IAP & Quảng cáo", g, 1, []


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
    for title, grid, hdr, swatches in tabs:
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
                    "properties": {"pixelSize": max(hs) + 14}, "fields": "pixelSize"}})
        svc.batchUpdate(spreadsheetId=TARGET, body={"requests": reqs}).execute()
        print("wrote tab:", title.encode("ascii", "replace").decode(), "|", len(grid), "rows,",
              len(img_rows), "image rows")


def main():
    tabs = [tab_mastery(), tab_battlepass(), tab_weapon_offer(),
            tab_chest(), tab_bundle(), tab_lootoffer(), tab_iap_ads()]
    # index 24 = ...F3(23) + 1
    write_tabs(tabs, 24)
    print("images GH:", len(IMGS), "| BC reuse:", "ok")


if __name__ == "__main__":
    main()
