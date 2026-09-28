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
- Hàm gọi `GameData.Save()` (VD CompanionService.Summon): bật `UserData.isDisableSaveDataLocal = true` → Save no-op; tráo module (`GameData.userData.companions = new UserCompanionData(); InitData()`) rồi `finally` trả object thật + tắt cờ.
- Test trong PLAY mode có bật `isDisableSaveDataLocal`: cờ static KHÔNG tự về false khi dừng Play (Unity chỉ reload domain lúc VÀO Play) → sau `manage_editor stop` phải set tay `UserData.isDisableSaveDataLocal = false` và kiểm tra lại.
