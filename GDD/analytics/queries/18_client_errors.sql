-- Lỗi phía client theo ngày + version: IAP hỏng lý do gì, không có quảng cáo, load quảng cáo hỏng, boot lâu.
SELECT d, app_info.version AS version, event_name,
  COALESCE(P_STR(event_params, 'reason'), P_STR(event_params, 'result'), P_STR(event_params, 'step'), '') AS detail,
  COUNT(*) AS n, COUNT(DISTINCT uid) AS users,
  ROUND(AVG(P_NUM(event_params, 'sec')), 2) AS avg_sec
FROM ev
WHERE d >= DATE_SUB(CURRENT_DATE(), INTERVAL 7 DAY)
  AND (event_name IN ('iap_fail', 'ads_reward_load_fail', 'ads_reward_show_fail', 'boot_step')
       OR (event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'failed')
       OR (event_name = 'ad_rw_request' AND P_INT(event_params, 'ready') = 0))
GROUP BY d, version, event_name, detail ORDER BY d DESC, n DESC;
