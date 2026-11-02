# Chat — Bản thiết kế (GD)

Cập nhật: 2026-11-02. Client + server đã code theo bản này, test emulator + test Play trong Editor đạt (mục 10).
Phần client reverse từ libil2cpp v41 (ChatController, ClanChatController, ChatTabPage, ChatButtonUI, ChatMessageItem,
ReportPopup, PlayerPopup, class lọc từ tục `rp`). Server gốc không nằm trong APK → các luật phía server là **[ĐỀ XUẤT]**.

Ký hiệu độ chắc chắn của từng mục:

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Xác nhận từ game gốc (đọc từ APK) |
| **[CHỐT]** | Đã thống nhất trong thảo luận |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ SỐ LIỆU]** | Cần ghi thêm từ game gốc mới quyết định được |

---

## 1. Tổng quan

Chat giữa người chơi gồm 2 kênh: **Thế giới** (mọi người) và **Clan** (thành viên cùng clan). Mở từ nút chat ở đáy
lobby. Bấm avatar 1 tin → xem hồ sơ người gửi, có nút Chặn và Báo cáo.

- Có 2 kênh World / Clan. **[GỐC]**
- Mở khoá ở PlayerLevel 6. **[GỐC]**
- Nút chat lobby xem trước 2 tin Thế giới mới nhất. **[GỐC]**
- Nút Discord của gốc mở cộng đồng game gốc → project ẩn nút. **[CHỐT]**

## 2. Đường truyền tin

- Tin lưu ở Firebase Realtime Database, nhánh `chat/ww` (Thế giới). **[GỐC]**
- Tin clan ở nhánh `chat/clans/{clanId}`. **[GỐC]**
- Client chỉ đọc 40 tin cuối, sắp theo trường `t`. **[GỐC]**
- Client chỉ đọc RTDB, mọi tin gửi qua Cloud Function. **[GỐC]**
- Client nghe RTDB qua REST streaming, không dùng Firebase Database SDK. **[CHỐT]**
- Mất kết nối thì tự nối lại sau 3 giây. **[ĐỀ XUẤT]**
- App vào nền thì ngắt nghe, quay lại thì nối lại. **[GỐC]**

### 2.1 Một tin nhắn

| Khoá | Ý nghĩa |
|---|---|
| `u` | uid người gửi |
| `a` | avatarId |
| `n` | tên |
| `m` | nội dung |
| `t` | giờ gửi (unix giây) |
| `tp` | loại tin (chỉ `itemDrop`) |

### 2.2 Endpoint

| Endpoint | Việc |
|---|---|
| `sendchatmessage` | gửi tin Thế giới |
| `sendclanchatmessage` | gửi tin Clan |
| `getchatplayerprofile` | hồ sơ khi bấm avatar |
| `updatechatplayerprofile` | cập nhật tên / avatar |
| `reportchatuser` | báo cáo người chơi |

- 5 endpoint trên trùng tên endpoint gốc. **[GỐC]**
- `getchatmessages` và `triggerchatreport` có tên trong APK nhưng client gốc không gọi → không làm. **[GỐC]**
- `clearchatban` (gỡ cấm) chỉ admin / emulator gọi được. **[ĐỀ XUẤT]**

## 3. Gửi tin

- Tin bị cắt còn 200 ký tự. **[GỐC]**
- Client lọc từ tục trước khi gửi. **[GỐC]**
- Server lọc lại bằng cùng danh sách. **[ĐỀ XUẤT]**
- 3 tin đầu gửi liền; từ tin thứ 4, mỗi tin phải cách tin trước 3 giây. **[GỐC]**
- Bộ đếm 3 tin tính riêng mỗi kênh, không reset trong phiên chơi. **[GỐC]**
- Đang chờ: ô nhập ghi "Too many messages (Ns)", nút gửi tắt. **[GỐC]**
- Tin mình gửi hiện mờ (alpha 0.5) cho tới khi server xác nhận. **[GỐC]**
- Gửi lỗi → xoá tin mờ, báo "Too many messages". **[GỐC]**
- Mỗi tin gửi kèm snapshot đồ (để người khác xem hồ sơ). **[GỐC]**
- Snapshot dùng chung khuôn với Boss Rush (đồ, pet, relic, power, avatar). **[CHỐT]**
- Chưa đặt tên mà bấm nút chat → mở popup đặt tên trước, có tên mới vào chat. **[GỐC]**
- Kênh Clan lấy clanId từ server, không tin clanId client gửi. **[ĐỀ XUẤT]**

### 3.1 Giới hạn phía server **[ĐỀ XUẤT]**

| Luật | Giá trị |
|---|---|
| Tin tối đa mỗi 60 giây | 10 |
| Tin giữ lại trong RTDB | 3 ngày |

## 4. Lọc từ tục

- Danh sách 112 từ tiếng Anh, chép nguyên từ class `rp` của gốc. **[GỐC]**
- Khớp nguyên từ (`\b...\b`), không phân biệt hoa thường. **[GỐC]**
- Mỗi chữ i/o/t/a/e/l/s khớp cả ký tự lách (1 ! | 0 7 @ 4 3 $ 5). **[GỐC]**
- Từ khớp thay bằng `***`. **[GỐC]**

## 5. Trang chat

- Thanh chọn kênh World | Clan, tiêu đề đổi theo kênh. **[GỐC]**
- Chưa có clan mà chọn Clan → báo "Join a clan to use clan chat", giữ kênh cũ. **[GỐC]**
- Mỗi dòng: avatar, tên, nội dung, giờ HH:mm theo giờ máy. **[GỐC]**
- Đang ở cuối danh sách thì tự cuộn theo tin mới. **[GỐC]**
- Đang tải → vòng xoay; tải lỗi → "Couldn't load chat". **[GỐC]**
- Bàn phím điện thoại bật → đẩy khung chat lên theo chiều cao bàn phím. **[GỐC]**
- Tin của người bị chặn không hiện (cả trang chat lẫn dòng xem trước). **[GỐC]**

## 6. Hồ sơ, chặn, báo cáo

- Bấm avatar → popup hồ sơ (dùng chung popup với Boss Rush). **[CHỐT]**
- Xem người khác: hiện nút Block/Unblock và Report. **[GỐC]**
- Xem chính mình: ẩn 2 nút. **[ĐỀ XUẤT]**
- Danh sách chặn chỉ lưu trên máy, không gửi server. **[GỐC]**
- Báo cáo có 7 lý do, tick được nhiều: Harassment, Spam, Inappropriate Language, Offensive Player Name, Cheating, Hate Speech, Other. **[GỐC]**
- Không tick lý do nào → "Please select at least one reason". **[GỐC]**

## 7. Cấm chat

- Server trả `bannedUntil` (unix giây) + `banTier` khi gửi tin. **[GỐC]**
- `bannedUntil` từ 32503680000 (năm 3000) = cấm vĩnh viễn. **[GỐC]**
- Đang bị cấm mà bấm nút chat → báo thời hạn, không mở chat. **[GỐC]**
- Thông báo làm tròn lên theo tuần / ngày / giờ / phút (chữ tiếng Anh cứng như gốc). **[GỐC]**

### 7.1 Luật tự cấm **[ĐỀ XUẤT]**

- Đủ 3 người khác nhau báo cáo trong 24 giờ → bị cấm.
- 1 người báo cáo nhiều lần chỉ tính 1.
- Báo cáo trước lần cấm gần nhất không tính cho lần sau.

| Lần bị cấm | Thời hạn |
|---|---|
| 1 | 1 giờ |
| 2 | 24 giờ |
| 3 | 7 ngày |
| 4 trở đi | vĩnh viễn |

## 8. Tin "rơi đồ"

- Gốc có code gửi "@tên dropped <Mythic/Artifact> - <món>" 1 lần mỗi độ hiếm mỗi phiên. **[GỐC]**
- Hàm đó không có ai gọi trong v41 (tính năng tắt) → không làm. **[GỐC]**

## 9. Chưa chốt

### Chờ bạn quyết định

1. Số giới hạn server (10 tin/phút, giữ 3 ngày) — mục 3.1.
2. Luật tự cấm 3 báo cáo / 24h và thang 1h → 24h → 7 ngày → vĩnh viễn — mục 7.1.
3. Kênh Clan: RTDB đang cho mọi người đã đăng nhập đọc `chat/clans/*` (ai biết clanId là đọc được). Muốn chặn hẳn
   phải ghi danh sách thành viên sang RTDB mỗi lần vào/rời clan.
4. Có làm link Discord / cộng đồng riêng của game mình không (nút gốc đang ẩn).

### Việc phải làm trên server thật

1. Firebase console → Realtime Database → Create Database (us-central1).
2. `firebase deploy --only functions,firestore,database` (thêm rules RTDB + index `chatReports`).

## 10. Tình trạng code

### Đã làm

- Server `server/functions/src/chat.ts`, rules `server/database.rules.json`, emulator database cổng 9000.
- Test `npm run test:emulator:chat` (gửi, đọc RTDB qua REST, lọc, giới hạn, clan, hồ sơ, báo cáo → cấm, gỡ cấm): PASS.
- Client `Scripts/Chat/` (ChatController, RealtimeDbStream, ProfanityFilter, UserChatData) + `Scripts/UI/Chat/`.
- Prefab: `UINew/Chat/ChatMessageItemUI`, `Resources/Prefabs/UI/UIChatTabPage`, `UIReportPopup`; nút chat lobby
  trong `UIMainGame2`; nút Block/Report trong `UIPlayerPopup`. Builder: `Tools/DungeonRush/Build Chat UI`.
- Test Play Editor + emulator: gửi/nhận tức thời 2 người, lọc từ tục, chờ 3s sau 3 tin, hồ sơ, chặn/bỏ chặn, báo cáo.

### Chưa làm

- Reskin trang chat + popup báo cáo theo bộ art mới (đang là art rip gốc).
- Chưa test trên điện thoại thật (đẩy khung theo bàn phím).
- Chưa deploy server thật.
