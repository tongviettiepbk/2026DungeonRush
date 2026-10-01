# DUNGEON_MODEL — hệ Dungeon (reverse libil2cpp v41)

Nguồn: `DecodedData/tables/DungeonData.json`, GameResources trong `StarterScene.unity` (AssetRipper),
save `appcache/drdump/playerprefs.xml`, disasm `DungeonController` / `DungeonPopup` / `DungeonTabPage` /
`UserController` / `EconomyController` (Il2CppDumper v6.7.46 + capstone).

## 3 dungeon

| DungeonType | Tên | Thưởng | Base | Scaler | Mở khoá |
|---|---|---|---|---|---|
| 0 DragonBoss | Dragon's Hoard | Hammer (lootbox) | 50 (`DungeonLootBoxRewardBase`) | 1 | theo tab Dungeon (PlayerLevel 4) |
| 1 ZombieHorde | Zombie Outbreak | Bone | 100 (`DungeonBoneRewardBase`) | 2 | PlayerLevel 5 (`DungeonTabPage.ZombieHordeUnlockPlayerLevel`, .cctor) |
| 2 Cultist | Cultist Ritual | Vial (`VialCurrency`) | 100 (`DungeonVialRewardBase`) | 2 | PlayerLevel 20 (`CultistDungeonUnlockPlayerLevel`) |

Thưởng màn N = `base + scaler * (N - 1)` — `EconomyController.hcu` (Dragon) / `hct` (Zombie) / `hcv` (Cultist).
Hàm popup `DungeonPopup.kcp` xác nhận: Dragon → `User.Hammer`, Zombie → `User.Bone`, Cultist → `User.VialCurrency`.

> Lưu ý: cột `RewardType` trong DungeonData.json (Dragon=3, Zombie=0, Cultist=10) KHÔNG phải thưởng thật;
> code dùng switch theo DungeonType như trên.

## Save (User) mỗi dungeon

`{X}Level`, `{X}HighestLevel`, `{X}DailyKeys`, `{X}BonusKeys`, `Last{X}KeyResetTime`, `{X}RewardedAdWatchCount`
(X = DragonBoss / ZombieHorde / Cultist) + chung `LastRewardedAdResetTime`. Thời gian = unix giây.

## Key

- Tổng key = DailyKeys + BonusKeys (`UserController.dts`).
- Reset (`DungeonController.hbw/hbx/hby`, gọi mỗi Update): `ev.ehv(unix)` = `new DateTime(1970,1,1,Kind=Utc).AddSeconds`;
  nếu `now.Date > last.Date` (ngày **UTC**) hoặc chưa reset lần nào (-1) → `DailyKeys = 2` (`dtp` kẹp ≤ 2, SET không cộng),
  lưu thời điểm reset. BonusKeys không bị reset.
- Tiêu key (`dtv`): DailyKeys trước, hết mới trừ BonusKeys.
- Text gốc: "Keys are only consumed when you complete the dungeon." / "Exit does not consume keys".

## Thắng màn (`DungeonController.hcd`)

Tiêu 1 key → thưởng màn `Level` hiện tại → `Level = Level + 1`. Thua/thoát không gọi → không tiêu key.

## Popup (`DungeonPopup`)

- Độ khó hiển thị từ HighestLevel h: `"{(h-1)/10+1}-{(h-1)%10+1}"` (`kch`, chia 10 bằng magic 0x66666667).
- Reward text = thưởng màn h. KeyCount = tổng key.
- **Sweep Last** (`kci`): cần tổng key ≥ 1 và h > 1; tiêu 1 key, thưởng màn `h-1`, không đổi level. Nút interactable = key>0 && h>1.
- **Ads** (`kcs`→`kct`→`kcw`): hiện khi `RewardedAdWatchCount < 3` (`kcr` = 3); xem xong BonusKeys+1, WatchCount+1 (`dua` kẹp ≤ 3).
- Reset lượt ads (`UserController.dub`): cùng luật ngày UTC, reset WatchCount của 3 dungeon (+ offline earnings).

## Tab (`DungeonTabPage`)

"Replenishing in: {0}" = thời gian tới 0h UTC kế. Khoá Zombie/Cultist theo PlayerLevel (overlay + icon khoá).

## Trận Dragon's Hoard (reverse 2026-10-01)

Chuỗi: `LevelController.hpv(level)` → `rs.ejz/irj` (generator Dragon) → `EconomyController.hcm` → `SpawnController.huu`.

- **Quái**: đúng 1 con, `sx{ CharacterId = DragonBossCharacterId (999999), Role = Range (1), combatLevel = jgm(level, 0) = 1 + (level-1)×3 }`.
  Preset (`hpy`, DungeonType 0) tạo tại chỗ: Melee 0 / Ranged 1 / Lancaster 1.0 → r = 2, damage ×0.8 (RangedUnitDamageMultiplier).
  Công thức chỉ số: `ENEMY_STATS_MODEL.md` (bảng DragonBoss đã sửa).
- **Tên màn** (`rs.ejz`): `"{(level-1)/10+1}-{(level-1)%10+1}"` (format giống popup).
- **Vị trí** (`GridManager.ijx(count)`, dùng chung mọi enemy): quét ô x ∈ [-W/2, W/2), y ∈ [5, H×res) (nửa trên),
  ô đi được + chưa chiếm + nằm trong vùng liên thông (`ijw` flood-fill) → `Random.Range` bốc ngẫu nhiên. Rồng = 1 ô ngẫu nhiên nửa trên.
- **Gear/animator** (`SpawnController.huy`, theme `DungeonThemeData` DragonHordeData): weapon/helmet/gloves + animator theo theme
  (role Range dùng bộ `EnemyRanged*` nếu khác null — Dragon để null nên dùng bộ thường). Weapon = DragonWeapon (Range, 1 đòn/giây,
  tầm 100, đạn tốc 12, Projectile_Dragon_*).
- **`SpawnController.huv`**: nếu theme `EnemyCanMove == false` → tắt cờ di chuyển của Character (`+0x15D = false`) và xoay
  transform hình (`+0x58`) `Euler(0, 0, π rad)` = quay 180° (rồng nhìn xuống). Sau đó `huw(char, ColliderRadiusMultiplier=1.1)`.
- **AI**: không có class AI riêng — Character thường, chỉ đứng yên + bắn đạn tầm 100 (bắn được cả map).
- **Kết thúc**: `GameplayUI.ul` (coroutine): chờ delay → nếu thắng chạy `LootDropController.hsv` (hiệu ứng rơi thưởng) và chờ xong →
  `GamePopupController.hmg(win, type, level)` → `DungeonEndPopup.kcd`: thắng "Completed" + text thưởng (sprite asset key/thưởng),
  thua "Defeat" / "Better luck next time!". Text: `Popup.DungeonEnd.*`.
- Không tìm thấy bộ đếm thời gian riêng cho dungeon.
- Save có thêm `DragonDungeonFailCount`, `IsRateUsFirstDragonWinShown` (rate-us/analytics — bỏ qua).

## Soát lại (2026-10-01, đối chiếu binary + StarterScene/GameplayScene)

- `hcd(type)` (gọi khi `GameState.Completed` = 6 trong dungeon): `hcc`→`UserController.dtv` tiêu key (Daily trước, hết mới Bonus;
  trả false nếu hết cả hai) → đọc `Level` (`dtl`) → analytics kèm `DragonDungeonFailCount` rồi reset về 0 (`egd`) →
  `dtm(Level+1)` (set Level, `HighestLevel = max`) → `hce(type, Level cũ)`: thưởng `base + scaler×(Level-1)` cộng thẳng vào
  `User.Hammer` (0xEC, = Hammer dùng để forge/loot). `dtm` chỉ được gọi từ `hcd` ⇒ Level luôn = HighestLevel.
- `GameState.Failed` = 5 trong dungeon: chỉ tăng `{X}DungeonFailCount` (analytics), không đụng key/level.
- Trận dungeon dựng quái từ `hpv(hbz(type))` = `hpv(Level)`.
- Hằng số (StarterScene GameResources): LootBox 50/1, Bone 100/2, Vial 100/2; DragonLevelBase 1 ×3, Zombie 5 ×3, Cultist 60 ×3;
  Ratio Melee 3 / Ranged 2 / Boss 10, RangedUnitDamageMultiplier 0.8; ArmyPowerBase 500, scaler √10. Remote config live KHÔNG
  ghi đè các giá trị này.
- Toàn bộ chuỗi `hck`→`hcj`(powf)→`hcm`(fsqrt) tính bằng **float32**, chỉ `Math.Round` (banker's) ở double. Tính bằng double
  sẽ lệch ±1 từ dungeon level ~68.
- Theme (đọc raw từ xapk): DragonHordeData = DragonWeaponData / DragonHelmetData / DragonGloveData (bộ "Dragon" thường,
  không phải Red/Green/Purple). HelmetSprite/GlovesSprite = Icon.
- Mở khoá: tab Dungeon PlayerLevel 4 (`TabData` "Dungeon" trong GameplayScene), Zombie 5, Cultist 20; Dragon theo tab.
- Popup: `kch` Enter/Ad chung chỗ (còn key → Enter; hết key + còn lượt ads → Ad, interactable khi ads sẵn sàng; hết cả hai → Enter khoá).
  `kci` Sweep: key ≥ 1 và HighestLevel > 1 → `dtv` + thưởng màn HighestLevel-1. `kcw` ads: BonusKeys +1, WatchCount +1 (≤ 3).
