# Melee v10 — tinh chỉnh theo phản hồi bộ v09

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

## Ảnh cuối

- `melee-daggers-L01-L10-v10.png`
- `melee-axes-L01-L10-v10.png`
- `melee-hammers-L01-L10-v10.png`

Mỗi bảng 5 cột × 2 hàng, đủ L01–L10. Nguồn sửa là ba bảng trong `../v09-approved-view/`, được user đánh giá tốt hơn rất nhiều; chỉ sửa những điểm mới dưới đây.

## Phản hồi và thay đổi

- Dao L05 nhiều chi tiết hơn L06 → bỏ các nét/inlay vàng trên lưỡi L05, giữ ngọc amber, chắn tay vàng và cán navy. Các dao khác giữ nguyên, đặc biệt không tăng chi tiết L06.
- Rìu L03/L04 chưa khác rõ L02 → L03 có họa tiết xanh lá và đinh tông xanh; L04 thêm ngọc xanh và họa tiết rune xanh. Vẫn trong cùng dáng rìu, không dùng motif vàng cao cấp quá sớm.
- Toàn bộ rìu bỏ mảng sáng trên sống phía trên để không gợi mặt trên 3D; giữ mảng sáng ở mép lưỡi cắt. Đã sửa riêng L10 để xóa dải trắng/vàng còn sót.
- Toàn bộ cán rìu/búa dài hơn, prompt yêu cầu 1.20 lần chiều dài lộ ra của cán, giữ rộng cán và kích thước đầu. Đây là chỉnh hình bằng ImageGen, chưa đo pixel để chứng nhận chính xác 20% cho từng món.
- Giữ kiểu nhìn một mặt trước, nét đậm, L01 gỗ trơn, nền rarity, motif các tier và bảng màu đã chốt.

V10 là ảnh concept đã sửa, chưa có xác nhận user sau lượt này; chưa tách sprite hoặc import Unity.

## Dao — bớt chi tiết L05

```text
Edit ONLY the L05 DAGGER (top row, fifth/rightmost tile on yellow background). Remove ALL the thin gold angular filigree and gold blade-inlay decoration from its BLADE. The blade must become clean simple blue-silver steel with the same broad light/mid/dark planes as L04. Keep L05's dark AMBER square guard gem, simple gold guard and navy grip. No ornament on the L05 blade. Its design must clearly be LESS intricate and LESS luxurious than the purple/gold L06 dagger below-left. Do not increase decoration on L06; simplify L05 only. Preserve original L05 blade shape, short size and single-face viewpoint. Do NOT change any other dagger or handle length. Same complete 5x2 sheet, labels, palette, bold lines and backgrounds. Only this one local simplification.
```

## Rìu — phân biệt tier, bỏ mảng trên, dài cán

```text
Revise this exact 10-axe sheet with ONLY the following requested changes, preserving all unrelated designs and colors.

1. ALL TEN AXES: Remove the LIGHT STRIP along the TOP/SPINE of the axe head. This is the pale diagonal strip along the UPPER-LEFT straight edge of the blade, running from the socket toward the highest point. It wrongly looks like a visible TOP FACE. DELETE THAT ENTIRE LIGHT TOP STRIP and fill it with the SAME MIDTONE FRONT-FACE COLOR as the adjacent blade body. The broad front face now runs right up to the outer black top-edge contour. No extra parallel line or thickness plane there. Also remove any bright upper lip of the socket plate. Keep the broad light curved CUTTING-EDGE STRIP on the RIGHT side: the cutting edge is allowed, the TOP-FACE STRIP is not. All heads are strictly one-face flat front elevations. Gold ornamental front lines may stay but no separate lit top plane.

2. L03 (top row third/lime tile): make it visibly more developed than plain L02. Keep the bearded axe silhouette but add ONE simple leaf-green V/CHEVRON enamel inlay ON the main blade face, dark olive socket, and green-toned rivet centers. Refined steel blue body, brown grip. One clear restrained green graphic, not elaborate gold.
3. L04 (top row fourth/sky-blue tile): make it clearly distinct from L02 and above L03. Give its socket a single small BLUE diamond gemstone in a dark steel/olive setting, and a modest clean BLUE angular/rune inlay ON the broad blade front. Keep navy grip, blue-silver blade and the same short bearded axe family. No overly ornate gold, no 3D extrusion. L02 stays plain iron with rivets.

4. ALL TEN HANDLES: increase the EXPOSED HANDLE LENGTH by 20 PERCENT measured from where grip exits the head/socket to the bottom tip. H_new=1.20*H_old. Keep the SAME HANDLE WIDTH, SAME HEAD DIMENSIONS and head designs. Add a matching grip segment and proportional bands along the same diagonal axis. The axe-head to grip-length ratio must visibly change; do NOT scale the whole weapon. Move each whole icon slightly upward/right to accommodate the longer lower-left grip if necessary, keeping everything fully inside each tile and preserving margins. Do not shrink heads. L01 remains plain bare wood, no wrapping added.

Preserve 5x2 layout, all labels, exact rarity background hues, bold clean cartoon linework and the approved single FRONT FACE with NO top/bottom/side surfaces. All other L05-L10 front ornament motifs remain. Output complete revised sheet.
```

## Búa — dài cán

```text
Edit ONLY the LENGTH OF ALL TEN HAMMER HANDLES in this exact sheet.
For each hammer, increase the EXPOSED HANDLE length by 20 PERCENT, from the point it exits the head down to the grip bottom. New length = 1.20 times original length.
Keep the SAME HEAD SIZE and HEAD SHAPE, same gemstone, front decorations, colors and end flanges. Keep the SAME GRIP WIDTH. Extend the grip by adding matching material/bands along its existing diagonal lower-left axis. Do NOT merely enlarge or uniformly scale the whole hammer, do NOT shrink heads. The grip must be visibly longer relative to the unchanged head. Slightly translate each whole hammer upward/right within its tile if needed for clearance. Keep the full weapon inside the square with margins.
L01 remains bare wood with NO wrapping or metal. L02-L10 retain the original colors and straight frontal bands.
Preserve the approved SINGLE FRONT FACE geometry; no visible cylinder end face, no top/side/bottom faces added.
Nothing else changes: same complete 5x2 ten-hammer board, same labels, backgrounds, bold linework, colors and all head ornaments.
```

## Rìu — xóa nốt dải sáng L10

```text
Edit ONLY the L10 axe in the BOTTOM RIGHT tile (orange background) of this exact sheet.
The L10 head still has a broad bright WHITE/GOLD STRIP along its straight UPPER-LEFT SPINE, between the gold socket and its top point. This strip looks like a TOP-FACING SURFACE and must DISAPPEAR.
Erase this entire broad diagonal top-spine stripe. Replace it seamlessly with the SAME IVORY MAIN FRONT-FACE fill adjacent to it, with no parallel line, no gold rail and no separate light plane along the straight top-left edge. The front face must meet the outer black spine contour directly.
Keep the existing thin GOLD FRONT ORNAMENT curves around the AQUA gems. Keep the LIGHT CURVED CUTTING EDGE on the RIGHT. Only the broad upper-left top-face stripe disappears.
Do not alter the approved new L03 green motif, L04 blue gemstone/rune, any other axe, the longer handles, their width or head sizes. Preserve the whole 5x2 sheet, labels, tile colors, dark bold outlines and ONE-FACE front view. Output full sheet with only that L10 top-spine strip removed.
```
