-- Tính năng nào được dùng: số người chơi từng mode, trận/người, thắng %, thoát giữa chừng %.
SELECT P_STR(event_params, 'mode') AS mode, COUNT(DISTINCT uid) AS players, COUNT(*) AS matches,
  ROUND(COUNT(*) / COUNT(DISTINCT uid), 1) AS matches_per_player,
  ROUND(100 * COUNTIF(P_STR(event_params, 'result') = 'win') / COUNT(*), 1) AS win_pct,
  ROUND(100 * COUNTIF(P_STR(event_params, 'result') = 'quit') / COUNT(*), 1) AS quit_pct,
  ROUND(APPROX_QUANTILES(P_NUM(event_params, 'sec'), 100)[OFFSET(50)], 1) AS median_sec
FROM ev WHERE event_name = 'match_end' GROUP BY mode ORDER BY players DESC;
