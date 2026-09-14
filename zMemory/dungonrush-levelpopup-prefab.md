---
name: dungonrush-levelpopup-prefab
description: "Popup \"Rarity Table\"/Level = LevelPopup.prefab gốc, đã rewire vào project (layout, sprite chính xác)"
metadata: 
  node_type: memory
  type: project
  originSessionId: c27d6dfc-d776-4c4b-9edf-54a2a46d0379
  modified: 2026-09-06T10:48:57.607Z
---

Popup bảng rarity trong screenshot (crown "Level N", "Rarity Table", 10 dòng tier %, Rewards, progress bar) = **`LevelPopup.prefab`** gốc (không phải tự dựng). Bản mỏ tương tự: `MiningRarityPopup.prefab` (Ore/Stone...).

Đã rewire GỐC → `2026DungeonRushUnity/Assets/_Assets/Resources/Prefabs/UI/LevelPopup.prefab` (guid meta cố định `43484cda06917b7fcf1c6594d92a7fa1`):
- **Layout/toạ độ/text/hierarchy** = nguyên gốc (98 GO).
- **Sprite CHÍNH XÁC** (17 sprite, resolve từ xapk qua [[dungonrush-il2cpp-sprite-pipeline]]): tier bars=`frame-double` tint màu, panel/nút=`Button-White-Active1`, crown=`crown_icon_02`, X=`Icon_WhiteIcon_Close`, reward=`coin_icon/box_icon/gem`, dim=`BasicFrame_Circle_80_White`, pattern tier cao=`pattern_dot/spiral/star`+`BasicFrame_Round24`, arrow-down, duration bar=`BasicFrame_Rectangle01_l_White`. (`gem_icon` chưa tách → dùng `gem`.)
- **Component** UGUI/TMP + font → guid chuẩn project.
- **ĐÃ STRIP** 5 script logic gốc (class thật: `LevelPopup` controller, `LocalizedLabel`, `RarityPatternEffect`, `PressButtonUI`, entrance/open-anim) → layout thuần, CHƯA có logic điền %/XP/reward. Wire controller sau: đổ % từ ForgeData ([[dungonrush-loot-forge-design]]), XP từ `experience_required_per_level.csv`.

Text tier % hiện là placeholder gốc (`%20`, `Lv.1/Lv2`). Bản tự dựng `RarityTablePopup.prefab` (đoán sprite) ĐÃ XOÁ.
