---
name: dungonrush-reskin-pvp
description: "Reskin PvP 2026-10-28 theo mockup _Concept/PvP: sảnh (danh sách world trong sảnh), chọn đối thủ (ô vé + avatar), Victory/Defeat, top bar \"PvP\"; kit 1_UIKit/PvP + TMP IsFist; CHƯA test Play, CHƯA commit"
metadata:
  node_type: memory
  type: project
  modified: 2026-10-28T16:48:47.595Z
  originSessionId: f7addc36-766a-4b9b-bf1c-8725b25a3183
---

Mockup + art giao (Telegram "pvp"): `ArtIncoming/_Concept/PvP/` (League demo, PvP demo2-6; `_art/` 6 mảnh). Kit sinh bằng `tools/reskin/gen_pvp.py` → `Art/1_UIKit/PvP/` (row_gray/row_blue 746x122 9-slice L24 B26 R24 T20, slot_dark = đen alpha 0.3 đã bù Linear, icon_shield, line_left/right). TMP sprite asset mới `Art/2_Icons/Currency/IsFist.asset` (icon_fist của Boss Rush, glyph 128 scale 1.5) cho dòng Power. Ảnh so: `_Concept/PvP/compare_{lobby,find,end}.png`.

**User chốt 2026-10-28 (bám gốc):** mockup có Season + bảng nhóm league + PROMOTION AREA → KHÔNG làm cơ chế đó; danh sách trong sảnh = leaderboard world gốc (top + dải "TOP 100" vạch xanh lá + quanh mình), dòng mình xanh. Nút Share (camera) → bỏ. Map trận nghĩa địa → giữ MainMap, chỉ đổi top bar thành "PvP" (UIMainLobby.LoadInfoMap, key UI.Events.PvP).

**Đã làm (sửa prefab qua MCP bằng ReskinPrefabUtil):**
- `UIPvPPopup` 816x1686: tên league ở header (txtRankName trỏ TitleText, RankNameText tắt), nút vuông xanh trái = Leaderboard, khiên (TopImage gốc bị xoay 45° → đã reset), thanh trophy ở chỗ "Season end in" + RankParent trong suốt phủ lên = nút Rewards (gốc bấm khung trophy), LeaderboardList (ScrollRect+RectMask2D, dòng RowTemplate + SeparatorTemplate), Battle + nút vé ads chồng cùng chỗ (1827). Code: `UIPvPPopup.LoadLeaderboard` + `UIPvPLeaderboardPopup.FillRows` (static dùng chung).
- `UIPvPFindOpponentPopup` 816x1352: khiên, TicketResetText ("Tickets refreshed in"), TicketBox box_tan "<IsTicket>4/5", dòng 746x154 có avatar, WinTrophyText "+N" trên nút, nút "Battle\n<vé>1". Code: `txtResetTime/txtTickets` + `ElementPvPPlayerUI.imgAvatar/txtWinTrophy`.
- `UIPvPEndPopup` 816x865: bỏ hình kiếm đầu popup, trophy (+/-) dưới header, nhãn Rewards trên ô tan, nút Claim (giữ chữ gốc, mockup ghi Continue). Defeat chữ đỏ (`defeatTitleColor`). Defeat dùng CHUNG bố cục Victory vì gốc có loseRewards.
- `ElementPvPLeaderboardUI`: thêm defaultSprite/highlightedSprite, imgAvatar, txtPower (popup Leaderboard đầy đủ chưa reskin dòng — vẫn tô màu cũ).
- Giữ chữ gốc ("#rank", "(You)", "Victory!", "Defeated", "Choose Opponent", "Battle") thay vì chữ mockup.

**CHƯA:** test Play với emulator (sảnh gọi thêm getPvPLeaderboard), popup Leaderboard + Rewards chưa theo kit dòng mới, commit.

Xem [[dungonrush-pvp-mode]], [[dungonrush-reskin-ui-howto]], [[dungonrush-reskin-boss-rush]], [[dungonrush-reskin-events]].
