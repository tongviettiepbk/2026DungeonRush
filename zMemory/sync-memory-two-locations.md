---
name: sync-memory-two-locations
description: Mọi memory ghi/cập nhật cho project này phải đồng bộ ra cả memory gốc VÀ E:\00Work\00Project\2026DungOnRush\zMemory\
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 0e211f56-ee06-4373-a11a-8dcaaa638111
  modified: 2026-08-23T10:42:59.701Z
---

Mỗi khi ghi mới hoặc cập nhật một file memory nào cho project DungOnRush, phải ghi đồng thời ở CẢ HAI nơi (nội dung giống hệt nhau):
1. `C:\Users\admin\.claude\projects\E--00Work-00Project-2026DungOnRush\memory\` (bộ nhớ gốc, tự động load vào context)
2. `E:\00Work\00Project\2026DungOnRush\zMemory\` (nằm trong project, user đồng bộ qua git/cloud giữa nhà và công ty)

Bao gồm cả `MEMORY.md` (file index) — cập nhật dòng index ở cả hai nơi khi thêm/sửa/xoá một memory.

**Why:** User làm việc ở 2 địa điểm (nhà và công ty), máy khác nhau nên bộ nhớ local ở `~/.claude/projects/...` không tự đồng bộ giữa 2 máy. Đặt bản sao trong `zMemory/` (trong repo project) để user có thể đồng bộ thủ công/qua git giữa hai nơi, đảm bảo công việc trơn tru dù làm ở máy nào.

**How to apply:** Bất cứ lúc nào Write hoặc Edit một file trong memory folder gốc cho project này, lặp lại thao tác y hệt (path đổi thành zMemory) ngay sau đó. Nếu chỉ có thời gian ghi 1 nơi vì lý do nào đó, ưu tiên ghi memory gốc trước (để không mất context ngay), nhưng phải quay lại đồng bộ zMemory trong cùng lượt trả lời.

## Quy trình đầu phiên (session-start check)

Vì user đổi qua lại giữa 2 máy (nhà/công ty), đầu mỗi phiên làm việc mới trên project này, chủ động kiểm tra lệch giữa 2 nơi trước khi tin tưởng nội dung bộ nhớ gốc:

1. So sánh danh sách file `.md` trong `zMemory/` (repo, path project hiện tại + `\zMemory\`) với bộ nhớ gốc trên máy đang chạy.
2. Nếu file chỉ có ở một bên → copy sang bên còn thiếu.
3. Nếu file trùng tên nhưng nội dung khác nhau → so `modified:` trong frontmatter (hoặc git log của zMemory nếu cần) để biết bản nào mới hơn, rồi lấy bản mới hơn ghi đè bản cũ ở CẢ HAI nơi.
4. Nếu phát hiện lệch và đã đồng bộ, báo ngắn gọn cho user biết đã nạp gì mới.
5. Không tự ý `git add/commit/push` thư mục zMemory — chỉ nhắc user commit/push khi có thay đổi, vì đó là hành động cần xác nhận (đẩy lên remote).

Không cần làm bước này nếu phiên làm việc chỉ hỏi đáp ngắn, không đụng tới code/memory — chỉ áp dụng khi bắt đầu một phiên làm việc thực sự (task đầu tiên có ý nghĩa trong phiên).

**Tần suất:** Chỉ chạy check này TỐI ĐA 1 LẦN MỖI NGÀY (theo ngày dương lịch hiện tại), hoặc khi user chủ động yêu cầu (vd: "check/nạp lại bộ nhớ"). Nếu trong ngày đã check rồi (dù ở phiên trước đó), không cần check lại ở các phiên sau trong cùng ngày trừ khi user yêu cầu.
