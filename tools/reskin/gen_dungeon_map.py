# Map dungeon từ ảnh user giao ở ArtIncoming/_Concept/<src>/ (map.* + bg.*) → Art/5_Maps/<out>/
#   <name>_map.png    : sàn + vòng tường, 128 px = 1 ô, tâm ảnh = tâm lưới 9x12 → import PPU 128
#   <name>_map_bg.png : nền, phần ảnh gốc rộng đúng màn 1:2 (16.8 ô) → import PPU 64.2857; canvas rộng 1.5 lần
#                       để máy màn rộng không lộ mép. Rìa do Codex vẽ thêm từ ảnh gốc đặt giữa canvas hồng
#                       (gen/prompts.txt), ở đây dán lại ảnh gốc vào giữa cho nét, mờ dần ở mép.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_dungeon_map.py <repo_root> dragon|zombie|cultist
import sys, os
import numpy as np
from PIL import Image, ImageDraw

COLS, ROWS = 9, 12
CELL = 128
EDGE = 8        # chừa mép để không cắt nét đen ngoài của tường
FEATHER = 32

MAPS = {
    # grid = mép lưới 9x12 đo trên map.* (x0, x1, y0, y1)
    'dragon': dict(src='DungeonDragonMap', out='DungeonDragon', bg='bg.webp', grid=(55, 562, 56, 730.2)),
    # post = hộp 1 cột tường trên map.png → cắt làm chướng ngại (ghi đè, giữ cỡ ảnh cũ + guid)
    'zombie': dict(src='DungeonZombieMap', out='DungeonZombie', bg='bg.png', grid=(59, 561, 60, 733),
                   post=(189, 1, 266, 76), obstacle='zombie_dungeon_obstacle.png'),
    'cultist': dict(src='DungeonCultistMap', out='DungeonCultist', bg='bg.webp', grid=(58, 562, 61, 733),
                    post=(353, 3, 426, 78), obstacle='cultist_dungeon_obstacle.png'),
}

root, name = sys.argv[1], sys.argv[2]
cfg = MAPS[name]
SRC = os.path.join(root, 'ArtIncoming/_Concept', cfg['src'])
OUT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/5_Maps', cfg['out'])

gx0, gx1, gy0, gy1 = cfg['grid']
m = Image.open(os.path.join(SRC, 'map.png')).convert('RGBA')
sx = CELL * COLS / (gx1 - gx0)
sy = CELL * ROWS / (gy1 - gy0)
scaled = m.resize((round(m.width * sx), round(m.height * sy)), Image.LANCZOS)
canvas = Image.new('RGBA', ((COLS + 2) * CELL + EDGE * 2, (ROWS + 2) * CELL + EDGE * 2), (0, 0, 0, 0))
canvas.paste(scaled, (round(CELL + EDGE - gx0 * sx), round(CELL + EDGE - gy0 * sy)))
canvas.save(os.path.join(OUT, name + '_map.png'))
print(name + '_map.png', canvas.size)

bg = Image.open(os.path.join(SRC, cfg['bg'])).convert('RGB')
S = bg.width * 3 // 2
wide = np.asarray(Image.open(os.path.join(SRC, 'gen/out/bg_wide.png')).convert('RGB').resize((S, S), Image.LANCZOS)).astype(np.float32)
x0, y0 = (S - bg.width) // 2, (S - bg.height) // 2
ax = np.clip(np.minimum(np.arange(bg.width), np.arange(bg.width)[::-1]) / FEATHER, 0, 1)
ay = np.clip(np.minimum(np.arange(bg.height), np.arange(bg.height)[::-1]) / FEATHER, 0, 1)
mask = np.outer(ay, ax)[..., None]
# Codex hay lệch tông nền vài mức → chỉnh rìa theo trung vị ảnh gốc, nếu không vùng dán lộ thành khung chữ nhật
src = np.asarray(bg, np.float32)
wide *= np.median(src, axis=(0, 1)) / np.median(wide[y0:y0 + bg.height, x0:x0 + bg.width], axis=(0, 1))
wide = np.clip(wide, 0, 255)
core = wide[y0:y0 + bg.height, x0:x0 + bg.width]
wide[y0:y0 + bg.height, x0:x0 + bg.width] = src * mask + core * (1 - mask)
Image.fromarray(wide.round().astype(np.uint8)).save(os.path.join(OUT, name + '_map_bg.png'))
print(name + '_map_bg.png', S, S)

if 'post' in cfg:
    path = os.path.join(OUT, cfg['obstacle'])
    size = Image.open(path).size
    post = m.crop(cfg['post']).resize(size, Image.LANCZOS)
    SS = 4
    r = Image.new('L', (size[0] * SS, size[1] * SS), 0)
    ImageDraw.Draw(r).rounded_rectangle((0, 0, size[0] * SS - 1, size[1] * SS - 1), size[0] * SS * 0.27, fill=255)
    post.putalpha(r.resize(size, Image.LANCZOS))
    post.save(path)
    print(cfg['obstacle'], size)
