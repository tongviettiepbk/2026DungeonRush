# Melee concept v04 — mảng sáng tối cartoon, góc 45 độ

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

Ảnh cuối: `melee-dagger-axe-hammer-10-rarities-v04-flat-45deg.png`.
Ảnh user chỉ rõ artstyle và góc nhìn: `v04-user-reference-flat-shading.png` (đã copy vào project để không phụ thuộc file tạm).

Yêu cầu mới nhất của user:
- Mặt vũ khí chính diện phẳng 2D, cả vật phẩm xoay khoảng 45 độ với phương ngang, cán dưới trái và đầu trên phải. KHÔNG hiểu 'chính diện' là bắt buộc dựng thẳng đứng như v03.
- Mảng khối sáng/trung gian/tối phải rõ như ảnh tham chiếu; mỗi mảng tô đều một màu, đường ranh sắc nét, không texture loang hoặc render ánh kim mềm.
- Đồ đá L01 không cần nhiều vết mẻ, tránh răng cưa hoặc vết nứt dày.
- Giữ các yêu cầu trước: dao gỗ L01, cán gỗ trơn cho rìu/búa L01; ngọc dao L04 xanh lam; ngọc L05 vàng đậm; stroke ngoài đậm; 3 món × 10 rarity trên nền ô.

Đây là bảng concept, chưa tách sprite để import.

## Prompt dựng lại style và góc nhìn

```text
Use case: style-transfer. Redraw a complete 30-icon weapon board.

REFERENCE ROLES:
Image 1 is the user's EXACT REQUIRED STYLE AND VIEWPOINT. Match its thick almost-black outlines, extremely simple flat color shapes and clearly separated LIGHT / MID / DARK planes. Its blade shading consists of large clean uniform-color polygons, like pale blue front plane, medium blue bevel and dark slate underside. STUDY THIS FLAT GRAPHIC SHADING.
Image 2 supplies ONLY the 3 weapon families, 10 rarity columns, color identities, labels and grid. Its smooth shiny gradients and rendering are WRONG and must be completely replaced. Do not preserve its surface rendering.

REQUIRED LAYOUT:
Same offwhite board, exactly 3 rows by 10 columns of square rounded rarity tiles. Labels across top: L01 L02 L03 L04 L05 L06 L07 L08 L09 L10. Row labels: DAGGER / AXE / HAMMER. Same rarity backgrounds left to right: gray, beige, lime green, sky cyan blue, yellow, lavender, forest jade green, royal navy blue, coral red, golden orange. Thick black tile borders with restrained inner rim. All 30 tiles and weapons visible, no cropping, consistent spacing.

POSE, VERY IMPORTANT:
Every weapon shown FLAT FACE-ON in ORTHOGRAPHIC FRONT VIEW, but rotated IN THE PICTURE PLANE at approximately 45 DEGREES TO THE HORIZONTAL, just like reference image 1. Handle tip at LOWER LEFT, weapon point/head at UPPER RIGHT. NO vertically upright weapons. This is a diagonal 2D sprite, NOT a tilted 3D camera. Do not show perspective, foreshortening, elliptical bottom end cap faces, or multiple visible thickness side faces. Full broad face of dagger blade, axe cutting head, hammer's broad rectangular head faces viewer. All 30 use this same 45-degree pose. Short chunky handles and broad blades/heads, item within central 80% of each square.

THREE WEAPON FAMILIES:
Top row: a single SHORT broad dagger with a simple short guard and stubby grip, not a long sword.
Middle row: a single SHORT-HANDLED bearded single-head axe, not a double-headed axe.
Bottom row: a single SHORT-HANDLED hammer with a wide rectangular block head and diamond-shaped center gemstone on L02-L10.
NO paired weapons, NO extra objects.

L01 PRIMITIVE ITEMS:
Dagger L01 entirely WOOD: flat tan highlight plane, mid brown face, dark brown shadow plane. No metal, gems or wrapping. Bare carved wood handle.
Axe L01: simple gray STONE single axe head on plain brown WOODEN HANDLE, NO grip wrapping, NO metal socket, NO fittings. Stone head has largely intact clear silhouette and only ONE tiny understated edge chip. Do not make a saw, not numerous jagged chips. Gray highlight/mid/shadow as only 3 solid polygons. No crack network.
Hammer L01: plain chunky gray STONE block head on plain WOODEN HANDLE, NO wrapping, NO metal, NO gems. Almost intact block, only ONE small corner imperfection. No large missing corners, no central crack, no crack pattern. Clear stone light/mid/dark planes in solid flat gray polygons.

COLOR DETAILS THAT MUST BE RETAINED:
L02 brown grip + gray/silver iron.
L03 steel pale blue plus dark slate/olive and brown grip. Hammer center gemstone GREEN.
L04 pale cold blue steel plus navy grip/green-slate trim. Dagger guard mounted BLUE gemstone. Hammer center gemstone BLUE.
L05 silver-blue metal, NAVY grips and gold trim; ALL JEWELS/INSETS and tiny round handle cap insets DARK GOLDEN YELLOW/AMBER (not blue, not red). The dark amber gems must have a black border to remain legible on yellow backgrounds. NO red anywhere in L05.
L06 lavender-silver, deep purple and gold, purple gems.
L07 pale mint, dark forest green and gold, green gems.
L08 icy pale cyan / white plus navy and blue gemstones.
L09 near-black/deep purple and ruby red, lavender edge plane.
L10 ivory, gold and pale aqua/turquoise gemstones.
Preserve coordinated multicolor physical components rather than tinting whole weapon background color.

EXACT RENDERING LANGUAGE, HIGHEST PRIORITY:
Use FLAT COLOR CEL-SHADED CARTOON SPRITE ART as in reference 1. Each material is a group of only TWO OR THREE LARGE UNIFORM SOLID COLOR REGIONS with ABRUPT CRISP BOUNDARIES: one LIGHT region, one MID region, one DARK region. Every pixel INSIDE any individual color region is the SAME UNIFORM COLOR. These flat tonal regions clearly explain the object's shape. No airbrush and no transitions between colors. A steel blade has a large pale blue main region, medium slate blue triangular bevel region and a dark slate base/shadow region. A hammer face has a large midtone field and broad simple light and dark edge planes. Not one single unshaded flat color for the whole object, and not blended shading: clearly separate 2-3 flat tones.
Remove all texture, surface mottling, photo grain, scratches, cracks, brushed metal, rippling reflections, ornamental filigree, specular shine, soft glints, small facets and volumetric render. Wood can have ONE simple grain line at most. Grips have 2-3 broad graphic bands maximum. Jewels are simple 2-tone geometric marks, NOT glossy spheres.
Thick black outer silhouette stroke, about 20% bolder than reference 2, rounded joins. Thin simple interior lines only where needed.
All icons should look hand-authored in the same simple flat cartoon UI sprite language as image 1. ZERO REALISTIC OR SOFT SHADING, ZERO GRADIENTS ON WEAPONS, ZERO 3D LOOK. The reference 1 style is the target, not reference 2 rendering.

Deliver one full clean large wide 30-item comparison board.
```

## Prompt tinh chỉnh hàng rìu

```text
Precise edit of this complete 30-weapon board. Keep the same board, labels, palette, all dagger and hammer designs, and the clean cel-shaded cartoon styling.

ONLY CHANGE the TEN AXES in the MIDDLE ROW:
Their handle axes are currently too steep, around 60 degrees from horizontal. Rotate each ENTIRE AXE CLOCKWISE approximately 15 DEGREES so the straight centerline through its grip and socket is EXACTLY 45 DEGREES from horizontal, handle butt in lower left and axe socket/head in upper right. In every middle-row square, the point at the center of the handle butt and the point at the center of the axe socket should have EQUAL horizontal and vertical separation. Same 45-degree slope as the dagger blades in the row above. Rotate the whole object in the flat image plane, NOT a 3D camera, preserving a face-on broad axe blade. Keep each axe fully inside its square, rescale very slightly if needed for border clearance. Do not straighten any item vertically.

For L01 axe only, reduce edge damage to just ONE shallow notch. The stone head is gray rock with 3 LARGE CLEAN UNIFORM SOLID-COLOR POLYGONS: light gray upper-left plane, mid gray main face, dark gray lower/right edge plane. No cracks, no texture speckles, no gradients, no streaks. Its wood handle stays plain, no wraps. Similarly keep L01 hammer mostly intact, only one tiny corner irregularity. All other cells/colors remain unchanged.

Retain thick black outer outlines, flat hard-edged light/mid/dark color blocks, no soft blended surfaces. Output the whole 3-by-10 board.
```
