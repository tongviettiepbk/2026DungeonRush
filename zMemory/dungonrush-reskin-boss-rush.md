---
name: dungonrush-reskin-boss-rush
description: Reskin Boss Rush 2026-10-27 (đã commit 45f3816, chưa test Play) — 5 popup + HUD trận trong UIMainGame2 theo 4 mockup; kit Art/1_UIKit/BossRush; ChatGPT vẽ icon nắm đấm Power
metadata:
  type: project
  modified: 2026-10-27
---

Reskin Boss Rush 2026-10-27 theo mockup `ArtIncoming/_Concept/BossRush/` (4 demo: Join, sảnh, Rewards, trận; `_art/` 6 thanh/dòng). Ảnh so: `_Concept/BossRush/compare_*.png`, `popups_game.png`. Đã commit 45f3816, chỉ chụp preview (UICaptureTool), CHƯA test Play.

- Kit `Art/1_UIKit/BossRush/` sinh bằng `tools/reskin/gen_boss_rush.py`: hp_track/hp_fill (art 949), hp_fill_white (vệt trắng chạy trễ), row_gray/row_blue (art 983, bảng damage trận), row_gray_tall/row_blue_tall (dòng sảnh + bóng đáy 8px ghép bằng code), dmg_track/dmg_fill (art 985), pill_white (ô tối = tô đen alpha 0.53/0.3 bằng Image.color), icon_fist (ChatGPT, prompt `_Concept/BossRush/gen/`).
- Khung/header/nút X/nút xanh/ô tan = kit Mastery; Exit = Gameplay/btn_red. 5 popup: UIBossRushJoinPopup, UIBossRushPopup (+RewardsPanel trong đó, thêm Dim phủ tối), UIBossRushClaimPopup, UIBossRushEndPopup (2 cái cuối không có mockup → chỉ đồng bộ kit).
- HUD trận + bảng damage nằm trong PREFAB `Prefabs/UI/UIMainGame2.prefab` (BossRushUI, SafeArea/EventBlockerUI) — scene MainGame không override; HUD neo ĐỈNH (đừng để ReskinPrefabUtil.Place neo giữa).
- Code đổi: `ElementBossRushPlayerUI` / `ElementBossRushDamageUI` thêm `defaultSprite/highlightedSprite` (có thì đổi sprite xám↔xanh, màu trắng); `UIBossRushRewardsPanel` thêm `txtGuaranteedRewards` (thưởng nhóm trong ô tan, nhãn giữ) + `placementRowTemplate` (mỗi mốc hạng 1 dòng tan: TMP con 0 = hạng, 1 = thưởng).
- Sảnh: thêm TitleText "Boss Rush" (key Popup.BossRushJoin.Title) trên tên league trong header. Giữ của game mà mockup không có: đầu quỷ BossImage trên header, avatar trong dòng, icon huy chương ở nút Rewards (mockup để trống), đồng hồ ở HUD/ô giờ, "Your placement" cuối bảng thưởng. Mockup có mà bỏ: tên boss trên top bar trận (game giữ top bar lobby).
- Bảng damage: dòng 972x68, rowSpacing 75 (8 dòng vừa trên nút Exit).

Xem [[dungonrush-reskin-plan]], [[dungonrush-reskin-ui-howto]], [[dungonrush-boss-rush-model]], [[dungonrush-boss-rush-damage-board]].
