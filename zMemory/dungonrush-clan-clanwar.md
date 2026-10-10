---
name: dungonrush-clan-clanwar
description: Clan + Clan War đã dựng 2026-10-10 (reverse DecodedData/CLAN_MODEL.md, server clan.ts/clanWar.ts emulator PASS, client Scripts/Clan + UI/Clan + UI/ClanWar, builder tự nối field gốc); CHƯA test Play
metadata:
  type: project
---
**Gốc:** CLAN_MODEL.md. Prefab rip AssetRipper CÒN wiring field gốc → `tools/rip_fieldmap.py` → `clan_fieldmap.json`; class/field UI project ĐẶT TRÙNG TÊN GỐC (popup thêm tiền tố UI) để `Tools/DungeonRush/Build Clan UI Prefabs` (ClanPrefabBuilder) nối tự động — đã chạy sạch: 12 card (Prefabs/UI/Clan/Built), Resources/Prefabs/UI/ClanTabPage + 13 UIClan*/UIClanWar* popup, ClanBannerCatalog.asset (thứ tự gốc tables/ClanBannerCatalog.names.json).
**Server:** clan.ts 13 endpoint, clanWar.ts (state/record/pvp ngày 6/claim/leaderboard, cron ghép T3 00:05 + admin matchclanwarsnow/seedclanwarpoints), số điểm/thưởng [SUY] ở clanWarConfig.ts. `node functions/scripts/clanEmulatorTest.mjs` PASS.
**Client:** ClanController/ClanWarController/UserClanData(key_user_clan); UITabClan (chuyển UI/Common→UI/Clan, giữ guid) nạp ClanTabPage từ Resources; PvP ngày 6 dùng PvPMode qua PvPBattleSession.BeginClanWar; hook điểm ở UIMainLobby loot, CompanionService/CapeService.Summon, DungeonService.ConsumeKey, UserPlayerData.AddExperience.
**CHƯA:** test Play (lần thử dừng vì lobby chưa load — đợi lâu hơn rồi uiLobby.OpenTab(Clan)); soát layout/hình; avatar; mining chưa có nguồn.
