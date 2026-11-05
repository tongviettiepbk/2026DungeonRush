-- Tường khó campaign: thắng/thua theo mỗi 10 level, thời gian trận, máu còn lại. Thua nhiều + người dừng nhiều = tường.
WITH m AS (
  SELECT uid, DIV(P_INT(event_params, 'level') - 1, 10) * 10 + 1 AS level_from, P_STR(event_params, 'result') AS result,
    P_NUM(event_params, 'sec') AS sec, P_NUM(event_params, 'hero_hp') AS hp
  FROM ev WHERE event_name = 'match_end' AND P_STR(event_params, 'mode') = 'defaultlevel'),
stop AS (SELECT level_from, COUNT(*) AS players_stopped_here FROM (SELECT uid, MAX(level_from) AS level_from FROM m GROUP BY uid) GROUP BY level_from),
g AS (
  SELECT level_from, COUNT(*) AS matches, COUNT(DISTINCT uid) AS players,
    ROUND(100 * COUNTIF(result = 'win') / COUNT(*), 1) AS win_pct,
    ROUND(AVG(sec), 1) AS avg_sec,
    ROUND(AVG(IF(result = 'win', hp, NULL)), 2) AS avg_hp_on_win
  FROM m GROUP BY level_from)
SELECT g.*, stop.players_stopped_here FROM g LEFT JOIN stop USING (level_from) ORDER BY level_from;
