# Ảnh cho 3 popup gameplay (NewItem / NewForge / Weapon Offer) theo mockup ArtIncoming/_Concept/Gameplay.
#  - arrow_up / arrow_down / tag_equipped / ribbon_offer = ảnh art giao (đổi tên).
#  - btn_red = Mastery/btn_blue pha màu: fit ma trận màu affine từ nút Equip (xanh) sang nút Sell (đỏ) trên
#    "game play 2 demo.png" — 2 nút cùng dáng, cùng hàng nên so từng điểm ảnh.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_gameplay_popups.py <thư_mục_concept_Gameplay> <btn_blue.png> <thư_mục_ra>
import sys, os, shutil
import numpy as np
from PIL import Image

src_dir, btn_blue, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)

for name, f in [('arrow_up', 'Polygon 2 copy.png'), ('arrow_down', 'Polygon 2 copy 2.png'),
                ('tag_equipped', 'Rectangle 7 copy.png'), ('ribbon_offer', 'Rectangle 7.png')]:
    shutil.copy(os.path.join(src_dir, f), os.path.join(out, name + '.png'))
    print(name)

# Nút Sell (x 217..491) và Equip (x 589..862), y 1849..2000 trên mockup 1080.
m = np.asarray(Image.open(os.path.join(src_dir, 'game play 2 demo.png')).convert('RGB'), np.float64)
blue = m[1855:1995, 600:850].reshape(-1, 3)
red = m[1855:1995, 228:478].reshape(-1, 3)
X = np.hstack([blue, np.ones((len(blue), 1))])
W, *_ = np.linalg.lstsq(X, red, rcond=None)

b = np.asarray(Image.open(btn_blue).convert('RGBA'), np.float64)
rgb = np.hstack([b[..., :3].reshape(-1, 3), np.ones((b.shape[0] * b.shape[1], 1))]) @ W
o = b.copy()
mapped = np.clip(rgb.reshape(b.shape[0], b.shape[1], 3), 0, 255)
# Viền ngoài tối giữ nguyên đen (mockup viền đen), chỉ pha màu phần sáng.
luma = b[..., :3] @ np.array([0.299, 0.587, 0.114])
w = np.clip((luma - 30) / 60.0, 0, 1)[..., None]
o[..., :3] = mapped * w + b[..., :3] * (1 - w)
Image.fromarray(o.astype(np.uint8)).save(os.path.join(out, 'btn_red.png'))
print('btn_red', b.shape[1], b.shape[0])
