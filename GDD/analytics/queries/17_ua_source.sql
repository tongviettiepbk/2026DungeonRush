-- Theo nguồn cài: người cài, D1, D7, % đạt bước sâu, % nạp, doanh thu/người.
WITH src AS (
  SELECT uid, ANY_VALUE(P_STR(event_params, 'media_source')) AS source, ANY_VALUE(P_STR(event_params, 'campaign')) AS campaign
  FROM ev WHERE event_name = 'install_attribution' GROUP BY uid),
inst AS (SELECT uid, MIN(d) AS d0 FROM ev WHERE event_name = 'first_open' GROUP BY uid),
u AS (
  SELECT i.uid, i.d0,
    MAX(IF(e.d = DATE_ADD(i.d0, INTERVAL 1 DAY), 1, 0)) AS d1,
    MAX(IF(e.d = DATE_ADD(i.d0, INTERVAL 7 DAY), 1, 0)) AS d7,
    MAX(IF(e.event_name = 'funnel_stage_10', 1, 0)) AS stage10,
    SUM(IF(e.event_name = 'in_app_purchase', e.event_value_in_usd, 0)) AS iap,
    SUM(IF(e.event_name = 'ad_revenue_sdk', P_NUM(e.event_params, 'value'), 0)) AS ad
  FROM inst i JOIN ev e USING (uid) GROUP BY i.uid, i.d0)
SELECT IFNULL(s.source, '?') AS source, IFNULL(s.campaign, '?') AS campaign, COUNT(*) AS installs,
  ROUND(100 * AVG(d1), 1) AS d1_pct, ROUND(100 * AVG(d7), 1) AS d7_pct, ROUND(100 * AVG(stage10), 1) AS stage10_pct,
  ROUND(100 * COUNTIF(iap > 0) / COUNT(*), 2) AS payer_pct,
  ROUND((SUM(iap) + SUM(ad)) / COUNT(*), 3) AS rev_per_install
FROM u LEFT JOIN src s USING (uid) GROUP BY source, campaign ORDER BY installs DESC;
