# Mô hình chỉ số ENEMY — công thức GỐC (reverse từ libil2cpp.so bản 41)

> Nguồn: dump native `libil2cpp.so` (Il2CppDumper) + đọc field serialized của `GameResources`
> bằng typetree (UnityPy + TypeTreeGenerator). Không phải suy đoán — là thân hàm thật.
> Các hàm (tên obfuscate 1 chữ) và giá trị field đều verify trực tiếp trên binary.

## 0b. CAMPAIGN vs DUNGEON — 2 NHÁNH KHÁC NHAU (quan trọng)

`LevelController.hpv` có 2 nhánh (theo cờ trên GameController):
- **CAMPAIGN (main)**: `combatLevel = level THÔ` (KHÔNG có base+×3). Level LIÊN TỤC 1-1=1..1-10=10,
  2-1=11... Preset = `jgx(level)`: level≤10 → **ManualPresets[level-1]**; level>10 → **ArmyPresets**
  xáo trộn (`jhe`). CHỈ lính melee/ranged, KHÔNG dragon/cultist, KHÔNG boss.
- **DUNGEON (Zombie/Dragon/Cultist)**: `combatLevel = jgm(level, type) = base+(level-1)×3`, preset riêng
  từng dungeon (mục 1 & 5 dưới). Đây là chế độ chơi RIÊNG, không phải campaign.

Campaign presets:
- ManualPresets (L1..L10): 1M / 2M / 1R / 2R / 1M1R / 1M2R / 2M2R / 3M3R / 5R / 5M (Lancaster tương ứng).
- ArmyPresets (pool level>10): 1M,1R,1M1R,3M,3R,6M,6R,2M2R,3M3R. `jhe(n)` (n=level-10):
  Fisher-Yates ArmyPresets bằng `new Random(PresetSeed=42 + (n-1)/9)` rồi lấy phần tử `(n-1)%9`.

## 0. Tổng quan chuỗi tính DUNGEON (entry: `LevelController.hpv(level)`)

```
dungeonLevel  ──jgm──►  combatLevel  ──hcj──►  totalArmyPower
                                          │
preset(dungeonLevel) ─► unitCount, Lancaster
                                          ▼
        perUnitPower = totalArmyPower / unitCount ^ Lancaster
                                          ▼
   mỗi unit (role → ratio r):  damage = round(√(perUnitPower / r)) × (0.8 nếu Ranged)
                               health = round(√(perUnitPower × r))     (đều min 1)
```

`hpv` gọi `jgm(level, dungeonType)` ra combatLevel rồi `EconomyController.hcm(units, combatLevel, preset)`.

## 1. combatLevel — `GameResources.jgm(dungeonLevel, dungeonType)`

```
combatLevel = base + (dungeonLevel - 1) × 3
```
| Dungeon | base (LevelBase) | mult |
|---|---|---|
| DragonBoss (0) | 1  | 3 |
| ZombieHorde (1) | 5  | 3 |
| Cultist (2)     | 60 | 3 |

## 2. totalArmyPower — `EconomyController.hcj(combatLevel)`

```
totalArmyPower = ArmyPowerBase × ArmyPowerExponentialScaler ^ (2 × hck(combatLevel))
              = 500 × 10 ^ hck(combatLevel)          (vì scaler = √10 ⇒ scaler^(2x)=10^x)
```

`hck(L)` = tích phân 1/LevelScaler theo `ArmyPowerSegments` (Threshold, LevelScaler):
```
segments: (80, 20) (200, 30) (999999, 40)
  L ≤ 80        : hck = L/20
  80 < L ≤ 200  : hck = 4   + (L-80)/30
  L > 200       : hck = 8   + (L-200)/40
```

## 3. Chia sức mạnh cho từng unit — `EconomyController.hcm`

```
unitCount   = preset.MeleeCount + preset.RangedCount          (ArmyPreset.jfk)
perUnitPower = totalArmyPower / unitCount ^ preset.LancasterCoefficient
```
Mỗi unit theo role, ratio r = `GameResources.jgw(role)`:
| role | ratio r |
|---|---|
| Melee (0/khác) | 3 |
| Ranged (1) | 2 |
| Boss (4) | 10 |

```
damage = max(1, round( √(perUnitPower / r) × (0.8 nếu Ranged) ))   # RangedUnitDamageMultiplier=0.8
health = max(1, round( √(perUnitPower × r) ))
```
Bất biến: `health / damage = r`, và `damage × health = perUnitPower`.
(round = banker's rounding, đúng `Math.Round` C#.)

## 4. Vũ khí/tier hiển thị — `jha`/`jgz` (KHÔNG ảnh hưởng số damage/health)

`jha(combatLevel, role)` chọn 1 WeaponData ngẫu nhiên có seed cố định để enemy cầm (chỉ hình
dạng + kiểu đánh cận/xa). Damage/HP thật hoàn toàn do `hcm` (Lancaster) quyết định.
- Seed: `System.Random(combatLevel×997 + roleConst + EnemyGearSeed×31)`, EnemyGearSeed=12345.
- Tier gear `jgz`: `EnemyGearTierWeights=[5,15,25,25,25,25,30,30,40,40,40]` (cộng dồn: 5,20,45,70,95,120,150,180,220,260,300), `EnemyGearMaxCombatLevel=300`. Tier = mốc cộng dồn đầu tiên vượt combatLevel.

## 5. Preset theo dungeon (ArmyPreset: MeleeCount, RangedCount, LancasterCoefficient)

- **ZombiePresets** (toàn melee): 6M / 8M / 10M / 12M  (Lancaster 1.0) — chọn `jgn(level)` = `[(level-1) % 4]`
- **CultistPresets** (melee+ranged, Lancaster 1.5): 2M3R / 3M4R / 3M3R / 3M2R / 4M3R — chọn `jgo(level)` = `[(level-1) % 5]`
- **ManualPresets / ArmyPresets**: dùng cho progression chung/PvP; chọn theo level qua `jgx`.

`hpy(level)` chọn preset cho stage (index theo level trong list preset của dungeon).

## 6. Chỉ số nền khác (GameResources)

```
EnemyBaseMoveSpeed = 1.5     EnemyBaseMeleeAttackDistance = 1.5
EnemyBaseRangeAttackDistance = 3.0   EnemyDetectionRange = 4.0
```

## 7. Item/gear stat (dùng cho gear người chơi & tham chiếu) — `GameResources.jgu`

```
stat = base(slot) × ItemStatTierScaler^tier × (1 + ItemStatLevelScaler × level)
ItemStatTierScaler = 3.16227770 (√10)   ItemStatLevelScaler = 0.015
base: MeleeWeapon=9 RangedWeapon=7 Gloves=6 Helmet=45 Backpack=30 Necklace=20 Ring=6
```

## 8. Bảng ví dụ (tính bằng scratchpad/enemy_calc.py)

ZombieHorde (melee) — SỬA 2026-10-02: preset = `jgn(level)` = ZombiePresets[(level-1) % 4]
(reverse `ru.irk`), số con xoay vòng 6/8/10/12 theo level (bảng cũ ghi số con sai):
| dLvl | combat | units | damage | health |
|---|---|---|---|---|
| 1  | 5   | 6  | 7      | 21 |
| 2  | 8   | 8  | 7      | 22 |
| 3  | 11  | 10 | 8      | 23 |
| 4  | 14  | 12 | 8      | 25 |
| 5  | 17  | 6  | 14     | 42 |
| 10 | 32  | 8  | 29     | 86 |
| 20 | 62  | 12 | 132    | 397 |
| 50 | 152 | 8  | 7,234  | 21,702 |
| 100| 302 | 12 | 701,995| 2,105,984 |

CultistRitual (2026-10-02, reverse `rq.irg`): preset `[(level-1) % 5]`, melee r=3 / ranged r=2 (dmg ×0.8), Lancaster 1.5:
| dLvl | combat | preset | melee dmg/hp | ranged dmg/hp |
|---|---|---|---|---|
| 1 | 60 | 2M3R | 122/366 | 120/299 |
| 2 | 63 | 3M4R | 113/338 | 110/276 |
| 3 | 66 | 3M3R | 150/451 | 147/368 |
| 4 | 69 | 3M2R | 205/615 | 201/502 |
| 5 | 72 | 4M3R | 189/568 | 185/464 |
| 10 | 87 | 4M3R | 392/1,177 | 385/961 |
| 20 | 117 | 4M3R | 1,241/3,723 | 1,216/3,040 |
| 50 | 207 | 4M3R | 36,694/110,083 | 35,953/89,883 |

DragonBoss — SỬA 2026-10-01: rồng là role **Range (1)**, KHÔNG phải Boss (4).
`rs.irj` tạo đúng 1 `sx{id=DragonBossCharacterId 999999, role=1, combatLevel=jgm(lv,0)}`;
preset `hpy` nhánh Dragon = `new ArmyPreset{Melee 0, Ranged 1, Lancaster 1.0}` → r = 2, damage ×0.8:
| dLvl | combat | dmg | hp |
|---|---|---|---|
| 1 | 1 | 13 | 33 |
| 5 | 13 | 27 | 67 |
| 10| 28| 63 | 158 |
| 20| 58| 357 | 891 |
| 50|148|17,195|42,987 |
(Bảng cũ "ratio 10: 7/75, 35/354..." là SAI.)
```
```
