-- Phễu UA: % người cài đạt từng bước, trung vị thời điểm, % đạt trong 24h, % payer đã qua bước TRƯỚC đơn đầu, tỉ lệ nạp
-- của người đạt. Luật chọn event tối ưu ở TRACKING_PLAN.md mục 5.
WITH steps AS (
  SELECT uid, event_name AS step, MIN(event_timestamp) AS ts, MIN(P_NUM(event_params, 'sec_since_start')) AS sec
  FROM ev WHERE STARTS_WITH(event_name, 'funnel_') GROUP BY uid, step),
n AS (SELECT COUNT(DISTINCT uid) AS installs FROM ev WHERE event_name = 'first_open'),
pay AS (SELECT uid, MIN(event_timestamp) AS ts FROM ev WHERE event_name = 'in_app_purchase' GROUP BY uid)
SELECT step, COUNT(*) AS users,
  ROUND(100 * COUNT(*) / ANY_VALUE(installs), 1) AS pct_installs,
  ROUND(APPROX_QUANTILES(sec / 60, 100)[OFFSET(50)], 1) AS median_min,
  ROUND(100 * COUNTIF(sec <= 86400) / COUNT(*), 1) AS pct_within_24h,
  ROUND(100 * SAFE_DIVIDE(COUNTIF(p.ts > s.ts), (SELECT COUNT(*) FROM pay)), 1) AS pct_payers_passed_before_buy,
  ROUND(100 * COUNTIF(p.uid IS NOT NULL) / COUNT(*), 2) AS pay_rate_of_reachers
FROM steps s CROSS JOIN n LEFT JOIN pay p USING (uid)
GROUP BY step ORDER BY users DESC;
