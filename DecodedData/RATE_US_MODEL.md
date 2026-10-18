# Rate Us model — reverse từ libil2cpp.so (APK bản 41)

> Nguồn: disasm ARM64 `RateUsPopup`, `UserController`, `ForgeController`, `DungeonController`, `PopupController`
> trong `libil2cpp.so` (đồ nghề `tools/il2cpp_reverse/`). Đây là REFERENCE bất biến, không sửa theo refactor code.

## Save (class `User`)
| Field | Offset | Ý nghĩa |
|---|---|---|
| `IsRateUsSubmitted` | 0x68 | Đã bấm Submit (có chọn sao) → không bao giờ hiện lại |
| `RateUsShowCount` | 0x240 | Số lần popup đã được bật |
| `IsRateUsFirstGreenEquipShown` | 0x244 | Đã dùng trigger "mặc đồ Rare lần đầu" |
| `IsRateUsFirstPurpleEquipShown` | 0x245 | Đã dùng trigger "mặc đồ Epic lần đầu" |
| `IsRateUsFirstDragonWinShown` | 0x246 | Đã dùng trigger "thắng Dragon dungeon lần đầu" |

## Hàm đã map
| Hàm | VA | Ý nghĩa |
|---|---|---|
| `UserController.dox()` | 0x2835374 | CanShow = `!IsRateUsSubmitted && !shownThisSession(UserController.ugc @0x21) && RateUsShowCount < 3` |
| `UserController.dpg()` | 0x28354B0 | TryShow: `dox` sai → thôi; đang dungeon (`GameController.wtp`) / PvP (`wtr`) / Boss Rush (`wue`) → thôi; còn lại: `ugc = true`, `RateUsShowCount++` + lưu, `PopupController.dgi(null)` |
| `UserController.dou(bool)` | 0x283530C | Gán `IsRateUsSubmitted` + lưu |
| `ForgeController.hex(rarity)` | 0x2789FF4 | Gọi cuối `hew` (mặc món forge, từ `heu`/`hev`): `dox` sai → thôi; rarity = 2 (Rare) và chưa có cờ Green → bật cờ, `dpg`; rarity = 3 (Epic) và chưa có cờ Purple → bật cờ, `dpg` |
| `DungeonController.hcd(type)` | 0x27821B0 | Thắng dungeon (`LevelController.OnStateChanged` state 6). Type 0 (Dragon): sau tiêu key/thưởng/level+1, nếu chưa có cờ Dragon và `dox` → bật cờ, `dpg` |
| `PopupController.dgi(cb)` | 0x275C368 | Đang PvP (`wts`) / Boss Rush (`wue`) → chỉ gọi cb; còn lại xếp hàng mở `RateUsPopup` sau **2.0s** (`dgj`) |
| `RateUsPopup.eip` | 0x2922334 | Mở: số sao chọn = 0, `ejc`; gắn onClick sao thứ i → `eja(i+1)`; Submit → `ejd`; Later → `ejf` |
| `RateUsPopup.eja(n)` / `ejb` / `ejc` | 0x292268C / 0x29226A8 / 0x2922614 | Chọn n sao; sao i < n = `StarFilledSprite`, còn lại `StarEmptySprite`; chữ Submit = `SubmitTextBlueColor` khi n ≥ 1 (xám khi 0), `SubmitButton.interactable = n > 0` |
| `RateUsPopup.ejd` | 0x2922728 | Submit: n ≥ 1 → `dou(true)`; n ≥ 4 → `eje`; đóng popup |
| `RateUsPopup.eje` | 0x29227CC | `OpenURL("market://details?id=" + identifier)`, lỗi → `https://play.google.com/store/apps/details?id=` |
| `RateUsPopup.ejf` | 0x2922944 | Later: đóng popup |

## Ghi chú
- Trigger Dragon gọi `dpg` lúc VẪN đang ở mode dungeon → `dpg` dừng ở kiểm tra `wtp`: cờ Dragon bị đánh dấu nhưng popup
  KHÔNG hiện. Save thật (`appcache/drdump/playerprefs.xml`) khớp: `IsRateUsFirstDragonWinShown = true`, `RateUsShowCount = 1`
  (lần hiện duy nhất là từ đồ Rare).
- Chữ prefab gốc: tiêu đề `Popup.RateUs.Title`, mô tả `Popup.RateUs.Description`, nút "SUBMIT" / "LATER" (cứng, không dịch).
- `MaxOutHelper.RateUsButton` là nút cheat mở popup (bỏ qua).
