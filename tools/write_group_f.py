#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Ghi NHOM F "PvP & xa hoi" vao Google Sheet DungeonRush - GDD (1 tinh nang = 1 tab).
# F1 PvP (PvPConfig + DTO), F2 Clan banner (ClanBannerCatalog), F3 Clan/ClanWar/Chat (server DTO).
# Anh: DecodedData/image_urls_f.json (upload boi tools/upload_images_f.py).
# Swatch mau: cell chua ma hex, script to nen cell bang dung mau do (repeatCell).
# Chay: python tools/write_group_f.py   (print ASCII vi console cp1252)
import json, os
from google.oauth2.credentials import Credentials
from googleapiclient.discovery import build

CRED = r"C:\Users\admin\.config\google-sheets\credentials.json"
TOK = r"C:\Users\admin\.config\google-sheets\token_sheets.json"
TARGET = "10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg"

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TABLES = os.path.join(ROOT, "DecodedData", "tables")
IMG_JSON = os.path.join(ROOT, "DecodedData", "image_urls_f.json")

IMGS = json.load(open(IMG_JSON, encoding="utf-8")) if os.path.exists(IMG_JSON) else {}

EMBLEMS = [("axe", "Rìu"), ("book", "Sách"), ("bow", "Cung"), ("castle", "Lâu đài"),
           ("crown", "Vương miện"), ("dragon", "Rồng"), ("helmet", "Mũ giáp"),
           ("horse", "Ngựa"), ("jester_hat", "Mũ hề"), ("knight", "Hiệp sĩ"),
           ("lion", "Sư tử"), ("mace", "Chùy"), ("scroll", "Cuộn giấy"),
           ("shield", "Khiên"), ("swords", "Kiếm chéo"), ("symbol", "Biểu tượng"),
           ("torch", "Đuốc"), ("trophy", "Cúp"), ("viking_hat", "Mũ viking"),
           ("wizard_hat", "Mũ phù thủy")]


def img(key, h=48):
    m = IMGS.get(key)
    if not m:
        return ""
    w = round(m["w"] * h / m["h"])
    url = "https://drive.google.com/thumbnail?id=%s&sz=w%d" % (m["id"], max(m["w"], 400))
    return '=IMAGE("%s",4,%d,%d)' % (url, h, w)


def hexc(c):
    return "#%02X%02X%02X" % tuple(round(c[k] * 255) for k in "rgb")


def tab_pvp():
    """F1. PvP Arena — PvPConfig + DTO + localization."""
    cfg = json.load(open(os.path.join(TABLES, "PvPConfig.json"), encoding="utf-8"))[0]
    tick, leagues = cfg["TicketConfig"], cfg["Leagues"]
    swatches = []
    g = [["F1. PvP / ĐẤU TRƯỜNG (PvP Arena) — PvPConfig (10 league, trophy kiểu Elo, vé)"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/PvPConfig.asset · PvP*DTO.cs, PvPPlayerModel.cs "
          "· localization strings_en (Popup.PvP.*, Errors.PvP.*)"],
         ["PvP BẤT ĐỒNG BỘ (async): không đánh trực tiếp người thật mà đánh BẢN SAO (snapshot) hồ sơ "
          "đối thủ — server trả về đầy đủ trang bị, companion, cánh, cape (kèm substats), enchantment "
          "của đối thủ để client mô phỏng trận đấu. Người chơi tốn 1 vé/trận, được chọn 1 trong danh "
          "sách đối thủ (roster) do server tìm, thấy trước điểm cộng/trừ dự kiến (ProjectedWin/LossTrophy). "
          "Kết quả nộp về server bằng battleToken (chống gian lận), server tính trophy và trả thưởng. "
          "Nội dung version hoá: ContentVersion %s." % cfg["ContentVersion"]],
         [img("pvp_illustration", 90)],
         [],
         ["1. VÉ ĐẤU (TicketConfig)"],
         ["Tham số", "Giá trị", "Ghi chú"],
         ["Vé miễn phí / ngày (DailyFreeTickets)", tick["DailyFreeTickets"],
          "Reset theo ngày (dayKey); UI: 'Tickets refreshed in: {0}'"],
         ["Vé xem quảng cáo / ngày (MaxRewardedAdTicketsPerDay)", tick["MaxRewardedAdTicketsPerDay"],
          "Mỗi ad +1 vé (PvPAdTicketRequest)"],
         ["Tổng tối đa / ngày", tick["DailyFreeTickets"] + tick["MaxRewardedAdTicketsPerDay"],
          "5 free + 4 ad = 9 trận/ngày"],
         [],
         ["2. MATCHMAKING & SNAPSHOT (Matchmaking + DTO)"],
         ["Cơ chế", "Chi tiết"],
         ["Upload snapshot", "Client upload hồ sơ (trang bị + chỉ số) lên server tối thiểu mỗi %d giây "
          "(= %d phút) một lần (SnapshotUploadMinIntervalSeconds) — bản sao của bạn mà người khác gặp "
          "có thể cũ tới chừng đó." % (cfg["Matchmaking"]["SnapshotUploadMinIntervalSeconds"],
                                       cfg["Matchmaking"]["SnapshotUploadMinIntervalSeconds"] // 60)],
         ["Roster đối thủ", "PvPFindOpponents trả danh sách đối thủ + rosterToken có hạn "
          "(rosterExpiresAt); hết hạn phải tìm lại ('Opponent roster expired'). UI: 'Choose Opponent'."],
         ["Thông tin đối thủ thấy trước", "Tên, quốc gia (CountryCode), avatar, Power, Trophy, "
          "trophy dự kiến thắng/thua (ProjectedWinTrophy / ProjectedLossTrophy) — chọn kèo trước khi đánh."],
         ["Snapshot chứa", "Items (id, level, substats), Companions, Wing, CapeId + CapeLevel + "
          "CapeSubStats, EnchantmentTiers, ShowCloak — đủ dựng lại build đối thủ trong trận."],
         ["Chống gian lận", "startBattle cấp battleToken có hạn (battleTokenExpiresAt); báo kết quả phải "
          "kèm token; forfeit (bỏ trận) tính thua; kết quả pending được lưu và nộp lại khi có mạng."],
         ["Leaderboard", "PvPLeaderboard theo trophy (Popup.PvP.Leaderboard.Title); Position trong model."],
         [],
         ["3. MƯỜI LEAGUE (Leagues) — thăng/giáng theo mốc trophy, K-factor kiểu Elo"],
         ["#", "League", "Màu", "Trophy từ", "Trophy đến", "Trophy khi thắng (KWin)",
          "Trophy khi thua (KLoss)", "WeeklyDecay"]]
    hdr_row = len(g) - 1
    for lg in leagues:
        row = len(g)
        swatches.append((row, 2, lg["LeagueColor"]))
        mx = lg["MaxTrophy"]
        g.append([lg["LeagueIndex"], lg["LeagueName"], hexc(lg["LeagueColor"]),
                  lg["MinTrophy"], "∞" if mx > 10**9 else mx,
                  lg["KWin"], lg["KLoss"], lg["WeeklyDecay"]])
    g += [["(League không có badge/icon riêng trong asset — UI phân biệt bằng TÊN + MÀU (LeagueColor). "
           "Trận đấu xếp league theo trophy hiện tại; startLeagueIndex ghi lại league lúc bắt đầu trận.)"],
          [],
          ["4. PHẦN THƯỞNG & FLOW SERVER (DTO — số liệu cụ thể do server trả)"],
          ["Bước", "Request/Response", "Nội dung"],
          ["Mở PvP", "PvPOpen", "Trả trophy, league hiện tại, vé còn lại và BẢNG THƯỞNG rewardTable: "
           "mỗi league có winRewards + loseRewards (danh sách Type + Amount) — thua vẫn có thưởng nhỏ."],
          ["Tìm trận", "PvPFindOpponents", "Tốn lượt tìm? Không — chỉ trả roster + vé hiện có."],
          ["Vào trận", "PvPStartBattle", "TRỪ VÉ tại đây; trả battleToken + snapshot đối thủ."],
          ["Kết thúc", "PvPBattleResult", "Server trả won/forfeit, oldTrophy → newTrophy (trophyDelta), "
           "old/newLeagueIndex (thăng/giáng ngay), trophy mới của đối thủ, rewards thực nhận."],
          ["Nhận thưởng", "PvPClaim", "Claim thưởng league (PvPRewardsPopup 'PvP Rewards'); "
           "có cơ chế áp thưởng pending ('Applied pending PvP rewards')."],
          [],
          ["Nhận xét"],
          ["• Thiết kế K-factor: league thấp thắng +40/thua −10 (tỉ lệ 4:1) → người mới leo rất nhanh; "
           "càng lên cao KWin giảm (40→20) và KLoss tăng (−10→−25) → Diamond/Master thắng thua ±25 "
           "cân bằng; Grandmaster/Legend về ±20. Đây là 'ladder có trợ lực' giai đoạn đầu."],
          ["• Mỗi league rộng đúng 200 trophy (trừ Iron 0–1199 và Legend 2800+∞) → từ Bronze lên Legend "
           "cần +1 600 trophy; với KWin trung bình ~27 cần ≈60 trận thắng ròng, tức 1–2 tháng với 9 vé/ngày."],
          ["• WeeklyDecay có field cho TỪNG league nhưng đang = 0 toàn bộ — hệ decay hàng tuần đã code "
           "sẵn (chỉ chờ bật config) để mùa sau ép top ladder phải chơi đều."],
          ["• Trận đánh là mô phỏng client vs bản sao → không cần server real-time, không lag; đổi lại "
           "server phải chống gian lận bằng battleToken + tự tính trophy, KHÔNG tin client."],
          ["• Reward table hoàn toàn server-driven (không có trong APK) → chỉnh thưởng không cần update app."],
          ["• PvP được TÁI DÙNG trong Clan War: ngày 6 của war là vòng PvP riêng (xem tab F3); "
           "vé PvP thường và vé Clan War PvP là hai hệ riêng biệt."],
          ["• Vé là nguồn thu ads: 4 ad/ngày cho vé + ad khác cho rương/cuốc — PvP là 'ad slot' đều đặn."]]
    return "F1. PvP Đấu trường", g, 3, swatches


def tab_banner():
    """F2. Clan banner — ClanBannerCatalog."""
    cat = json.load(open(os.path.join(TABLES, "ClanBannerCatalog.json"), encoding="utf-8"))[0]
    bg_colors, im_colors = cat["backgroundColors"], cat["imageColors"]
    n_shape, n_emb = len(cat["backgroundSprites"]), len(cat["imageSprites"])
    total = n_shape * len(bg_colors) * n_emb * len(im_colors)
    swatches = []
    g = [["F2. CLAN BANNER — ClanBannerCatalog (bộ ghép cờ hiệu clan)"],
         ["Nguồn: AssetRipper → Assets/MonoBehaviour/ClanBannerCatalog.asset · sprite clan_flag_* / "
          "clan_icon_* (Texture2D) · localization Clan.Banner.*"],
         ["Banner clan ghép từ 4 lựa chọn trong Banner Editor (Shape / Background Color / Emblem Shape / "
          "Emblem Color) và lưu thành 4 số nguyên trong dữ liệu clan (bannerBackgroundTypeId, "
          "bannerBackgroundColorId, bannerImageTypeId, bannerImageColorId — thấy trong mọi DTO clan). "
          "Tổng tổ hợp: %d hình cờ × %d màu nền × %d emblem × %d màu emblem = %s banner khác nhau. "
          "Sprite gốc là GRAYSCALE, game tint màu lúc chạy." % (
              n_shape, len(bg_colors), n_emb, len(im_colors), format(total, ","))],
         ["Ví dụ banner ghép (cờ 01 + nền đỏ + emblem rồng trắng):"],
         [img("banner_example", 64)],
         [],
         ["1. TÁM HÌNH CỜ (backgroundSprites — preview tint màu xanh #2D5BB5)"],
         ["Preview", "Id", "Sprite", "Ghi chú"]]
    for i in range(1, n_shape + 1):
        g.append([img("flag_%02d" % i, 52), i - 1, "clan_flag_%02d" % i,
                  "Cờ nền (base) + lớp overlay clan_flag_%02d_color được tint màu nền đã chọn" % i])
    g += [[],
          ["2. MƯỜI MÀU NỀN (backgroundColors)"],
          ["Id", "Màu", "Hex", "Gợi tên"]]
    names_bg = ["Kem", "Đỏ", "Cam", "Vàng", "Xanh lá nhạt", "Xanh da trời",
                "Xanh lá đậm", "Xanh dương", "Tím", "Xám đen"]
    for i, c in enumerate(bg_colors):
        row = len(g)
        swatches.append((row, 1, c))
        g.append([i, hexc(c), hexc(c), names_bg[i]])
    g += [[],
          ["3. HAI MƯƠI EMBLEM (imageSprites — sprite gốc, chưa tint)"],
          ["Emblem", "Sprite", "Tên"]]
    for key, vn in EMBLEMS:
        g.append([img("emblem_" + key, 44), "clan_icon_" + key, vn])
    g += [[],
          ["4. TÁM MÀU EMBLEM (imageColors)"],
          ["Id", "Màu", "Hex", "Gợi tên"]]
    names_im = ["Kem", "Nâu da", "Vàng cam", "Tím nhạt", "Xám", "Xanh băng", "Xanh xám", "Xám đen"]
    for i, c in enumerate(im_colors):
        row = len(g)
        swatches.append((row, 1, c))
        g.append([i, hexc(c), hexc(c), names_im[i]])
    g += [[],
          ["Nhận xét"],
          ["• Catalog thuần thẩm mỹ — không gameplay, không bán: mọi lựa chọn banner đều miễn phí, "
           "mở khoá từ đầu. Vai trò là nhận diện clan trong war/leaderboard (mọi DTO clan đều mang 4 id banner)."],
          ["• Màu nền (10) và màu emblem (8) là hai palette KHÁC nhau: màu emblem nhạt/trầm hơn để nổi "
           "trên nền — palette nền rực (đỏ cam vàng), palette emblem là kem/xám/pastel."],
          ["• 20 emblem theo chủ đề fantasy trung cổ (vũ khí, huy hiệu, quái, mũ) khớp art style game."],
          ["• Lưu ý kỹ thuật: AssetRipper không export .meta nên KHÔNG xác minh được thứ tự sprite trong "
           "catalog ↔ tên file (guid không tra được); bảng trên xếp theo tên file (flag 01→08, emblem A→Z), "
           "id trong bảng là SUY ĐOÁN theo thứ tự đó. Màu là chính xác 100%% từ catalog."],
          ["• Banner render 3 lớp: cờ base (viền/thanh treo) + overlay tint màu nền + emblem tint màu "
           "emblem, kèm clan_flag_shadow đổ bóng."]]
    return "F2. Clan Banner", g, 3, swatches


def tab_clan():
    """F3. Clan / Clan War / Chat — server DTO."""
    g = [["F3. CLAN / CLAN WAR / CHAT — server-authoritative (khôi phục từ DTO + UI strings, "
          "KHÔNG có config local)"],
         ["Nguồn: AssetRipper → Clan*.cs, ClanWar*.cs, Chat*.cs (48 DTO) · localization strings_en "
          "(Clan.*, Clan.War.*, UI.Chat.*, Errors.*)"],
         ["Toàn bộ hệ xã hội chạy trên server riêng (không phải Firebase RC): APK chỉ chứa hợp đồng "
          "dữ liệu (DTO) và text UI. Mọi con số cụ thể (điểm milestone, thưởng theo tier, số vé war) do "
          "server trả runtime qua ClanWarConfigDTO — bảng dưới là CẤU TRÚC hệ thống + các con số client "
          "hard-code trong UI strings."],
         [],
         ["1. CLAN CƠ BẢN (ClanWireDTO, ClanCreateRequestDTO, Errors.Clan.*)"],
         ["Mục", "Chi tiết"],
         ["Tạo clan", "Tên 3–15 ký tự (chỉ chữ + số, lọc từ bậy, không trùng); có Tag, Description, "
          "Announcement; chọn banner (tab F2); server theo region."],
         ["Kiểu tham gia (joinSetting)", "Open (vào ngay) hoặc Approval Only (gửi request, Leader/Captain "
          "duyệt Accept/Deny); có giới hạn request/ngày ('Daily join request limit reached')."],
         ["Vai trò", "Leader → Captain → Member (Promote/Demote/Kick); số Captain có giới hạn "
          "('Already have the maximum number of captains')." ],
         [img("role_leader", 36), "Icon Leader (clan_leader_icon)"],
         [img("role_captain", 36), "Icon Captain (clan_captain_icon)"],
         ["Chống nhảy clan", "Rời clan phải chờ 24h mới vào clan khác (JoinCooldown) — chống 'war tourist'."],
         ["Thành viên", "Hiển thị Power từng người + totalPower clan; memberCount (clan full có giới hạn — "
          "số cụ thể do server); avatarId; joinedAt."],
         ["Tìm clan", "Search theo tên + Advanced Search: lọc min/max member, ẩn clan Approval Only."],
         ["Tier clan", "clanTier / rewardTier / tierPoints — clan có bậc (Tier {0}), quyết định mức thưởng "
          "war (Tier {0} Rewards); điểm tier tích qua các war."],
         [],
         ["2. CLAN WAR — CHU TRÌNH TUẦN (ClanWarWireDTO, Clan.War.*)"],
         ["Mục", "Chi tiết"],
         ["Ghép cặp", "Mỗi tuần (weekId) clan được ghép 1 vs 1 với clan khác (myClan vs enemyClan; "
          "có thể gặp BOT — field isBot). Chờ ghép: 'Your clan will be matched when the next war week begins'."],
         ["Nhịp ngày", "War chia theo ngày (activeDay, dayEndsAt): Ngày 1–5 đua ĐIỂM CỐNG HIẾN theo "
          "nguồn quy định từng ngày (sourcesByDay — 'Day {0} Actions'), Ngày 6 là vòng PVP riêng, "
          "sau đó chốt kết quả + cooldown nhận thưởng ('New War Starts in')."],
         ["Thắng ngày → War Score", "Mỗi ngày, clan nhiều điểm hơn thắng ngày đó và nhận War Score "
          "(warScoreByDay — điểm có thể khác nhau theo ngày); dailyResults lưu winner + MVP từng ngày; "
          "tổng War Score cuối tuần định thắng thua cả war (tỉ số '{0} : {1}')."],
         ["Live", "Thanh live so điểm daily/weekly 2 clan (ClanWarLiveBarDTO); leaderboard cống hiến "
          "Daily/Weekly từng thành viên, cập nhật mỗi phút; MVP mỗi bên."],
         ["Championship War", "Nhánh war đặc biệt (type): BXH Championship Ranking (cập nhật 10 phút/lần) — "
          "clan cống hiến cao nhất tuần giành quyền THÁCH ĐẤU Champion Clan tuần sau; chưa có Champion "
          "thì top 2 đánh nhau. Thắng giữ đai (leaderClanId)."],
         [],
         ["3. CÁCH GHI ĐIỂM CỐNG HIẾN (ClanWarActionDTO + ClanWarAwardsDTO + Clan.War.Action.*)"],
         ["Nguồn điểm", "Chi tiết (điểm cụ thể server cấu hình theo rarity/loại)"],
         ["Lên level nhân vật", "Level Up: điểm = level mới × 100 (công thức hiện trong UI "
          "'New Level x 100 pts'; server có levelUpMultiplier)"],
         ["Nhặt trang bị", "Loot Equipment theo 10 bậc rarity Common → Divine (awards.lootEquipment: "
          "điểm theo rarity)"],
         ["Đào quặng", "Mine Stone/Coal/Iron/Ruby/Emerald/Gold/Diamond (nối hệ Mining tab E1; "
          "awards.mining theo loại quặng)"],
         ["Triệu hồi", "Summon Cape / Summon Companion theo rarity (awards.summonCape/summonCompanion)"],
         ["Dungeon Key", "Use Dungeon Key (awards.dungeonKey) — dùng chìa vào dungeon"],
         ["Thắng PvP war", "awards.pvpWin — điểm cho mỗi trận thắng ở vòng PvP ngày 6"],
         ["(Mỗi action gửi lên server: actionId chống trùng, source, rarity, resource, newLevel, count, "
          "timestamp — server tự quy ra điểm, client không tự cộng.)"],
         [],
         ["4. NGÀY 6 — VÒNG PVP CLAN WAR (ClanWarPvpDTO, Clan.War.Pvp.*)"],
         ["Mục", "Chi tiết"],
         ["Thể thức", "Mỗi thành viên có số vé riêng (startTickets, 'Tickets {0}/{1}'); chọn đánh thành "
          "viên clan địch (targets, thấy Power); thắng → đối thủ bị đánh dấu DEFEATED (mỗi người chỉ bị "
          "hạ 1 lần/vòng, đánh người đã hạ không được điểm) + winPoints điểm cống hiến."],
         ["Reset vòng", "Khi hạ đủ ngưỡng resetThreshold ('Tickets refresh when {0} players are defeated') "
          "→ mở vòng mới (round), hồi vé + xoá cờ defeated; tối đa maxRoundResets lần/war."],
         ["Luật clan nhỏ", "Nếu 1 trong 2 clan bắt đầu vòng dưới 30 thành viên (minMembersForReset) → "
          "KHÔNG hồi vé/reset (chống clan đông tạo phụ 'farm' reset)."],
         ["Đồng bộ", "Đánh xong nộp report; mất mạng thì kết quả pending tự nộp lại "
          "('Result will be synced when the connection recovers')."],
         [],
         ["5. PHẦN THƯỞNG WAR (ClanWarMilestoneRowDTO, ClanWarRewardsDTO, ClanWarClaimsDTO)"],
         ["Loại", "Chi tiết"],
         ["Cá nhân — milestone", "Mốc điểm cống hiến tuần (threshold) → nhận dần: Loot Box, Bones, "
          "Pickaxe (cuốc — nối hệ Mining), Experience, Cloak Currency (tiền nâng cape); đủ hết còn "
          "1 BUNDLE tổng (personalBundle). Claim từng mốc hoặc 'Claim Remaining Personal Rewards'."],
         ["Clan — theo tier + thắng/thua", "clanRewards[tier][win|lose] (Win Card / Lose Card — thua vẫn "
          "có thưởng): thêm Golden Pickaxe, Drill, Vial so với thưởng cá nhân; cả clan cùng mức, "
          "Leader không phải chia."],
         ["Cooldown", "Sau war có 'Reward Cooldown' — hết hạn không claim là mất; claims lưu trạng thái "
          "từng loại đã nhận."],
         [],
         ["6. CHAT (Chat*.cs, UI.Chat.*)"],
         ["Mục", "Chi tiết"],
         ["Kênh", "2 tab: World Chat (toàn server) + Clan Chat (phải có clan — 'Join a clan to use clan "
          "chat'); server trả maxMessages tin gần nhất."],
         ["Tin nhắn", "Model tối giản (JSON key 1 chữ cái u/a/n/m/t/tp — tiết kiệm băng thông): userId, "
          "avatar, tên, nội dung, timestamp, type."],
         ["Khoe đồ (itemDrop)", "Type đặc biệt 'itemDrop': gửi kèm TOÀN BỘ build (items + substats, "
          "companions, wing, cape + substats, enchantment tiers) → người khác bấm vào xem chi tiết món "
          "đồ vừa rớt. Chat = kênh flex loot."],
         ["Rate limit", "Chống spam: 'Too many messages! Wait {0}s' + placeholder đếm ngược cooldown."],
         ["Moderation", "Report người chơi (ChatReport); lọc từ bậy dùng chung hệ với tên clan; "
          "xem profile người chat (ChatPlayerProfile)."],
         [],
         ["Nhận xét"],
         ["• Thiết kế war 'chơi game bình thường = cống hiến': điểm đến từ level up, loot, mining, "
          "summon — không bắt mode riêng 5 ngày đầu; chỉ ngày 6 là content war thật (PvP). Cấu trúc "
          "sourcesByDay cho phép server đổi 'chủ đề ngày' (hôm nay điểm mining, mai điểm loot...)."],
         ["• Mọi số cân bằng (điểm/action, milestone, thưởng tier, vé PvP war) đều server-side → "
          "tune từng tuần không cần update app; client chỉ hard-code 2 số trong UI: ×100 điểm level up "
          "và mốc 30 thành viên."],
         ["• Chống gian lận nhiều lớp: actionId idempotent, battleToken, server tự tính điểm, "
          "session key riêng cho war (ClanWarSessionKeyResponse), rate limit."],
         ["• Thưởng war bơm ngược vào các hệ khác: Pickaxe/Golden Pickaxe/Drill (Mining E1), Cloak "
          "Currency (Cape C7), Bones + Vial (currency), Loot Box, EXP — war là 'động cơ' kéo mọi hệ."],
         ["• Chat itemDrop + inspect build là social proof: khoe đồ hiếm ngay khi rớt → kích thích "
          "người khác cày/mua. Kết hợp 24h join cooldown → giữ chân trong clan."],
         ["• Trong ClanWarWireDTO có 4 field obfuscated bgnm/bgnn/bgno/bgnp (luôn false) — dấu vết "
          "obfuscator, có thể là feature flag nội bộ."]]
    return "F3. Clan · Clan War · Chat", g, 4, []


def service():
    c = json.load(open(CRED))["installed"]
    t = json.load(open(TOK))
    creds = Credentials(
        token=t.get("access_token"), refresh_token=t.get("refresh_token"),
        token_uri=c["token_uri"], client_id=c["client_id"], client_secret=c["client_secret"],
        scopes=t.get("scope", "").split())
    return build("sheets", "v4", credentials=creds, cache_discovery=False)


def contrast_text(c):
    lum = 0.299 * c["r"] + 0.587 * c["g"] + 0.114 * c["b"]
    v = 1.0 if lum < 0.5 else 0.0
    return {"red": v, "green": v, "blue": v}


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
        # swatch mau: to nen cell = mau that, chu hex tuong phan
        for r, c, col in swatches:
            reqs.append({"repeatCell": {
                "range": {"sheetId": sid, "startRowIndex": r, "endRowIndex": r + 1,
                          "startColumnIndex": c, "endColumnIndex": c + 1},
                "cell": {"userEnteredFormat": {
                    "backgroundColor": {"red": col["r"], "green": col["g"], "blue": col["b"]},
                    "textFormat": {"bold": True, "foregroundColor": contrast_text(col)},
                    "horizontalAlignment": "CENTER"}},
                "fields": "userEnteredFormat(backgroundColor,textFormat,horizontalAlignment)"}})
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
        print("wrote tab:", len(grid), "rows,", len(img_rows), "image rows,", len(swatches), "swatches")


def main():
    tabs = [tab_pvp(), tab_banner(), tab_clan()]
    # index 21 = Mind-map(0) + A(1..6) + B(7..9) + C(10..18) + D1(19) + E1(20)
    write_tabs(tabs, 21)
    print("images:", ("ON %d keys" % len(IMGS)) if IMGS else "OFF")


if __name__ == "__main__":
    main()
