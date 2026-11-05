-- Rewarded theo chỗ đặt: lượt bấm, chưa có quảng cáo, xem hết, tắt ngang, người xem, No Ads bỏ qua.
SELECT P_STR(event_params, 'placement') AS placement,
  COUNTIF(event_name = 'ad_rw_request') AS requests,
  COUNTIF(event_name = 'ad_rw_request' AND P_INT(event_params, 'ready') = 0) AS not_ready,
  COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'finished') AS finished,
  COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'skipped') AS skipped,
  COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'skipped_noads') AS skipped_noads,
  COUNT(DISTINCT IF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'finished', uid, NULL)) AS watchers
FROM ev WHERE event_name IN ('ad_rw_request', 'ad_rw_result')
GROUP BY placement ORDER BY finished DESC;
