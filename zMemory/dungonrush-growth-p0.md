---
name: dungonrush-growth-p0
description: Nhóm P0 tăng retention/IAP 2026-11-05: No Ads, Starter Pack, popup thiếu gem, giới hạn popup tự bật, người mới vào thẳng game, thưởng 7 ngày, thông báo đẩy; đều LỆCH GỐC có chủ ý
metadata:
  type: project
---

User duyệt 2026-11-05 ("Làm nhóm P0"), các mục này LỆCH GỐC có chủ ý, đừng gỡ theo chỉ thị làm đúng gốc ([[dungonrush-follow-original-strict]]). Chi tiết + cách đo ở `GDD/analytics/GROWTH_PLAN.md`.

- No Ads: xử lý tập trung ở `MaxAdController.ShowRewardedVideoAd`, mọi placement trả Finished ngay. Store hiện No Ads + Starter Pack ở đầu trang; ribbon Daily Deals luôn bật (đã bỏ field OffersRibbonParent).
- Starter Pack: `StarterPackService`, từ level 3, 48 giờ, 1 lần; `UIStarterPackPopup`, nút lobby `StarterPackButtonView`.
- `AutoOfferGate`: 1 popup bán hàng tự bật mỗi lần mở app, Starter trước Weapon Offer, đóng dưới 2 giây thì im 24 giờ. Vào game xếp hàng Offline → 7 ngày → chào hàng.
- `UIMissingGemsPopup`: thay toast thiếu gem ở chest, Store, Mastery, tạo clan; chọn gói nhỏ nhất đủ bù.
- Login: `passedStageId == 0` thì tự vào game.
- `SevenDayService` + `UISevenDayPopup`: số thưởng là đề xuất, chưa cân kinh tế; mốc lưu ở profile.
  - 2026-11-10: thêm dòng "Tap anywhere to close" (key `Popup.SevenDay.TapToClose`, 10 ngôn ngữ) dưới khung; mọi Graphic trong khung tắt raycast (trừ nút Claim) nên bấm đâu cũng đóng. Sửa thẳng prefab, builder có `AddTapToClose` tương ứng.
- `LocalPushController` + package `com.unity.mobile.notifications` 2.4.2: 3 thông báo, xin quyền sau màn 10.
- 3 popup sinh bằng `Scripts/Shop/Editor/RetentionUIBuilder` (Tools/DungeonRush/Build Retention UI) từ khung UIMiningOfferPopup; chạy lại sẽ dựng lại popup với GUID mới, ghi đè chỉnh tay.

Test Play qua batch đạt 24 kiểm tra (2026-11-05); build APK batch đạt, APK có POST_NOTIFICATIONS; nhánh iOS biên dịch đạt. Chưa chạy máy thật, đã commit 9d44f195. Thông báo chưa bật đặt lại sau khi máy khởi động lại (không có RECEIVE_BOOT_COMPLETED).

**Why:** Racing cho thấy No Ads là cửa đơn đầu, popup tự bật dày bị đóng phản xạ, và game không có lý do quay lại.

**How to apply:** Sản phẩm `tnt.noads`, `tnt.starterpack` phải tạo trên store. Thông báo đẩy chỉ kiểm tra được trên máy thật. Xem [[dungonrush-analytics]].
