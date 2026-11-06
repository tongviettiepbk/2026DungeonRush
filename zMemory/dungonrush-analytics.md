---
name: dungonrush-analytics
description: Hệ Firebase Analytics theo Racing đã gắn vào game; doc + query ở GDD/analytics; phương án tăng retention/IAP/ads ở GROWTH_PLAN.md
metadata:
  type: project
---

2026-11-05 đã port tracking của 2026RaceManager vào `Scripts/Analytics/` (Analytics, AnalyticsTracker, MonetizeStats, UaFunnel, AppsflyerAttribution) và gắn khắp game. Test Play qua batch đạt phần boot/phiên/tab/popup/rewarded/IAP/gem/loot/trận/phễu; chưa chạy máy thật, chưa thử event tính năng (clan, wing, cape, mastery...). Đã commit 9d44f195.

Tài liệu ở `GDD/analytics/`: TRACKING_PLAN.md, events.csv, queries/ (18 query, chưa chạy vì chưa có dataset, phải thay `analytics_XXXXXXXXX`), GROWTH_PLAN.md (phát hiện + phương án P0/P1/P2).

**Why:** user coi đây là phần cực kì quan trọng khi chạy game, cần đo để biết nên update gì cho retention, thời gian chơi, IAP, quảng cáo.

**How to apply:**
- Event mới: thêm dòng events.csv trước, gọi `Analytics.Log`. Tiền thì bọc `Analytics.Scope("lý_do")`, thiếu sẽ ra `unknown`.
- KHÔNG bắn event tên `purchase`: Firebase tự thu `in_app_purchase`, doanh thu sẽ trùng; dùng `iap_success` (không value).
- Mốc một lần lưu ở `profile.funnel.marks`, bộ đếm ở `profile.monetize`, đều đi cloud save. Game chưa phát hành nên không có bước bù save cũ.
- Không dựng `Parameter` trước khi SDK sẵn sàng (xem FirebaseServices), mọi lời gọi xếp hàng tối đa 200.
- Phương án có nhãn [LỆCH GỐC] phải được user duyệt theo [[dungonrush-follow-original-strict]].
- Còn phải làm trên console (user): kiểm tra đã liên kết GA, bật BigQuery Daily + Streaming, key event, custom dimension. Remote Config user chốt chưa dùng ([[dungonrush-monetization-sdk]]), A/B phụ thuộc quyết định này.
