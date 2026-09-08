---
name: dungonrush-campaign-flow
description: Trạng thái implement luồng campaign win/lose + hệ exp/playerLevel (rarity table)
metadata: 
  node_type: memory
  type: project
  originSessionId: 93d5ec13-cae4-47da-8bc3-ea944554097d
  modified: 2026-09-07T17:10:20.275Z
---

Luồng campaign đang dựng theo mô tả user (2026-09-07): win = giết hết quái → lên màn kế; lose = hero chết → đánh lại màn hiện tại; exp từ thắng → lên level → bảng rarity tốt lên + thưởng.

**Phase 1 XONG (compile sạch):** `CampaignMode.CalculateResult(isWin)` override — win → `campaign.PassStage(stageId)` + `GameData.Save(true)`; thua → giữ curStageId; cả 2 reload màn sau `delayEndGame` giây (coroutine, tránh hủy unit giữa lúc dispatch UnitDie). Win/lose detect sẵn ở BaseMode (hết teamB=win, hết teamA=lose).

**Cấu trúc data (đã sửa cho khớp data gốc):**
- Module MỚI `UserPlayerData` (Scripts/Player/): `playerLevel`(1-indexed) + `playerExperience`. Key `key_user_player`. Đây LÀ level ở popup "Rarity Table" (vương miện Level N). Đã nối vào UserData (const/property/listData/Load).
- `UserCampaignData`: chỉ còn `curStageId`/`passedStageId` (~ User.Level/HighestLevel). **ĐÃ BỎ field `forgeLevel`.**
- Rarity table index = `playerLevel - 1` (row 0-based). `UIMainLobby.OnClickLoot` giờ dùng `player.playerLevel-1`, không còn `campaign.forgeLevel`. Xem [[dungonrush-exp-reward-formula]].

**Phase 2 XONG (compile sạch):** `StaticExperienceData` (Scripts/Player/, NHÚNG bảng 100 dòng + hằng 49/1, GetXpRequired/GetStageExp) đăng ký `experience` trong StaticGameData. `StaticCampaignData.GetLevel(stageId)` (dùng chung EnemySpawnGenerator). `UserPlayerData.AddExperience(amount)` → cộng exp + while lên playerLevel (cap MaxLevel=100), trả levelsGained. `CampaignMode.CalculateResult(win)` → AddExperience(GetStageExp(GetLevel(stageId))) trước PassStage.

**Phase 3 ĐANG DỞ:** controller `UILevelPopup.cs` (Scripts/UI/, :BaseUI) ĐÃ viết + compile — hiện Level N, thanh exp, bảng rarity Lv N vs N+1 (từ StaticForgeData row level-1 & level). CÒN LẠI (cần Unity, UnityMCP đang ngắt): (1) gán field serialized vào prefab Resources/Prefabs/UI/LevelPopup.prefab trong Inspector (rows Common..Divine → CommonCurrentRate/CommonNextRate; Header CurrentLevelText/NextLevelText; Slider exp; CloseButton); (2) gọi Show khi levelsGained>0 + chặn reload tới lúc đóng popup; (3) **data THƯỞNG mỗi level chưa có** (ảnh: +5 gem; bản reverse không lộ bảng reward level-up → cần reverse tiếp hoặc user chốt). Xem [[dungonrush-levelpopup-prefab]].

**Đính chính:** enum `Rarity` rebuild ĐÃ ĐÚNG (Common..Mythic, Artifact, Ancient, Immortal, Divine, Ultimate) — khớp popup game. KHÔNG có lỗi tên tier (trước nhầm với comment tên-cột-forge Divine/Celestial/... trong StaticForgeData — đó chỉ là tên cột nội bộ, không phải tên hiển thị).
