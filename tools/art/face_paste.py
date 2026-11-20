import sys, json, numpy as np
from PIL import Image, ImageFilter
base, face, box, out = sys.argv[1], sys.argv[2], json.loads(sys.argv[3]), sys.argv[4]
x,y,s = box
b = Image.open(base).convert('RGBA'); f = Image.open(face).convert('RGB').resize((s,s), Image.LANCZOS)
# 가장자리 부드럽게 섞는 가림판(안쪽 75%는 새 얼굴, 바깥으로 갈수록 원본)
m = np.zeros((s,s), np.float32); yy,xx = np.mgrid[0:s,0:s]; cx=cy=s/2
d = np.sqrt(((xx-cx)/(s*0.5))**2 + ((yy-cy)/(s*0.5))**2)
m = np.clip((1.0-d)/0.25, 0, 1)
reg = b.crop((x,y,x+s,y+s)); arr = np.array(reg).astype(np.float32)
# 원본 알파 위의 색만 바꾼다(배경은 흰색이었으므로 원본 알파 유지)
fa = np.array(f).astype(np.float32)
a = arr[...,3:4]/255.0
# 흰 배경과 합성됐던 원본 색 복원 기준: 새 얼굴 색을 그대로 쓰되 알파는 원본
mix = m[...,None]
arr[...,:3] = arr[...,:3]*(1-mix) + fa*mix
# 새 얼굴이 원래 윤곽 밖으로 나온 곳은 알파를 넓힌다(흰 바탕은 투명 그대로)
ink = np.clip((245.0 - fa.mean(axis=2)) / 40.0, 0, 1)
newa = np.clip(ink * m, 0, 1) * 255.0
if len(sys.argv) > 5 and sys.argv[5] == "grow": arr[...,3] = np.maximum(arr[...,3], newa)
reg2 = Image.fromarray(np.clip(arr,0,255).astype(np.uint8),'RGBA')
b.paste(reg2,(x,y)); b.save(out)
