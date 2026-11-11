---
name: dungonrush-android-release-build
description: "Build APK + AAB phát hành bằng Assets/Editor/AndroidBuilder.cs (batch, ký duckDungeon.keystore alias duck, mật khẩu qua biến môi trường); tool test tự loại vì #if UNITY_EDITOR"
metadata:
  node_type: memory
  type: project
  originSessionId: 25230f17-6db7-4fa3-80e3-398d1070bec3
  modified: 2026-11-10T17:07:21.897Z
---

2026-11-11: build lần đầu bản lên store đạt (APK + AAB ~150 MB mỗi file, arm64 + armv7, targetSdk 36, versionName 1 / versionCode 1, ký keystore `duckDungeon.keystore` alias `duck`, chủ O=TNT, SHA-1 d50baa3b…). Mỗi lần build ~10 phút cho cả 2 file.

Lệnh (Unity phải ĐÓNG project này, chạy ở thư mục 2026DungeonRushUnity):
`DR_KEYSTORE_PASS=<pass> DR_KEYALIAS_PASS=<pass> /Volumes/Work/installed/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod AndroidBuilder.BuildRelease -logFile <log>`
Ra `Builds/DungeonDuck_<version>_<code>.apk/.aab`. User đưa mật khẩu trong chat, KHÔNG lưu mật khẩu vào memory/project.

Tool dev đã kiểm: EquipmentTester ("Đồ"), BossRushTester ("Boss"), phím cheat UIMainLobby L/V/O/K/F/P, BaseMode W/L, Forge mũi tên đều nằm trong `#if UNITY_EDITOR` → không có trong bản build, không cần tắt tay.

**How to apply:** lần sau lên bản mới phải tăng versionCode (Play từ chối code trùng). Build làm bẩn `crashlytics_build_id.xml` và sinh `.utmp/` (chưa ignore).

Liên quan [[dungonrush-monetization-sdk]], [[dungonrush-equipment-tester]], [[dungonrush-boss-rush-tester]].
