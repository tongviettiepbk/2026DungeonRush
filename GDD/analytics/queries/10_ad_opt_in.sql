-- Tỉ lệ nhận quảng cáo: thấy cơ hội → bấm xem → xem hết. Cơ hội = rương bay xuất hiện hoặc mở màn có nút quảng cáo.
WITH o AS (
  SELECT 'rewarded_chest' AS placement, COUNT(*) AS seen, COUNT(DISTINCT uid) AS seen_users FROM ev WHERE event_name = 'rewarded_chest_spawn'
  UNION ALL
  SELECT '2x_offline_collect', COUNT(*), COUNT(DISTINCT uid) FROM ev WHERE event_name = 'screen_view' AND P_STR(event_params, 'screen_name') = 'UIOfflineEarningPopup'
  UNION ALL
  SELECT 'companion_summon_ad', COUNT(*), COUNT(DISTINCT uid) FROM ev WHERE event_name = 'screen_view' AND P_STR(event_params, 'screen_name') = 'UICompanionAdSummonPopup'
  UNION ALL
  SELECT 'dungeon_key', COUNT(*), COUNT(DISTINCT uid) FROM ev WHERE event_name = 'screen_view' AND P_STR(event_params, 'screen_name') = 'UIDungeonPopup'
  UNION ALL
  SELECT 'mining_tool', COUNT(*), COUNT(DISTINCT uid) FROM ev WHERE event_name = 'screen_view' AND P_STR(event_params, 'screen_name') = 'UIMiningPopup'),
r AS (
  SELECT P_STR(event_params, 'placement') AS placement,
    COUNTIF(event_name = 'ad_rw_request') AS requests,
    COUNTIF(event_name = 'ad_rw_result' AND P_STR(event_params, 'result') = 'finished') AS finished
  FROM ev WHERE event_name IN ('ad_rw_request', 'ad_rw_result') GROUP BY placement)
SELECT placement, seen, seen_users, requests, finished,
  ROUND(100 * SAFE_DIVIDE(requests, seen), 1) AS opt_in_pct,
  ROUND(100 * SAFE_DIVIDE(finished, requests), 1) AS finish_pct
FROM o LEFT JOIN r USING (placement) ORDER BY seen DESC;
