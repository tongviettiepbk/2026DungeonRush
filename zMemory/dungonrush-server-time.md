---
name: dungonrush-server-time
description: "Reset key Dungeon + vé Events theo giờ server (Cloud Function getservertime, lớp ServerTime); offline dùng giờ máy; chưa deploy prod"
metadata:
  node_type: memory
  type: project
  originSessionId: 122581e4-1b74-437d-9b68-ecb90120b2bb
  modified: 2026-10-29T04:01:48.315Z
---

2026-10-29: mốc reset 0h UTC của key Dungeon và vé Events chuyển sang **giờ server** để chỉnh giờ máy không hồi sớm.
- Server: `server/functions/src/time.ts` → callable `getservertime` trả `{serverTimeMs}` (không cần uid). Test emulator OK.
- Client: `Scripts/Network/ServerTime.cs` — `SyncRoutine` gọi 1 lần ở `Login.LoadRoutine` (chờ tối đa 3s), sau đó `UtcNow` = giờ server + `realtimeSinceStartupAsDouble` trôi. Chưa sync (offline / vào thẳng MainGame trong Editor) → `DateTime.UtcNow` giờ máy.
- Chỉ `DungeonService.GetUtcNow` + `EventService.GetUtcNow` dùng; `GameUtils.GetTimeNow` (Mining, Shop, Companion, BossRushSchedule, Profile...) VẪN giờ máy.
- Vé PvP/BossRush thật vẫn do server quyết (dayKey UTC), client chỉ hiển thị.
- Đã commit 04d2f30. **Chưa deploy** lên prod (user: deploy gộp sau khi làm thêm tính năng server) (dungeon-duck-quack-slash) — thiếu hàm thì client tự lùi về giờ máy, không lỗi.

**Why:** user muốn chống chỉnh giờ máy để hồi key/vé.
**How to apply:** muốn hệ khác chống chỉnh giờ → đổi sang `ServerTime.UtcNow`, đừng dựng hàm mới. Xem [[dungonrush-dungeon-system]], [[dungonrush-events-tab]], [[dungonrush-firebase-prod-deploy]].

Đã deploy lên server thật 2026-11-04.
