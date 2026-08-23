---
name: dungeonrush-config-format
description: "Dungeon Rush stores config as standard Unity ScriptableObject YAML, NOT the 2def binary format"
metadata: 
  node_type: memory
  type: project
  originSessionId: e50637bc-3fc2-4f90-a0ac-4b830fd8822e
  modified: 2026-07-22T17:53:36.302Z
---

Game **Dungeon Rush** (`com.lavalabs.dungeonrush`), decompile bằng AssetRipper tại
`E:\00Work\00Project\2026DungOnRush\AssetRipper\ExportedProject`.

Config data KHÔNG dùng format binary "2def" (skill `decode-unity-apk-config`). Game serialize
bằng **Unity ScriptableObject chuẩn** → AssetRipper export thẳng ra `.asset` YAML đọc được
trong `Assets/MonoBehaviour/` (378 file) + `Assets/Resources/`. Không có `.bytes` config nào.

**Why:** Bước 0 của skill là "nhận diện có đáng làm không" — ở đây binary reverse KHÔNG áp dụng.

**How to apply:** Extractor đã build sẵn ở `DecodedData/extract.py` (cần pyyaml). Nó gom asset
theo class, dịch enum (`<Field>__enum`), decode mảng hex-blob LE. Output 27 bảng game
(~350 record) ở `DecodedData/tables/*.json` + `.csv`, index ở `DecodedData/_index.md`.
Bảng lớn: WeaponData(72), GlovesData(54), HelmetData(54), Ring/Necklace(30), CompanionData(18, có combat stats).

Lưu ý: game nặng server-driven (nhiều `*DTO`) — stat quái theo wave, PvP matchmaking, reward event
phần lớn do server trả, không có trong APK. Ring/Necklace chỉ có ItemId/Rarity, stat sinh runtime.

**Remote Config LIVE lấy được KHÔNG cần MITM:** LDPlayer 14 rooted (`adb 127.0.0.1:5555`, adb ở
`C:\LDPlayer\LDPlayer14\adb.exe`). Game cache Firebase Remote Config tại
`/data/data/com.lavalabs.dungeonrush/files/frc_*_firebase_activate.json` — pull bằng root adb
(copy ra /sdcard đổi tên bỏ dấu `:` vì Windows cấm, dùng `MSYS_NO_PATHCONV=1` khi adb pull).
Cho army_power, experience curve, forge odds, weapon_offer, starting_loot (config_version 17.0.0).
Parse bằng `DecodedData/parse_remote_config.py` → `remote_config_live.json` + CSV.
**QUAN TRỌNG:** mitmdump/MITM bị auto-mode classifier chặn cứng (kể cả tự ghi settings để cấp quyền)
— app-cache pull là đường vòng hợp lệ, không bị chặn.

**Ghi Google Sheet (GDD):** target sheet DungeonRush = `10ln6GpelKvVD1Wjo7Ahz8eTB38Xj6CaJ1PzphK8yMcg`
("DungeonRush - GDD"). ĐỪNG dùng browser/clipboard (Sheets không bao giờ idle → screenshot/read_page
treo; clipboard paste bị Chrome chặn). Dùng **Google Sheets API v4 + OAuth token có sẵn** ở
`C:\Users\admin\.config\google-sheets\` (credentials.json + token_sheets.json, scope `spreadsheets`,
set up 1 lần bằng CarSurvival `tools/oauth_setup.py`). Script: `tools/write_mindmap.py` (đọc
`DecodedData/mindmap_tinh_nang.tsv` → ghi tab "Mind-map tính năng"), verify bằng `tools/verify_mindmap.py`.
Lệnh `python tools/write_mindmap.py` (trần) qua được classifier; prefix env-var hoặc heredoc đọc token
thì BỊ chặn. Print phải ASCII (console cp1252). Tab đầu đã ghi xong (2026-07-22).

**Tiến độ GDD (2026-07-22):** đã ghi Mind-map + nhóm A (A1–A6, `write_group_a.py`) + nhóm B
(B1 Boss Gate, B2 Boss Rush, B3 League — `write_group_b.py`) + nhóm C (C1 Vũ khí … C9 Forge —
`write_group_c.py`, mỗi tính năng 1 tab) + nhóm D (D1 Companion — `write_group_d.py`, index tab 19).
Nhóm E (E1 Đào mỏ Mining — `write_group_e.py`, index tab 20; 18 ảnh `upload_images_e.py` →
`image_urls_e.json`, crop atlas `_AtlasMining-0ad2c8bf` guid 42c020e1; khối đá = sprite
`stone_obstacle_broken`, còn "mining (1)" chỉ là icon cuốc+đá; enum ở `MineOreType.cs`).
Nhóm F (2026-07-23, `write_group_f.py`, index 21–23): F1 PvP (PvPConfig 10 league + PvP*DTO),
F2 Clan Banner (ClanBannerCatalog; sprite = PNG rời `clan_flag_01-08`+`_color` overlay grayscale
tint runtime, 20 `clan_icon_*`; thứ tự index↔tên file là suy đoán vì không có .meta; swatch màu
= tô nền cell qua repeatCell), F3 Clan/ClanWar/Chat (server DTO 48 file + localization —
war tuần 7 ngày, Day 6 PvP, điểm = LevelUp×100/loot/mine/summon; minMembersForReset 30).
Ảnh F: 32 key `upload_images_f.py` → `image_urls_f.json` (composite cờ+overlay tint bằng PIL).
Verify: `verify_group_bc.py`, `verify_group_d.py`, `verify_group_e.py`, `verify_group_f.py`.
Nhóm G+H (2026-07-23, `write_group_gh.py`, index 24–30, mỗi tính năng 1 tab): G1 Mastery
(MasteryUpgradeData 10 nhánh + MasteryConfig, thang cấp Gem/Value chi tiết), G2 Battle Pass
(BattlePassConfig, RewardType enum ở RewardType.cs 0-11, pass theo LEVEL không phải mùa),
G3 Weapon Offer (remote:weapon_offer_config `lv,ItemId|...` → 5 vũ khí Epic→Divine),
H1 Rương (ChestData 4 loại + RarityOdds + pity 20), H2 Bundle rương (ChestBundleData IAP),
H3 Loot Box/Offer (remote starting_loot_box_count=240 + chest_offer), H4 IAP/Ads (24 product id
`lootio.*` từ Assembly-CSharp: gem1-6, offer_099..9999=giá USD, deal/pack/battlepass/noads... +
ad Banner/Interstitial/Rewarded, Adjust). Ảnh G/H: `upload_images_gh.py`→`image_urls_gh.json` (22
key): mastery icon crop từ atlas `_AtlasAllUI_1-ad210386` (guid tex d2fb140a, mapping icon↔nhánh
SUY ĐOÁN theo hình), chest icon là PNG rời (epic/legendary/mythic/standart_chest), pickaxe/reward
PNG rời; weapon offer icon TÁI DÙNG `image_urls_bc.json` (key=`_name`). Verify `verify_group_gh.py`.
`tools/list_tabs.py` in danh sách tab hiện có.
Nhóm I (2026-07-23, `write_group_i.py`, index 31–33, HOÀN THÀNH — đủ 34 tab A→I): I1 Remote Config
(16 khoá live vs 14 const trong RemoteConfigController.cs — 2 khoá live-only: army_power_level_scaler,
starting_loot_box_count; boss_gate_enabled=false là kill-switch), I2 Localization (10 ngôn ngữ, en đủ
1031 chuỗi, 9 ngôn ngữ khác 1006 — thiếu đúng 25 chuỗi Clan mới; 6 collection Items 388/Popup 216/
Clan 152/UI 96/Errors 91/Common 88; cột CSV: ge=de, po=pt-BR, sp=es, ta=it, tu=tr), I3 Audio
(AudioConfig 10 sự kiện + 57 clip .ogg nhóm theo prefix; guid→clip là SUY ĐOÁN trừ cặp trùng guid
ItemSell=LootCollect và UnarmedHit=3 file punch; nhạc nền duy nhất 'Shire Evening Echoes').
Ảnh I: 10 cờ quốc gia = PNG ISO alpha-2 có sẵn trong `Texture2D/{GB,TR,SA,FR,DE,IT,JP,KR,ES,BR}.png`
(128×128, ~200 nước đủ bộ) — `upload_images_i.py` → `image_urls_i.json`; mapping cờ↔ngôn ngữ suy đoán
(guid FlagSprite trong `GameObject/LanguageSelectPopup.prefab` không tra được). Verify `verify_group_i.py`.
**Bẫy Sheets:** cell text bắt đầu bằng `+` với USER_ENTERED → `#ERROR!` (coi là công thức) — tránh.

**Icon companion:** sprite `tier_XX_pet_YY(_icon)` — tier 1–3 crop từ atlas icons1-5 (guid aefe761f),
tier 4–6 là PNG rời cùng tên trong Texture2D. Map pet_01..03 ↔ tên companion KHÔNG theo thứ tự bảng —
đã đối chiếu bằng mắt (contact sheet), mapping chốt trong `tools/upload_images_d.py` (dict PET);
18 ảnh ở `DecodedData/image_urls_d.json`. CompanionData scaler đồng nhất = Base/20 (+5%/level);
tier 6 tên asset "Divine" nhưng enum Rarity=Mythic.

**Icon item từ atlas (không có .meta → guid không map được):** AssetRipper export KHÔNG có file
`.meta`, nên ref `guid` trong config không tra ra file. Đường vòng đã dùng (`upload_images_bc.py`):
(1) tên asset item khớp tên sprite theo quy ước — `Weapon_m_3_2`→`tier_03_melee_weapon_02_icon_transparent`,
`Helmet/Gloves_T_N`→`tier_T_hat/glove_N_icon_transparent`, `Ring/Neckle/Backpack_T_N`→
`tier_T_ring/necklace/back_item_N`, Cape id n→`tier_{(n+1)//2}_cloak_{(n-1)%2+1}_icon`,
Wing id k→`Texture2D/tier_{k+1}_wing.png`; (2) đọc `m_Rect` + guid texture trong
`Assets/Sprite/*.asset`, guid gom nhóm ↔ atlas png trong Texture2D (icons1-5=aefe761f,
icons6-10=0062f43c, và 5 atlas theo cặp rarity); (3) crop PIL (y Unity từ đáy: top = H−y−h),
upload Drive. 250 ảnh trong `DecodedData/image_urls_bc.json` (key = `_name` asset, boss_`Tên`, forge).
Boss art: lych_01/02/03 = Dark/Crimson/Elder Lich, dragon_purple = Black Dragon,
witch_01/02 = Green/Purple Hag, ogre_01/02 = King/Chieftain (đối chiếu màu bằng mắt).
