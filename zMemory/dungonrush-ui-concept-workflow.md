---
name: dungonrush-ui-concept-workflow
description: "Quy trình chuẩn chuẩn bị reskin lại bộ UI — học style ảnh mẫu, concept HTML bằng sprite thật, user duyệt, tool editor sửa prefab tại chỗ; mẫu đầu là popup 7 ngày (2026-11-11)"
metadata:
  node_type: memory
  type: project
  originSessionId: 3164be48-a1fc-4de0-a1a9-4dd4a3175277
  modified: 2026-11-11T08:33:36.794Z
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

## Các bước
1. Đo màu ảnh mẫu bằng PIL (trung bình 5×5 px), rút quy tắc viền/nút/chữ/bố cục.
2. (Tuỳ) concept nháp bằng widget HTML để chốt bố cục/UX.
3. Concept bằng sprite thật: `tools/concept/<màn>/concept.html` trỏ thẳng `Art/` + font Asap; 1 px CSS = 2 đơn vị canvas 1080×2160; vẽ đủ mọi trạng thái cạnh nhau.
4. Chụp: `tools/concept/render_concept.sh <html> <png>` (Chrome headless ×2); ảnh để cạnh html trong `tools/concept/<màn>/`.
5. Ảnh kit không có → sinh bằng code `tools/reskin/gen_*.py` (VD `gen_day7_bg.py`); icon minh hoạ mới → skill chatgpt-image.
6. User duyệt concept xong mới đụng Unity.
7. Tool editor `Scripts/Art/Editor/<Màn>Reskin.cs`: sửa prefab có sẵn TẠI CHỖ (giữ GUID + chỉnh tay cũ), chạy lại ra cùng kết quả, số đo = concept × 2; builder cũ gọi tool ở cuối.
8. Đổi trạng thái = đổi sprite / ẩn hiện trong script UI, không tô màu đè.
9. Chữ mới: key vào đủ 10 file `Resources/Json/Localize/Languages/`, chữ dài bật auto size.
10. Chụp kiểm `UICaptureTool.Capture` (không cần Play), ghép cạnh concept.

## Bẫy
- Unity `spriteBorder` = (trái, dưới, phải, trên); CSS `border-image-slice` = (trên, phải, dưới, trái).
- Concept: `position:absolute` tính từ mép trong viền → bỏ `border-image` phải dời lại vị trí.
- `Loc.L` ở edit mode trả key (LocalizeManager chỉ nạp khi Play) → chụp kiểm gán chữ tay. Đừng gọi `X.Instance` Singleton không có trong scene đang mở.
- Nút khoá: `Button.transition = None` + sprite xám, để ColorTint sẽ tối thêm.

## Mẫu đầu: popup 7 ngày (2026-11-11)
`tools/concept/seven_day/` (concept.html + ảnh concept + ảnh Unity), `SevenDayPopupReskin` (menu Tools/DungeonRush/Reskin/Seven Day Popup), `SevenDayCellUI` đổi sprite, `UISevenDayPopup` ô đếm giờ + nút X, nền `Art/1_UIKit/SevenDay/day7_bg.png`, 3 key `Popup.SevenDay.Subtitle/NextGift/BigReward`. Chụp edit mode khớp concept; CHƯA test Play, CHƯA commit.

**Why:** user sẽ reskin lại cả bộ UI theo style mới; concept trước → duyệt → áp để khỏi làm lại prefab nhiều lần.

**How to apply:** màn mới → làm theo các bước trên, chờ user duyệt concept rồi mới viết tool reskin. Liên quan [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-plan]], [[dungonrush-growth-p0]].

## Kế hoạch toàn bộ popup (2026-11-11)
User yêu cầu concept lại MỌI popup còn lại, chia đợt: concept xong user chốt mới ghép vào game. Kế hoạch + bảng tiến độ ở `tools/concept/PLAN.md` (đợt 0 kit chung + 9 đợt, 67 popup). Đọc file đó khi user nói "làm đợt tiếp", cập nhật bảng tiến độ sau mỗi bước.
- Concept các đợt link `tools/concept/kit/kit.css` (CSS kit chung); C# kit = `Scripts/Art/Editor/PopupStyle.cs` (Shell/Title/CloseButton/Button màu chức năng/Outline/Plain/PlaceTop đo từ mép trên).
- User đòi: TRƯỚC khi concept popup có chỉ số phải đếm số dòng tối đa từ data thật (gear 1+3 substat, Wing 2+5, Cape 2+2, Auto Loot 10 rarity + 13 lọc) → ô cao cố định theo max, rỗng hiện "No bonus stats".
- Đợt 1 (2026-11-11) đã áp: `Batch1Reskin` (menu Tools/DungeonRush/Reskin/Batch 1), `ItemStatsView` dùng chung cho ItemInfo/NewItem/NewForge, builder cũ Forge/Level/Offline gọi tool cuối; thêm nút Claim tím ở Combat Summary (user chốt concept). CHƯA test Play, CHƯA commit.
- Bẫy chụp: Auto Loot có ContentSizeFitter lồng → ép LayoutRebuilder từng list rồi Content; TMP sprite asset cần UpdateLookupTables trước khi chụp.
