# Lobby đợt 3 (mockup "lobby demo 4", 2026-10-28): chép art giao + cắt ghép phần thiếu từ chính mockup.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_lobby2.py <thư_mục_art_giao> <thư_mục_Assets/_Assets/Art>
# Nhóm A (ghi đè giữ đúng W×H ảnh cũ): 10 khung rarity, tier_11 pha từ khung tím mới, auto-forge, icon_boost, panel_companion.
# Nhóm B (ảnh mới ở 1_UIKit/Lobby/): profile_box + gem_box (CẮT TỪ MOCKUP — art chưa giao, chờ thay), chest_bar, tab_bar,
#   tab_selected, chat_off.
import sys, os
from PIL import Image

src, art = sys.argv[1], sys.argv[2]
S = lambda n: os.path.join(src, n)
A = lambda n: os.path.join(art, n)
mock = Image.open(S('lobby demo 4.png')).convert('RGBA')

def save(im, rel):
    im.save(A(rel)); print(rel, im.size)

# ---- 10 khung rarity: tên file giao cũ ↔ bậc (khớp màu đợt trước) + 2 file mới cho bậc 7/8
RARITY = {1: 'Rounded Rectangle 1.png', 2: 'Rounded Rectangle 1 copy.png', 3: 'Rounded Rectangle 1 copy 2.png',
          4: 'Rounded Rectangle 1 copy 3-2.png', 5: 'Rounded Rectangle 1 copy 3-1.png', 6: 'Rounded Rectangle 1 copy 3.png',
          7: 'Rounded Rectangle 1 copy 15.png', 8: 'Rounded Rectangle 1 copy 16.png', 9: 'Rounded Rectangle 1 copy 4-1.png',
          10: 'Rounded Rectangle 1 copy 4.png'}
for t, n in RARITY.items():
    save(Image.open(S(n)).convert('RGBA'), '1_UIKit/Rarity/tier_%02d_background.png' % t)

# bậc 11 chưa có art: pha từ khung tím mới (fill #A288F2) sang tím đậm, nhân theo tỉ lệ kênh (viền đen giữ đen)
im = Image.open(S('Rounded Rectangle 1 copy 3.png')).convert('RGBA'); px = im.load()
SRC, DST = (162, 136, 242), (110, 75, 196)
for y in range(im.size[1]):
    for x in range(im.size[0]):
        r, g, b, a = px[x, y]
        if a: px[x, y] = tuple(min(255, round(c * d / s)) for c, s, d in zip((r, g, b), SRC, DST)) + (a,)
save(im, '1_UIKit/Rarity/tier_11_icon_background.png')

# ---- icon: đệm đúng tỉ lệ ảnh cũ rồi đưa về đúng W×H cũ
def fit(new, w, h):
    im = Image.open(S(new)).convert('RGBA'); bw, bh = im.size
    s = min(w / bw, h / bh)
    im = im.resize((round(bw * s), round(bh * s)), Image.LANCZOS)
    c = Image.new('RGBA', (w, h), (0, 0, 0, 0)); c.paste(im, ((w - im.width) // 2, (h - im.height) // 2)); return c
save(fit('09_auto.png', 128, 128), '6_Features/Forge/auto-forge.png')
save(fit('10_boost_x2.png', 87, 128), '1_UIKit/Lobby/icon_boost.png')

# ---- nền hàng pet: dải dọc đo trên mockup (y 1004..1011 viền đen + vệt sáng), fill #706F7D; giữ 1080x188 như cũ
panel = Image.new('RGBA', (1080, 188), (0x70, 0x6F, 0x7D, 255))
for y, c in enumerate(['000000'] * 4 + ['9D9EA6', '9899A1', '8E8E98', '7F7F8B', '757482']):
    panel.paste(tuple(int(c[i:i + 2], 16) for i in (0, 2, 4)) + (255,), (0, y, 1080, y + 1))
save(panel, '1_UIKit/Lobby/panel_companion.png')

# ---- hộp avatar: cắt từ mockup x55..308, y44..144 — mép trái 12px (x67 đã dính viền chữ "(") + cột fill sạch x=298 + mép phải x296..308
L = mock.crop((55, 44, 67, 145)); M = mock.crop((298, 44, 299, 145)); R = mock.crop((296, 44, 309, 145))
box = Image.new("RGBA", (12 + 14 + 13, 101))
box.paste(L, (0, 0))
for x in range(12, 26): box.paste(M, (x, 0))
box.paste(R, (26, 0))
for y in range(box.height):            # nền top bar ở 4 góc → trong suốt
    for x in range(box.width):
        p = box.getpixel((x, y))
        if abs(p[0] - 0x70) + abs(p[1] - 0x6F) + abs(p[2] - 0x7D) < 12: box.putpixel((x, y), (0, 0, 0, 0))
save(box, '1_UIKit/Lobby/profile_box.png')

# ---- ô gem: nửa phải sạch của ô mockup (x962..1024, y60..139) + lật làm nửa trái
Rg = mock.crop((962, 60, 1024, 139))
gem = Image.new('RGBA', (124, 79)); gem.paste(Rg.transpose(Image.FLIP_LEFT_RIGHT), (0, 0)); gem.paste(Rg, (62, 0))
for y in range(gem.height):
    for x in range(gem.width):
        p = gem.getpixel((x, y))
        if abs(p[0] - 0x70) + abs(p[1] - 0x6F) + abs(p[2] - 0x7D) < 12: gem.putpixel((x, y), (0, 0, 0, 0))
save(gem, '1_UIKit/Lobby/gem_box.png')

# ---- art giao dùng thẳng
for n, rel in [('Rectangle 1129.png', 'chest_bar'), ('Rectangle 1.png', 'tab_bar'),
               ('Rectangle 5 copy.png', 'tab_selected'), ('Rectangle 1133.png', 'chat_off')]:
    save(Image.open(S(n)).convert('RGBA'), '1_UIKit/Lobby/%s.png' % rel)
