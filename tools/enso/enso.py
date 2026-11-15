import numpy as np, math
from PIL import Image, ImageFilter
S=1024; rng=np.random.default_rng(11)
alpha=np.zeros((S,S),np.float32)
cx,cy=S/2,S/2+8; R=360
# 붓 털 60가닥: 각 털은 반지름 오프셋과 잉크량을 가진다
nb=110
offs=rng.uniform(-1,1,nb); ink=rng.uniform(0.35,1.0,nb); dry=np.clip(rng.normal(0.8,0.13,nb),0.35,0.99)
gap=rng.uniform(0,1,nb)
start=math.radians(-60); sweep=math.radians(318)
steps=2600
yy,xx=np.mgrid[0:S,0:S]
for i in range(steps):
    t=i/(steps-1)
    a=start+sweep*t
    # 굵기: 처음 눌러 굵고(먹 번짐), 끝으로 갈수록 가늘고 갈라진다
    w=(0.75+0.25*min(1,t/0.04))*96*(1-0.5*t)*(0.88+0.12*math.sin(t*7.0+1))+ (30*math.exp(-t*14))
    r=R+12*math.sin(t*math.pi*1.3)+6*math.sin(t*9)
    px=cx+r*math.cos(a); py=cy+r*math.sin(a)
    for b in range(nb):
        if t>dry[b] : continue  # 이 털은 먹이 다했다
        if t>0.5 and math.sin(t*23*(1+gap[b])+gap[b]*30)>1.0-0.9*(t-0.5): continue  # 마른 붓: 털이 군데군데 끊긴다
        o=offs[b]*w/2
        bx=px+o*math.cos(a); by=py+o*math.sin(a)
        ix,iy=int(bx),int(by)
        if 1<=ix<S-1 and 1<=iy<S-1:
            v=ink[b]*(1.0 if t<dry[b]-0.08 else (dry[b]-t)/0.08)
            alpha[iy-1:iy+2,ix-1:ix+2]=np.maximum(alpha[iy-1:iy+2,ix-1:ix+2], v)
# 시작점 먹 번짐
sa=start; sx=cx+R*math.cos(sa); sy=cy+R*math.sin(sa)
d=np.sqrt((xx-sx)**2+(yy-sy)**2)
ang=np.arctan2(yy-sy,xx-sx); rr=40+8*np.sin(ang*5)+5*np.sin(ang*11)
pass
img=Image.fromarray((alpha*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.1))
a=np.asarray(img).astype(np.float32)/255
# 종이 결 노이즈로 가장자리 거칠게
noise=rng.random((S,S)).astype(np.float32)
a=np.clip(a*(0.85+0.3*noise),0,1)
out=np.zeros((S,S,4),np.uint8); out[...,0]=24; out[...,1]=20; out[...,2]=18; out[...,3]=(a*255).astype(np.uint8)
Image.fromarray(out,'RGBA').save('MolakEnso.png')
bg=Image.new('RGBA',(S,S),(243,239,229,255)); bg.alpha_composite(Image.fromarray(out,'RGBA')); bg.convert('RGB').resize((384,384)).save('prev.png')
