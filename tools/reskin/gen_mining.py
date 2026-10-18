# Ảnh UI + khối mỏ màn Mining từ bộ art giao (ArtIncoming/_Concept/Mining/_art) + số đo mockup "Mining demo.png".
# Phần 1: khung/nút → Art/1_UIKit/Mining/. Phần 2 (cần icon quặng ChatGPT đã fit ở ArtIncoming/5_Maps/Mining/*_icon.png):
#   khối quặng = khối đất art + icon quặng ở giữa → ArtIncoming/5_Maps/Mining/<ore>_mine.png (ghi đè qua Apply Reskin).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_mining.py
import os
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = os.path.join(ROOT, 'ArtIncoming/_Concept/Mining/_art')
DST = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Mining')
INC = os.path.join(ROOT, 'ArtIncoming/5_Maps/Mining')
MAPS = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/5_Maps/Mining')
os.makedirs(DST, exist_ok=True)
SS = 4


def src(name):
    return Image.open(os.path.join(SRC, name + '.png')).convert('RGBA')


def hexc(s, a=255):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rr(self, x0, y0, x1, y1, r, col):
        self.d.rounded_rectangle((x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1), max(r, 0) * SS, fill=col)

    def save(self, path):
        self.im.resize((self.w, self.h), Image.LANCZOS).save(path)
        print(os.path.relpath(path, ROOT), self.w, self.h)


def button3d(w, h, r, O, D, L, F, S, side=12):
    # Cùng khuôn nút kit Mastery (btn_blue): viền O 3, vòng tối D 2, viền sáng L 3, ruột F, đáy dày S.
    c = Canvas(w, h)
    c.rr(0, 0, w, h, r, hexc(O))
    c.rr(3, 3, w - 3, h - 3, r - 3, hexc(S))
    fb = h - 3 - side
    c.rr(3, 3, w - 3, fb, r - 3, hexc(D))
    c.rr(5, 5, w - 5, fb - 2, r - 5, hexc(L))
    c.rr(8, 8, w - 8, fb - 5, r - 8, hexc(F))
    return c


# ----- Phần 1: khung / nút -----
# Ảnh art đổi tên thẳng.
COPY = {
    'btn_square_gray': 'Layer 88',                  # nút vuông xám góc trái (Rarity) 152x157
    'btn_square_yellow': 'Rounded Rectangle 3 copy 6',  # nút vuông vàng góc phải (Offer) 152x157
    'ore_slot': 'Rounded Rectangle 6073 copy 4',    # ô số quặng tan 178x52
    'world_frame': 'Rectangle 11 copy 4',           # khung lưới mỏ 870x870 viền đen 5
    'depth_line': 'Rectangle 508 copy 2',           # vạch độ sâu xanh lá 11x9
}
for dst, name in COPY.items():
    src(name).save(os.path.join(DST, dst + '.png'))
    print(dst, src(name).size)

# Nút dụng cụ xám (không chọn) — đo mockup x=290: O #0D0D0D 3, D #2F2F2F 2, L #9D9D9D 3, ruột #868686, đáy #535353 12.
button3d(276, 154, 17, '#0D0D0D', '#2F2F2F', '#9D9D9D', '#868686', '#535353').save(os.path.join(DST, 'btn_gray.png'))

# Thanh level mỏ 494x58: viền đen 4, rãnh #353B4B; ruột vàng 2 dải #F5CE6F / #ECAE63 (đo x=350).
c = Canvas(64, 58); c.rr(0, 0, 64, 58, 14, hexc('#000000')); c.rr(4, 4, 60, 54, 10, hexc('#353B4B'))
c.save(os.path.join(DST, 'depth_track.png'))
c = Canvas(64, 50); c.rr(0, 0, 64, 50, 10, hexc('#ECAE63')); c.rr(0, 0, 64, 25, 10, hexc('#F5CE6F'))
c.d.rectangle((0, 12 * SS, 64 * SS - 1, 25 * SS - 1), fill=hexc('#F5CE6F'))
c.save(os.path.join(DST, 'depth_fill.png'))

# Sao giảm giá 8 cánh 180x180 (popup Offer): viền đen 4, viền vàng #FFF949 10, ruột đỏ #E4475C.
import math
def star(rout, rin):
    pts = []
    for i in range(16):
        r = rout if i % 2 == 0 else rin
        a = math.pi * 2 * i / 16 - math.pi / 2
        pts.append((90 * SS + r * SS * math.cos(a), 90 * SS + r * SS * math.sin(a)))
    return pts
c = Canvas(180, 180)
c.d.polygon(star(88, 66), fill=hexc('#000000'))
c.d.polygon(star(82, 61), fill=hexc('#FFF949'))
c.d.polygon(star(70, 52), fill=hexc('#E4475C'))
c.save(os.path.join(DST, 'badge_discount.png'))

# Khối đá (Stone) riêng cho mỏ — KHÔNG ghi đè 5_Maps/Campaign/stone_obstacle.png (map campaign dùng chung).
src('Layer 5031 copy').save(os.path.join(MAPS, 'stone_block.png'))
print('5_Maps/Mining/stone_block.png 100x100')

# ----- Phần 2: khối quặng = khối đất + icon quặng (icon chiếm 68% khối như mockup) -----
dirt = src('Layer 502 copy')
os.makedirs(INC, exist_ok=True)
dirt.save(os.path.join(INC, 'dirt.png'))
ORES = ['coal', 'diamond', 'emerald', 'gold', 'iron', 'ruby']
for ore in ORES:
    p = os.path.join(ROOT, 'ArtIncoming/_Concept/Mining/gen/keyed', ore + '.png')
    if not os.path.exists(p):
        print('chưa có icon', ore)
        continue
    icon = Image.open(p).convert('RGBA')
    icon = icon.crop(icon.getbbox())
    S = 400  # vẽ to rồi thu về 100 cho icon sắc
    base = dirt.resize((S, S), Image.LANCZOS)
    k = 0.68 * S / max(icon.size)
    icon = icon.resize((round(icon.width * k), round(icon.height * k)), Image.LANCZOS)
    base.alpha_composite(icon, ((S - icon.width) // 2, (S - icon.height) // 2))
    base.resize((100, 100), Image.LANCZOS).save(os.path.join(INC, ore + '_mine.png'))
    print('ArtIncoming/5_Maps/Mining/' + ore + '_mine.png')
