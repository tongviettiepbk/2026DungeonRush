-- Khối đầu chung của mọi query (run_all.py tự ghép; dán tay lên console thì dán khối này trước).
-- Dataset GA4 export của Dungeon Duck, region US (đổi analytics_XXXXXXXXX sau khi bật export). `ev` lấy bảng ngày events_YYYYMMDD khi đã có,
-- không thì lấy bảng intraday cùng ngày, để không đếm trùng. `uid` = user_id (uid Firebase Auth) khi lần cài đó
-- từng có, gán cho CẢ event trước lúc đăng nhập (first_open không mang user_id) để một người không tách làm hai.
-- Chỉ đọc 30 ngày gần nhất để mỗi lần chạy không quét lại toàn bộ lịch sử; cần xa hơn thì `run_all.py --days N`.
CREATE TEMP FUNCTION P_STR(params ANY TYPE, k STRING) AS ((SELECT value.string_value FROM UNNEST(params) WHERE key = k));
CREATE TEMP FUNCTION P_INT(params ANY TYPE, k STRING) AS ((SELECT COALESCE(value.int_value, CAST(value.double_value AS INT64)) FROM UNNEST(params) WHERE key = k));
CREATE TEMP FUNCTION P_NUM(params ANY TYPE, k STRING) AS ((SELECT COALESCE(value.double_value, value.float_value, CAST(value.int_value AS FLOAT64)) FROM UNNEST(params) WHERE key = k));
CREATE TEMP FUNCTION U_STR(props ANY TYPE, k STRING) AS ((SELECT value.string_value FROM UNNEST(props) WHERE key = k));
CREATE TEMP TABLE ev AS
SELECT *, COALESCE(MAX(user_id) OVER (PARTITION BY user_pseudo_id), user_pseudo_id) AS uid, PARSE_DATE('%Y%m%d', event_date) AS d
FROM `dungeon-duck-quack-slash.analytics_XXXXXXXXX.events_*`
WHERE (_TABLE_SUFFIX BETWEEN FORMAT_DATE('%Y%m%d', DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY)) AND '99999999'
    OR _TABLE_SUFFIX >= CONCAT('intraday_', FORMAT_DATE('%Y%m%d', DATE_SUB(CURRENT_DATE(), INTERVAL 30 DAY))))
  AND (_TABLE_SUFFIX NOT LIKE 'intraday_%'
   OR REGEXP_EXTRACT(_TABLE_SUFFIX, r'(\d{8})$') NOT IN (
        SELECT REGEXP_EXTRACT(table_name, r'(\d{8})$')
        FROM `dungeon-duck-quack-slash.analytics_XXXXXXXXX.INFORMATION_SCHEMA.TABLES`
        WHERE REGEXP_CONTAINS(table_name, r'^events_\d{8}$')));
