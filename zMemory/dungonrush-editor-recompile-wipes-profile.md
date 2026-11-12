---
name: dungonrush-editor-recompile-wipes-profile
description: Recompile script lúc Editor đang Play reset GameData.userData về rỗng → CloudSave.Upload ghi đè key_user_profile (mất tên/isNameSet); đã chặn bằng UserData.IsLoaded 2026-11-12
metadata:
  node_type: memory
  type: project
  originSessionId: 1c33459c-68e4-4b02-b18b-03eb3392c411
  modified: 2026-11-12T05:50:19.150Z
---

Triệu chứng 2026-11-12: đặt nickname rồi mà lần Play sau popup Enter Nickname lại hiện, lobby tên "PlayerXXXXXX", `[NewDay] lastDayLogin=1767225600000` lúc boot.

Nguyên nhân: session khác sửa .cs trong lúc user đang Play → Unity recompile giữa Play → static `GameData.userData = new UserData()` (listData null, profile mặc định). `CloudSaveController.Update → Upload` gọi `profile.Save(true)` → ghi profile RỖNG đè key_user_profile + đẩy save rỗng lên cloud. Các module khác thoát nhờ `UserData.Save` NRE vì listData null — nên chỉ mất profile.

Sửa: `UserData.IsLoaded` (listData != null); `UserData.Save` và `CloudSaveController.Upload` bỏ qua khi chưa nạp.

**Why:** chỉ xảy ra trong Editor (máy thật không recompile giữa chừng), nhưng phá save test + cloud.
**How to apply:** đừng compile/refresh Unity khi user đang Play (xem log `Temp/__Backupscenes` / `Reloading assemblies for play mode` để biết); code mới ghi save phải tôn trọng `IsLoaded`. Fallback tên rỗng → PlayerXXXXXX trong UserData.Load là vá triệu chứng của bug này. Liên quan [[dungonrush-account-system]].
