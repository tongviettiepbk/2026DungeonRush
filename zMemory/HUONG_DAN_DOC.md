# zMemory — bản sao bộ nhớ Claude Code cho project DungOnRush

Thư mục này chứa bản sao bộ nhớ (auto-memory) mà Claude Code tích luỹ khi làm việc trên project DungOnRush, để đồng bộ giữa máy công ty và máy nhà qua git.

## Cấu trúc

- `MEMORY.md` — file index, mỗi dòng trỏ tới 1 file memory con.
- Mỗi file `.md` còn lại — một memory cụ thể, có frontmatter:
  ```yaml
  ---
  name: ten-slug
  description: mô tả ngắn
  metadata:
    type: user | feedback | project | reference
  ---
  ```

## Vì sao cần "nạp" chứ không đọc trực tiếp

Claude Code tự động load bộ nhớ từ một thư mục CỐ ĐỊNH gắn với path project trên từng máy, dạng:

```
~/.claude/projects/<path-project-đã-mã-hoá>/memory/
```

Tên thư mục con được suy ra từ đường dẫn đầy đủ của project trên máy đó (thay `\`, `/`, `:` bằng `-`). Vì máy nhà và máy công ty có thể có đường dẫn project khác nhau (ví dụ ổ đĩa khác), thư mục bộ nhớ gốc trên 2 máy có tên khác nhau — Claude ở nhà sẽ KHÔNG tự thấy nội dung trong `zMemory/` này, phải nạp thủ công một lần.

## Cách dùng ở nhà (hoặc bất kỳ máy mới nào)

Mở Claude Code trong project này rồi nói, ví dụ:

> "Đọc thư mục zMemory và nạp toàn bộ vào bộ nhớ (auto-memory) của bạn cho project này"

Claude sẽ:
1. Đọc từng file `.md` trong `zMemory/` (trừ file hướng dẫn này).
2. Copy/ghi từng file đó vào thư mục bộ nhớ gốc thật của máy đó (`~/.claude/projects/<...>/memory/`).
3. Copy/ghi `MEMORY.md` (index) vào cùng thư mục đó.

Sau bước này, các phiên làm việc tiếp theo trên máy đó sẽ tự động có đầy đủ context như bên máy công ty.

## Quy tắc đồng bộ 2 chiều (đã lưu trong `sync-memory-two-locations.md`)

Từ giờ mỗi khi Claude ghi/sửa một memory mới cho project này (ở bất kỳ máy nào), Claude sẽ ghi đồng thời ở CẢ:
1. Bộ nhớ gốc của máy đang chạy.
2. `zMemory/` này (để commit/push qua git, sync sang máy kia).

Ở đầu phiên làm việc tại máy khác, nếu nghi ngờ zMemory có bản mới hơn (do sửa ở máy kia và đã git pull), hãy nhắc Claude: "kiểm tra zMemory có gì mới hơn bộ nhớ gốc không, nếu có thì nạp lại".
