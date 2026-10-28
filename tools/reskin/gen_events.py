# Ảnh reskin tab Events theo mockup ArtIncoming/_Concept/Events (canvas 1080x2160 = 1:1 pixel).
# - card_*: 3 thẻ event art giao (990x486, đã có viền bo + vạch màu đáy) → chép nguyên (Co-op chưa dùng: gốc tắt).
# - ribbon_*: ruy băng tên event góc trái thẻ = ruy băng kit Dungeon (152x106), 9-slice ngang L20/R52 để giãn theo chữ
#   (mockup: PvP 152 rộng, Boss Rush ~286).
# - tag_*: ô vé góc phải dưới thẻ = ruy băng lật ngang, 9-slice L52/R20 (mockup ~222 rộng).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_events.py <repo_root>
import sys, os
from PIL import Image, ImageOps

root = sys.argv[1]
ART_IN = os.path.join(root, 'ArtIncoming/_Concept/Events/_art')
DUNGEON_KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Dungeon')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Events')
os.makedirs(KIT, exist_ok=True)


def save(im, name):
    path = os.path.join(KIT, name)
    im.save(path)
    print(os.path.relpath(path, root), im.size)


cards = {'card_pvp.png': 'Layer 738.png',
         'card_coop.png': 'Layer 738 copy.png',
         'card_boss_rush.png': 'Layer 738 copy 2.png'}
for name, src in cards.items():
    save(Image.open(os.path.join(ART_IN, src)).convert('RGBA'), name)

for color in ('red', 'purple', 'blue'):
    im = Image.open(os.path.join(DUNGEON_KIT, 'ribbon_' + color + '.png')).convert('RGBA')
    save(im, 'ribbon_' + color + '.png')
    save(ImageOps.mirror(im), 'tag_' + color + '.png')
