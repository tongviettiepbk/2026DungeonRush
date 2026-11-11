# Sinh nền thẻ "Ngày 7" của UISevenDayPopup: tím chuyển màu + tia sáng toả từ sau ô quà + chấm halftone hồng ở góc,
# kèm viền tối, mép sáng trong và gờ đáy (ảnh nguyên khối, dùng Image Simple trong Unity).
# Chạy: tools/.venv/bin/python3 -I tools/reskin/gen_day7_bg.py 2026DungeonRushUnity/Assets/_Assets/Art/1_UIKit/SevenDay/day7_bg.png
import math
import sys
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
W, H = 876, 336          # = kích thước thẻ trên canvas 1080×2160
SS = 2                   # vẽ to gấp đôi rồi thu nhỏ cho mép mịn
w, h = W * SS, H * SS

OUTLINE = 5 * SS         # viền tối ngoài cùng
LIP = 9 * SS             # gờ đáy
RADIUS = 26 * SS
RAY_CENTER = (0.64, 0.58)  # tâm tia sáng = giữa 2 ô quà


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def face_art(fw, fh):
    # Nền chuyển màu chéo: xanh tím góc trái trên → tím hồng góc phải dưới.
    left, right = (88, 52, 214), (160, 62, 226)
    grad = Image.new("RGB", (fw, fh))
    px = grad.load()
    for x in range(fw):
        for y in range(0, fh, 1):
            t = min(1.0, max(0.0, 0.75 * x / fw + 0.25 * y / fh))
            px[x, y] = lerp(left, right, t)
    art = grad.convert("RGBA")

    cx, cy = fw * RAY_CENTER[0], fh * RAY_CENTER[1]
    far = math.hypot(fw, fh)

    # Tia sáng: 20 nan quạt trắng mờ, mờ dần theo khoảng cách tới tâm.
    rays = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    d = ImageDraw.Draw(rays)
    count = 20
    for i in range(count):
        a0 = 2 * math.pi * i / count
        a1 = a0 + math.pi / count
        d.polygon([(cx, cy), (cx + far * math.cos(a0), cy + far * math.sin(a0)),
                   (cx + far * math.cos(a1), cy + far * math.sin(a1))], fill=(255, 255, 255, 34))
    falloff = Image.radial_gradient("L").resize((int(far * 1.3), int(far * 1.3)))
    falloff = falloff.point(lambda v: 255 - v)
    mask = Image.new("L", (fw, fh), 0)
    mask.paste(falloff, (int(cx - falloff.width / 2), int(cy - falloff.height / 2)))
    rays.putalpha(Image.composite(rays.getchannel("A"), Image.new("L", (fw, fh), 0), mask))
    art = Image.alpha_composite(art, rays)

    # Quầng sáng ở tâm tia.
    glow = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    r = fh * 0.55
    gd.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(255, 220, 255, 70))
    art = Image.alpha_composite(art, glow.filter(ImageFilter.GaussianBlur(fh * 0.22)))

    # Halftone: chấm hồng to ở góc dưới trái, nhỏ dần ra xa; chấm trắng nhỏ ở góc trên phải.
    dots = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
    dd = ImageDraw.Draw(dots)
    step = 15 * SS
    for corner, color, reach, rmax in (((0, fh), (255, 120, 226, 170), fh * 1.25, 5.2 * SS),
                                       ((fw, 0), (255, 255, 255, 60), fh * 0.9, 4.0 * SS)):
        for gy in range(0, fh + step, step):
            for gx in range(0, fw + step, step):
                x = gx + (step / 2 if (gy // step) % 2 else 0)
                t = 1 - math.hypot(x - corner[0], gy - corner[1]) / reach
                if t <= 0.05:
                    continue
                rr = rmax * t
                dd.ellipse((x - rr, gy - rr, x + rr, gy + rr), fill=color)
    return Image.alpha_composite(art, dots)


def rounded_mask(size, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=255)
    return m


img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
d.rounded_rectangle((0, 0, w - 1, h - 1), RADIUS, fill=(36, 18, 66, 255))                       # viền tối
d.rounded_rectangle((OUTLINE, OUTLINE, w - 1 - OUTLINE, h - 1 - OUTLINE), RADIUS - OUTLINE,
                    fill=(98, 40, 168, 255))                                                     # gờ đáy

fw, fh = w - 2 * OUTLINE, h - 2 * OUTLINE - LIP
face = face_art(fw, fh)
rim = ImageDraw.Draw(face)
rim.rounded_rectangle((0, 0, fw - 1, fh - 1), RADIUS - OUTLINE, outline=(205, 160, 255, 150), width=4 * SS)
img.paste(face, (OUTLINE, OUTLINE), rounded_mask((fw, fh), RADIUS - OUTLINE))

img.resize((W, H), Image.LANCZOS).save(OUT)
print("saved", OUT)
