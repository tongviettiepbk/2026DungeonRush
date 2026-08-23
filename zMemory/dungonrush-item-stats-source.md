---
name: dungonrush-item-stats-source
description: "Chỉ số item mặc lấy từ SAVE (PlayerPrefs current_user) trên emulator, KHÔNG phải APK/Firebase; kèm enum SubStatType + ItemType đã giải mã"
metadata: 
  node_type: memory
  type: reference
  originSessionId: b9924016-cb51-416b-a90a-9831e7c3603c
  modified: 2026-08-05T04:55:03.074Z
---

Chỉ số buff của item Dungeon Rush KHÔNG nằm trong data tĩnh (BackpackData/HelmetData... chỉ có tên/rarity/icon; chỉ Cape/Wing nhúng HealthBase/DamageBase/Scaler). Nguồn thật:

**SAVE người chơi** = PlayerPrefs key `current_user` (JSON ~12KB) ở:
`/data/data/com.lavalabs.dungeonrush/shared_prefs/com.lavalabs.dungeonrush.v2.playerprefs.xml`
(emulator LDPlayer14, adb `emulator-5554`, có root). Value URL-decode ra JSON.

`EquippedItems[]` mỗi món: `ItemId, ItemType(slot), Rarity, Level, SubStats[{Type,Value}]`.
**SubStats (các dòng "+X%") được LƯU SẴN giá trị đã roll** — không cần công thức. **Health/Damage chính (số to) KHÔNG có trong save** → tính runtime bằng công thức (ĐÃ GIẢI MÃ, verify khớp ảnh 14.44K & 1.12K):

**Main = Base(slot) × (√10)^Rarity × (1 + 0.015×Level)**  (√10=3.1622777=ItemStatTierScaler; 0.015=ItemStatLevelScaler)
Base mỗi slot: Helmet 45, Backpack 30, Necklace 20(Health) · WeaponMelee 9, WeaponRanged 7, Gloves 6, Ring 6(Damage). Helmet/Backpack/Necklace→Health, Weapon/Gloves/Ring→Damage. Level 1..100. Rarity = enum CharacterRarity 11 bậc (0..10): Common,Uncommon,Rare,Epic,Legendary,Mythic,Artifact,Ancient,Immortal,Divine,Ultimate (project Rarity enum ĐÚNG cho 0..9; đã thêm Ultimate=10). ĐỪNG đoán tên rarity — trích từ metadata.
SubStat roll: SubStatConfigs mỗi Type MaxValue (AttackSpeed40,BlockChance5,CritChance12,CritDamage100,Damage15,DoubleHit40,Health15,HealthRegen6,Lifesteal20,MeleeDamage50,RangedDamage15,CompanionCD7,CompanionDmg30), Weight đều 100, Spread 0.25. Số substat theo rarity: Common/Uncommon 0, Rare/Epic 1, Legendary→Celestial 2, Immortal/Eternal 3.

**Công thức roll GIÁ TRỊ 1 dòng substat — ĐÃ REVERSE XONG (2026-08-05) từ native libil2cpp v41** (KHÔNG còn là uniform): hàm `GameResources.jhh(1, MaxValue)` = **truncated normal** trên `[1, MaxValue]`, mean=(1+Max)/2, sigma=(Max−1)×Spread(0.25); x=mean+sigma×Z, loop reject tới khi ∈[1,Max], làm tròn 2 số lẻ (round(x×100)/100, banker's). Z chuẩn N(0,1) từ `jhi()` = **Box-Muller**: sqrt(−2·ln U1)·cos(2π·U2), U1,U2=Random.Range(0.0001,1). CHẶN DƯỚI là 1.0 (không phải 0). Biên = ±2σ nên giá trị dồn về giữa, hiếm chạm biên. Đã port vào GearStatCalculator.RollSubStatValue(maxValue, spread) + verify Monte-Carlo khớp ảnh in-game. Cách reverse: extract libil2cpp.so+global-metadata.dat từ xapk → Il2CppDumper (net7, DOTNET_ROLL_FORWARD=LatestMajor) ra script.json → capstone disasm VA hàm (GameResources$$jhh=0x27fdad4, jhi=0x27fdbf0) → dịch ARM64 float. Field SubStatDistributionSpread ở GameResources offset 0x70.

Hằng số ở MonoBehaviour **GameResources** (scene level, path_id 132). Cách trích: Il2CppDumper (build dotnet, net9 roll-forward)→DummyDll; UnityPy + TypeTreeGeneratorAPI, load_il2cpp(bytes), monkeypatch strip ".dll" ở get_nodes mới ra node. APK data: assets/bin/Data/*.split* phải cat ghép (sort -V); MonoScript ở globalgamemanagers.assets.

Enum `SubStatType` (giải mã từ global-metadata.dat v31, type#979, thứ tự khai báo = value 0..12; đã verify khớp ảnh Type2=Crit, Type4=Damage, Type3=CritDmg 73%):
0 AttackSpeed, 1 BlockChance, 2 CriticalChance, 3 CriticalDamage, 4 Damage, 5 DoubleHitChance, 6 Health, 7 HealthRegen, 8 Lifesteal, 9 MeleeDamage, 10 RangedDamage, 11 CompanionCooldown, 12 CompanionDamage.

ItemType(slot) enum (map qua ItemId→bảng): 0 Weapon, 1 Helmet, 2 Backpack, 3 Gloves, 4 Necklace, 5 Ring.

Game là il2cpp thuần (global-metadata v31), KHÔNG có DLL HybridCLR trên máy. Parse metadata: header 256B, 31 section pair(off,size); FieldDefinition **stride 12** (nameIdx,typeIdx,token); TypeDefinition 88B nhưng offset fieldStart≠b+32 (dùng tên type "SubStatType" fieldStart=7212 để định vị). Xem [[dungeonrush-config-format]], [[dungonrush-map-spawn-procedural]].
