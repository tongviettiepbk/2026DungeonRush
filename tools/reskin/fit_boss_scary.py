# Canh art boss "đáng sợ" (concept ArtIncoming/_Concept/BossRush/gen_boss_scary/keyed/bNN.png) theo sprite thân boss cũ:
# khớp bề ngang thân dưới (trung vị dải 55-85% chiều cao), đáy chân + tâm chân; giữ khung ảnh cũ (pivot giữa),
# tràn thì nới khung đều 2 phía. Dùng: python fit_boss_scary.py <out_dir>  → <out_dir>/<file cũ>.png + check_*.png
import sys, os, numpy as np
from PIL import Image
ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
ART = os.path.join(ROOT, '2026DungeonRushUnity/Assets/_Assets/Art/4_Characters/BossNew')
NEW = os.path.join(ROOT, 'ArtIncoming/_Concept/BossRush/gen_boss_scary/keyed')
BOSSES = ['BossRushDarkLich/Boss 1.png', 'BlackDragon/Boss 7-dragonBlack.png', 'BossRushGreenDragon/Boss 8.png',
          'BossRushRedDragon/Boss 9.png', 'BossRushCrimsonLich/Boss 2.png', 'BossRushElderLich/Boss 10.png',
          'BossRushOgreKing/Boss 3.png', 'BossRushOgreChieftain/Boss 4.png', 'BossRushGreenHag/Boss 5.png',
          'BossRushPurpleHag/Boss 6.png']

MAX_HEIGHT = 1.12   # art mới không cao quá 112% thân cũ (rùa: mai mới cao hơn nhiều)

def height(a):
    ys = np.nonzero(a > 128)[0]
    return ys.max() - ys.min() + 1

def metrics(a):
    ys, xs = np.nonzero(a > 128)
    y0, y1 = ys.min(), ys.max()
    h = y1 - y0 + 1
    widths = []
    for y in range(int(y0 + 0.55 * h), int(y0 + 0.85 * h)):
        row = np.nonzero(a[y] > 128)[0]
        if len(row): widths.append(row.max() - row.min() + 1)
    feet = np.nonzero(a[int(y1 - 0.08 * h):y1 + 1] > 128)[1]
    return np.median(widths), y1, (feet.min() + feet.max()) / 2.0

out = sys.argv[1]
os.makedirs(out, exist_ok=True)
for i, rel in enumerate(BOSSES):
    old = Image.open(os.path.join(ART, rel)).convert('RGBA')
    new = Image.open(os.path.join(NEW, 'b%02d.png' % (i + 1))).convert('RGBA')
    ow, ob, ofx = metrics(np.asarray(old)[..., 3])
    nw, nb, nfx = metrics(np.asarray(new)[..., 3])
    s = min(ow / nw, MAX_HEIGHT * height(np.asarray(old)[..., 3]) / height(np.asarray(new)[..., 3]))
    new = new.resize((max(1, round(new.width * s)), max(1, round(new.height * s))), Image.LANCZOS)
    nb, nfx = nb * s, nfx * s
    x, y = round(ofx - nfx), round(ob - nb)               # góc trên-trái art mới trong khung cũ
    W, H = old.size
    pl = max(0, -x); pr = max(0, x + new.width - W); pt = max(0, -y); pb = max(0, y + new.height - H)
    padx, pady = max(pl, pr), max(pt, pb)                 # nới đều 2 phía để tâm khung không đổi
    canvas = Image.new('RGBA', (W + 2 * padx, H + 2 * pady), (0, 0, 0, 0))
    canvas.alpha_composite(new, (x + padx, y + pady))
    canvas.save(os.path.join(out, os.path.basename(rel)))
    check = Image.new('RGBA', canvas.size, (62, 61, 77, 255))
    ghost = old.copy(); ghost.putalpha(Image.fromarray((np.asarray(old)[..., 3] * 0.45).astype(np.uint8)))
    check.alpha_composite(canvas); check.alpha_composite(ghost, (padx, pady))
    check.save(os.path.join(out, 'check_%02d.png' % (i + 1)))
    print('%-40s old %s -> %s scale %.2f pad %d,%d' % (rel, old.size, canvas.size, s, padx, pady))
