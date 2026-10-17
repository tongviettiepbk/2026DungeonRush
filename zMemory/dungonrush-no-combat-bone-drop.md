---
name: dungonrush-no-combat-bone-drop
description: Game gốc v41 KHÔNG rơi Bone (và rương) từ quái — nhánh combat_drop là code chết; đừng port. Doc ở DecodedData/LOOT_DROP_MODEL.md
metadata:
  type: project
---

Reverse 2026-10-18: `LootDropController.hsn` chỉ được gọi với type 2 (mảnh EXP, từ `hsm`) và type 1 (rương, từ `hsl` — chỉ khi có `CreativeDropSettings` của màn creative quay quảng cáo, build thật không có). `GameResources.BoneDropPrefab = null`, `EnableBoneUI = 0`; field BoneDropChance/CountMin/Max/BonePerDrop/BonePerMinute không ai đọc. Code thu Bone `combat_drop` (hsy, qi.hsh) không bao giờ chạy.

Nguồn Bone thật của gốc: dungeon Zombie (Base 100 + Scaler 2), BattlePass, ClanWar, IAP/deal. Tiêu Bone: summon pet VÀ `ForgeUpgradePopup.igt` (nâng cấp Forge tốn Bone, analytics `forge_upgrade`).

**Why:** session trước nghi thiếu Bone do chưa port combat_drop — sai, gốc tắt.
**How to apply:** không thêm Bone rơi từ quái; nếu kinh tế Bone thiếu thì xét các nguồn thật ở trên. Xem [[dungonrush-companion-unlock-summon]], [[dungonrush-exp-shard-drop]], [[dungonrush-follow-original-strict]].
