-- Thời gian chơi theo ngày tuổi tài khoản: số phiên/ngày, phút/phiên, phút/ngày, trận + quảng cáo mỗi phiên.
WITH s AS (
  SELECT uid, d, P_INT(event_params, 'day_n') AS day_n, P_NUM(event_params, 'duration_sec') / 60 AS m,
    P_INT(event_params, 'matches') AS matches, P_INT(event_params, 'ads_watched') AS ads
  FROM ev WHERE event_name = 'session_end'),
g AS (SELECT uid, d, ANY_VALUE(day_n) AS day_n, COUNT(*) AS sessions, SUM(m) AS mins, SUM(matches) AS matches, SUM(ads) AS ads FROM s GROUP BY uid, d)
SELECT LEAST(day_n, 15) AS day_n, COUNT(*) AS user_days,
  ROUND(AVG(sessions), 2) AS avg_sessions,
  ROUND(APPROX_QUANTILES(mins / sessions, 100)[OFFSET(50)], 1) AS median_min_per_session,
  ROUND(APPROX_QUANTILES(mins, 100)[OFFSET(50)], 1) AS median_min_per_day,
  ROUND(AVG(matches), 1) AS avg_matches,
  ROUND(AVG(ads), 2) AS avg_rewarded
FROM g GROUP BY day_n ORDER BY day_n;
