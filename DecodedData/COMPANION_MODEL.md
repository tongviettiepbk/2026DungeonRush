# Companion (Pet) — Model unlock & summon (reverse từ game gốc v41)

Nguồn: reverse `libil2cpp.so` (Il2CppDumper v6.7.46 + capstone ARM64) từ `Dungeon+Rush_41_APKPure.xapk`,
đối chiếu `GameplayScene.unity` (AssetRipper), save `playerprefs.xml`, bảng `CompanionData`, localization.
Ngày: 2026-09-22.

## 1. Unlock — mở khoá theo PlayerLevel

Tab dưới cùng gate bằng `TabData.UnlockPlayerLevel` (class `TabData`, field 0x38), serialize trên từng
`TabBarElementUI` trong `GameplayScene.unity`. `TabBarUI` so `PlayerLevel >= UnlockPlayerLevel` để mở.

| Tab (TabIds) | UnlockPlayerLevel |
|---|---|
| Battle (0) | 0 (luôn mở, không có TabData) |
| Store (3) | 3 |
| Dungeon (2) | 4 |
| **Companion (5)** | **5** |
| Events (4) | 15 |
| Clan (6) | 20 |

> `PlayerLevel` = cấp tài khoản (bảng rarity 1–100), KHÔNG phải `Level`/progress campaign.
> Cờ save `IsCompanionTabUnlocked` KHÔNG phải điều kiện gate (getter/setter `UserController.dwp/dwq`
> không được code gọi trực tiếp) — nó chỉ là marker "đã hiện popup mở khoá". Gate thật = TabData.

## 2. Nguyên liệu = Bone (Xương)

- Dungeon **"Zombie Outbreak"** (`ZombieHorde`, DungeonData) có `RewardType = Bone` → nguồn chính.
- Save: field `Bone` (currency), `ZombieHordeDailyKeys`/`BonusKeys` (vé đánh dungeon/ngày).
- GameResources có kinh tế Bone: `DungeonBoneRewardBase/Scaler`, `BonePerMinute`, `BoneDropChance`,
  `BoneDropCountMin/Max`, `BonePerDrop`. (KHÔNG dùng Gem để summon companion.)

## 3. Summon — 3 cách, đều ra "thẻ" (card)

Summon cộng dồn `CardCount` vào `OwnedCompanions:[{CompanionId, Level, CardCount, IsNew}]`; đủ thẻ → lên
Level con đó. Trang bị tối đa 3 con (`EquippedCompanions`). Executor: `UserController.edi(int count)` — loop
`count` lần, mỗi lần roll rarity (có pity theo `TotalCompanionSummons`).

Hệ số chung: **SummonCapacity** = mastery `CompanionSummonCount` (type 7), default 0, unlock lvl1 = 60 gem,
Levels Value 1..5 (GemCost 60/120/160/200/250). Gọi `round()` (banker's rounding) trước khi dùng.

### 3a. Bằng quảng cáo (CompanionAdSummonPopup)
`count = min( round(SummonCapacity) + AdBonus + 11 , round(SummonCapacity) + 35 )`
(hàm `UserController.eby`; `AdBonus` = save `CompanionAdSummonBonusCount`). Giới hạn/ngày:
`CompanionAdSummonDailyCount` + `LastResetDate`.
→ Account có SummonCapacity=1, AdBonus=0: `min(1+0+11, 1+35) = 12`. **Ad = x12.** ✓

### 3b. Bằng Bone — nút NHỎ (SummonBoneSmallButton)
Thunk `gtj → gtk(cost=100, base=15)`. Số summon thực = `guc(15) = round(SummonCapacity) + 15`.
→ SummonCapacity=1: **100 Bone → 16 summon.** ✓

### 3c. Bằng Bone — nút LỚN (SummonBoneButton) + multiplier
Handler `gti`: `cost = wke[wjp] * 200`, `base = wke[wjp] * 35` (gse/gsf), rồi `gtk(cost, base)`.
`wke` = `readonly int[]` (field 0x210) — các bậc hệ số nhân, đổi bằng `ChangeSummonBoneMultiplierButton`
(index `wjp`). Số summon thực = `round(SummonCapacity) + base`.
→ SummonCapacity=1, multiplier x1: `cost=200`, `base=35` → **200 Bone → 36 summon.** ✓

## 4. Công thức chốt (guc — cộng SummonCapacity vào count Bone)
`CompanionTabPage.guc(int base) = round(MasteryValue(CompanionSummonCount)) + base`
- Bone nhỏ:  100 Bone → guc(15)
- Bone lớn:  (M×200) Bone → guc(M×35), M = wke[wjp]
- Ad:        eby (công thức 3a), KHÔNG qua guc

Với người chơi thực tế (SummonCapacity mastery = 1): ad **12**, Bone 100→**16**, 200→**36**.
Account mới tinh chưa mua mastery (=0): 11 / 15 / 35.

## 5. 18 companion / 6 rarity (CompanionData)
Common(3): Ember Fist, Magic Wool, Spark Mouse · Uncommon(3): Flame Wing, Life Horn, Snow Fang ·
Rare(3): Blaze Tail, Storm Eye, Frost Claw · Epic(3): Arcane Paw, Vital Root, Glacier Fist ·
Legendary(3): Echo Wing, Phantom Gaze, Star Feather · Mythic/Divine(3): Halo Hare, Radiant Talon, Celestial Mind.

## 6. Công thức SÁT THƯƠNG in-battle (reverse il2cpp v41)

Mỗi `CompanionType` map sang 1 strategy class riêng qua factory `lp$$gjt(type)` (switch/jump-table,
`Companion.gim` build & lưu vào field 0x48). Bảng type → strategy:

| type | tên | strategy | | type | tên | strategy |
|---|---|---|---|---|---|---|
| 0 DPS | kn | 0x2C40… | | 8 Guardian | ko | |
| 1 Healer | kq | | | 9 AoeSlower | kd | |
| 2 Slower | kx | | | 10 Blaster | kh | |
| 3 Lightning | kx (chung Slower) | | | 11 Meteor | (?) | |
| 4 ChainHealer | kl | | | 12 Siphon | kt | |
| 5 Bomber | kk | | | 13 HealNova | kt (chung Siphon) | |
| 6 MultiSlower | lh | | | 14 MirrorClone | System.Object (no-op?) | |
| 7 Beam | kf | | | 15 Immortality | kv | |

**DPS (`kn$$gfk`, thân hàm @0x2C408D0) — công thức áp cho projectile:**
```
damage = (DamageBase + DamageScaler × level) × (1 + CompanionDamageBonus/100)
```
- ASM: `ldp s9,s8,[data,#0x6c]` (s9=DamageBase 0x6C, s8=DamageScaler 0x70); `ldr s10,[comp,#0xD8]`
  (level); `scvtf`→(float)level; `fmul s1,s8,s1`=Scaler×level; `fadd s1,s9,s1`=Base+…; `Companion.bgoh`
  = `Character.CompanionDamageBonus(0xF4)/100`; `fadd s0,s0,#1`=1+bonus; `fmul s0,s0,s1`.
- **level dùng THẲNG (1-based, cap 100), KHÔNG phải (level-1).** DPS Ember Fist lv1 = 90+4.5×1 = **94.5**
  (chuỗi mô tả "<Value>=90" chỉ là base hiển thị, khác giá trị combat).
- `(1 + CompanionDamageBonus/100)` = hệ số CompanionDamage của chủ (Stats lưu dạng bội số 1+%).
- Healer/Lightning… (kq/kx) suy đoán cùng dạng `Base + Scaler×level` với Heal/Damage tương ứng —
  cần disasm kq/kx để chốt (chưa làm).

## 7. SAVE companion + NÂNG CẤP (reverse il2cpp v41)

**Save (trên class User):**
- `OwnedCompanions: List<CompanionModel>` (0x338) — `CompanionModel { string CompanionId(0x10); int Level(0x18);
  int CardCount(0x1C); bool IsNew(0x20) }`. Lên cấp bằng THẺ (CardCount), KHÔNG có XP.
- `EquippedCompanions: List<string>` (0x340) — trang bị bằng CompanionId (assetName), **tối đa 3**.

**Nâng cấp (class helper `ly`, thao tác companion level/card):**
- `ly$$gpx()` = **max level = 100** (`mov w0,#0x64`).
- `ly$$gpz(int level)` = **cards cần để lên cấp** = tra BẢNG `int[]` (index = level); level ≥ độ dài bảng →
  **fallback 16** (`mov w0,#0x10`). Bảng int[] nằm trong ScriptableObject "companion manager" (singleton
  `[0x58eb000+0xe40]`, field list +8), **CHƯA trích** → code tạm dùng fallback 16 cho mọi cấp.
- `ly$$gpu(int)` level-từ-threshold, `ly$$gpw/gpv(int)` → ValueTuple 6 float stat theo level (bảng khác, +0),
  `ly$$gpy(int)` → (int,int,bool) tiến độ card, `ly$$gqa(int,int)` can-upgrade, `ly$$gpt(int)` rarity.
- Executor summon cộng thẻ + auto lên cấp: `UserController$$edi(count)` (@0x283e6d4, chưa disasm chi tiết).

**Đã dựng trong project:** `CompanionModel`, `UserCompanionData` (owned/equipped + GetLevel/Own/AddCards/Equip),
`CompanionUpgradeConfig` (MAX_LEVEL 100 + fallback 16). CampaignMode nạp level pet từ save theo assetName.

## 8. SUMMON LEVEL + tỉ lệ rarity + bảng thẻ nâng cấp (reverse il2cpp v41, 2026-09-25)

Nguồn: `ly.cctor` (@0x2763C48) dựng 2 mảng static; các hàm `ly.gpu/gpy/gqb/gpt/gpz/gqa`; executor `UserController.edi`.

**Summon Level** — theo TỔNG lượt đã summon (save `TotalCompanionSummons`, User 0x348), KHÔNG theo Bone:
- `whm = (threshold, t1..t6)[100]`. `gpu(total)` = (index hàng đầu tiên có threshold > total) + 1, cap 100.
  → lv1: 0–49, lv2: 50–104, lv3: 105–164 … hàng 100 threshold = int.MaxValue (max).
- `gpy(total)` = (total − threshold[i−1], threshold[i] − threshold[i−1], isMax) → text/thanh tiến độ.
- `gpt(total)` roll rarity: `r = Random.Range(0,100)`, so cộng dồn từ Mythic (t6) xuống; t1..t6 = Common..Mythic.
- Bảng đầy đủ 100 hàng: `Scripts/Companions/CompanionSummonLevelConfig.cs`. Mốc tiêu biểu:
  lv1 100% Common · lv7 lần đầu ra Rare 0.2% · lv25 Epic 0.01% · lv50 Legendary 0.01% · lv75 Mythic 0.01% ·
  lv100 = 17.5/16.5/16.5/16.5/16.5/16.5.

**Executor `edi(count)`** — mỗi lượt: rarity = gpt(Total hiện tại) → `GameResources.jhm(rarity)` (random 1 con) →
chưa có: `new CompanionModel(id)` (Level 1, 0 thẻ, IsNew) · có rồi: `CardCount++` (KHÔNG tự lên cấp) → `Total++`.

**Thẻ nâng cấp `gpz(level)`** = `whn[level]`, `whn = int[15] {0,2,3,3,3,4,4,5,5,6,7,8,10,11,13}` (InitializeArray,
global-metadata offset 0x79DBC8); level ≤ 0 → whn[1]; level ≥ 15 → 16. `gqa(level, cards)` = cards ≥ gpz(level).
→ Nâng cấp THỦ CÔNG (thay cho ghi chú "auto lên cấp" ở §7).

**Quick Equip** — comparer `UserController.ep.dnc`: rarity giảm dần, cùng rarity thì Level giảm dần → lấy top 3.
