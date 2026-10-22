# Melee v08 — chỉ một mặt item

Ngày: 2026-10-22. Tạo bằng built-in ImageGen.
Ảnh: `melee-dagger-axe-hammer-10-rarities-v08-one-face.png`.

## Chỉ đạo mới nhất của user

User nhận xét v07: đầu búa đã ổn, nhưng cán búa còn nghiêng; rìu và dao còn nhìn thấy cả mặt đáy và mặt trên. User chốt: **"với góc nhìn này mặt chỉ nhìn thấy đúng 1 mặt của item thôi, các mặt khác biến mất"**.

- Giữ nguyên đầu búa v07; chỉ sửa cán búa.
- Dao/rìu phải sửa cả phần cán và các bề mặt trên/bên/đáy còn lộ.
- Mỗi bộ phận thể hiện bằng MỘT mặt trước; không phải render vật 3D rồi chỉ đổi sang camera orthographic.
- Không để mặt đáy elip, miếng đáy màu tối, mặt trên của chắn tay/socket hoặc mặt bên do độ dày khối.
- Các đường băng trên cán là dải trên mặt trước; không cong vòng quanh trụ để lộ góc nghiêng.
- Cả vật phẩm vẫn xoay trong mặt tranh khoảng 45 độ, cán dưới trái, đầu trên phải.
- Giữ dáng và chi tiết theo concept gốc/v06; không quay về các bản đã làm đơn giản quá tay v03-v05.

V08 là bản sửa để xem, chưa được user duyệt. Không tự gọi là đạt chuẩn chỉ vì đã tạo ảnh. Nếu sửa tiếp, kiểm lần lượt cán búa, rìu, dao theo tiêu chí MỘT MẶT và giữ đầu búa đã được chấp nhận.

## Prompt

```text
Precise viewpoint correction to this 30-weapon board. The user's rule is literal: WE SEE EXACTLY ONE FACE OF EACH ITEM. ALL OTHER FACES DISAPPEAR.

Image 1 is the edit target, keep its designs, rarity colors, layout, labels, diagonal orientation, ornament shapes and item proportions. Image 2 demonstrates the required ONE-FACE 2D sprite projection. Do not copy its designs.

LOCK the broad decorated HAMMER HEADS in the bottom row: their front view is approved. Do not change those hammer heads. Correct their handles only.

REDRAW THESE PARTS AS SINGLE-FACE FLAT SHAPES:
ALL 30 HANDLES:
The whole visible handle is ONE FRONT PLANE. Grip wrapping stripes are STRAIGHT PARALLEL BANDS, perpendicular to the diagonal handle centerline, NOT arcs curving around a cylinder. Grip ends have a single outer contour only: NO inset oval, NO cap bottom face, NO dark terminal wedge, NO curved line across the end that reveals a second plane. Remove every visible end-grain or terminal surface, including L01 wood ends. Keep the short elegant grip silhouette with gently rounded corners and existing colors. Decorative end hardware is a flat band printed ON THIS ONE FRONT FACE. Keep the original short designed grip; don't lengthen it or replace its proportions.

ALL DAGGERS, TOP ROW:
Blade, crossguard, grip and pommel each show ONLY THEIR FRONT FACE. No side lip or thickness wall around the crossguard. No crossguard top surface. The guard should be a single shaped 2D frontal band across the shaft with its original ornate outline and center jewel. Keep the beautiful asymmetric leaf blade, individual rarity shape details, original gem colors and gold decoration. Erase raised sidewalls and underside faces. Painted light/dark front-face color regions may remain but must not outline an extruded edge. Remove the dark thickness ribbon along a blade edge if it reads as a sidewall. These are flat graphical front projections, NOT three-quarter renderings.

ALL AXES, MIDDLE ROW:
Axe blade, socket plate, grip and cuff each show EXACTLY ONE FRONT FACE. The rectangular jeweled/riveted socket plate must be a single flat frontal quadrilateral, no TOP LEDGE, no visible side or back thickness. The axe blade has one frontal cutting-blade silhouette, no upper thickness lip or side edge plane. Existing ornate gold/rune/ice decorations stay ON THE FRONT as flat line/motif shapes. All top-facing surfaces at axe poll/socket disappear. Its central axis is in the picture plane, no forward camera tilt.

This means ZERO ISOMETRIC geometry. ZERO cuboid side faces. ZERO cylinder end faces. ZERO visible thickness anywhere on dagger/axe parts or any handle. The answer must be a literal SINGLE-FACE illustration like a decorated flat cutout, not a realistic 3D object viewed with an orthographic lens. Orthographic alone is insufficient if extra surfaces remain visible.

Keep the existing original richness of silhouette and front-face decoration. Do not redesign into generic weapons or erase original ornament patterns. Only remove the extra visible faces and map their required colors onto the ONE remaining front face. Keep clear clean large light/mid/dark patches on that face, no dirty texture. All remain at approximately 45 degrees, grip lower left and head upper right, inside the same 30 square rarity tiles.
Don't change approved hammer heads. Output complete board.
```
