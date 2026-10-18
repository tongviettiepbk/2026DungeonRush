# Ảnh UI Shop từ bộ art giao (ArtIncoming/_Concept/Shop/_art) → Art/1_UIKit/Shop/
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_shop.py
import os
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = os.path.join(ROOT, 'ArtIncoming/_Concept/Shop/_art')
DST = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Shop')
os.makedirs(DST, exist_ok=True)

def src(name):
    return Image.open(os.path.join(SRC, name + '.png')).convert('RGBA')

# Ảnh art đổi tên thẳng.
COPY = {
    'card_boost': 'Layer 60',          # hàng Boost Exp x2 Forever 1006x208
    'deal_orange': 'Group 3',          # thẻ deal cam 1006x302
    'deal_purple': 'Group 3 copy 2',   # thẻ deal tím 1006x302
    'chest_card': 'Layer 66',          # thẻ rương dọc 466x608
    'card_tan': 'Layer 68 copy',       # thẻ Boosters/Resources đầu vàng 308x422
    'gem_card': 'Layer 71',            # thẻ gói gem đầu vàng 268x378
}
for out, name in COPY.items():
    src(name).save(os.path.join(DST, out + '.png'))

# Ruy băng tiêu đề mục 956x100 → đệm trong suốt 25px mỗi bên để giãn cả bề ngang 1006 vẫn hụt vào như mockup.
rib = src('Rounded Rectangle 12')
pad = Image.new('RGBA', (rib.width + 50, rib.height), (0, 0, 0, 0))
pad.paste(rib, (25, 0))
pad.save(os.path.join(DST, 'ribbon.png'))

# Ô thưởng deal = đen 20% → project Linear làm nhạt lớp đen bán trong suốt, bù alpha a' = 1-(1-a)^2.2.
slot = src('Rounded Rectangle 11')
px = slot.load()
for y in range(slot.height):
    for x in range(slot.width):
        r, g, b, a = px[x, y]
        if a:
            px[x, y] = (r, g, b, round((1 - (1 - a / 255) ** 2.2) * 255))
slot.save(os.path.join(DST, 'reward_slot.png'))
print('xong', sorted(os.listdir(DST)))
