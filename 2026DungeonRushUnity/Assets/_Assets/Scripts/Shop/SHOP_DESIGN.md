# Store (Shop) — Bản thiết kế (GD)

Cập nhật: 2026-10-10. Client đã code theo bản này (mục 13 ghi phần còn lại). Các mục **[ĐỀ XUẤT]** đang chạy đúng
như đề xuất, đổi được khi bạn quyết khác.
Số liệu reverse từ game gốc nằm ở `DecodedData/SHOP_MODEL.md`; file này là thiết kế của project mình.

Ký hiệu độ chắc chắn của từng mục:

| Ký hiệu | Nghĩa |
|---|---|
| **[GỐC]** | Xác nhận từ game gốc (đọc từ APK bản 41) |
| **[ĐỀ XUẤT]** | Đề xuất, chờ duyệt |
| **[CHỜ SỐ LIỆU]** | Cần ghi thêm từ game gốc mới quyết định được |

---

## 1. Tổng quan

Store là tab đầu tiên ở thanh menu lobby. Nó làm ba việc:

- **Bán gem bằng tiền thật** (6 gói gem, Daily Deals, Lifetime Boost, gói rương).
- **Đổi gem lấy tiến trình**: rương pet/áo choàng, boost kinh nghiệm, dụng cụ đào mỏ.
- **Cho người chơi lý do quay lại mỗi ngày**: 3 Daily Deal đổi theo ngày.

Gem là tiền chính của Store. Ngoài Store, gem còn tiêu ở Mastery và phí tạo clan; nguồn gem miễn phí là thưởng lên level.

## 2. Lối vào và mở khoá

- Tab Store mở được ngay từ đầu, không cần level. **[ĐỀ XUẤT]** (gốc có cờ `IsStoreTabUnlocked` gắn với hướng dẫn tân thủ; project chưa có hướng dẫn nên để mở sẵn)
- Riêng **mục rương** chỉ hiện khi người chơi đã mở tính năng Áo choàng: PlayerLevel ≥ 15, hoặc đã mua một gói rương (mục 6). **[GỐC]**

## 3. Bố cục trang Store **[GỐC]**

Một danh sách cuộn dọc, từ trên xuống:

| # | Mục | Trả bằng | Ghi chú |
|---|---|---|---|
| 1 | Lifetime Boost | Tiền thật | Biến mất sau khi mua |
| 2 | Chests | Gem | Ẩn khi chưa mở Áo choàng |
| 3 | Daily Deals (3 ô) | Tiền thật | Đổi mỗi ngày |
| 4 | Boosters (2 ô) | Gem | Boost kinh nghiệm có thời hạn |
| 5 | Resources (2 ô) | Gem | Dụng cụ đào mỏ |
| 6 | Gems (6 gói) | Tiền thật | |

Trong prefab gốc còn 5 mục **đang tắt** ở bản 41: No Ads, Starter Pack, Epic Pack, Dungeon Pack, Resource Pack. Thưởng của chúng
vẫn có trong code (mục 10.2) nên bật lại được khi cần.

## 4. Lifetime Boost **[GỐC]**

- Mua một lần bằng tiền thật: **kinh nghiệm ×2 vĩnh viễn**.
- Mua xong thì ô này biến mất khỏi Store.
- Nhân dồn với các boost có thời hạn (mục 8).

## 5. Rương

Rương là nguồn **pet và áo choàng** thứ hai, bên cạnh summon bằng Bone (pet) và Cloak (áo choàng). Khác với summon,
rương **ưu tiên ra mẫu chưa sở hữu**, nên đây là cách nhanh nhất để sưu tầm đủ bộ.

### 5.1 Bốn loại rương **[GỐC]**

| Rương | Giá | Số món / lần mở | Rarity cao nhất | Pity | Điều kiện bán |
|---|---:|---:|---|---:|---|
| Standard Chest | 40 gem | 2 | Rare | không | — |
| Epic Chest | 80 gem | 6 | Epic | 20 lần | — |
| Legendary Chest | 120 gem | 6 | Legendary | 20 lần | — |
| Mythic Vault | 200 gem | 6 | Mythic | 20 lần | Đã sưu tầm đủ rarity Epic |

Tỉ lệ rarity của **mỗi món** (số hiển thị trong popup thông tin rương):

| Rương | Common | Uncommon | Rare | Epic | Legendary | Mythic |
|---|---:|---:|---:|---:|---:|---:|
| Standard | 87% | 9% | 4% | | | |
| Epic | 75% | 20% | 4% | 1% | | |
| Legendary | 63.13% | 30.3% | 5.05% | 1.01% | 0.51% | |
| Mythic | 45.92% | 40% | 10% | 3% | 0.75% | 0.33% |

(Trọng số gốc của rương Legendary là 62.5 / 30 / 5 / 1 / 0.5, tổng 99, nên số hiển thị lệch nhẹ như trên.)

### 5.2 Một lần mở **[GỐC]**

1. Roll rarity cho từng món theo bảng trên.
2. Kiểm tra pity (mục 5.3).
3. Với mỗi món, chọn một pet hoặc một mẫu áo choàng của rarity đó (mục 5.4).
4. Hiện màn mở rương.

### 5.3 Pity **[GỐC]**

- Mỗi rương có pity đếm số lần mở **liên tiếp không ra món nào thuộc rarity cao nhất** của rương đó.
- Lần mở thứ 20 mà vẫn chưa ra: **món cuối cùng** của lần mở đó bị ép thành rarity cao nhất.
- Ra được món rarity cao nhất (tự nhiên hoặc do ép) thì bộ đếm về 0.
- Thẻ rương và popup thông tin luôn hiện "Get {rarity} in {n} opens", với n = số lần còn lại.
- Standard Chest không có pity.

### 5.4 Chọn pet hay áo choàng **[GỐC]**

- Mỗi rarity có 3 pet và 2 mẫu áo choàng, tổng 5 mẫu.
- Còn mẫu **chưa sở hữu** trong rarity đó: chọn ngẫu nhiên đều trong các mẫu chưa sở hữu (pet và áo choàng gộp chung).
- Đã sở hữu hết: chọn ngẫu nhiên đều trong cả 5 mẫu.
- Ra **pet đã có**: +1 thẻ của pet đó (dùng để nâng cấp pet).
- Ra **áo choàng**: luôn là một chiếc mới với chỉ số phụ roll riêng, kể cả khi đã có mẫu đó (áo trùng dùng làm nguyên liệu nâng cấp).
- Món là mẫu lần đầu sở hữu thì gắn nhãn **NEW**.

### 5.5 "Sưu tầm đủ rarity" **[GỐC]**

Đủ một rarity = sở hữu cả 3 pet **và** có ít nhất một chiếc của cả 2 mẫu áo choàng thuộc rarity đó.
Mythic Vault chỉ xuất hiện trong Store khi đã đủ rarity Epic.

### 5.6 Giá trị kỳ vọng

Tính từ tỉ lệ và pity ở trên (không phải số của game gốc):

| Rương | Gem / món | Xác suất một lần mở ra rarity cao nhất | Trung bình để ra một món rarity cao nhất | Tệ nhất |
|---|---:|---:|---|---|
| Standard | 20 | 7.8% | ~12.8 lần mở ≈ 510 gem | không giới hạn |
| Epic | 13.3 | 5.9% | ~12 lần mở ≈ 960 gem | 20 lần = 1600 gem |
| Legendary | 20 | 3.0% | ~15.2 lần mở ≈ 1830 gem | 20 lần = 2400 gem |
| Mythic | 33.3 | 2.0% | ~16.7 lần mở ≈ 3330 gem | 20 lần = 4000 gem |

Epic Chest rẻ nhất tính theo món. Từ Legendary trở lên, phần lớn món rarity cao nhất đến từ pity chứ không phải may mắn.

### 5.7 Giao diện **[GỐC]**

- **Thẻ rương**: Standard và Epic là thẻ dọc xếp lưới 2 cột; Legendary và Mythic là thẻ ngang lớn nằm trên lưới, kèm ảnh 5 pet/áo
  tiêu biểu. Thứ tự hai thẻ ngang: rarity cao ở trên. **[ĐỀ XUẤT]**
- Mỗi thẻ có: ảnh, tên, "Contains {các rarity}", dòng pity, nút mua ghi giá gem, nút **i**.
- **Popup thông tin** (nút i): tên, ảnh, "Contains … cloak and companion", dòng pity, bảng tỉ lệ từng rarity.
- **Màn mở rương**: chạm để mở → mỗi lần chạm hiện một món (ô lớn, có bộ đếm số món còn lại) → bảng tổng kết → đóng.
  Nút **Open all** nhảy thẳng tới bảng tổng kết.
- Thiếu gem: báo "Not enough gems!", không mở gì.

## 6. Gói rương (tiền thật)

| Gói | Gem kèm theo | Rương mở sẵn | Khi nào chào bán |
|---|---:|---|---|
| Legendary Chest Bundle | 2500 | 20 Epic + 10 Legendary | Mặc định |
| Mythic Chest Bundle | 3000 | 20 Legendary + 10 Mythic | Khi đã sưu tầm đủ rarity Epic |

- Mua xong: cộng gem, mở toàn bộ 30 rương (180 món) và hiện bảng kết quả dạng lưới cuộn. **[GỐC]**
- Mỗi rương trong gói vẫn tính pity như mua lẻ, nên 20 rương Epic chắc chắn ra ít nhất một món Epic.
- Người chơi **chưa tới level 15** mua gói thì được mở sớm tính năng Áo choàng (popup ghi "Unlocks the Cloak feature!"). **[GỐC]**
- Popup chào bán hiện: tên gói theo rarity, 5 ảnh tiêu biểu, các món trong gói (bấm vào rương để xem tỉ lệ), giá, nhãn -80%.
- Gốc mở popup này từ một nút ở lobby. Project **chưa có nút đó** (mục 13).
- Dữ liệu gốc còn 2 gói nhỏ (500 gem + 2 Legendary; 400 gem + 2 Mythic) không thấy chỗ bán ở bản 41.

## 7. Daily Deals

### 7.1 Quy tắc **[GỐC]**

- Mỗi ngày (đổi lúc 00:00 UTC) có **3 deal**. Ba deal luôn **khác cỡ và khác loại** nhau.
- Có 4 cỡ (Tiny, Small, Medium, Large) × 5 loại (Dungeon, Gem, Cloak, Mining, Companion) = 20 deal có thể có.
- Bộ 3 deal sinh từ ngày hiện tại, nên mọi người chơi cùng ngày thấy cùng bộ deal.
- Deal đã mua biến mất. **Mua hết cả 3 thì ra ngay 3 deal mới**, không lặp lại deal đã mua trong ngày.
- Sang ngày mới thì danh sách "đã mua" xoá sạch.
- Thứ tự 3 ô: cỡ nhỏ ở trên. **[ĐỀ XUẤT]**

### 7.2 Thưởng theo cỡ **[GỐC]**

| Loại deal | Thưởng chính | Tiny | Small | Medium | Large |
|---|---|---:|---:|---:|---:|
| Dungeon | Key Dragon Boss | 1 | 2 | 3 | 5 |
| | Key Zombie Horde | 1 | 2 | 3 | 5 |
| Gem | Gem | 200 | 500 | 2000 | 5000 |
| Cloak | Cloak (tiền summon áo choàng) | 20 | 50 | 200 | 500 |
| Mining | Pickaxe | 10 | 20 | 100 | 200 |
| | Golden Pickaxe | 1 | 2 | 10 | 20 |
| | Drill | 1 | 2 | 10 | 20 |
| Companion | Bone (tiền summon pet) | 400 | 1000 | 4000 | 10000 |

Mọi deal kèm thêm:

| Kèm thêm | Tiny | Small | Medium | Large | Áp dụng |
|---|---:|---:|---:|---:|---|
| Loot box | 100 | 300 | 1500 | 3000 | Mọi loại |
| Gem | 50 | 150 | 750 | 1500 | Mọi loại trừ deal Gem |

Key mua từ deal là key cộng thêm, không mất khi sang ngày.

## 8. Boosters **[GỐC]**

| Boost | Giá | Hiệu lực | Thời lượng |
|---|---:|---|---|
| x1.5 Exp Boost | 45 gem | Kinh nghiệm ×1.5 | 20 phút |
| x2 Exp Boost | 75 gem | Kinh nghiệm ×2 | 5 phút |

- Hai boost và Lifetime Boost **nhân dồn**: bật cả ba là ×6 (2 × 1.5 × 2).
- Đang chạy thì nút mua ẩn đi, thay bằng đồng hồ đếm ngược. Không mua chồng để cộng thêm giờ.
- Thời gian tính theo giờ thực, vẫn trôi khi thoát game.
- Boost áp vào **kinh nghiệm thắng màn campaign**. Kinh nghiệm từ thưởng Boss Rush chưa được nhân. **[ĐỀ XUẤT]**

## 9. Resources **[GỐC]**

| Món | Giá | Nhận |
|---|---:|---|
| Gold Pickaxe | 45 gem | 3 cái |
| Drill | 45 gem | 3 cái |

Đây là dụng cụ của hệ Đào mỏ. Project chưa có hệ Đào mỏ nên hiện mua được, cộng vào kho, nhưng chưa có chỗ dùng.

## 10. Bán bằng tiền thật

### 10.1 Gói gem

| Gói | Gem **[GỐC]** | Giá **[ĐỀ XUẤT]** | Gem / 1$ |
|---|---:|---:|---:|
| Gem 1 | 100 | $0.99 | 101 |
| Gem 2 | 255 | $2.49 | 102 |
| Gem 3 | 520 | $4.99 | 104 |
| Gem 4 | 1100 | $9.99 | 110 |
| Gem 5 | 2700 | $24.99 | 108 |
| Gem 6 | 5500 | $49.99 | 110 |

### 10.2 Các món khác

| Món | Thưởng **[GỐC]** | Giá **[ĐỀ XUẤT]** | Trạng thái |
|---|---|---:|---|
| Lifetime Boost | Kinh nghiệm ×2 vĩnh viễn | $4.99 | Đang bán |
| Daily Deal Tiny / Small / Medium / Large | Mục 7.2 | $0.99 / $2.49 / $9.99 / $24.99 | Đang bán |
| Legendary / Mythic Chest Bundle | Mục 6 | $19.99 / $29.99 | Có popup, chưa có nút mở |
| No Ads | Tắt quảng cáo | $4.99 | Tắt trong prefab gốc |
| Starter Pack | 500 gem + 300 loot box | $1.99 | Tắt trong prefab gốc |
| Epic Pack | 2400 gem + 1500 loot box + 750 bone | $9.99 | Tắt trong prefab gốc |
| Dungeon Pack | 4 key Dragon + 4 key Zombie | $1.99 | Tắt trong prefab gốc |
| Resource Pack | 1200 loot box + 750 bone + 400 gem | $4.99 | Tắt trong prefab gốc |

Giá tiền thật do cửa hàng ứng dụng trả về lúc chạy nên **không nằm trong APK**. Toàn bộ cột giá là đề xuất, suy từ lượng gem
(gói gem giữ khoảng 100–110 gem cho 1$; Daily Deal loại Gem cho gấp đôi mức đó).

### 10.3 Thanh toán

- Chưa gắn SDK thanh toán. Trong Unity Editor, bấm mua là thành công ngay để test. Trên máy thật, bấm mua sẽ báo
  "Store is not available" và không cộng gì.
- Khi gắn SDK: thưởng chỉ phát sau khi cửa hàng xác nhận thanh toán.
- Daily Deal ghi lại deal đang chờ thanh toán trước khi gọi cửa hàng, để nếu game tắt giữa chừng vẫn phát đúng thưởng. **[GỐC]**

## 11. Dữ liệu lưu

Lưu ở máy, khoá `key_user_shop`:

- Bộ đếm pity của từng rarity.
- Daily Deals: ngày hiện tại, 3 deal đang bán, các deal đã mua trong ngày, deal đang chờ thanh toán.
- Thời điểm hết hạn của hai boost.
- Các cờ mua một lần: Lifetime Boost, No Ads, đã mở sớm Áo choàng.

## 12. Chưa chốt

### Chờ bạn quyết định

| Việc | Hiện đang |
|---|---|
| Bảng giá tiền thật (mục 10) | Theo đề xuất |
| Có bật lại 5 pack đang tắt không | Tắt như gốc |
| Nút chào gói rương ở lobby đặt ở đâu | Chưa có nút |
| Boost có nhân kinh nghiệm từ Boss Rush không | Không |
| Giờ dùng cho boost và Daily Deals | Giờ của máy. Người chơi chỉnh đồng hồ có thể kéo dài boost hoặc đổi bộ deal; muốn chặn cần giờ server |
| Tab Store có khoá theo level không | Mở sẵn |

### Chờ số liệu từ game gốc

| Việc | Ghi chú |
|---|---|
| Giá thật của từng sản phẩm | Xem trong game gốc trên máy thật |
| Thứ tự 3 Daily Deal và 2 thẻ rương ngang | Gốc có sắp xếp nhưng chưa đọc được quy tắc |
| Rương quảng cáo ở lobby (Loot Box Offer / Gem Offer) | Có prefab popup, chưa đọc số thưởng và giới hạn mỗi ngày |
| Weapon Offer | Có prefab popup, chưa đọc cách chọn vũ khí và thời hạn |

## 13. Tình trạng code

### Đã làm (2026-10-10)

| Hạng mục | Ở đâu |
|---|---|
| Dữ liệu rương, gói rương | `Resources/Scriptable Objects/Shop/` (4 rương, 4 gói, `ChestConfig`) |
| Mở rương, pity, chọn pet/áo, điều kiện Mythic | `Scripts/Shop/ShopService.cs` |
| Boost kinh nghiệm, mua Drill / Gold Pickaxe | `ShopService.cs`; nhân exp ở `CampaignMode` |
| Daily Deals: sinh theo ngày, mua, đổi bộ mới | `Scripts/Shop/DailyDealService.cs` |
| Bảng thưởng gói tiền thật, giá đề xuất | `Scripts/Shop/StaticShopData.cs` |
| Lớp thanh toán nền (chưa có SDK) | `Scripts/Shop/PurchaseController.cs` |
| Save | `Scripts/Shop/UserShopData.cs` |
| Trang Store, thẻ rương, ô Daily Deal | `Scripts/UI/Shop/`, prefab `Resources/Prefabs/UI/StoreTabPage` |
| Popup thông tin rương, màn mở rương, bảng kết quả gói, popup chào gói | `UIChestInfoPopup`, `UIChestRevealPopup`, `UIChestBundleRevealPopup`, `UIChestBundleOfferPopup` |

Đổi số liệu rương hoặc dựng lại prefab: menu Unity **Tools → DungeonRush → Build Shop UI Prefabs**.

Đã chạy thử trong Unity: mở 400 rương Epic ra tỉ lệ 76 / 18.5 / 4.1 / 1.4% và không lần nào trượt Epic quá 19 lượt;
Daily Deal đổi bộ mới sau khi mua hết; ba boost nhân dồn; mua gói rương ra 180 món.

### Chưa làm

| Hạng mục | Ghi chú |
|---|---|
| SDK thanh toán | Viết lớp kế thừa `PurchaseController`, giống cách làm quảng cáo |
| Nút chào gói rương ở lobby | Popup đã có, gọi `UIChestBundleOfferPopup.Show(ShopService.GetFeaturedBundle())` |
| Rương quảng cáo ở lobby | Prefab `RewardedChestPopup` có sẵn, chưa dựng |
| Weapon Offer | Prefab `OfferPopup` có sẵn, chưa dựng |
| Hệ Đào mỏ | Drill / Pickaxe chưa có chỗ dùng |
| Battle Pass, Mining Offer, boost quảng cáo | Thuộc tính năng khác |
| Hiệu ứng hạt trên thẻ rương và popup gói | Gốc dùng plugin UIParticle, project không có nên đang tắt |
| Hiệu ứng mở rương | Món chưa bay từ rương ra ô như gốc |
