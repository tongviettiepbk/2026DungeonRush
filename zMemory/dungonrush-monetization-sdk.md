---
name: dungonrush-monetization-sdk
description: IAP, MAX, AppsFlyer, Firebase Analytics/Crashlytics theo 2026RaceManager; mã sản phẩm tnt.*, verifypurchase đã deploy; chưa chạy máy thật, chưa cấu hình Play Console
metadata:
  type: project
---

Tích hợp 2026-11-04, đã commit 520b5d02 (2026-11-05). Lớp SDK kế thừa base, chỗ gọi cũ giữ nguyên: `MaxAdController : MediationAds`, `IapController : PurchaseController`, tạo trong `Root.Start`. Thêm `FirebaseServices`, `AppsflyerHelper`, `MonetizationSettings` (ô key) ở Scripts/Network. Compile qua cả Android lẫn iOS. Test Play qua batch 2026-11-04 đạt 24/24 trên fake store (mua, huỷ, phát lại đơn, UI lỗi sau khi trao, trả quyền NoAds); chưa chạy máy thật, chưa test quảng cáo thật.

Khác RaceManager có chủ ý:
- Không dùng define `MAX`: SDK luôn nằm trong project, bỏ được bẫy thiếu define.
- Chỉ rewarded, vì 7 chỗ gọi đều là rewarded. Không có AdMob riêng (App Open/native).
- Đơn phát lại trao theo mã sản phẩm qua `OnPurchased`, không mất hàng; sổ `grantedTransactionIds` chặn trao 2 lần. NoAds/BattlePass/LifetimeBoost là NonConsumable, được trả lại khi vào MainGame.

**Why:** user yêu cầu giống 2026RaceManager, phần nào có rồi thì bỏ qua. User chốt KHÔNG dùng Firebase Remote Config lúc này, cần thì tự thêm sau; đừng import lại.

**How to apply:**
- Đã điền 2026-11-04 (coi là của Android, user chưa nói rõ nền tảng): SDK key + AdMob App ID ở AppLovinSettings.asset; ad unit rewarded/inter + AppsFlyer dev key ở `MonetizationSettings`. Inter chỉ lưu ID, game chưa có chỗ gọi nên chưa code.
- Adapter MAX: đã chép 9 mạng từ RaceManager + Force Resolve 2026-11-04. Vungle bên RaceManager chỉ có iOS nên Android thực tế 8 mạng; muốn Liftoff trên Android thì cài qua Integration Manager.
- Build APK batch đạt 2026-11-04 (15 phút, 113 MB, arm64, ký debug): đủ MAX, 8 adapter, AppsFlyer, Billing, Analytics, Crashlytics; AdMob ID trong manifest; SDK key MAX nằm ở res json (applovin_settings), KHÔNG có trong manifest là đúng thiết kế 8.x. Chưa cài lên máy thật. Build sinh `.utmp/` chưa ignore, xoá hoặc thêm .gitignore.
- Consent flow đã bật 2026-11-05, TẠM dùng link /privacy + /terms của Racing Tycoon theo yêu cầu user; 2 trang ghi tên game + package Racing nên PHẢI thay trước khi phát hành. MAX bắt buộc Privacy URL khi bật flow, Terms tuỳ chọn. Còn phải tạo thông báo GDPR trong AdMob.
- Còn thiếu: keystore, link chính sách riêng của game.
- Mã sản phẩm: tiền tố đổi `lootio.*` → `tnt.*` 2026-11-05 trong Assets/_Assets (DecodedData giữ nguyên làm reference). Bundle ID Android `com.zone.dungeon.duck.quack.slash`.
- Xác minh server: hàm `verifypurchase` (server/functions/src/iap.ts + iapVerify.ts) đã deploy prod 2026-11-05, client gọi trong `IapController.VerifyThenGrant` (chỉ Android, Editor và iOS trao thẳng). CHƯA chạy với biên lai thật: còn phải bật Google Play Android Developer API và mời SA `970487268119-compute@developer.gserviceaccount.com` vào Play Console (README server). Khác RaceManager có chủ ý: đơn chưa khép + cùng uid hỏi lại vẫn `ok` (phản hồi mất giữa đường không làm mất hàng), uid khác → `invalid` (không khép, Google tự hoàn 3 ngày), đã khép → `duplicate`. Đường trả quyền NonConsumable ở `OnPurchasesFetched` CHƯA qua server.
- iOS còn thiếu Bundle ID, GoogleService-Info.plist, nút Restore.
- Bẫy: gói AppsFlyer `.unitypackage` kèm EDM 1.2.186, import là đè tệp khởi động của EDM 1.2.189 kể cả guid `.meta`. Import lại phải `git restore` thư mục `Assets/ExternalDependencyManager`. AppsFlyer đang là 6.17.81 trong Assets/, MAX 8.6.6, Unity IAP 5.4.4 (RaceManager là 6.18.1 / 8.6.2 / 5.4.2).

Liên quan [[dungonrush-mediation-ads]], [[dungonrush-shop-system]].
