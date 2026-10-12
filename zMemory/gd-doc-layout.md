---
name: gd-doc-layout
description: Viết GD (bản thiết kế) phải theo đúng khuôn GDD/BOSS_RUSH_DESIGN.md — mỗi luật 1 gạch đầu dòng + 1 nhãn cuối dòng, bảng mỗi ô 1 số
metadata:
  type: feedback
---
Khi user bảo "viết GD" cho một tính năng: viết file `.md` theo ĐÚNG khuôn `GDD/BOSS_RUSH_DESIGN.md`.

- Vị trí: MỌI file GD nằm ở `GDD/` tại gốc repo (user chỉ định 2026-10-12, đã chuyển BOSS_RUSH/CLAN/SHOP/COMPANION vào đó). GD mới cũng viết vào `GDD/`, không để ở `server/` hay trong `Assets/`.
- Ảnh trong GD: dùng cú pháp markdown `![tên](img/<tính năng>/x.png)` với ảnh thu nhỏ lưu ở `GDD/img/`; KHÔNG dùng thẻ HTML `<img>` (trình xem của app in ra nguyên chữ — user báo 2026-10-12).
- Mở đầu: dòng cập nhật + bảng ký hiệu **[GỐC] / [CHỐT] / [ĐỀ XUẤT] / [CHỜ SỐ LIỆU]**.
- Mỗi luật = 1 gạch đầu dòng ngắn, kết thúc bằng ĐÚNG 1 nhãn. Luật nửa gốc nửa tự đặt → tách 2 dòng. Không chèn nhãn giữa câu, không chú thích trong ngoặc sau nhãn.
- Cả mục là đề xuất → gắn nhãn ở tiêu đề mục (`## 7. Ghép cặp **[ĐỀ XUẤT]**`).
- Bảng: mỗi ô 1 giá trị (không nhồi "600 · 600 · 150 · 150" hay "Common 1 · Uncommon 2…" vào 1 ô); bảng hẹp, tách bảng nếu cần.
- Chia mục con (4.1, 4.2…) thay cho đoạn văn dài; các bước tuần tự dùng danh sách số.
- Cuối file: "Chưa chốt" (Chờ bạn quyết định / Chờ số liệu từ game gốc) rồi "Tình trạng code" (Đã làm / Chưa làm).
- Hằng số gốc mà ý nghĩa do mình suy ra → [CHỜ SỐ LIỆU], không phải [GỐC].

**Why:** 2026-10-10 bản đầu của `GDD/CLAN_DESIGN.md` bị user chê "bố trí khó đọc, sao không như boss_rush_design" — nhãn chen giữa câu, ô bảng nhồi nhiều số, đoạn văn dài; phải viết lại toàn bộ.

**How to apply:** trước khi viết GD mới, mở `GDD/BOSS_RUSH_DESIGN.md` (hoặc `GDD/CLAN_DESIGN.md` bản đã sửa) làm mẫu rồi mới viết. Xem [[dungonrush-clan-clanwar]], [[dungonrush-boss-rush-model]].
