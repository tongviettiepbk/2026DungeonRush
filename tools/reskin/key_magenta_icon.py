# Tách nền magenta (#FF00FF) của ảnh ChatGPT → PNG trong suốt, cắt sát, thu về cạnh dài = size, đệm vuông nếu square
# Dùng: ... key_magenta_icon.py <ảnh_chatgpt.png> <out.png> <cạnh_dài_px> <1=đệm vuông|0>
import sys
from PIL import Image
src,out,size,square=sys.argv[1],sys.argv[2],int(sys.argv[3]),sys.argv[4]=='1'
im=Image.open(src).convert('RGB'); w,h=im.size
o=Image.new('RGBA',(w,h)); px=im.load(); po=o.load()
for y in range(h):
    for x in range(w):
        r,g,b=px[x,y]
        m=min(1.0,max(0,min(r,b)-g)/235.0)
        a=1-m
        if a<=0.10: po[x,y]=(0,0,0,0); continue
        fg=[min(255,max(0,round((c-(1-a)*mc)/a))) for c,mc in ((r,255),(g,0),(b,255))]
        po[x,y]=(*fg,round(a*255))
bb=o.getchannel('A').point(lambda v:255 if v>40 else 0).getbbox()
o=o.crop(bb); cw,ch=o.size; k=size/max(cw,ch)
o=o.resize((max(1,round(cw*k)),max(1,round(ch*k))),Image.LANCZOS)
if square:
    c=Image.new('RGBA',(size,size),(0,0,0,0)); c.paste(o,((size-o.size[0])//2,(size-o.size[1])//2)); o=c
o.save(out); print(out,o.size)
