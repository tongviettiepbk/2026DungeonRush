# Battle Pass (reverse il2cpp v41, 2026-10-17)

Nguồn: `BattlePassPopup` (juh, juw, jvc, jvd, jvk), `BattlePassButton` (jtu, jtx), `BattlePassFreeRewardUI.juc`, `BattlePassPremiumRewardUI.jvt`, asset `BattlePassConfig` (bảng `tables/BattlePassConfig.json`).

## Luật
- Nút lobby hiện khi playerLevel ≥ `GameResources.BattlePassUnlockPlayerLevel` = 4; chấm đỏ khi còn mốc nhận được.
- Tiến độ = `User.HighestLevel` (level campaign cao nhất đã tới).
- Mốc thưởng: StartLevel 20, RewardInterval 5 → màn 20, 25, 30, … (vô hạn). Hiện thêm FutureRewardCount = 10 mốc tương lai.
- Mỗi mốc có 2 hàng: Free và Premium (mua `lootio.battlepass` → `IsPremiumBattlePassBought`); mỗi hàng 2 thưởng.
- Lưu: `ClaimedFreeBattlePassRewards`, `ClaimedPremiumBattlePassRewards` (danh sách mốc đã nhận).
- Mốc nhận được = đã tới mốc và chưa nhận; Premium cần đã mua.

## Công thức thưởng (juw)
- pool = các dòng `LevelRewards` có UnlockLevel ≤ mốc (rỗng thì lấy cả bảng).
- bước = (mốc − StartLevel) / RewardInterval.
- dòng = pool[bước % pool.Count].
- lượng = (int)(BaseAmount × Multiplier^(bước / pool.Count)) — bảng v41 mọi Multiplier = 1.

| Dòng | UnlockLevel | Free 1 | Premium 1 |
|---|---|---|---|
| 0 | 0 | Gem 20 | Hộp loot 100 |
| 1 | 0 | Bone 50 | Key Dragon 2 |
| 2 | 0 | Khoan 1 | Key Zombie 2 |
| 3 | 0 | Bone 50 | Khoan 3 |
| 4 | 0 | Cuốc vàng 1 | Cuốc vàng 3 |
| 5 | 0 | Gem 20 | Hộp loot 250 |
| 6 | 0 | Cuốc 10 | Cuốc 20 |
| 7 | 0 | Bone 50 | Key Dragon 2 |
| 8 | 0 | Khoan 1 | Key Zombie 2 |
| 9 | 0 | Bone 50 | Khoan 3 |
| 10 | 0 | Cuốc vàng 1 | Cuốc vàng 3 |
| 11 | 120 | Vial 50 | Key Cultist 2 |

- Mọi dòng: Free 2 = hộp loot 25, Premium 2 = Gem 20.

## UI
- Chữ thưởng "<sprite=0>{N}" với sprite asset theo loại thưởng (jvk).
- Màu nền ô: đã nhận RewardClaimedColor (0.698), chưa tới RewardNotReachedColor (0.169), nhận được RewardClaimableColor (0.078, 0.624, 0.992).
- Bố cục gốc: LevelElementTopPadding 100, LevelElementSpacing 80, RewardElementHeight 150; 3 cột FreeRewards / Levels / PassRewards.
