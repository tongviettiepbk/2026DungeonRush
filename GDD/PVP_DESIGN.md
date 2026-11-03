# PvP — Bản thiết kế (GD)

Cập nhật 2026-11-04: chuyển từ PvP Elo (bản v41) sang hệ mùa + nhóm league theo game thật (bản v59).

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Thấy trong game thật hoặc code bản v59 |
| **[CHỐT]** | Người làm game đã quyết |
| **[ĐỀ XUẤT]** | Tự đặt, chỉnh được |
| **[CHỜ SỐ LIỆU]** | Chưa có số thật, đang dùng tạm |

## 1. Tóm tắt

Mỗi mùa người chơi vào một nhóm tối đa 100 người cùng league. Mỗi ngày có 5 vé để thách đấu người trong nhóm, thắng được
cúp. Hết mùa, nhóm xếp hạng theo cúp: nhận thưởng theo hạng, người đầu bảng lên league, người cuối bảng xuống league.

Trận đấu giữ nguyên: bất đồng bộ, 30 giây, đánh với snapshot đồ và pet của đối thủ.

## 2. Điều kiện mở

- Mở từ PlayerLevel 15. **[GỐC]**

## 3. Mùa

- Lịch mùa dùng chung với Boss Rush: Thứ Ba 00:00 đến Chủ nhật 23:59:59 UTC. **[ĐỀ XUẤT]**
- Thứ Hai nghỉ, không thách đấu được. **[ĐỀ XUẤT]**
- Cúp về 0 khi sang mùa mới. **[ĐỀ XUẤT]**
- Popup hiện "Season ends in", còn ngày nghỉ hiện giờ tới mùa sau. **[GỐC]**

## 4. League và nhóm

- Có 10 league: Iron, Bronze, Silver, Gold, Platinum, Emerald, Diamond, Master, Grandmaster, Legend. **[GỐC]**
- Người mới và dữ liệu cũ bắt đầu ở Iron, 0 cúp. **[CHỐT]**
- League PvP tách riêng với league Boss Rush. **[GỐC]**
- League không tính theo cúp. **[GỐC]**
- Nhóm tối đa 100 người cùng league, cùng mùa. **[GỐC]**
- League cao ít người thì nhóm nhỏ hơn 100. **[GỐC]**
- Người chơi vào nhóm lúc mở popup PvP lần đầu trong mùa. **[ĐỀ XUẤT]**
- Vào nhóm còn chỗ đầu tiên của league, đầy thì sang nhóm mới. **[ĐỀ XUẤT]**

### 4.1 Bot **[ĐỀ XUẤT]**

- Tạo nhóm mới thì bù bot cho đủ 30 người, để luôn có đối thủ.
- Người thật vào sau không thay chỗ bot.
- Bot dùng chung ngân hàng bộ đồ của Boss Rush.
- Cúp của bot tăng dần theo thời gian trong mùa, không lưu vào database.
- Bot không nhận thưởng, không lên xuống league và không bị đổi cúp khi thua.

## 5. Vé

- 5 vé mỗi ngày. **[CHỐT]**
- Không có vé xem quảng cáo. **[CHỐT]**
- Vé hồi lúc 0h UTC. **[GỐC]**
- Mỗi lần thách đấu tốn 1 vé, thắng hay thua đều mất. **[GỐC]**

## 6. Chọn đối thủ

- Đối thủ lấy trong cùng nhóm. **[CHỐT]**
- Tìm theo số cúp gần mình, không theo sức mạnh. **[CHỐT]**
- Tối đa 5 người, ưu tiên có cả người trên và dưới cúp mình. **[CHỐT]**
- Ít người phù hợp thì hiện ít dòng hơn. **[CHỐT]**
- Cùng độ lệch cúp thì chọn ngẫu nhiên. **[ĐỀ XUẤT]**
- Danh sách giữ trong 10 phút, đã đánh ai thì bỏ người đó khỏi danh sách. **[ĐỀ XUẤT]**

## 7. Cúp

- Thắng nhận đúng số cúp hiện lúc chọn đối thủ. **[CHỐT]**
- Thua không mất cúp. **[CHỐT]**
- Người bị đánh không đổi cúp. **[ĐỀ XUẤT]**
- Hết giờ báo kết quả tính là thua. **[ĐỀ XUẤT]**

Danh sách xếp theo cúp giảm dần, người nhiều cúp nhất nhận mức cao nhất. Cùng cúp thì ai mạnh hơn đứng trên. **[ĐỀ XUẤT]**

| Vị trí trong danh sách | Cúp nếu thắng |
|---|---|
| 1 | +5 |
| 2 | +4 |
| 3 | +3 |
| 4 | +2 |
| 5 | +1 |

Danh sách ít hơn 5 người thì phát từ +5 trở xuống, ví dụ 3 người là +5, +4, +3. **[ĐỀ XUẤT]**

## 8. Bảng xếp hạng nhóm

- Xếp theo cúp giảm dần. **[CHỐT]**
- Cùng cúp thì xếp theo sức mạnh giảm dần. **[CHỐT]**
- Mỗi dòng có hạng, avatar, tên, sức mạnh, số cúp. **[GỐC]**
- Dòng của mình ghim xanh ở đáy khi đang ở ngoài vùng nhìn thấy. **[GỐC]**
- Có vạch PROMOTION AREA dưới người cuối được lên. **[GỐC]**
- Có vạch DEMOTION AREA trên người đầu tiên bị xuống. **[GỐC]**

## 9. Lên và xuống league

- Tính theo phần trăm số người trong nhóm, kể cả bot. **[CHỐT]**
- Số người lên làm tròn lên, số người xuống làm tròn xuống. **[ĐỀ XUẤT]**
- Mỗi mùa chỉ lên hoặc xuống một league. **[ĐỀ XUẤT]**
- Phải có ít nhất 1 cúp mới được lên. **[ĐỀ XUẤT]**
- Thứ hạng lấy lúc hết mùa. **[GỐC]**

| League | Lên | Xuống |
|---|---|---|
| Iron | 40% | 0% |
| Bronze | 35% | 5% |
| Silver | 30% | 10% |
| Gold | 30% | 10% |
| Platinum | 25% | 15% |
| Emerald | 20% | 15% |
| Diamond | 15% | 20% |
| Master | 10% | 20% |
| Grandmaster | 10% | 25% |
| Legend | 0% | 25% |

- Iron là số thật: nhóm 100 người có vạch lên ở hạng 40 và không xuống. **[GỐC]**
- Các league còn lại là số tự đặt, chỉnh được ở `config/pvp` không cần deploy. **[ĐỀ XUẤT]**

## 10. Thưởng mùa

- Chỉ trả một lần khi hết mùa, không có thưởng theo trận. **[GỐC]**
- Tính theo league của mùa vừa xong, không phải league mới. **[GỐC]**
- Phải đánh ít nhất 1 trận trong mùa mới có thưởng. **[ĐỀ XUẤT]**
- Mở popup PvP lần đầu sau mùa sẽ hiện "Season Complete!" để nhận. **[GỐC]**

### 10.1 Công thức **[GỐC]**

Dãy Gem, mỗi số bằng số trước × 1.2 rồi làm tròn:

`50, 60, 72, 86, 103, 124, 149, 179, 215, 258, 310, 372, 446, 535, 642, 770, 924`

Vị trí trong dãy (tính từ 0) = (league − 1) + bậc hạng, với Iron là league 1 và Legend là league 10.

| Hạng | Bậc |
|---|---|
| 1 | 7 |
| 2 | 6 |
| 3 | 5 |
| 4–10 | 3 |
| 11–25 | 2 |
| 26–50 | 1 |
| 51–100 | 0 |

| Món | Số lượng |
|---|---|
| Gem | Theo dãy |
| Lootbox | Gem × 4 |
| Bone | Gem × 6 |
| Cloak | Gem × 3 |

Đã đối chiếu khớp đủ 280 ô trong ảnh bảng thưởng của 10 league.

### 10.2 Ví dụ

| League | Hạng | Gem | Lootbox | Bone | Cloak |
|---|---|---|---|---|---|
| Iron | 1 | 179 | 716 | 1074 | 537 |
| Iron | 51–100 | 50 | 200 | 300 | 150 |
| Gold | 11–25 | 124 | 496 | 744 | 372 |
| Legend | 1 | 924 | 3696 | 5544 | 2772 |

## 11. Màn hình

1. **Popup PvP:** tên league, đồng hồ mùa, nút Season Rewards, danh sách nhóm, dòng ghim, nút Challenge.
2. **Chọn đối thủ:** giờ hồi vé, số vé, tối đa 5 dòng có tên, sức mạnh, cúp nếu thắng, nút Challenge tốn 1 vé.
3. **Cuối trận:** Victory hoặc Defeated, số cúp mới và số cúp vừa cộng, không có thưởng.
4. **Season Rewards:** lật qua lại 10 league, mỗi league 7 khung hạng với 4 món thưởng.
5. **Season Complete:** hạng cuối, league mới nếu đổi, số cúp, thưởng, nút Claim.

## 12. Chưa chốt

### Chờ bạn quyết định

1. Có giữ cách xếp cùng cúp theo sức mạnh không, vì ảnh game thật không xếp như vậy.
2. Tỉ lệ lên và xuống của 9 league trên Iron.

### Chờ số liệu từ game gốc

1. Độ dài mùa và giờ kết thúc mùa chính xác.
2. Luật gán +5 đến +1 của server gốc.
3. Số người thật trong nhóm Platinum, để tính lại tỉ lệ lên và xuống.

## 13. Tình trạng code

### Đã làm

1. Server: `pvp.ts`, `pvpConfig.ts`, `pvpGroup.ts`; nhóm được chốt khi thành viên đầu tiên mở PvP sau mùa, không cần tác vụ hẹn giờ.
2. Client: `Scripts/PvP`, `Scripts/UI/PvP`, popup mới `UIPvPSeasonClaimPopup`.
3. Test: `pvpConfigTest`, `pvpEmulatorTest`, phần B của `onlineModesSeedTest`.
4. Chạy thử trong Unity trên emulator: popup đầu, thưởng mùa, chọn đối thủ, thắng, nhận thưởng mùa.

### Chưa làm

1. Xem hồ sơ khi bấm vào dòng người chơi.
2. Deploy server thật.
3. Chữ dịch của 9 ngôn ngữ ngoài tiếng Anh chưa được người đọc lại.
