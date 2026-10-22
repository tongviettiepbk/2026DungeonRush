# Melee concept v06 — quay lại concept ban đầu

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

Ảnh: `melee-dagger-axe-hammer-10-rarities-v06-original-base.png`.

## Chỉ đạo mới nhất, ưu tiên khi sửa tiếp

User nhận xét các bản về sau càng xấu hơn và gửi lại bản ban đầu, yêu cầu bám sát bản này, chỉ sửa các điểm đã nói. Ảnh user gửi đã lưu tại `v06-user-approved-original-reference.png`.

**Dùng bản concept gốc này làm chuẩn hình dáng và độ hoàn thiện. Không dùng v03-v05 làm chuẩn thiết kế.** Không tự thay dao thành kiếm thường, cán thành que chữ nhật, hoặc đầu búa thành khối trống, không lược bỏ toàn bộ giáp/viền/trang trí đẹp của bản đầu. Giữ tỉ lệ ngắn, bố cục, bộ 30 món, màu nền và dáng ban đầu.

Các sửa đổi vẫn cần giữ: L01 dao gỗ/rìu và búa đá cán gỗ trơn ít mẻ; dao L04 ngọc xanh lam; ngọc búa L03 xanh lá, L04 xanh lam; L05 bỏ đỏ (enamel theo navy cán, ngọc vàng đậm); hướng chính diện xoay khoảng 45 độ, bỏ mặt đáy elip sai phối cảnh ở cán; mảng sáng tối rõ sạch hơn, stroke ngoài tăng nhẹ.

V06 là bản sửa để duyệt, chưa được user xác nhận. Quan sát ảnh cuối: giữ gần dáng gốc, màu ngọc chính đã đổi; đầu đá L01 còn đường vân/facet và dấu chi tiết từ bản gốc nên không tuyên bố đã sạch tuyệt đối. Không import vào Unity hoặc thay asset game.

## Prompt sửa trực tiếp bản gốc

```text
Edit the attached ORIGINAL concept board conservatively. This is the user's preferred master design. Preserve its beauty, craftsmanship and design identity. Do NOT redesign the set.

LOCK THESE INVARIANTS:
Keep the exact 3-by-10 board, square tiles, all background colors, labels, item positions, short proportions, and original graceful weapon silhouettes. Keep the broad sculpted asymmetric leaf dagger, the bearded axe with its notched concave underside, and the chamfered rectangular hammer. Keep the original crossguards, layered trim, tasteful high-tier ornaments, jewel settings and rarity progression. Do NOT turn them into generic straight swords, plain rectangular sticks, simplified blank hammers or coarse geometric toys. Keep the recognizable original handle shape and short proportions. No new shapes or layout. All complete weapons stay diagonal, roughly 45 degrees in the picture plane, handle lower left and blade/head upper right. Broad faces toward the viewer.

APPLY ONLY THESE REQUESTED CORRECTIONS:

1) L01 FIRST COLUMN:
- Dagger becomes an inexpensive carved WOODEN dagger, retaining the original short broad asymmetric blade and original overall silhouette. Blade/guard/grip use attractive warm brown wood light/mid/dark blocks; no metal or gemstones.
- Axe becomes a primitive gray STONE head on a plain WOODEN handle, same compact original bearded-axe silhouette but simpler materials. No leather/string wrapping, no metal caps or plates, no gems. Just ONE small subtle chip at most, not jagged or heavily damaged.
- Hammer becomes a primitive gray STONE block on a plain WOODEN handle, retaining the original compact chunky rectangular head silhouette. No metal plate, gems or wrapping. Mostly intact with only one small subtle corner imperfection.
Do not make all L01 objects into ugly broken junk; simple attractive low-tier tools.

2) L03 / L04:
- Hammer L03 center diamond gem becomes clearly leaf GREEN.
- Hammer L04 center diamond gem becomes clearly BLUE.
- Dagger L04 adds a small BLUE gemstone set into its EXISTING original guard, without replacing or reshaping that guard.

3) L05 FIFTH COLUMN:
Remove every red accent from all three L05 weapons.
Replace the large red decorative enamel areas and small red rivets with the SAME DARK NAVY as the handgrip.
Replace the main red gemstones on guard/head and pommel jewel insets with DARK GOLDEN YELLOW / AMBER. Keep original gem shapes and settings. Keep gold trim and silver-blue metal. NO RED anywhere in L05. L06 retains purple gems and must still read a tier above L05. Do not change other columns' jewel colors.

4) HANDLE VIEWPOINT, ESPECIALLY AXES:
Correct only the perspective of the handle butts while retaining the original refined grip shape and decorative cuff. We see the weapon's FRONT, so we must NOT see a separate bottom circular/elliptical surface on the end of a cylindrical grip. Remove that exposed elliptical bottom plane. Turn the end fitting into a clean face-on decorative band across the grip, ending flush at the grip silhouette. Keep grip short, gently rounded and naturally integrated, NOT a long bare rectangular stick. No visible bottom-face disc, no underside hole. Do not change the overall weapon silhouette beyond the minimum correction. Keep the approved original dagger camera and blade shapes.

5) CLEAN CARTOON FINISH, WITHOUT LOSS OF DESIGN:
Keep original rich color combinations, authored construction, original fancy guards and high-tier trim. Clean up the rendering into polished hand-drawn CARTOON game-icon shading: a few broad coherent light/mid/dark color planes per material with sharp boundaries and perfectly smooth even color inside each plane. Remove mottled dirty texture, random tiny scratches, noisy grain and messy multi-facet reflections. Keep enough attractive bevel structure and major ornaments so the original design stays beautiful and recognizable. Do not flatten the whole weapon into one color. Do not over-simplify or strip away the original large decorative construction.
Increase the outermost silhouette stroke slightly, about 15-20%, keep inner detail strokes finer.
No photorealistic surface texture, no painterly smudging. High-quality clean cartoon production concept, detailed where the ORIGINAL DESIGN is detailed, simplified only in noisy surface treatment.

This must look like a carefully corrected edition of THIS EXACT original board, not another redesign. Preserve 90% of its visual identity. Only the listed changes. Output the complete original-layout 30-item board.
```

## Prompt vật liệu và màu ngọc

```text
Make only FIVE specific edits to this board; leave the other 25 weapon designs unchanged.
1. TOP LEFT, dagger L01: entirely carved MATTE WOOD, including blade, guard, grip and butt. Make it clearly wooden with warm brown/tan large flat planes, not shiny bronze or metal. Remove every shiny silver/gold fixture, remove handle wrapping. Keep its original beautiful broad dagger outline.
2. MIDDLE LEFT, axe L01: head becomes simple unadorned GRAY STONE on PLAIN BROWN WOOD HANDLE. Remove riveted metal socket plate, every rivet, all metal collars/end caps, all wrapping. No gem. Only stone block axe head and bare wood stick. Keep original axe outline, one tiny chip at most.
3. BOTTOM LEFT, hammer L01: head becomes simple GRAY STONE block on PLAIN BROWN WOOD HANDLE. Remove diamond central gem, every plate, metal band, wrapping and end cap. Only gray stone block and bare wooden stick. Keep original hammer outline, one tiny chip at most.
4. Bottom row THIRD tile L03: change center diamond hammer gem to vivid GREEN, two simple green facets.
5. Bottom row FOURTH tile L04: change center diamond hammer gem to vivid BLUE, two simple blue facets.
All original weapon forms and all 30 diagonal poses remain. Preserve the original refined designs everywhere else. Output complete board.
```

## Prompt tinh chỉnh

```text
Edit this exact board. Keep its original beautiful weapon silhouettes, elaborate high-tier construction, layout and color backgrounds intact. NO redesign into plain toy shapes.

Correct these specific residual errors:
A. BOTTOM ROW THIRD TILE, on LIME GREEN background: the central diamond gemstone of the HAMMER must be VIVID GREEN (emerald/leaf green), not white or gray.
B. FIRST COLUMN, on gray backgrounds: the THREE L01 weapons must be primitive. Wooden dagger, stone axe, stone hammer.
Replace the whole L01 axe head interior with SIMPLE MATTE GRAY STONE, no riveted plate, no rivets. Keep its same outer axe shape.
Replace the whole L01 hammer head interior with SIMPLE MATTE GRAY STONE, NO CENTRAL DIAMOND, no plates or metal. Keep its same rectangular outer head shape.
No spiderweb cracks or faceted shards on either stone head, use only 3 large clean flat grays.
All three L01 grips must be bare warm-brown wood, NO wraps, rings, bands or caps, ONE simple wood shadow stripe. Dagger blade and guard also matte warm brown wood, not shining bronze.
C. ALL AXE/HAMMER HANDLE BUTTS: remove the visible elliptical BOTTOM-FACING SURFACE and render as frontal decorative cuff ending flush with the grip contour. Keep original short grip shape and original hardware design, but no circular exposed underside plane. These are FRONT-VIEW 2D sprites. Cuff front is a colored band; no ellipse at the very end. Do not turn handles into long square sticks.
D. FIFTH COLUMN L05: main gemstones and any jewel insets golden amber; no red or blue dots. Retain navy enamel and grips.

Keep all high-tier outlines, ornamented guards, trim shapes and head shapes. Clean rendering only: replace tiny scratches, muddy texture and noisy facets with broad coherent smooth light/mid/dark regions; preserve original beauty and material separation. Slightly bolder black outer stroke. Do not simplify away authored ornamentation. All are diagonal compact icons, same board.
```
