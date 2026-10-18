# Đệm trong suốt cho ảnh mới đạt đúng tỉ lệ w/h của ảnh cũ (icon căn giữa, không co giãn)
# Dùng: ... pad_to_old_aspect.py <ảnh_mới.png (ghi đè tại chỗ)> <ảnh_cũ.png>
import sys
from PIL import Image
new,old=sys.argv[1],sys.argv[2]
im=Image.open(new).convert('RGBA'); ow,oh=Image.open(old).size
w,h=im.size; r=ow/oh
if w/h<r: W,H=round(h*r),h
else: W,H=w,round(w/r)
c=Image.new('RGBA',(W,H),(0,0,0,0)); c.paste(im,((W-w)//2,(H-h)//2)); c.save(new)
print(new.split('/')[-1],(w,h),'->',(W,H),'old',(ow,oh))
