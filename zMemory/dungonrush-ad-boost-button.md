---
name: dungonrush-ad-boost-button
description: Nút "Boost x2 +5m" cạnh Loot = boost EXP xem quảng cáo (BoostButton gốc); đã dựng 2026-10-16; icon <sprite=0> = IsAd
metadata:
  type: project
---

Nút Boost ở ForgeUI lobby = **xem quảng cáo → EXP ×AdBoostWorth trong AdBoostDuration giây** (reverse il2cpp BoostButton jvy/jwh/jwn/jwo/jwf, 2026-10-16).
- Mastery bật: hệ số = nhánh AdBoostWorth (mặc định x2, Lvl1 x2.1 … x4), thời lượng = AdBoostDuration (mặc định 300s); tắt → x2 / 300s (EconomyController).
- Xem khi đang chạy → cộng thêm 1 lượt, trần 12 × thời lượng. Hiện từ playerLevel ≥ 3. Nhân dồn với Lifetime/X2/X5 trong ShopService.GetExpMultiplier.
- Nhãn gốc runtime: "<sprite=0>Boost x{0}\n+{1}" (thời lượng format kiểu "5m"; "+5 min" trong scene rip chỉ là chữ đặt sẵn).
- **Icon mặt cười** = TMP rơi về EmojiOne vì ButtonText spriteAsset null → gốc gán `IsAd` (Art/2_Icons/Currency/IsAd.asset).
- Code: ShopService.AdBoost* + UserShopData.adBoostEndTime + UI/MainLobby/BoostButtonView (dùng ForgeButtonView cho hiệu ứng ấn, màu xanh gốc). UIMainLobby.boostButton thay btBoost.
- Test Play OK (Editor ads trả Finished). Chưa commit. Liên quan [[dungonrush-mastery-feature]], [[dungonrush-shop-system]], [[dungonrush-mediation-ads]].
