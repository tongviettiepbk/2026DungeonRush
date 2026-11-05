# Tracking plan — Dungeon Duck

Cập nhật: 2026-11-05. Lấy khung từ Racing (`2026RaceManager/config-data/analytics/TRACKING_PLAN.md`).

- Cửa bắn event duy nhất: `Scripts/Analytics/Analytics.cs`.
- Từ điển event: [events.csv](events.csv). Thêm event mới thì thêm dòng CSV trước.
- Query BigQuery: [queries/](queries). Chạy `python3 queries/run_all.py [NN ...]`.
- Phương án tăng chỉ số: [GROWTH_PLAN.md](GROWTH_PLAN.md).

## 1. Luồng event

1. Chỗ gọi bắn `Analytics.Log(tên, tham số)`.
2. `Analytics` gắn 5 ngữ cảnh `plv, stage, day_n, session_n, payer`.
3. `FirebaseServices` xếp hàng tới khi SDK sẵn sàng, rồi cắt theo giới hạn GA4 và gửi.
4. Editor chỉ in Console với tiền tố `[Analytics]`, không gửi.
5. Bước phễu (`UaFunnel`) gửi thêm sang AppsFlyer cùng tên.

| Lớp | Việc |
|---|---|
| `Analytics` | Event gameplay, ngữ cảnh, phiên, màn hình, paywall, ví, user property |
| `AnalyticsTracker` | Vào MainGame, xuống nền, thoát app, nhịp 1 giây |
| `MonetizeStats` | Bộ đếm IAP + rewarded trong save, nhóm monetize |
| `UaFunnel` | Mốc 1 lần mỗi tài khoản, trong save |
| `AppsflyerAttribution` | Nguồn cài → event + user property |
| `FirebaseServices` | Chạm SDK Firebase, hàng đợi 200 lời gọi |

## 2. Khác Racing có chủ ý

| Racing | Dungeon Duck | Lý do |
|---|---|---|
| Event `purchase` có `value` | `iap_success` không có `value` | Firebase đã tự thu `in_app_purchase`; bắn cả hai là doanh thu trùng |
| Dựng `Parameter` lúc gọi `Log` | Dựng ngay trước lúc gửi | Tạo sớm là hỏng SDK cả phiên |
| Mốc lần đầu lưu PlayerPrefs | Lưu trong save | Cài lại máy không bắn lại |
| Nâng cấp gom 2 giây | Loot gom 3 giây, tối đa 60 giây | Auto Loot chạy liên tục |
| Mọi loại tiền bắn từng lượt | Chỉ GEM từng lượt, còn lại gom theo phiên | Quặng, búa đổi liên tục |
| Có bước bù save cũ | Bỏ | Game chưa phát hành |
| Tên user property cắt 36 ký tự | Cắt 24 ký tự | Giới hạn GA4 là 24 |

## 3. Nhóm monetize

| Nhóm | Điều kiện |
|---|---|
| `payer` | `iapCount > 0` trong cả đời tài khoản |
| `ad_watcher` | Có rewarded xem hết trong 7 ngày gần nhất |
| `non_monetizer` | Còn lại |

- Bộ đếm nằm ở `UserProfileData.monetize`, đi theo cloud save.
- `iapUsdTotal` là giá niêm yết, không phải tiền thật thu.
- Hai SKU `tnt.mining_offer` và `tnt.mining_offer_large` chưa có giá trong `DEFAULT_PRICES` nên `price_usd = 0`. Thêm giá khi chốt.

## 4. Cấu hình phải làm trên console

Các việc này không làm được từ code:

1. Firebase Console → Project settings → Integrations: kiểm tra đã liên kết Google Analytics. `google-services.json` không cho biết việc này.
2. Integrations → BigQuery → bật Daily + Streaming, chọn region US.
3. Thay `analytics_XXXXXXXXX` trong `queries/_header.sql` bằng dataset vừa tạo.
4. GA4 → Admin → Data retention → 14 tháng.
5. Đánh dấu key event: `first_purchase`, `in_app_purchase`, `funnel_stage_10`, `funnel_return_d2`.
6. Tạo custom dimension cho tham số cần xem trên console: `placement`, `popup`, `mode`, `level`, `result`, `plv`, `stage`.
7. Không liên kết AdMob với Firebase: `ad_impression` của MAX đã mang doanh thu, liên kết thêm dễ trùng.
8. AppsFlyer: map `funnel_*` và `first_purchase` sang event từng mạng quảng cáo.

Trên BigQuery chạy thêm `gcloud auth login` trước `run_all.py`, máy này chưa cài `gcloud`.

## 5. Phễu UA

Mỗi bước bắn 1 lần mỗi tài khoản, kèm `sec_since_start`.

| Event | Nghĩa |
|---|---|
| `funnel_loot_1` | Loot lần đầu |
| `funnel_stage_1` | Thắng màn đầu |
| `funnel_stage_10` | Qua màn 10 |
| `funnel_stage_30` | Qua màn 30 |
| `funnel_level_5` | Lên level 5, mở Pet |
| `funnel_level_15` | Lên level 15, mở PvP + Boss Rush + Cape |
| `funnel_dungeon_1` | Thắng dungeon đầu |
| `funnel_ad_1` | Xem hết rewarded đầu |
| `funnel_return_d2` | Mở app sau 24 giờ tuổi tài khoản |
| `funnel_return_d7` | Mở app sau 6 ngày tuổi tài khoản |
| `first_purchase` | Đơn tiền thật đầu tiên |

Luật chọn event tối ưu cho UA (của Racing): ít nhất 85% payer đã qua trước khi mua, 25–50% người cài đạt, đạt trong 24 giờ.
Mốc trên là phỏng đoán. Chạy `04_ua_funnel.sql` khi có đủ người rồi chọn lại.

## 6. Giới hạn cần nhớ

| Giới hạn | Số |
|---|---|
| Tên event riêng mỗi người dùng app | 500 |
| Tham số mỗi event | 25 |
| Dài tên event, tên tham số | 40 |
| Dài giá trị tham số | 100 |
| User property | 25 |
| Dài tên user property | 24 |
| Dài giá trị user property | 36 |
| Export Daily bản chuẩn | 1 triệu event/ngày |
| Giá Streaming export | $0,05/GB, khoảng 600.000 event/GB |

Đang dùng 18 user property. Ước tính 120–200 event mỗi DAU mỗi ngày, chạm trần Daily ở khoảng 5.000–8.000 DAU. Khi đó dùng bộ lọc export bỏ `screen_close` trước.

## 7. Kiểm tra trên máy thật

```bash
adb shell setprop debug.firebase.analytics.app com.zone.dungeon.duck.quack.slash
```

Mở Firebase Console → Analytics → DebugView, chơi một vòng: Loot, thắng màn, xem quảng cáo, mua thử. Xong thì tắt:

```bash
adb shell setprop debug.firebase.analytics.app .none.
```

## 8. Tình trạng

Đã làm:

- Lớp Analytics + 48 dòng event, gắn vào game. Biên dịch sạch với define Editor lẫn máy thật.
- Test Play qua batch: boot, phiên, tab, popup, rewarded, IAP, gem, loot, trận, phễu, ra đúng tham số.
- 18 query BigQuery, chưa chạy vì chưa có dataset.

Chưa làm:

- Chưa chạy trên máy thật nên chưa thấy event lên DebugView.
- Chưa bắn thử các event tính năng: clan, chat, wing, cape, mastery, pet, dungeon sweep, attribution.
- Chưa có `SetConsent` cho Consent Mode.
- Event mới của nhóm P0 (`seven_day_claim`, `gem_short`) đã bắn trong test Play; `push_permission` và `from_push` chỉ chạy trên máy thật.
