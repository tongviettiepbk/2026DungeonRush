---
name: dungonrush-follow-stickidle
description: Chỉ thị dự án — phát triển DungeonRush bám theo cấu trúc project 2026StickIdle
metadata: 
  node_type: memory
  type: project
  originSessionId: 2353e537-b729-4ea6-a1ea-1d48264d4ff5
  modified: 2026-07-24T06:58:32.639Z
---

Quy tắc cố định cho việc phát triển game **DungeonRush**: **bám sát kiến trúc của project 2026StickIdle** ([[stickidle-architecture]]) làm khuôn mẫu cho toàn bộ sản phẩm.

**Why:** User đã có StickIdle là idle RPG hoàn chỉnh, muốn tái sử dụng khuôn kiến trúc đã kiểm chứng thay vì thiết kế lại từ đầu.

**How to apply:**
- Phát triển theo 6 phần: (1) Scene flow Root→Login→Lobby, (2) Data GameData/StaticGameData/UserData, (3) GameConfig+feature flags, (4) Combat Units/BattleMechanic, (5) UI UIManager/BaseUI, (6) Patterns Singleton/Observer/Pooling.
- Làm **từng phần, từng bước**, mỗi lần chỉ bóc lấy **core base** (chưa cần đúng hoàn toàn data thật của DungeonRush).
- Giữ đúng convention StickIdle: feature-folder, tách static/user data, prefix UI (`UI*`/`Popup*`/`Cell*`/`L*`...), mỗi module save kế thừa `BaseUserData`.
- Tiến độ hiện tại xem [[dungonrush-rebuild-progress]].
