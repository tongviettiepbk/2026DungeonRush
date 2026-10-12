# Pet (Companion) — Bản thiết kế (GD)

Cập nhật: 2026-10-12. Client đã code theo bản này (mục 14 ghi phần còn lại). Các mục **[ĐỀ XUẤT]** đang chạy đúng
như đề xuất, đổi được khi bạn quyết khác.
Số liệu reverse từ game gốc nằm ở `DecodedData/COMPANION_MODEL.md`; file này là thiết kế của project mình.
Chỉ số từng pet lấy từ `Resources/Scriptable Objects/Companions/*.asset`; avatar là sprite `icon` của asset đó, thu nhỏ còn 80px và lưu ở `GDD/img/companion/`.

Ký hiệu độ chắc chắn của từng mục:

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Xác nhận từ game gốc (đọc từ APK hoặc ảnh chụp khi chơi thật) |
| **[CHỐT]** | Đã thống nhất trong thảo luận |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ SỐ LIỆU]** | Cần ghi thêm từ game gốc mới quyết định được |

---

## 1. Tổng quan

Pet là bạn đồng hành đi theo hero trong trận, tự tung kỹ năng theo hồi chiêu. Có **18 pet** chia đều 6 độ hiếm.
Người chơi lấy pet bằng cách **summon** (tiêu Bone hoặc xem quảng cáo), gom thẻ trùng để **nâng cấp**, và chọn
**3 pet** mang vào trận. Pet đã sở hữu còn cộng thẳng Attack và Health cho hero, dù có mang vào trận hay không.

- Có 18 pet, mỗi độ hiếm 3 con. **[GỐC]**
- Mỗi pet có một kiểu kỹ năng riêng, tổng cộng 16 kiểu. **[GỐC]**
- Mang tối đa 3 pet vào trận. **[GỐC]**
- Mọi pet đã sở hữu đều cộng chỉ số cho hero (mục 6). **[GỐC]**

## 2. Mở khoá

- Tab Pet mở ở PlayerLevel 5. **[GỐC]**
- PlayerLevel là cấp tài khoản, không phải số màn campaign đã qua. **[GỐC]**
- Dungeon Zombie Outbreak (nguồn Bone) cũng mở ở PlayerLevel 5. **[GỐC]**

## 3. Danh sách 18 pet

| Avatar | Pet | Độ hiếm | Kiểu kỹ năng | Vai trò |
|---|---|---|---|---|
| ![Ember Fist](img/companion/ember_fist.png) | Ember Fist | Common | DPS | Sát thương đơn |
| ![Magic Wool](img/companion/magic_wool.png) | Magic Wool | Common | Healer | Hồi máu |
| ![Spark Mouse](img/companion/spark_mouse.png) | Spark Mouse | Common | Lightning | Sát thương đơn |
| ![Flame Wing](img/companion/flame_wing.png) | Flame Wing | Uncommon | Bomber | Sát thương vùng |
| ![Life Horn](img/companion/life_horn.png) | Life Horn | Uncommon | ChainHealer | Hồi máu |
| ![Snow Fang](img/companion/snow_fang.png) | Snow Fang | Uncommon | Slower | Khống chế |
| ![Blaze Tail](img/companion/blaze_tail.png) | Blaze Tail | Rare | Bomber | Sát thương vùng |
| ![Storm Eye](img/companion/storm_eye.png) | Storm Eye | Rare | Lightning | Sát thương lan |
| ![Frost Claw](img/companion/frost_claw.png) | Frost Claw | Rare | MultiSlower | Khống chế |
| ![Arcane Paw](img/companion/arcane_paw.png) | Arcane Paw | Epic | Beam | Sát thương đơn |
| ![Vital Root](img/companion/vital_root.png) | Vital Root | Epic | Guardian | Phòng thủ |
| ![Glacier Fist](img/companion/glacier_fist.png) | Glacier Fist | Epic | AoeSlower | Khống chế vùng |
| ![Echo Wing](img/companion/echo_wing.png) | Echo Wing | Legendary | Blaster | Sát thương vùng |
| ![Phantom Gaze](img/companion/phantom_gaze.png) | Phantom Gaze | Legendary | Siphon | Sát thương + hồi máu |
| ![Star Feather](img/companion/star_feather.png) | Star Feather | Legendary | Meteor | Sát thương vùng |
| ![Halo Hare](img/companion/halo_hare.png) | Halo Hare | Mythic | MirrorClone | Triệu hồi |
| ![Radiant Talon](img/companion/radiant_talon.png) | Radiant Talon | Mythic | HealNova | Hồi máu + sát thương vùng |
| ![Celestial Mind](img/companion/celestial_mind.png) | Celestial Mind | Mythic | Immortality | Phòng thủ |

- Trong data, độ hiếm thứ 6 của pet đặt tên asset là `Divine`, nhưng giá trị rarity là Mythic. **[GỐC]**

## 4. Chi tiết từng pet

Cách đọc bảng của mỗi pet:

- "Gốc" là số trong data; "lv 1" và "lv 100" là số thật ở level đó, chưa nhân hệ số sát thương pet (mục 5).
- Khoảng cách và tốc độ tính theo đơn vị thế giới của Unity.
- Một lượt của pet = pha kích hoạt + hồi chiêu (mục 5.3).

### 4.1 Common

#### Ember Fist

![Ember Fist](img/companion/ember_fist.png)

Kiểu **DPS** — sát thương đơn.

- Bắn một viên đạn lửa đuổi theo enemy gần hero nhất. **[GỐC]**
- Trúng gây sát thương một mục tiêu. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 2.9 giây |
| Tốc độ đạn | 10 |
| Sát thương gốc | 90 |
| Sát thương lv 1 | 94.5 |
| Sát thương lv 100 | 540 |

#### Magic Wool

![Magic Wool](img/companion/magic_wool.png)

Kiểu **Healer** — hồi máu.

- Bắn một viên đạn hồi máu bay về hero. **[GỐC]**
- Bắn mỗi lần hồi chiêu xong, kể cả khi hero đầy máu. **[GỐC]**
- Không cần có enemy trên sân. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 1.9 giây |
| Tốc độ đạn | 8 |
| Hồi máu gốc | 60 |
| Hồi máu lv 1 | 63 |
| Hồi máu lv 100 | 360 |

#### Spark Mouse

![Spark Mouse](img/companion/spark_mouse.png)

Kiểu **Lightning** — sát thương đơn.

- Phóng tia sét vào enemy gần hero nhất. **[GỐC]**
- Tia chạm mục tiêu gây trọn sát thương. **[GỐC]**
- Tia được giữ một lúc, mỗi nhịp gây trọn sát thương thêm lần nữa. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 1 giây |
| Hồi chiêu | 3 giây |
| Thời gian giữ tia | 1 giây |
| Nhịp sát thương | 0.2 giây |
| Số mục tiêu | 1 |
| Sát thương gốc | 12 |
| Sát thương lv 1 | 12.6 |
| Sát thương lv 100 | 72 |

### 4.2 Uncommon

#### Flame Wing

![Flame Wing](img/companion/flame_wing.png)

Kiểu **Bomber** — sát thương vùng.

- Ném bom theo đường cong vào chỗ enemy đứng đông nhất. **[GỐC]**
- Bom nổ gây sát thương mọi enemy trong bán kính nổ. **[GỐC]**
- Không gây cháy. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 7.9 giây |
| Bán kính nổ | 2 |
| Tốc độ bom | 7 |
| Sát thương gốc | 240 |
| Sát thương lv 1 | 252 |
| Sát thương lv 100 | 1,440 |

#### Life Horn

![Life Horn](img/companion/life_horn.png)

Kiểu **ChainHealer** — hồi máu.

- Nối một tia hồi máu vào hero. **[GỐC]**
- Tia được giữ một lúc, mỗi nhịp hồi trọn lượng máu. **[GỐC]**
- Không cần có enemy trên sân. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Chờ đầu trận | 1 giây |
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 4.9 giây |
| Thời gian giữ tia | 3 giây |
| Nhịp hồi máu | 0.1 giây |
| Hồi máu gốc | 90 |
| Hồi máu lv 1 | 94.5 |
| Hồi máu lv 100 | 540 |

#### Snow Fang

![Snow Fang](img/companion/snow_fang.png)

Kiểu **Slower** — khống chế.

- Bắn cầu băng vào enemy gần hero nhất, gây sát thương. **[GỐC]**
- Mục tiêu bị làm chậm cả tốc độ chạy lẫn tốc độ đánh. **[GỐC]**
- Trúng lại khi đang chậm chỉ làm mới thời gian, không cộng dồn. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 2 giây |
| Hồi chiêu | 2 giây |
| Tốc độ đạn | 12 |
| Làm chậm | 50% |
| Thời gian chậm | 2 giây |
| Sát thương gốc | 900 |
| Sát thương lv 1 | 945 |
| Sát thương lv 100 | 5,400 |

### 4.3 Rare

#### Blaze Tail

![Blaze Tail](img/companion/blaze_tail.png)

Kiểu **Bomber** — sát thương vùng.

- Ném bom như Flame Wing: vào chỗ đông enemy nhất, nổ theo bán kính. **[GỐC]**
- Enemy trúng bom bị cháy, mỗi nhịp mất một lượng máu cố định. **[GỐC]**
- Sát thương cháy không tăng theo level và không nhân hệ số sát thương pet. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 7.9 giây |
| Bán kính nổ | 2 |
| Tốc độ bom | 7 |
| Sát thương gốc | 1,800 |
| Sát thương lv 1 | 1,890 |
| Sát thương lv 100 | 10,800 |
| Sát thương cháy mỗi nhịp | 600 |
| Nhịp cháy | 0.5 giây |
| Thời gian cháy | 2 giây |

#### Storm Eye

![Storm Eye](img/companion/storm_eye.png)

Kiểu **Lightning** — sát thương lan.

- Phóng tia sét vào enemy gần hero nhất rồi nối tiếp sang các enemy khác. **[GỐC]**
- Mỗi enemy trong chuỗi nhận trọn sát thương khi tia chạm tới. **[GỐC]**
- Tia được giữ một lúc, mỗi nhịp cả chuỗi nhận trọn sát thương lần nữa. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 2 giây |
| Hồi chiêu | 5 giây |
| Thời gian giữ tia | 2 giây |
| Nhịp sát thương | 0.2 giây |
| Số mục tiêu | 3 |
| Sát thương gốc | 135 |
| Sát thương lv 1 | 141.75 |
| Sát thương lv 100 | 810 |

#### Frost Claw

![Frost Claw](img/companion/frost_claw.png)

Kiểu **MultiSlower** — khống chế.

- Bắn cầu băng vào enemy gần hero nhất: gây sát thương và làm chậm. **[GỐC]**
- Từ điểm trúng, cầu băng tách thành các viên nhỏ bay tới những enemy gần nhất chưa bị trúng. **[GỐC]**
- Mỗi viên tách cũng gây trọn sát thương và làm chậm. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 9.9 giây |
| Tốc độ đạn | 12 |
| Số viên tách | 2 |
| Làm chậm | 50% |
| Thời gian chậm | 2 giây |
| Sát thương gốc | 1,000 |
| Sát thương lv 1 | 1,050 |
| Sát thương lv 100 | 6,000 |

### 4.4 Epic

#### Arcane Paw

![Arcane Paw](img/companion/arcane_paw.png)

Kiểu **Beam** — sát thương đơn.

- Bắn tia phép vào enemy gần hero nhất. **[GỐC]**
- Tia chạm mục tiêu gây trọn sát thương. **[GỐC]**
- Tia được giữ một lúc, mỗi nhịp gây trọn sát thương thêm lần nữa. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 5.9 giây |
| Thời gian giữ tia | 1.5 giây |
| Nhịp sát thương | 0.25 giây |
| Sát thương gốc | 4,500 |
| Sát thương lv 1 | 4,725 |
| Sát thương lv 100 | 27,000 |

#### Vital Root

![Vital Root](img/companion/vital_root.png)

Kiểu **Guardian** — phòng thủ.

- Nhắm vào hero, không cần có enemy trên sân. **[GỐC]**
- Khi kích hoạt hồi máu hero một lần. **[GỐC]**
- Cộng thêm tỉ lệ chặn đòn cho hero trong thời gian hiệu lực. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 5 giây |
| Hồi chiêu | 4 giây |
| Chặn đòn cộng thêm | 5% |
| Thời gian hiệu lực | 5 giây |
| Hồi máu gốc | 18,000 |
| Hồi máu lv 1 | 18,900 |
| Hồi máu lv 100 | 108,000 |

#### Glacier Fist

![Glacier Fist](img/companion/glacier_fist.png)

Kiểu **AoeSlower** — khống chế vùng.

- Ném cầu tuyết theo đường cong vào chỗ enemy đứng đông nhất. **[GỐC]**
- Cầu tuyết nổ gây sát thương mọi enemy trong bán kính nổ. **[GỐC]**
- Mọi enemy trúng nổ đều bị làm chậm. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 4 giây |
| Hồi chiêu | 3 giây |
| Bán kính nổ | 2 |
| Tốc độ cầu tuyết | 4.25 |
| Làm chậm | 50% |
| Thời gian chậm | 2 giây |
| Sát thương gốc | 9,000 |
| Sát thương lv 1 | 9,450 |
| Sát thương lv 100 | 54,000 |

### 4.5 Legendary

#### Echo Wing

![Echo Wing](img/companion/echo_wing.png)

Kiểu **Blaster** — sát thương vùng.

- Nhắm enemy gần hero nhất trong tầm. **[GỐC]**
- Phát liên tiếp nhiều sóng âm hình nón, hướng nón bám theo mục tiêu. **[GỐC]**
- Mỗi sóng gây trọn sát thương mọi enemy nằm trong nón. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 2 giây |
| Hồi chiêu | 5 giây |
| Tầm | 6 |
| Góc nón | 55° |
| Số sóng | 4 |
| Thời gian phát | 1.75 giây |
| Sát thương gốc | 52,500 |
| Sát thương lv 1 | 55,125 |
| Sát thương lv 100 | 315,000 |

#### Phantom Gaze

![Phantom Gaze](img/companion/phantom_gaze.png)

Kiểu **Siphon** — sát thương + hồi máu.

- Bắn đạn hồn ma bay lượn sóng tới enemy xa hero nhất, gây sát thương. **[GỐC]**
- Sau khi trúng, đạn bay ngược về hero và hồi máu. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 0.1 giây |
| Hồi chiêu | 5.9 giây |
| Tốc độ đạn | 4.5 |
| Sát thương gốc | 270,000 |
| Sát thương lv 1 | 283,500 |
| Sát thương lv 100 | 1,620,000 |
| Hồi máu gốc | 202,500 |
| Hồi máu lv 1 | 212,625 |
| Hồi máu lv 100 | 1,215,000 |

#### Star Feather

![Star Feather](img/companion/star_feather.png)

Kiểu **Meteor** — sát thương vùng.

- Thả lần lượt nhiều thiên thạch từ trên trời xuống. **[GỐC]**
- Thiên thạch chia lần lượt cho các enemy, rơi lệch ngẫu nhiên quanh mục tiêu. **[GỐC]**
- Mỗi viên nổ như bom, gây sát thương mọi enemy trong bán kính nổ. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 4 giây |
| Hồi chiêu | 6 giây |
| Số thiên thạch | 6 |
| Giãn cách mỗi viên | 0.7 giây |
| Độ lệch tối đa | 1 |
| Bán kính nổ | 2 |
| Sát thương gốc | 60,000 |
| Sát thương lv 1 | 63,000 |
| Sát thương lv 100 | 360,000 |

### 4.6 Mythic

#### Halo Hare

![Halo Hare](img/companion/halo_hare.png)

Kiểu **MirrorClone** — triệu hồi.

- Bắn đạn về hero; đạn tới nơi thì tạo một bản sao của hero ở ô trống gần đó. **[GỐC]**
- Bản sao đánh như hero, máu bằng một phần máu tối đa của hero. **[GỐC]**
- Bản sao tự biến mất khi hết thời gian tồn tại. **[GỐC]**
- Hero chết là thua, kể cả khi bản sao còn sống. **[ĐỀ XUẤT]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 3 giây |
| Hồi chiêu | 6 giây |
| Thời gian tồn tại | 6 giây |
| Máu bản sao gốc | 30% |
| Máu bản sao lv 1 | 30.5% |
| Máu bản sao lv 100 | 80% |

#### Radiant Talon

![Radiant Talon](img/companion/radiant_talon.png)

Kiểu **HealNova** — hồi máu + sát thương vùng.

- Bắn đạn về hero; đạn tới nơi thì hồi máu hero. **[GỐC]**
- Ngay sau đó một vòng sáng nở ra từ hero. **[GỐC]**
- Mỗi enemy bị vòng sáng quét qua nhận sát thương đúng một lần. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Pha kích hoạt | 1 giây |
| Hồi chiêu | 3 giây |
| Hồi máu gốc | 607,500 |
| Hồi máu lv 1 | 637,875 |
| Hồi máu lv 100 | 3,645,000 |
| Sát thương vòng sáng gốc | 270,000 |
| Sát thương vòng sáng lv 1 | 283,500 |
| Sát thương vòng sáng lv 100 | 1,620,000 |
| Bán kính đầu | 0.5 |
| Bán kính cuối | 5 |
| Thời gian nở | 1.5 giây |

#### Celestial Mind

![Celestial Mind](img/companion/celestial_mind.png)

Kiểu **Immortality** — phòng thủ.

- Bắn đạn về hero; đạn tới nơi thì hero bất tử. **[GỐC]**
- Khi bất tử, máu hero không tụt xuống dưới 1. **[GỐC]**
- Thời gian bất tử tăng theo level, không nhân hệ số sát thương pet. **[GỐC]**
- Pha kích hoạt của pet này dài đúng bằng thời gian bất tử. **[GỐC]**

| Thông số | Giá trị |
|---|---|
| Chờ đầu trận | 1 giây |
| Hồi chiêu | 12 giây |
| Bất tử gốc | 2 giây |
| Bất tử lv 1 | 2.02 giây |
| Bất tử lv 100 | 4 giây |

## 5. Công thức chung

### 5.1 Sát thương và hồi máu

- Sát thương = (Sát thương gốc + Tăng mỗi cấp × level) × (1 + CompanionDamage% / 100). **[GỐC]**
- Hồi máu dùng đúng công thức đó, cùng hệ số CompanionDamage%. **[GỐC]**
- Tăng mỗi cấp = 5% chỉ số gốc, đúng với cả 18 pet. **[GỐC]**
- Level dùng thẳng, bắt đầu từ 1: lv 1 = 1.05 × gốc, lv 100 = 6 × gốc. **[GỐC]**
- Sát thương của pet không chí mạng. **[GỐC]**
- Hồi máu không vượt quá máu tối đa của hero. **[GỐC]**
- Số hiện trong popup thông tin pet = Gốc + Tăng mỗi cấp × level, không nhân CompanionDamage%. **[GỐC]**

### 5.2 Chỉ số phụ của đồ ảnh hưởng tới pet

| Chỉ số phụ | Tác dụng |
|---|---|
| CompanionDamage | Cộng % vào hệ số sát thương và hồi máu của mọi pet |
| CompanionCooldown | Giảm % hồi chiêu của mọi pet |

- Cả hai cộng dồn thẳng theo %, không có trần. **[GỐC]**
- Mastery không cộng hai chỉ số này. **[GỐC]**

### 5.3 Hồi chiêu

- Một lượt = pha kích hoạt + hồi chiêu. **[GỐC]**
- Hồi chiêu thực = Hồi chiêu × (1 − CompanionCooldown% / 100). **[GỐC]**
- Giảm hồi chiêu không áp cho pha kích hoạt và thời gian chờ đầu trận. **[GỐC]**
- Pet có "Chờ đầu trận" phải chờ hết khoảng đó mới tung kỹ năng lần đầu; pet còn lại tung ngay khi vào trận. **[GỐC]**
- Sau khi kích hoạt, pet vung đòn 0.3 giây rồi kỹ năng mới bay ra. **[ĐỀ XUẤT]**

### 5.4 Chọn mục tiêu

| Cách chọn | Pet dùng |
|---|---|
| Enemy gần hero nhất | Ember Fist, Spark Mouse, Snow Fang, Storm Eye, Frost Claw, Arcane Paw, Echo Wing |
| Enemy xa hero nhất | Phantom Gaze |
| Chỗ enemy đứng đông nhất | Flame Wing, Blaze Tail, Glacier Fist |
| Lần lượt từng enemy | Star Feather |
| Hero | Magic Wool, Life Horn, Vital Root, Halo Hare, Radiant Talon, Celestial Mind |

- Bảng trên đúng theo game gốc. **[GỐC]**
- Storm Eye nối chuỗi sang enemy theo thứ tự trong danh sách enemy của trận. **[GỐC]**
- Project đang nối sang enemy gần nhất thay cho thứ tự danh sách. **[ĐỀ XUẤT]**

## 6. Own Effect — chỉ số cộng cho hero

- Mỗi pet đã sở hữu cộng Attack và Health phẳng cho hero. **[GỐC]**
- Tính mọi pet đã sở hữu, không cần trang bị. **[GỐC]**
- Attack cộng = Attack gốc + Tăng mỗi cấp × level; Health tương tự. **[GỐC]**
- Tăng mỗi cấp = 5% chỉ số gốc: lv 1 = 1.05 × gốc, lv 100 = 6 × gốc. **[GỐC]**
- Cộng cùng lúc với chỉ số nền và chỉ số đồ, trước khi nhân % chỉ số phụ. **[GỐC]**
- Own Effect cũng được tính vào Power. **[GỐC]**

| Pet | Attack gốc | Health gốc | Attack lv 100 | Health lv 100 |
|---|---|---|---|---|
| Ember Fist | 10 | 30 | 60 | 180 |
| Magic Wool | 5 | 50 | 30 | 300 |
| Spark Mouse | 15 | 20 | 90 | 120 |
| Flame Wing | 45 | 60 | 270 | 360 |
| Life Horn | 15 | 150 | 90 | 900 |
| Snow Fang | 30 | 90 | 180 | 540 |
| Blaze Tail | 450 | 600 | 2,700 | 3,600 |
| Storm Eye | 300 | 900 | 1,800 | 5,400 |
| Frost Claw | 150 | 1,500 | 900 | 9,000 |
| Arcane Paw | 4,500 | 6,000 | 27,000 | 36,000 |
| Vital Root | 1,500 | 15,000 | 9,000 | 90,000 |
| Glacier Fist | 3,000 | 9,000 | 18,000 | 54,000 |
| Echo Wing | 45,000 | 60,000 | 270,000 | 360,000 |
| Phantom Gaze | 30,000 | 90,000 | 180,000 | 540,000 |
| Star Feather | 15,000 | 150,000 | 90,000 | 900,000 |
| Halo Hare | 90,000 | 270,000 | 540,000 | 1,620,000 |
| Radiant Talon | 45,000 | 450,000 | 270,000 | 2,700,000 |
| Celestial Mind | 135,000 | 180,000 | 810,000 | 1,080,000 |

Sở hữu đủ 18 pet ở lv 100 cộng tổng **2,220,120 Attack** và **7,400,400 Health**.

## 7. Pet trong trận

### 7.1 Quy tắc chung

- Pet ra trận là các pet đang trang bị, tối đa 3 con. **[GỐC]**
- Pet đi theo hero, giữ khoảng cách vài ô. **[GỐC]**
- Enemy không nhắm pet, pet không mất máu, không chết, không bị khống chế. **[CHỐT]**
- Tầm tung kỹ năng của mọi pet là 20. **[ĐỀ XUẤT]**
- Trong lúc hồi chiêu pet chỉ chạy theo hero. **[CHỐT]**

### 7.2 Tự động và thủ công

- Có nút bật/tắt tự động ở lobby, lưu vào save. **[CHỐT]**
- Tự động: pet tung kỹ năng ngay khi hồi chiêu xong. **[CHỐT]**
- Thủ công: hồi chiêu xong pet chờ, người chơi bấm vào ô pet để tung. **[CHỐT]**
- Ô pet hiện thanh tiến độ hồi chiêu. **[CHỐT]**

### 7.3 Theo chế độ chơi

| Chế độ | Pet ra trận | Own Effect |
|---|---|---|
| Campaign | Có | Có |
| Dungeon | Có | Có |
| PvP | Có | Có |
| Boss Rush | Không | Có |

- PvP: đối thủ ra trận với 3 pet của họ, Own Effect tính theo pet đối thủ sở hữu. **[GỐC]**
- Boss Rush: không pet nào ra trận, kể cả của 7 người hỗ trợ. **[GỐC]**

## 8. Summon

### 8.1 Nguyên liệu

- Summon tiêu **Bone**, không dùng Gem. **[GỐC]**
- Bone nhận từ dungeon Zombie Outbreak. **[GỐC]**
- Bone cũng có trong thưởng cuối mùa Boss Rush. **[GỐC]**

### 8.2 Ba cách summon

| Cách | Giá | Số lượt gốc |
|---|---|---|
| Xem quảng cáo | 1 quảng cáo | 11 |
| Nút Bone nhỏ | 100 Bone | 15 |
| Nút Bone lớn | 200 Bone | 35 |

- Số lượt thật = số lượt gốc + Summon Capacity. **[GỐC]**
- Nút Bone lớn có hệ số nhân M: giá = M × 200 Bone, số lượt gốc = M × 35. **[GỐC]**
- Các bậc M mà nút đổi hệ số cho chọn: chưa đọc được. **[CHỜ SỐ LIỆU]**
- Quảng cáo giới hạn 3 lần mỗi ngày. **[GỐC]**
- Quảng cáo có thêm điểm cộng AdBonus lưu trong save, tổng không quá 35 + Summon Capacity. **[GỐC]**
- Cách tăng AdBonus: chưa rõ. **[CHỜ SỐ LIỆU]**

### 8.3 Summon Capacity

- Summon Capacity là một nhánh của Mastery, cộng thêm lượt cho cả ba cách summon. **[GỐC]**
- Chưa mở nhánh thì bằng 0. **[GỐC]**

| Cấp Mastery | Summon Capacity | Phí Gem |
|---|---|---|
| 1 | 1 | 60 |
| 2 | 2 | 120 |
| 3 | 3 | 160 |
| 4 | 4 | 200 |
| 5 | 5 | 250 |

Ví dụ với Summon Capacity = 1: quảng cáo ra 12 lượt, 100 Bone ra 16 lượt, 200 Bone ra 36 lượt.

### 8.4 Mỗi lượt summon

1. Roll độ hiếm theo tỉ lệ của Summon Level hiện tại (mục 9).
2. Chọn ngẫu nhiên 1 trong 3 pet của độ hiếm đó.
3. Chưa có pet đó → sở hữu pet ở lv 1 với 0 thẻ, gắn nhãn New.
4. Đã có → cộng 1 thẻ cho pet đó.
5. Tổng số lượt đã summon tăng 1.

- Năm bước trên đúng theo game gốc. **[GỐC]**
- Lượt thứ 53 của tài khoản luôn ra Uncommon, không roll. **[GỐC]**
- Summon không tự nâng cấp pet. **[GỐC]**

## 9. Summon Level và tỉ lệ độ hiếm

- Summon Level tính theo tổng số lượt đã summon, không theo Bone đã tiêu. **[GỐC]**
- Có 100 level. **[GỐC]**
- Từ level n lên n + 1 cần 45 + 5 × n lượt. **[GỐC]**
- Mỗi lượt roll theo tỉ lệ của level tại thời điểm đó, nên giữa một gói summon có thể lên level. **[GỐC]**

Tỉ lệ (%) ở các mốc tiêu biểu; bảng đủ 100 level nằm ở `Scripts/Companions/CompanionSummonLevelConfig.cs`: **[GỐC]**

| Summon Level | Tổng lượt cần | Common | Uncommon | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|---|
| 1 | 0 | 100 | 0 | 0 | 0 | 0 | 0 |
| 2 | 50 | 98 | 2 | 0 | 0 | 0 | 0 |
| 7 | 375 | 86.8 | 13 | 0.2 | 0 | 0 | 0 |
| 15 | 1,155 | 45 | 50 | 5 | 0 | 0 | 0 |
| 25 | 2,580 | 17.5 | 38.49 | 44 | 0.01 | 0 | 0 |
| 50 | 8,330 | 17.5 | 16.5 | 21.99 | 44 | 0.01 | 0 |
| 75 | 17,205 | 17.5 | 16.5 | 16.5 | 16.5 | 32.99 | 0.01 |
| 100 | 29,205 | 17.5 | 16.5 | 16.5 | 16.5 | 16.5 | 16.5 |

Level đầu tiên có thể ra từng độ hiếm:

| Độ hiếm | Từ Summon Level |
|---|---|
| Uncommon | 2 |
| Rare | 7 |
| Epic | 25 |
| Legendary | 50 |
| Mythic | 75 |

## 10. Thẻ và nâng cấp

- Pet lên cấp bằng thẻ trùng, không có kinh nghiệm. **[GỐC]**
- Level tối đa 100. **[GỐC]**
- Nâng cấp thủ công: đủ thẻ thì người chơi bấm Upgrade trong popup thông tin pet. **[GỐC]**
- Nâng cấp trừ đúng số thẻ cần, thẻ dư giữ lại. **[GỐC]**
- Có nút nâng tất cả: nâng mọi pet tới khi hết thẻ. **[ĐỀ XUẤT]**

Số thẻ cần để lên cấp kế tiếp: **[GỐC]**

| Level hiện tại | Thẻ cần |
|---|---|
| 1 | 2 |
| 2 | 3 |
| 3 | 3 |
| 4 | 3 |
| 5 | 4 |
| 6 | 4 |
| 7 | 5 |
| 8 | 5 |
| 9 | 6 |
| 10 | 7 |
| 11 | 8 |
| 12 | 10 |
| 13 | 11 |
| 14 | 13 |
| 15 – 99 | 16 |

Từ lv 1 lên lv 100 cần tổng **1,444 thẻ** cho một pet.

## 11. Trang bị

- Trang bị tối đa 3 pet. **[GỐC]**
- Chỉ trang bị được pet đã sở hữu. **[GỐC]**
- Ô pet có nút Add khi đã sở hữu, chưa trang bị và còn chỗ; có nút Remove khi đang trang bị. **[CHỐT]**
- Quick Equip: xếp độ hiếm giảm dần, cùng độ hiếm thì level giảm dần, lấy 3 con đầu. **[GỐC]**
- Đổi pet trang bị có hiệu lực ngay trong trận đang chạy, không phải chờ màn sau. **[CHỐT]**

## 12. Popup thông tin pet

Bấm vào ô pet → popup: **[GỐC]**

- Avatar, tên, độ hiếm, level.
- Mô tả kỹ năng, các con số điền theo level hiện tại.
- Own Effect: Damage và Health pet đang cộng cho hero.
- Số thẻ đang có / số thẻ cần, nút Upgrade.

Quy tắc hiển thị số:

- Cắt bỏ phần lẻ (47.25 hiện 47). **[GỐC]**
- Từ 1000 trở lên rút gọn k / M / B / T, giữ 3 chữ số có nghĩa. **[GỐC]**
- Số trong mô tả tô màu `#E5F36B`. **[GỐC]**

## 13. Chưa chốt

### Chờ bạn quyết định

1. Nút quảng cáo: tạm giới hạn 3 lần/ngày ngay, hay chờ có hệ ads thật rồi làm một thể.
2. Hero chết khi bản sao của Halo Hare còn sống: thua luôn (đang làm) hay để bản sao đánh tiếp.
3. Storm Eye nối chuỗi: giữ "enemy gần nhất" (đang làm) hay đổi về thứ tự danh sách như gốc.
4. Tầm tung kỹ năng chung 20 cho mọi pet (mục 7.1).
5. Màu chữ và khung theo độ hiếm trong popup thông tin và popup kết quả summon.

### Chờ số liệu từ game gốc

1. Các bậc hệ số nhân M của nút Bone lớn.
2. Cách tăng AdBonus của summon quảng cáo.
3. Bảng màu độ hiếm của pet.
4. Pet có bị enemy nhắm hay mất máu trong game gốc không (mục 7.1).
5. Game gốc có chế độ thủ công cho pet không, hay luôn tự động (mục 7.2).

## 14. Tình trạng code

### Đã làm

| Hạng mục | Ở đâu |
|---|---|
| Data 18 pet | `Resources/Scriptable Objects/Companions/`, `Scripts/Companions/CompanionData.cs` |
| Kỹ năng đủ 16 kiểu | `Scripts/Unit/PetUnit.cs`, `Scripts/Unit/Pet/PetCompanion*.cs` |
| Hiệu ứng dùng chung: chậm, cháy, chặn đòn, bất tử, tia nối, vòng nở | `Scripts/Unit/Pet/Effects/` |
| Summon, roll độ hiếm, lượt 53 ép Uncommon | `Scripts/Companions/CompanionService.cs` |
| Bảng Summon Level 100 hàng | `Scripts/Companions/CompanionSummonLevelConfig.cs` |
| Bảng thẻ nâng cấp, nâng thủ công, nâng tất cả | `Scripts/Companions/CompanionUpgradeConfig.cs` |
| Giá và số lượt summon, Summon Capacity từ Mastery | `Scripts/Companions/CompanionSummonConfig.cs` |
| Save: pet sở hữu, pet trang bị, tổng lượt summon, cờ tự động | `Scripts/Companions/UserCompanionData.cs` |
| Own Effect cộng vào hero và Power | `CompanionService.BuildOwnEffectModifiers` |
| Giảm hồi chiêu và hệ số sát thương pet từ chỉ số phụ | `PetUnit`, `Stats` |
| Tab Pet, popup thông tin, popup kết quả summon | `Scripts/UI/Companions/` |
| Prefab riêng từng pet, tên file trùng tên asset | `Resources/Prefabs/Units/Companions/` |

Kỹ năng pet mới compile sạch, **chưa chạy thử đủ 18 con trong Unity**.

### Chưa làm

| Hạng mục | Ghi chú |
|---|---|
| Nút quảng cáo | Đang summon miễn phí vô hạn, chưa xem ads, chưa giới hạn 3 lần/ngày |
| Khoá tab Pet theo level | Tab đang mở luôn; mốc level 5 mới áp cho 3 ô pet ở lobby |
| Hệ số nhân nút Bone lớn | Đang cố định M = 1 |
| Nhãn New trong popup kết quả summon | Summon đã ghi cờ, popup chưa có ô hiện |
| Màu và khung độ hiếm | Chờ bảng màu |
| Sự kiện sau mỗi lượt summon | Gốc bắn sự kiện kèm độ hiếm, chưa port |
| Hiệu ứng hình ảnh gốc | FX gốc nằm trong AssetBundle không xuất được, đang dùng FX thay thế |
| Giữ tỉ lệ máu khi Own Effect đổi | Gốc giữ nguyên % máu, project chỉ chặn trần |
| Popup tỉ lệ summon theo level | Nút info ở tab Pet chưa nối |
