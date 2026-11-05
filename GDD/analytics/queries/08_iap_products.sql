-- SKU bán được, số lần là món đầu tiên, giờ từ lúc cài tới lần mua đầu. Doanh thu thật theo GA4 ở query 01.
WITH p AS (
  SELECT uid, event_timestamp AS ts, P_STR(event_params, 'product_id') AS sku, P_NUM(event_params, 'price_usd') AS usd
  FROM ev WHERE event_name = 'iap_success'),
first_p AS (SELECT uid, ARRAY_AGG(sku ORDER BY ts LIMIT 1)[OFFSET(0)] AS first_sku, MIN(ts) AS ts FROM p GROUP BY uid),
inst AS (SELECT uid, MIN(event_timestamp) AS t0 FROM ev WHERE event_name = 'first_open' GROUP BY uid),
f AS (
  SELECT first_sku AS sku, COUNT(*) AS as_first_purchase,
    ROUND(APPROX_QUANTILES((first_p.ts - inst.t0) / 3.6e9, 100)[OFFSET(50)], 1) AS median_h_to_first_buy
  FROM first_p LEFT JOIN inst USING (uid) GROUP BY sku),
s AS (SELECT sku, COUNT(*) AS orders, COUNT(DISTINCT uid) AS buyers, ROUND(SUM(usd), 2) AS list_usd FROM p GROUP BY sku)
SELECT s.*, f.as_first_purchase, f.median_h_to_first_buy
FROM s LEFT JOIN f USING (sku) ORDER BY list_usd DESC;
