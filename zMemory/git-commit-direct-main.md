---
name: git-commit-direct-main
description: Project DungeonRush commit thẳng vào main, không tách nhánh riêng rồi merge
metadata:
  type: feedback
---

Commit thẳng vào `main`, không tạo nhánh riêng cho từng việc. Vẫn chỉ commit khi user bảo, và không tự push.

**Why:** 2026-11-03 sau 2 lần tôi tách nhánh rồi phải merge lại, user bảo xoá nhánh "đỡ merge"; toàn bộ lịch sử repo cũng commit thẳng main.
**How to apply:** khi user nói "commit đi" ở main thì commit luôn tại main. Chỉ tách nhánh khi user yêu cầu. Liên quan [[sync-memory-two-locations]].
