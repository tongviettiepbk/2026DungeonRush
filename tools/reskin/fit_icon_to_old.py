# Tách nền magenta ảnh ChatGPT rồi đặt vào ĐÚNG kích thước pixel ảnh cũ, phần hình khớp hộp bao (alpha bbox) của hình cũ
# → ghi đè tại chỗ không đổi PPU/9-slice, ảnh nằm trong TMP sprite asset (IsDrill, IsRedKey...) không phải sửa glyph.
# Dùng: tools/.venv/bin/python -I tools/reskin/fit_icon_to_old.py <ảnh_chatgpt.png> <ảnh_cũ.png> <out.png>
#       tools/.venv/bin/python -I tools/reskin/fit_icon_to_old.py <ảnh_chatgpt.png> <cạnh_dài_px> <out.png>   (ảnh mới, không có ảnh cũ)
import sys
from PIL import Image, ImageFilter


# Nền = điểm gần đúng #FF00FF (kể cả lỗ kín như vòng chìa khoá). Chỉ dải 3px sát nền mới khử viền theo độ magenta;
# bên trong giữ đặc → màu tím của hình (gem, rương mythic) không bị ăn thành bán trong suốt.
def key_magenta(path):
    im = Image.open(path).convert('RGB')
    w, h = im.size
    px = im.load()
    bg = Image.new('L', (w, h), 0)
    pb = bg.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if r > 200 and b > 200 and g < 70:
                pb[x, y] = 255
    band = bg.filter(ImageFilter.MaxFilter(7)).load()
    o = Image.new('RGBA', (w, h))
    po = o.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            if pb[x, y]:
                po[x, y] = (0, 0, 0, 0)
                continue
            if not band[x, y]:
                po[x, y] = (r, g, b, 255)
                continue
            a = 1 - min(1.0, max(0, min(r, b) - g) / 235.0)
            if a <= 0.10:
                po[x, y] = (0, 0, 0, 0)
                continue
            fg = [min(255, max(0, round((c - (1 - a) * mc) / a))) for c, mc in ((r, 255), (g, 0), (b, 255))]
            po[x, y] = (*fg, round(a * 255))
    return o.crop(o.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox())


if __name__ == '__main__':
    src, old, out = sys.argv[1], sys.argv[2], sys.argv[3]
    o = key_magenta(src)
    if old.isdigit():
        k = int(old) / max(o.size)
        o = o.resize((max(1, round(o.width * k)), max(1, round(o.height * k))), Image.LANCZOS)
        o.save(out)
        print(out.split('/')[-1], '->', o.size)
        sys.exit()

    ref = Image.open(old).convert('RGBA')
    ow, oh = ref.size
    bl, bt, br, bb = ref.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox()
    bw, bh = br - bl, bb - bt
    k = min(bw / o.width, bh / o.height)
    o = o.resize((max(1, round(o.width * k)), max(1, round(o.height * k))), Image.LANCZOS)
    c = Image.new('RGBA', (ow, oh), (0, 0, 0, 0))
    c.paste(o, (bl + (bw - o.width) // 2, bt + (bh - o.height) // 2))
    c.save(out)
    print(out.split('/')[-1], 'old', (ow, oh), 'bbox', (bw, bh), '->', o.size)
