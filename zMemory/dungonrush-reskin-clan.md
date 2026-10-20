---
name: dungonrush-reskin-clan
description: "Reskin Clan 2026-10-20 theo mockup _Concept/Clan (BallIdle): ClanTabPage 3 tab + 13 popup + thẻ; banner ghép giữ hệ khung+lớp màu+icon tô màu — khung vẽ code style art, 20 icon + Leader/Captain ChatGPT; đã commit "Reskin Clan theo mockup BallIdle""
metadata:
  node_type: memory
  type: project
  originSessionId: 0702fcef-61a8-4baa-86f2-aaff5bdb96ef
  modified: 2026-10-20T09:55:46.388Z
---

Reskin Clan 2026-10-20 (đã commit "Reskin Clan theo mockup BallIdle", chỉ chụp preview, chưa test Play). Mockup `ArtIncoming/_Concept/Clan/` (6 demo: Join, Clan information, Clan member, Clan war, Days & results, Rewards; `_art/` 14 ảnh). Ảnh so: `_Concept/Clan/compare_*.png`.

- Kit `Art/1_UIKit/Clan/` từ `tools/reskin/gen_clan.py`: card_dark/gray/blue (art, viền 22), input_dark, icon_search, war_bar_track/blue/red/divider, vbar_track/fill (art ngang xoay 90°), dot_status, box_dark (vẽ code: viền #19191E, ruột #302C3D), tab_track/tab_pill (rãnh tối + viên cam viền vàng), separator (bevel 8px).
- Banner clan là hệ GHÉP (ClanBannerView: 8 khung `clan_flag_0X` + lớp màu `_color` tô backgroundColors + 20 icon tô imageColors) → GIỮ hệ: khung vẽ code theo banner art (thanh treo cắt từ art + viền vàng quanh đúng hình lớp màu cũ), lớp màu xám có khối, `clan_flag_shadow` để trống; 20 icon ChatGPT vẽ vàng (`_Concept/Clan/gen/`, dragon/swords ref = crop art) → `tools/reskin/gen_clan_icons.py` chuyển XÁM theo kênh G để code tô (màu 2 = vàng như mockup). Leader/Captain icon giữ màu. Ghi đè qua ArtIncoming + Apply Reskin.
- Trang (ClanTabPage, vùng trang lobby y 105..2001; mockup 168..1962 → y game = mockup − 63, phần neo đáy + 39): nền #403F51, khu trên Join #302C3D, thanh Member/Battle tab_track + tab_pill. `ReskinPrefabUtil.OpenPage/SavePage` (mới) bọc trang trong khung 1080x2160 để Place theo toạ độ màn hình.
- Popup có mockup: UIClanInfoPopup (816x1688, ô tan chứa banner+tên+mô tả+chỉ số, dòng thành viên = prefab biến thể `UINew/Clan/ClanMemberCardGray`, nút Join SpriteSwap disabled = Mining/btn_gray khi clan đầy), UIClanWarDaysAndResultsPopup (dòng ngày box_tan; Win/Lose tô nhạt), UIClanWarRewardsPopup (thanh dọc vbar, mốc: `ClanWarMilestoneCard.ReachedSprite/UnreachedSprite` đổi sprite thay tô màu). 10 popup còn lại: kit Mastery chung (cao +70 cho header, tiêu đề vào header, nút theo màu cũ: xanh/đỏ Gameplay/cam BattlePass).
- Mockup có mà game không: ô cash/gem header, status online "4h 17m" ở dòng thành viên → bỏ. Game có mà mockup không (Tier/Power ở thẻ clan, mô tả clan, nút Settings/Requests, avatar) → giữ, đổi hình.
- KHÔNG chạy lại `Tools/DungeonRush/Build Clan UI Prefabs` (ClanPrefabBuilder ghi đè prefab + catalog).

Xem [[dungonrush-reskin-plan]], [[dungonrush-reskin-ui-howto]], [[dungonrush-clan-clanwar]].
