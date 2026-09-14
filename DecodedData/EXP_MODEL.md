# EXP / Level model — reverse từ libil2cpp.so (APK bản 41)

> Nguồn: disasm ARM64 `ExperienceController` + `LootDropController` trong `libil2cpp.so`
> (đồ nghề `tools/il2cpp_reverse/`). Giá trị hằng: `gameresources_values.json` + `remote_config_live.json`.
> Đây là REFERENCE bất biến (giống ENEMY_STATS_MODEL.md), không sửa theo refactor code.

## Hằng số (GameResources / remote config)
- `ExperienceLevelBase`   = 49   (field @0x310)
- `ExperienceLevelScaler` = 1    (field @0x314)
- `experience_required_per_level` = bảng 100 dòng (xem `tables/experience_required_per_level.csv`)
  → **XP để LÊN 1 level** (không cộng dồn): lv1=200, lv2=600, lv3=1200, ... về sau +1800/level.

## Hàm đã map (obfuscate → ý nghĩa)
| Hàm | VA | Ý nghĩa |
|---|---|---|
| `ExperienceController.hdk(level)` | 0x278634C | = `experience_required_per_level[clamp(level-1, 0, 99)]` — ngưỡng XP để lên level đó. Null-safe fallback = 100. |
| `ExperienceController.hdl()` | 0x27865F8 | = `min(1, curExp / hdk(curLevel))` — % thanh exp (0..1). |
| `ExperienceController.hdm(level)` | 0x2786690 | **= round(ExperienceLevelBase + ExperienceLevelScaler × level)** — TỔNG exp cả màn khi clear sạch. Round = banker's (ToEven). |
| `ExperienceController.hdn(level, count)` | 0x2786790 | Chia `hdm(level)` cho `count` enemy: mỗi con = `floor(total/count)`, `count%total` con đầu +1 (dùng counter `wre` xoay vòng), min 1. → **exp mỗi con quái**. |
| `ExperienceController.hdq(amount, ...)` | 0x2786808 | AddExperience (cộng vào exp gốc + xử lý lên level). Caller: `OfflineEarnningPopup` (đường offline). |
| `LootDropController.hsm(...)` | 0x27A0B28 | Rơi drop mỗi khi 1 enemy chết; gọi `hdn(level, enemyCount)` với `level` = field 0x40 của level-model, `count` = số enemy trong list. |

## Công thức chốt
```
stageExp(level)      = round(49 + 1 × level)          # clear sạch màn
perEnemyExp(level,n) = phân phối stageExp cho n con    # tổng = stageExp
```
- Vì `hdn` phân phối đủ `stageExp` cho `n` con → **clear hết quái = nhận đúng `stageExp`, độc lập số quái**.
- **Thua không cộng** (drop exp chỉ khi thắng/clear).

## Đối chiếu dữ liệu thật (validate)
Với `level = (world-1)×10 + stage` (**10 màn/world**), exp = round(49 + level):

| Màn | level | round(49+level) | Đo in-game |
|---|---|---|---|
| 5-2 | 42 | 91 | 91 ✅ |
| 5-3 | 43 | 92 | 92 ✅ |
| 5-4 | 44 | 93 | 93 ✅ |
| 5-5 | 45 | 94 | 94 ✅ |

4 điểm liên tiếp khớp tuyệt đối (slope=1=Scaler, intercept=49=Base). Đây cũng chứng minh **exp phụ thuộc LEVEL của màn, không phụ thuộc số quái** (5-3 & 5-4 chỉ 1 quái vẫn cho 92/93).

> Data point cũ "2-5 = 60" bị lệch (10 màn/world thì 2-5 = level 15 → 64). Bỏ, dùng 4 điểm liên tiếp ở trên.

## playerLevel ↔ bảng Rarity Table (xác nhận bằng UI in-game)
Popup "Rarity Table" (vương miện **Level N**) = bảng `forge_rarity_probabilities` tại **row (N-1)**:
- Level 9 → row 8: Common 70.39 / Uncommon 28 / Rare 1.56 / Epic 0.05 (khớp UI).
- Cột "Lv 10" preview → row 9: 65.92 / 32 / 2 / 0.07 (khớp UI).
- Thanh exp popup `9.41K/12.60K`, `12.60K = experience_required_per_level[9]`.

⟹ **Chỉ MỘT con level**: thắng campaign → +exp → đầy ngưỡng → `PlayerLevel++` → bảng rarity tốt lên + thưởng (ảnh: +5 gem). Đây LÀ cái level ở popup, KHÔNG phải con `User.ForgeLevel` riêng (đó là hệ auto-forge/hammer khác).

## Thưởng mỗi lần lên level (reverse cơ chế)
- Loại thưởng = **GEM**. `LevelPopup.kmz(amount)`: `gem = UserController.dol(gem) + amount` rồi `UserController.dob` (set) — cộng thẳng vào gem người chơi.
- Số lượng = **bảng tra theo level**: `LevelPopup.kmv(level) = rewardList[clamp(level-1, 0, n-1)]` (List<int>, KHÔNG phải công thức base+scaler như dungeon/exp).
- Bảng = static `int[100]` `LevelPopup.ywc`, baked trong `global-metadata.dat` (FieldDefaultValues, fieldIndex=14992), init `.cctor` qua InitializeArray. KHÔNG ở remote_config/tables/*, KHÔNG ở `.so` rodata (il2cpp v31 để trong metadata).
- **Bảng gem/level (index 0..99, dump 2026-09-09):**
```
5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 10 10 10 10 10 10 10 15 15 15 15 15 15 15 20 20 20 25 25 25 25 25 25 25 25 30 30 30 35 35 40 40 40 45 45 45 45 45 45 45 45 45 45 45 50 50 55 55 60 60 60 60 60 60 60 60 65 65 70 70 75 75 75 80 80 80 80 80 80 80 85 85 90 90 95 95 100 100 105 110 110 115 115 120
```
  → index 0-15 = 5 (level 1-16), rồi tăng dần tới 120. `kmv(level)` = `ywc[clamp(level-1,0,99)]`; kmt cộng qua `kmz`. Data point khớp: Level 9 & 12 đều nằm vùng =5.
- (Lưu ý: `hct/hcu/hcv` = reward DUNGEON Bone/LootBox/Vial = base+scaler×(lvl-1), KHÁC thưởng level-up này.)

## Lưu ý cho bản REBUILD
- `StaticCampaignData.STAGES_PER_CHAPTER = 10` **khớp gốc** — giữ nguyên. Công thức áp cho `level` mà `EnemySpawnGenerator` tính: `exp = round(49 + level)`.
- exp = thuộc tính của campaign level (không phải enemy) → khi WIN chỉ cộng `round(49 + level)` cho màn vừa clear, KHÔNG gắn exp vào từng enemy prefab.
- **`playerLevel` (UserPlayerData) = index bảng rarity** (row = playerLevel − 1). Rebuild đã BỎ `UserCampaignData.forgeLevel`; loot roll (`LootService.RollOne`) nhận `playerLevel − 1`.
- 3 con số trong save gốc là ĐỘC LẬP:
  - `Level`/`HighestLevel` = tiến trình màn campaign (số liên tục).
  - `PlayerLevel`/`PlayerExperience` = hệ exp/level người chơi (hdk/hdm/hdn).
  - `ForgeLevel` = nâng riêng bằng hammer/thời gian, **không** do exp. (Rebuild chọn gộp exp→forgeLevel là design riêng, khác gốc.)
