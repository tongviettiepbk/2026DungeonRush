-- Tân thủ: bước tutorial + mốc "lần đầu": bao nhiêu % người cài đạt, sau bao lâu.
WITH m AS (
  SELECT uid, P_STR(event_params, 'step') AS k, P_NUM(event_params, 'sec_since_start') AS sec, 'tutorial' AS kind
  FROM ev WHERE event_name = 'tutorial_step'
  UNION ALL
  SELECT uid, P_STR(event_params, 'milestone'), P_NUM(event_params, 'sec_since_start'), 'milestone'
  FROM ev WHERE event_name = 'ftue_milestone'),
n AS (SELECT COUNT(DISTINCT uid) AS installs FROM ev WHERE event_name = 'first_open')
SELECT kind, k, COUNT(DISTINCT uid) AS users,
  ROUND(100 * COUNT(DISTINCT uid) / ANY_VALUE(installs), 1) AS pct_installs,
  ROUND(APPROX_QUANTILES(sec / 60, 100)[OFFSET(50)], 1) AS median_min,
  ROUND(APPROX_QUANTILES(sec / 60, 100)[OFFSET(90)], 1) AS p90_min
FROM m CROSS JOIN n GROUP BY kind, k ORDER BY users DESC;
