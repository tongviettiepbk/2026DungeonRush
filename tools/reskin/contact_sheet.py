# Bảng ghép (contact sheet) mọi PNG trong 1 thư mục, có đánh số + tên file, nền magenta để thấy vùng trong suốt.
# Dùng: tools/.venv/bin/python -I tools/reskin/contact_sheet.py <thư_mục_ảnh> <out.png>
import sys,os
from PIL import Image,ImageDraw
src=sys.argv[1]; out=sys.argv[2]
fs=sorted(f for f in os.listdir(src) if f.endswith('.png') and f!='lobby demo.png')
cell=300; cols=5; rows=(len(fs)+cols-1)//cols
sheet=Image.new('RGBA',(cols*cell,rows*(cell+30)),(255,0,255,255))
d=ImageDraw.Draw(sheet)
for i,f in enumerate(fs):
    im=Image.open(os.path.join(src,f)).convert('RGBA'); im.thumbnail((cell-10,cell-10))
    x=(i%cols)*cell; y=(i//cols)*(cell+30)
    sheet.paste(im,(x+5,y+5),im)
    d.text((x+5,y+cell+5),f"{i}:{f[:38]}",fill=(0,0,0,255))
sheet.save(out)
print('\n'.join(f"{i}:{f}" for i,f in enumerate(fs)))
