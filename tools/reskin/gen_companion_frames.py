# Ảnh màn Companion theo mockup ArtIncoming/_Concept/Companion (canvas 1080x2160 = 1:1 pixel).
#  - Ảnh art giao sẵn → chép + đặt tên theo vai trò.
#  - Thanh tiến độ (track) vẽ bằng code theo số đo mockup: cao 46, bo 12, viền đen 3, ruột #19191C.
#  - Nút vàng (Summon quảng cáo) = pha màu affine từ Lobby/btn_green sang cột nút vàng đo trên mockup.
#  - Tay chỉ tutorial = hand.png của art xoay cho ngón chỉ xuống, ghi đè Common/arrow-down.png (giữ khung 256).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_companion_frames.py <thư_mục_art_Companion> <thư_mục_Art_Unity>
import sys, os
from PIL import Image, ImageDraw

src = sys.argv[1]
art = sys.argv[2]
out = os.path.join(art, '1_UIKit/Companion')
os.makedirs(out, exist_ok=True)
SS = 4

# 1) Ảnh art giao sẵn
copies = {
    'Rounded Rectangle 1 copy 9.png': 'overlay_equipped.png',   # đen 60% — ô đang mặc
    'Rounded Rectangle 1 copy 12.png': 'overlay_locked.png',    # đen 50% — pet chưa sở hữu
    'Rounded Rectangle 10 copy 3.png': 'plus_gray.png',         # dấu + ô slot trống
    'Rounded Rectangle 9 copy.png': 'btn_add.png',              # + xanh góc ô
    'Rounded Rectangle 9.png': 'btn_remove.png',                # − xanh góc ô
    'Polygon 1.png': 'icon_ad.png',                             # icon quảng cáo
}
for a, b in copies.items():
    im = Image.open(os.path.join(src, a)).convert('RGBA')
    if b.startswith('overlay_'):
        # Project Linear color space: UI trộn alpha trong không gian linear → đen alpha a làm tối nền theo (1-a)^(1/2.2)
        # thay vì (1-a) như Photoshop. Bù alpha để ra đúng độ tối mockup: a' = 1 - (1-a)^2.2.
        px = im.load()
        for y in range(im.size[1]):
            for x in range(im.size[0]):
                r_, g_, b_, al = px[x, y]
                if al:
                    px[x, y] = (r_, g_, b_, round(255 * (1 - (1 - al / 255) ** 2.2)))
    im.save(os.path.join(out, b))
    print(b)

# 2) Thanh tiến độ
w, h, r = 48, 46, 12
im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.rounded_rectangle((0, 0, w * SS - 1, h * SS - 1), r * SS, fill=(0, 0, 0, 255))
d.rounded_rectangle((3 * SS, 3 * SS, (w - 3) * SS - 1, (h - 3) * SS - 1), (r - 3) * SS, fill=(0x19, 0x19, 0x1C, 255))
im.resize((w, h), Image.LANCZOS).save(os.path.join(out, 'bar_track.png'))
print('bar_track.png')

# 3) Nút vàng: fit ma trận màu từ btn_green sang cột nút vàng mockup (x=60/75, y1621-1779)
mock = Image.open(os.path.join(src, 'companion demo.png')).convert('RGB')
green = Image.open(os.path.join(art, '1_UIKit/Lobby/btn_green.png')).convert('RGBA')


def solve(A, b):
    n = len(A); M = [A[i][:] + [b[i]] for i in range(n)]
    for i in range(n):
        p = max(range(i, n), key=lambda q: abs(M[q][i])); M[i], M[p] = M[p], M[i]
        for q in range(n):
            if q != i:
                f = M[q][i] / M[i][i]; M[q] = [M[q][k] - f * M[i][k] for k in range(n + 1)]
    return [M[i][n] / M[i][i] for i in range(n)]


small = green.convert('RGB').resize((277, 158), Image.LANCZOS)
G = [small.getpixel((x, y)) for x in (17, 32) for y in range(158)]
T = [mock.getpixel((x, y)) for x in (60, 75) for y in range(1621, 1779)]
X = [list(c) + [1.0] for c in G]
XtX = [[sum(X[k][i] * X[k][j] for k in range(len(X))) + (1e-3 if i == j else 0) for j in range(4)] for i in range(4)]
W = [solve(XtX, [sum(X[k][i] * T[k][c] for k in range(len(X))) for i in range(4)]) for c in range(3)]
o = green.copy(); px = o.load()
for y in range(o.size[1]):
    for x in range(o.size[0]):
        rr, gg, bb, a = px[x, y]
        if a == 0:
            continue
        v = (rr, gg, bb, 1.0); m = [sum(W[c][i] * v[i] for i in range(4)) for c in range(3)]
        lum = 0.3 * rr + 0.59 * gg + 0.11 * bb
        t = max(0.0, min(1.0, (lum - 25) / 60))  # viền tối giữ nâu-đen như mockup (#1F1200)
        px[x, y] = tuple(max(0, min(255, round(t * m[c] + (1 - t) * (0x1F, 0x12, 0x00)[c]))) for c in range(3)) + (a,)
o.save(os.path.join(out, 'btn_yellow.png'))
print('btn_yellow.png')

# 4) Tay chỉ: hand.png (ngón chỉ sang trái) xoay 90° → ngón chỉ xuống, đặt vào khung 256 theo bbox tay cũ
hand = Image.open(os.path.join(src, 'hand.png')).convert('RGBA').rotate(90, expand=True)
hand = hand.crop(hand.getbbox())
old_path = os.path.join(art, '1_UIKit/Common/arrow-down.png')
old = Image.open(old_path).convert('RGBA')
bx = old.getbbox()
bw, bh = bx[2] - bx[0], bx[3] - bx[1]
s = min(bw / hand.width, bh / hand.height)
hand = hand.resize((round(hand.width * s), round(hand.height * s)), Image.LANCZOS)
canvas = Image.new('RGBA', old.size, (0, 0, 0, 0))
# căn đáy (đầu ngón tay) theo tay cũ, giữa theo chiều ngang
canvas.paste(hand, (bx[0] + (bw - hand.width) // 2, bx[3] - hand.height), hand)
canvas.save(sys.argv[3] if len(sys.argv) > 3 else os.path.join(out, '_hand_preview.png'))
print('hand', old.size, bx, hand.size)
