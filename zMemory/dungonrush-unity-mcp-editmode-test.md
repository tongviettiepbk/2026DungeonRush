---
name: dungonrush-unity-mcp-editmode-test
description: Test logic qua UnityMCP execute_code ở edit mode — GameData.staticData CHƯA load (phải gọi Load), codedom C#6, đổi userData tạm thì backup/restore không Save
metadata:
  type: reference
---

Khi verify logic bằng `mcp__UnityMCP__execute_code` (không vào Play mode):
- Edit mode: `GameData.userData` có, nhưng `GameData.staticData` CHƯA nạp → gọi `GameData.staticData.Load()` trước (không thì
  GetData/companions null — các hàm có guard sẽ trả rỗng, dễ tưởng logic sai).
- Compiler mặc định codedom (C# 6): `Object` bị mơ hồ → viết `UnityEngine.Object.DestroyImmediate`.
- Muốn thử dữ liệu save (VD owned pet) → backup list + `isDataChanged`, gán tạm, `finally` restore, KHÔNG gọi GameData.Save.
- Hàm private: gọi qua reflection (BindingFlags.NonPublic) trên component AddComponent tạm rồi DestroyImmediate.
- Console còn 3 lỗi Inspector editor (MissingReference m_Targets / SerializedObjectNotCreatable) — vô hại, không phải lỗi code.
Xem [[dungonrush-rebuild-progress]].
- **Cầu nối UnityMCP rớt (instances = 0, 'no_unity_session') dù Unity đang mở (Mac, 2026-10-12):** server python cổng 8080 vẫn chạy nhưng plugin trong Unity đã ngắt, biên dịch lại cũng không tự nối. Cách nối lại không cần user bấm: viết tạm `Assets/Editor/TmpMcpResume.cs` (`[InitializeOnLoad]`, `EditorApplication.delayCall` → reflection `MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor` → `Bridge.StartAsync()` nếu `IsRunning` false), đưa Unity lên trước để nó import (`osascript -e 'tell application "Finder" to activate' -e 'delay 1' -e 'tell application "System Events" to set frontmost of (first process whose unix id is <pid Unity>) to true'`), chờ `lsof -nP -iTCP:8080` thấy dòng Unity, rồi XOÁ file tạm + .meta và refresh. Unity không ở trước thì không tự import file mới (không có .meta = chưa import).
- **Compile-check ngoài Unity trên Mac:** csc ở `/Volumes/Work/installed/6000.3.9f1/Unity.app/Contents/Resources/Scripting/DotNetSdkRoslyn/csc.dll`, chạy bằng `/usr/local/share/dotnet/dotnet`; gom `<Compile>` + `<HintPath>` từ Assembly-CSharp(.Editor).csproj + quét thêm *.cs mới (csproj chưa có file mới) như `tools/il2cpp_reverse/compile_check.py`.
- Hàm gọi `GameData.Save()` (VD CompanionService.Summon): bật `UserData.isDisableSaveDataLocal = true` → Save no-op; tráo module (`GameData.userData.companions = new UserCompanionData(); InitData()`) rồi `finally` trả object thật + tắt cờ.
- Test trong PLAY mode có bật `isDisableSaveDataLocal`: cờ static KHÔNG tự về false khi dừng Play (Unity chỉ reload domain lúc VÀO Play) → sau `manage_editor stop` phải set tay `UserData.isDisableSaveDataLocal = false` và kiểm tra lại.
- Test PLAY qua MCP (2026-10-10): scene Root → Login dừng ở nút Tap To Play → gọi `FindObjectOfType<Login>().btTapToPlay.onClick.Invoke()`, chờ ~9s rồi `uiLobby.OpenTab(...)`. Ảnh inline của `manage_camera screenshot` bị PHỦ TRẮNG MỜ khi có Image bán trong suốt (lỗi của ảnh xem trước) → Read file PNG trên đĩa (output_folder Temp/...) mới đúng màu. userData.dungeons ở edit mode rỗng → tráo `new UserDungeonData()` + InitData/ValidateData trước khi gọi DungeonService.

- **Session KHÔNG có tool UnityMCP nhưng server vẫn chạy (2026-10-16):** gọi thẳng HTTP JSON-RPC `POST http://127.0.0.1:8080/mcp` (header `Content-Type: application/json`, `Accept: application/json, text/event-stream`; initialize → lấy `mcp-session-id` → notifications/initialized → tools/call / resources/read; trả về dạng SSE `data:`). Dùng được refresh_unity, read_console, execute_code (compiler codedom, Roslyn không có). Unity ở nền thì compile treo → `osascript ... set frontmost of (first process whose unix id is <pid>)`.
