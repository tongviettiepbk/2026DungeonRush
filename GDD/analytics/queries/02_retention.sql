-- Retention theo ngày cài (first_open): % quay lại đúng ngày D1/D3/D7. Cohort chưa đủ tuổi để trống.
WITH inst AS (SELECT uid, MIN(d) AS d0 FROM ev WHERE event_name = 'first_open' GROUP BY uid),
act AS (SELECT DISTINCT uid, d FROM ev),
maxd AS (SELECT MAX(d) AS m FROM ev)
SELECT d0, COUNT(DISTINCT i.uid) AS installs,
  IF(DATE_ADD(d0, INTERVAL 1 DAY) <= m, ROUND(100 * COUNT(DISTINCT IF(a.d = DATE_ADD(d0, INTERVAL 1 DAY), a.uid, NULL)) / COUNT(DISTINCT i.uid), 1), NULL) AS d1_pct,
  IF(DATE_ADD(d0, INTERVAL 3 DAY) <= m, ROUND(100 * COUNT(DISTINCT IF(a.d = DATE_ADD(d0, INTERVAL 3 DAY), a.uid, NULL)) / COUNT(DISTINCT i.uid), 1), NULL) AS d3_pct,
  IF(DATE_ADD(d0, INTERVAL 7 DAY) <= m, ROUND(100 * COUNT(DISTINCT IF(a.d = DATE_ADD(d0, INTERVAL 7 DAY), a.uid, NULL)) / COUNT(DISTINCT i.uid), 1), NULL) AS d7_pct
FROM inst i
JOIN act a USING (uid)
CROSS JOIN maxd
GROUP BY d0, m
ORDER BY d0;
