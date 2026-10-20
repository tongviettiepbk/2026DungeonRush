# Ảnh reskin màn Clan theo mockup ArtIncoming/_Concept/Clan (canvas 1080x2160 = 1:1 pixel).
# - Khung/nút/thanh art giao (_art/) đổi tên → Art/1_UIKit/Clan/ (lớp đen bán trong suốt bù alpha Linear).
# - Hình học mockup không có trong art (ô tối viền mảnh, thanh tab + viên cam) vẽ bằng code theo số đo.
# - 8 khung cờ banner vẽ lại theo style banner art giao (thanh treo vàng + viền vàng + nét đen), giữ đúng hình
#   lớp màu cũ (clan_flag_0X_color) để hệ banner ghép (khung + lớp màu tô + icon tô) vẫn chạy như cũ
#   → ghi ra ArtIncoming/6_Features/Clan/ cho Apply Reskin (cùng cỡ 256, giữ GUID).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_clan.py <repo_root>
import sys, os
import numpy as np
from PIL import Image, ImageDraw

root = sys.argv[1]
ART = os.path.join(root, 'ArtIncoming/_Concept/Clan/_art')
OLD = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/6_Features/Clan')
KIT = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/Clan')
INC = os.path.join(root, 'ArtIncoming/6_Features/Clan')
os.makedirs(KIT, exist_ok=True)
os.makedirs(INC, exist_ok=True)
SS = 4


def art(name):
    return Image.open(os.path.join(ART, name)).convert('RGBA')


def bake_linear(im):
    # Lớp đen bán trong suốt trên UI Linear hiện nhạt hơn Photoshop → a' = 1-(1-a)^2.2
    a = np.asarray(im).astype(np.float32)
    al = a[..., 3] / 255.0
    a[..., 3] = (1 - (1 - al) ** 2.2) * 255
    return Image.fromarray(a.clip(0, 255).astype(np.uint8))


def hexc(s, a=255):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


def rr_image(w, h, layers):
    # layers: [(inset, radius, color)] vẽ chồng từ ngoài vào trong, siêu lấy mẫu cho viền mượt
    im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for inset, r, col in layers:
        if isinstance(inset, tuple):
            l, t, rgt, b = inset
        else:
            l = t = rgt = b = inset
        d.rounded_rectangle((l * SS, t * SS, (w - rgt) * SS - 1, (h - b) * SS - 1), max(r, 0) * SS, fill=col)
    return im.resize((w, h), Image.LANCZOS)


# ---------- 1. Ảnh art giao → kit ----------
art('Rounded Rectangle 3 copy 11.png').save(os.path.join(KIT, 'card_dark.png'))      # thẻ clan / MVP / thành viên
art('Rounded Rectangle 3 copy 6.png').save(os.path.join(KIT, 'card_gray.png'))       # dòng thành viên popup, mốc chưa đạt
art('Rounded Rectangle 3 copy 7.png').save(os.path.join(KIT, 'card_blue.png'))       # mốc thưởng đã đạt
art('Rounded Rectangle 698.png').save(os.path.join(KIT, 'input_dark.png'))           # ô tìm clan
art('Rounded Rectangle 710 copy 3.png').save(os.path.join(KIT, 'icon_search.png'))   # kính lúp
art('Rectangle 801.png').save(os.path.join(KIT, 'war_bar_track.png'))                # thanh điểm war: nền đen
art('Rectangle 801 copy.png').save(os.path.join(KIT, 'war_bar_blue.png'))
art('Rectangle 801 copy 2.png').save(os.path.join(KIT, 'war_bar_red.png'))
bake_linear(art('Rectangle 804.png')).save(os.path.join(KIT, 'war_bar_divider.png'))
bake_linear(art('Ellipse 750.png')).save(os.path.join(KIT, 'dot_status.png'))
# Thanh mốc dọc popup Rewards: art giao nằm ngang → xoay 90° ngược chiều kim (nửa sáng sang trái như mockup).
art('Rectangle 2.png').rotate(90, expand=True).save(os.path.join(KIT, 'vbar_track.png'))
art('Rectangle 2 copy 4.png').rotate(90, expand=True).save(os.path.join(KIT, 'vbar_fill.png'))

# ---------- 2. Hình học vẽ code ----------
# Ô tối viền mảnh (khung thông báo clan, dòng hành động war): viền #19191E 3px, ruột #302C3D, bo 20.
rr_image(64, 64, [(0, 20, hexc('19191E')), (3, 17, hexc('302C3D'))]).save(os.path.join(KIT, 'box_dark.png'))
# Thanh tab Member/Battle: rãnh 88 cao viền đen 3, ruột #18161E; viên chọn cam viền đen 4 + vành vàng 4.
rr_image(120, 88, [(0, 44, hexc('000000')), (3, 41, hexc('18161E'))]).save(os.path.join(KIT, 'tab_track.png'))
rr_image(120, 88, [(0, 44, hexc('000000')), (4, 40, hexc('FDCD51')), (8, 36, hexc('EFA14F'))]).save(os.path.join(KIT, 'tab_pill.png'))
# Vạch ngăn khu trên/dưới (bevel đo mockup y 701..709): 56,52,72 / 73,67,93 / 82,75,104 x2 / 24,22,31 x4.
sep = Image.new('RGBA', (4, 8))
rows = [(56, 52, 72), (73, 67, 93), (82, 75, 104), (82, 75, 104), (24, 22, 31), (24, 22, 31), (24, 22, 31), (24, 22, 31)]
for y, c in enumerate(rows):
    for x in range(4):
        sep.putpixel((x, y), c + (255,))
sep.save(os.path.join(KIT, 'separator.png'))


# ---------- 3. Khung cờ banner ----------
def disc_offsets(r):
    return [(dx, dy) for dy in range(-r, r + 1) for dx in range(-r, r + 1) if dx * dx + dy * dy <= r * r + r * 0.5]


def dilate(m, r):
    out = np.zeros_like(m)
    H, W = m.shape
    for dx, dy in disc_offsets(r):
        sx0, sx1 = max(0, -dx), min(W, W - dx)
        sy0, sy1 = max(0, -dy), min(H, H - dy)
        out[sy0 + dy:sy1 + dy, sx0 + dx:sx1 + dx] |= m[sy0:sy1, sx0:sx1]
    return out


def erode(m, r):
    return ~dilate(~m, r)


def signed_dist(m):
    # khoảng cách Euclid tới mép vùng m (âm bên trong), tính vét cạn theo điểm biên — ảnh 256 đủ nhanh
    edge = m & ~erode(m, 1)
    ys, xs = np.nonzero(edge)
    pts = np.stack([xs, ys], 1).astype(np.float32)
    H, W = m.shape
    gy, gx = np.mgrid[0:H, 0:W].astype(np.float32)
    best = np.full((H, W), 1e9, np.float32)
    for k in range(0, len(pts), 64):
        p = pts[k:k + 64]
        dd = np.sqrt((gx[..., None] - p[:, 0]) ** 2 + (gy[..., None] - p[:, 1]) ** 2).min(-1)
        best = np.minimum(best, dd)
    return np.where(m, -best - 0.5, best - 0.5)


# Thanh treo: dòng 0..21 của banner art (thân thanh dòng 6..20, 2 mũi nhọn tới dòng ~22), bỏ điểm vải xanh lọt vào,
# phóng 1.45 lần; đáy thanh đè lên mép trên ruột cờ (y 58) như art — phần viền cờ phía trên y 56 bị xoá.
banner = art('exec-2a02cda8-e5a5-433f-8a45-7b235448988a copy.png')
ROD_H = 23
rod = np.asarray(banner.crop((0, 0, banner.width, ROD_H))).copy()
blue = (rod[..., 2].astype(int) > rod[..., 0].astype(int) + 40)
rod[blue, 3] = 0
rod = Image.fromarray(rod)
S = 1.45
rod = rod.resize((round(rod.width * S), round(ROD_H * S)), Image.LANCZOS)
ROD_X, ROD_Y = (256 - rod.width) // 2, 30
TOP_CUT = 56

BLACK = np.array([10, 4, 0, 255], np.uint8)
GOLD_HI = np.array([252, 213, 36, 255], np.uint8)
GOLD_LO = np.array([233, 166, 6, 255], np.uint8)
for i in range(1, 9):
    old = np.asarray(Image.open(os.path.join(OLD, 'clan_flag_%02d_color.png' % i)).convert('RGBA'))
    m = old[..., 3] > 128                                  # ruột (vùng lớp màu tô) giữ đúng hình cũ
    dist = signed_dist(m)
    # từ ngoài vào: nét đen ngoài 5px, vàng sáng 5px, vàng tối 5px, nét đen trong 3px, ruột trắng (lớp màu phủ lên)
    frame = np.zeros(m.shape + (4,), np.float32)
    for r, c in [(18, BLACK), (13, GOLD_HI), (8, GOLD_LO), (3, BLACK), (0, np.array([255, 255, 255, 255]))]:
        a = np.clip(r - dist + 0.5, 0, 1)[..., None]
        frame = frame * (1 - a) + c.astype(np.float32) * a
    frame[:TOP_CUT] = 0
    big = Image.fromarray(frame.clip(0, 255).astype(np.uint8))
    big.alpha_composite(rod, (ROD_X, ROD_Y))
    big.save(os.path.join(INC, 'clan_flag_%02d.png' % i))

    # Lớp màu: trắng giữa, tối dần sát viền (như vải banner art: mép tối ~62%) → code tô màu nhân lên vẫn có khối.
    d = -dist
    shade = 0.62 + 0.38 * np.clip((d - 2) / 10.0, 0, 1)
    v = (shade * 255).astype(np.uint8)
    col = np.zeros(m.shape + (4,), np.uint8)
    col[..., 0] = col[..., 1] = col[..., 2] = v
    col[..., 3] = old[..., 3]
    Image.fromarray(col).save(os.path.join(INC, 'clan_flag_%02d_color.png' % i))

# Bóng thanh treo cũ (dải tím đè ngang đầu cờ) — style mới không có → ảnh trống cùng cỡ.
Image.new('RGBA', (256, 256), (0, 0, 0, 0)).save(os.path.join(INC, 'clan_flag_shadow.png'))
print('ok')
