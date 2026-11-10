import numpy as np
from PIL import Image, ImageFilter
def smooth_noise(rng,n,scale):
    k=max(2,int(n/scale)); pts=rng.normal(0,1,k+3)
    x=np.linspace(0,k,n); i=x.astype(int); f=x-i; f=f*f*(3-2*f)
    return pts[i]*(1-f)+pts[i+1]*f
def stroke(seed,W=1024,H=200):
    rng=np.random.default_rng(seed)
    xs=np.linspace(0,1,W); ys=np.arange(H)[:,None]
    cy=H*0.5+H*0.035*smooth_noise(rng,W,300)
    st=0.03; en=0.94+0.03*rng.random()
    head=np.sqrt(np.clip((xs-st)/0.05,0,1))
    tail=np.clip((en-xs)/0.42,0,1)**0.55
    th=H*0.31*head*tail*(1+0.06*smooth_noise(rng,W,160))
    top=cy-th*(1+0.05*smooth_noise(rng,W,70)); bot=cy+th*(1+0.05*smooth_noise(rng,W,70))
    inside=(ys>=top[None,:])&(ys<=bot[None,:])
    dry=np.clip((xs-0.50)/(en-0.50),0,1)**1.3
    rowthr=np.clip(smooth_noise(rng,H,3)*0.5+0.5+0.12*rng.normal(0,1,H),0,1)[:,None]
    keep=(rowthr+0.06) > dry[None,:]*1.05
    rel=np.abs((ys-cy[None,:])/(th[None,:]+1e-3))
    keep&= ~((rel>0.82)&(rng.random((H,W))<dry[None,:]*0.7))
    ink=0.95+0.05*smooth_noise(rng,H,4)[:,None]
    ink=ink*(1-0.30*dry[None,:])
    a=np.where(inside&keep,ink,0).astype(np.float32)
    a*=(0.93+0.07*rng.random((H,W)))
    im=Image.fromarray((np.clip(a,0,1)*255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))
    out=np.zeros((H,W,4),np.uint8); out[...,:3]=255; out[...,3]=np.array(im)
    return Image.fromarray(out)
prev=Image.new('RGBA',(1024,200*3),(225,222,214,255))
for i,sd in enumerate([5,19,42]):
    s=stroke(sd); s.save('MolakTitleBrush%d.png'%i)
    tint=Image.new('RGBA',s.size,(25,22,20,255)); tint.putalpha(s.split()[3])
    prev.alpha_composite(tint,(0,200*i))
prev.convert('RGB').resize((512,300)).save('brush_prev.jpg')
