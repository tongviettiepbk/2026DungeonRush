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
**Git:** code Clan nằm trong commit `7b0d56b` (chung PvP+Clan+Store, phiên khác commit); GD + memory commit riêng 2026-10-10. CHƯA push — user tự push.
**GD:** `server/CLAN_DESIGN.md` (15 mục, khuôn [[gd-doc-layout]]). Đọc file này TRƯỚC khi code tiếp Clan; mục [ĐỀ XUẤT] chưa được user duyệt. Mục 14: 10 việc chờ user quyết (số điểm/thưởng tự đặt, chống gian lận điểm, phí tạo clan client tự trừ, kick có chờ 24h, điểm người rời clan, tier có tụt, đối thủ PvP không đồ, thưởng chưa nhận, nút chuyển Leader, Day 2/4 thiếu mining) + 6 thứ cần chụp từ game gốc.
**Số [ĐỀ XUẤT] đang chạy (clanWarConfig.ts):** War Score ngày 1-5 = 1, ngày 6 = 2; vé PvP 5, thắng 1000 điểm, reset tối đa 2 lần, cần ≥30 người/clan (30 là gốc); tier theo điểm tier D0/C7/B18/A35/S60 (điểm tier = War Score, chỉ tăng); mốc cá nhân 1k→60k; thưởng clan D thắng 150/150/35/35 … S thắng 600/600/150/150 (thua = nửa); bot clan điểm = clan thật × 0,55–1,25.
**Độ chắc cần nhớ:** 3 Captain / 20 kết quả tìm / 5 đơn mỗi ngày là hằng số gốc (ik.vhu/vhy/via) nhưng Ý NGHĨA do mình suy → ghi [CHỜ SỐ LIỆU], đừng gắn [GỐC].
