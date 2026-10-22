# Phối màu trang bị theo 10 rarity

Ngày ghi nhận: 2026-10-22.

## Nguồn và yêu cầu của user

Đã đọc `C:/Users/Z.One1/Downloads/Telegram Desktop/ART_HERO_EQUIPMENT.html` và xem trực tiếp các ảnh nhúng: bộ art vịt mới, vũ khí cận chiến, vũ khí tầm xa, nhẫn và dây chuyền.

User yêu cầu học cách phối màu của concept này: một rarity phối nhiều màu với nhau; rarity đỏ không có nghĩa toàn bộ món đồ chỉ dùng các sắc đỏ. Áp dụng nguyên tắc này khi tạo, sửa hoặc đánh giá art trang bị sau này.

Ảnh tham chiếu trong repo: [cận chiến](img/hero_equipment/01_weapon_melee.png), [tầm xa](img/hero_equipment/01_weapon_ranged.png), [nhẫn](img/hero_equipment/05_ring.png), [dây chuyền](img/hero_equipment/06_necklace.png), [art vịt mới](img/hero_equipment/new_art_done.png). Khi có khác biệt, đối chiếu lại file HTML user gửi.

## Bảng màu quan sát từ bộ item gốc

Đây là mô tả bằng mắt từ ảnh tham chiếu, không phải danh sách mã màu hay tỉ lệ diện tích cố định. Cột màu nền chỉ nói về ô UI; cột phối màu mô tả bản thân vật phẩm.

| Tier | Rarity | Nền ô UI | Phối màu trên vật phẩm quan sát được |
|---|---|---|---|
| 1 | Common | Xám | Nâu gỗ/da, be, trắng ngà/xương, xám sắt; từng món có thể thiên về một chất liệu khác nhau. |
| 2 | Uncommon | Be/nâu nhạt | Nâu gỗ và da sẫm đi với bạc/xám thép, ngà và chi tiết tối. |
| 3 | Rare | Xanh lá non | Thép bạc hơi xanh, xanh xám tối; cán nâu, dây quấn cam; nhẫn/dây chuyền có điểm nhấn cam hoặc xương trắng. |
| 4 | Epic | Xanh lơ | Thép xanh nhạt, xanh xám đậm, xanh lá/olive; trang sức dùng đá xanh lá trên khung kim loại lạnh. |
| 5 | Legendary | Vàng | Thép xanh/bạc, xanh xám tối, vàng kim và đỏ; đá đỏ nổi trên khung vàng hoặc kim loại lạnh. |
| 6 | Mythic | Tím | Tím đậm/tím nhạt, bạc ánh tím và vàng kim; đá tím đi với khung bạc hoặc vàng. |
| 7 | Artifact | Xanh lá đậm | Vũ khí phối xanh mint/xanh lá nhạt, xanh rừng và vàng kim; điểm trang trí gợi lá/rune. |
| 8 | Ancient | Xanh navy | Vũ khí phối cyan/xanh băng, trắng sáng, xanh lam và xanh đậm; mảng sáng tạo cảm giác tinh thể/băng. |
| 9 | Immortal | Đỏ | Tím than/gần đen làm thân và khung, đỏ/crimson ở đá hoặc lõi, tím sáng/lavender ở lưỡi và mép. Nhiều món có diện tích tím lớn hơn đỏ. |
| 10 | Divine | Cam | Vàng kim/cam vàng đi với trắng ngà và xanh ngọc nhạt/cyan ở đá hoặc lưỡi; chi tiết tối giúp tách hình. |

Giới hạn ảnh nguồn: ô nhẫn và dây chuyền tier 7–8 trong HTML có hình bị phóng/cắt, không thể đọc đầy đủ thiết kế trang sức của hai tier này. Mô tả tier 7–8 ở bảng dựa chủ yếu vào ảnh vũ khí rõ nét; không suy ra chi tiết trang sức từ các ô bị cắt.

## Điểm cần giữ khi áp dụng cho concept vịt

- Xây bảng màu cho từng món bằng màu chất liệu, màu cấu trúc/phụ kiện, màu điểm nhấn và vùng sáng/tối. Màu rarity có thể xuất hiện ở đá, lõi, hoa văn hoặc một phần thân; không bắt buộc phủ cả món.
- Với vũ khí, cho phép lưỡi, cán, dây quấn, chắn tay và đá có màu khác nhau. Với nhẫn/dây chuyền, tách màu khung/vòng/dây khỏi màu đá/mặt trang trí.
- Phân biệt các tier bằng tổ hợp màu, độ tương phản, chất liệu và trang trí. Khi đổi tier, xem lại cách phân bố các mảng này; không chỉ đổi hue đồng loạt trên cùng một ảnh.
- Giữ nét viền đậm và các mảng sáng/tối rõ như ảnh tham chiếu để icon nhỏ vẫn đọc được hình và chất liệu.
- Tránh để vật phẩm chìm vào nền UI cùng màu; kiểm tra cả trên nền trong suốt và trong ô rarity thực tế.
- Các mẫu A/B/C cùng tier có thể thay đổi màu phụ theo thiết kế, miễn vẫn thuộc cùng ngôn ngữ chất liệu và phối màu của tier.
- Không áp đặt một tỉ lệ màu cố định cho mọi món: đây là nguyên tắc phối màu, không phải quy định mỗi món phải có cùng số màu hoặc cùng phần trăm màu rarity.

## Art vịt mới cũng tách màu nền và màu đồ

Ảnh mục 5 của HTML cho thấy một bảng phối riêng, không trùng hoàn toàn với bộ item gốc:

- L05 nền vàng: mũ/áo/balo chủ yếu tím, có chi tiết vàng; dao phối tím, bạc và vàng.
- L06 nền tím: nhiều món dùng vàng làm màu chính, phối phần tối và điểm sáng trắng/bạc.
- L07 nền xanh lá đậm: nhiều món dùng đỏ với đen và vàng.
- L08 nền navy: nhiều món dùng cyan/xanh ngọc sáng với phần tối.
- L09 nền đỏ: nhiều món dùng cam/vàng với đen; dao có vàng/cam, bạc và điểm đỏ.
- L10 nền cam: phối vàng, trắng/ngà và các chi tiết sáng.

Cả bộ gốc và bộ vịt mới đều minh hoạ yêu cầu user: **nhận diện rarity bằng một tổ hợp màu có chủ ý, không đồng nhất màu nền UI với toàn bộ màu vật phẩm**. Khi vẽ một bộ mới, chọn bộ tham chiếu phù hợp với yêu cầu cụ thể; không tự lấy màu nền làm lệnh tô đơn sắc hoặc coi hai bộ có bảng màu giống hệt nhau.
