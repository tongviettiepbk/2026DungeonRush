# Rìu L04 — chuẩn góc nhìn đã được user xác nhận

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

## Trạng thái và hướng tiếp theo

- User bác bỏ góc nhìn rìu của các bảng v07/v08; chúng vẫn có cảm giác 3D. Không dùng các bảng đó làm chuẩn góc nhìn.
- Đã dựng riêng `axe-L04-single-face-v01.png` để sửa hình học. User xác nhận: **"chuẩn góc luôn, style cũng khá ổn rồi, nhưng tao cần đường nét đậm hơn"**.
- Vì vậy **mẫu rìu riêng v01 là chuẩn góc nhìn và style đã được duyệt**. Phải giữ đúng mặt trước duy nhất: không thấy đáy cán, ngàm chỉ một mặt, không mặt trên/bên, vẫn chéo khoảng 45 độ.
- Theo yêu cầu mới nhất, tạo `axe-L04-single-face-v02-bold.png` tăng độ đậm viền ngoài và nét trong; chưa có xác nhận user cho độ dày nét mới. Không tự thay góc, màu, tỉ lệ hoặc chi tiết ở bước chỉnh nét.
- Yêu cầu gốc vẫn là 3 dòng melee × 10 rarity. Bảng 30 món chưa được hoàn thiện theo chuẩn góc nhìn này; chưa tự coi là đã xong toàn bộ.
- Giữ thiết kế concept đầu cho bộ đồ; khi triển khai lại, dùng hình học một mặt của mẫu rìu này. Không hồi quy về góc 3D vì copy lại bảng cũ.

## Prompt dựng mẫu một mặt

```text
Draw ONLY ONE weapon: the L04 single-headed bearded axe from the attached board, enlarged on one sky-blue square rarity tile. This is a corrected VIEWPOINT STUDY, not a new design. Preserve its blade outline, navy short grip, muted green/steel socket, three small rivets and blue-silver blade palette.

REQUIRED VIEW: a literal flat 2D FRONT ELEVATION, like a decorated paper cutout. Weapon rotated 45 degrees within the page, grip bottom-left, head top-right. All visible parts lie in the SAME SINGLE FRONT PLANE. Exactly ONE FACE is visible. No 3D object rendering.

Geometry:
1. Grip is a front-view short shaped strip, with a rounded terminal OUTER CONTOUR. Grip wrapping is straight parallel diagonal stripes. The terminal has no internal line, no cap face, no dark wedge, no end surface. A navy grip with a flat olive band across the front is enough.
2. Socket plate is one single flat front-view rectangle rotated in the page, decorated with three dots. There is NO thickness polygon above it, NO top ledge, NO dark sidewall or second outline. Its top and bottom edges are straight and parallel, perpendicular to the grip centerline.
3. Axe blade is the broad original single curved bearded blade drawn as one flat silhouette facing directly towards viewer. No extra surface along its spine or top. Paint TWO clear uniform blue-gray regions ON THAT ONE FACE to indicate light and shadow; do not add sidewall depth.
4. Every part has a single bold black OUTER contour. Front-face decoration can remain. No inset contour following the butt that creates a bottom face.

Use clean attractive cartoon game-icon artwork, smooth solid flat fills, hard-edged large tonal regions, no texture, no grain, no gradients, no cylindrical highlights, no specular glints, no raised bevels. Keep the proportions and family of the original approved axe; avoid a chunky generic toy. No labels, no red annotations, no other weapons. Center the whole axe with generous margins in a single rounded black-bordered sky-blue square. The only permitted geometry is the single front silhouette and its colored flat interior regions.
```

## Prompt tăng độ đậm nét

```text
Edit this exact single axe icon with ONE CHANGE ONLY: make the weapon's linework bolder.
Increase the BLACK OUTERMOST CONTOUR around the axe head, socket and handle by approximately 30%.
Increase the black INTERNAL DIVIDING LINES and black rings around the three rivets by approximately 15-20%, less than the outer contour. Keep crisp smooth anti-aliased edges, consistent black, graceful clean joins. The blade outline, grip outline and socket outline should visibly read more boldly at small inventory-icon scale.
DO NOT thicken the already-heavy outer square tile border. DO NOT change its size.

Preserve EVERYTHING ELSE exactly: the successful SINGLE-FACE flat frontal viewpoint, the 45-degree diagonal orientation, all silhouettes and proportions, original bearded axe head contour, navy grip with straight wrap stripes, olive socket and front bands, three pale blue rivets, the broad light/mid/dark blue-gray blade regions, sky-blue tile background, all spacing and framing.
Do not introduce ANY perspective: no top face, no side face, no bottom face, no cylindrical handle end face. Keep the approved flat frontal image unchanged apart from stroke weight. No new ornament, no texture changes, no added details, no palette shifts. Output the same complete single square icon with visibly bolder weapon linework.
```
