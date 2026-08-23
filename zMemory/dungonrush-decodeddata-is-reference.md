---
name: dungonrush-decodeddata-is-reference
description: DecodedData/ là data tham chiếu gốc bất biến — KHÔNG sửa theo refactor code (docs/tables/model)
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 9157e0cd-bf68-4ad3-8dce-080b84084998
  modified: 2026-07-30T13:45:51.544Z
---

Thư mục `DecodedData/` (docs như MAP_MODE_STRUCTURE.md, MAP_AND_SPAWN_MODEL.md, tables/, mindmap...) là **data tham chiếu gốc**, giữ nguyên để sau này tra cứu thông tin game gốc cho chính xác.

**Why:** User sẽ đổi/xoá một số class C# khi cần (VD đã xoá `MapConfig` 2026-07-30), nhưng muốn `DecodedData` bất biến làm nguồn đối chiếu — nếu sửa docs chạy theo code thì mất mốc so sánh với game gốc.

**How to apply:** Khi refactor code, KHÔNG chỉnh file trong `DecodedData/` cho "khớp" code, dù docs có nhắc tên class/hàm đã đổi. Chỉ cập nhật code + memory. Nếu docs lệch với code, để nguyên — đó là mô tả game gốc, không phải mô tả implementation hiện tại. Liên quan: [[dungonrush-map-mode-structure]], [[dungeonrush-config-format]].
