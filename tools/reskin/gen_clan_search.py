# Ảnh reskin 2 popup Clan Search (UIAdvancedSearchPopup) + Enter Clan Name (UICreateClanPopup)
# theo mockup ArtIncoming/_Concept/ClanSearch (canvas 1080x2160 = 1:1 pixel).
# - Nút −/+ : art giao (_art/btn_minus, btn_plus 100x110 có sẵn bóng) → chép thẳng.
# - Ô nhập tên tan: hình học → vẽ code theo đo mockup (viền đen 3px, gờ sáng #EFD7BF 4px, ruột #DEAF7F, bo góc ngoài 13).
# - Dấu tick xanh lá: ChatGPT (gen/out/check_green.png, nền magenta) → tách nền, cắt sát, 128x128 như check.png.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_clan_search.py <repo_root>
import sys, os, shutil
import numpy as np
from PIL import Image, ImageDraw

root = sys.argv[1]
CON = os.path.join(root, 'ArtIncoming/_Concept/ClanSearch')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/ClanSearch')
os.makedirs(KIT, exist_ok=True)
SS = 4

# Nút −/+ art giao.
shutil.copy(os.path.join(CON, '_art/btn_minus.png'), os.path.join(KIT, 'btn_minus.png'))
shutil.copy(os.path.join(CON, '_art/btn_plus.png'), os.path.join(KIT, 'btn_plus.png'))

# Ô nhập tan 64x64 (9-slice viền 20).
W = H = 64
im = Image.new('RGBA', (W * SS, H * SS), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
for inset, r, col in ((0, 13, (0, 0, 0)), (3, 10, (0xEF, 0xD7, 0xBF)), (7, 6, (0xDE, 0xAF, 0x7F))):
    d.rounded_rectangle([inset * SS, inset * SS, (W - inset) * SS - 1, (H - inset) * SS - 1], radius=r * SS, fill=col + (255,))
im.resize((W, H), Image.LANCZOS).save(os.path.join(KIT, 'input_tan.png'))

# Dấu tick xanh lá từ ChatGPT: nền ≈ #FF00FF → trong suốt, khử viền dải 3px sát nền.
src = os.path.join(CON, 'gen/out/check_green.png')
if os.path.exists(src):
    a = np.asarray(Image.open(src).convert('RGBA')).astype(np.float32)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    bg = (r > 200) & (b > 200) & (g < 70)
    alpha = np.where(bg, 0, 255).astype(np.float32)
    # viền: pixel pha magenta (r,b cao hơn g nhiều) gần nền → giảm alpha theo độ magenta
    from PIL import ImageFilter
    near = np.asarray(Image.fromarray((bg * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(7))) > 0
    mag = np.clip((np.minimum(r, b) - g) / 255.0, 0, 1)
    edge = near & ~bg
    alpha[edge] = 255 * (1 - mag[edge])
    a[..., 3] = alpha
    # trả màu viền về tối (bỏ ánh hồng)
    a[edge, 0] = np.minimum(a[edge, 0], a[edge, 1] + 20)
    a[edge, 2] = np.minimum(a[edge, 2], a[edge, 1] + 20)
    a[alpha < 26] = 0
    out = Image.fromarray(a.astype(np.uint8))
    box = out.getbbox()
    out = out.crop(box)
    s = 120 / max(out.size)
    out = out.resize((max(1, round(out.size[0] * s)), max(1, round(out.size[1] * s))), Image.LANCZOS)
    canvas = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    canvas.paste(out, ((128 - out.size[0]) // 2, (128 - out.size[1]) // 2), out)
    canvas.save(os.path.join(KIT, 'check_green.png'))
print('ok', os.listdir(KIT))
