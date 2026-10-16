---
name: dungonrush-battle-pass
description: Battle Pass dựng theo gốc 2026-10-17 (CHƯA test Play) — mốc 20+5k theo HighestLevel campaign, Free/Premium, pool thưởng gốc; doc DecodedData/BATTLE_PASS_MODEL.md
metadata:
  type: project
---

Dựng 2026-10-17, commit 64ef2be, USER CHƯA TEST Play.
- Logic: `Scripts/BattlePass/BattlePassService.cs` (bảng LevelRewards gốc nhúng trong code, enum BattlePassRewardType = RewardType gốc 0..11). HighestLevel = level của màn kế màn cao nhất đã qua (max với màn hiện tại).
- UI: `UI/BattlePass/` UIBattlePassPopup + BattlePassRewardCellView + BattlePassLevelView + BattlePassButtonView (node UIMainGame2/SafeArea/BattlepassButton). Builder `Tools/DungeonRush/Build Battle Pass UI/Battle Pass Popup`.
- Save: UserShopData.isPremiumBattlePassBought / claimedFree/PremiumBattlePassRewards; sản phẩm `lootio.battlepass` (giá $9.99 [SUY]).
- Lệch có chủ đích: bố cục ô đặt tuyến tính theo padding/spacing gốc, không dùng pool 30 ô; icon Cloak / key Cultist chưa có TMP sprite asset nên hiện số không icon.
Xem [[dungonrush-shop-system]], [[dungonrush-follow-original-strict]].
