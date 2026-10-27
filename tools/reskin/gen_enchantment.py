# Ảnh reskin Enchantment theo mockup ArtIncoming/_Concept/Enchantment (canvas 1080x2160 = 1:1 pixel).
# - btn_yellow: nút multiplier vàng cạnh Summon — fit ma trận màu affine từ nút xanh Summon sang nút vàng
#   ngay bên trái trên mockup (cùng hàng y1730-1882), áp lên Lobby/btn_green (giữ dáng/viền).
# - arrow_right: mũi tên popup Merge = Gameplay/arrow_up xoay 90° (art giao, viền đen dày).
# - Hình ô trống 3 slot đã đổi đồ (Wing→kính, Cape→tẩu, Gloves→áo): ChatGPT vẽ sticker trên nền xanh lá
#   (gen/out/empty_*.png) → chuyển thành bóng xám mờ đúng bảng màu ô trống cũ (viền 69, bóng 94, ruột 150,
#   alpha 140), giữ ĐÚNG W×H ảnh cũ (riêng áo: khung găng cũ 166x100 bè ngang làm áo bé → vuông 136x136) → ghi vào ArtIncoming/3_Equipment/00_EmptySlot/ để Apply Reskin.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_enchantment.py <repo_root>
import sys, os
import numpy as np
from PIL import Image

root = sys.argv[1]
CON = os.path.join(root, 'ArtIncoming/_Concept/Enchantment')
ART = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art')
KIT = os.path.join(ART, '1_UIKit/Enchantment')
os.makedirs(KIT, exist_ok=True)


def save(im, path):
    im.save(path)
    print(os.path.relpath(path, root), im.size)


# ----- Nút vàng -----
mock = np.asarray(Image.open(os.path.join(CON, 'enchanment demo.png')).convert('RGB')).astype(np.float64)


def cols(xs, y0=1726, y1=1890):
    return np.concatenate([mock[y0:y1, x - 3:x + 4].reshape(-1, 3) for x in xs])


G = cols([430, 445, 640, 655])
Y = cols([235, 250, 315, 330])
X = np.hstack([G, np.ones((len(G), 1))])
W, *_ = np.linalg.lstsq(X, Y, rcond=None)
err = np.abs(X @ W - Y).mean()
print('fit vàng sai số TB', round(err, 2))
src = np.asarray(Image.open(os.path.join(ART, '1_UIKit/Lobby/btn_green.png')).convert('RGBA')).astype(np.float64)
rgb = np.hstack([src[..., :3].reshape(-1, 3), np.ones((src.shape[0] * src.shape[1], 1))]) @ W
out = src.copy()
out[..., :3] = np.clip(rgb.reshape(src.shape[0], src.shape[1], 3), 0, 255)
save(Image.fromarray(out.astype(np.uint8)), os.path.join(KIT, 'btn_yellow.png'))

# ----- Mũi tên phải -----
save(Image.open(os.path.join(ART, '1_UIKit/Gameplay/arrow_up.png')).convert('RGBA').rotate(-90, expand=True),
     os.path.join(KIT, 'arrow_right.png'))

# ----- Hình ô trống -----
EMPTY = os.path.join(ART, '3_Equipment/00_EmptySlot')
INC = os.path.join(root, 'ArtIncoming/3_Equipment/00_EmptySlot')
pairs = {'empty_glasses': 'empty_icon_wing_transparent.png',
         'empty_pipe': 'empty_icon_cloak_transparent.png',
         'empty_shirt': 'empty_icon_gloves_transparent.png'}
for gen, old in pairs.items():
    p = os.path.join(CON, 'gen/out', gen + '.png')
    if not os.path.exists(p):
        print('chưa có', p)
        continue
    a = np.asarray(Image.open(p).convert('RGB')).astype(np.float64)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    # Nền xanh lá #00FF00: g cao, r/b thấp. Mép khử răng cưa → alpha theo độ "xanh" trong dải mép.
    green = (g - np.maximum(r, b)) / 255.0
    alpha = np.clip(1.0 - (green - 0.25) / 0.45, 0, 1)
    lum = a.max(axis=2)
    obj = alpha > 0.5
    # Viền đen + nét trong → 69; tông bóng tối của hình → 94; ruột → 150 (bảng màu ô trống cũ).
    inner = lum[obj & (lum > 90)]
    split = np.percentile(inner, 35) if inner.size else 160
    tone = np.where(lum <= 90, 69, np.where(lum < split, 94, 150))
    o = np.zeros(a.shape[:2] + (4,), np.uint8)
    o[..., 0] = o[..., 1] = o[..., 2] = tone
    o[..., 3] = (alpha * 140).astype(np.uint8)
    im = Image.fromarray(o)
    im = im.crop(im.getbbox())
    ow, oh = (136, 136) if gen == 'empty_shirt' else Image.open(os.path.join(EMPTY, old)).size
    s = min(ow / im.width, oh / im.height)
    im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    canvas = Image.new('RGBA', (ow, oh), (0, 0, 0, 0))
    canvas.paste(im, ((ow - im.width) // 2, (oh - im.height) // 2))
    os.makedirs(INC, exist_ok=True)
    save(canvas, os.path.join(INC, old))
