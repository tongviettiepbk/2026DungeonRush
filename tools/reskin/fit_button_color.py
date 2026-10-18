# Đổi màu btn_green → nút khoá xám/đỏ/xanh: fit ma trận màu affine (least squares) từ cột nút xanh sang cột nút đích trên mockup
# Dùng: ... fit_button_color.py <mockup.png> <btn_green.png> <thư_mục_out>
# LƯU Ý: toạ độ cột mẫu trên mockup (nút xanh y1612-1770, nút đích y1785-1943, cột x) đang viết cứng theo lobby demo.png — đổi mockup thì sửa toạ độ.
import sys
from PIL import Image
mock=Image.open(sys.argv[1]).convert('RGB'); src=Image.open(sys.argv[2]).convert('RGBA'); out_dir=sys.argv[3]
def col(x,y0,y1): return [mock.getpixel((xx,y)) for y in range(y0,y1) for xx in range(x-3,x+4)]
def solve(A,b):
    n=len(A); M=[A[i][:]+[b[i]] for i in range(n)]
    for i in range(n):
        p=max(range(i,n),key=lambda r:abs(M[r][i])); M[i],M[p]=M[p],M[i]
        for r in range(n):
            if r!=i:
                f=M[r][i]/M[i][i]; M[r]=[M[r][k]-f*M[i][k] for k in range(n+1)]
    return [M[i][n]/M[i][i] for i in range(n)]
small=src.convert('RGB').resize((278,158),Image.LANCZOS)
def scol(x): return [small.getpixel((xx,y)) for y in range(158) for xx in range(x-3,x+4)]
G=scol(55)+scol(255)
targets={'btn_gray':(100,300),'btn_red':(437,637),'btn_blue':(815,1015)}
for name,(x1,x2) in targets.items():
    T=col(x1,1785,1943)+col(x2,1785,1943); n=min(len(G),len(T))
    X=[list(G[i])+[1.0] for i in range(n)]
    XtX=[[sum(X[k][i]*X[k][j] for k in range(n))+(1e-3 if i==j else 0) for j in range(4)] for i in range(4)]
    W=[]
    for c in range(3):
        Xty=[sum(X[k][i]*T[k][c] for k in range(n)) for i in range(4)]; W.append(solve(XtX,Xty))
    err=sum(abs(sum(W[c][i]*X[k][i] for i in range(4))-T[k][c]) for k in range(n) for c in range(3))/(3*n)
    o=src.copy(); px=o.load()
    for y in range(o.size[1]):
        for x in range(o.size[0]):
            r,g,b,a=px[x,y]
            if a==0: continue
            v=(r,g,b,1.0); m=[sum(W[c][i]*v[i] for i in range(4)) for c in range(3)]
            lum=0.3*r+0.59*g+0.11*b
            t=max(0.0,min(1.0,(lum-25)/60))   # viền tối (#051B00, #1C5B04) giữ đen trung tính
            px[x,y]=tuple(max(0,min(255,round(t*m[c]+(1-t)*lum*0.6))) for c in range(3))+(a,)
    o.save(f"{out_dir}/{name}.png"); print(name,'mean err',round(err,1))
