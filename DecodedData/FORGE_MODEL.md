# FORGE (Loot) + AUTO FORGE — mô hình gốc (reverse libil2cpp v41)

Nguồn: `ForgeController`, `ForgeUI`, `NewItemPopup`, `AutoForgePopup`, `AutoForgeButton`, `UserController`,
`GameResources` (dump Il2CppDumper + disasm ARM64, giá trị serialized đọc bằng UnityPy từ xapk v41).
Trong project: búa (Hammer) = `ItemType.LOOT_TICKET`.

## Trạng thái ForgeController (`ForgeState`)
`Idle=0` → `PlayingAnimation=1` → `ShowingItems=2` → `ShowingPopup=3`.

## 1 lượt forge (`hej(isAuto)` → `hek`)
- Số búa tiêu: thủ công = 1; auto = `SelectedAutoForgeHammerCount` (`dog`). Kẹp `min(số chọn, búa đang có)`.
- Trừ búa trước, rồi sinh đúng N món (mỗi búa 1 món), đặt lên bục `ForgeUI.ItemList`.
- Chọn slot món: `Random.Range(0, 6)` (6 slot loot, không Wing/Cape); level món theo món đang mặc ở slot đó.

## Xử lý từng món trên bục (`hem`)
Lấy lần lượt từng món:
- Đang auto **và** `heo(món) == true` → bán luôn (`hep`): cộng EXP, xoá khỏi bục, sang món kế.
- Ngược lại → hiện popup (`heq`):
  - Slot của món đang TRỐNG → `NewItemPopup` (`her` → `GamePopupController.hmc`): 1 thẻ, nút Equip → mặc món vào hero.
  - Slot có đồ → `NewForgePopup` (`hes` → `GamePopupController.hmd`): 2 thẻ (Equipped | món mới):
    - **Equip** (`ihn`): mặc món mới, 2 thẻ đổi chỗ ngay trong popup (món cũ sang thẻ phải) → vẫn chọn tiếp.
    - **Sell** (`OnSellButtonClicked`): món thẻ phải rarity > đồ đang mặc → `ConfirmPanel`
      ("The item you are selling is stronger than your character are you sure?", key `Popup.NewForge.SellConfirm`);
      ngược lại bán luôn (`hey` → `hez`): cộng EXP.
- Hết món trên bục: auto còn bật và còn ≥ 1 búa → lượt mới; hết búa → tắt auto (`heg(false)`).

## `heo` — auto có BỎ (bán) món không
```
bỏ = !giữRarity(món.rarity)                      // AutoForgeDisabledRarities chứa rarity
   || (lọcSubstat bật && không substat nào của món nằm trong AutoForgeEnabledSubStats)
```

## Bán món → EXP (`hez`)
`exp = ItemSellExperiencePerRarity[clamp(rarity, 0, 7)]` = **[1, 2, 4, 6, 8, 10, 12, 14]**,
cộng qua `ExperienceController.hdq` (có áp boost exp như exp thắng màn).

## Save (class `User`)
| Field gốc | Ý nghĩa |
|---|---|
| `Hammer` | số búa (= LOOT_TICKET) |
| `SelectedAutoForgeHammerCount` | số búa / lượt auto |
| `AutoForgeDisabledRarities` | rarity KHÔNG giữ (auto bán) |
| `AutoForgeSubStatFilterEnabled` | bật lọc substat |
| `AutoForgeEnabledSubStats` | substat muốn giữ |
| `AutoSellLowerLevel` | **dead**: chỉ popup đọc, vòng forge không dùng; node trong prefab gốc tắt sẵn |
| `MaxAutoForgeHammerCount` | **dead**: không ai đọc ngoài getter |

## AutoForgePopup
- Mỗi dòng rarity: % = tỉ lệ ở bảng forge theo `PlayerLevel` (`EconomyController.hcq`); dòng có % = 0 thì ẩn cả dòng lẫn nút.
- Số búa tối đa (`igg`):
  - `PlayerLevel ≥ MasteryUnlockPlayerLevel (6)` và Mastery bật → `max(1, round(Mastery AutoLootHammerCount))`.
  - Ngược lại → số mốc `HammerUnlockLevels = [0,10,20,30,40,50,60,70]` mà `PlayerLevel` đã đạt (tối thiểu 1).
- 8 ô chọn số búa 1..8; ô vượt max → khoá.
- Lọc substat mở ở `PlayerLevel ≥ SubStatFilterUnlockForgeLevel (7)`.
- Bấm Start: lọc bật mà chưa chọn substat nào → tự tắt lọc; đóng popup, bắt đầu auto.

## AutoForgeButton
- Hiện từ `PlayerLevel ≥ 2` (`VisibleAtPlayerLevel`), dùng được từ `≥ 3` (`UsableAtPlayerLevel`).
- Đang auto → bấm = dừng; chưa auto → mở AutoForgePopup.

## NewItemPopup (slot trống, 1 thẻ) / NewForgePopup (slot có đồ, 2 thẻ)
- `NameText` = tên món (vũ khí thêm ` [Melee/Range]`), tô màu rarity.
- `RarityText` = tên rarity, tô màu rarity.
- `BaseStatText` = "Damage|Health <số>", số rút gọn (`rm.iqf`).
- `SubStatText` = các dòng substat.
- Nhãn "NEW" đánh dấu món vừa loot (NewForgePopup: theo món đổi chỗ sau khi Equip).
