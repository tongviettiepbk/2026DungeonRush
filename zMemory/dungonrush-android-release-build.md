---
name: dungonrush-android-release-build
description: "2 kiểu build Android ở Assets/Editor/AndroidBuilder.cs: Product (BuildRelease APK+AAB, không cheat, mua/ads thật) và Dev (BuildDev APK, define DEV_BUILD: cheat + mua/ads luôn thành công)"
metadata:
  node_type: memory
  type: project
  originSessionId: 25230f17-6db7-4fa3-80e3-398d1070bec3
  modified: 2026-11-10T17:07:21.897Z
---

2026-11-11: build lần đầu bản lên store đạt (APK + AAB ~150 MB mỗi file, arm64 + armv7, targetSdk 36, versionName 1 / versionCode 1, ký keystore `duckDungeon.keystore` alias `duck`, chủ O=TNT, SHA-1 d50baa3b…). Mỗi lần build ~10 phút cho cả 2 file.

Lệnh (Unity phải ĐÓNG project này, chạy ở thư mục 2026DungeonRushUnity):
`DR_KEYSTORE_PASS=<pass> DR_KEYALIAS_PASS=<pass> /Volumes/Work/installed/6000.3.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod AndroidBuilder.BuildRelease -logFile <log>`
Ra `Builds/DungeonDuck_<version>_<code>.apk/.aab`.

Khi Unity ĐANG MỞ project (không chạy batch được): build ngay trong Editor bằng UnityMCP `execute_code` gọi THẲNG `BuildPipeline.BuildPlayer` (buildAppBundle=true, gán keystorePass/keyaliasPass trong code) — lệnh MCP trả lỗi rỗng nhưng build vẫn chạy, chờ dòng log tự in trong `~/Library/Logs/Unity/Editor.log` (chỉ AAB ~9 phút). ĐỪNG dùng `EditorApplication.delayCall`: Editor ở nền không tick nên không bao giờ chạy (2026-11-11). Kiểm chữ ký: keytool ở `/Volumes/Work/installed/6000.3.9f1/PlaybackEngines/AndroidPlayer/OpenJDK/bin` (máy không có java trong PATH). User đưa mật khẩu trong chat, KHÔNG lưu mật khẩu vào memory/project.

2026-11-11 tách 2 kiểu build (cờ ở `Scripts/Utilities/DevBuild.cs`):
- **Product** `AndroidBuilder.BuildRelease` (menu Tools/DungeonRush/Build/Android Product): APK+AAB, KHÔNG có DEV_BUILD → tool/cheat bị loại, IAP/MAX thật; tự chặn nếu Player Settings lỡ có define DEV_BUILD.
- **Dev** `AndroidBuilder.BuildDev` (menu .../Android Dev): 1 APK `_DEV.apk`, thêm DEV_BUILD qua `BuildPlayerOptions.extraScriptingDefines` (không sửa Player Settings). Bật: EquipmentTester "Đồ", BossRushTester "Boss" (trên máy không có botBank.json → 0 đồng đội), bảng `_Test/DevCheatPanel.cs` nút "Cheat" (gom phím L/V/O/K/F/P + Thắng/Thua vì điện thoại không có phím), log DebugCustom, Cultist mở sẵn. Mua IAP = OnPurchased ngay, ads rewarded = Finished ngay (không gọi store/MAX). Tắt Firebase Analytics (SetAnalyticsCollectionEnabled; bản Product bật lại vì cờ SDK lưu qua phiên) + AppsFlyer; Crashlytics vẫn chạy. Có mật khẩu keystore thì ký như Product, không thì ký debug.
- Quy ước: tool/cheat mới phải bọc `#if UNITY_EDITOR || DEV_BUILD` (KHÔNG chỉ UNITY_EDITOR), check runtime dùng `DevBuild.IsEnabled` thay `Application.isEditor`.
- Lưu ý: bản Dev vẫn nối server THẬT → tài khoản cheat lọt bảng xếp hạng PvP/Boss Rush/Clan.

**How to apply:** lần sau lên bản mới phải tăng versionCode (Play từ chối code trùng). Build làm bẩn `crashlytics_build_id.xml` và sinh `.utmp/` (chưa ignore).

Liên quan [[dungonrush-monetization-sdk]], [[dungonrush-equipment-tester]], [[dungonrush-boss-rush-tester]].
