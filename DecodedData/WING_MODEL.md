# Wing (C8) — chế tạo / lên cấp / reroll (reverse il2cpp v41)

Nguồn: `WingCraftTabPage`, `WingCraftPopup`, `WingRerollPopup`, `WingClaimPage`, `SubStatUIElement`,
`UserController.eea/eeb/eec/eed/ecx/ecz` trong libil2cpp.so + giá trị `GameResources` đọc từ asset.

## Dữ liệu
- 10 `WingData` (1 mẫu/rarity), danh sách = `GameResources.WingDatas` sort theo rarity.
- Save gốc: `User.OwnedWings : List<WingModel{WingId, Level, CardCount, IsNew, SubStats}>`; wing đang mặc = id (`eea`).
- `GameResources.WingUnlockPlayerLevel = 6` (Cape = 15).
- Màu nút: Disabled (0.671,0.671,0.671) · Default (0.208,0.6,0.867) · Danger (1,0.239,0.271).
- RarityColors: CACACA E0C69D B5E05C 45C4FF FFDD61 A288F2 49A872 3C4AC2 E66161 FFB236.
- Quặng = `MineOreType` (Coal 0, Diamond 1, Emerald 2, Gold 3, Iron 4, Ruby 5, Dirt 6, Stone 7) — hệ Mining.

## Chỉ số
- Health/Damage = `vh.lal/lak`: round(Base × TierScaler^(rarity+1) × (1 + Scaler × level)). Chưa sở hữu → hiện ở level 1.

## Chế tạo (WingCraftPopup.kye)
- Quặng `CraftOreType` < `CraftOreCost` → toast `Errors.InsufficientResources`.
- Trừ quặng → `WingModel{Level 1, IsNew = true, SubStats = bản sao WingData.SubStats}` (substat KHỞI ĐIỂM cố định, không random).
- Xong → trang hiện `WingClaimPage` ("New Wing Obtained!": tên + Health/Damage lv1 + từng substat) → Claim → chọn wing vừa làm.

## Lên cấp (WingCraftTabPage.kzc)
- Giá = round(LevelUpCost × LevelUpCostMultiplier^(Level−1)) (float, Math.Round về số chẵn), quặng `LevelUpOreType`.
- Level ≥ MaxLevel → chữ `Common.Level.Max`, nút màu Disabled. Thiếu quặng → toast InsufficientResources.
- Wing đang mặc → toast Power thay đổi.

## Reroll (WingRerollPopup)
- Mỗi substat 1 dòng có nút khoá (mặc định mở). Giá = `RerollCosts[min(số dòng khoá, Count−1)]`, quặng `RerollOreType`.
- Khoá hết → toast `Popup.WingReroll.MustUnlockSubstat`.
- Exclude = mọi loại substat hiện có; mỗi dòng KHÔNG khoá: `lag(exclude)` = random theo weight trong SubStatConfigs
  (weight ≥ 1, loại ∉ exclude) → giá trị `GameResources.jhh(1, MaxValue)` (truncated normal như đồ thường) → thêm loại vào exclude.
  Pool rỗng → (Damage, 1).

## Trang (WingCraftTabPage)
- Carousel: ItemSpacing 320, AnimationDuration 0.3 (Ease OutCubic), CenterScale 1.5, SideScale 0.6, DragSensitivity 1; nút trái/phải chỉ chạy khi ≥ 3 ô.
- Ô chọn mặc định (`kyq`): wing đang mặc → không thì wing sở hữu rarity cao nhất → không thì ô 0.
- Nút Craft (chưa có, hiện "quặng có/cần") ↔ Reroll (đã có). Upgrade + Equip/Unequip chỉ hiện khi đã có.
- Pattern nền info: Ancient(7)/Immortal(8)/Divine(9).

## Project (2026-10-09)
`Scripts/Gears/Wing/{UserWingData, WingService}`, `Scripts/UI/Wing/{UITabWing, WingElementUI, UIWingCraftPopup,
UIWingRerollPopup, ElementSubStatRerollUI}`. Quặng = `ItemType.COAL_ORE + (int)MineOreType` (7..14).
