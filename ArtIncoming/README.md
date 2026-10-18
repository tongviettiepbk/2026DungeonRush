# ArtIncoming — nơi giao ảnh reskin

Kế hoạch đầy đủ: `GDD/RESKIN_PLAN.md`.

## Quy tắc giao file

- Cây thư mục ở đây giống hệt `2026DungeonRushUnity/Assets/_Assets/Art/`.
- Ảnh mới đặt ĐÚNG thư mục và ĐÚNG tên file của ảnh cũ cần thay (phân biệt hoa thường).
- PNG nền trong suốt.
- Giữ tỉ lệ rộng/cao như ảnh cũ. Vẽ to hơn (vd gấp đôi) thì được, game tự giữ kích thước hiển thị.
- Ảnh có viền 9-slice (cột `9_slice` = có trong bảng): giữ độ dày viền tương ứng, gửi kèm số px viền nếu thay đổi.
- Ảnh hoàn toàn mới (không thay ảnh cũ nào): đặt vào thư mục bắt đầu bằng `_` (vd `_New/`) và báo dev gán tay.

## Tài liệu tham khảo

| File | Nội dung |
|---|---|
| `_Reference/sheets/*.png` | Ảnh xem trước từng thư mục: ảnh cũ, tên file, kích thước |
| `_Reference/art_manifest.csv` | Mỗi dòng 1 ảnh: kích thước, 9-slice, đang dùng không, đã thay chưa |
| `_Reference/reskin_report.txt` | Kết quả lần áp dụng gần nhất (file lệch tỉ lệ, sai tên...) |
| `_Concept/` | Mockup + art mới chưa gắn (lobby, companion, đồ hero) |

## Dev áp ảnh vào game

Unity → `Tools/DungeonRush/Reskin/`:

1. `Kiểm tra ArtIncoming (không ghi)` — xem trước sẽ thay gì.
2. `Apply Reskin` — ghi đè ảnh cũ, giữ `.meta` (GUID), tự chỉnh Pixels Per Unit + viền 9-slice.
3. `Xuất bảng ảnh (CSV) + thư mục gương` — cập nhật `art_manifest.csv`.

Contact sheet dựng lại bằng `tools/.venv/bin/python tools/art_contact_sheets.py`.
