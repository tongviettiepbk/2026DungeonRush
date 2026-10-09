# Enchantment ("Relic") — Model reverse từ game gốc v41

Nguồn: reverse `libil2cpp.so` (Il2CppDumper v6.7.46 + capstone ARM64) từ `Dungeon+Rush_41_APKPure.xapk`,
đối chiếu class rip (`EnchantmentTabPage`, `EnchantmentMergePopup`, `EnchantmentInfoPopup`,
`EnchantmentTierRatesPopup`, `EnchantmentSlotUI`, `EnchantmentElementUI`, `SoldierEnchantmentEffectController`),
localization `Popup.Enchantment.*`. Ngày: 2026-10-09.

## 1. Khái niệm

- Relic chỉ có **TIER 1..11** (`qm.xkk = 11`), không có id/level riêng. Icon = `GameResources.EnchantmentTierIcons[tier-1]`
  (`enchantment_01..11.png`), nền = `ItemRarityBackgroundSprites[tier-1]` (`GameResources.jgg`; dùng chung bảng nền rarity đồ).
- **8 slot** = 8 loại trang bị (`ItemType` Weapon, Helmet, Backpack, Gloves, Necklace, Ring, Wing, Cloak).
- Tiền tệ: **Vial** (`User.VialCurrency`) — nguồn chính dungeon **Cultist** (`DungeonVialRewardBase + Scaler×(lv-1)`),
  ngoài ra BattlePass, ClanWar.
- Localization: `Popup.Enchantment.Title` "Enchantment", `.Buff` "Relic Power +{0}%", `.Tier` "Relic +{0}",
  `.Equip/.Unequip/.Merge/.MergeAll/.MergeTitle/.Dismantle/.QuickEquip`, `.TierRatesTitle` "Tier Chances",
  `UI.Enchantment.SummonX` "Summon x{0}".

## 2. Save (class `User`)

| Field | Kiểu | Ý nghĩa |
|---|---|---|
| `OwnedEnchantmentCounts` (0x390) | `List<int>` | index = tier, giá trị = số relic trong KHO (relic đang đeo không tính) |
| `EquippedEnchantmentTiers` (0x398) | `List<int>` | index = ItemType slot, giá trị = tier đang đeo (0 = trống) |
| `TotalEnchantmentSummons` (0x3A0) | `int` | tổng lượt summon → Summon Level |
| `VialCurrency` (0x3A4) | `int` | Vial (`UserController.edj/edk`, clamp ≥ 0) |

Snapshot Boss Rush / PvP / Loadout có thêm `EnchantmentTiers: List<int>` (relic đang đeo).

## 3. Hiệu ứng — nhân main stat món ở slot

- `qm.hxe(tier)` = `(tier/10) × (tier/10 × 100)` = **tier²** (%) → "Relic Power +tier²%".
- `Soldier.eyo(ItemType)` = `1 + hxe(tier)/100` (tier ≥ 1) — dùng trong `Soldier.ewc` nhân **main stat của món đang mặc**
  ở slot đó (weapon/helmet/backpack/gloves/necklace/ring/wing; cape nhân % stat cape trong `eyv`).
  Slot **chưa mặc đồ** dùng PlayerBase*, **không** nhân relic.

| Tier | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Buff % | 1 | 4 | 9 | 16 | 25 | 36 | 49 | 64 | 81 | 100 | 121 |

## 4. Summon (`EnchantmentTabPage` + `UserController.edq` + `qm`)

- 1 multiplier = **100 Vial → 5 lượt** (`xlr = 100`, `xls = 5`). Multiplier `xlu = [1, 2, 3, 5, 10, 20, 30]`.
- Nút multiplier chỉ hiện khi **Vial ≥ 2000** (`xlt`); dưới mức đó multiplier reset về x1. Bấm = vòng `(i+1) % 7`.
- Nút summon: `"<sprite=0>{giá}\nSummon x{lượt}"`, giá đỏ khi thiếu Vial. Thiếu Vial → popup thông báo (không summon).
- `edq(count)`: mỗi lượt `tier = qm.hxc(qm.hwz(TotalEnchantmentSummons))` → `Owned[tier]++` → `Total++`. Không có pity.
- `qm.hxc(level)`: `roll = Random.Range(0f, 100f)`; cộng dồn p6 → p2, `roll < acc` → tier đó; không trúng → tier 1.
  **Summon chỉ ra tier 1..6**; tier 7..11 chỉ có qua Merge.

### Summon Level (max 50)

`qm.xko` (int[49]) = **15, 20, 25 … 255** (= 10 + 5×level): số lượt cần để vượt level đó.
`hwz(total)`: level = i+1 với i đầu tiên mà Σxko[0..i] > total, hết mảng → 50. `hxa` = (total − Σtrước, xko[i], isMax).

### Bảng tỉ lệ `qm.xkn` (ValueTuple<float×6>[50], dựng inline trong `.cctor`) — % tier 1..6

| Lv | T1 | T2 | T3 | T4 | T5 | T6 |
|---|---|---|---|---|---|---|
| 1 | 100 | 0 | 0 | 0 | 0 | 0 |
| 2 | 99.5 | 0.5 | | | | |
| 3 | 99.31 | 0.69 | | | | |
| 4 | 99.05 | 0.95 | | | | |
| 5 | 98.69 | 1.31 | | | | |
| 6 | 98.19 | 1.81 | | | | |
| 7 | 97.5 | 2.5 | | | | |
| 8 | 96.55 | 3.45 | | | | |
| 9 | 95 | 5 | | | | |
| 10 | 92.9 | 7 | 0.1 | | | |
| 11 | 90.02 | 9.8 | 0.18 | | | |
| 12 | 85.96 | 13.72 | 0.32 | | | |
| 13 | 80.21 | 19.21 | 0.58 | | | |
| 14 | 72.06 | 26.89 | 1.05 | | | |
| 15 | 60.46 | 37.65 | 1.89 | | | |
| 16 | 43.89 | 52.71 | 3.4 | | | |
| 17 | 35.11 | 59.89 | 5 | | | |
| 18 | 28.09 | 64.81 | 7 | 0.1 | | |
| 19 | 22.47 | 67.55 | 9.8 | 0.18 | | |
| 20 | 17.98 | 67.98 | 13.72 | 0.32 | | |
| 21 | 16.5 | 63.71 | 19.21 | 0.58 | | |
| 22 | 16.5 | 55.56 | 26.89 | 1.05 | | |
| 23 | 16.5 | 43.96 | 37.65 | 1.89 | | |
| 24 | 16.5 | 27.39 | 52.71 | 3.4 | | |
| 25 | 16.5 | 16.5 | 62 | 5 | | |
| 26 | 16.5 | 16.5 | 59.9 | 7 | 0.1 | |
| 27 | 16.5 | 16.5 | 57.02 | 9.8 | 0.18 | |
| 28 | 16.5 | 16.5 | 52.96 | 13.72 | 0.32 | |
| 29 | 16.5 | 16.5 | 47.21 | 19.21 | 0.58 | |
| 30 | 16.5 | 16.5 | 39.06 | 26.89 | 1.05 | |
| 31 | 16.5 | 16.5 | 27.46 | 37.65 | 1.89 | |
| 32 | 16.5 | 16.5 | 16.5 | 47.1 | 3.4 | |
| 33 | 16.5 | 16.5 | 16.5 | 45.5 | 5 | |
| 34 | 16.5 | 16.5 | 16.5 | 43.4 | 7 | 0.1 |
| 35 | 16.5 | 16.5 | 16.5 | 40.52 | 9.8 | 0.18 |
| 36 | 16.5 | 16.5 | 16.5 | 36.46 | 13.72 | 0.32 |
| 37 | 16.5 | 16.5 | 16.5 | 30.71 | 19.21 | 0.58 |
| 38 | 16.5 | 16.5 | 16.5 | 22.56 | 26.89 | 1.05 |
| 39 | 16.5 | 16.5 | 16.5 | 16.5 | 32.11 | 1.89 |
| 40 | 16.5 | 16.5 | 16.5 | 16.5 | 30.6 | 3.4 |
| 41 | 16.5 | 16.5 | 16.5 | 16.5 | 29 | 5 |
| 42 | 16.5 | 16.5 | 16.5 | 16.5 | 28.3 | 5.7 |
| 43 | 16.5 | 16.5 | 16.5 | 16.5 | 27.5 | 6.5 |
| 44 | 16.5 | 16.5 | 16.5 | 16.5 | 26.59 | 7.41 |
| 45 | 16.5 | 16.5 | 16.5 | 16.5 | 25.56 | 8.44 |
| 46 | 16.5 | 16.5 | 16.5 | 16.5 | 24.37 | 9.63 |
| 47 | 16.5 | 16.5 | 16.5 | 16.5 | 23.03 | 10.97 |
| 48 | 16.5 | 16.5 | 16.5 | 16.5 | 21.49 | 12.51 |
| 49 | 16.5 | 16.5 | 16.5 | 16.5 | 19.74 | 14.26 |
| 50 | 17.5 | 16.5 | 16.5 | 16.5 | 16.5 | 16.5 |

(Mọi hàng tổng 100%. Bảng đọc bằng mini-emulator theo dõi thanh ghi s0..s5 / spill sp / thunk `b` ngoài hàm trong `.cctor`.)

## 5. Đeo / Merge / Dismantle (`UserController`)

| Hàm | Logic |
|---|---|
| `edo(slot, tier)` Equip | tier ≥ 1 cần `Owned[tier] ≥ 1`; relic cũ ở slot → `Owned[old]++`; `Owned[tier]--`; `Equipped[slot] = tier`. tier = 0 → tháo. |
| `edr(tier)` Merge | tier 1..10, `Owned[tier] ≥ 3` → `Owned[tier] -= 3`, `Owned[tier+1]++` |
| `eds()` Merge All | tier 1→10 tăng dần: `n = Owned[t]/3`; `Owned[t] -= 3n`; `Owned[t+1] += n` (dây chuyền) |
| `edx(slot)` Merge đang đeo | tier 1..10, `Owned[tier] ≥ 2` → `Owned[tier] -= 2`, slot lên tier+1 |
| `edw(tier)` Dismantle | tier 2..11, `Owned[tier] ≥ 1` → `Owned[tier]--`, `Owned[tier-1] += 3` |
| `edy(slot)` Dismantle đang đeo | tier ≥ 2 → slot = tier-1, `Owned[tier-1] += 2` |
| `edt()` Quick Equip | tháo hết về kho → slot có đồ xếp theo priority giảm dần (hoà → thứ tự `ugv`), slot trống xếp sau → lần lượt gán tier cao nhất còn trong kho |

- Priority `edu(ItemType, rarity)` = `min(rarity + (Cloak ? 4 : Wing ? 1 : 0), 10)`; rarity = rarity món đang mặc.
- Thứ tự `UserController.ugv` = **Weapon, Cloak, Wing, Helmet, Backpack, Gloves, Ring, Necklace**.
- Comparer `UserController.eq.dnd`: `prio[b].CompareTo(prio[a])`, hoà → `order[a].CompareTo(order[b])`.
- Mọi thao tác: `dsm(true)` (save) + `EnchantmentChanged`.

## 6. Luồng UI gốc (đã đối chiếu native 2026-10-09)

**Mở khoá** (`ItemElementUI.icu`): nút enchantment trên ô trang bị chỉ hiện khi `PlayerLevel >= GameResources.CultistDungeonUnlockPlayerLevel`
(cùng mốc mở dungeon Cultist) và slot không bị khoá. Nhãn "+tier" trên ô (`ict`) hiện khi tier ≥ 1 và slot không khoá
(gốc KHÔNG xét có đồ hay không).

**Trang `EnchantmentTabPage`**
- Kho (`hyu`/`hzq`): mỗi relic 1 ô, tier **tăng dần** 1→11. Đang chọn slot để đeo → kho ẩn, chỉ hiện 1 ô relic đang chọn.
- Summon Level: `"Lvl {0}"` (`Common.Level.Abbrev`), tiến độ `"cur/req"`, max → `"MAX"` + thanh đầy.
- Nút summon: `"<sprite=0>{giá}\nSummon x{lượt}"`, giá đỏ khi thiếu; màu nút Default/Disabled. Thiếu Vial → chuyển tab **Dungeon** (`TabBar.kvq(2)`).
- Bấm relic kho → `EnchantmentInfoPopup(tier, -1, onEquip)`; Equip → `hze(tier)` chế độ chọn slot (`EnchantmentSlotUI.hye(true)` bật TutorialUI).
  Bấm slot khi đang chọn → `edo(slot, tier)`; bấm relic khi đang chọn → huỷ. Bấm slot có relic → `EnchantmentInfoPopup(tier, slot)`.
- Nút: QuickEquip `edt`, MergeAll `eds`, "Merge" mở popup Merge trống `hlv(0, -1)`, (i) mở TierRatesPopup.

**Slot `EnchantmentSlotUI.hyb`**: Placeholder khi không có đồ, ItemIcon = icon đồ, Background = `jgr(rarity đồ)` (không đồ → `jgr(0)` Common);
badge (`TierIndicatorBackground`) hiện khi tier > 0, sprite `jgg(tier)`, chữ `"+tier"`; `RelicIndicatorText` = `"Lvl {tier}"`.

**Ô relic `EnchantmentElementUI.hxh(tier)`**: nền `jgg(max(tier,1))`; tier > 0 → icon `jgh(tier)` + chữ `"Lvl {tier}"`; tier 0 = ô trống.

**`EnchantmentInfoPopup.eip`**: nền `jgg`, icon `jgh`, TierText `"Relic +{tier}"`, InfoText `"Relic Power + <color=#E5F36B>{tier²:0.#}</color>%"`.
Nút Equip/Unequip theo `SlotIndex < 0`; **Merge bật khi tier < 11** (cả relic đang đeo); **Dismantle bật khi tier > 1**.
- Equip (`hxo`): slot ≥ 0 → `edo(slot, 0)`; không → đóng popup + `onEquip(tier)`.
- Merge (`hxp`): đóng popup → mở **MergePopup(InitialTier = tier, SourceSlot = slot)** (cả relic đang đeo).
- Dismantle (`hxq`): tier ≥ 2 → slot ≥ 0 ? `edy(slot)` : `edw(tier)`.

**`EnchantmentMergePopup`** — state `xli` tier, `xlj` số ô đã đặt, `xlk` source slot (-1 = không).
- Mở: InitialTier ≥ 1 → tier = InitialTier, **count = 1**, src = SourceSlot.
- `hxt`: ô i hiện tier nếu i < count (bấm được), ngược lại trống; ô kết quả hiện tier+1 **chỉ khi đủ 3**; nút Merge bật khi đủ 3.
- `hxu`: kho tier 1..11, đã chọn tier thì **chỉ hiện tier đó**, số = owned − (count − (src ≥ 0 ? 1 : 0)).
- `hxw` bấm kho: bỏ qua nếu đủ ô / tier > 10 / khác tier đang chọn; còn relic thì count++.
- `hxx` bấm ô i < count: ô 0 là relic đang đeo → src = -1; count--; hết → reset.
- `hxy` Merge: src ≥ 0 ? `edx(src)` : `edr(tier)` → reset popup trống.

**`EnchantmentTierRatesPopup.hzw`**: TierTexts `"Relic +{i}"` (6 hàng), level `"Lv {0}"` (`Common.Level.Short`),
level kế = min(level+1, 50), tỉ lệ `"{0:F2}%"`.

## 7. Port sang project (2026-10-09)

| Gốc | Project |
|---|---|
| `qm` + hằng TabPage | `Scripts/Enchantment/EnchantmentConfig.cs` |
| `User.*Enchantment*` | `UserEnchantmentData` (key `key_user_enchantment`; equipped khoá `(int)GearSlotType`) |
| `VialCurrency` | `UserItemData` — `ItemType.VIAL` (đã có, reward dungeon Cultist) |
| `UserController.edo..edy` | `EnchantmentService` (static) |
| `Soldier.eyo` | `EquipmentStatResolver.BuildModifiers(equipment, enchantments)` nhân main stat |
| `EnchantmentTierIcons` / `ItemRarityBackgroundSprites` | `EnchantmentSpriteConfig` (Resources/Scriptable Objects/Enchantment) |
| `EnchantmentTabPage` | `UITabEnchantment` (UiMainGame/ContentPage/PageEnchantment) |
| `EnchantmentSlotUI` | `ElementEnchantmentEquipUI` |
| `EnchantmentElementUI` | `ElementEchantmentUI` / `ElementEnchantmentMergeUI` |
| `EnchantmentInfoPopup` | `UIEnchantmentElementInfo` (prefab `UiEnchantmentInfoItem`) |
| `EnchantmentMergePopup` | `UIEnchantmentMerge` (prefab `UiEnchantmentMerge`) |
| `EnchantmentTierRatesPopup` | `UIUpgradeEnchantment` (prefab `uiUpgradeEnchantment`) |
| SummonResultPanel | `UISumonEnchantment` (prefab `UiSumonEnchantment`) |

## 8. Chỗ khác trong game gốc có dùng relic (rà callers 2026-10-09)

| Gốc | Logic | Project |
|---|---|---|
| `Soldier.ewc/eyo/eyn` | main stat món × (1 + tier²/100); ghost đọc `EnchantmentTiers` của snapshot | ✅ hero + ghost Boss Rush |
| `BossRushController.ekr/eln` | join/update gửi `enchantmentTiers` (= `edn()`), `BossRushPlayerModel.EnchantmentTiers` | ✅ client + server (index = GearSlotType) |
| `ItemInfoPopup.igz` (`EnchantmentTier` set ở `ItemUI.ifa`) | info món đang mặc: main stat × hệ số relic; popup so sánh loot KHÔNG tính | ✅ `UIGearInfo.Show(result, tier)` |
| `DungeonController.hce` / `DungeonPopup.kcp` | thắng / Sweep dungeon Cultist → Vial | ✅ có sẵn (rewardType VIAL) |
| BossRush reward `RewardType.Vial` | Vial | ✅ có sẵn |
| `rm.iqm` (Power) | Power người chơi cộng phần tăng từ relic: Σ main stat × (hệ số − 1) theo Damage/Health, wing × hệ số slot 6, cape % × hệ số slot 7 | ⚠️ project chưa có hệ Power chung (Boss Rush dùng Attack×MaxHp hero → đã gồm relic) |
| `rm.iqp/iqq` | trước/sau đeo-merge-dismantle-quick equip → hiện chênh lệch Power | ❌ chờ hệ Power + UI |
| `BattlePassPopup.jvd`, `ClanWarController.gao` | thưởng Vial | ❌ project chưa có BattlePass/ClanWar |
| `PvPController`, `ChatController`, `ClanChatController`, `LoadoutUploadRequestDTO` | gửi `enchantmentTiers` trong snapshot | ❌ project chưa có PvP/Chat/Loadout |
| `SoldierEnchantmentEffectController`, `PreviewCharacter` | hiệu ứng relic trên nhân vật | ❌ phần hình ảnh |
