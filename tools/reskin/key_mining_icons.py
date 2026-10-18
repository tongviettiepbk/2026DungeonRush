# Icon Mining vẽ bằng ChatGPT (ArtIncoming/_Concept/Mining/gen/out) → tách nền → ghi đè tại chỗ qua ArtIncoming/5_Maps/Mining.
# - keyed/<id>.png: bản lớn đã tách nền (gen_mining.py dùng ghép khối quặng).
# - <ore>_icon.png, mining (1).png: đúng W×H + hộp bao hình cũ (fit_icon_to_old).
# - *_character.png (dụng cụ cầm tay trong mỏ) = bản icon dụng cụ mới (đợt Shop), cùng kích thước.
# diamond vẽ nền XANH LÁ (#00FF00) vì thạch anh tím gần màu magenta: đảo màu → tách như magenta → đảo lại.
# Dùng: tools/.venv/bin/python -I tools/reskin/key_mining_icons.py
import os, sys
from PIL import Image, ImageOps

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
sys.path.insert(0, os.path.dirname(__file__))
from fit_icon_to_old import key_magenta

GEN = os.path.join(ROOT, 'ArtIncoming/_Concept/Mining/gen')
ART = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/5_Maps/Mining')
INC = os.path.join(ROOT, 'ArtIncoming/5_Maps/Mining')
os.makedirs(os.path.join(GEN, 'keyed'), exist_ok=True)
os.makedirs(INC, exist_ok=True)

GREEN_BG = {'diamond'}
TARGET = {o: o + '_icon.png' for o in ['stone', 'coal', 'iron', 'emerald', 'gold', 'diamond', 'ruby']}
TARGET['mining_rarity'] = 'mining (1).png'


def key(id_):
    src = os.path.join(GEN, 'out', id_ + '.png')
    if id_ not in GREEN_BG:
        return key_magenta(src)
    tmp = os.path.join(GEN, 'keyed', '_inv.png')
    ImageOps.invert(Image.open(src).convert('RGB')).save(tmp)
    o = key_magenta(tmp)
    os.remove(tmp)
    r, g, b, a = o.split()
    return Image.merge('RGBA', (*ImageOps.invert(Image.merge('RGB', (r, g, b))).split(), a))


def fit(o, old_path):
    ref = Image.open(old_path).convert('RGBA')
    ow, oh = ref.size
    bl, bt, br, bb = ref.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox()
    bw, bh = br - bl, bb - bt
    k = min(bw / o.width, bh / o.height)
    o = o.resize((max(1, round(o.width * k)), max(1, round(o.height * k))), Image.LANCZOS)
    c = Image.new('RGBA', (ow, oh), (0, 0, 0, 0))
    c.paste(o, (bl + (bw - o.width) // 2, bt + (bh - o.height) // 2))
    return c


for id_, name in TARGET.items():
    if not os.path.exists(os.path.join(GEN, 'out', id_ + '.png')):
        print('chưa có', id_)
        continue
    o = key(id_)
    o.save(os.path.join(GEN, 'keyed', id_ + '.png'))
    fit(o, os.path.join(ART, name)).save(os.path.join(INC, name))
    print(name, Image.open(os.path.join(ART, name)).size)

for tool in ['pickaxe', 'drill', 'gold_pickaxe']:
    a = Image.open(os.path.join(ART, tool + '.png'))
    b = Image.open(os.path.join(ART, tool + '_character.png'))
    assert a.size == b.size, (tool, a.size, b.size)
    a.save(os.path.join(INC, tool + '_character.png'))
    print(tool + '_character.png', a.size)
