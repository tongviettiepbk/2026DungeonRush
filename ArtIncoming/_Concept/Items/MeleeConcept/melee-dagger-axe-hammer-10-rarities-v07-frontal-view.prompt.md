# Melee concept v07 — góc chính diện theo ảnh user

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

Ảnh: `melee-dagger-axe-hammer-10-rarities-v07-frontal-view.png`.

User chấp nhận tạm v06 và yêu cầu CHỈ sửa góc nhìn theo `v07-user-reference-frontal-projection.png`: chính diện 2D, xoay trong mặt tranh khoảng 45 độ; không phải phối cảnh 3D. Giữ thiết kế, chi tiết và màu bản v06. Không lấy v03-v05 làm chuẩn hình dạng.

Lượt đầu đã sửa đuôi cán nhưng đầu búa vẫn lộ mặt trên/bên; đã báo rõ khi user hỏi chuẩn chưa. Lượt sau sửa riêng đầu búa về mặt trước, các viền nẹp nằm cùng mặt phẳng; giữ hình dáng và ngọc/trang trí. Ảnh là ứng viên để user xem, chưa có xác nhận cuối cùng.

## Prompt góc nhìn toàn bộ

```text
Change ONLY THE VIEWING ANGLE of image 1's weapon icons, using image 2 as the exact 2D front-view projection reference.

IMAGE 1 = v06 board to edit. Preserve its complete 3 rows x 10 columns, same labels, same backgrounds, same weapon designs, beautiful ornamental guards, head details, rarity palettes, gems, short proportions and original quality.
IMAGE 2 = projection/camera reference ONLY. Its items are FACE-ON flat 2D SPRITES rotated diagonally within the page. Do not copy its item designs or simplify image 1 into its different shapes.

Required camera for ALL 30 items:
The camera is exactly PERPENDICULAR to the weapon's broad front plane. Orthographic front elevation. The entire item lies in the page and is rotated around 45 degrees within the page, grip lower-left and point/head upper-right.
This is NOT an isometric or three-quarter camera. Do not show an underside, top thickness surface, side extrusion, round end face, elliptical cap or perspective foreshortening. We should see ONE FRONT FACE per main part, like the reference.
Keep shading and bevel highlights ON the front face; surface bevel shading is allowed, but physically separate extruded side/top/underside surfaces are not.

Correct the geometry visibly:
- DAGGERS: full broad blade front, crossguard front, front grip and front-facing pommel ornament. Do not see bottom of grip. Guard sides not foreshortened. Keep the original asymmetric leaf blades and elaborate higher-tier guards.
- AXES: full broad cutting blade front, ornamented socket FRONT, short grip FRONT. The handle butt currently has an OVAL END FACE facing the lower-left viewer. DELETE that bottom-facing oval plane entirely. Preserve the colored original cuff as a FACE-ON BAND across the grip, flush with the handle silhouette. Any pommel decoration is placed on the visible FRONT surface, not on the exposed bottom. No hole, ellipse or disc at the grip's end. Do not replace the designed grip with a plain square stick.
- HAMMERS: broad decorated rectangular head FRONT directly faces viewer, retains the exact center jewel, trims, flanges and ornament. No visible head TOP or SIDE thickness face. Handle front faces same direction as head. No oval terminal face at the grip. Keep the shaped original short handle and face-on decorative bands.

Do not repaint palettes or change material designs. L01 wood dagger and stone axe/hammer; L03 hammer green gem; L04 blue gems; L05 amber/navy/gold with no red; L06 purple/gold; L07 mint/green/gold; L08 ice/navy; L09 purple/red; L10 gold/ivory/aqua remain as in image 1. Same complete board layout.
Preserve image 1's aesthetic and detailed construction. ONLY flatten the camera/projection into the exact face-on diagonal 2D viewpoint shown in image 2. The changed viewpoint must be obvious, especially at every handle butt and hammer head. Output the full 30-item board.
```

## Prompt đầu búa chính diện

```text
Edit ONLY the HAMMER HEADS in the BOTTOM ROW of this exact board. Preserve all daggers and axes unchanged; preserve all grips and bands unchanged. Same whole 30-tile board.

Make all ten hammer heads STRICT ORTHOGRAPHIC FRONT ELEVATION: a broad decorated rectangular face seen perfectly straight on. They remain rotated diagonally within the page, as the current handles do. Remove the visible protruding TOP THICKNESS and SIDE THICKNESS planes that make the hammer a 3D block. Each hammer head should read like one decorated planar face, NOT an isometric cuboid. Its two end flanges are symmetrical flat strips IN THE SAME FRONT PLANE, NOT raised shelves with visible tops/side walls. Keep the original shaped/chamfered rectangular head outline and center jewel, gold trims, star/ice motifs, colors and ornamentation. Preserve these authored designs. Do not replace them with empty rectangles or simplify away decorative construction. Flat face-on borders and bevel color bands are okay; projecting physical thickness faces are not.
Preserve L01 stone, L02 silver gem, L03 green gem, L04 blue gem, L05 amber gem, L06 purple gem, L07 green/mint motif, L08 ice/blue crystal, L09 dark red/purple motif, L10 ivory/gold/aqua.
No changes to board layout, labels, background colors or other weapons. Output the whole comparison board.
```
