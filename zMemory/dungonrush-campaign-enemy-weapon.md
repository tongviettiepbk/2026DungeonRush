---
name: dungonrush-campaign-enemy-weapon
description: "Quái campaign cầm VŨ KHÍ NGƯỜI CHƠI theo seed + tier (jha/jgz/jhd gốc) — đã port EnemyGearPicker 2026-10-21, chưa test Play"
metadata:
  node_type: memory
  type: project
  originSessionId: 6e5f8f71-cc2f-4503-bdc1-eecfe62105ed
  modified: 2026-10-21T15:55:25.559Z
---

Quái campaign gốc KHÔNG có vũ khí riêng: `SpawnController.huy` → `GameResources.jha` chọn 1 vũ khí của người chơi
(rarity = tier − 1, đúng loại melee/ranged, seed `L×997 + (ranged?7:3) + 12345×31`). Level 1–4 lính cận chiến tay không.
Chỉ có vũ khí, không mũ/găng. Quái dungeon/boss mới dùng đồ riêng (GearsEnemy + theme).

Port 2026-10-21: `Scripts/GamePlay/Map/EnemyGearPicker.cs` (giữ thứ tự 59 vũ khí gốc trong WEAPON_ORDER — thứ tự quyết định
món nào được chọn); `EnemySpawnInfo.overrideWeapon/weapon` (chỉ campaign set); `EnemyUnit.weaponRenderer` = weapon_slot
của 00Enemy, hình qua EquipVisualResolver. Test edit mode khớp mốc tier; CHƯA test Play (hình cầm vũ khí trên rig vịt).

**Why:** user yêu cầu làm đúng gốc ([[dungonrush-follow-original-strict]]). Doc: DecodedData/ENEMY_STATS_MODEL.md mục 4.
**How to apply:** đừng gán weaponData cố định cho 00Enemy; dungeon variants vẫn dùng weaponData prefab. Xem [[dungonrush-enemy-stats-formula]].
