# Xuất ảnh reskin Battle Pass từ bộ art giao (ArtIncoming/_Concept/BattlePass) → Art/1_UIKit/BattlePass/
# - đổi tên ảnh art theo vai trò (khung, ruy băng, ô thưởng 3 màu, nhãn mốc 3 màu, thanh mốc, khoá, chấm than)
# - panel_inner: lớp đen 30% (Rounded Rectangle 603) ÉP SẴN lên vùng gradient của khung → ảnh đục,
#   tránh lệch màu do project Linear color space (lớp đen bán trong suốt ra nhạt hơn Photoshop)
# - btn_orange (nút giá Premium): pha màu ô xanh lá → cam, fit ma trận màu affine từ ô xanh sang nút cam đo trên mockup
# - icon_check: ảnh ChatGPT (gen/out/icon_check_big.png, nền magenta) → tách nền bằng key_magenta_icon.py trước khi chạy
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_battle_pass.py
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtIncoming/_Concept/BattlePass')
OUT = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/BattlePass')
os.makedirs(OUT, exist_ok=True)


def art(name):
    return Image.open(os.path.join(SRC, name)).convert('RGBA')


RENAME = {
    'Layer 87.png': 'popup_frame.png',
    'Rectangle 7.png': 'ribbon.png',
    'Rounded Rectangle 3 copy 2-1.png': 'cell_blue.png',
    'Rounded Rectangle 3 copy 2.png': 'cell_green.png',
    'Rounded Rectangle 3 copy 2-2.png': 'cell_gray.png',
    'Rounded Rectangle 605.png': 'level_blue.png',
    'Rounded Rectangle 605 copy.png': 'level_green.png',
    'Rounded Rectangle 605 copy 2.png': 'level_gray.png',
    'Rectangle 8 copy 3-1.png': 'track.png',
    'Rectangle 8 copy 3.png': 'track_fill.png',
    'Rounded Rectangle 6071.png': 'icon_lock.png',
    '_.png': 'icon_alert.png',
}
for src, dst in RENAME.items():
    art(src).save(os.path.join(OUT, dst))

# Khung popup ở (84,306) trên mockup, panel trong ở (107,804) → lệch (23,498) trong khung.
frame = art('Layer 87.png')
inner = art('Rounded Rectangle 603.png')
under = frame.crop((23, 498, 23 + inner.width, 498 + inner.height))
baked = Image.alpha_composite(under, inner)
alpha = inner.getchannel('A').point(lambda a: 255 if a > 0 else 0)
baked.putalpha(alpha)
baked.save(os.path.join(OUT, 'panel_inner.png'))

# Nút cam (nút giá Premium): ô xanh lá → cam theo bảng màu đo trên mockup (viền nâu đỏ, viền sáng, thân, bóng đáy).
# Mỗi điểm ảnh chiếu lên đoạn nối 2 màu khoá gần nhất → nội suy sang 2 màu đích (giữ khử răng cưa).
KEYS = [((0, 0, 0), (78, 8, 0)), ((157, 255, 146), (250, 193, 123)),
        ((105, 234, 133), (242, 167, 80)), ((61, 197, 99), (205, 116, 44))]


def remap(c):
    best = None
    for i in range(len(KEYS)):
        for j in range(len(KEYS)):
            if i == j:
                continue
            a, b = KEYS[i][0], KEYS[j][0]
            d = [b[k] - a[k] for k in range(3)]
            dd = sum(v * v for v in d)
            t = max(0.0, min(1.0, sum((c[k] - a[k]) * d[k] for k in range(3)) / dd))
            q = [a[k] + t * d[k] for k in range(3)]
            err = sum((c[k] - q[k]) ** 2 for k in range(3))
            if best is None or err < best[0]:
                best = (err, i, j, t)
    _, i, j, t = best
    A, B = KEYS[i][1], KEYS[j][1]
    return tuple(round(A[k] + t * (B[k] - A[k])) for k in range(3))


btn = art('Rounded Rectangle 3 copy 2.png')
px = btn.load()
for y in range(btn.height):
    for x in range(btn.width):
        r, g, b_, a = px[x, y]
        if a < 200:   # bóng đổ mờ dưới nút giữ đen
            continue
        px[x, y] = remap((r, g, b_)) + (a,)
btn.save(os.path.join(OUT, 'btn_orange.png'))

chk = os.path.join(SRC, 'gen/out/icon_check_big_keyed.png')
if os.path.exists(chk):
    im = Image.open(chk).convert('RGBA')
    im = im.crop(im.getbbox())
    im.thumbnail((160, 160), Image.LANCZOS)
    im.save(os.path.join(OUT, 'icon_check.png'))
print('xong', sorted(os.listdir(OUT)))
