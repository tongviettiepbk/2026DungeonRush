-- Người mua lần đầu đang ở đâu: món, popup, level, màn, số phút từ lúc cài, phiên thứ mấy.
SELECT uid, P_STR(event_params, 'product_id') AS sku, P_STR(event_params, 'popup') AS popup,
  P_NUM(event_params, 'price_usd') AS price_usd, P_INT(event_params, 'plv') AS plv, P_INT(event_params, 'stage') AS stage,
  P_INT(event_params, 'day_n') AS day_n, P_INT(event_params, 'session_n') AS session_n
FROM ev WHERE event_name = 'iap_success' AND P_INT(event_params, 'nth_purchase') = 1
ORDER BY event_timestamp DESC LIMIT 200;
