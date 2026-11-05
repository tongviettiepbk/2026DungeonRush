-- Rớt ở đâu trong 3 phút đầu: boot → bấm Tap To Play → vào game → loot đầu → thắng màn đầu.
WITH s AS (
  SELECT uid,
    MAX(IF(event_name = 'first_open', 1, 0)) AS a_install,
    MAX(IF(event_name = 'boot_step' AND P_STR(event_params, 'step') = 'user_load', 1, 0)) AS b_boot_start,
    MAX(IF(event_name = 'boot_complete', 1, 0)) AS c_boot_done,
    MAX(IF(event_name = 'boot_step' AND P_STR(event_params, 'step') = 'tap_to_play', 1, 0)) AS d_tap,
    MAX(IF(event_name = 'session_begin', 1, 0)) AS e_session,
    MAX(IF(event_name = 'funnel_stage_1', 1, 0)) AS f_stage1,
    MAX(IF(event_name = 'funnel_loot_1', 1, 0)) AS g_loot1,
    MAX(IF(event_name = 'funnel_stage_10', 1, 0)) AS h_stage10
  FROM ev GROUP BY uid HAVING a_install = 1)
SELECT COUNT(*) AS installs, SUM(b_boot_start) AS boot_start, SUM(c_boot_done) AS boot_done, SUM(d_tap) AS tap_to_play,
  SUM(e_session) AS session, SUM(f_stage1) AS stage1, SUM(g_loot1) AS loot1, SUM(h_stage10) AS stage10
FROM s;
-- Từ khi có auto vào game, người chưa qua màn nào có tap_to_play sec gần 0: cột tap_to_play không còn đo rớt ở nút nữa.
