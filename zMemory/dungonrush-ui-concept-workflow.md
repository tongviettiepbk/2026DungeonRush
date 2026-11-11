---
name: dungonrush-ui-concept-workflow
description: "Reskin lại MỌI popup theo đợt: concept HTML sprite thật → user chốt → tool PopupStyle/Batch<N>Reskin sửa prefab tại chỗ; quy trình + tiến độ ở tools/concept/PLAN.md; Đợt 0..8 xong (Đợt 8 Clan War & Chat 2026-11-12), tiếp Đợt 9"
metadata:
  node_type: memory
  type: project
  originSessionId: 3164be48-a1fc-4de0-a1a9-4dd4a3175277
  modified: 2026-11-12T16:00:00.000Z
---

User duyệt cách làm này ngày 2026-11-11, dặn CHỈ lưu trong bộ nhớ, KHÔNG viết tài liệu vào GDD/.

## Style UI mục tiêu (học từ ảnh mẫu game vịt, chỉ học cách thiết kế, không lấy hình)
- Mọi khung/nút/icon/chữ viền tối dày 3–5 px + bóng đổ xuống; nền màn tối xám tím `#3E3D4D`, màu rực chỉ cho chỗ bấm được / đồ giá trị.
- Thân popup kem `#F4E6C9`, header tím xám `#49456A`, ô chỉ số nâu nhạt `#DFC9A0`.
- Nút 3D có mép đáy đậm (xanh `#92E35B`, mép `#629C41`); màu theo chức năng: xanh lá = nhận/mua, cam = hành động chính, hồng = ads/miễn phí, tím = phụ, xanh lơ = thông tin.
- 1 font: Asap SemiBold + `Asap-SemiBold-Outline-Black`; chữ nhỏ trên nền kem không viền, nâu `#7A5C3C`.
- Chấm đỏ `!` = có việc, tick xanh = xong. Thẻ ưu đãi lớn: nền chuyển màu + tia sáng + chấm halftone góc, ruy băng đỏ nghiêng lấn mép.

## Kit có sẵn theo vai trò (`Art/1_UIKit/`) — user muốn GIỮ item/nút của game
- Khung popup `Mastery/popup_frame` (9-slice 14), header `Mastery/popup_header` (cao 138).
- Nút X đóng popup: GIỮ nút đỏ `Mastery/btn_close` 104 (màu không đổi), chỉ đặt NẰM TRONG thanh tiêu đề sát mép phải, cách đều trên/dưới/phải (tâm cách góc 69,69) — user chốt 2026-11-11; không để lấn ra ngoài góc.
- Nút: `Lobby/btn_green` / `Lobby/btn_gray` (khoá), `BattlePass/btn_orange`, `Common/btn_violet`, `Common/btn_pink`.
- Ô thưởng theo quy ước Battle Pass (đã chốt cho cả game): `BattlePass/cell_green` nhận được, `cell_blue` đã nhận + `icon_check`, `cell_gray` chưa tới; chấm `BattlePass/icon_alert`.
- Ruy băng vàng `Gameplay/ribbon_offer`, nhãn đỏ `Events/ribbon_red` (9-slice L20 R52), ô tối bo tròn `Mastery/mastery_panel` (9-slice 45), thẻ `Mastery/card_locked`/`card_unlocked`.
- Icon tiền `Art/2_Icons/Currency/`: `gem`, `box_icon` (= búa Loot), `bone_icon`.

## Quy trình + tiến độ: `tools/concept/PLAN.md` (nguồn chính, đọc đầu tiên)
PLAN.md giữ: danh sách 67 popup chia Đợt 0..9, quy trình tối ưu 3 phase (A khảo sát → B concept → C áp), bẫy đã gặp, bảng tiến độ + dòng "Đang ở / Tiếp theo". Cập nhật bảng sau MỖI bước.

## Code dùng chung đã có
- `tools/concept/kit/kit.css` (CSS kit, concept các đợt link vào) + `tools/concept/render_concept.sh` (Chrome headless ×2).
- `Scripts/Art/Editor/PopupStyle.cs` (Shell/Title/CloseButton/Button màu chức năng/Outline/Plain/PlaceTop đo từ mép trên).
- `Scripts/Art/Editor/Batch1Reskin.cs` = mẫu tool 1 đợt (BuildStats/StatsLayout/NewTag để `internal` cho đợt sau dùng); `Batch2Reskin.cs` có thêm helper OfferShell (popup ưu đãi ruy băng vàng, không header), RewardBox, TapToClose; `UICaptureTool.Capture` chụp edit mode.
- `Scripts/UI/Forge/ItemStatsView.cs` = khối chữ món đồ (dùng lại cho Wing/Cape/Enchantment info ở đợt sau).

## Điều user đã dặn / sửa (đừng lặp lỗi)
- Trước khi concept popup có chỉ số: đếm số dòng TỐI ĐA từ data thật, khung cao cố định theo max (user hỏi "đủ không gian chưa").
- Nhãn/tag không được dính sát ô bên dưới (user báo "bị kích quá").
- Thanh tiến độ: fill phải thụt đều trong viền (user báo lỗi bản đầu).
- Viền đen bị mask vùng cuộn cắt (user báo "viền đứt") → mask không sprite + lề 8.
- Chốt concept rồi mới đụng Unity; commit thẳng main khi user bảo (user đã bảo commit sau mỗi đợt).
- Chữ "Lvl N" ở ô relic + ô tẩu căn GIỮA đáy ô (user sửa 2026-11-12, Batch4Reskin.BottomLevel); ô kính/Item Info/Pet Info vẫn góc trái.
- Thông tin màn/map phải ghi TÊN rõ ràng, không chỉ số level/id (user sửa ô mốc Battle Pass → "Normal 3-5" theo StaticCampaignData.GetLevelTextTwoLines, tên Tier dịch Items.Tier.Name.TierN).

## Đã làm
- Mẫu đầu: popup 7 ngày (`SevenDayPopupReskin`) + kết quả dungeon (`DungeonEndPopupReskin`) — commit 37505a04.
- Đợt 0 kit + Đợt 1 Trang bị & Loot (6 popup) — commit 812200d0, sửa viền Auto Loot sau đó. Thêm nút Claim tím ở Combat Summary (user chốt concept). Popup Đợt 1 khác Auto Loot CHƯA test Play.
- Đợt 2 Shop & ưu đãi (9 popup + ô con ChestRewardElementUI/ChestContentUI ở Prefabs/UINew/Shop) — 2026-11-11, chụp edit mode batch2/unity.png khớp concept, CHƯA test Play. Lệch gốc user duyệt: X Battle Pass lên header + nút mua xanh lá, X cho Missing Gems, tiêu đề "Rewards" + "Tap anywhere to close" ở 2 màn mở rương. Glow_Circle02 đã bỏ nén (ASTC làm vệt khối).

- Đợt 3 Pet & Mastery (UIPetInfo, uiUpgradePet, UiSumonPet, UICompanionAdSummonPopup, MateryPopupUpgrade trong UIMainGame2) — 2026-11-11, tool `Batch3Reskin`, chụp batch3/unity.png khớp concept, CHƯA test Play. User duyệt: tiêu đề "Summon Rates", "Remove"→Common.Unequip, 7 key mới (Popup.Companion.OwnEffect/SummonToUnlock, Popup.CompanionAdSummon.*), mô tả pet (descriptionKey) + tên/mô tả mastery (Items.Mastery.Name/Desc.<type>) dùng key dịch. Số trong mô tả pet đổi xanh #2F8F1F (vàng gốc không đọc được trên kem). Pet info chạm nền tối để đóng.
- Bẫy Đợt 3: chữ cũ để fontStyle Bold → PopupStyle.Outline không reset, phải đặt Normal; chữ viền đen trên panel tối (mastery_panel) trông như không viền — đúng như concept, không phải lỗi; SaveAsPrefabAsset UIMainGame2 sắp lại thứ tự component (diff to nhưng đủ 8 UIParticle).

- Đợt 4 Enchantment/Kính/Tẩu (10 popup + ô con CapeElementUI, SubstatUIElementPrefab) — tool `Batch4Reskin`, chụp batch4/unity.png, CHƯA test Play. User duyệt: header relic "Relic +N" (bỏ "Enchantment"), "Relic Merge", Summon Rates tẩu, 10 key mới (Popup.Enchantment.PowerLabel/PowerDesc/RelicMerge/Inventory/TapToAdd, Popup.WingReroll.LockedHint, Popup.CapeDetail.Title, Popup.CapeSalvage.Title/Select, Popup.Cape.NewPipe). Cape Detail/Salvage + Wing Reroll tách cột nhãn nâu/số viền (UICapePopup.SetStatLabels, UIWingRerollPopup.SetBaseStats); Show Pipe = công tắc kit Setting (sprKnobOn/Off + objShowCloakFill).
- Bẫy Đợt 4: prefab rip hay có HLG TẮT + phần tử đặt tay / LayoutElement.ignoreLayout (Merge) → bật lại; panel mới chèn trong trang có nền toàn màn phải đặt sibling SAU nền; input_tan trùng màu thân popup → dùng box_tan; ClaimPage/CapeShowOffPanel là con của popup nên toạ độ tính theo tâm popup (= tâm màn).

- Đợt 5 Dungeon & Mining (UIDungeonPopup, UIMiningPopup, UIMiningRarityPopup, UIMiningOfferPopup) — 2026-11-11, tool `Batch5Reskin` (chạy lại ra y hệt), chụp batch5/unity.png khớp concept, CHƯA test Play. User duyệt: Dungeon thêm nhãn "Keys" (key mới Popup.Dungeon.Keys) + dòng mô tả UI.Dungeon.Description; Sweep tím / Enter cam / Ads hồng / khoá xám (UIDungeonPopupStart.sprSweep/sprEnter/sprDisabled); nút dụng cụ Mining: chọn cam / còn hàng tím / hết còn ads hồng / hết cả ads xám (UIMiningPopup.sprToolAd/sprToolEmpty); cột level kế bảng quặng tăng xanh/giảm đỏ. Sửa kèm: "Next in" + "Increase Maximum Pickaxe Storage" dùng key dịch, gỡ key nhầm trên tiêu đề Offer. OfferShell/TapToClose của Batch2Reskin đổi sang `internal` để dùng lại.
- Bẫy Đợt 5: UnityMCP rớt vì server 8080 không chạy (không phải bridge) → script tạm gọi `MCPServiceLocator.Server.StartLocalHttpServer(true)` rồi `Bridge.StartAsync()`; RawImage mỏ chụp edit mode ra trắng (chỉ có hình khi Play).
- Đợt 6 PvP & Boss Rush (5 popup PvP + 4 Boss Rush + RewardsPanel trong UIBossRushPopup + UIPlayerPopup) — 2026-11-11, tool `Batch6Reskin`, chụp batch6/unity.png khớp concept, CHƯA test Play. GIỮ art mockup cũ (dòng xám/xanh, vạch PROMOTION, thanh máu boss, đầu boss + khiên nhô trên header); chỉ đồng bộ khung/X/nút/chữ. User duyệt: PvP thua Continue tím (sprContinueWin/Lose), Fight Boss Rush cam/hồng vé ads (chữ "<sprite=0>" IsAd)/xám (sprFight*), UIPlayerPopup giữ bố cục user chỉnh tay — chỉ X + tách cột chỉ số (txtSubStatValues). Thêm StatusPanel tối cho dòng "No fights remaining" (trước đó StatusText tắt sẵn nên không bao giờ hiện). Chữ cứng → key: Boss #, FightCount, NoFightsRemaining/NextIn, YourPlacement, Placement, TeamDamage/DamageDealt; key mới Popup.BossRushRewards.BossesKilled.

- Đợt 7 Clan (Create, Settings, Banner Editor, Edit Announcement, Requests, Clan Info, Clan Player, Clan Search, Championship Ranking + ô con ClanRequestCard/ClanWarClanRowCard/ClanBannerOptionItem ở Prefabs/UINew/Clan) — 2026-11-12, tool `Batch7Reskin` (chạy lại ra y hệt), chụp batch7/unity.png 16 ảnh, CHƯA test Play. User duyệt: Banner Editor bỏ dải cuộn → lưới thấy hết (8 / 5×2 / 7 cột / 8, ô box_dark, chọn = viền vàng + tick), tên clan lên header Clan Info, tên người chơi lên header Clan Player (icon Leader/Captain, Promote cam ↔ Demote tím đổi sprite, không nút → popup thấp lại), thêm nút Clear tím (key mới Popup.ClanSearch.Clear). Sửa kèm: Create dùng key Clan.Create.Button + thiếu gem số đỏ; bộ đếm n/200 mô tả clan (ClanUIUtil.SetCounter, đủ → đỏ); dòng clan của mình ở bảng xếp hạng = card_blue đổi sprite (ClanWarClanRowCard.NormalSprite/HighlightSprite, dùng chung Clan War Leaderboard Đợt 8).
- Bẫy Đợt 7: `PvP/slot_dark` đen trong 55% → trên nền kem ra xám nhạt, dùng `Clan/box_dark`; UI `Outline` nhân màu sprite (viền vàng trên sprite tối ra tối) → lớp trắng tô vàng to hơn + đè ô trong; chụp edit mode ô nhập phải gán `input.text`. Lưu prefab Clan qua Unity chỉ đổi PressYValue → PressScale/PressDuration (vô hại), sprite asset TMP không mất.

- Đợt 8 Clan War & Chat (Contribution Leaderboard, War Results, Clan Leaderboard, War Rewards, Report + ô con ClanWarContributionCard/ClanWarDayCard/ClanWarMilestoneCard) — 2026-11-12, tool `Batch8Reskin` (gọi lại helper Batch7Reskin đã để `internal`), chụp batch8/unity.png 9 ảnh, CHƯA test Play. User duyệt: tên clan lên header bảng đóng góp (banner nhỏ cạnh "Day n Leaderboard"); thẻ ngày đổi sprite mình thắng card_blue / địch thắng card_gray / chưa chốt card_dark (ClanWarDayCard.StatusImage + StateLabelText "Not Concluded" phủ cột Winner+MVP); 2 key mới Popup.ClanWarResults.Day/.Points; nút ? (Common/question) thay nút "i" Champion; mốc thưởng có ClaimTag (nút xanh + chấm !) / CollectedTag (tick) — ClanWarMilestoneCard; cooldown ẩn Win/Lose (UIClanWarRewardsPopup.TierSummaryRoot), nút Collect → "Claim", đã nhận thì thưởng mờ; Report nút đỏ, lý do/tiêu đề dùng key Popup.Report.* (trước ghi cứng sai chính tả), UIReportPopup.SubmitText ẩn khi gửi. Chữ ghép cứng → key: DailySubheader, Clan.War.Points, Clan.War.Day, TierHeader, MyScore; tỉ số bên mình xanh #5EC8FF.

**Why:** user sẽ reskin lại cả bộ UI theo style mới; concept trước → duyệt → áp để khỏi làm lại prefab nhiều lần.

**How to apply:** user nói "làm đợt tiếp"/"làm Đợt N" → mở PLAN.md, làm theo quy trình A→B→C, chờ user chốt concept rồi mới viết tool reskin. Liên quan [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-plan]], [[dungonrush-growth-p0]].
