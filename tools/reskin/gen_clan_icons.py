# Hậu kỳ icon Clan do ChatGPT vẽ (ArtIncoming/_Concept/Clan/gen/out) → ArtIncoming/6_Features/Clan/ cho Apply Reskin.
# - 20 icon emblem banner (clan_icon_*): ChatGPT vẽ vàng gold như banner art giao → tách nền, khớp đúng W×H + hộp bao icon cũ,
#   rồi chuyển XÁM theo kênh G (vàng sáng = trắng, nét đen giữ đen) vì ClanBannerView tô Image.color = imageColors[id]
#   lên icon (màu 2 = vàng 1,0.75,0.22 → ra lại vàng như mockup; các màu khác vẫn đúng ý người chơi chọn).
# - clan_leader_icon / clan_captain_icon: giữ màu (không bị tô), chỉ tách nền + khớp ảnh cũ.
# Dùng: tools/.venv/bin/python -I tools/reskin/gen_clan_icons.py <repo_root>
import sys, os, glob
from PIL import Image

root = sys.argv[1]
sys.path.insert(0, os.path.join(root, 'tools/reskin'))
from fit_icon_to_old import key_magenta  # noqa: E402

GEN = os.path.join(root, 'ArtIncoming/_Concept/Clan/gen/out')
OLD = os.path.join(root, '2026DungeonRushUnity/Assets/_Assets/Art/6_Features/Clan')
INC = os.path.join(root, 'ArtIncoming/6_Features/Clan')
os.makedirs(INC, exist_ok=True)
G_WHITE = 215.0  # kênh G của vàng sáng (#FDDC1D ~ 220) coi là trắng


def fit(src, old_path):
    o = key_magenta(src)
    ref = Image.open(old_path).convert('RGBA')
    ow, oh = ref.size
    bl, bt, br, bb = ref.getchannel('A').point(lambda v: 255 if v > 40 else 0).getbbox()
    bw, bh = br - bl, bb - bt
    k = min(bw / o.width, bh / o.height)
    o = o.resize((max(1, round(o.width * k)), max(1, round(o.height * k))), Image.LANCZOS)
    c = Image.new('RGBA', (ow, oh), (0, 0, 0, 0))
    c.paste(o, (bl + (bw - o.width) // 2, bt + (bh - o.height) // 2))
    return c


def to_tintable(im):
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            v = min(255, round(g / G_WHITE * 255))
            px[x, y] = (v, v, v, a)
    return im


for src in sorted(glob.glob(os.path.join(GEN, '*.png'))):
    name = os.path.basename(src)
    old = os.path.join(OLD, name)
    if not os.path.exists(old):
        print('bỏ qua (không có ảnh cũ):', name)
        continue
    im = fit(src, old)
    if name.startswith('clan_icon_'):
        im = to_tintable(im)
    im.save(os.path.join(INC, name))
    print(name, im.size)
