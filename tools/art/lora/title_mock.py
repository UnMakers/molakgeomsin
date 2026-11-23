# 타이틀 시안 모의 화면(2026-11-23) — MolakTitle.cs 겹침(붉은 달·결말별 실루엣·진엔딩 새벽 틴트)을 흉내 내 시안별 4상태를 한 장에. 실루엣 자리·크기는 어림.
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
import numpy as np, glob
T='/mnt/user-data/uploads/output/molak/title/'
fs=glob.glob('/usr/share/fonts/**/*CJK*',recursive=True); F=ImageFont.truetype(fs[0],22); FB=ImageFont.truetype(fs[0],30)
W,H=832,1216
def sil(name,seed,h,tint,alpha):
    im=Image.open(T+'sil_%s_s%d_cut_00001_.png'%(name,seed)).convert('RGBA')
    arr=np.asarray(im).astype(float); lum=arr[...,:3].mean(2)
    keep=(arr[...,3]>40)&((lum<185) if tint[0]<128 else True)
    from scipy import ndimage
    keep=ndimage.binary_opening(keep,iterations=1)
    arr[...,3]=np.where(keep,arr[...,3],0); im=Image.fromarray(arr.astype(np.uint8),'RGBA')
    a=np.asarray(im)[...,3]; ys,xs=np.where(a>40); im=im.crop((xs.min(),ys.min(),xs.max()+1,ys.max()+1))
    im=im.resize((int(im.width*h/im.height),h))
    m=(np.asarray(im)[...,3].astype(float)*alpha).astype(np.uint8)
    out=Image.new('RGBA',im.size,tint+(0,)); out.putalpha(Image.fromarray(m)); return out
def place(base,s,x,feet):
    base.alpha_composite(s,(int(x*W-s.width/2),int(feet*H-s.height)))
def frame(bg,state):
    b=Image.open(T+bg).convert('RGBA').resize((W,H))
    d=ImageDraw.Draw(b); cx,cy,r=0.494*W,0.1213*H,0.0529*W
    moon=(150,20,16,255) if state<3 else (215,120,90,255)
    d.ellipse((cx-r,cy-r,cx+r,cy+r),fill=moon)
    if state>=2:
        place(b,sil('lawlord',8101,int(0.42*H),(235,235,235),0.22),0.16,0.90)
        place(b,sil('jinbaek',8202,int(0.10*H),(20,20,24),0.85),0.68,0.875)
        place(b,sil('hajinmu',8303,int(0.15*H),(240,240,240),0.85),0.74,0.99)
        place(b,sil('hajinmu',8101,int(0.14*H),(240,240,240),0.8),0.84,0.99)
    if state>=1:
        place(b,sil('soa',8404,int(0.19*H),(14,14,18),0.95),0.49,0.879)
    if state==3:
        warm=Image.new('RGBA',b.size,(255,190,140,255)); b=Image.blend(b,ImageChops.multiply(b,warm),0.55)
    return b.convert('RGB')
LAB=['처음 켰을 때','소아와 함께한 뒤','여러 결말을 겪은 뒤','진엔딩(새벽)']
def concept(name,bg,out):
    tw,th=360,526
    s=Image.new('RGB',(tw*4+5*12,th+70),(14,14,16)); d=ImageDraw.Draw(s); d.text((12,6),name,fill=(240,240,240),font=FB)
    for i in range(4):
        f=frame(bg,i).resize((tw,th)); s.paste(f,(12+i*(tw+12),58)); d.text((18+i*(tw+12),62),LAB[i],fill=(255,110,110),font=F)
    s.save(out,quality=88)
C=[('시안 1 · 고요한 수면','still_s8202_00001_.png'),('시안 2 · 불타는 산장(붉은 불티)','ash_s8202_00001_.png'),('시안 3 · 비 오는 밤','rain_s8101_00001_.png'),('시안 4 · 붉은 꽃잎','petals_s8202_00001_.png'),('시안 5 · 눈','snow_s8202_00001_.png')]
for i,(n,bg) in enumerate(C): concept(n,bg,'/tmp/tm/c%d.jpg'%(i+1))
