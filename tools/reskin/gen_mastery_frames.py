# Vẽ các khung hình học màn Mastery theo số đo mockup ArtIncoming/_Concept/Mastery (canvas 1080x2160 = 1:1 pixel).
# Hình học nên vẽ bằng code (đúng màu/bán kính đo được), không nhờ ChatGPT.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_mastery_frames.py <thư_mục_ra>
import sys, os
from PIL import Image, ImageDraw

SS = 4  # siêu lấy mẫu cho viền mượt
out = sys.argv[1]
os.makedirs(out, exist_ok=True)


def hexc(s, a=255):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), a)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    def rr(self, x0, y0, x1, y1, r, col, corners=None):
        # toạ độ pixel thật, x1/y1 là mép ngoài
        self.d.rounded_rectangle((x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1), max(r, 0) * SS, fill=col, corners=corners)

    def rect(self, x0, y0, x1, y1, col):
        self.d.rectangle((x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1), fill=col)

    def save(self, name):
        self.im.resize((self.w, self.h), Image.LANCZOS).save(os.path.join(out, name))
        print(name, self.w, self.h)


def button3d(name, w, h, r, O, D, L, F, S, side=12, F2=None):
    # Nút nổi kiểu mockup: viền ngoài O 3px, vòng tối D 2px, mặt trên viền sáng L 3px + ruột F (F2 = màu đáy nếu dải dọc), đáy dày S.
    c = Canvas(w, h)
    c.rr(0, 0, w, h, r, hexc(O))
    c.rr(3, 3, w - 3, h - 3, r - 3, hexc(S))
    fb = h - 3 - side  # mép dưới mặt trên (kể cả vòng tối)
    c.rr(3, 3, w - 3, fb, r - 3, hexc(D))
    c.rr(5, 5, w - 5, fb - 2, r - 5, hexc(L))
    if F2 is None:
        c.rr(8, 8, w - 8, fb - 5, r - 8, hexc(F))
    else:
        # dải màu dọc F → F2 trong ruột
        g = Image.new('RGBA', (1, 256))
        a, b = hexc(F), hexc(F2)
        for i in range(256):
            t = i / 255
            g.putpixel((0, i), tuple(int(a[k] + (b[k] - a[k]) * t) for k in range(4)))
        x0, y0, x1, y1 = 8, 8, w - 8, fb - 5
        grad = g.resize(((x1 - x0) * SS, (y1 - y0) * SS))
        mask = Image.new('L', grad.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle((0, 0, grad.size[0] - 1, grad.size[1] - 1), (r - 8) * SS, fill=255)
        c.im.paste(grad, (x0 * SS, y0 * SS), mask)
    return c


# 1) Panel lưới: bo 39, viền 3 #19191E, nền #302F3F
c = Canvas(128, 128); c.rr(0, 0, 128, 128, 39, hexc('#19191E')); c.rr(3, 3, 125, 125, 36, hexc('#302F3F')); c.save('mastery_panel.png')

# 2) Khung popup: nền kem, viền đen 3, bo 9
c = Canvas(64, 64); c.rr(0, 0, 64, 64, 9, hexc('#000000')); c.rr(3, 3, 61, 61, 6, hexc('#F9E9C8')); c.save('popup_frame.png')

# 3) Thanh tiêu đề popup: cao 138 (588..725), bo trên 9, dải sáng/đậm/nhạt + kẻ đen đáy 3
W, H = 64, 138
c = Canvas(W, H)
c.rr(0, 0, W, H, 9, hexc('#000000'), corners=(True, True, False, False))
bands = [(3, 6, '#625D8F'), (6, 7, '#5C5786'), (7, 8, '#4F4B73'), (8, 69, '#49456A'), (69, 70, '#464266'), (70, 71, '#413D5E'), (71, 135, '#3E3B5A')]
body = Image.new('RGBA', ((W - 6) * SS, (H - 6) * SS))
bd = ImageDraw.Draw(body)
for y0, y1, col in bands:
    bd.rectangle((0, (y0 - 3) * SS, body.size[0], (y1 - 3) * SS - 1), fill=hexc(col))
mask = Image.new('L', body.size, 0)
ImageDraw.Draw(mask).rounded_rectangle((0, 0, body.size[0] - 1, body.size[1] + 20 * SS), 6 * SS, fill=255, corners=(True, True, False, False))
c.im.paste(body, (3 * SS, 3 * SS), mask)
c.save('popup_header.png')

# 4) Ô nền tan (khung icon, ô tiến trình): bo 8, #E7CA9E
c = Canvas(48, 48); c.rr(0, 0, 48, 48, 8, hexc('#E7CA9E')); c.save('box_tan.png')

# 5) Nút xanh dương popup: 276x154, bo 17
button3d('btn_blue', 276, 154, 17, '#000E28', '#063D7D', '#84C1EA', '#61A7D8', '#41699F').save('btn_blue.png')

# 6) Nút đóng đỏ 120x120, bo 13, ruột dải #E22F4F→#EF3F62, dấu X trắng viền đỏ sẫm
c = button3d('btn_close', 120, 120, 13, '#1B0000', '#920F05', '#FD6192', '#E22F4F', '#9C2737', F2='#EF3F62')
cx, cy = 59.5 * SS, 54.5 * SS  # tâm X trên mockup (882.5-823, 650.5-596)


def arm(length, thick, col):
    a = Image.new('RGBA', (int(length * SS), int(thick * SS)), (0, 0, 0, 0))
    ImageDraw.Draw(a).rounded_rectangle((0, 0, a.size[0] - 1, a.size[1] - 1), 5 * SS, fill=col)
    return a


for L, T, col in [(84, 26, hexc("#920F05")), (78, 20, hexc("#FFFFFF"))]:
    for ang in (45, -45):
        a = arm(L, T, col).rotate(ang, resample=Image.BICUBIC, expand=True)
        c.im.alpha_composite(a, (int(cx - a.size[0] / 2), int(cy - a.size[1] / 2)))
    if col[0] == 0x92:
        pass
c.save('btn_close.png')

# 7) Tab: rãnh tối pill cao 88 #18161E; ô chọn pill viền đen 4 + vàng 4 + cam #EFA14F
c = Canvas(96, 88); c.rr(0, 0, 96, 88, 44, hexc('#18161E')); c.save('tab_track.png')
c = Canvas(96, 88); c.rr(0, 0, 96, 88, 44, hexc('#000000')); c.rr(4, 4, 92, 84, 40, hexc('#FDCD51')); c.rr(8, 8, 88, 80, 36, hexc('#EFA14F')); c.save('tab_selected.png')
