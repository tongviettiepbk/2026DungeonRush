-- Nhóm monetize suy từ event: số ngày hoạt động + doanh thu theo nhóm.
WITH u AS (
  SELECT uid,
    SUM(IF(event_name = 'in_app_purchase', event_value_in_usd, 0)) AS iap_usd,
    COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'finished') AS rw,
    SUM(IF(event_name = 'ad_revenue_sdk', P_NUM(event_params, 'value'), 0)) AS ad_usd,
    COUNT(DISTINCT d) AS days
  FROM ev GROUP BY uid)
SELECT CASE WHEN iap_usd > 0 THEN '1_payer' WHEN rw >= 20 THEN '2_rw_20p' WHEN rw >= 5 THEN '3_rw_5_19'
            WHEN rw >= 1 THEN '4_rw_1_4' ELSE '5_non_monetizer' END AS seg,
  COUNT(*) AS users,
  ROUND(100 * COUNT(*) / SUM(COUNT(*)) OVER (), 1) AS pct_users,
  ROUND(AVG(days), 2) AS avg_active_days,
  ROUND(100 * COUNTIF(days >= 2) / COUNT(*), 1) AS pct_2d_plus,
  ROUND(SUM(iap_usd), 2) AS iap_usd,
  ROUND(SUM(ad_usd), 2) AS ad_usd,
  ROUND((SUM(iap_usd) + SUM(ad_usd)) / COUNT(*), 3) AS rev_per_user
FROM u GROUP BY seg ORDER BY seg;
