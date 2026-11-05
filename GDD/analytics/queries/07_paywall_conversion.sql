-- Popup bán hàng: lượt mở, người thấy, tự bật, đóng dưới 2 giây, bấm mua, mua thật.
WITH v AS (
  SELECT P_STR(event_params, 'popup') AS popup, COUNT(*) AS views, COUNT(DISTINCT uid) AS viewers,
    COUNTIF(P_STR(event_params, 'trigger') = 'auto') AS auto_views
  FROM ev WHERE event_name = 'paywall_view' GROUP BY popup),
c AS (
  SELECT P_STR(event_params, 'popup') AS popup, COUNTIF(P_NUM(event_params, 'sec_open') < 2) AS closed_under_2s
  FROM ev WHERE event_name = 'paywall_close' GROUP BY popup),
k AS (SELECT P_STR(event_params, 'popup') AS popup, COUNT(*) AS iap_clicks FROM ev WHERE event_name = 'iap_click' GROUP BY popup),
p AS (
  SELECT P_STR(event_params, 'popup') AS popup, COUNT(*) AS purchases, ROUND(SUM(P_NUM(event_params, 'price_usd')), 2) AS list_usd
  FROM ev WHERE event_name = 'iap_success' GROUP BY popup)
SELECT popup, views, viewers, auto_views, closed_under_2s,
  ROUND(100 * SAFE_DIVIDE(closed_under_2s, views), 1) AS pct_under_2s,
  iap_clicks, purchases, list_usd,
  ROUND(100 * SAFE_DIVIDE(purchases, viewers), 2) AS buy_pct_of_viewers
FROM v LEFT JOIN c USING (popup) LEFT JOIN k USING (popup) LEFT JOIN p USING (popup)
ORDER BY views DESC;

-- Bấm mua bằng gem mà thiếu (chạy riêng): chỗ nào nên dẫn sang gói gem.
-- SELECT P_STR(event_params, 'sink') AS sink, COUNT(*) AS n, COUNT(DISTINCT uid) AS users FROM ev WHERE event_name = 'gem_short' GROUP BY sink ORDER BY n DESC;
