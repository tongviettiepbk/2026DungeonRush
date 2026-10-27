# Ảnh reskin Dungeon theo mockup ArtIncoming/_Concept/Dungeon (canvas 1080x2160 = 1:1 pixel).
# - card_*: 3 thẻ dungeon art giao (990x486, đã có viền bo + vạch đỏ đáy) → chép nguyên.
# - ribbon_*: ruy băng góc trái thẻ (art giao 152x106) — đỏ/tím/xanh theo thứ tự thẻ trên mockup.
# - tag_*: ô "2/2" góc phải dưới thẻ = ruy băng cùng màu lật ngang (mockup cùng cỡ 152x106, mũi nhọn quay trái).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_dungeon.py <repo_root>
import sys, os
from PIL import Image, ImageOps

root = sys.argv[1]
ART_IN = os.path.join(root, 'ArtIncoming/_Concept/Dungeon/_art')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Dungeon')
os.makedirs(KIT, exist_ok=True)


def save(im, name):
    path = os.path.join(KIT, name)
    im.save(path)
    print(os.path.relpath(path, root), im.size)


cards = {'card_boss_raid.png': 'exec-bdb3efd1-736e-482a-834a-4b59944fedfb.png',
         'card_street_rush.png': 'Layer 606.png',
         'card_mafia_crew.png': 'Layer 608.png'}
for name, src in cards.items():
    save(Image.open(os.path.join(ART_IN, src)).convert('RGBA'), name)

ribbons = {'red': 'Layer 602 copy 2.png', 'purple': 'Layer 620 copy 2.png', 'blue': 'Layer 620 copy 4.png'}
for color, src in ribbons.items():
    im = Image.open(os.path.join(ART_IN, src)).convert('RGBA')
    save(im, 'ribbon_' + color + '.png')
    save(ImageOps.mirror(im), 'tag_' + color + '.png')
