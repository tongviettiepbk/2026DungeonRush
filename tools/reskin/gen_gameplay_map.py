# Vẽ tường/góc/cửa/chướng ngại map campaign theo mockup ArtIncoming/_Concept/Gameplay (gameplay 1 demo.png).
# Đo trên mockup 1080: khối nền #848798, viền đen 4px, vát 5px (trên-trái sáng B7B8C2→888B9B, dưới-phải tối 828596→565763),
# đoạn tường dày 48px bo ~8, khối góc 66px bo ~10 + bulông tròn ~19px. Ảnh xuất BÁM Ô MAP: 1 ô = CELL px
# (import PPU = CELL, scale 1 → 1 ô = 1 world unit): tường dày 1 ô dài 2 ô, góc/bulông 1 ô, nửa cửa 1.5x1 ô,
# chướng ngại 1x1 ô. K = px sprite / px mockup, giữ đúng độ dày viền/vát world như bản cũ (186/48 px ở PPU 100 scale 0.44).
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_gameplay_map.py <thư_mục_ra>
import sys, os
import numpy as np
from PIL import Image

SS = 4
CELL = 256
K = 186 / 48.0 * 0.44 * CELL / 100.0
out = sys.argv[1]
os.makedirs(out, exist_ok=True)

FILL = (0x84, 0x87, 0x98)
LIGHT = [(0xB7, 0xB8, 0xC2), (0xB1, 0xB3, 0xBD), (0xA4, 0xA7, 0xB3), (0x94, 0x96, 0xA5), (0x88, 0x8B, 0x9B)]
DARK = [(0x56, 0x57, 0x63), (0x5F, 0x61, 0x6E), (0x6D, 0x6F, 0x7E), (0x7B, 0x7D, 0x8D), (0x82, 0x85, 0x96)]
OUTLINE = 4 * K
BEVEL = 5 * K


def rr_sdf(w, h, r, inset):
    # SDF hình chữ nhật bo góc (âm = bên trong), toạ độ pixel siêu mẫu → trả thêm pháp tuyến gần đúng.
    ys, xs = np.mgrid[0:h * SS, 0:w * SS].astype(np.float32)
    px = (xs + 0.5) / SS - w / 2.0
    py = (ys + 0.5) / SS - h / 2.0
    hx = w / 2.0 - inset - r
    hy = h / 2.0 - inset - r
    qx = np.abs(px) - hx
    qy = np.abs(py) - hy
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    d = outside + inside - r
    # hướng tới mép gần nhất: dùng dấu toạ độ + trục trội
    nx = np.where(qx > qy, np.sign(px), 0.0)
    ny = np.where(qy >= qx, np.sign(py), 0.0)
    corner = (qx > 0) & (qy > 0)
    nx = np.where(corner, np.sign(px) * qx / np.maximum(np.hypot(qx, qy), 1e-6), nx)
    ny = np.where(corner, np.sign(py) * qy / np.maximum(np.hypot(qx, qy), 1e-6), ny)
    return d, nx, ny


def block(w, h, r):
    # Khối vát: viền đen → vát sáng (mép trên/trái) / tối (mép dưới/phải) → ruột FILL.
    d_out, _, _ = rr_sdf(w, h, r, 0)
    d_in, nx, ny = rr_sdf(w, h, max(r - OUTLINE, 1), OUTLINE)
    img = np.zeros((h * SS, w * SS, 4), np.float32)
    img[d_out <= 0] = (0, 0, 0, 255)
    inner = d_in <= 0
    depth = -d_in  # khoảng cách vào trong từ mép ruột
    step = np.clip((depth / (BEVEL / 5.0)).astype(int), 0, 5)
    lit = (nx + ny) < 0  # mép trên/trái (y ảnh hướng xuống)
    col = np.zeros((h * SS, w * SS, 3), np.float32)
    col[:] = FILL
    for i in range(5):
        m = inner & (step == i)
        col[m & lit] = LIGHT[i]
        col[m & ~lit] = DARK[i]
    img[inner, :3] = col[inner]
    img[inner, 3] = 255
    return img


def bolt(img, w, h, cx, cy, dia):
    # Bulông cầu: viền đen 2px mockup, màu sáng trên-trái → tối dưới-phải.
    ys, xs = np.mgrid[0:h * SS, 0:w * SS].astype(np.float32)
    px = (xs + 0.5) / SS - cx
    py = (ys + 0.5) / SS - cy
    rad = dia / 2.0
    dist = np.hypot(px, py)
    img[dist <= rad] = (0, 0, 0, 255)
    ri = rad - 2 * K
    m = dist <= ri
    t = np.clip(((px + py) / (2 * ri) + 0.5), 0, 1)[m][:, None]
    a = np.array((0xB7, 0xB9, 0xC1), np.float32)
    b = np.array((0x59, 0x5B, 0x67), np.float32)
    img[m, :3] = a + (b - a) * t
    img[m, 3] = 255


def save(img, w, h, name):
    im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), 'RGBA').resize((w, h), Image.LANCZOS)
    im.save(os.path.join(out, name))
    print(name, w, h)


# Đoạn tường dọc 1x2 ô + bản ngang cho tường trên/dưới (vát phải sáng TRÊN, không xoay được).
W, H = CELL, CELL * 2
save(block(W, H, 8 * K), W, H, 'map_wall_v.png')
save(block(H, W, 8 * K), H, W, 'map_wall_h.png')

# Khối góc + bulông giữa.
C = CELL
img = block(C, C, 10 * K)
bolt(img, C, C, C / 2.0, C / 2.0, 19 * K)
save(img, C, C, 'map_wall_corner.png')

# Cửa: 1 đoạn ngang ngắn (mockup 2 đoạn gặp nhau giữa cửa).
DW, DH = CELL * 3 // 2, CELL
save(block(DW, DH, 8 * K), DW, DH, 'map_door.png')

# Chướng ngại: khối vuông cùng chất liệu + 4 bulông nhỏ ở góc (mockup không có, suy từ khối góc).
O = CELL
img = block(O, O, 10 * K)
for cx in (O * 0.27, O * 0.73):
    for cy in (O * 0.27, O * 0.73):
        bolt(img, O, O, cx, cy, 12 * K)
save(img, O, O, 'map_obstacle.png')
