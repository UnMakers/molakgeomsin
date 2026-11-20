import sys,numpy as np
from PIL import Image
a=np.asarray(Image.open(sys.argv[1]).convert('RGB')).astype(np.float32)
r,g,b=a[...,0],a[...,1],a[...,2]
grey=0.299*r+0.587*g+0.114*b
keep=(r-np.maximum(g,b))>90
out=a.copy()
for c in range(3): out[...,c]=np.where(keep,a[...,c],grey)
Image.fromarray(out.clip(0,255).astype(np.uint8)).save(sys.argv[2])
