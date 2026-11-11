# Sinh nền thẻ ưu đãi trang Shop theo style kit (giống thẻ Ngày 7 — gen_day7_bg.py): nền chuyển màu + tia sáng +
# chấm halftone góc, viền tối dày, mép sáng trong, gờ đáy. Ảnh nguyên khối (Image Simple), đúng kích thước canvas.
# Chạy: tools/.venv/bin/python3 -I tools/reskin/gen_shop_cards.py <thư_mục_ra>
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFilter

OUT_DIR = sys.argv[1]
SS = 2  # vẽ to gấp đôi rồi thu nhỏ cho mép mịn

# tên: (rộng, cao, màu trái-trên, màu phải-dưới, viền tối, gờ đáy, mép sáng, chấm góc dưới-trái, tâm tia (x,y))
CARDS = {
    "offer_red":    (1006, 208, (232, 58, 92), (255, 132, 70), (74, 14, 30), (176, 36, 60), (255, 190, 170), (255, 220, 120, 150), (0.12, 0.55)),
    "offer_blue":   (1006, 208, (44, 150, 236), (74, 84, 222), (14, 28, 72), (36, 62, 170), (170, 220, 255), (150, 230, 255, 140), (0.12, 0.55)),
    "offer_purple": (1006, 208, (88, 52, 214), (160, 62, 226), (36, 18, 66), (98, 40, 168), (205, 160, 255), (255, 120, 226, 170), (0.12, 0.55)),
    "deal_orange":  (1006, 302, (255, 106, 74), (255, 180, 64), (78, 28, 10), (198, 82, 32), (255, 216, 160), (255, 236, 130, 160), (0.84, 0.55)),
    "deal_purple":  (1006, 302, (88, 52, 214), (160, 62, 226), (36, 18, 66), (98, 40, 168), (205, 160, 255), (255, 120, 226, 170), (0.84, 0.55)),
    "chest_gold":   (1006, 302, (255, 186, 52), (255, 108, 46), (82, 36, 6), (196, 92, 20), (255, 232, 160), (255, 246, 170, 170), (0.17, 0.55)),
}

OUTLINE = 5 * SS
LIP = 9 * SS
RADIUS = 26 * SS


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def face_art(fw, fh, left, right, dot_color, ray_center):
    grad = Image.new("RGB", (fw, fh))
    px = grad.load()
    for x in range(fw):
        for y in range(fh):
            t = min(1.0, max(0.0, 0.75 * x / fw + 0.25 * y / fh))
            px[x, y] = lerp(left, right, t)
    art = grad.convert("RGBA")

    cx, cy = fw * ray_center[0], fh * ray_center[1]
    far = math.hypot(fw, fh)
    rays = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    d = ImageDraw.Draw(rays)
    count = 24
    for i in range(count):
        a0 = 2 * math.pi * i / count
        a1 = a0 + math.pi / count
        d.polygon([(cx, cy), (cx + far * math.cos(a0), cy + far * math.sin(a0)),
                   (cx + far * math.cos(a1), cy + far * math.sin(a1))], fill=(255, 255, 255, 30))
    reach = fh * 2.6
    falloff = Image.radial_gradient("L").resize((int(reach), int(reach))).point(lambda v: 255 - v)
    mask = Image.new("L", (fw, fh), 0)
    mask.paste(falloff, (int(cx - reach / 2), int(cy - reach / 2)))
    rays.putalpha(Image.composite(rays.getchannel("A"), Image.new("L", (fw, fh), 0), mask))
    art = Image.alpha_composite(art, rays)

    glow = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    r = fh * 0.5
    ImageDraw.Draw(glow).ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 240, 220, 70))
    art = Image.alpha_composite(art, glow.filter(ImageFilter.GaussianBlur(fh * 0.2)))

    # Halftone: chấm màu ở góc đối diện tâm tia, chấm trắng nhỏ ở góc trên còn lại.
    near_left = ray_center[0] < 0.5
    dots = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    dd = ImageDraw.Draw(dots)
    step = 15 * SS
    corners = (((fw, fh) if near_left else (0, fh), dot_color, fh * 1.2, 5.0 * SS),
               ((fw, 0) if not near_left else (fw * 0.45, 0), (255, 255, 255, 55), fh * 0.8, 3.6 * SS))
    for corner, color, reach_d, rmax in corners:
        for gy in range(0, fh + step, step):
            for gx in range(0, fw + step, step):
                x = gx + (step / 2 if (gy // step) % 2 else 0)
                t = 1 - math.hypot(x - corner[0], gy - corner[1]) / reach_d
                if t <= 0.05:
                    continue
                rr = rmax * t
                dd.ellipse((x - rr, gy - rr, x + rr, gy + rr), fill=color)
    return Image.alpha_composite(art, dots)


def rounded_mask(size, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=255)
    return m


def make(name, W, H, left, right, outline, lip, rim, dot, ray_center):
    w, h = W * SS, H * SS
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), RADIUS, fill=outline + (255,))
    d.rounded_rectangle((OUTLINE, OUTLINE, w - 1 - OUTLINE, h - 1 - OUTLINE), RADIUS - OUTLINE, fill=lip + (255,))
    fw, fh = w - 2 * OUTLINE, h - 2 * OUTLINE - LIP
    face = face_art(fw, fh, left, right, dot, ray_center)
    ImageDraw.Draw(face).rounded_rectangle((0, 0, fw - 1, fh - 1), RADIUS - OUTLINE, outline=rim + (150,), width=4 * SS)
    img.paste(face, (OUTLINE, OUTLINE), rounded_mask((fw, fh), RADIUS - OUTLINE))
    out = os.path.join(OUT_DIR, name + ".png")
    img.resize((W, H), Image.LANCZOS).save(out)
    print("saved", out)


os.makedirs(OUT_DIR, exist_ok=True)
for key, spec in CARDS.items():
    make(key, *spec)
