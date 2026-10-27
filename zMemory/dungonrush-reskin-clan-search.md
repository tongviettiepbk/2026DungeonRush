---
name: dungonrush-reskin-clan-search
description: "Reskin 2 popup Clan Search (UIAdvancedSearchPopup) + Enter Clan Name (UICreateClanPopup) 2026-10-27 theo mockup _Concept/ClanSearch; kit 1_UIKit/ClanSearch (nút −/+ art, ô nhập tan vẽ code, tick xanh ChatGPT); chưa commit, chưa test Play"
metadata:
  node_type: memory
  type: project
  originSessionId: d0c41f16-3e9e-4788-8116-c47d1f054332
  modified: 2026-10-27T10:17:17.270Z
---

Reskin 2026-10-27 (CHƯA commit, chỉ chụp preview, chưa test Play). Mockup + so sánh ở `ArtIncoming/_Concept/ClanSearch/` (`compare_search.png`, `compare_create.png`; `gen/` prompt ChatGPT tick).

- Kit `Art/1_UIKit/ClanSearch/` sinh bằng `tools/reskin/gen_clan_search.py`: `btn_minus`/`btn_plus` (art giao 100x110 có sẵn bóng, có ký hiệu → ẩn node `Content` chữ −/+ của nút), `input_tan` (vẽ code: viền đen 3, gờ #EFD7BF 4, ruột #DEAF7F, bo 13, 9-slice 20), `check_green` (ChatGPT, 128x128 như check.png). Ô tick = `Mastery/box_tan` (#E7CA9E đúng màu mockup). KHÔNG ghi đè `Common/check.png` (dùng 6 chỗ).
- Prefab: tắt `VerticalLayoutGroup` của `VerticalLayout`, đặt tay từng hàng theo pixel mockup; khung 822 rộng (Search cao 1041, Create cao 898); nhãn trái x=193 MidlineLeft fs 40, chữ gợi ý bỏ nghiêng.
- Game có mà mockup không → GIỮ: banner xem trước cạnh Browse (đặt cột ô tick x=510), giá "Create \n<sprite=0>100", chữ gợi ý theo Loc ("Clan Name..." chứ không phải "Name...").

Xem [[dungonrush-reskin-clan]], [[dungonrush-reskin-ui-howto]].
