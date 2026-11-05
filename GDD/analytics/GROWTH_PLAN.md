# Dungeon Duck — Đo gì và làm gì để tăng retention, thời gian chơi, IAP, quảng cáo

Cập nhật: 2026-11-05. Đầu vào: code game, hệ tracking và số liệu thật của Racing, báo cáo ngành.

Ký hiệu:

| Ký hiệu | Nghĩa |
|---|---|
| **[ĐÃ CÓ]** | Đã kiểm tra trong code hoặc số liệu thật |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[LỆCH GỐC]** | Khác game gốc, cần bạn duyệt theo chỉ thị "làm đúng gốc" |
| **[CHỜ SỐ LIỆU]** | Phải có dữ liệu thật mới quyết được |

---

## 1. Kết luận

1. Lớp đo đã gắn xong theo Racing. Game chưa có số liệu thật, nên mọi mức tăng dưới đây là giả thuyết để thử.
2. Điểm hổng lớn nhất về giữ chân: game không có thông báo đẩy, điểm danh, nhiệm vụ ngày.
3. Điểm hổng lớn nhất về IAP: gói No Ads và Starter Pack đang ẩn. Ở Racing, No Ads là món mua đầu của 43% người nạp tuần 26/10 và cùng bản Plus chiếm 47% doanh thu IAP.
4. Quảng cáo có sẵn 7 chỗ đặt. Việc đầu tiên là đo tỉ lệ nhận ở từng chỗ, chưa cần thêm chỗ mới.
5. Muốn thử có đối chứng thì cần Remote Config. Bạn đã chốt chưa dùng lúc này, xem mục 6.
6. Nhóm P0 đã làm xong ngày 2026-11-05. Vì chưa có A/B nên phải so theo version và đọc thận trọng.

## 2. Học được gì từ Racing **[ĐÃ CÓ]**

### 2.1. Luồng event

1. Một cửa `Analytics.Log`, tự gắn ngữ cảnh tiến trình vào mọi event.
2. `session_begin` + `session_end` trả lời người chơi rời ở đâu.
3. `paywall_view` → `iap_click` → `purchase` trả lời popup nào bán được.
4. `ad_rw_request` → `ad_rw_result` trả lời quảng cáo nào được xem.
5. `earn` + `spend` theo lý do trả lời kinh tế lệch ở đâu.
6. Phễu `funnel_*` gửi cả Firebase và AppsFlyer để UA tối ưu.
7. Query BigQuery cố định, chạy lại mỗi tuần.

### 2.2. Số thật tuần 26/10–01/11

| Chỉ số | Racing |
|---|---|
| D1 | 33% |
| D3 | 14% |
| D7 | 5,5% |
| Người cài trong tuần | 1.100 |
| DAU trung bình | 350 |
| Doanh thu tuần | $675 |
| Tỉ trọng IAP | 61% |
| Tỉ trọng quảng cáo | 39% |
| ARPDAU tính lại | $0,28 |
| Rewarded mỗi DAU tính lại | 2,3 |
| eCPM rewarded | $41,85 |

| Định dạng quảng cáo | Phần doanh thu |
|---|---|
| Rewarded | 89% |
| Interstitial | 9% |
| Banner | 1,5% |

### 2.3. Bài học dùng được

- Nhóm rời sớm có 2 kiểu: 32% người chỉ chơi 1 ngày rời trước phút đầu, 35% chơi trên 10 phút rồi không quay lại. Hai kiểu cần hai cách sửa.
- Popup mở nhiều nhất bán kém nhất: Income x2 có 5.041 lượt, 94% đóng dưới 2 giây, 2 đơn.
- Popup người chơi tự mở chuyển đổi cao: Season Pass 7,7%.
- 8 trên 17 đơn đầu nằm trong 3 phút đầu một phiên.
- Người xem nhiều quảng cáo ở lại lâu: nhóm 20+ rewarded có 82% quay lại, nhóm không xem 22%. Đây là tương quan, chưa phải nhân quả.
- Racing thiếu nguồn cài trong BigQuery: 72% lần cài không rõ campaign. Dungeon Duck đã có `install_attribution` ngay từ đầu.
- Mẫu nhỏ dễ đọc sai: 12–17 người nạp chỉ để định hướng.

## 3. Hiện trạng game

| # | Phát hiện | Vị trí |
|---|---|---|
| 1 | Không có thông báo đẩy, điểm danh, nhiệm vụ ngày (đã thêm thông báo + 7 ngày, còn nhiệm vụ ngày) | Toàn bộ `Scripts`, `manifest.json` |
| 2 | No Ads luôn ẩn trên Store (đã bật) | `StoreTabPage.RefreshSections` |
| 3 | Starter Pack, Epic Pack, Dungeon Deal, Resource Deal tắt sẵn (đã bật Starter Pack) | `DecodedData/SHOP_MODEL.md` |
| 4 | Mua No Ads vẫn phải xem quảng cáo ở 3 chỗ (đã sửa) | `BoostButtonView`, `UIDungeonPopupStart`, `UIBossRushPopup` |
| 5 | Thiếu gem chỉ hiện toast, không dẫn sang gói gem (đã sửa) | `ChestStoreCard`, `StoreTabPage` |
| 6 | Weapon Offer tự bật mỗi lần vào game, chồng với popup Offline (đã giới hạn) | `UIMainLobby.cs`, thấy trong test Play |
| 7 | Chưa có interstitial, banner | `MaxAdController` |
| 8 | PvP, Boss Rush, Cape mở ở level 15, cần 137.900 exp | `StaticExperienceData` |
| 9 | Rương gem mở cùng Cape ở level 15 | `ShopService.IsChestStoreOpen` |
| 10 | Tab Clan mở level 20, còn data clan ghi 15 | `UIMainLobby.TAB_UNLOCK_LEVELS`, `StaticClanData` |

Các mục 2, 3, 6, 8, 9, 10 là hành vi gốc v41. Đổi chúng là **[LỆCH GỐC]**.

### 3.1. Kho quảng cáo mỗi ngày

| Chỗ đặt | Trần |
|---|---|
| `rewarded_chest` | 800/ngày, thực tế 1 lượt mỗi vài phút |
| `boost_button` | Cộng dồn tối đa 60 phút |
| `2x_offline_collect` | 1 lần mỗi lần quay lại |
| `dungeon_key` | 3 mỗi dungeon, tổng 9 |
| `companion_summon_ad` | 3 |
| `mining_tool` | 6 mỗi dụng cụ, tổng 18 |
| `boss_rush_ad` | 3 |

Trần không chặn. Câu hỏi là người chơi có muốn xem không, và `10_ad_opt_in.sql` trả lời việc đó.

## 4. Bộ chỉ số

### 4.1. Mốc so sánh

GameAnalytics 2025 không còn số theo thể loại, chỉ có số toàn thị trường.

| Chỉ số | Trung vị | Top 10% | Racing |
|---|---:|---:|---:|
| D1 | 22% | 40% | 33% |
| D7 | gần 4% | 11–12% | 5,5% |
| Phút chơi/ngày | 12 | 40–42 | chưa đo |
| Phút/phiên | 3,1–3,5 | hơn 8 | chưa đo |
| Phiên/ngày | 3,8–3,9 | 9,6 | chưa đo |

Số theo thể loại RPG giữa các nguồn lệch nhau nhiều, không nên dùng làm mục tiêu. Top 25 midcore theo Sensor Tower là 42% / 21% / 11% cho D1 / D7 / D30, đó là trần.

### 4.2. Mục tiêu soft launch **[ĐỀ XUẤT]**

| Chỉ số | Công thức | Query | Mục tiêu |
|---|---|---|---:|
| D1 | Quay lại đúng ngày 1 / người cài | 02 | 35% |
| D7 | Quay lại đúng ngày 7 / người cài | 02 | 10% |
| D30 | Quay lại đúng ngày 30 / người cài | 02 | 3% |
| Phút chơi/ngày | Trung vị `engagement_time_msec` | 01 | 25 |
| Phiên/ngày | Trung bình `session_end` | 15 | 4 |
| Tỉ lệ nạp | Người nạp D30 / người cài | 04 | 2% |
| Rewarded/DAU | `finished` / DAU | 01 | 3 |
| Tỉ lệ nhận quảng cáo | `ad_rw_request` / lượt thấy | 10 | 40% |
| Tỉ lệ có quảng cáo | 1 − `not_ready` / `requests` | 09 | 97% |
| Popup đóng dưới 2 giây | `closed_under_2s` / `views` | 07 | dưới 60% |
| ARPDAU | (IAP + quảng cáo) / DAU | 01 | $0,30 |

Mục tiêu đặt theo mức Racing đang đạt cộng thêm một bậc. Chỉnh lại sau tuần dữ liệu đầu.

### 4.3. Cây chẩn đoán

| Thấy gì | Query | Nghĩa | Sửa ở |
|---|---|---|---|
| Rớt nhiều trước `tap_to_play` | 06 | Boot chậm | 5.1-A |
| Nhiều người dưới 1 phút | 12 | 3 phút đầu kém | 5.1-A |
| Dừng ở một bước `tutorial_step` | 05 | Hướng dẫn kẹt | 5.1-A |
| `win_pct` tụt mạnh ở một mốc | 13 | Tường khó | 5.1-E |
| Chơi trên 10 phút rồi không về | 12 | Thiếu lý do quay lại | 5.1-B, C, D |
| D7 tụt, level 15 đến muộn | 04 | Tính năng mở quá xa | 5.2-A |
| Nhiều lượt thấy, ít lượt nhận quảng cáo | 10 | Thưởng chưa đáng | 5.4-B |
| `not_ready` trên 5% | 09 | Thiếu kho quảng cáo | 5.4-A |
| Popup trên 80% đóng nhanh | 07 | Bật quá dày | 5.3-D |
| `gem_short` nhiều, ít đơn gem | 07 | Hụt chỗ bán | 5.3-C |

## 5. Phương án

Ưu tiên P0 làm trước khi chạy UA, P1 trong tháng đầu, P2 khi có số liệu.

### 5.1. Retention

**A. Sửa 3 phút đầu — P0 [CHỐT] — đã làm**

- Người chưa qua màn nào vào thẳng game, không cần bấm Tap To Play.
- Đo: `06_boot_drop.sql`, `05_ftue.sql`.
- Công sức: nhỏ.

**B. Thông báo đẩy cục bộ — P0 [LỆCH GỐC] [CHỐT] — đã làm**

- `offline_full`: kho offline đầy, dời khỏi giờ ngủ 22h–9h.
- `daily`: 19h, key và vé đã hồi.
- `comeback`: 19h sau 3 ngày vắng.
- Xuống nền thì đặt lịch, quay lại thì huỷ. Tối đa 2 thông báo mỗi ngày.
- Xin quyền sau khi qua màn 10, sớm hơn bản nháp ngày 2 để kịp nhắc quay lại ngày 1.
- Chưa làm các nhắc theo sự kiện: cuốc đầy, Boss Rush mở, Clan War.
- Đo: `session_begin.from_push`, `push_permission`.
- Rủi ro: Android 13 trở lên phải xin quyền.

**C. Thưởng 7 ngày đầu — P0 [LỆCH GỐC] [CHỐT] — đã làm**

- Bản rút gọn của `RoadToPro`: mỗi ngày UTC nhận 1 ô, bỏ lỡ ngày không mất ô, không có nhiệm vụ.
- Hiện từ màn 3 để không chen vào hướng dẫn.
- Thưởng: 20 gem, 100 búa, 30 gem, 200 Bone, 150 búa, 50 gem, rồi 150 gem + 500 Bone.
- Ngày 7 là gem + Bone, chưa phải pet hay vũ khí hiếm như bản nháp.
- Đo: `seven_day_claim` theo ngày; so D7 trước và sau.

**D. Nhiệm vụ ngày — P1 [LỆCH GỐC]**

- 3–5 nhiệm vụ dẫn tới dungeon, Loot, pet, PvP.
- Thưởng gem nhỏ và một ô quảng cáo nhân đôi.
- Đo: `daily_quest_claim`, số mode chơi mỗi ngày từ `14_mode_adoption.sql`.
- Công sức: vừa.

**E. Cân tường khó — P1 [CHỜ SỐ LIỆU]**

- Dùng `13_stage_wall.sql` tìm mốc `win_pct` tụt.
- Hiện thua là lùi 1 màn. Theo dõi người chơi kẹt thua liên tiếp.

### 5.2. Thời gian chơi

**A. Kéo mốc mở tính năng sớm hơn — P1 [LỆCH GỐC] [CHỜ SỐ LIỆU]**

- Xem `funnel_level_15` đến sau bao lâu.
- Nếu trung vị quá 3 ngày thì hạ PvP hoặc Boss Rush về level 10–12.

**B. Chuỗi hành động sau trận — P1 [ĐỀ XUẤT]**

- Hết dungeon gợi ý Loot, hết búa gợi ý xem quảng cáo.
- Đo: `screens_opened`, `matches`, `median_min_per_session` ở query 15.

**C. Mục tiêu tuần — P2 [ĐỀ XUẤT]**

- Boss Rush và Clan War đã có nhịp tuần. Hiện rõ đồng hồ và phần thưởng ngay ở lobby.
- Đo: tỉ lệ người vào lại trong tuần theo `in_clan`.

### 5.3. IAP

**A. Bật No Ads — P0 [LỆCH GỐC] [CHỐT] — đã làm**

- Mọi chỗ đặt rewarded trao thưởng ngay khi đã mua, xử lý tập trung ở `MaxAdController`.
- Mục No Ads nằm đầu Store, mô tả "Get ad rewards instantly".
- Chưa đổi icon quảng cáo trên nút Boost, key dungeon, Boss Rush cho người đã mua.
- Đo: `07_paywall_conversion.sql`, `08_iap_products.sql`.
- Rủi ro: mất doanh thu rewarded của người mua. So doanh thu mỗi người trước và sau.

**B. Bật Starter Pack — P0 [LỆCH GỐC] [CHỐT] — đã làm**

- Từ level 3, đếm ngược 48 giờ, mua được 1 lần. Có popup, nút lobby và mục đầu Store.
- 500 gem + 300 búa, giá $1,99.
- Đo: tỉ lệ mua trên người thấy; `first_purchase` theo SKU.

**C. Dẫn từ thiếu gem sang gói gem — P0 [LỆCH GỐC] [CHỐT] — đã làm**

- Thiếu gem ở rương, Store, Mastery, tạo clan thì chào gói gem nhỏ nhất đủ bù.
- Đo: `gem_short` so với `iap_success` ngay sau đó; `paywall_view.trigger = missing_gems`.

**D. Giới hạn popup tự bật — P0 [CHỐT] — đã làm**

- Tối đa 1 popup bán hàng tự bật mỗi lần mở app, Starter Pack ưu tiên hơn Weapon Offer.
- Đóng dưới 2 giây thì im 24 giờ, nút lobby vẫn còn.
- Xếp hàng lúc vào game: Offline → 7 ngày → chào hàng.
- Đo: `pct_under_2s` ở query 07.
- Lưu ý: Racing cảnh báo không nên giảm đại trà, vì popup đầu phiên là nơi chốt đơn đầu. Cần so có đối chứng.

**E. Gói tiến trình — P2 [LỆCH GỐC]**

- Thử Growth Fund, Gem Bank, Monthly Pass của Racing.
- Ở Racing, Growth Fund đạt 5,3%, Season Pass 7,7%.
- Battle Pass ở đây theo mốc level, không theo mùa. Cân nhắc chuyển sang mùa.

### 5.4. Quảng cáo

**A. Giữ kho quảng cáo đủ — P0 [CHỐT] — đã đo, chờ số liệu**

- Theo dõi `not_ready` theo chỗ đặt.
- Racing có 3% toàn bộ, Income x2 cao nhất 6,5%.
- Nạp sẵn quảng cáo kế ngay khi vừa đóng, code hiện đã làm.

**B. Tăng giá trị chỗ có sẵn — P1 [CHỜ SỐ LIỆU]**

- Rương bay trả 10 + 2 × level búa, hoặc 6 gem.
- Nút Boost là ×2 exp trong 5 phút.
- Chỗ nào tỉ lệ nhận dưới 20% thì tăng thưởng, không tăng tần suất.

**C. Thêm chỗ đặt ở khoảnh khắc thắng — P1 [LỆCH GỐC]**

- Nhân đôi thưởng dungeon sau khi thắng.
- Nhân đôi gem khi lên level.
- Rương miễn phí mỗi ngày trên Store. Ở Racing, các gói miễn phí nằm trong nhóm đặt nhiều lượt nhất.
- Đo: `09_rewarded_placements.sql`, kèm `15_session_depth.sql` để chắc không làm ngắn phiên.

**D. Interstitial — P2 [LỆCH GỐC]**

- Chỉ khi đã có đối chứng: từ ngày 2, người chưa nạp, giãn 180 giây, không trong trận.
- Racing chỉ thu 9% doanh thu quảng cáo từ định dạng này.
- Mục đích phụ là đẩy người chơi mua No Ads.
- Rủi ro: giảm retention. Không bật nếu chưa đo được D7.

**E. Banner — Không nên.** Racing chỉ thu 1,5%, lại chiếm chỗ UI.

## 6. Cách thử

| Cách | Ưu | Nhược |
|---|---|---|
| Remote Config + Firebase A/B | Đổi không cần build, có thống kê sẵn | Phải thêm SDK, bạn đã chốt chưa dùng |
| Tự chia nhóm theo hash `uid` + user property | Không thêm SDK | Đổi cấu hình phải ra build |
| So trước và sau theo version | Dễ nhất | Lẫn nguồn UA, dễ đọc sai |

Đề xuất dùng Remote Config trước khi chạy UA lớn.

### 6.1. Cỡ mẫu

Mỗi nhóm cần số người cài sau, mức tin cậy 95%, lực kiểm định 80%:

| Muốn thấy | Mỗi nhóm |
|---|---:|
| D1 từ 33% lên 36% | 3.900 |
| D7 từ 5,5% lên 7% | 4.100 |
| Tỉ lệ nạp từ 1,3% lên 1,8% | 9.600 |

Với 1.100 lần cài/tuần như Racing, thử retention mất 7 tuần cho 2 nhóm, thử tỉ lệ nạp mất gần 4 tháng. Vì vậy ở quy mô nhỏ nên thử thay đổi lớn, ít thử cùng lúc, và dùng chỉ số sớm như `first_paywall_seen`, `funnel_stage_10`.

## 7. Lộ trình

1. Trước khi chạy UA: làm mục 4 của `TRACKING_PLAN.md`, kiểm tra DebugView trên máy thật.
2. Trước khi chạy UA: tạo `tnt.noads`, `tnt.starterpack` trên Play Console, thử P0 trên máy thật.
3. Tuần 1 sau ra mắt: chạy 02, 06, 12, 13. Chỉ sửa lỗi lớn.
4. Tuần 2: chạy 04 chọn event UA, chạy 07, 09, 10.
5. Tuần 3–4: làm P1 theo cây chẩn đoán.
6. Mỗi tuần: viết `weekly/YYYY-MM-DD.md` như Racing.

## 8. Chưa chốt

Chờ bạn quyết định:

- Duyệt phần [LỆCH GỐC] còn lại, nhóm P1 và P2.
- Có bật Remote Config trước khi chạy UA không.
- Giá 2 gói Mining.
- Consent Mode cho EEA: tài liệu Google yêu cầu truyền `ad_user_data`, `ad_personalization`. Nếu chạy UA ở Đức, Anh như Racing thì nên làm.

Chờ số liệu:

- Toàn bộ mục tiêu ở 4.2.
- Mốc phễu UA.
- Mức thưởng quảng cáo.

## 9. Nguồn

- [GameAnalytics 2026 Mobile & PC Benchmarks](https://www.gameanalytics.com/reports/2026-mobile-pc-gaming-benchmarks)
- [Innovecs: Mobile Game Retention Benchmarks](https://www.innovecsgames.com/blog/mobile-game-retention-benchmarks/)
- [Google Analytics: giới hạn thu thập](https://support.google.com/analytics/answer/9267744)
- [Google Analytics: BigQuery Export](https://support.google.com/analytics/answer/9358801)
- [Firebase: event tự thu](https://support.google.com/firebase/answer/9234069)
- [Firebase Unity: đo in-app purchase](https://firebase.google.com/docs/analytics/unity/measure-in-app-purchases)
- [AdMob: event tự thu](https://support.google.com/admob/answer/9755157?hl=en)
- [Google: Consent mode cho app](https://developers.google.com/tag-platform/security/guides/app-consent)
- [Firebase: DebugView](https://firebase.google.com/docs/analytics/debugview)
- Racing nội bộ: `2026RaceManager/config-data/analytics/` (`ANALYSIS_2026-10-26.md`, `ANALYSIS_2026-10-29.md`, `weekly/2026-10-26-claude.md`)
