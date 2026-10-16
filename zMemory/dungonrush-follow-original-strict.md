---
name: dungonrush-follow-original-strict
description: "CHỈ THỊ 2026-10-17 — làm ĐÚNG game gốc v41; chỗ nào lệch gốc thì sửa; tính năng gốc tắt thì không làm; tự chạy lần lượt các phần trong kế hoạch, không hỏi từng bước"
metadata:
  node_type: memory
  type: feedback
  originSessionId: a7015a71-d3e9-49aa-b7f2-b6ec5e0b8749
  modified: 2026-10-16T18:02:26.906Z
---

User chốt 2026-10-17: "làm theo game gốc", chỗ nào chưa đúng gốc thì sửa, rồi cứ chạy lần lượt các phần cần làm.

**Why:** user muốn bản dựng lại khớp game gốc Dungeon Rush v41, không thêm thiết kế riêng.

**How to apply:**
- Trước khi dựng 1 tính năng: reverse xem gốc có BẬT không (remote config live, GameResources, GameplayUI SetActive, hàm hiện có ai gọi). Gốc tắt/ẩn (VD nút Offline Earning lobby, QuickEarnings, Boss Gate) → KHÔNG làm, ghi lại lý do.
- Gặp chỗ project lệch gốc (cờ test, khoá level thiếu, luật tự đặt) → sửa về gốc, ghi rõ trong commit.
- Chạy tuần tự theo kế hoạch ở [[dungonrush-pending-todo]] (mục 6), xong đợt nào commit đợt đó (ghi "CHƯA test Play"), không dừng hỏi giữa chừng; chỉ hỏi khi cần quyết định không suy ra được từ gốc (giá tiền thật, số liệu chỉ server có).
- Vẫn giữ kiến trúc StickIdle [[dungonrush-follow-stickidle]].
