---
name: dungonrush-pvp-mode
description: PvP Arena ĐÃ dựng (2026-10-10) — reverse gốc ở DecodedData/PVP_MODEL.md; server pvp.ts (emulator PASS) + client Scripts/PvP + UI/PvP + 5 prefab UIPvP*; Play test thắng/thua OK; thưởng win/lose CHƯA có số gốc
metadata:
  node_type: memory
  type: project
  modified: 2026-10-09T18:50:32.890Z
  originSessionId: 26bc42f5-487a-4358-9838-a306be66dc37
---

**Gốc (reverse v41, CHẮC):** PvP bất đồng bộ — đánh với SNAPSHOT đồ+pet người khác. Trophy Elo: người mới 1000, 10 league (PvPConfig: Iron 0-1199 KWin40/KLoss-10 … Legend 2800+); E=1/(1+10^((opp-me)/400)), thắng max(1,RoundToEven(KWin(1-E))), thua min(-1,Round(-|KLoss|E)), K theo league trophy MÌNH. Vé 5 free + 4 ads/ngày. Trận 30s; đối thủ phe Away ở hàng trên lùi PvPEnemyYOffset=2, mặc snapshot, PET đối thủ cũng spawn (khác Boss Rush); thắng khi hero đối thủ chết, thua khi hero mình chết/Exit; hết giờ so Σ(HP/MaxHP) hero mỗi phe, hoà=thắng. Mình block được đòn đối thủ, đối thủ KHÔNG block đòn mình (Character.ewb). Map = MainMap không box. Đối thủ sắp theo winDelta giảm dần. Leaderboard tab world/country, "#rank", " (You)". ResetText "Tickets refreshed in" gốc KHÔNG bật (prefab để tắt). Thanh trophy "{trophy}/{MaxTrophy league}".
**Server** `server/functions/src/pvp.ts`+`pvpConfig.ts`: openpvp/findpvpopponents/startpvpbattle/reportpvpbattle/grantpvpadticket/getpvpleaderboard/initpvpprofile + admin seed/remove bot. Firestore `pvpPlayers/{uid}`, thưởng `config/pvp.rewardTable` (Type = TÊN RewardType). Giả định (server gốc giấu): roster 5 người gần trophy, TTL 600s, token 180s, đối thủ phòng thủ cũng đổi trophy. `npm test` + `scripts/pvpEmulatorTest.mjs` PASS.
**Client:** `Scripts/PvP/` (PvPDTOs, StaticPvPData, UserPvPData key_user_pvp, PvPController+PvPBattleSession, PvPMode, Editor/PvPPrefabBuilder menu Tools/DungeonRush/Build PvP UI Prefabs), `Scripts/UI/PvP/` 7 script; prefab Resources/Prefabs/UI/UIPvP{,FindOpponent,End,Rewards,Leaderboard}Popup + Game Modes/PvPMode. HUD = UIMainLobby.objPvpUI/txtTimePvp (có sẵn scene), EventBlocker Exit → PvPMode.GiveUp. TMP icon mới ở _ResourceGame/Currency: IsTrophy/IsPower/IsAd/IsRedKey/IsGreenKey/IsCrown/IsExp (metrics từ rip Resources/spritesheets). PetUnit theo tag chủ (phe B = AI tự ra chiêu); MirrorClone theo phe+đồ chủ; đối thủ phe B không nhận mastery của mình. BossRushController.GrantRewards thêm Cloak/Lootbox/key dungeon.
**CHƯA:** số thưởng win/lose thật từng league (cần ảnh popup PvP Rewards gốc); PvPPlayerPopup (xem đồ đối thủ) chưa có prefab; avatar/country flag; nhánh hết giờ chưa test Play; tab selector leaderboard đã sửa sang LateUpdate nhưng chưa chụp lại.
Xem [[dungonrush-boss-rush-model]], [[dungonrush-events-tab]].
