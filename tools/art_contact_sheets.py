# Contact sheet cho doi art: moi thu muc trong Assets/_Assets/Art/ (co PNG truc tiep) -> 1 anh luoi thumbnail + ten file.
# Ra: ArtIncoming/_Reference/sheets/<duong_dan_thu_muc voi "__">.png  (xem GDD/RESKIN_PLAN.md muc 2.3)
# Chay: tools/.venv/bin/python tools/art_contact_sheets.py
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "2026DungeonRushUnity", "Assets", "_Assets", "Art")
OUT = os.path.join(ROOT, "ArtIncoming", "_Reference", "sheets")
CELL = 180          # o vuong chua thumbnail
LABEL_H = 34        # 2 dong ten file
COLS = 8
PAD = 6

def font(size):
    for p in ["/System/Library/Fonts/Supplemental/Arial.ttf", "/Library/Fonts/Arial.ttf"]:
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()

F_LABEL = font(12)
F_TITLE = font(20)

def checker(w, h, s=10):
    im = Image.new("RGB", (w, h), (235, 235, 235))
    d = ImageDraw.Draw(im)
    for y in range(0, h, s):
        for x in range((y // s) % 2 * s, w, 2 * s):
            d.rectangle([x, y, x + s - 1, y + s - 1], fill=(205, 205, 205))
    return im

def wrap(name, width=26):
    return [name[i:i + width] for i in range(0, len(name), width)][:2]

def sheet(folder, files):
    rows = (len(files) + COLS - 1) // COLS
    cw, ch = CELL + PAD, CELL + LABEL_H + PAD
    title_h = 40
    W = COLS * cw + PAD
    H = title_h + rows * ch + PAD
    canvas = Image.new("RGB", (W, H), (255, 255, 255))
    d = ImageDraw.Draw(canvas)
    rel = os.path.relpath(folder, ART).replace(os.sep, "/")
    d.text((PAD, 10), f"Art/{rel}  ({len(files)} anh)", fill=(0, 0, 0), font=F_TITLE)
    for i, f in enumerate(files):
        x = PAD + (i % COLS) * cw
        y = title_h + (i // COLS) * ch
        canvas.paste(checker(CELL, CELL), (x, y))
        im = Image.open(os.path.join(folder, f)).convert("RGBA")
        size = f"{im.width}x{im.height}"
        im.thumbnail((CELL - 8, CELL - 8))
        canvas.paste(im, (x + (CELL - im.width) // 2, y + (CELL - im.height) // 2), im)
        d.text((x + 2, y + 2), size, fill=(90, 90, 90), font=F_LABEL)
        for j, line in enumerate(wrap(os.path.splitext(f)[0])):
            d.text((x, y + CELL + 2 + j * 15), line, fill=(0, 0, 0), font=F_LABEL)
    return canvas, rel

def main():
    os.makedirs(OUT, exist_ok=True)
    n = 0
    for dp, _, fns in os.walk(ART):
        files = sorted([f for f in fns if f.lower().endswith(".png")], key=str.lower)
        if not files:
            continue
        canvas, rel = sheet(dp, files)
        canvas.save(os.path.join(OUT, rel.replace("/", "__") + ".png"), optimize=True)
        n += 1
    print("sheets:", n, "->", OUT)

if __name__ == "__main__":
    main()
