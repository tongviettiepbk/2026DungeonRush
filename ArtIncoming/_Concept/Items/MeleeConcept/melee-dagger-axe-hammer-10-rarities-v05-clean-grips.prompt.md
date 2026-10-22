# Melee concept v05 — cán chính diện, mảng màu sạch hơn

Ngày: 2026-10-22. Tạo bằng built-in ImageGen.

Ảnh cuối: `melee-dagger-axe-hammer-10-rarities-v05-clean-grips.png`.

User chỉ rõ v04 còn hai lỗi: texture lem nhem và mặt đáy cán rìu/búa sai phối cảnh. Ảnh `v05-reference-frontal-dagger.png` minh hoạ góc chính diện chấp nhận được; ảnh `v05-reference-handle-perspective-error.png` khoanh mặt đáy elip cần bỏ. Đây là ảnh chỉ dẫn của user, không phải mục tiêu sao chép thiết kế.

Định hướng cần giữ từ đây:
- Cả vũ khí nằm trong mặt tranh 2D, xoay khoảng 45 độ. Không được thấy mặt đáy hoặc nắp trụ hình elip ở đuôi cán.
- Cán kết thúc bằng cạnh phẳng, màu được chia thành ít mảng khối rõ; không dùng đốm đen/viên đá ở đáy để gợi trụ nghiêng.
- Mỗi vật liệu dùng ít mảng sáng/tối lớn, đều màu và ranh giới rõ. Tránh facet vụn, nét ngẫu nhiên và texture loang; giản lược trang trí đầu búa.
- Giữ màu nền Artifact xanh jade đậm và Ancient navy, không biến thành mint sáng và xanh lơ.
- Bộ 3 món × 10 rarity vẫn là bảng concept, chưa tách sprite. Chưa có xác nhận user rằng v05 đạt artstyle cuối.

## Prompt dựng lại cán phẳng

```text
Use case: stylized-concept.
Generate a FRESH new impeccably clean flat cartoon mobile RPG inventory board, exactly thirty single weapons. Use the attached small dagger ONLY as a reference for flat frontal broad-face projection and dark chunky silhouette outlines. Do not reproduce its uneven texture. DO NOT recreate 3D cylindrical handles.

BOARD: wide landscape, 10 columns x 3 rows of square rounded tiles, all equally sized, 30 complete items. Column headers L01 L02 L03 L04 L05 L06 L07 L08 L09 L10; row labels DAGGER, AXE, HAMMER. Solid cream outer background, black tile borders. Square tile backgrounds in order gray, beige, lime, sky blue, yellow, lavender, jade green, royal blue, coral red, orange. Flat uniform fills.
Each weapon is a compact front elevation tilted 45 degrees within its flat picture plane, handle lower-left and weapon head upper-right. Each whole weapon fits in its tile.

HANDLE CONSTRUCTION: THIS IS MANDATORY:
All 30 grips are simple FLAT TWO-DIMENSIONAL TRAPEZOIDS with a FLAT STRAIGHT lower end, as if drawn on paper. There are NO metal end caps, NO pommel hardware, NO rounded butt fittings. The lower end of every grip is simply one straight heavy black line connecting the two long grip edges. NOTHING extends beyond that line. NO circle, NO oval, NO ellipse, NO dark spot, NO end grain, NO visible bottom face. Do not draw cylinders or 3D rods. The grip can have a two-tone frontal shadow stripe, but no cylindrical lighting. One flat rectangular guard band near the head is okay; the bottom of the grip is always PLAIN AND UNADORNED. For ALL DAGGERS, ALL AXES and ALL HAMMERS.
Handle and blade/head are in the SAME FLAT FRONTAL PLANE. No perspective thickness or top side behind the head; no round pegs protrude above heads. Absolutely no isometric projection.

WEAPONS:
Row DAGGER: short chunky broad leaf-point dagger, small simple crossguard, short plain trapezoid grip. Center guard gemstone L03-L10.
Row AXE: short one-handed bearded single-headed hatchet, broad blade with simple concave lower edge, short plain trapezoid grip ending in straight line. Simple small gemstone set on FRONT of head socket at L03-L10.
Row HAMMER: rectangular hammer block in perfectly flat broad-face front view, rotated with grip at 45 degrees; no head thickness side/top surfaces. Large simple center diamond gem L02-L10, no gem L01. Short plain trapezoid grip with flat end.

PALETTES BY COLUMN:
L01 cheapest: entirely wooden dagger (tan/brown/dark brown); axe and hammer simple mostly intact gray stone heads on bare brown wooden sticks, no wrapping, no metal, no crack patterns, a single tiny corner chip at most.
L02 iron gray/silver with brown grip.
L03 pale blue steel, slate/olive hardware, brown grip, GREEN gem.
L04 pale blue steel, dark navy grip, BLUE gem (including dagger and hammer).
L05 silver-blue metal, dark navy grip, gold guard/trim and DARK AMBER GOLDEN-YELLOW gems, no red anywhere.
L06 pale lavender metal, deep purple grip, gold fittings and PURPLE gems.
L07 mint metal, forest green grip, gold fittings and GREEN gems.
L08 ice cyan-white metal, navy grip, BLUE gems.
L09 dark purple/near-black with ruby RED gems and lavender edge plane.
L10 ivory metal, gold trim, cream grip and pale AQUA gems.
Keep richer tiers elegantly simple. Absolutely NO scribbled gold ornament, NO filigree, no tiny decorative marks. One simple flat gold border or band and central jewel is enough.

RENDERING MUST BE EXTRAORDINARILY CLEAN:
Professional flat cartoon UI asset illustration, visual finish of crisp vector shapes, but deliver raster image. Bold consistent black outer lines, thinner interior lines.
EVERY SURFACE consists of exactly TWO OR THREE LARGE SOLID UNIFORM COLOR SHAPES with CRISP HARD EDGES showing LIGHT, MID and DARK. Every area has a single perfectly constant color. Blade split into a broad pale plane and broad medium blue plane with tiny dark base. Stone head into only three large gray polygons. Each grip into two flat uniform frontal shapes. NOT a uniform unshaded object: 2-3 intentionally separate flat tones show volume graphically.
NO TEXTURE. NO noise. NO grain. NO smudging. NO mottling. NO watercolor. NO soft brush. NO gradients. NO random semi-transparent triangles. NO glossy reflections. NO realistic metal. NO tiny scratches, cracks or woodgrain. NO faceted confetti shapes. Gems are flat geometric 2-tone insets, not glossy beads and no white round highlight.
All shape boundaries and linework flawlessly smooth, clean and anti-aliased. Simple generous readable silhouettes, compact items. Entire 30-item board from one coherent professional cartoon inventory asset set.

FINAL HARD CHECK: Every handle is a flat trapezoid ending in ONE STRAIGHT LINE, nothing circular at bottom. All 30 are front-view cutout silhouettes at 45 degrees. All colored patches are smooth completely solid color, no texture.
```

## Prompt làm sạch mảng màu

```text
STYLE REFERENCE = image 1 (original simple equipment icons).
EDIT TARGET = image 2 (30-item board with flat-ended grips).
Redraw image 2 using the VERY SIMPLE CLEAN COLOR BLOCKS of image 1. Keep image 2's 30 weapon silhouettes, flat-ended grips, diagonal poses, grid and labels. The flat-ended grips have been corrected and MUST STAY FLAT: do not add any cap, ellipse, round end face or hardware to the ends of handles.

Specific problem to REMOVE: image 2 has lots of messy irregular facets, reflective white shards and soft tonal patches on hammer heads and blade surfaces. These are WRONG. REMOVE THEM COMPLETELY.

Use an extremely simple 2-COLOR SHADING SYSTEM on each material:
- Each dagger blade: just TWO LARGE UNIFORM regions separated by ONE STRAIGHT diagonal line lengthwise: light material color, darker material color. No extra triangle at tip. No reflection fragments.
- Each axe blade: just TWO LARGE UNIFORM regions: flat medium-color face and one broad flat lighter cutting-edge strip. No third color fragment and no reflection shards.
- Each HAMMER HEAD: one perfectly UNIFORM medium-color rectangular main face covering about 80%, and ONE simple dark-color shadow strip along the lower-right edge about 20%. NO OTHER FACETS OR MARKS. A simple light outer bevel band may be a single continuous uniform color around top-left, not a collection of patches. Large center gemstone remains. No polygons floating across the face, no cracks, no gold scribble lines. High tiers have only a plain gold border and one gemstone.
- Each handle: just TWO flat color regions, no texture, no grain, no diagonal stripes, no wrapping, no mottling, no reflections.
- Every gemstone: simple 2-tone uniform geometric split, NO white highlight dot, NO glossy surface.

All color boundaries are crisp. All interiors are solid clean even opaque colors. Absolutely NO gradients, NO noise, NO painterly look, NO realistic reflections, NO translucent layers, NO fragmentary facets. Keep color shapes as simple and clear as image 1's tiny sprites. Do not add detail in the larger target just because there is room. Flat vector-style finished cartoon game assets with thick black outlines.

Restore exact rarity tile colors, all UNIFORM FLAT FILLS:
L01 gray #CACACA
L02 beige #E4C79C
L03 lime #B5E15A
L04 sky blue #3FC2F4
L05 yellow #FFE064
L06 lavender #A385E8
L07 FOREST JADE GREEN #4AA577 (NOT bright mint)
L08 DEEP ROYAL NAVY BLUE #3C48BF (NOT sky blue)
L09 coral red #EB6365
L10 golden orange #FFB932.
Preserve item palette roles from target: L03 green gems, L04 blue gems, L05 amber gems with navy grips and gold, L06 purple/gold, L07 mint/forest green/gold, L08 white/cyan/navy, L09 purple-black/red, L10 ivory/gold/aqua. L01 wood dagger, plain gray stone axe/hammer on wooden grips.
Keep all 30 simple FLAT ENDED handles like image 2, black straight end line. Same square tiles and complete 3-by-10 board.
```
