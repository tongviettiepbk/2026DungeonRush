-- Kinh tế: nguồn vào (source) và chỗ tiêu (item_name) của mọi loại tiền. reason = unknown là lỗ hổng cần bọc Analytics.Scope.
SELECT P_STR(event_params, 'virtual_currency_name') AS currency,
  IF(event_name = 'earn_virtual_currency', 'earn', 'spend') AS dir,
  IF(event_name = 'earn_virtual_currency', P_STR(event_params, 'source'), P_STR(event_params, 'item_name')) AS reason,
  COUNT(*) AS n, COUNT(DISTINCT uid) AS users, ROUND(SUM(P_NUM(event_params, 'value'))) AS amount
FROM ev WHERE event_name IN ('earn_virtual_currency', 'spend_virtual_currency')
GROUP BY currency, dir, reason ORDER BY currency, dir, amount DESC;
