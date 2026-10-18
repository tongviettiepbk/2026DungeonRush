# Pha khung/ô vuông màu mới từ 1 khung XÁM (fill ~#B1B0AE): giữ nguyên dáng, viền sáng, viền đen; chỉ đổi màu fill.
# Dùng: ... recolor_frame.py <khung_xám.png> <out.png> R G B   (R G B = màu fill đích)
import sys
from PIL import Image
src,out,r,g,b=sys.argv[1],sys.argv[2],*map(int,sys.argv[3:6])
im=Image.open(src).convert('RGBA'); px=im.load(); w,h=im.size
F=177.0; T=(r,g,b)
for y in range(h):
    for x in range(w):
        pr,pg,pb,a=px[x,y]
        if a==0: continue
        v=(pr+pg+pb)/3
        if v>=F: t=(v-F)/(255-F); c=tuple(round(T[i]*(1-t)+255*t) for i in range(3))
        else: s=(F-v)/F; c=tuple(round(T[i]*(1-s)) for i in range(3))
        px[x,y]=(*c,a)
im.save(out)
