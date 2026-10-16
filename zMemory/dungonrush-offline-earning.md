---
name: dungonrush-offline-earning
description: Thưởng offline "Combat Summary" dựng 2026-10-13 theo gốc (OfflineEarnningPopup reverse il2cpp) — công thức, số GameResources, file đã làm, phần chưa làm
metadata:
  type: project
---
Popup thưởng offline lúc mới vào game: `UIOfflineEarningPopup` (UIKey.OfflineEarningPopup, `Resources/Prefabs/UI/UIOfflineEarningPopup`), logic `Scripts/OfflineEarning/OfflineEarningService.cs`, save `UserOfflineEarningData.collectTime` (key_user_offline_earning), builder `OfflineEarningPrefabBuilder` (menu Tools/DungeonRush/Build Offline Earning UI — chạy lại là GHI ĐÈ prefab) từ rip `Prefabs/UI/Settings/OfflineEarningPopup`. Test Play OK 2026-10-13 (bật popup, x2, heartbeat), CHƯA commit.

**Logic gốc (reverse libil2cpp v41, OfflineEarnningPopup kpd/kpe/kpl + GameplayUI kjb/kjc/kjd):**
- GameResources: MaxOfflineSeconds = 14400, HammerPerMinute = 1 (BonePerMinute 60 và OfflineEarningExpPercent 3 KHÔNG dùng trong popup).
- Đang chơi: GameplayUI.Update mỗi 10s đặt `OfflineDurationCollectTime = now` (ngừng khi popup đang chờ nhận). Vào game: chưa có mốc → đặt mốc, không bật; có mốc và vắng ≥ 60s → bật popup.
- duration = min(now − mốc, 14400 × Mastery MaxOfflineTime); minutes = duration / 60; worth = Mastery OfflineEarningWorth.
- exp/phút = max(1, round(49 + level màn campaign)) (= exp thắng 1 màn), 0 nếu playerLevel đã max; exp = round(worth × exp/phút × minutes); rương = round(worth × 1 × minutes); "Defeated" = round(rand(0.98..1.02) × 0.15 × duration) chỉ để hiển thị.
- Nút X = nhận ×1 (không có nút bỏ qua); "Collect x2" = quảng cáo `2x_offline_collect` (No Ads thì nhận luôn). Nhận xong: mốc = now − (elapsed % 60); exp cộng KHÔNG qua boost exp Store (hdq cờ boost = false).
- Số hiển thị kiểu K/M (`ShopUIUtil.FormatAmount`), thời gian `ShopUIUtil.FormatDuration` (đã nâng lên đủ d/h/m/s, tối đa 2 đơn vị như ev.ehx).

**Project khác gốc:** dùng giờ UTC của máy (`ShopService.Now()`), gốc dùng giờ server. Hook ở `UIMainLobby.Start` + `UpdateOfflineHeartbeat`; AutoSaveManager lo ghi mốc.

**Reverse 2026-10-17: nút lobby BỊ TẮT ở gốc v41** — GameplayUI.kim/kka đều SetActive(false), OfflineEarningButton.kow (hàm hiện) không ai gọi; QuickEarnings cũng ẩn → chờ user quyết có làm không. **Chưa làm:** nút OfflineEarningButton ở lobby (chấm đỏ khi tích ≥ 600s, mở lại popup) + `LastManualCollectTime`; trạng thái nút x2 "Loading" khi ads chưa sẵn (MediationAds chưa có API kiểm tra); hiệu ứng icon bay về lobby (kpu); các khối bản rip để TẮT sẵn không dùng: DurationText, RewardParent, Coin/HammerPerSecond (thanh fill theo phút), QuickEarnings (User.QuickEarningsDailyCount — tính năng gốc đã ẩn).
**Bẫy test Play:** `UserData.LoadDone` đặt lại `isDisableSaveDataLocal = false` → phải bật cờ SAU khi màn Login hiện nút Tap To Play. Muốn popup bật: ghi PlayerPrefs `key_user_offline_earning` = {"collectTime": now − N} trước khi Play, test xong xoá key.
Xem [[dungonrush-reverse-native-il2cpp]], [[dungonrush-mastery-feature]], [[dungonrush-exp-reward-formula]], [[dungonrush-unity-mcp-editmode-test]].
