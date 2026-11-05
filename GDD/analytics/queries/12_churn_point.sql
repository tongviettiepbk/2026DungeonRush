-- Người chỉ chơi một ngày so với người quay lại: thời gian chơi, số trận, tiến trình, màn cuối.
WITH u AS (
  SELECT uid, COUNT(DISTINCT d) AS days,
    ARRAY_AGG(IF(event_name = 'session_end', P_STR(event_params, 'last_screen'), NULL) IGNORE NULLS
              ORDER BY event_timestamp DESC LIMIT 1)[SAFE_OFFSET(0)] AS last_screen,
    COUNTIF(event_name = 'match_end') AS matches,
    MAX(P_INT(event_params, 'plv')) AS plv,
    MAX(P_INT(event_params, 'stage')) AS stage,
    SUM(IF(event_name = 'user_engagement', P_NUM(event_params, 'engagement_time_msec'), 0)) / 60000 AS play_min
  FROM ev GROUP BY uid)
SELECT IF(days = 1, 'one_day', 'returned') AS cohort, COUNT(*) AS users,
  ROUND(APPROX_QUANTILES(play_min, 100)[OFFSET(50)], 1) AS median_play_min,
  ROUND(100 * COUNTIF(play_min < 1) / COUNT(*), 1) AS pct_under_1_min,
  ROUND(100 * COUNTIF(play_min < 3) / COUNT(*), 1) AS pct_under_3_min,
  ROUND(100 * COUNTIF(play_min >= 10) / COUNT(*), 1) AS pct_over_10_min,
  APPROX_QUANTILES(matches, 100)[OFFSET(50)] AS median_matches,
  APPROX_QUANTILES(plv, 100)[OFFSET(50)] AS median_plv,
  APPROX_QUANTILES(stage, 100)[OFFSET(50)] AS median_stage,
  APPROX_TOP_COUNT(last_screen, 5) AS top_last_screens
FROM u GROUP BY cohort;
