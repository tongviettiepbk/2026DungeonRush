# Clan + Clan War — Bản thiết kế (GD)

Cập nhật: 2026-10-10. Server và client đã code theo bản này (mục 15 ghi phần còn lại). Các mục **[ĐỀ XUẤT]** đang
chạy đúng như đề xuất và nằm trong config, đổi được khi bạn quyết khác.
Số liệu reverse từ game gốc nằm ở `DecodedData/CLAN_MODEL.md`; file này là thiết kế của project mình.

Ký hiệu độ chắc chắn của từng mục:

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Xác nhận từ game gốc (đọc từ APK) |
| **[CHỐT]** | Đã thống nhất trong thảo luận |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt (game gốc giữ phần này ở server, APK không có) |
| **[CHỜ SỐ LIỆU]** | Cần ghi thêm từ game gốc mới quyết định được |

---

## 1. Tổng quan

Clan là nhóm tối đa 50 người chơi. Mỗi tuần clan đấu **Clan War** với một clan khác: 5 ngày thi điểm đóng góp,
1 ngày PvP, 1 ngày nghỉ nhận thưởng. Cả clan nhận thưởng theo thắng/thua, mỗi người nhận thêm theo điểm cá nhân.

- Mở khoá ở PlayerLevel 15. **[GỐC]**
- Vào từ tab Clan ở thanh dưới lobby.
- Chưa có clan → màn tìm clan. Có clan → 2 tab **Members** và **Battle** (Clan War). **[GỐC]**

## 2. Tạo clan

- Phí tạo: **100 gem**. **[GỐC]**
- Tên: **3–15 ký tự**, chỉ chữ cái và số, không chứa từ bậy. **[GỐC]**
- Tên không trùng clan khác, không phân biệt hoa/thường. **[GỐC]**
- Tên không đổi được sau khi tạo. **[GỐC]**
- Kiểu gia nhập: **Public** (vào ngay) hoặc **Approval Only** (chờ duyệt). Mặc định Public. **[GỐC]**
- Người tạo là Leader. Clan mới ở Tier D.
- Mỗi người chỉ ở một clan. **[GỐC]**

### Banner

Chọn 4 thứ, đổi lại được trong cài đặt clan: **[GỐC]**

| Thành phần | Số lựa chọn |
|---|---|
| Hình nền | 8 |
| Màu nền | 10 |
| Biểu tượng | 20 |
| Màu biểu tượng | 8 |

## 3. Vai trò và quyền

- Ba vai trò: **Leader** (1 người), **Captain**, **Member**. **[GỐC]**
- Tối đa **3 Captain** mỗi clan. **[CHỜ SỐ LIỆU]**

| Việc | Member | Captain | Leader |
|---|---|---|---|
| Đóng góp Clan War, rời clan | ✔ | ✔ | ✔ |
| Sửa announcement | | ✔ | ✔ |
| Duyệt / từ chối đơn xin vào | | ✔ | ✔ |
| Kick Member | | ✔ | ✔ |
| Kick Captain | | | ✔ |
| Thăng Member ↔ giáng Captain | | | ✔ |
| Sửa mô tả, kiểu gia nhập, banner | | | ✔ |

Bảng trên: **[GỐC]**

- Announcement tối đa 300 ký tự. **[GỐC]**
- Mô tả clan tối đa 200 ký tự. **[GỐC]**

### Khi Leader rời clan **[ĐỀ XUẤT]**

1. Có Captain → Captain có Power cao nhất lên Leader.
2. Không có Captain → Member có Power cao nhất lên Leader.
3. Không còn ai → clan giải tán, tên clan được dùng lại.

Game gốc không có nút chuyển Leader thủ công; bản này cũng chưa có.

## 4. Tìm và gia nhập clan

### 4.1 Tìm clan

- Danh sách tối đa **20 clan** cùng server. **[CHỜ SỐ LIỆU]**
- Không gõ tên: xếp theo tổng Power giảm dần. **[ĐỀ XUẤT]**
- Gõ tên: tìm theo phần đầu tên. **[ĐỀ XUẤT]**
- Tìm nâng cao: số thành viên tối thiểu / tối đa (0–50), ẩn clan Approval Only. **[GỐC]**

### 4.2 Gia nhập

- Clan Public: bấm **Join** là vào ngay. **[GỐC]**
- Clan Approval Only: bấm **Request**, chờ Captain hoặc Leader duyệt. **[GỐC]**
- Số đơn xin vào mỗi ngày có giới hạn, tính lại lúc 0h UTC. **[GỐC]**
- Giới hạn đang đặt là **5 đơn mỗi ngày**. **[CHỜ SỐ LIỆU]**
- Được nhận vào một clan thì đơn đang chờ ở clan khác tự huỷ. **[ĐỀ XUẤT]**
- Clan đủ **50 người** thì không nhận thêm. **[GỐC]**

### 4.3 Thời gian chờ

- Rời clan xong phải **chờ 24 giờ** mới vào hoặc tạo clan khác. **[GỐC]**
- Bị kick cũng phải chờ 24 giờ. **[ĐỀ XUẤT]**

## 5. Tier của clan

- 5 tier: **D → C → B → A → S**. **[GỐC]**
- Tier quyết định mức thưởng Clan War (mục 11.2). **[GỐC]**
- Mỗi tuần clan được cộng **điểm tier = War Score** giành được tuần đó (tối đa 7). **[ĐỀ XUẤT]**
- Thua vẫn được cộng phần War Score của mình. **[ĐỀ XUẤT]**
- Điểm tier chỉ tăng, không giảm. **[ĐỀ XUẤT]**

| Tier | Điểm tier cần | Số tuần nhanh nhất |
|---|---|---|
| D | 0 | — |
| C | 7 | 1 |
| B | 18 | 3 |
| A | 35 | 5 |
| S | 60 | 9 |

Bảng trên: **[ĐỀ XUẤT]**

## 6. Lịch tuần Clan War

- Một tuần war bắt đầu **Thứ Ba 0h UTC**. **[GỐC]**
- Mỗi ngày đổi lúc 0h UTC. **[GỐC]**

| Ngày war | Thứ | Nội dung |
|---|---|---|
| Day 1 | Thứ Ba | Thi điểm — nhóm A |
| Day 2 | Thứ Tư | Thi điểm — nhóm B |
| Day 3 | Thứ Năm | Thi điểm — nhóm A |
| Day 4 | Thứ Sáu | Thi điểm — nhóm B |
| Day 5 | Thứ Bảy | Thi điểm — nhóm A |
| Day 6 | Chủ Nhật | PvP với clan địch |
| Day 7 | Thứ Hai | Nghỉ, nhận thưởng |

Bảng trên: **[GỐC]**

- **Nhóm A**: nhặt đồ, triệu hồi pet, lên level. **[GỐC]**
- **Nhóm B**: đào mỏ, dùng key dungeon, triệu hồi áo choàng. **[GỐC]**

## 7. Ghép cặp **[ĐỀ XUẤT]**

Chạy tự động Thứ Ba 00:05 UTC.

1. Chốt war tuần trước: cộng điểm tier, ghi clan Champion nếu có trận Championship.
2. Ghép trận Championship nếu đủ điều kiện (mục 12).
3. Xếp các clan còn lại theo điểm tier giảm dần; cùng điểm thì theo tổng Power.
4. Ghép lần lượt từng đôi: hạng 1 gặp hạng 2, hạng 3 gặp hạng 4…
5. Clan lẻ cuối cùng gặp một clan bot (mục 13).

- Clan có ít nhất 1 thành viên là được ghép.
- Clan tạo sau giờ ghép phải chờ tuần sau; tab Battle hiện "Waiting For War" và đếm ngược. **[GỐC]**

## 8. Điểm đóng góp (Day 1–5)

- Làm đúng hoạt động của ngày thì được điểm đóng góp. **[GỐC]**
- Hoạt động không thuộc ngày hôm đó thì không có điểm. **[GỐC]**
- Điểm tính cho cá nhân, đồng thời cộng vào tổng điểm ngày của clan. **[GỐC]**

### 8.1 Điểm theo độ hiếm **[ĐỀ XUẤT]**

| Độ hiếm | Nhặt 1 món đồ | Triệu hồi 1 pet | Triệu hồi 1 áo choàng |
|---|---|---|---|
| Common | 1 | 10 | 10 |
| Uncommon | 2 | 20 | 20 |
| Rare | 5 | 50 | 50 |
| Epic | 10 | 100 | 100 |
| Legendary | 25 | 250 | 250 |
| Mythic | 50 | 500 | 500 |
| Artifact | 100 | — | — |
| Ancient | 200 | — | — |
| Immortal | 400 | — | — |
| Divine | 800 | — | — |

### 8.2 Điểm hoạt động khác

| Hoạt động | Điểm | Độ chắc |
|---|---|---|
| Lên level | level mới × 100 | **[GỐC]** |
| Dùng 1 key dungeon | 500 | **[GỐC]** |

### 8.3 Điểm đào mỏ **[ĐỀ XUẤT]**

| Quặng | Điểm |
|---|---|
| Stone | 1 |
| Coal | 2 |
| Iron | 4 |
| Ruby | 8 |
| Emerald | 15 |
| Gold | 30 |
| Diamond | 60 |

Project chưa có tính năng đào mỏ, nên Day 2 và Day 4 hiện chỉ kiếm điểm được bằng key dungeon và áo choàng.

### 8.4 Quy tắc ghi điểm

- Client gom hoạt động rồi gửi lên server sau 8 giây; mất mạng thì giữ lại gửi sau. **[GỐC]**
- Server chỉ nhận hoạt động diễn ra trong ngày hiện tại, bỏ hoạt động trùng. **[ĐỀ XUẤT]**
- Vào clan giữa tuần thì được đóng góp ngay cho war của clan mới. **[ĐỀ XUẤT]**
- Rời clan thì điểm đã góp vẫn ở lại trong tổng của clan cũ. **[ĐỀ XUẤT]**

## 9. Thắng ngày và thắng tuần

- Hết ngày, clan có tổng điểm ngày cao hơn **thắng ngày** và nhận War Score của ngày đó. **[GỐC]**
- Hoà thì không clan nào nhận. **[ĐỀ XUẤT]**
- **MVP ngày** của mỗi clan là người đóng góp nhiều điểm nhất ngày đó. **[GỐC]**
- Hết Day 6, clan có tổng War Score cao hơn **thắng tuần**. **[GỐC]**
- War Score bằng nhau thì clan có tổng điểm đóng góp cả tuần cao hơn thắng. **[ĐỀ XUẤT]**

| Ngày | War Score |
|---|---|
| Day 1–5 | 1 mỗi ngày |
| Day 6 | 2 |
| Cả tuần | 7 |

Bảng trên: **[ĐỀ XUẤT]**

## 10. Day 6 — PvP

### 10.1 Trận đánh

- Đánh với thành viên clan địch do máy điều khiển, mặc bộ đồ và mang pet đã lưu của người thật. **[GỐC]**
- Trận 30 giây, luật giống PvP Arena. **[GỐC]**
- Bộ đồ đối thủ lấy từ lần gần nhất họ mở PvP Arena.
- Người chưa từng mở PvP Arena ra trận **không có đồ**, chỉ có tên và Power (xem mục 14).

### 10.2 Vé và điểm

- Mỗi người có **5 vé**. **[ĐỀ XUẤT]**
- Bắt đầu trận là mất 1 vé, thắng hay thua cũng vậy. **[ĐỀ XUẤT]**
- Thắng được **1.000 điểm đóng góp**. **[ĐỀ XUẤT]**
- Mỗi đối thủ chỉ tính điểm một lần cho cả clan; người đã bị đồng đội hạ thì không đánh lại được. **[GỐC]**

### 10.3 Reset vòng

- Clan hạ hết toàn bộ đối thủ thì danh sách mở lại và mọi người được hồi vé. **[GỐC]**
- Chỉ reset khi **cả hai clan có từ 30 thành viên** lúc bắt đầu war. **[GỐC]**
- Tối đa **2 lần reset** mỗi war. **[ĐỀ XUẤT]**

## 11. Thưởng

### 11.1 Mốc cá nhân — theo điểm đóng góp cả tuần **[ĐỀ XUẤT]**

| Mốc điểm | Lootbox | Bone | Exp | Cloak |
|---|---|---|---|---|
| 1.000 | 20 | 20 | 50 | — |
| 3.000 | 40 | 40 | 100 | 5 |
| 6.000 | 60 | 60 | 200 | 10 |
| 10.000 | 80 | 80 | 300 | 15 |
| 15.000 | 100 | 100 | 500 | 20 |
| 25.000 | 150 | 150 | 800 | 30 |
| 40.000 | 200 | 200 | 1.200 | 40 |
| 60.000 | 300 | 300 | 2.000 | 60 |

- Đạt mốc là bấm nhận được ngay. **[GỐC]**
- Sang Day 7 có nút gộp nhận hết các mốc còn lại. **[GỐC]**

### 11.2 Thưởng clan — theo tier và thắng/thua **[ĐỀ XUẤT]**

| Tier | Kết quả | Lootbox | Bone | Cloak | Vial |
|---|---|---|---|---|---|
| S | Thắng | 600 | 600 | 150 | 150 |
| S | Thua | 300 | 300 | 75 | 75 |
| A | Thắng | 450 | 450 | 110 | 110 |
| A | Thua | 225 | 225 | 55 | 55 |
| B | Thắng | 320 | 320 | 80 | 80 |
| B | Thua | 160 | 160 | 40 | 40 |
| C | Thắng | 220 | 220 | 55 | 55 |
| C | Thua | 110 | 110 | 25 | 25 |
| D | Thắng | 150 | 150 | 35 | 35 |
| D | Thua | 75 | 75 | 15 | 15 |

- Nhận trong Day 7. **[GỐC]**
- Tính theo tier của clan **lúc war bắt đầu**.

### 11.3 Điều kiện nhận

- Thưởng clan chỉ dành cho người đã đóng góp ít nhất 1 điểm trong tuần. **[ĐỀ XUẤT]**
- Thưởng chưa nhận mất khi tuần mới bắt đầu (Thứ Ba 0h UTC). **[ĐỀ XUẤT]**
- Bot không nhận thưởng.

### 11.4 Loại thưởng trong game

| Loại | Vật phẩm trong project |
|---|---|
| Lootbox | `ItemType.LOOT_TICKET` |
| Bone | `ItemType.BONE` |
| Exp | Exp người chơi |
| Cloak | `ItemType.CLOAK` |
| Vial | `ItemType.VIAL` |
| Pickaxe, Golden Pickaxe, Drill | Chưa có (chờ tính năng đào mỏ), các bảng trên để 0 |

## 12. Bảng xếp hạng và Championship

### 12.1 Bảng xếp hạng

| Bảng | Nội dung | Mở từ |
|---|---|---|
| Bảng đóng góp | Điểm từng thành viên của một clan, theo ngày hoặc cả tuần | Bấm banner clan ở tab Battle |
| Clan Leaderboard | Các clan đứng đầu theo điểm tier, kèm dòng clan Champion | Nút Leaderboard |
| Championship Ranking | Các clan theo tổng điểm đóng góp trong tuần | Từ Clan Leaderboard |

Ba bảng trên: **[GỐC]**

### 12.2 Trận Championship

- Clan đứng đầu Championship Ranking tuần trước được thách đấu clan Champion. **[GỐC]**
- Chưa có Champion thì hai clan đứng đầu gặp nhau. **[GỐC]**
- Clan thắng trận này là Champion mới, có vương miện trên banner. **[GỐC]**

## 13. Clan bot **[ĐỀ XUẤT]**

Mục tiêu: clan lẻ khi ghép cặp vẫn có war để chơi.

### 13.1 Danh tính

- Tên lấy từ danh sách có sẵn kèm số, ví dụ "Ironclad42".
- Banner ngẫu nhiên.
- Cùng tier với clan thật.

### 13.2 Thành viên

- Số bot bằng số thành viên clan thật, tối thiểu 5, tối đa 30.
- Mỗi bot sao chép bộ đồ của một thành viên clan thật.
- Power từ 80% đến 110% của người đó.

### 13.3 Điểm của bot

- Điểm ngày = điểm clan thật × hệ số từ **0,55 đến 1,25**.
- Hệ số cố định cho từng ngày của từng war: có ngày bot thắng, có ngày bot thua.
- Trong ngày, điểm bot tăng dần theo giờ.
- Clan thật không đóng góp gì trong ngày thì bot vẫn có khoảng 2.100–3.500 điểm và thắng ngày đó.
- Bot không được cộng điểm tier, không lên bảng xếp hạng.

## 14. Chưa chốt

### Chờ bạn quyết định

1. Toàn bộ số điểm và thưởng ở mục 8, 9, 10, 11: đều là số tự đặt, chưa cân bằng với kinh tế game.
2. Chống gian lận điểm: server đang tin số hoạt động client gửi lên, chưa có trần điểm mỗi ngày.
3. Phí tạo clan do client tự trừ gem, server không kiểm tra.
4. Người bị kick có phải chờ 24 giờ không (mục 4.3).
5. Điểm của người rời clan giữa tuần: giữ cho clan cũ hay trừ đi (mục 8.4).
6. Tier chỉ tăng, hay có tụt khi thua nhiều (mục 5).
7. Đối thủ PvP không có đồ khi chưa từng mở PvP Arena (mục 10.1). Đề xuất: gửi bộ đồ lên server ngay khi vào clan.
8. Thưởng chưa nhận có giữ sang tuần sau không (mục 11.3).
9. Có cần nút chuyển Leader thủ công không (mục 3).
10. Day 2 và Day 4 khi chưa có đào mỏ: giữ nguyên hay tạm đổi nhóm hoạt động (mục 8.3).

### Chờ số liệu từ game gốc

1. Ảnh bảng "Day N Actions" ở một ngày lẻ và một ngày chẵn → điểm thật của từng hoạt động.
2. Ảnh popup "War Rewards" → các mốc cá nhân và thưởng Win/Lose theo tier.
3. Ảnh popup "War Results" sau một tuần → War Score từng ngày.
4. Ảnh tab Battle vào Chủ Nhật → số vé PvP, điểm mỗi trận thắng, ngưỡng reset vòng.
5. Tier của một clan qua vài tuần liên tiếp → luật lên/xuống tier.
6. Ba con số 3 Captain, 20 clan mỗi lần tìm, 5 đơn mỗi ngày: có trong bảng hằng số của game gốc nhưng chưa
   xác nhận được đúng ý nghĩa → thử trong game gốc để kiểm.

## 15. Tình trạng code

### Đã làm (2026-10-10)

| Phần | Nội dung | Vị trí |
|---|---|---|
| Server Clan | 13 hàm theo tên gốc | `functions/src/clan.ts` |
| Server Clan War | 10 hàm + tác vụ ghép cặp hàng tuần + 2 hàm admin để thử | `functions/src/clanWar.ts` |
| Config | Mọi con số **[ĐỀ XUẤT]**; ghi đè bằng Firestore `config/clanWar` | `functions/src/clanWarConfig.ts` |
| Test server | Đạt trên emulator | `functions/scripts/clanEmulatorTest.mjs` |
| Client | Tab Clan, 13 popup, tự ghi điểm đóng góp | `Scripts/Clan/`, `Scripts/UI/Clan/`, `Scripts/UI/ClanWar/` |
| Giao diện | Dựng từ prefab game gốc | Menu `Tools/DungeonRush/Build Clan UI Prefabs` |

- Test server đã phủ: tạo, tìm, vào, xin vào, duyệt, thăng chức, kick, rời, giải tán clan; ghép war, gửi điểm,
  bảng xếp hạng, nhận mốc cá nhân.
- Điểm đóng góp tự ghi khi: nhặt đồ, triệu hồi pet, triệu hồi áo choàng, dùng key dungeon, lên level.
- Trận PvP Day 6 dùng lại màn PvP Arena.

### Chưa làm

- Chạy thử trong Unity (Play mode): tab Clan, các popup, tab Battle.
- Test nhánh Day 6 PvP và nhận thưởng Day 7 (phụ thuộc ngày thật trong tuần).
- Soát bố cục và hình ảnh từng màn so với game gốc.
- Ảnh đại diện người chơi.
- Điểm đào mỏ (chờ tính năng đào mỏ).
- Chat clan (game gốc có).
- Triển khai lên project Firebase thật.
