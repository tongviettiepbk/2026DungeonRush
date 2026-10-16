---
name: dungonrush-experience-ui-levelup
description: "Thanh exp đáy (ExperienceUI) + LevelPopup lên level theo gốc — exp chặn ở mức đầy, bấm Complete mới lên level; dựng + test Play 2026-10-16"
metadata:
  node_type: memory
  type: project
  originSessionId: 12ba3dd9-9a49-48a5-aa2e-90ca6ba494e5
  modified: 2026-10-16T02:21:56.452Z
---

Reverse il2cpp ExperienceUI / ExperienceController / LevelPopup (2026-10-16), đã port + test Play OK, chưa commit:
- **Luật gốc:** exp KHÔNG tự lên level. `PlayerExpService.Grant(amount, accumulateDisplay, applyMultiplier, storeOverflow)` = hdq: chặn exp ở ngưỡng, dư bị bỏ (trừ storeOverflow: offline + phần thưởng BossRush → để dành, đổ vào sau khi lên level). Đầy → nút Level xanh + chấm đỏ, LevelPopup tự bật nếu đang ở màn chính (không tab/Enchantment/Wing mở), không thì chờ `CloseAllTabs` (hdr).
- Cờ gốc: campaign/mảnh exp (true,true,false), bán đồ (true,true,false), offline + reward (false,false,true).
- **ExperienceUI** (`Scripts/UI/MainLobby/ExperienceUI.cs`, gắn ở UIMainGame2 BottomUI/ExperienceUI, `UIMainLobby.experienceUI`): tween slider 0.2s OutQuad, "cur/need" + " <color=#00FF00>(+N)</color>" ẩn sau 3s, "Lvl N", màu nút Completed(0.078,0.624,0.992)/Active(0.298 xám), BoostAmountText "xN". LevelButton + ButtonInvisible đều mở popup. Đã BỎ txtLevelPlayer/sliderProcessPlayer/txtProcess khỏi UIMainLobby.
- **UILevelPopup** (prefab `Resources/Prefabs/UI/UILevelPopup` do menu `Tools/DungeonRush/Build Level Popup` dựng từ LevelPopup.prefab rip; UIKey.LevelPopup): Complete → +gem `GetLevelUpGemReward(level+1)` → `CompleteLevelUp` → hàng rarity nảy từ Divine lên Common (0.2s, 1.07, OutBack 0.1, stagger 0.1, nháy nền Image_x) → `lobby.Refresh` + PlayGemFly. Max = nút xám "MAXED". Đang chờ Complete thì bấm nền không đóng. Hoa văn 3 hàng Ancient/Immortal/Divine chạy bằng RarityPatternEffect có sẵn (Scripts/UI/Common), builder gắn vào node .../Image(_1)/Pattern.
- Tutorial FTUE (LevelTutorialUI, step 2) chưa có hệ FTUE → ẩn. Số dạng rn.iqt: <1000 nguyên, lớn "F2"+K/M/B/T (ngưỡng/hậu tố đoán, chưa đọc hằng).
- Thay [[dungonrush-campaign-flow]] Phase 3 (AddExperience tự lên level ĐÃ XOÁ). Xem [[dungonrush-exp-reward-formula]], [[dungonrush-levelpopup-prefab]].
