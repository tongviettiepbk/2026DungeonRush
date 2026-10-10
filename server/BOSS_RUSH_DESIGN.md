# Boss Rush — Bản thiết kế (GD)

Cập nhật: 2026-10-10. Server đã code theo bản này (mục 14 ghi phần còn lại). Các mục **[ĐỀ XUẤT]** đang chạy đúng
như đề xuất và nằm trong config, đổi được khi bạn quyết khác.
Số liệu reverse từ game gốc nằm ở `DecodedData/BOSS_RUSH_MODEL.md`; file này là thiết kế của project mình.

Ký hiệu độ chắc chắn của từng mục:

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Xác nhận từ game gốc (đọc từ APK hoặc ảnh chụp khi chơi thật) |
| **[CHỐT]** | Đã thống nhất trong thảo luận |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ SỐ LIỆU]** | Cần ghi thêm từ game gốc mới quyết định được |

---

## 1. Tổng quan

Boss Rush là sự kiện theo tuần. Người chơi được xếp vào một **sảnh** tối đa 100 người cùng league. Cả sảnh cùng
đánh một con boss có **máu chung**. Mỗi lượt đánh, người chơi vào trận cùng 7 người hỗ trợ lấy từ sảnh, đánh boss
trong 30 giây. Cuối mùa, mỗi người nhận thưởng theo số boss cả sảnh đã giết và theo hạng của mình trong sảnh,
rồi lên hoặc xuống league.

- Mở khoá ở PlayerLevel 15. **[GỐC]**
- Vào từ tab Events → Join.

## 2. Mùa

- Một mùa = một tuần: mở **Thứ Ba 00:00 → Chủ nhật 23:59:59 (UTC)**. **[GỐC]**
- **Thứ Hai nghỉ**: chốt mùa cũ, trả thưởng, dựng sảnh mùa mới. **[GỐC]** (phần dựng sảnh là **[ĐỀ XUẤT]**)
- Mã mùa = ngày Thứ Ba bắt đầu, dạng `yyyy-MM-dd`. **[GỐC]**
- Sảnh giữ nguyên thành viên từ lúc tạo tới khi hết mùa và trả thưởng xong. **[CHỐT]**

## 3. League

- Có **10 league**: Iron, Bronze, Silver, Gold, Platinum, Emerald, Diamond, Master, Grandmaster, Legend. **[GỐC]**
- Người mới bắt đầu ở Iron (league 1). **[GỐC]**
- League Boss Rush là giá trị riêng, không liên quan cúp PvP. **[GỐC]**
- League không xếp theo sức mạnh: trong một sảnh Iron thật có người 40M lẫn 629M power. **[GỐC]**
- League càng cao boss càng trâu và đánh càng đau (mục 7).
- Luật lên/xuống league: mục 10.

## 4. Sảnh

### 4.1 Quy tắc chung

- Sảnh tối đa **100 người**, cùng league, cùng mùa. **[GỐC]** (ảnh sảnh thật có hạng tới 100)
- Sảnh cần trông có khoảng **60 người**. Thiếu người thật thì bot bù cho đủ; đủ người thật thì không có bot. **[CHỐT]**
- Chỗ trống còn lại (tới 100) dành cho người thật vào bất cứ lúc nào trong mùa. **[CHỐT]**
- Người thật **không thay chỗ bot**; bot đã vào sảnh thì ở tới hết mùa. **[CHỐT]**
- Sảnh đầy 100 thì tạo sảnh mới theo cùng quy tắc. **[CHỐT]**

### 4.2 Dựng sảnh đầu mùa

Chạy vào Thứ Hai bằng tác vụ hẹn giờ. Dự phòng: nếu tác vụ chưa chạy thì lượt Join đầu tiên của mùa sẽ kích hoạt
(có khoá để hai người vào cùng lúc không dựng trùng). **[ĐỀ XUẤT]**

1. **Chốt từng sảnh mùa cũ**: tính hạng cuối (người thật + bot), ghi thưởng chờ nhận và league mới cho người thật.
2. **Gom người còn hoạt động** của mùa cũ theo league mới của họ.
3. **Trộn ngẫu nhiên** trong từng league rồi **chia đều** vào các sảnh, mỗi sảnh giữ chỗ tối đa 80 người. **[ĐỀ XUẤT]**
4. Sảnh nào dưới 60 người thật thì **thêm bot cho đủ 60**. Sảnh từ 60 người thật trở lên không có bot.
5. Sảnh sẵn sàng, chỗ còn lại tới 100 để trống.

"Còn hoạt động" = mùa trước đã đánh ít nhất 1 trận Boss Rush. **[ĐỀ XUẤT]**

Ví dụ (giữ chỗ tối đa 80 người mỗi sảnh):

| Người còn hoạt động trong một league | Kết quả đầu mùa |
|---|---|
| 0 | Chưa có sảnh. Người đầu tiên Join → sảnh mới: 1 người + 59 bot |
| 25 | 1 sảnh: 25 người + 35 bot, còn 40 chỗ |
| 70 | 1 sảnh: 70 người, không bot, còn 30 chỗ |
| 150 | 2 sảnh × 75 người, không bot, mỗi sảnh còn 25 chỗ |
| 200 | 3 sảnh × 66–67 người, không bot |

Không chia sảnh theo power: sảnh toàn người yếu sẽ dễ lấy thưởng top hơn hẳn. **[ĐỀ XUẤT]**

### 4.3 Khi người chơi bấm Join

1. Đã được giữ chỗ từ đầu mùa → vào thẳng sảnh đó.
2. Chưa có chỗ (người mới, người mùa trước không chơi):
   - Có sảnh cùng league còn chỗ → vào sảnh đang có **nhiều người thật nhất**.
   - Không còn sảnh nào → tạo sảnh mới: 1 người + 59 bot.
3. Hiện sảnh: bảng xếp hạng, boss hiện tại, thời gian còn lại của mùa, số vé.

Sảnh tạo giữa mùa: bot đã có sẵn điểm ứng với số ngày đã qua và boss đã mất máu tương ứng, trông như sảnh chạy
từ đầu mùa. **[ĐỀ XUẤT]**

## 5. Trận đánh

- Bấm Fight là vào trận ngay, không chờ ai. **[GỐC]**
- Đội gồm người chơi + **7 người hỗ trợ** lấy ngẫu nhiên từ sảnh (người thật hoặc bot), mặc đúng bộ đồ của họ. **[GỐC]**
- Không có pet ra trận, kể cả của người hỗ trợ. Chỉ số cộng thêm từ pet đang sở hữu vẫn tính. **[GỐC]**
- Trận dài **30 giây**, sân 12×12. Chỉ hết giờ mới kết thúc; hero chết vẫn chờ hết giờ. **[GỐC]**
- Hết giờ → gửi kết quả lên server → popup "Fight Complete!" (Team Damage, Damage Dealt). **[GỐC]**
- **Thoát giữa trận: không tính gì cả** (không trừ máu boss, không cộng điểm). **[GỐC]**
  Vé đã bị trừ lúc bắt đầu trận (theo cách server hiện tại làm).

## 6. Điểm và xếp hạng

- **Máu boss** bị trừ theo tổng damage của **cả 8 người** trong trận. **[CHỐT]**
- **Bảng xếp hạng** chỉ cộng damage của **chính người vào đánh**. 7 người hỗ trợ không được cộng gì. **[CHỐT]**
- Hạng trong sảnh = thứ tự điểm giảm dần; bằng điểm thì ai vào sảnh trước đứng trên.
- Hạng trong sảnh chính là "Placement" dùng để trả thưởng và xét lên/xuống league. **[GỐC]**
- Điểm phụ thuộc chủ yếu vào số trận đã đánh, không đi theo power (ảnh sảnh thật: 629M power ≈ 3.82K điểm,
  41M power ≈ 3.30K điểm). **[GỐC]**
- Cách quy đổi damage → điểm: chưa rõ. Điểm thật tính bằng K trong khi máu boss tính bằng M. Config gốc có
  `DamageDivisor = 100` mà client không dùng, có thể server chia 100. **[CHỜ SỐ LIỆU]**

## 7. Boss

- Mỗi league có 20 boss nối tiếp; giết boss này thì boss kế xuất hiện ngay. **[GỐC]**
- 10 mẫu boss lặp vòng: Dark Lich, Black Dragon, … **[GỐC]**
- Máu boss #1 theo league: 500M, 1B, 2B, 3B, 5B, 10B, 20B, 30B, 50B, 100B. Boss sau trâu hơn boss trước khoảng
  1.5 lần. Bảng đầy đủ: `functions/src/config.ts`. **[GỐC]**
- Sát thương mỗi đòn của boss theo league: 300, 600, 900, 3000, 6000, 9000, 12000, 15000, 18000, 21000. **[GỐC]**
- Tham chiếu tốc độ: một sảnh Iron thật (khoảng 100 người) mới giết 1 boss sau khoảng 4 ngày. **[GỐC]**

## 8. Vé

- Vé reset mỗi ngày lúc 0h UTC. **[GỐC]**
- Config trong APK ghi 3 vé thường + 3 vé xem quảng cáo, nhưng save gốc và thẻ ở tab Events lại hiện 5. Số thật
  do server gốc trả về. **[CHỜ SỐ LIỆU]** (server mình đang dùng 3 + 3)

## 9. Thưởng

Chỉ trả **một lần khi hết mùa**, không có thưởng theo trận hay theo ngày. **[GỐC]**
Thưởng = phần Guaranteed + phần Placement.

### 9.1 Guaranteed — theo số boss cả sảnh đã giết

Cả sảnh nhận như nhau. Tỉ lệ Cloak : Lootbox : Bone luôn là 1 : 3 : 3, kèm key dungeon. **[GỐC]**

| Thời điểm (Iron) | Cloak | Lootbox | Bone | Key Dragon | Key Zombie |
|---|---|---|---|---|---|
| Sảnh đang ở Boss #2 (đã giết 1 boss) | 140 | 420 | 420 | 2 | 2 |
| Chốt mùa (mùa khác) | 180 | 540 | 540 | 2 | 2 |

Bước tăng theo từng boss: chưa đủ số liệu. Server đang dùng tạm **Cloak = 120 + 20 × số boss đã giết** (Lootbox và
Bone gấp 3), khớp cả hai mốc trên nếu mùa đó sảnh giết 3 boss. **[CHỜ SỐ LIỆU]**

### 9.2 Placement — theo hạng trong sảnh (Iron)

| Hạng | Cloak | Gem |
|---|---|---|
| 1 | 100 | 100 |
| 2 | 80 | 80 |
| 3 | 50 | 50 |
| 4–10 | 30 | 30 |
| 11–30 | 20 | 20 |
| 31–50 | 10 | 10 |
| 51–100 | 5 | 5 |

**[GỐC]** — đã đối chiếu khớp với một lần nhận thưởng thật: hạng 71 nhận 185 Cloak (180 + 5), 540 Lootbox,
540 Bone, 2 + 2 key, 5 Gem.

### 9.3 Thưởng theo league

Bảng trên là của Iron. League cao hơn nhân với hệ số (key dungeon không nhân, vì mỗi ngày chỉ có 2 key): **[ĐỀ XUẤT]**

| League | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Hệ số | 1 | 1.2 | 1.4 | 1.6 | 1.8 | 2 | 2.3 | 2.6 | 3 | 3.5 |

### 9.4 Điều kiện nhận

- Chỉ người đã đánh ít nhất 1 trận trong mùa mới được thưởng (người được giữ chỗ mà không quay lại thì không). **[ĐỀ XUẤT]**
- Bot không nhận thưởng.

### 9.5 Loại thưởng trong game

| Loại | Vật phẩm trong project |
|---|---|
| Cloak | `ItemType.CLOAK` (summon áo choàng) |
| Lootbox | `ItemType.LOOT_TICKET` |
| Bone | `ItemType.BONE` |
| Gem | `ItemType.GEM` |
| Key Dragon / Zombie / Cultist | Key của dungeon tương ứng |

## 10. Lên / xuống league **[ĐỀ XUẤT]**

Xét theo hạng hiển thị trong sảnh lúc chốt mùa (tính cả bot).

| League | Lên nếu | Xuống nếu |
|---|---|---|
| Iron | Hạng 1–15 | Không xuống |
| Bronze, Silver | Hạng 1–15 | Không đánh trận nào trong mùa |
| Gold, Platinum, Emerald | Hạng 1–10 | Trong 15% cuối sảnh, hoặc không đánh trận nào |
| Diamond, Master, Grandmaster | Hạng 1–5 | Trong 25% cuối sảnh, hoặc không đánh trận nào |
| Legend | Không lên nữa | Trong 30% cuối sảnh, hoặc không đánh trận nào |

- Mỗi mùa chỉ lên hoặc xuống một league.
- Phải có điểm mới được lên.
- "X% cuối sảnh" tính theo số người thực có trong sảnh, không tính theo 100.
- Bot chiếm hạng nhưng không lên hay xuống.
- Người không Join mùa nào thì giữ nguyên league.

Các con số nằm trong config trên Firestore, chỉnh được mà không cần deploy lại.

## 11. Bot

Mục tiêu: sảnh không trống khi game ít người, và người chơi khó nhận ra bot. Khi đủ người thật thì bot tự hết.

### 11.1 Vòng đời

- Bot chỉ được tạo lúc tạo sảnh, số lượng = 60 − số người thật của sảnh lúc đó (tối thiểu 0). **[CHỐT]**
- Bot ở trong sảnh tới hết mùa. Mùa mới tạo bot mới hoàn toàn, không giữ danh tính. **[CHỐT]**
- Bot không nhận thưởng, không lên/xuống league.

### 11.2 Danh tính **[ĐỀ XUẤT]**

- Id giống uid thật, không có tiền tố riêng. Cờ bot chỉ lưu ở server, **không gửi về client**.
- Tên: khoảng 20–30% bot dùng đúng dạng tên mặc định của game ("Player" + 6 số); còn lại lấy từ kho vài nghìn
  tên nhiều kiểu. Không trùng trong sảnh, không trùng tên người thật cùng sảnh.
- Avatar ngẫu nhiên, đa số là avatar mặc định.

### 11.3 Bộ đồ và sức mạnh **[ĐỀ XUẤT]**

Bot có thể được chọn làm người hỗ trợ và người chơi xem được hồ sơ của bot (mục 12), nên bộ đồ phải khớp đúng
với power hiển thị.

- Dùng một công cụ trong Unity Editor **mô phỏng nhân vật chơi 7 ngày** bằng đúng công thức của game (tỉ lệ ra
  đồ, level đồ, nâng cánh, relic, công thức Power), chụp lại bộ đồ mỗi ngày → một **chuỗi 7 bộ đồ** kèm power.
- Sinh vài trăm đến vài nghìn chuỗi thành một "ngân hàng", **đóng gói kèm code server**.
- Mỗi bot gán một chuỗi. Mỗi ngày bot có một giờ "đăng nhập" riêng: tới giờ đó chuyển sang bộ đồ kế tiếp;
  ngày nào không đăng nhập thì giữ nguyên. Sức mạnh nhờ vậy tăng theo bước nhảy như người thật.
- Về sau bổ sung ngân hàng bằng lịch sử bộ đồ (ẩn danh) của người chơi thật.

Mẫu người thật ở Iron để đối chiếu: đồ độ hiếm cao nhưng level 1–4, cánh level 25–43, áo choàng có hoặc không,
relic +1 đến +3, 3 pet đang dùng.

### 11.4 Điểm của bot **[ĐỀ XUẤT]**

Bot không "chạy". Mỗi bot có một lịch chơi cố định sinh từ seed lúc tạo sảnh; điểm tại một thời điểm được **tính
ra** từ seed và giờ hiện tại mỗi khi có người mở sảnh.

| Nhóm | Tỉ lệ | Hành vi |
|---|---|---|
| Không chơi | ~15% | Vào sảnh rồi để 0 điểm |
| Thỉnh thoảng | ~35% | 1–2 trận/ngày, có ngày nghỉ |
| Đều đặn | ~35% | 3–4 trận/ngày |
| Cày | ~15% | Hết vé + xem quảng cáo |

- Mỗi bot có khung giờ chơi riêng nên điểm không tăng đồng loạt.
- Điểm mỗi trận dao động, số lẻ; điểm tổng không đi theo power.
- Tham số khoá cứng lúc tạo sảnh. Mỗi sảnh ghi phiên bản công thức; đổi công thức thì sảnh cũ vẫn dùng bản cũ
  tới hết mùa, để điểm không bao giờ giảm.

### 11.5 Bot và máu boss **[ĐỀ XUẤT]**

Mỗi trận của bot cũng trừ máu boss chung (damage của bot + 7 người hỗ trợ ước lượng), tính theo cùng lịch chơi.
Tốc độ này ảnh hưởng thẳng tới thưởng Guaranteed của người thật nên là một núm chỉnh kinh tế.

### 11.6 Chi phí vận hành

| Việc | Chi phí |
|---|---|
| Tạo bot | Ghi một lần lúc tạo sảnh, khoảng 10KB cho 59 bot trong document sảnh |
| Điểm bot, máu boss | Tính lúc đọc sảnh, không đọc/ghi thêm |
| Bộ đồ bot | Lấy từ ngân hàng đóng gói trong code, không đọc database |
| Hết mùa | Tính điểm bot tại thời điểm kết thúc mùa, không cần tác vụ riêng |

## 12. Hồ sơ người chơi

Bấm avatar một người trong sảnh → popup thông tin: **[GỐC]**

- Tên, avatar.
- Hình hero mặc đồ và 3 pet trên một ô lưới nhỏ.
- Power.
- 8 ô đồ: độ hiếm, level, huy hiệu relic (+N).
- 3 pet đang dùng.
- Danh sách tổng chỉ số phụ.

Chỉ tải dữ liệu khi người chơi bấm vào. Áp dụng cho cả người thật lẫn bot.

## 13. Chưa chốt

### Chờ bạn quyết định

1. Giữ chỗ tối đa 80 hay 60 người cũ mỗi sảnh (mục 4.2).
2. Định nghĩa "còn hoạt động" (mục 4.2) và điều kiện nhận thưởng (mục 9.4).
3. Bot có được chiếm top 3 không. Đề xuất: giới hạn nhóm "cày" để người thật đánh hết vé + xem quảng cáo luôn
   có cửa vào top 3.
4. Tốc độ bot giết boss: bám mốc gốc (1–2 boss/tuần ở Iron) hay rộng tay hơn.
5. Luật lên/xuống league (mục 10) và hệ số thưởng theo league (mục 9.3).
6. Có thưởng một lần khi lần đầu đạt league mới không.

### Chờ số liệu từ game gốc

1. Điểm trước và sau một trận + "Damage Dealt" / "Team Damage" của trận đó → cách quy đổi điểm.
2. Bảng Rewards chụp kèm "Boss #N" ở vài thời điểm → bước tăng Guaranteed theo boss.
3. Số vé thường và vé quảng cáo mỗi ngày.
4. Tổng số người trong một sảnh (kéo xuống cuối danh sách).
5. Hạng của người được lên league lúc chốt mùa, nếu thấy.
6. Bảng Rewards của league khác Iron.

## 14. Tình trạng code

### Đã làm (2026-10-10)

| Hạng mục | Ở đâu |
|---|---|
| Sảnh 100 người, bot bù cho đủ 60, người thật vào chỗ trống | `functions/src/season.ts` (`createPool`), `bossRush.ts` (Join) |
| Bot tính từ seed, không chạy nền; điểm, máu boss, power | `functions/src/bots.ts`, `bossRushPool.ts` |
| Chốt mùa cũ + dựng sảnh mùa mới, giữ chỗ người còn hoạt động | `season.ts` (`prepareSeason`), tác vụ Thứ Hai + dự phòng lúc Join |
| Bảng thưởng Iron thật, hệ số league, điều kiện ≥ 1 trận | `functions/src/rewardConfig.ts` |
| Lên/xuống league | `rewardConfig.ts` (`computeNewTier`) |
| 7 người hỗ trợ do server chọn, kèm bộ đồ | `startbossrushfight` trả `allies` |
| Hồ sơ người chơi (phía server) | `getbossrushprofile` |
| Bảng xếp hạng gọn (không kèm bộ đồ), không lộ cờ bot | `BossRushPlayerRow` |
| Client: nhận `allies`, bỏ cờ bot, báo "đang chuẩn bị" | `Scripts/BossRush/` |
| Cộng thưởng Cloak, Lootbox, 3 loại key ở client | `BossRushController.GrantRewards` |
| Ngân hàng bộ đồ cho bot: 580 nhân vật mô phỏng (level 15–100) × 7 mốc | Công cụ Unity `Assets/Editor/BossRushBotBankGenerator.cs` (menu DungeonRush → Boss Rush → Generate Bot Bank) xuất `functions/data/botBank.json` |

Ngân hàng bộ đồ phải **sinh lại** mỗi khi đổi cân bằng đồ hoặc công thức Power, rồi deploy lại server. Mỗi sảnh chọn
cho bot các chuỗi có power gần người thật trong sảnh nhất, không trùng nhau.

Độ phủ sức mạnh của ngân hàng (đo bằng cách dựng thử một sảnh 59 bot quanh người chơi có power P):

| Power người chơi | Bot nằm trong khoảng 0.25×–8× | Bot mạnh hơn người chơi |
|---|---|---|
| 5e5 (level 15, gần như chưa có đồ) | 58 / 59 | 52 / 59 |
| 1e6 → 4e13 (level 15 → level 100 bình thường) | 55–59 / 59 | khoảng một nửa |
| 1e14 (level 100, cày nặng) | 37 / 59 | 17 / 59 |
| từ 3e14 trở lên | dưới 20 / 59 | 0 |

Ngân hàng trải từ 3.5e5 đến 2e14. Trên 2e14 là mức mô phỏng không tạo ra được, nên người chơi mạnh cỡ đó sẽ thấy mọi bot
yếu hơn mình. Muốn phủ thêm thì tăng mức cày của nhóm `HIGH_END_COUNT` trong công cụ.

Test: `npm test` (thưởng, league, chia sảnh, máu boss, bot) và `npm run test:emulator` (cả luồng trên emulator).

### Mốc của bot khi sảnh chưa có số liệu

Bot cần biết "một người chơi trung bình đánh được bao nhiêu damage mỗi trận" để điểm của nó hợp lý. Mốc này lấy theo
thứ tự: damage trung bình của người thật trong sảnh lúc tạo → số liệu chung của league → **chưa có**. Trường hợp
chưa có (những sảnh đầu tiên sau khi ra game), bot đứng yên ở 0 điểm cho tới trận thật đầu tiên của sảnh, lấy trận
đó làm mốc và bắt đầu hoạt động từ lúc đó.

### Chưa làm

| Hạng mục | Ghi chú |
|---|---|
| Popup hồ sơ ở client (mục 12) | Server đã có `getbossrushprofile` |
| Avatar | Client chưa có hệ avatar; mọi người đang là avatar 0 |
| Quy đổi damage → điểm | Đang để điểm = damage |
| Số vé | Đang 3 thường + 3 quảng cáo |
| Chống gian lận | Server vẫn tin damage client gửi, chưa kiểm tra level 15 |
| Deploy thật | Cần deploy cả index Firestore mới (`firestore.indexes.json`) |
