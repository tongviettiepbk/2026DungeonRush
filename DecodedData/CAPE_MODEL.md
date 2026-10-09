# Cape (C7) — summon / salvage / equip (reverse il2cpp v41)

Nguồn: `CapePopup`, `CapeDetailPopup`, `CapeSalvagePopup`, `CapeUpgradeInfoPopup`, `CapePageElementUI`, `CapeClaimPage`,
static class `ft` (bảng summon), `CapeConfig.eql/eqm`, `UserController.efa/efb/efc/efd/eem/eek`, `GameResources.jhn/jht`.

## Dữ liệu / save
- 12 CapeData (2 mẫu/rarity, Common..Mythic). `CapeConfig`: MaxLevel 100, SummonCost 20, RarityConfigs (XP).
- Save gốc: `OwnedCapes: List<CapeModel{InstanceId(Guid), CapeId, Level, CurrentXP, CardCount, IsNew, SubStats}>`,
  `EquippedCapeId` (= InstanceId), `TotalCapeSummons`, `ShowCloak`, `CloakCurrency`. Mỗi lần summon = 1 bản riêng.
- `GameResources.CapeUnlockPlayerLevel = 15`.

## Summon (CapePopup.etg → UserController.efa)
- Multiplier `[1,2,3,5,10,20,30]` (nút chỉ hiện khi Cloak > 99, không thì về x1). Giá = SummonCost × multiplier, ra multiplier cape.
- Mỗi lượt: rarity = `ft.eqo(TotalCapeSummons)` → random 1 CapeData cùng rarity (`jhn`) → CapeModel Level 1, Guid mới,
  SubStats = `jht(CapeData.SubStatCount)` (random theo weight, không trùng loại, giá trị `jhh(1,max)`) → TotalCapeSummons++.
- 1 kết quả → ClaimPage "New Cloak Obtained!"; nhiều → bảng kết quả (bấm để đóng).
- Roll: `Random.Range(0,100)`, cộng dồn tỉ lệ từ rarity CAO xuống thấp, < tổng thì ra rarity đó.

## Summon Level (ft)
- Level (1-based) = vị trí mốc đầu tiên có threshold > TotalCapeSummons (`eqp`); tối đa 50.
- Tiến độ (`equ`): (total − mốc trước) / (mốc này − mốc trước); vượt mốc 7842 → Max.
- Popup Info: "Level N", trái/phải xem level khác, 6 dòng "{0:F1}%", thanh tiến độ chỉ hiện ở level hiện tại.

| Lv | mốc (total <) | Common | Uncommon | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|---|
| 1 | 2 | 100 | 0 | 0 | 0 | 0 | 0 |
| 2 | 7 | 99.5 | 0.5 | 0 | 0 | 0 | 0 |
| 3 | 15 | 99.31 | 0.69 | 0 | 0 | 0 | 0 |
| 4 | 26 | 99.05 | 0.95 | 0 | 0 | 0 | 0 |
| 5 | 40 | 98.69 | 1.31 | 0 | 0 | 0 | 0 |
| 6 | 57 | 98.19 | 1.81 | 0 | 0 | 0 | 0 |
| 7 | 77 | 97.5 | 2.5 | 0 | 0 | 0 | 0 |
| 8 | 100 | 96.55 | 3.45 | 0 | 0 | 0 | 0 |
| 9 | 126 | 95 | 5 | 0 | 0 | 0 | 0 |
| 10 | 155 | 92.9 | 7 | 0.1 | 0 | 0 | 0 |
| 11 | 187 | 90.02 | 9.8 | 0.18 | 0 | 0 | 0 |
| 12 | 222 | 85.96 | 13.72 | 0.32 | 0 | 0 | 0 |
| 13 | 260 | 80.21 | 19.21 | 0.58 | 0 | 0 | 0 |
| 14 | 301 | 72.06 | 26.89 | 1.05 | 0 | 0 | 0 |
| 15 | 345 | 60.46 | 37.65 | 1.89 | 0 | 0 | 0 |
| 16 | 392 | 43.89 | 52.71 | 3.4 | 0 | 0 | 0 |
| 17 | 442 | 35.11 | 59.89 | 5 | 0 | 0 | 0 |
| 18 | 495 | 28.09 | 64.81 | 7 | 0.1 | 0 | 0 |
| 19 | 551 | 22.47 | 67.55 | 9.8 | 0.18 | 0 | 0 |
| 20 | 610 | 17.98 | 67.98 | 13.72 | 0.32 | 0 | 0 |
| 21 | 672 | 16.5 | 63.71 | 19.21 | 0.58 | 0 | 0 |
| 22 | 737 | 16.5 | 55.56 | 26.89 | 1.05 | 0 | 0 |
| 23 | 805 | 16.5 | 43.96 | 37.65 | 1.89 | 0 | 0 |
| 24 | 876 | 16.5 | 27.39 | 52.71 | 3.4 | 0 | 0 |
| 25 | 950 | 16.5 | 16.5 | 62 | 5 | 0 | 0 |
| 26 | 1027 | 16.5 | 16.5 | 59.9 | 7 | 0.1 | 0 |
| 27 | 1107 | 16.5 | 16.5 | 57.02 | 9.8 | 0.18 | 0 |
| 28 | 1190 | 16.5 | 16.5 | 52.96 | 13.72 | 0.32 | 0 |
| 29 | 1276 | 16.5 | 16.5 | 47.21 | 19.21 | 0.58 | 0 |
| 30 | 1365 | 16.5 | 16.5 | 39.06 | 26.89 | 1.05 | 0 |
| 31 | 1457 | 16.5 | 16.5 | 27.46 | 37.65 | 1.89 | 0 |
| 32 | 1552 | 16.5 | 16.5 | 16.5 | 47.1 | 3.4 | 0 |
| 33 | 1650 | 16.5 | 16.5 | 16.5 | 45.5 | 5 | 0 |
| 34 | 1782 | 16.5 | 16.5 | 16.5 | 43.4 | 7 | 0.1 |
| 35 | 1948 | 16.5 | 16.5 | 16.5 | 40.52 | 9.8 | 0.18 |
| 36 | 2148 | 16.5 | 16.5 | 16.5 | 36.46 | 13.72 | 0.32 |
| 37 | 2382 | 16.5 | 16.5 | 16.5 | 30.71 | 19.21 | 0.58 |
| 38 | 2650 | 16.5 | 16.5 | 16.5 | 22.56 | 26.89 | 1.05 |
| 39 | 2952 | 16.5 | 16.5 | 16.5 | 16.5 | 32.11 | 1.89 |
| 40 | 3288 | 16.5 | 16.5 | 16.5 | 16.5 | 30.6 | 3.4 |
| 41 | 3658 | 16.5 | 16.5 | 16.5 | 16.5 | 29 | 5 |
| 42 | 4062 | 16.5 | 16.5 | 16.5 | 16.5 | 28.3 | 5.7 |
| 43 | 4500 | 16.5 | 16.5 | 16.5 | 16.5 | 27.5 | 6.5 |
| 44 | 4972 | 16.5 | 16.5 | 16.5 | 16.5 | 26.59 | 7.41 |
| 45 | 5478 | 16.5 | 16.5 | 16.5 | 16.5 | 25.56 | 8.44 |
| 46 | 6018 | 16.5 | 16.5 | 16.5 | 16.5 | 24.37 | 9.63 |
| 47 | 6592 | 16.5 | 16.5 | 16.5 | 16.5 | 23.03 | 10.97 |
| 48 | 7200 | 16.5 | 16.5 | 16.5 | 16.5 | 21.49 | 12.51 |
| 49 | 7842 | 16.5 | 16.5 | 16.5 | 16.5 | 19.74 | 14.26 |
| 50 | max | 17.5 | 16.5 | 16.5 | 16.5 | 16.5 | 16.5 |

## XP / lên cấp (CapeConfig, UserController.efd)
- XP từ level L lên L+1 (`eql`), x = (L−1)×0.5:
  - L ≤ 29: LevelUpBaseXP + LevelUpScaler × x^LevelUpExpo
  - L > 29: LevelUpBaseXP2 + LevelUpScaler2 × x
  - làm tròn Math.Round (về số chẵn).
- XP salvage 1 cape level L (`eqm`) = round(SalvageBaseXP + Σ_{l=1}^{L−1} eql(l)) — KHÔNG tính CurrentXP dở dang.
- `efd`: CurrentXP += xp; lên cấp liên tục tới khi thiếu XP hoặc MaxLevel (chạm Max → CurrentXP = 0).

## Salvage (CapeSalvagePopup)
- Danh sách = mọi cape khác cape đang nâng; chọn nhiều làm nguyên liệu (cape đang mặc không chọn được).
- Xem trước: "Lv N (+k)", "+X% Damage (+Δ%)", thanh "xp/need xp" (MAX khi max). Upgrade: xoá nguyên liệu (`efc`), cộng XP (`efd`).

## Detail / Equip
- Tên màu rarity, "[Rarity]", "+X.X% Damage", "+X.X% Health" (fu.eqx/eqy), substat. Equip ↔ Unequip (`eem`), đóng popup + toast Power.
- Show Cloak (chỉ hiện khi có ≥ 1 cape): tắt thì vẫn mặc nhưng ẩn hình áo choàng.

## Project (2026-10-09)
`Scripts/Gears/Cape/{CapeSummonConfig, UserCapeData, CapeService}`, `Scripts/UI/Cape/{UICapePopup, UICapeDetailPopup,
UICapeSalvagePopup, UICapeUpgradeInfoPopup, CapeElementUI}`, prefab `Resources/Prefabs/UI/UICape*.prefab`,
`Prefabs/UINew/Cape/CapeElementUI.prefab`. Cloak = `ItemType.CLOAK` (15). Nền rarity: `RarityBackgroundConfig` (Resources).
