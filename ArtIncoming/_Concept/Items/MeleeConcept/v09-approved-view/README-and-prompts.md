# Melee v09 — bộ 30 món theo mẫu một mặt đã chốt

Ngày: 2026-10-22. Công cụ: built-in ImageGen.

## File bàn giao concept

- `melee-daggers-L01-L10-v09.png`: 10 dao găm, L01–L10.
- `melee-axes-L01-L10-v09.png`: 10 rìu, L01–L10.
- `melee-hammers-L01-L10-v09.png`: 10 búa, L01–L10.
- Mỗi bảng 5 cột × 2 hàng: trên L01–L05, dưới L06–L10. Cả bộ đủ 30 món, đã đặt trên nền rarity.
- `source-30-item-designs.png`: bảng user yêu cầu vẽ lại, dùng làm nguồn dáng và phối màu, không dùng làm chuẩn phối cảnh.
- Mẫu chuẩn góc nhìn/style/nét: `../axe-L04-single-face-v02-bold.png`. User đã xác nhận góc chuẩn và yêu cầu dùng để vẽ lại bộ item.

## Quy tắc giữ cho các lần sửa sau

Chỉ một mặt trước cho mọi bộ phận, không mặt trên/bên/đáy; vũ khí xoay trong mặt tranh khoảng 45 độ. Nét đen đậm, mảng khối sáng tối sạch. Giữ nhận diện dao lá bản rộng, rìu bearded, búa bản chữ nhật và các motif cấp cao từ concept đầu. Tránh dùng lại camera 3D của các bảng cũ.

Đã sửa riêng L01 của cả ba bảng về cán gỗ trơn; dao L01 toàn gỗ, rìu/búa L01 đầu xám đá, không ngọc, không kim loại trang trí. Ngọc dao L04 xanh; ngọc búa L03 xanh lá, L04 xanh lam; L05 navy/gold với ngọc amber, không đỏ.

Đây là ảnh concept; chưa tách thành 30 sprite nền trong suốt, chưa thay asset trong Unity. Ba bảng mới chưa được user duyệt riêng.

## Rìu — prompt tạo bảng 10 món

```text
Create a NEW finished concept sheet of ONLY TEN AXES, a 5-column by 2-row grid, using reference image 1's EXACT APPROVED flat one-face cartoon style and BOLD BLACK line weight.

Reference 1: THE MASTER. All ten axes must be variations of THIS FRONT-FACING SINGLE-PLANE AXE: same beautiful compact bearded blade silhouette, short grip ending in one outer contour, flat socket plate with no top/side face, straight front grip bands. This reference's graphic finish and viewpoint OVERRIDE reference 2.
Reference 2: ONLY the middle row's TEN RARITY DESIGNS AND COLORS to adapt. Do NOT copy its 3D bevel/camera or thin lines. Do NOT draw the daggers/hammers. Redraw the axe row in the unmistakable single-face geometry of reference 1.

SHEET: 10 square rounded black-bordered tiles, 5 columns x 2 rows. Labels above each tile: top row L01 L02 L03 L04 L05; bottom row L06 L07 L08 L09 L10. No other text. Uniform offwhite outside. Every whole axe tilted 45 degrees, grip lower left and head upper right, centered with comfortable padding. Broad blade FRONT is seen, socket FRONT is seen, handle FRONT is seen. These are decorated FLAT CUTOUTS with NO physical thickness or top/bottom/side face. Exactly as reference 1. No elliptical grip end surface, no extra socket ledge. Grip bands straight like master.

IDENTITIES:
L01 gray tile, simple matte gray stone bearded blade on bare wood grip, no rivets, no crack web, no metal fittings, no gems.
L02 beige tile, silver-gray blade with dark gray flat riveted plate and brown grip.
L03 lime tile, pale blue blade, olive/slate flat plate with simple rivets, brown grip.
L04 sky-blue tile: reproduce the approved reference 1 axe with its muted olive socket, silver-blue blade and navy grip, 3 front rivets. MUST look the same.
L05 yellow tile, gold socket/trim and clean open gold angular motif ON navy-blue blade inset, silver cutting edge, navy grip. All gems dark amber. No red.
L06 lavender tile, gold socket/trim, purple inlay and angular gold motif, lavender-silver edge, purple grip.
L07 jade green tile #4AA577, gold socket/trim and a simple vine/leaf-like graphic over mint/forest green blade, green grip.
L08 royal navy tile #3C48BF, icy white/cyan blade with a few intentional large ice shapes, blue socket and navy grip.
L09 coral red tile, near-black/purple socket and blade with crimson graphic front motif, lavender cutting edge and deep purple grip.
L10 orange tile, gold socket/trim and ivory blade front, aqua simple decorative motif, ivory grip.
These are the same high-tier design languages as the middle row in reference 2, mapped onto master's flat front view, not newly invented unrelated weapons.

STYLE: EXACTLY reference 1: very bold smooth black contour about 3-4% of weapon width, readable internal black lines, 2-3 broad CLEAN UNIFORM solid-color light/mid/dark shapes per material. Smooth even fills, no mottling or scratches. No tiny facet confetti, no 3D render highlights, no airbrush texture. Keep high-tier gold framework and major motifs elegant and recognizable but flat on the single face. Do not make fronts blank.
Output one complete high-resolution clean 10-axe sheet, 5x2 grid, all labels correct.
```

## Dao — prompt tạo bảng 10 món

```text
Create one polished TEN-ICON concept sheet, 5 columns by 2 rows. Top labels L01 L02 L03 L04 L05, bottom labels L06 L07 L08 L09 L10, no other text. Same tile sizes, spacing, clean offwhite outside, square rounded black-bordered tiles as reference 3.
Reference 1 = USER-APPROVED geometry and art master: exactly ONE FRONT FACE, very bold black outer contours, clean 2-3 broad light/mid/dark color patches, no texture. Every part is a flat front shape, with no visible top, bottom or side surface. Handle is a short shaped strip with straight wrap bands, no ellipse or circular bottom end. All items diagonally 45 degrees in the page, grip lower left and head upper right. Do not draw 3D or isometric rendering.
Reference 2 = weapon DESIGN IDENTITIES and palettes ONLY. Retain the requested family's silhouette, major ornamentation and each rarity's character, redrawn in reference 1's viewpoint and finish.
Reference 3 = layout, level of line boldness and graphic finish for the companion sheet already produced; the new sheet must belong to this same matching set.
Tile background colors in order: gray #CACACA, beige #E4C79C, lime #B5E15A, sky blue #3FC2F4, yellow #FFE064, lavender #A385E8, forest jade #4AA577, royal navy #3C48BF, coral red #EB6365, golden orange #FFB932.
Colors: L01 primitive wood/stone, L02 iron/brown, L03 steel/olive/brown, L04 cold blue steel/navy, L05 silver-blue/navy/gold and amber gems NO RED, L06 lavender/purple/gold, L07 mint/forest green/gold, L08 ice cyan/white/navy, L09 deep purple-black/crimson, L10 ivory/gold/aqua.
All whole items contained with margins, exactly one weapon per tile, no pairs. Bold black silhouette contour roughly 3-4% of weapon width, interior lines thinner. Clean uniform broad fills, crisp hard-edged tonal separation, smooth edges, no noisy facets, no grain, no cracks, no soft muddy shading. Preserve authored ornamental silhouettes from reference 2, don't erase high-tier beauty.
SUBJECT: ONLY TEN DAGGERS, using the TOP ROW of reference 2.
Keep original short BROAD asymmetric leaf/drop-point blade silhouette with a small concave notch at its base, tiny shaped crossguard and short grip. Never turn it into a generic long sword.
The blade, guard, grip and front bands each show one front face only. Guard has NO upper/side thickness ledges. No exposed grip-bottom surface. Keep ornate flared/hooks/star guard outlines on L05-L10 as FLAT FRONT GRAPHIC SHAPES.
L01 ENTIRELY matte warm wooden dagger, wood blade+guard+bare plain wood grip with NO metal and NO wrapping, only large tan/brown planes.
L02 silver/iron blade and brown grip, simple silver guard.
L03 pale steel blade, slate/olive guard, brown grip.
L04 pale blue steel, olive/slate guard, navy grip, BLUE square guard gem.
L05 silver-blue blade, ornate gold guard, navy grip and dark AMBER square gem, gold angular blade-base inlay, NO RED.
L06 lavender blade, gold guard and PURPLE gem, purple grip.
L07 mint blade, gold/green guard and GREEN gem, forest-green grip.
L08 ice white/cyan blade, icy blue guard and BLUE gem, navy grip.
L09 near-black/deep purple blade with lavender front highlight, crimson gem and purple guard/grip.
L10 ivory blade, elaborate golden frontal guard, AQUA square gem, cream grip.
Keep L05 less luxurious than L06, no red at L05. Output full 5x2 ten-dagger sheet.
```

## Búa — prompt tạo bảng 10 món

```text
Create one polished TEN-ICON concept sheet, 5 columns by 2 rows. Top labels L01 L02 L03 L04 L05, bottom labels L06 L07 L08 L09 L10, no other text. Same tile sizes, spacing, clean offwhite outside, square rounded black-bordered tiles as reference 3.
Reference 1 = USER-APPROVED geometry and art master: exactly ONE FRONT FACE, very bold black outer contours, clean 2-3 broad light/mid/dark color patches, no texture. Every part is a flat front shape, with no visible top, bottom or side surface. Handle is a short shaped strip with straight wrap bands, no ellipse or circular bottom end. All items diagonally 45 degrees in the page, grip lower left and head upper right. Do not draw 3D or isometric rendering.
Reference 2 = weapon DESIGN IDENTITIES and palettes ONLY. Retain the requested family's silhouette, major ornamentation and each rarity's character, redrawn in reference 1's viewpoint and finish.
Reference 3 = layout, level of line boldness and graphic finish for the companion sheet already produced; the new sheet must belong to this same matching set.
Tile background colors in order: gray #CACACA, beige #E4C79C, lime #B5E15A, sky blue #3FC2F4, yellow #FFE064, lavender #A385E8, forest jade #4AA577, royal navy #3C48BF, coral red #EB6365, golden orange #FFB932.
Colors: L01 primitive wood/stone, L02 iron/brown, L03 steel/olive/brown, L04 cold blue steel/navy, L05 silver-blue/navy/gold and amber gems NO RED, L06 lavender/purple/gold, L07 mint/forest green/gold, L08 ice cyan/white/navy, L09 deep purple-black/crimson, L10 ivory/gold/aqua.
All whole items contained with margins, exactly one weapon per tile, no pairs. Bold black silhouette contour roughly 3-4% of weapon width, interior lines thinner. Clean uniform broad fills, crisp hard-edged tonal separation, smooth edges, no noisy facets, no grain, no cracks, no soft muddy shading. Preserve authored ornamental silhouettes from reference 2, don't erase high-tier beauty.
SUBJECT: ONLY TEN WAR HAMMERS, using the BOTTOM ROW of reference 2.
Keep compact original wide rectangular head and opposing FLAT frontal end flanges, large centered jewel, short grip. Head front fully faces viewer. Absolutely NO visible cuboid TOP or SIDE thickness plane, no raised end shelves. End flanges and central gem settings are decorated front-view graphic regions in one plane. Head and handle both face viewer. Keep original authored gold/star/ice motifs at high tiers.
L01 simple matte GRAY STONE block head, MOSTLY INTACT with one tiny edge imperfection, on a bare plain brown wooden handle. NO metal plates, NO gemstone, NO rivets, NO wrapping, NO cracks.
L02 dark brown/gray metal head front with silver end strips, simple silver diamond inset, brown grip.
L03 dark slate/olive head and pale steel flanges, GREEN center diamond gem, brown grip.
L04 blue/slate head and pale steel flanges, BLUE center diamond gem, navy grip.
L05 navy/blue-silver head, gold frame around AMBER diamond gem, navy grip and gold bands, NO RED, restrained elegant front ornament.
L06 lavender-silver/purple head with gold frame and PURPLE center diamond gem, purple grip.
L07 mint/forest green head with gold STAR-shaped setting and GREEN gemstone, green grip.
L08 ice white/cyan head with simple clean large ice motif around BLUE star/crystal gem, navy grip.
L09 near-black purple head with dark star setting and RED/crimson gemstone, purple grip.
L10 ivory/gold head with golden sun/star setting and AQUA gem, cream grip.
Keep major motifs beautiful but no filigree scribbles, no realistic reflection shards. Output full 5x2 ten-hammer sheet.
```

## Dao — sửa cán L01

```text
Edit ONLY L01, the UPPER LEFT tile on gray background, in this ten-item sheet. Keep all other nine tiles completely unchanged, same layout, labels, bold linework and single-face geometry.
The L01 HANDLE currently has black diagonal grip-wrapping lines and tan end/neck bands. REMOVE EVERY BLACK WRAPPING LINE and REMOVE THE COLORED END AND NECK BANDS from that L01 handle. Replace the entire handle interior with a PLAIN BARE WOODEN GRIP: one smooth warm-brown base region and one simple darker brown front-shadow region, no transverse lines, no leather, no rope, no collars, no metal caps. Keep the same short grip outline and one-face front projection. Outer black outline stays, black transverse stripes inside L01 grip must all disappear.
The entire first dagger remains wood, including blade and guard. Keep its same silhouette and broad wood-tonal blade planes.
Do not modify L02-L10. Output the complete same 5x2 sheet, only L01 corrected.
```

## Rìu — sửa cán và vật liệu L01

```text
Edit ONLY L01, the UPPER LEFT tile on gray background, in this ten-item sheet. Keep all other nine tiles completely unchanged, same layout, labels, bold linework and single-face geometry.
The L01 HANDLE currently has black diagonal grip-wrapping lines and tan end/neck bands. REMOVE EVERY BLACK WRAPPING LINE and REMOVE THE COLORED END AND NECK BANDS from that L01 handle. Replace the entire handle interior with a PLAIN BARE WOODEN GRIP: one smooth warm-brown base region and one simple darker brown front-shadow region, no transverse lines, no leather, no rope, no collars, no metal caps. Keep the same short grip outline and one-face front projection. Outer black outline stays, black transverse stripes inside L01 grip must all disappear.
L01 head must be matte GRAY STONE. Keep its broad axe outline, use medium gray main face and a LIGHT GRAY edge strip instead of bright white shiny steel. No rivets, gem or metal fittings in L01. The brown rectangular attachment band also becomes part of the plain wood. Do not add cracks or heavy chipping.
Do not modify L02-L10. Output the complete same 5x2 sheet, only L01 corrected.
```

## Búa — sửa cán L01

```text
Edit ONLY L01, the UPPER LEFT tile on gray background, in this ten-item sheet. Keep all other nine tiles completely unchanged, same layout, labels, bold linework and single-face geometry.
The L01 HANDLE currently has black diagonal grip-wrapping lines and tan end/neck bands. REMOVE EVERY BLACK WRAPPING LINE and REMOVE THE COLORED END AND NECK BANDS from that L01 handle. Replace the entire handle interior with a PLAIN BARE WOODEN GRIP: one smooth warm-brown base region and one simple darker brown front-shadow region, no transverse lines, no leather, no rope, no collars, no metal caps. Keep the same short grip outline and one-face front projection. Outer black outline stays, black transverse stripes inside L01 grip must all disappear.
L01 head stays matte gray STONE with the same shape and large gray planes, no jewel, no metal, no cracks. Keep the approved one-face front view.
Do not modify L02-L10. Output the complete same 5x2 sheet, only L01 corrected.
```
