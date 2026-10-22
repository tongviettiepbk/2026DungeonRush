# Rìu L01–L10 — PNG nền trong suốt

Ngày: 2026-10-22.

## Bàn giao

- `png/axe-L01.png` đến `png/axe-L10.png`: mỗi món một PNG 1254 × 1254, alpha trong suốt.
- `axes-L01-L10-transparent.zip`: chỉ chứa đúng 10 PNG, không lẫn ảnh bảng nguồn.
- `source-sheet.png`: bản nguồn user đính kèm, để đối chiếu.
- `alpha-audit.csv`: kích thước, số mẫu pixel trong suốt/hiện hình và kích thước file; đã kiểm đủ 10 PNG và 10 mục trong ZIP.

## Phương pháp

Dùng built-in ImageGen ở chế độ background-extraction cho từng món, `transparent_background=true`, không dùng CLI/API. Đã kiểm bằng mắt nhận diện tier và kiểm kênh alpha bằng đọc PNG. Không chỉnh alpha hay hình ảnh bằng code; file PNG sao chép nguyên kết quả công cụ. Đây là tách bằng mô hình ảnh, không phải mặt nạ/crop đảm bảo giữ nguyên từng pixel ảnh gốc; có thể có khác biệt nhỏ ở viền hoặc mảng màu. Có sửa lại riêng L10 vì công cụ thêm dải vàng trên sống rìu so với ảnh nguồn.

Chưa import/thay asset Unity. Mọi ảnh đều giữ khung PNG vuông, không cắt sát biên để tránh mất nét vũ khí.

## L01

```text
Use case: background-extraction.
Extract ONLY the single L01 AXE from the provided sheet. It is in the TOP ROW, FIRST/LEFTMOST tile, GRAY background, labeled L01. Identification: plain brown wooden grip and simple matte gray stone axe head, NO socket plate or rivets, NO wrap bands, NO gems.
This is a faithful cutout, NOT a redesign or a new concept. Preserve the selected axe's exact shape, diagonal orientation, one-face front projection, proportions, colors, internal markings, gemstone/rivet details and bold black outline. Do not change the artwork, add detail, simplify the design, recolor, rotate or change handle length. No other axes.
Remove EVERYTHING except the axe itself: remove the colored rarity tile fill, rounded black square frame, offwhite sheet, and all text/labels. Preserve the weapon's own black contour and every part of head and grip. Areas around the item AND the concave gap under the blade must be fully transparent.
Output one individual PNG cutout with GENUINE ALPHA TRANSPARENCY (not a white background, not a checkerboard image, not a colored background). Center the entire whole axe on a square transparent canvas with modest even padding, same apparent scale used for all ten cutouts. No cast shadow or glow. ONLY the isolated L01 axe on transparent background.
```

## L02

```text
Use case: background-extraction.
Extract ONLY the single L02 AXE from the provided sheet. It is in the TOP ROW, SECOND tile, BEIGE background, labeled L02. Identification: brown wrapped grip with gray end band, gray front socket plate with three silver rivets and plain blue-gray steel axe blade.
This is a faithful cutout, NOT a redesign or a new concept. Preserve the selected axe's exact shape, diagonal orientation, one-face front projection, proportions, colors, internal markings, gemstone/rivet details and bold black outline. Do not change the artwork, add detail, simplify the design, recolor, rotate or change handle length. No other axes.
Remove EVERYTHING except the axe itself: remove the colored rarity tile fill, rounded black square frame, offwhite sheet, and all text/labels. Preserve the weapon's own black contour and every part of head and grip. Areas around the item AND the concave gap under the blade must be fully transparent.
Output one individual PNG cutout with GENUINE ALPHA TRANSPARENCY (not a white background, not a checkerboard image, not a colored background). Center the entire whole axe on a square transparent canvas with modest even padding, same apparent scale used for all ten cutouts. No cast shadow or glow. ONLY the isolated L02 axe on transparent background.
```

## L03

```text
Use case: background-extraction.
Extract ONLY the single L03 AXE from the provided sheet. It is in the TOP ROW, THIRD tile, LIME GREEN background, labeled L03. Identification: brown wrapped grip with olive end band, olive socket with three green rivets, pale blue-gray blade with the distinctive green leaf/V motif.
This is a faithful cutout, NOT a redesign or a new concept. Preserve the selected axe's exact shape, diagonal orientation, one-face front projection, proportions, colors, internal markings, gemstone/rivet details and bold black outline. Do not change the artwork, add detail, simplify the design, recolor, rotate or change handle length. No other axes.
Remove EVERYTHING except the axe itself: remove the colored rarity tile fill, rounded black square frame, offwhite sheet, and all text/labels. Preserve the weapon's own black contour and every part of head and grip. Areas around the item AND the concave gap under the blade must be fully transparent.
Output one individual PNG cutout with GENUINE ALPHA TRANSPARENCY (not a white background, not a checkerboard image, not a colored background). Center the entire whole axe on a square transparent canvas with modest even padding, same apparent scale used for all ten cutouts. No cast shadow or glow. ONLY the isolated L03 axe on transparent background.
```

## L04

```text
Use case: background-extraction. Extract ONLY the L04 AXE from the sheet, located TOP ROW, FOURTH tile, CYAN/SKY BLUE background, labeled L04. Exact identity: navy wrapped grip and olive end band, olive socket with ONE BLUE diamond gemstone, pale blue-gray blade with a distinct angular BLUE rune.
Faithful cutout only: preserve the selected axe artwork, whole silhouette, long grip, original proportions, colors, markings, jewel/rivet count, bold outline, front-only 2D viewpoint and diagonal orientation. Do NOT redesign, recolor, rotate, simplify or invent detail. Remove all other nine axes.
Remove the colored tile, its square rounded border, offwhite sheet and all letters. Keep the axe's own black outline intact. Fully transparent exterior and the concave open gap below the blade.
Return a SINGLE isolated whole axe centered with modest padding on a square TRANSPARENT PNG with actual alpha. Match the size/framing of the series: weapon fills about 80-85% of square, not clipped. No checkerboard painted into image, no solid background, no floor or shadow. Only L04 axe, nothing else.
```

## L05

```text
Use case: background-extraction. Extract ONLY the L05 AXE from the sheet, located TOP ROW, FIFTH/RIGHTMOST tile, YELLOW background, labeled L05. Exact identity: navy wrapped grip with gold end band, gold socket and three AMBER rivets, dark blue blade front with gold angular ornament and pale cutting edge.
Faithful cutout only: preserve the selected axe artwork, whole silhouette, long grip, original proportions, colors, markings, jewel/rivet count, bold outline, front-only 2D viewpoint and diagonal orientation. Do NOT redesign, recolor, rotate, simplify or invent detail. Remove all other nine axes.
Remove the colored tile, its square rounded border, offwhite sheet and all letters. Keep the axe's own black outline intact. Fully transparent exterior and the concave open gap below the blade.
Return a SINGLE isolated whole axe centered with modest padding on a square TRANSPARENT PNG with actual alpha. Match the size/framing of the series: weapon fills about 80-85% of square, not clipped. No checkerboard painted into image, no solid background, no floor or shadow. Only L05 axe, nothing else.
```

## L06

```text
Use case: background-extraction. Extract ONLY the L06 AXE from the sheet, located BOTTOM ROW, FIRST/LEFTMOST tile, PURPLE/LAVENDER background, labeled L06. Exact identity: purple wrapped grip with gold end band, gold socket with three PURPLE rivets, purple blade decorated with a gold angular motif and pale lavender cutting edge.
Faithful cutout only: preserve the selected axe artwork, whole silhouette, long grip, original proportions, colors, markings, jewel/rivet count, bold outline, front-only 2D viewpoint and diagonal orientation. Do NOT redesign, recolor, rotate, simplify or invent detail. Remove all other nine axes.
Remove the colored tile, its square rounded border, offwhite sheet and all letters. Keep the axe's own black outline intact. Fully transparent exterior and the concave open gap below the blade.
Return a SINGLE isolated whole axe centered with modest padding on a square TRANSPARENT PNG with actual alpha. Match the size/framing of the series: weapon fills about 80-85% of square, not clipped. No checkerboard painted into image, no solid background, no floor or shadow. Only L06 axe, nothing else.
```

## L07

```text
Use case: background-extraction. Extract ONLY the L07 AXE from the sheet, located BOTTOM ROW, SECOND tile, GREEN/JADE background, labeled L07. Exact identity: forest-green wrapped grip with gold end band, gold socket with three GREEN rivets, green blade front decorated with GOLD LEAF/VINE shapes and mint cutting edge.
Faithful cutout only: preserve the selected axe artwork, whole silhouette, long grip, original proportions, colors, markings, jewel/rivet count, bold outline, front-only 2D viewpoint and diagonal orientation. Do NOT redesign, recolor, rotate, simplify or invent detail. Remove all other nine axes.
Remove the colored tile, its square rounded border, offwhite sheet and all letters. Keep the axe's own black outline intact. Fully transparent exterior and the concave open gap below the blade.
Return a SINGLE isolated whole axe centered with modest padding on a square TRANSPARENT PNG with actual alpha. Match the size/framing of the series: weapon fills about 80-85% of square, not clipped. No checkerboard painted into image, no solid background, no floor or shadow. Only L07 axe, nothing else.
```

## L08

```text
Use case: background-extraction. Extract ONLY the L08 AXE from the provided sheet, located in the BOTTOM ROW, THIRD tile, ROYAL NAVY BLUE background, labeled L08. Identification: navy/blue wrapped grip with ice-blue end band, blue socket and THREE cyan-blue rivets, icy cyan axe blade with large angular ICE/CRYSTAL pattern.
This is a faithful cutout of an existing selected icon, not a redesign. Preserve its exact whole silhouette, long grip proportion, diagonal orientation, single-face front projection, palette, front ornament shapes, jewel/rivet count and heavy black outer line. Do NOT add a top-spine highlight strip, new facets, new face, new detail, altered saturation or different markings. Do not change the artwork. Remove every other item.
Completely remove the colored tile/background, black rounded square FRAME, all labels and the offwhite sheet. The axe's OWN black silhouette outline must stay. Exterior and concave gap under blade become fully transparent.
Output ONE individual square PNG containing ONLY the full L08 axe on true TRANSPARENT ALPHA background. No black/white solid background, no checkerboard pixels, no shadow, no glow. Center item with modest padding, weapon fills about 80-85% of the square with nothing clipped. Match the rest of this ten-icon transparent cutout series.
```

## L09

```text
Use case: background-extraction. Extract ONLY the L09 AXE from the provided sheet, located in the BOTTOM ROW, FOURTH tile, CORAL RED background, labeled L09. Identification: deep purple wrapped grip with lavender end band, near-black-purple socket with THREE RED rivets, dark purple blade front with a RED angular/crimson motif and lavender cutting edge.
This is a faithful cutout of an existing selected icon, not a redesign. Preserve its exact whole silhouette, long grip proportion, diagonal orientation, single-face front projection, palette, front ornament shapes, jewel/rivet count and heavy black outer line. Do NOT add a top-spine highlight strip, new facets, new face, new detail, altered saturation or different markings. Do not change the artwork. Remove every other item.
Completely remove the colored tile/background, black rounded square FRAME, all labels and the offwhite sheet. The axe's OWN black silhouette outline must stay. Exterior and concave gap under blade become fully transparent.
Output ONE individual square PNG containing ONLY the full L09 axe on true TRANSPARENT ALPHA background. No black/white solid background, no checkerboard pixels, no shadow, no glow. Center item with modest padding, weapon fills about 80-85% of the square with nothing clipped. Match the rest of this ten-icon transparent cutout series.
```

## L10

```text
Use case: background-extraction. Extract ONLY the L10 AXE from the provided sheet, located in the BOTTOM ROW, FIFTH/RIGHTMOST tile, ORANGE background, labeled L10. Identification: ivory/cream wrapped grip with gold and aqua end band, gold socket with THREE AQUA rivets, ivory blade front with GOLD curves and AQUA gem/leaf motif.
This is a faithful cutout of an existing selected icon, not a redesign. Preserve its exact whole silhouette, long grip proportion, diagonal orientation, single-face front projection, palette, front ornament shapes, jewel/rivet count and heavy black outer line. Do NOT add a top-spine highlight strip, new facets, new face, new detail, altered saturation or different markings. Do not change the artwork. Remove every other item.
Completely remove the colored tile/background, black rounded square FRAME, all labels and the offwhite sheet. The axe's OWN black silhouette outline must stay. Exterior and concave gap under blade become fully transparent.
Output ONE individual square PNG containing ONLY the full L10 axe on true TRANSPARENT ALPHA background. No black/white solid background, no checkerboard pixels, no shadow, no glow. Center item with modest padding, weapon fills about 80-85% of the square with nothing clipped. Match the rest of this ten-icon transparent cutout series.
```

## Sửa L10 để khớp nguồn

```text
Correct the isolated TRANSPARENT L10 axe to match the source sheet exactly.
Image 1 is the isolated cutout to correct. Image 2 is the original source sheet; the correct L10 axe is the BOTTOM RIGHT tile on orange.
The cutout incorrectly added a broad GOLD/YELLOW stripe along the UPPER-LEFT straight spine of the blade. The original L10 DOES NOT HAVE this gold top strip. REMOVE the entire broad gold/yellow strip and its white parallel rim. Fill that blade-spine region with the same IVORY WHITE FRONT-FACE COLOR as the adjacent blade. The head's front must meet its outer black spine contour directly. There must be NO separate top face, no parallel gold rail there.
Keep the thin gold decorative CURVES around the aqua motifs on the blade front, keep the gold socket and three aqua rivets, ivory grip, gold/aqua front end band, light curved cutting edge. Do not change the silhouette or other artwork.
This is still ONE isolated axe PNG with GENUINE transparent alpha everywhere outside and in the concave gap. No solid background, no checkerboard pattern, no text, no tile. Same framing. Change ONLY the incorrect broad top-spine stripe to ivory to match original L10.
```
