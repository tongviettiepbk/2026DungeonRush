---
name: dungonrush-enemy-stats-formula
description: "Công thức GỐC chỉ số enemy (damage/health theo màn) reverse từ il2cpp — Lancaster power split, đã verify"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 77eac08e-6c70-4865-812e-1d65f8dcd01c
  modified: 2026-09-06T16:07:53.268Z
---

Chỉ số enemy game gốc KHÔNG phải bảng tĩnh mà tính runtime bằng mô hình **Lancaster power split**. Đã reverse thân hàm thật từ `libil2cpp.so` (bản 41) + đọc giá trị field `GameResources` bằng typetree. Doc đầy đủ: `DecodedData/ENEMY_STATS_MODEL.md`; calculator: `DecodedData/enemy_calc.py`; giá trị field: `DecodedData/gameresources_values.json`.

**Chuỗi** (entry `LevelController.hpv` → `jgm` → `EconomyController.hcm`):
1. `combatLevel = base + (dungeonLevel-1)×3`  (base: Dragon 1 / Zombie 5 / Cultist 60) — `jgm`
2. `totalArmyPower = 500 × 10^hck(combatLevel)`  — `hcj`; ArmyPowerBase=500, scaler=√10
   `hck(L)`: L≤80→L/20; 80<L≤200→4+(L-80)/30; L>200→8+(L-200)/40  (ArmyPowerSegments)
3. `perUnitPower = totalArmyPower / unitCount^Lancaster`  — `hcm` (unitCount+Lancaster từ ArmyPreset)
4. Mỗi unit, ratio r (Melee 3 / Ranged 2 / Boss 10 = `jgw`):
   `damage = round(√(perUnitPower/r)) × (0.8 nếu Ranged)`; `health = round(√(perUnitPower×r))`; min 1.
   ⇒ bất biến `health/damage = r`, `damage×health = perUnitPower`.

Preset theo dungeon: Zombie 6/8/10/12 melee (Lanc 1.0); Cultist mix melee+ranged (Lanc 1.5). `jha`/`jgz` chỉ chọn weapon HIỂN THỊ + tier, KHÔNG ảnh hưởng số damage/health.

VD Zombie: L1(6M)→dmg7/hp21; L20(10M)→145/435; L100(12M)→702k/2.1M. Dragon boss L1→7/75.

CAMPAIGN ≠ DUNGEON (user xác nhận đã chơi gốc): campaign là mạch LIÊN TỤC (1-1..1-10,2-1=level11...), combatLevel = level THÔ (KHÔNG base+×3), CHỈ lính melee/ranged (KHÔNG dragon/cultist/boss). Preset = jgx: level≤10 → ManualPresets[level-1]; level>10 → ArmyPresets Fisher-Yates bằng System.Random(42+(n-1)/9), n=level-10. Dungeon (Zombie/Dragon/Cultist) là chế độ RIÊNG dùng jgm base+×3 + preset dungeon.

ĐÃ WIRE VÀO CODE (2026-09-06, compile sạch): `EnemySpawnGenerator.cs` viết theo NHÁNH CAMPAIGN (ManualPresets+ArmyPresets, level=globalStage, có class NetRandom bản Mono khớp shuffle); `EnemySpawnInfo` thêm `attackRange`+`isRanged`; `EnemyUnit.SpawnEnemy` dùng attackRange theo role. VD: 1-1 = 1 lính 14/41; 1-8 = 3M3R mỗi con 5/16 (6 con chia power). KHÔNG còn boss stage / dungeon mapping. Compile-check: dùng csc bundled Unity 6.3 (`Editor/Data/DotNetSdkRoslyn/csc.dll`) chạy bằng runtime `Editor/Data/NetCoreRuntime/dotnet.exe` (máy KHÔNG có .NET SDK/6); response file gom Compile+HintPath+ProjectReference(spine từ Library/ScriptAssemblies) từ Assembly-CSharp.csproj. Pipeline reverse: xem [[dungonrush-reverse-native-il2cpp]] và [[dungonrush-il2cpp-sprite-pipeline]] (Il2CppDumper self-contained bản win, .NET runtime máy chỉ có 3.1/5.0). Xem [[dungonrush-item-stats-source]] cho jgu.
