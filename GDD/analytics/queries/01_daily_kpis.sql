-- KPI theo ngày: DAU, cài mới, payer, doanh thu, ARPDAU, phút chơi mỗi người.
-- IAP đọc từ in_app_purchase (Firebase tự thu, đã loại đơn test). iap_success không mang value nên không đếm trùng.
WITH play AS (
  SELECT d, uid, SUM(P_NUM(event_params, 'engagement_time_msec')) / 60000 AS play_min
  FROM ev WHERE event_name = 'user_engagement' GROUP BY d, uid),
m AS (SELECT d, APPROX_QUANTILES(play_min, 100)[OFFSET(50)] AS median_play_min FROM play GROUP BY d),
k AS (
  SELECT d,
    COUNT(DISTINCT uid) AS dau,
    COUNT(DISTINCT IF(event_name = 'first_open', uid, NULL)) AS installs,
    COUNT(DISTINCT IF(event_name = 'in_app_purchase', uid, NULL)) AS payers,
    SUM(IF(event_name = 'in_app_purchase', event_value_in_usd, 0)) AS iap_usd,
    SUM(IF(event_name = 'ad_revenue_sdk', P_NUM(event_params, 'value'), 0)) AS ad_usd,
    COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'finished') AS rewarded
  FROM ev GROUP BY d)
SELECT d, dau, installs, payers,
  ROUND(iap_usd, 2) AS iap_usd, ROUND(ad_usd, 2) AS ad_usd,
  ROUND((iap_usd + ad_usd) / dau, 3) AS arpdau,
  ROUND(rewarded / dau, 2) AS rewarded_per_dau,
  ROUND(median_play_min, 1) AS median_play_min,
  ROUND(100 * payers / dau, 2) AS pay_pct
FROM k LEFT JOIN m USING (d) ORDER BY d;
