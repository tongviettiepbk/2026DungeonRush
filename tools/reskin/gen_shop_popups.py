# Ảnh cho popup Shop (ChestBundleOffer): ruy băng tô màu được theo rarity.
# Gốc tô RibbonImage.color = màu rarity → cần bản TRẮNG/XÁM của Gameplay/ribbon_offer (cam): mặt chính → trắng,
# nếp gấp → xám nhạt, viền đen giữ nguyên; Image.color nhân vào ra đúng màu rarity.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_shop_popups.py
import os
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), '..', '..', '2026DungeonRushUnity', 'Assets', '_Assets', 'Art', '1_UIKit')
src = Image.open(os.path.join(ROOT, 'Gameplay', 'ribbon_offer.png')).convert('RGBA')

# Độ sáng mặt chính (giữa ảnh) làm mốc = 255.
px = src.load()
w, h = src.size
r, g, b, _ = px[w // 2, h // 3]
ref = 0.299 * r + 0.587 * g + 0.114 * b

out = Image.new('RGBA', src.size)
po = out.load()
for y in range(h):
    for x in range(w):
        r, g, b, a = px[x, y]
        lum = (0.299 * r + 0.587 * g + 0.114 * b) / ref
        v = max(0, min(255, round(lum * 255)))
        po[x, y] = (v, v, v, a)

dst = os.path.join(ROOT, 'Shop', 'ribbon_tint.png')
out.save(dst)
print(dst, out.size)
