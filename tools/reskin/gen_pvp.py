# Ảnh reskin PvP theo mockup ArtIncoming/_Concept/PvP (canvas 1080x2160 = 1:1 pixel).
# - Dòng xám/xanh (746x122, có bóng đáy sẵn), khiên kiếm chéo, 2 vạch xanh lá: art giao (_art/) đổi tên → Art/1_UIKit/PvP/.
# - Ô tối (avatar, ô trophy cuối dòng) = đen alpha 0.3 → project Linear làm nhạt → bù alpha a' = 1-(1-a)^2.2.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_pvp.py <repo_root>
import sys, os
import numpy as np
from PIL import Image

root = sys.argv[1]
ART = os.path.join(root, 'ArtIncoming/_Concept/PvP/_art')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/PvP')
os.makedirs(KIT, exist_ok=True)


def art(name):
    return Image.open(os.path.join(ART, name)).convert('RGBA')


def save(im, name):
    im.save(os.path.join(KIT, name))
    print(name, im.size)


save(art('Rounded Rectangle 3 copy 6.png'), 'row_gray.png')
save(art('Rounded Rectangle 3 copy 7.png'), 'row_blue.png')
save(art('exec-70ee37fb-a00b-4f8c-9500-1611b5415b8d.png'), 'icon_shield.png')
save(art('Layer 684 copy 2.png'), 'line_left.png')
save(art('Layer 684 copy.png'), 'line_right.png')

a = np.asarray(art('Rounded Rectangle 574 copy 4.png')).astype(np.float32)
alpha = a[..., 3] / 255.0
a[..., 3] = (1 - (1 - alpha) ** 2.2) * 255
save(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)), 'slot_dark.png')
