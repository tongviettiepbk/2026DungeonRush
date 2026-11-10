---
name: dungonrush-app-icon
description: Icon app Android = vịt trắng cầm búa (mẫu 4 ChatGPT), ở Art/AppIcon; adaptive chỉ dùng lớp nền; commit 30239c1c 2026-11-10
metadata:
  type: project
---

Icon app Android chốt 2026-11-10 (user chọn mẫu 4 trong 4 mẫu ChatGPT/Codex): vịt trắng mắt gắt cầm búa vàng ngọc xanh, nền dungeon tím-cam. Commit 30239c1c.

- File: `Assets/_Assets/Art/AppIcon/` — AppIcon (legacy + icon mặc định), AppIcon_Round (cắt tròn), AppIcon_Adaptive_Background (hình phủ đúng vùng nhìn thấy 72/108, rìa = bản phóng mờ cho parallax), AppIcon_Adaptive_Foreground (trong suốt).
- ProjectSettings đã gán đủ 18 ô Android + icon mặc định; đã kiểm bằng Unity batchmode (PlayerSettings.GetPlatformIcons). Chưa xem trên máy thật.
- Gốc + prompt + Play Store 512 ở `ArtArchive/AppIcon/`. iOS chưa gán icon.

- BẪY (2026-11-10): .meta tự viết thiếu trường → Unity import thành SingleChannel/Alpha8, icon hiện TRẮNG dù gán đúng. Ảnh mới thì để Unity tự sinh .meta hoặc set TextureImporter qua API (Default, RGB). Đã sửa 4 ảnh qua MCP.

**Why:** chưa tách được vịt ra lớp foreground nên dồn cả hình vào lớp nền, thu về 72/108 để launcher cắt tròn không mất đầu búa.
**How to apply:** đổi icon thì thay ảnh giữ GUID; làm iOS thì dùng AppIcon.png 1024. Xem [[dungonrush-monetization-sdk]] cho phần build Android.
