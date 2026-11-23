# -*- coding: utf-8 -*-
"""옛 입상의 머리 뒤 붉은 원광을 새 입상 뒤에 옮겨 깐다(교주). 로라 그림이 원광을 빠뜨렸을 때.
  python3 tools/art/lora/carry_halo.py 옛.png 새.png 출력.png
원광 = 옛 입상 위 1/4 안의 가장 큰 붉은 덩어리(구멍 메우고, 머리에 가려진 줄마다 왼끝~오른끝을 채움).
자리 = 두 입상의 머리 꼭대기(원광 뺀 알파 맨 위)와 그 아래 120화소 띠의 가로 중심 차이만큼 옮긴다."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

def top (arr):
    al = arr[..., 3] > 32; ys = np.where(al.any(1))[0]; y0 = ys[0]
    xs = np.where(al[y0:y0 + 120].any(0))[0]; return y0, xs.mean()

def main ():
    o = np.asarray(Image.open(sys.argv[1]).convert('RGBA')).astype(int)
    n = Image.open(sys.argv[2]).convert('RGBA')
    r, g, b, a = o[..., 0], o[..., 1], o[..., 2], o[..., 3]
    red = (r > 150) & (g < 90) & (b < 90) & (a > 100); red[int(o.shape[0] * 0.25):] = False
    lab, k = ndimage.label(red)
    if not k: n.save(sys.argv[3]); print('원광 없음'); return
    big = 1 + int(np.argmax(ndimage.sum(red, lab, range(1, k + 1))))
    halo = ndimage.binary_fill_holes(ndimage.binary_closing(lab == big, iterations=6))
    for y in np.where(halo.any(1))[0]:
        xs = np.where(halo[y])[0]; halo[y, xs.min():xs.max() + 1] = True
    col = tuple(int(c) for c in np.median(o[lab == big][:, :3], 0))
    lay = np.zeros((o.shape[0], o.shape[1], 4), np.uint8); lay[halo] = col + (255,)
    oa = o.copy(); oa[halo, 3] = 0
    y0o, x0o = top(oa); y0n, x0n = top(np.asarray(n).astype(int))
    L = Image.fromarray(lay, 'RGBA').transform(n.size, Image.AFFINE, (1, 0, -(x0n - x0o), 0, 1, -(y0n - y0o)))
    L.alpha_composite(n); L.save(sys.argv[3]); print('원광 옮김', round(x0n - x0o, 1), y0n - y0o)

if __name__ == '__main__':
    main()
