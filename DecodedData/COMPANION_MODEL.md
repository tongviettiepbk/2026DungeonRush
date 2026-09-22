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
