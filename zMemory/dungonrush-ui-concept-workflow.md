---
name: dungonrush-ui-concept-workflow
description: "Reskin lại MỌI popup theo đợt: concept HTML sprite thật → user chốt → tool PopupStyle/Batch<N>Reskin sửa prefab tại chỗ; quy trình + tiến độ ở tools/concept/PLAN.md; Đợt 0+1+2 xong 2026-11-11, tiếp Đợt 3"
metadata:
  node_type: memory
  type: project
  originSessionId: 3164be48-a1fc-4de0-a1a9-4dd4a3175277
  modified: 2026-11-11T22:00:00.000Z
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
- Thông tin màn/map phải ghi TÊN rõ ràng, không chỉ số level/id (user sửa ô mốc Battle Pass → "Normal 3-5" theo StaticCampaignData.GetLevelTextTwoLines, tên Tier dịch Items.Tier.Name.TierN).

## Đã làm
- Mẫu đầu: popup 7 ngày (`SevenDayPopupReskin`) + kết quả dungeon (`DungeonEndPopupReskin`) — commit 37505a04.
- Đợt 0 kit + Đợt 1 Trang bị & Loot (6 popup) — commit 812200d0, sửa viền Auto Loot sau đó. Thêm nút Claim tím ở Combat Summary (user chốt concept). Popup Đợt 1 khác Auto Loot CHƯA test Play.
- Đợt 2 Shop & ưu đãi (9 popup + ô con ChestRewardElementUI/ChestContentUI ở Prefabs/UINew/Shop) — 2026-11-11, chụp edit mode batch2/unity.png khớp concept, CHƯA test Play. Lệch gốc user duyệt: X Battle Pass lên header + nút mua xanh lá, X cho Missing Gems, tiêu đề "Rewards" + "Tap anywhere to close" ở 2 màn mở rương. Glow_Circle02 đã bỏ nén (ASTC làm vệt khối).

**Why:** user sẽ reskin lại cả bộ UI theo style mới; concept trước → duyệt → áp để khỏi làm lại prefab nhiều lần.

**How to apply:** user nói "làm đợt tiếp"/"làm Đợt N" → mở PLAN.md, làm theo quy trình A→B→C, chờ user chốt concept rồi mới viết tool reskin. Liên quan [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-plan]], [[dungonrush-growth-p0]].
