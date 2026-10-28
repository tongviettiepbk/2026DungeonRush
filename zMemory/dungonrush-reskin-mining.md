---
name: dungonrush-reskin-mining
description: "Reskin Mining 2026-10-19 theo mockup _Concept/Mining (commit 075395e) — kit Mastery + ảnh art giao ở 1_UIKit/Mining, ChatGPT vẽ 7 icon quặng + icon Rarity, khối quặng = đất art + icon; nút dụng cụ đổi sprite; CẤM chạy lại Build Mining"
metadata:
  node_type: memory
  type: project
  originSessionId: d423d3cb-fd25-44ac-bd6f-13041216d6e4
  modified: 2026-10-18T17:33:03.007Z
---

Reskin Mining 2026-10-19 (đã commit 075395e, test Play OK: lưới/khối/đổi nút dụng cụ). Mockup `ArtIncoming/_Concept/Mining/` (Mining demo + Mining Offer demo, `_art/` 8 ảnh). Ảnh so: `_Concept/Mining/compare_mockup_vs_game.png`, `play_mining.png`.

- `tools/reskin/gen_mining.py` → `Art/1_UIKit/Mining/` (btn_square_gray = nút Rarity, btn_square_yellow = nút Offer, ore_slot, world_frame 870 viền đen 5, depth_track/fill, btn_gray cùng khuôn Mastery/btn_blue, badge_discount sao 8 cánh) + `5_Maps/Mining/stone_block.png` (khối Stone RIÊNG — KHÔNG ghi đè Campaign/stone_obstacle dùng chung) + khối quặng `<ore>_mine.png` = đất art 100px + icon 68%.
- ChatGPT (Codex, `_Concept/Mining/gen/`, ref = crop mockup): 7 icon quặng + `mining (1).png` (nút Rarity). Diamond = THẠCH ANH TÍM theo mockup, vẽ nền xanh lá rồi đảo màu → tách (`tools/reskin/key_mining_icons.py`). `*_character.png` = bản icon dụng cụ mới đợt Shop. Ghi đè qua ArtIncoming + Apply Reskin (18 ảnh).
- Prefab: UIMiningPopup khung 953x1686 theo mockup (lưới quặng 4 cột 178x52 cách 41x24, thứ tự Stone Coal Iron Emerald / Gold Diamond Ruby; mỏ 860; hàng dưới tối đen α0.78 (bù Linear); vạch độ sâu xanh #2AFF00); UIMiningOfferPopup (popup_frame, Gameplay/ribbon_offer, box_tan, Shop/reward_slot, btn_blue, badge_discount); UIMiningRarityPopup (kit Mastery, không có mockup).
- Code: UIMiningPopup.sprToolSelected/sprToolPassive — nút dụng cụ đang chọn = Mastery/btn_blue, còn lại Mining/btn_gray (thay tô màu DEFAULT/PASSIVE).
- Config: MiningConfig.BlockScale = 100/95 (khối 100px trên ô 95 như mockup), OreConfigs[7] Stone → stone_block; `_MiningGridMaterial` nền #EEA063, kẻ #E18F52; 2026-10-28 ô lưới = 1 ô gạch (_GridSize 1, _LineWidth 0.03, _Padding 0.006; _Offset 0.5 vì World ở toạ độ nguyên → kẻ nằm mép ô).
- KHÔNG chạy lại `Tools/DungeonRush/Build Mining` (MiningPrefabBuilder ghi đè prefab UI + config → mất reskin). Riêng mục Fix Ore Particles thì được.

Xem [[dungonrush-reskin-plan]], [[dungonrush-mining-system]], [[dungonrush-reskin-shop]].
