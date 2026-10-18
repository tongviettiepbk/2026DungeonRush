# Vẽ các khung hình học màn Profile/Settings theo mockup ArtIncoming/_Concept/Profile (canvas 1080x2160 = 1:1 pixel).
# Khung popup/header/nút X/nút xanh/ô tan dùng lại kit 1_UIKit/Mastery; ở đây chỉ vẽ phần kit chưa có.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_profile_frames.py <thư_mục_ra>
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

    def rr(self, x0, y0, x1, y1, r, col):
        self.d.rounded_rectangle((x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1), max(r, 0) * SS, fill=col)

    def ellipse(self, x0, y0, x1, y1, col):
        self.d.ellipse((x0 * SS, y0 * SS, x1 * SS - 1, y1 * SS - 1), fill=col)

    def save(self, name):
        self.im.resize((self.w, self.h), Image.LANCZOS).save(os.path.join(out, name))
        print(name, self.w, self.h)


# 1) Rãnh tab Profile|Setting: pill cao 88 màu #7C7464, không viền (mockup y 1943..2031)
c = Canvas(96, 88); c.rr(0, 0, 96, 88, 44, hexc('#7C7464')); c.save('tab_track_gray.png')

# 2) Ô avatar trong lưới chọn: 120x120 bo 4, màu đo trên mockup #C9A380 (tan + lớp đỏ sẫm 20%) — vẽ đặc, tránh bẫy alpha Linear
c = Canvas(120, 120); c.rr(0, 0, 120, 120, 4, hexc('#C9A380')); c.save('avatar_slot.png')

# 3) Rãnh slider / nút gạt: pill tan #E7CA9E viền tối 3 (cùng tông ô tan)
c = Canvas(64, 40); c.rr(0, 0, 64, 40, 20, hexc('#5A4630')); c.rr(3, 3, 61, 37, 17, hexc('#E7CA9E')); c.save('track_tan.png')

# 4) Phần đầy slider: pill xanh nút (#61A7D8) dải sáng trên #84C1EA
c = Canvas(40, 28); c.rr(0, 0, 40, 28, 14, hexc('#84C1EA')); c.rr(0, 4, 40, 28, 14, hexc('#61A7D8')); c.save('fill_blue.png')

# 5) Núm tròn (tay nắm slider, nút gạt rung): trắng để code tô màu bật/tắt (UITabWing.DEFAULT/DISABLED_BUTTON_COLOR),
#    viền đen 4 + đáy dày 8 xám nhạt như nút nổi
c = Canvas(96, 104)
c.ellipse(0, 0, 96, 104, hexc('#000000'))
c.ellipse(4, 4, 92, 100, hexc('#9A9AA8'))
c.ellipse(4, 4, 92, 92, hexc('#FFFFFF'))
c.save('knob.png')
