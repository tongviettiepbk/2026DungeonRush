# Ảnh reskin Boss Rush theo mockup ArtIncoming/_Concept/BossRush (canvas 1080x2160 = 1:1 pixel).
# - Thanh máu boss, dòng xếp hạng xám/xanh, thanh damage: art giao (_art/) đổi tên → Art/1_UIKit/BossRush/.
# - Dòng sảnh trong mockup có bóng đáy (#656565 xám / #1E74AC xanh, 8px) mà art không có → ghép bản lệch 8px tô màu bóng
#   nằm dưới art (viền đáy của art nhường chỗ cho bóng).
# - Vệt trắng chạy trễ của thanh máu = bản trắng của fill; viên tối của ô damage = pill trắng tô màu bằng Image.color.
# - Icon nắm đấm (Power) ChatGPT vẽ ở gen/out/fist.png → tách nền magenta.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_boss_rush.py <repo_root>
import sys, os
import numpy as np
from PIL import Image, ImageDraw

root = sys.argv[1]
CON = os.path.join(root, 'ArtIncoming/_Concept/BossRush')
ART = os.path.join(CON, '_art')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/BossRush')
os.makedirs(KIT, exist_ok=True)
sys.path.insert(0, os.path.dirname(__file__))


def art(name):
    return Image.open(os.path.join(ART, name)).convert('RGBA')


def save(im, name):
    im.save(os.path.join(KIT, name))
    print(name, im.size)


# Art giữ nguyên
save(art('Rounded Rectangle 949.png'), 'hp_track.png')
save(art('Rounded Rectangle 949 copy 2.png'), 'hp_fill.png')
save(art('Rounded Rectangle 983.png'), 'row_gray.png')
save(art('Rounded Rectangle 983 copy 2.png'), 'row_blue.png')
save(art('Rounded Rectangle 985.png'), 'dmg_track.png')
save(art('Rounded Rectangle 985 copy.png'), 'dmg_fill.png')

# Vệt trắng: giữ alpha của fill, sáng 2 tông như fill (tông dưới tối hơn) để code tô màu
a = np.asarray(art('Rounded Rectangle 949 copy 2.png')).astype(np.float32)
lum = a[..., :3].max(axis=2, keepdims=True) / 238.0
a[..., :3] = np.clip(lum * 255, 0, 255)
save(Image.fromarray(a.astype(np.uint8)), 'hp_fill_white.png')


# Dòng sảnh cao có bóng đáy (mockup: fill → viền sáng → bóng 8px → nét đen 3px)
def tall_row(src, shadow_rgb, out):
    A = np.asarray(art(src)).astype(np.int32)
    h, w = A.shape[:2]
    dark = (A[..., :3].max(axis=2) < 40) & (A[..., 3] > 0)
    B = A.copy()
    B[..., :3][~dark] = shadow_rgb          # bản bóng: giữ nét đen, ruột = màu bóng
    res = np.zeros((h + 8, w, 4), np.int32)
    res[8:] = B
    top = np.zeros_like(res)
    top[:h] = A
    tdark = np.zeros((h + 8, w), bool)
    tdark[:h] = dark
    bdark = np.zeros((h + 8, w), bool)
    bdark[8:] = dark
    bopaque = res[..., 3] > 0
    # A đè lên B, trừ nét đen của A nằm trên ruột B (nét đáy A nhường cho bóng)
    use_top = (top[..., 3] > 0) & ~(tdark & bopaque & ~bdark)
    res[use_top] = top[use_top]
    save(Image.fromarray(res.astype(np.uint8)), out)


tall_row('Rounded Rectangle 983.png', (0x65, 0x65, 0x65), 'row_gray_tall.png')
tall_row('Rounded Rectangle 983 copy 2.png', (0x1E, 0x74, 0xAC), 'row_blue_tall.png')

# Pill trắng bo 12 (ô damage tối, tô màu đen bán trong suốt bằng Image.color)
SS = 4
p = Image.new('RGBA', (48 * SS, 48 * SS), (0, 0, 0, 0))
ImageDraw.Draw(p).rounded_rectangle((0, 0, 48 * SS - 1, 48 * SS - 1), 12 * SS, fill=(255, 255, 255, 255))
save(p.resize((48, 48), Image.LANCZOS), 'pill_white.png')

# Icon nắm đấm (ChatGPT) → tách nền magenta, cắt sát, cỡ 128
fist = os.path.join(CON, 'gen/out/fist.png')
if os.path.exists(fist):
    from fit_icon_to_old import key_magenta
    im = key_magenta(fist)
    s = 128 / max(im.size)
    im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    c = Image.new('RGBA', (128, 128), (0, 0, 0, 0))
    c.paste(im, ((128 - im.width) // 2, (128 - im.height) // 2), im)
    save(c, 'icon_fist.png')
