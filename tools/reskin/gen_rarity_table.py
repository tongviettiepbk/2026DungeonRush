# Ảnh reskin popup Rarity Table (UILevelPopup) theo mockup ArtIncoming/_Concept/RarityTable/.
# - 10 thanh tier = ảnh art giao (754x68, viền đen 2, bo 12) → đổi tên theo rarity.
# - bar_flash: hình thanh tô trắng (lớp nháy khi lên level), bar_mask: ruột thanh (mask hoa văn 3 tier cuối).
# - xp_track: rãnh thanh exp 608x52, viền đen 4, bo 12, ruột #353B4B (fill dùng lại Lobby/xp_fill).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_rarity_table.py
import os
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtIncoming/_Concept/RarityTable')
OUT = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/RarityTable')
os.makedirs(OUT, exist_ok=True)

# Rarity 0..9 → file art (khớp màu tâm với màu tier cũ trong prefab).
BARS = [
    ('common', 'Rounded Rectangle 586.png'),
    ('uncommon', 'Rounded Rectangle 586 copy.png'),
    ('rare', 'Rounded Rectangle 586 copy 2.png'),
    ('epic', 'Rounded Rectangle 586 copy 3-2.png'),
    ('legendary', 'Rounded Rectangle 586 copy 3-1.png'),
    ('mythic', 'Rounded Rectangle 586 copy 3.png'),
    ('artifact', 'Rounded Rectangle 586 copy 4-3.png'),
    ('ancient', 'Rounded Rectangle 586 copy 4-2.png'),
    ('immortal', 'Rounded Rectangle 586 copy 4-1.png'),
    ('divine', 'Rounded Rectangle 586 copy 4.png'),
]
for name, f in BARS:
    Image.open(os.path.join(SRC, f)).convert('RGBA').save(os.path.join(OUT, f'bar_{name}.png'))

# Lớp nháy: giữ alpha của thanh, tô trắng.
base = Image.open(os.path.join(SRC, BARS[0][1])).convert('RGBA')
white = Image.new('RGBA', base.size, (255, 255, 255, 255))
white.putalpha(base.getchannel('A'))
white.save(os.path.join(OUT, 'bar_flash.png'))

# Mask ruột: điểm sáng (không phải viền đen) của thanh xám, alpha theo độ sáng để mép mượt.
r, g, b, a = base.split()
inner = r.point(lambda v: max(0, min(255, int((v - 40) * 255 / (202 - 40)))))
mask = Image.new('RGBA', base.size, (255, 255, 255, 0))
mask.putalpha(Image.composite(inner, Image.new('L', base.size, 0), a))
mask.save(os.path.join(OUT, 'bar_mask.png'))

# Rãnh thanh exp: vẽ siêu mẫu ×4 rồi thu nhỏ cho mép mượt.
SS = 4
W, H = 608, 52
im = Image.new('RGBA', (W * SS, H * SS), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.rounded_rectangle((0, 0, W * SS - 1, H * SS - 1), 12 * SS, fill=(0, 0, 0, 255))
d.rounded_rectangle((4 * SS, 4 * SS, (W - 4) * SS - 1, (H - 4) * SS - 1), 8 * SS, fill=(0x35, 0x3B, 0x4B, 255))
im.resize((W, H), Image.LANCZOS).save(os.path.join(OUT, 'xp_track.png'))

print('\n'.join(sorted(os.listdir(OUT))))
